using UnityEngine;
using Framework.Utils; // FIFOBuffer< T >
using Framework.Logger;

namespace Game.Logic
{
  public class BossBrain : MonsterBrain
  {
    #region PUBLIC METHODS

    #region PUBLIC OVERRIDDENS
    /**
     * initialize this brain
     * NOTE:m_bIsInitialized MUST be SET in doInitialize() method of derived class
     */
    public override void doInitialize(Character a_pCharacterModel)
    {
      // at first, initialize the base
      base.doInitialize(a_pCharacterModel);

      // BossBrain MUST be mounted to a Boss
      if(false == (m_pCharacterModel is Boss))
      {
        DefaultGameLogger.Instance.dumpLogError("[[BossBrain]][doInitialize][" + gameObject.ToString() + "]: Character Model MUST be Boss Type");

        return;
      }

      // set initial state
      m_eCurState = BrainStates.BRAIN_STATES_IDLE_MODE;

      // set initial rage points
      m_fCurRagePoint = 0.0f;

      // create event queue with maximal 5 events
      m_pEventQueue = new FIFOBuffer< BrainEvent >(5);

      // set initialization flag
      m_bIsInitialized = true;
    }

    /**
     * destroy this brain
     * NOTE:m_bIsInitialized MUST be CLEARED in doDestroy() method in derived class
     */
    public override void doDestroy()
    {
      // reset brain state
      m_eCurState = BrainStates.BRAIN_STATES_IDLE_MODE;

      // clear up the event queue
      m_pEventQueue.clear();

      // at last, destroy the base
      base.doDestroy();

      // clear initialization flag
      m_bIsInitialized = false;
    }

    /**
     * called when a collision happened and the mounted character can handle this collision
     */
    public override void onCollision(Entity a_pTrigger,                   // who has triggered this Hit
                                     CollisionHandler a_pCollidedPart     // collided part
                                    )
    {
      switch(a_pTrigger.EntityType)
      {
      // when a weapon hit this monster
      case EntityTypes.ENTITY_TYPES_WEAPON:
        {
          // if the weapon is NOT dead
          if(false == a_pTrigger.IsDead)         
          {
            // if the weapon is from the opposite side
            if(CharacterTypes.CHARACTER_TYPES_HERO == ((Weapon)a_pTrigger).OwnerType
               ||
               CharacterTypes.CHARACTER_TYPES_HERO_REMOTE == ((Weapon)a_pTrigger).OwnerType
               ||
               CharacterTypes.CHARACTER_TYPES_TOADY == ((Weapon)a_pTrigger).OwnerType
              )
            {
              // let base class handle this collision
              base.onCollision(a_pTrigger, a_pCollidedPart);

              // update the current rage points
              updateRagePoints(a_pTrigger, a_pCollidedPart);
            }
          }

          break;
        }

        default:
        {
          break;
        }
      }
    }

    #region CHARACTER STATE CHANGE END CALLBACKS
    /**
     * called when UnSpawn animation has done: JUST simulated
     */
    public override void onUnSpawningEnded()
    {
      // destroy this monster
      MonsterMgr.Instance.destroyBoss(m_pCharacterModel.gameObject);
    }

    /**
     * called when Hit animation has done: Hit Animation will go automatically to Idle state when done
     * normally brain is supposed to submit: doIdleCom()
     */
    public override void onHitEnded()
    {
      // if former state was walking or running before hit: defend if we need
      if(EntityStates.ENTITY_STATES_WALKING == m_pCharacterModel.PreEntityState
         ||
         EntityStates.ENTITY_STATES_RUNNING == m_pCharacterModel.PreEntityState
        )
      {
        decideDefendAction(); // will change brain state after start defending
      }

      // by defending: return: defend will roll back the action before hit
      if(BrainStates.BRAIN_STATES_DEFENDING_MODE == m_eCurState)
      {
        return;
      }

      // otherwise, roll back to previous action in any brain state
#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][onHitEnded][" + gameObject.ToString() + "] Roll Back to Last Com");
#endif

      m_pCharacterModel.doLastCom();
    }

    /**
     * called when Struck animation has done
     * normally brain is supposed to submit: doIdleCom()
     */
    public override void onStruckEnded()
    {
      onHitEnded();
    }

    /**
     * called when any sort of battle animation ended: Normal Attack, Melee or Rage Attack Animation
     * - each sort of battle has its OWN start method: some needs a trigger, but some needs a boolean Flag to start that animation
     * - go back to Idle state and let update routine determines the next action
     */
    public override void onAttackEnded()
    {
#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][onAttackEnded][" + gameObject.ToString() + "]");
#endif

      // bring model back to idle state at first
      m_pCharacterModel.doIdleCom();

      // let the brain decide the next attack type
      decideAttackAction();
    }

    /**
     * called when Target Position has been reached
     * - go back to Idle state and let update routine determines the next action
     */
    public override void onTargetPosReached()
    {
      // change the model to idle state at first
      m_pCharacterModel.doIdleCom();

      // by waiting for move complete: goto idle state
      if(BrainStates.BRAIN_STATES_WAITING_FOR_MOVE_COMPLETE == m_eCurState)
      {
#if !RETAIL
        //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][onTargetPosReached][Move Complete][" + gameObject.ToString() + "] Brain State: " + m_eCurState.ToString() + "-->" + BrainStates.BRAIN_STATES_IDLE_MODE.ToString());
#endif

        m_eCurState = BrainStates.BRAIN_STATES_IDLE_MODE;
      }
      else
      {
        // other states: decide next attack
        decideAttackAction();
      }
    }
    #endregion // CHARACTER STATE CHANGE END CALLBACKS

    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS
    // do the given attack action
    protected void doAttackAction(AttackAnimationData a_pExpectedAttackAnimationData)
    {
      // check if we can do this attack action directly
      // 1. this attack is the first attack animation and using a Melee Weapon
      // 2. this attack is NOT first attack animation and has the same minimal attack distance as the former one
      if((null == m_pCharacterModel.LastAttackAnimationData && WeaponTypes.WEAPON_TYPES_MELEEWEAPON == a_pExpectedAttackAnimationData.UsedWeapon.WeaponType)
          ||
          (null != m_pCharacterModel.LastAttackAnimationData && m_pCharacterModel.LastAttackAnimationData.UsedWeapon.MinAttackDist == a_pExpectedAttackAnimationData.UsedWeapon.MinAttackDist)
        )
      {
        // do attack directly
#if !RETAIL
        //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][doAttackAction][" + gameObject.ToString() + "] doAttackCom: " + a_pExpectedAttackAnimationData.Trigger);
#endif

        m_pCharacterModel.doAttackCom(a_pExpectedAttackAnimationData);  

        // bring brain back to idle state
#if !RETAIL
        //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][doAttackAction][" + gameObject.ToString() + "] Brain State: " + m_eCurState.ToString() + "-->" + BrainStates.BRAIN_STATES_IDLE_MODE.ToString());
#endif

        m_eCurState = BrainStates.BRAIN_STATES_IDLE_MODE;
      }
      else
      {
        // we have to move the boss forwards/backwards along the attack direction
        // former weapon minimal attack distance - expected weapon minimal attack distance := move distance
        float _fMoveDist = null == m_pCharacterModel.LastAttackAnimationData
                         ?
                           -a_pExpectedAttackAnimationData.UsedWeapon.MinAttackDist
                         :
                           m_pCharacterModel.LastAttackAnimationData.UsedWeapon.MinAttackDist - a_pExpectedAttackAnimationData.UsedWeapon.MinAttackDist;

        // move the boss
#if !RETAIL
        //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][doAttackAction][" + gameObject.ToString() + "] doMoveFrontBackCom: " + _fMoveDist);
#endif

        m_pCharacterModel.doMoveFrontBackCom(_fMoveDist);

        // store this attack request
        BrainEvent _pAttackEvent = new BrainEvent(BrainEventIDs.BRAIN_EVENT_IDS_EXPECTING_ATTACK, a_pExpectedAttackAnimationData);
        m_pEventQueue.push(_pAttackEvent);

        // change brain state to waiting state
#if !RETAIL
        //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][doAttackAction][" + gameObject.ToString() + "] Brain State: " + m_eCurState.ToString() + "-->" + BrainStates.BRAIN_STATES_WAITING_FOR_MOVE_COMPLETE.ToString());
#endif

        m_eCurState = BrainStates.BRAIN_STATES_WAITING_FOR_MOVE_COMPLETE;
      }
    }

    // do the given rage attack action
    protected void doRageAttackAction(AttackAnimationData a_pExpectedRageAttackAnimationData)
    {
      // check if we can do this attack action directly
      // 1. this attack is the first attack animation and using a Melee Weapon
      // 2. this attack is NOT first attack animation and has the same minimal attack distance as the former one
      if((null == m_pCharacterModel.LastAttackAnimationData && WeaponTypes.WEAPON_TYPES_MELEEWEAPON == a_pExpectedRageAttackAnimationData.UsedWeapon.WeaponType)
          ||
          (null != m_pCharacterModel.LastAttackAnimationData && m_pCharacterModel.LastAttackAnimationData.UsedWeapon.MinAttackDist == a_pExpectedRageAttackAnimationData.UsedWeapon.MinAttackDist)
        )
      {
        // YES
        // attack with the expected weapon
#if !RETAIL
        //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][doRageAttackAction][" + gameObject.ToString() + "] doRageAttackCom");
#endif

        ((Boss)m_pCharacterModel).doRageAttackCom(a_pExpectedRageAttackAnimationData);

        // change to raging mode
#if !RETAIL
        //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][doRageAttackAction][" + gameObject.ToString() + "] Brain State: " + m_eCurState.ToString() + "-->" + BrainStates.BRAIN_STATES_RAGING_MODE.ToString());
#endif

        m_eCurState = BrainStates.BRAIN_STATES_RAGING_MODE;
      }
      else
      {
        // NO
        // move forwards or backwards along attack direction
        // former weapon minimal attack distance - expected weapon minimal attack distance := move distance
        float _fMoveDist = null == m_pCharacterModel.LastAttackAnimationData
                         ?
                           -a_pExpectedRageAttackAnimationData.UsedWeapon.MinAttackDist
                         :
                           m_pCharacterModel.LastAttackAnimationData.UsedWeapon.MinAttackDist - a_pExpectedRageAttackAnimationData.UsedWeapon.MinAttackDist;

        // move the boss
#if !RETAIL
        //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][doRageAttackAction][" + gameObject.ToString() + "] doMoveFronBackCom: " + _fMoveDist);
#endif

        m_pCharacterModel.doMoveFrontBackCom(_fMoveDist);

        // store the brain event
        BrainEvent _pAttackEvent = new BrainEvent(BrainEventIDs.BRAIN_EVENT_IDS_EXPECTING_RAGE_ATTACK, a_pExpectedRageAttackAnimationData);
        m_pEventQueue.push(_pAttackEvent);

        // change brain state to waiting state
#if !RETAIL
        //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][doRageAttackAction][" + gameObject.ToString() + "] Brain State: " + m_eCurState.ToString() + "-->" + BrainStates.BRAIN_STATES_WAITING_FOR_MOVE_COMPLETE.ToString());
#endif

        m_eCurState = BrainStates.BRAIN_STATES_WAITING_FOR_MOVE_COMPLETE;
      }
    }

    #region PROTECTED OVERRIDDENS
    /**
     * doUpdate() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last doUpdate() call
     * check true == m_bIsInitialized before doing the real thing
     */
    protected override void doUpdate()
    {
      // if NOT initialized: return
      if(false == m_bIsInitialized)
      {
        return;
      }

      base.doUpdate();

      // check current brain state
      switch(m_eCurState)
      {
        case BrainStates.BRAIN_STATES_IDLE_MODE:
        {
          // idle state
          // there is pending events
          if(m_pEventQueue.isEmpty() == false)
          {
            // pop up the oldest event
            BrainEvent _pEvent = m_pEventQueue.pop();

            // check event ID
            switch(_pEvent.EventID)
            {
              case BrainEventIDs.BRAIN_EVENT_IDS_EXPECTING_ATTACK:
              {
#if !RETAIL
                //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][Update][Brain Event][" + gameObject.ToString() + "] doAttackCom: " + (_pEvent.EventData as AnimationData).Trigger);
#endif

                m_pCharacterModel.doAttackCom(_pEvent.EventData as AnimationData);

                break;
              }

              case BrainEventIDs.BRAIN_EVENT_IDS_EXPECTING_RAGE_ATTACK:
              {
#if !RETAIL
                //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][Update][Brain Event][" + gameObject.ToString() + "] doRageAttackCom");
#endif

                ((Boss)m_pCharacterModel).doRageAttackCom(_pEvent.EventData as AnimationData);

                // change to raging mode
#if !RETAIL
                //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][Update][Brain Event][" + gameObject.ToString() + "] Brain State: " + m_eCurState.ToString() + "-->" + BrainStates.BRAIN_STATES_RAGING_MODE.ToString());
#endif

                m_eCurState = BrainStates.BRAIN_STATES_RAGING_MODE;

                break;
              }

              default:
              {
                break;
              }
            }
          }

          break;
        }

        case BrainStates.BRAIN_STATES_DEFENDING_MODE:
        case BrainStates.BRAIN_STATES_RAGING_MODE:
        {
          // done with defending or rage attacking
          if(m_fCurCoolDownTime < 1.0e-5f)
          {
            // zero the rage point
            m_fCurRagePoint = 0.0f;

            // done with defending
            if(BrainStates.BRAIN_STATES_DEFENDING_MODE == m_eCurState)
            {
              // go out of defending
#if !RETAIL
              //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][Update][Defending][" + gameObject.ToString() + "] doUnDefendCom");
#endif

              m_pCharacterModel.doUnDefendCom(); // roll back to the action before hit

              // return to idle state
#if !RETAIL
              //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][Update][Defending][" + gameObject.ToString() + "] Brain State: " + m_eCurState.ToString() + "-->" + BrainStates.BRAIN_STATES_IDLE_MODE.ToString());
#endif

              m_eCurState = BrainStates.BRAIN_STATES_IDLE_MODE;
            }

            // done with rage attacking
            else
            {
              // go out of rage attacking
#if !RETAIL
              //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][Update][Raging][" + gameObject.ToString() + "] doUnRageAttackCom");
#endif

              // doUnRageAttackCom() --> onAttackEnded()
              ((Boss)m_pCharacterModel).doUnRageAttackCom();
            }

            break;
          }

          // otherwise, decrease the timer
          m_fCurCoolDownTime -= Time.deltaTime;

          break;
        }

        default:
        {
          break;
        }
      }
    }

    /**
     * return the selected attack target
     * NOTE: always returns the melee attack target, which ALSO supports shooting, if this boss has one shoot weapon
     */
    protected override bool getAttackTarget(ref AttackTargetInfo a_pAttackTarget)
    {
      // clear up the attack target
      a_pAttackTarget.OwnerIndex    = -1;
      a_pAttackTarget.Index         = -1;
      a_pAttackTarget.WorldSpacePos = Vector3.zero;
      a_pAttackTarget.ForwardDirWS  = Vector3.forward;
      a_pAttackTarget.RightDirWS    = Vector3.right;

      // the weapon used to get attack target info
      Weapon _pAttackWeapon = null;

      // check all equipped weapons: if there is a shoot weapon?
      for(int _iWeaponIdx = 0; _iWeaponIdx < m_pCharacterModel.EquippedWeaponList.Length; _iWeaponIdx++)
      {
        _pAttackWeapon = m_pCharacterModel.EquippedWeaponList[_iWeaponIdx];

        // if there is a ShootWeapon: we use the ShootWeapon as base weapon to get an attack position
        if(m_pCharacterModel.EquippedWeaponList[_iWeaponIdx] != null
           &&
           WeaponTypes.WEAPON_TYPES_SHOOTWEAPON == m_pCharacterModel.EquippedWeaponList[_iWeaponIdx].WeaponType
          )
        {
          break;
        }
      }

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][getAttackTargetPos][" + m_pCharacterModel.gameObject.ToString() + "]: trying to get Attack Pos");
#endif

      // NO attack target
      if(null == GameLevelMgr.Instance.CurrentGameLevel
         ||
         false == (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).getMonsterAttackTarget(_pAttackWeapon, ref a_pAttackTarget)
        )
      {
        return false;
      }

      // shift along positive direction, if the attack weapon is shoot weapon
      if(WeaponTypes.WEAPON_TYPES_SHOOTWEAPON == _pAttackWeapon.WeaponType)
      {
        a_pAttackTarget.WorldSpacePos += a_pAttackTarget.ForwardDirWS * _pAttackWeapon.MinAttackDist;
      }

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][getAttackTargetPos][" + gameObject.ToString() + "]: Attack Pos based on weapon[" + _pBaseWeapon.WeaponType.ToString() + "]: " + _pBaseWeapon.ToString());
#endif

      return true;
    }

    // update the current rage points based on the Hitter and Collided part
    protected virtual void updateRagePoints(Entity a_pTrigger,                   // who has triggered this Hit
                                            CollisionHandler a_pCollidedPart     // collided part
                                           )
    {
      // get rage weight
      float _fWeight = 1.0f;

      // based on different collided part type
      switch(a_pCollidedPart.Type)
      {
      case HitObjectTypes.HIT_OBJECT_TYPES_HEAD:
        {
          _fWeight = 7.5f;

          break;
        }

      case HitObjectTypes.HIT_OBJECT_TYPES_BODY:
        {
          _fWeight = 3.5f;

          break;
        }

      case HitObjectTypes.HIT_OBJECT_TYPES_LIMB:
        {
          _fWeight = 1.5f;

          break;
        }

      default:
        {
          break;
        }
      }

      // calculate rage points: rage points := rewards points * part weight
      m_fCurRagePoint += a_pCollidedPart.Rewards * _fWeight;

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][updateRagePoints][RagePoints]: " + m_fCurRagePoint);
#endif
    }

    // decide if this boss will take defend action
    protected virtual void decideDefendAction()
    {
      // 25+: defending during moving
      // NO enough rage points: return
      if(m_fCurRagePoint < 25.1f)
      {
        return;
      }

      // NO defend animation at all: return
      if(0 == m_pCharacterModel.DefendAnimations.Length)
      {
        return;
      }

      // calculate defend cool down timer: [4, 6]
      m_fCurCoolDownTime = Random.value * 2.0f + 4.0f;

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][decideDefendAction][Defending]: CoolDown: " + m_fCurCoolDownTime);

      // do defend
      //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][decideDefendAction][" + gameObject.ToString() + "] doDefendCom");
#endif

      m_pCharacterModel.doDefendCom();

      // change to defending mode
#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][decideDefendAction][" + gameObject.ToString() + "] Brain State: " + m_eCurState.ToString() + "-->" + BrainStates.BRAIN_STATES_DEFENDING_MODE.ToString());
#endif

      m_eCurState = BrainStates.BRAIN_STATES_DEFENDING_MODE;
    }

    // decide which kind of attack action this boss will take based on current rage points
    protected virtual void decideAttackAction()
    {
      // randomly choose attack animations
      // choose randomly one rage attack animation
      AttackAnimationData _pCurExpectedRageAttackAnimationData   = m_pCharacterModel.getArrayRandomElement(((Boss)m_pCharacterModel).RageAnimations) as AttackAnimationData;

      // choose randomly one melee animation
      AttackAnimationData _pCurExpectedMeleeAttackAnimationData  = m_pCharacterModel.getArrayRandomElement(m_pCharacterModel.MeleeAnimations)        as AttackAnimationData;

      // choose randomly one normal attack animation
      AttackAnimationData _pCurExpectedNormalAttackAnimationData = m_pCharacterModel.getArrayRandomElement(m_pCharacterModel.AttackAnimations)       as AttackAnimationData;

      // if normal attack is not available: use melee attack
      if(null == _pCurExpectedNormalAttackAnimationData)
      {
        _pCurExpectedNormalAttackAnimationData = _pCurExpectedMeleeAttackAnimationData;
      }

      // if melee attack is not available: use normal attack
      if(null == _pCurExpectedMeleeAttackAnimationData)
      {
        _pCurExpectedMeleeAttackAnimationData = _pCurExpectedNormalAttackAnimationData;
      }

      // if rage attack is not available: use melee attack
      if(null == _pCurExpectedRageAttackAnimationData)
      {
        _pCurExpectedRageAttackAnimationData = _pCurExpectedMeleeAttackAnimationData;
      }

      /**
       * the current rage point:
       * -  0 --> 15: normal attack
       * - 15 --> 25: melee attack
       * - 25+: defending during moving
       * - 25+: rage attacking
       */

      // 0 --> 15: normal attack
      if(m_fCurRagePoint < 15.1f)
      {
        // use normal attack
        doAttackAction(_pCurExpectedNormalAttackAnimationData);
      }
      else if(m_fCurRagePoint < 25.1f)
      {
        // 15+ --> 25
        // use melee attack
        doAttackAction(_pCurExpectedMeleeAttackAnimationData);
      }
      else
      {
        // 25+
        // if there is NO rage attack animation: defend if we can
        if(0 == ((Boss)m_pCharacterModel).RageAnimations.Length)
        {
          decideDefendAction();
        }
        else
        {
          // if we can do rage attack
          // calculate rage attacking cool down timer: [5, 10]
          m_fCurCoolDownTime = Random.value * 5.0f + 5.0f;

#if !RETAIL
          //DefaultGameLogger.Instance.dumpLogInfo("[BossBrain][decideAttackAction][Rage Attacking]: CoolDown: " + m_fCurCoolDownTime);
#endif

          // do rage attack
          doRageAttackAction(_pCurExpectedRageAttackAnimationData);
        }
      }
    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    protected enum BrainStates
    {
      // idle mode
      BRAIN_STATES_IDLE_MODE,

      // waiting states
      // waiting for the finish of the current movement
      BRAIN_STATES_WAITING_FOR_MOVE_COMPLETE,

      // performing action states
      // brain is attacking
      BRAIN_STATES_ATTACKING_MODE,
      // brain is defending
      BRAIN_STATES_DEFENDING_MODE,
      // brain is raging
      BRAIN_STATES_RAGING_MODE,
    }
    // current brain state
    protected BrainStates m_eCurState;
    // current cool down timer
    protected float m_fCurCoolDownTime;
    /**
     * the current rage point:
     * -  0 --> 15: normal attack
     * - 15 --> 25: melee attack
     * - 25+: defending during moving
     * - 25+: rage attacking
     */
    protected float m_fCurRagePoint;
    // event queue
    protected FIFOBuffer< BrainEvent > m_pEventQueue;
    #endregion // PROTECTED DATA MEMBERS
  }

} // namespace Game.Logic

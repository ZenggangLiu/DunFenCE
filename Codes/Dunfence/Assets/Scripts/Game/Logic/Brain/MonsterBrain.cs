using UnityEngine;
using Framework.Logger;

namespace Game.Logic
{
  public class MonsterBrain : Brain
  {
    #region PUBLIC DATA MEMBERS
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    /**
     * return current attack building index
     */
    public int CurrentAttackBuildingIndex
    {
      get
      {
        return m_iAttackBuildingIndex;
      }
    }

    /**
     * return current attack target index
     */
    public int CurrentAttackTargetIndex
    {
      get
      {
        return m_iAttackTargetIndex;
      }
    }

    #region PUBLIC OVERRIDDENS
    /**
     * initialize this brain
     * NOTE:m_bIsInitialized MUST be SET in doInitialize() method of derived class
     */
    public override void doInitialize(Character a_pCharacterModel)
    {
      base.doInitialize(a_pCharacterModel);

      // MonsterBrain MUST be mounted to a Monster type
      if(false == (m_pCharacterModel is Monster))
      {
        DefaultGameLogger.Instance.dumpLogError("[MonsterBrain][doInitialize][" + gameObject.ToString() + "]: Character Model MUST be Monster Type");

        return;
      }

      // clear attack building's index
      m_iAttackBuildingIndex = -1;

      // clear attack target's index
      m_iAttackTargetIndex = -1;

      // clear find new attack target flag
      m_bFindNewAttackTarget = false;

      // set initialization flag
      m_bIsInitialized = true;
    }

    /**
     * destroy this brain
     * NOTE:m_bIsInitialized MUST be CLEARED in doDestroy() method in derived class
     */
    public override void doDestroy()
    {
      // unregester the building destroyed event handler
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_PLAYER_BUILDING_DESTROYED, onPlayerBuildingDestroyed);

      // clear attack building's index
      m_iAttackBuildingIndex = -1;

      // clear attack target's index
      m_iAttackTargetIndex = -1;

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
              // let weapon handle this hit
              ((Weapon)a_pTrigger).onCollision(m_pCharacterModel, a_pCollidedPart);

              // handle this hit internally
              // gain RP
              if(a_pCollidedPart.Rewards != 0)
              {
                EconomyMgr.Instance.gainRP(a_pCollidedPart.Rewards);
              }

              // submit hit command
              m_pCharacterModel.doHitCom(a_pTrigger, a_pCollidedPart);
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
    
    /**
     * called when doHeadToCom() has ended
     */
    public override void onHeadToEnded()
    {
      
    }

    #region CHARACTER STATE CHANGE END CALLBACKS
    /**
     * called when Spawn animation has done: keyframed animation or simulated animation
     * normally brain is supposed to submit: doIdleCom()
     */
    public override void onSpawningEnded()
    {
      // change simply to idle state
      m_pCharacterModel.doIdleCom();
    }

    /**
     * called when UnSpawn animation has done: JUST simulated
     */
    public override void onUnSpawningEnded()
    {
      // destroy this monster
      MonsterMgr.Instance.destroyMonster(m_pCharacterModel.gameObject);
    }

    /**
     * called when Hit animation has done: Hit Animation will go automatically to Idle state when done
     * normally brain is supposed to submit: doIdleCom()
     */
    public override void onHitEnded()
    {
      m_pCharacterModel.doLastCom();
    }

    /**
     * called when Struck animation has done
     * normally brain is supposed to submit: doIdleCom()
     */
    public override void onStruckEnded()
    {
      m_pCharacterModel.doLastCom();
    }

    /**
     * called when Dead animation has done
     * normally brain is supposed to submit: doIdleCom()
     */
    public override void onDeadEnded()
    {
      // unSpawn this monster
      m_pCharacterModel.doUnSpawnCom();
    }

    /**
     * called when Jump animation has done
     * normally brain is supposed to submit: doIdleCom()
     */
    public override void onJumpEnded()
    {
      m_pCharacterModel.doIdleCom(); 
    }

    /**
     * called when Leap animation has done
     * normally brain is supposed to submit: doIdleCom()
     */
    public override void onLeapEnded()
    {
      m_pCharacterModel.doIdleCom();
    }

    /**
     * called when Attack or Melee animation has done
     * normally brain is supposed to submit: doIdleCom() then doAttackCom() based on some sort of strategy and current state
     */
    public override void onAttackEnded()
    {
      // change to idle state at first
      m_pCharacterModel.doIdleCom();

      // then start next attack
      startNextAttack();
    }

    /**
     * called when Target Position has been reached
     * normally brain is supposed to submit: doIdleCom() then doAttackCom() based on some sort of strategy and current state
     */
    public override void onTargetPosReached()
    {
      // change to idle state at first
      m_pCharacterModel.doIdleCom();

      // then start an attack
      startNextAttack();
    }
    #endregion // CHARACTER STATE CHANGE END CALLBACKS

    #endregion // PUBLIC OVERRIDDENS
    
    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS
    
    #region PROTECTED OVERRIDDENS
    /**
     * doResetInEditor() is called when the user hits the Reset button in the Inspector's context menu
     * or when adding the component the first time 
     * This function is only called in editor mode 
     * doResetInEditor() is most commonly used to give good default values in the inspector
     */
    protected override void doResetInEditor()
    {
      BrainType = BrainTypes.BRAIN_TYPES_AI_CONTROLLED;
    }

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

      // if there is NO entity model: return
      if(null == m_pCharacterModel)
      {
        DefaultGameLogger.Instance.dumpLogError("[MonsterBrain][" + gameObject.ToString() + "]: NO Character Model: Check mounted Character type Script");

        return;
      }

      // get current character state
      EntityStates _eCurState = m_pCharacterModel.CurrentEntityState;

      // branching on the current state
      switch(_eCurState)
      {
        case EntityStates.ENTITY_STATES_INITIALIZING:
        {
          // initializing
          return;
        }

        case EntityStates.ENTITY_STATES_SPAWNING:
        {
          // spawning
          // if NOT simulating spawn: return
          if(false == m_pCharacterModel.IsSimulatingSpawn)
          {
            return;
          }

          // simulating spawn animation
          Monster _pRealModel = m_pCharacterModel as Monster;

          // update timer
          _pRealModel.CurrentDragTime -= Time.deltaTime;

          // check if spawn is done
          if(_pRealModel.CurrentDragTime < 1.0e-5f)
          {
            // YES: tell this brain, spawn is done
            onSpawningEnded();
          }
          else
          {
            // drag this monster
            float _fDragDistance = _pRealModel.DragSpeed * Time.deltaTime;

            gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y + _fDragDistance, gameObject.transform.position.z);

            // check if spawn is done
            if(gameObject.transform.position.y > 1.0e-5f)
            {
              // YES: tell this brain, spawn is done
              onSpawningEnded();
            }
          }
          return;
        }

        case EntityStates.ENTITY_STATES_IDLE:
        {
          // idle
          // if first time changed to idle state
          // select the move target based on weapon's attach range and move to it
          if(EntityStates.ENTITY_STATES_SPAWNING == m_pCharacterModel.PreEntityState)
          {
            moveToAttackTargetPos();    
          }

          return;
        }

        case EntityStates.ENTITY_STATES_UNSPAWNING:
        {
          // unspawn
          // if NOT simulating spawn: return
          if(false == m_pCharacterModel.IsSimulatingSpawn)
          {
            return;
          }

          // simulating unspawn animation
          Monster _pRealModel = m_pCharacterModel as Monster;

          // update timer
          _pRealModel.CurrentDragTime -= Time.deltaTime;

          // check if unspawn is done
          if(_pRealModel.CurrentDragTime < 1.0e-5f)
          {
            // YES: tell this brain, unspawn is done
            onUnSpawningEnded();
          }
          else
          {
            // drag this monster
            float _fDragDistance = -_pRealModel.DragSpeed * Time.deltaTime;

            gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y + _fDragDistance, gameObject.transform.position.z);

            // check if spawn is done
            if(gameObject.transform.position.y < -3.0f)
            {
              // YES: tell this brain, spawn is done
              onUnSpawningEnded();
            }
          }

          return;
        }

        default:
        { 
          if(true == m_bFindNewAttackTarget)
          {
            // unregester the building destroyed event handler
            Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_PLAYER_BUILDING_DESTROYED, onPlayerBuildingDestroyed);

            // before finding a new attack target cache current attack building index and attack target index
            int _iPrevAttackBuildingIndex = m_iAttackBuildingIndex;
            int _iPrevAttackTargetIndex   = m_iAttackTargetIndex;

            // found a new attack target
            if(true == moveToAttackTargetPos())
            {
              // decrease all sorts of attacker counts
              if((GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel) != null)
              {
                // decrease the attacker count of previous attacking building
                (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).CurrentBuildingAttackPosCache[_iPrevAttackBuildingIndex].OwnerBuilding.CurrentAttackerCounter--;

#if !RETAIL
                DefaultGameLogger.Instance.dumpLogInfo("[MonsterBrain][ChangeAttackTarget]: Leaving Building: "   + (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).CurrentBuildingAttackPosCache[_iPrevAttackBuildingIndex].OwnerBuilding.ToString() + " #Attacker: --> " + (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).CurrentBuildingAttackPosCache[_iPrevAttackBuildingIndex].OwnerBuilding.CurrentAttackerCounter);
                DefaultGameLogger.Instance.dumpLogInfo("[MonsterBrain][ChangeAttackTarget]: Targeting Building: " + (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).CurrentBuildingAttackPosCache[m_iAttackBuildingIndex].OwnerBuilding.ToString() + " #Attacker: --> " + (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).CurrentBuildingAttackPosCache[m_iAttackBuildingIndex].OwnerBuilding.CurrentAttackerCounter);
#endif

                // decrease the attcher count of the attack pos
                (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).CurrentBuildingAttackPosCache[_iPrevAttackBuildingIndex].AttackTargetList[_iPrevAttackTargetIndex].RealAttackPos.CurrentAttackerCount--;
              }
            }

            // clear the flag
            m_bFindNewAttackTarget = false;
          }

          return;
        }
      }
    }
    
    /**
     * decide and start which attack type will be taken as the next one
     */
    protected virtual void startNextAttack()
    {
      // get randomly one attack animation and melee animation
      AnimationData _pSelectedAttackAnimation = m_pCharacterModel.getArrayRandomElement(m_pCharacterModel.AttackAnimations);
      AnimationData _pSelectedMeleeAnimation  = m_pCharacterModel.getArrayRandomElement(m_pCharacterModel.MeleeAnimations);

      // if normal attack is not availble: use melee attack
      if(null == _pSelectedAttackAnimation)
      {
        _pSelectedAttackAnimation = _pSelectedMeleeAnimation;
      }

      // if melee attack is not availble: use normal attack
      if(null == _pSelectedMeleeAnimation)
      {
        _pSelectedMeleeAnimation = _pSelectedAttackAnimation;
      }

      // 70% next attack will be Normal
      // 30% next attack will be Melee
      int _iAttackIndex = (int)(Random.value * 99.0f);  // [0, 99]
      if(_iAttackIndex <= 69)
      {
        // 70% normal attack
        m_pCharacterModel.doAttackCom(_pSelectedAttackAnimation);
      }
      else
      {
        // 30% melee attack
        m_pCharacterModel.doAttackCom(_pSelectedMeleeAnimation);
      }
    }

    /**
     * return the selected attack target
     */
    protected virtual bool getAttackTarget(ref AttackTargetInfo a_pAttackTarget)
    {
      // clear up the attack target
      a_pAttackTarget.OwnerIndex    = -1;
      a_pAttackTarget.Index         = -1;
      a_pAttackTarget.WorldSpacePos = Vector3.zero;
      a_pAttackTarget.ForwardDirWS  = Vector3.forward;
      a_pAttackTarget.RightDirWS    = Vector3.right;

      // get the attack pos based on the base weapon: first weapon
#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[MonsterBrain][getAttackTargetPos][" + m_pCharacterModel.gameObject.ToString() + "]: trying to get Attack Pos");
#endif

      // if we can NOT get an attack pos
      if(null == GameLevelMgr.Instance.CurrentGameLevel
         ||
         false == (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).getMonsterAttackTarget(m_pCharacterModel.EquippedWeaponList[0], ref a_pAttackTarget)
        )
      {
        return false;
      }

      return true;
    }

    /**
     * try to get an attack target pos and move this monster to it
     * return true on success
     */
    protected virtual bool moveToAttackTargetPos()
    {
      AttackTargetInfo _pAttackTarget = new AttackTargetInfo();

      if(true == getAttackTarget(ref _pAttackTarget))
      {
        // register this character model on building destroyed event
        Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_PLAYER_BUILDING_DESTROYED, onPlayerBuildingDestroyed);

        // store the attack building index
        m_iAttackBuildingIndex = _pAttackTarget.OwnerIndex;

        // store the attack target index
        m_iAttackTargetIndex = _pAttackTarget.Index;

        // tell model to walk to that target
        bool _bIsRunning = Random.value > 0.5f;

#if !RETAIL
        //DefaultGameLogger.Instance.dumpLogDebug("[MonsterBrain][moveToAttackTargetPos][" + gameObject.ToString() + "]: move To: " + _pAttackTarget.RealAttackPos.ToString() + "Run: " + _bIsRunning);
#endif

        m_pCharacterModel.doMoveToCom(_pAttackTarget.WorldSpacePos, _pAttackTarget.ForwardDirWS, _bIsRunning);

        return true;
      }

      return false;
    }

    // callback when one player building is destroyed: data: reference to the building
    protected virtual void onPlayerBuildingDestroyed(Game.Event.GameEvent a_pEventRef)
    {
      if(null == GameLevelMgr.Instance.CurrentGameLevel   // wrong level type
         ||
         (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).CurrentBuildingAttackPosCache.Length <= m_iAttackBuildingIndex   // NO enough buildings
        )
      {
        return;
      }
        
      // different building has been destroyed
      if((a_pEventRef.Data as Building) != (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).CurrentBuildingAttackPosCache[m_iAttackBuildingIndex].OwnerBuilding)
      {
        return;
      }

      // when the attacking building is destroyed, find another building
      // mark it
      m_bFindNewAttackTarget = true;
    }
    #endregion // PROTECTED OVERRIDDENS
    
    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    // attack building's index within building cache
    protected int m_iAttackBuildingIndex;
    // attack target's index within building's attack pos list
    protected int m_iAttackTargetIndex;
    // need new attack target flag
    bool m_bFindNewAttackTarget;
    #endregion // PROTECTED DATA MEMBERS
  }
} // namespace Game.Logic

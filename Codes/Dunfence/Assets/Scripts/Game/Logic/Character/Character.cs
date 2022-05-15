/**
 * Base class of Monster, Evil, Bummer, Toady, Hero
 * - provides common interface to controll animations
 * - if this character has equipped a weapon USE Animation to start using and stop using it via Animation Events: onUseWeapon and onStopUseWeapon
 */

using System;   // Math.Asb()
using UnityEngine;
using Framework.Utils;
using Framework.Logger;
using Game.Event;
using System.Collections.Generic; // List<T>

namespace Game.Logic
{
  // character types
  public enum CharacterTypes
  {
    CHARACTER_TYPES_UNKNOWN = 0,
    CHARACTER_TYPES_MONSTER,                        // normal monster: weak attack and no magic
    CHARACTER_TYPES_BOSS,                           // powerful monster: strong attack with different magic
    CHARACTER_TYPES_BUMMER,                         // non-hostile character, who always bums helps off the current player: like to rescue him/her
    CHARACTER_TYPES_TOADY,                          // AI controlled normal hero: weak attack and no magic, helping user controlled hero
    CHARACTER_TYPES_HERO,                           // user controlled hero: divers attack with divers magic
    CHARACTER_TYPES_HERO_REMOTE,                    // remote user controlled hero: divers attack with divers magic
  }

  public enum MoveTypes
  {
    MOVE_TYPES_NO_MOVE = 0,                         // stationary NO movement at all
    MOVE_TYPES_ROT_TO_HEADINGDIR,                   // rotate this character to the currently expected heading direction
    MOVE_TYPES_FIND_HEADINGDIR,                     // find a heading direction which can lead this character reach the target position
    MOVE_TYPES_MOVE_ALONG_FORWARDDIR_FOREVER,       // move along currrent forward direction forever
    MOVE_TYPES_MOVE_ALONG_FORWARDDIR_TO_TARGET,     // move along currrent forward direction to the target position
    MOVE_TYPES_ROT_TO_TARGETDIR,                    // rotate from current forward direction to the given target orientation
  }

  // data used during each kind of attack: normal, melee and rage
  [Serializable]
  public class AttackAnimationData : AnimationData
  {
    public AttackAnimationData(string a_szTrigger, Weapon a_pUsedWeapon)
    :
      base(a_szTrigger)
    {
      UsedWeapon = a_pUsedWeapon;
    }
    // used weapon reference
    public Weapon UsedWeapon;
  }

  public class Character : Entity
  {
    #region PUBLIC DATA MEMBERS
    // character type
    public CharacterTypes CharacterType = CharacterTypes.CHARACTER_TYPES_UNKNOWN;
    // name: can be empty, which will get a randomly generated name
    public string Name = null;
    // level: > 0
    public int Level = -1;
    // scale used during level up: > 0
    public float LevelUpScale = -1.0f;
    // maximal health points: > 0
    public int MaxHealth = -1;
    // maximal magic points(Mana): >= 0
    public int MaxMagic = -1;
    // maximal rewards points: >= 0
    public int MaxRewards = -1;
    // maximal protection points: >= 0
    public int MaxProtection = -1;
    // walk speed(Meter per Second): >= 0
    public float WalkSpeed = -1.0f;
    // run speed(Meter per Second): >= 0
    public float RunSpeed = -1.0f;
    // turn speed(Degree per Second): > 0: how fast this character can turn right or turn left
    public float TurnSpeed = -1.0f;
    // equipped weapon object
    public Weapon[] EquippedWeaponList = null;

    // animation trigger name list
    // NOTE: if there are more than ONE animation trigger names for one type of animation, resize the corresponding array

    // idle
    public AnimationData[] IdleAnimations = {new AnimationData("Idle")};
    // walk
    public AnimationData[] WalkAnimations = {new AnimationData("Walk")};
    // run
    public AnimationData[] RunAnimations = {new AnimationData("Run")};
    // jump
    public AnimationData[] JumpAnimations = {new AnimationData("Jump")};
    // leap: high jump
    public AnimationData[] LeapAnimations = {new AnimationData("Leap")};
    // attack
    public AttackAnimationData[] AttackAnimations = {new AttackAnimationData("Attack", null)};
    // melee
    public AttackAnimationData[] MeleeAnimations = {new AttackAnimationData("Melee", null)};
    // defend
    public AnimationData[] DefendAnimations = {new AnimationData("Defend")};
    // shoot
    public AnimationData[] ShootAnimations = {new AnimationData("Shoot")};
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    /**
     * get the mounted brain
     */
    public Brain MountedBrain
    {
      get
      {
        return m_pMountedBrain;
      }
    }

    // get previous entity state
    public EntityStates PreEntityState
    {
      get
      {
        return m_ePrevEntityState;
      }
    }

    /**
     * get current level
     */
    public int CurrentLevel
    {
      get
      {
        return m_iCurLevel;
      }
    }

    /**
     * get current integer XP
     */
    public int CurrentXP
    {
      get
      {
        return (int)(m_fCurXP + 0.5f) ;
      }
    }

    /**
     * get current integer HP
     */
    public int CurrentHP
    {
      get
      {
        return (int)(m_fCurHP + 0.5f);
      }
    }

    /**
     * get current integer MP
     */
    public int CurrentMP
    {
      get
      {
        return (int)(m_fCurMP + 0.5f);
      }
    }

    /**
     * get current integer RP
     */
    public int CurrentRP
    {
      get
      {
        return  (int)(m_fCurRP + 0.5f);
      }
    }

    /**
     * get current integer ProP
     */
    public int CurrentProP
    {
      get
      {
        return (int)(m_fCurProP + 0.5f);
      }
    }

    /**
     * get current damage reduction factor
     * - CausedDamage = RealWeaponDamage * (1.0 - CurrentProFactor)
     */
    public float CurrentProPFactor
    {
      get
      {
        return getCurrentProPFactor();
      }
    }

    // get latest attack animation data
    public AttackAnimationData LastAttackAnimationData
    {
      get
      {
        return m_pLastAttackAnimationData;
      }
    }

    // default constructor
    public Character()
    {
      // create all states
      m_pIdleState   = new IdleState();
      m_pWalkState   = new WalkState();
      m_pRunState    = new RunState();
      m_pAttackState = new AttackState();
      m_pShootState  = new ShootState();
    }

    #region PUBLIC OVERRIDDENS
    /**
     * Collsion Handler for any Entity
     * - Normal Entity(Except Weapon) will call weapon's onCollision() during its handling
     */
    public override void onCollision(Entity a_pTrigger,                   // who has triggered this Hit
                                     CollisionHandler a_pCollidedPart     // collided part
                                    )
    {
      // if this character can NOT handle a hit collsion: return
      if(false == canHandleCollision(a_pTrigger, a_pCollidedPart))
      {
        return;
      }

      // let the brain handle this collision
      if(null != m_pMountedBrain)
      {
        m_pMountedBrain.onCollision(a_pTrigger, a_pCollidedPart);
      }
    }

    // animation related commands
    /**
     * UnSpawn: last animation of whole entity's life
     */
    public override void doUnSpawnCom()
    {
      // if there is some equipped weapon: unequip them
      if(EquippedWeaponList != null
         && 
         EquippedWeaponList.Length > 0
        )
      {
        for(int _iWeaponIdx = 0; _iWeaponIdx < EquippedWeaponList.Length; _iWeaponIdx++)
        {
          EquippedWeaponList[_iWeaponIdx].doUnSpawnCom();  
        }
      }  
    }

    /**
     * Idle
     */
    public virtual void doIdleCom()
    {
      // already in idle state: return
      if(EntityStates.ENTITY_STATES_IDLE == m_eCurEntityState)
      {
        return;
      }

      // NO need to play Idle Animation, because each state will go back to Idle in some way

      // cache old state
      m_ePrevEntityState = m_eCurEntityState;

      // change to idle state
      m_eCurEntityState = EntityStates.ENTITY_STATES_IDLE;

      // change to no move mode
      m_eCurMoveType = MoveTypes.MOVE_TYPES_NO_MOVE;
    }

    // Walk and Run
    /**
     * move along current forward direction for given distance
     */
    public virtual void doMoveFrontBackCom(float a_fMoveDist)
    {
      // NOTE: NO check on moving state: because we accept continuously fixing target position via moving

      // if the character is not moving: play move animation
      if(MoveTypes.MOVE_TYPES_NO_MOVE == m_eCurMoveType)
      {
        playMoveAnimation(WalkAnimations);

        // cache old state
        m_ePrevEntityState = m_eCurEntityState;

        // change to the walk state
        m_eCurEntityState = EntityStates.ENTITY_STATES_WALKING;
      }

      // store movement direction
      m_fMoveDirFactor = a_fMoveDist < 0.0f ? -1.0f : 1.0f;

      // store expected heading dir
      m_vInitialExpectedHeadingDirWs = transform.forward * m_fMoveDirFactor;

      // store the target position
      m_vMoveTargetPosWs = transform.position + transform.forward * a_fMoveDist;

      // change to move along mode
      m_eCurMoveType = MoveTypes.MOVE_TYPES_MOVE_ALONG_FORWARDDIR_TO_TARGET;
    }

    /**
     * MoveAlong
     * move the character along the given direction vector forever in local space
     * a_bRunning: indicates if the character will be walking or running along the given direction
     * - head instantly in the given direction
     */
    public virtual void doMoveAlongCom(Vector3 a_vMoveDirLs,         // move direction in local space
                                       bool a_bRunning               // if the character will be running to that position
                                      )
    {
      // NOTE: NO check on moving state: because we accept continuously fixing target position via moving

      // if the character is not moving: play move animation
      if(MoveTypes.MOVE_TYPES_NO_MOVE == m_eCurMoveType)
      {
        // play run or walk animation based on a_bRunning flag
        if(true == a_bRunning)
        {
          playMoveAnimation(RunAnimations);
        }
        else
        {
          playMoveAnimation(WalkAnimations);
        }

        // positive movement
        m_fMoveDirFactor = 1.0f;

        // cache old state
        m_ePrevEntityState = m_eCurEntityState;

        // change to the right state
        m_eCurEntityState = true == a_bRunning ? EntityStates.ENTITY_STATES_RUNNING : EntityStates.ENTITY_STATES_WALKING;
      }

      // instant heading in the given direction
      // get given direction in world space
      Vector3 _vMoveDirWs = gameObject.transform.TransformVector(a_vMoveDirLs);
      // normalize the direction vector
      _vMoveDirWs.Normalize();

      // head to the given direction immediately: set it as the forward direction
      gameObject.transform.forward = _vMoveDirWs;

      // change to move along mode
      m_eCurMoveType = MoveTypes.MOVE_TYPES_MOVE_ALONG_FORWARDDIR_FOREVER;
    }

    /**
     * MoveTo
     * move the character to the given target position in world space
     * a_vTargetOrientationWs: the goal orientation of the target position in world space
     * a_bRunning: indicates if the character will be walking or running to the given target position
     * - at first, rotate and face TargetPos - CurPos in world space
     */
    public virtual void doMoveToCom(Vector3 a_vTargetPosWs,            // target position in world space
                                    Vector3 a_vTargetOrientationWs,    // target orientation in world space
                                    bool a_bIsRunning                  // if the character will be running to that position
                                   )
    {
      // NOTE: NO check on moving state: because we accept continuously fixing target position via moving

      // store the target position
      m_vMoveTargetPosWs = a_vTargetPosWs;

      // store the target orientation
      m_vMoveTargetOrienWs = a_vTargetOrientationWs;

      // calculate the expected heading direciton: CurPos ---> TargetPos
      m_vInitialExpectedHeadingDirWs = a_vTargetPosWs - gameObject.transform.position;

      // if the character is not moving
      if(MoveTypes.MOVE_TYPES_NO_MOVE == m_eCurMoveType)
      {
        // cache old state
        m_ePrevEntityState = m_eCurEntityState;

        // select the correct move animation flag
        if(0 == RunAnimations.Length                              // NO run animation at all
           ||                                                     // OR
           m_vInitialExpectedHeadingDirWs.sqrMagnitude < 0.01f    // within 1cm: just walk needed
           ||                                                     // OR
           false == a_bIsRunning                                    // we JUST need walking animation
           )
        {
          // walk
          playMoveAnimation(WalkAnimations);

          // change to walk state
          m_eCurEntityState = EntityStates.ENTITY_STATES_WALKING;
        }
        else
        {
          // run
          playMoveAnimation(RunAnimations);

          // change to run state
          m_eCurEntityState = EntityStates.ENTITY_STATES_RUNNING;        
        }
      }

      // positive movement
      m_fMoveDirFactor = 1.0f;

      // normalize the initial expected heading direction
      m_vInitialExpectedHeadingDirWs.Normalize();

      // if the initial heading direciton is away from the expected heading direction
      if(Vector3.Dot(transform.forward, m_vInitialExpectedHeadingDirWs) < 0.0f)
      {
        m_eCurMoveType = MoveTypes.MOVE_TYPES_ROT_TO_HEADINGDIR;
      }
      else
      {
        // start to find a heading direction, which will lead this character to the target position
        m_eCurMoveType = MoveTypes.MOVE_TYPES_FIND_HEADINGDIR;
      }
    }

    /**
     * DragTo
     * grag this character instantly to the given world space position with the given heading direction
     */
    public virtual void doDragTo(Vector3 a_vTargetPosWs,           // target position in world space
                                 Vector3 a_vTargetHeadDirWs        // target heading direction in world space
                                )
    {
      // set the given position
      gameObject.transform.position = a_vTargetPosWs;
      // set the given heading
      doHeadTo(a_vTargetHeadDirWs);

      // reset to idle state
      doIdleCom();
    }

    /**
     * HeadTo
     * rotate the forward to the given direciton
     */
    protected virtual void doHeadTo(Vector3 a_vHeadDirWs)
    {
      // normalize the given heading direction
      a_vHeadDirWs.Normalize();

      // adjust the forward direction to the given direction
      gameObject.transform.forward = a_vHeadDirWs;

      if(null != m_pMountedBrain)
      {
        m_pMountedBrain.onHeadToEnded();
      }
    }

    /**
     * Jump
     */
    public virtual void doJumpCom()
    {
      // already in jumping state: return
      if(EntityStates.ENTITY_STATES_JUMPING == m_eCurEntityState)
      {
        return;
      }

      // play the jump animation
      playAnimationTrigger(JumpAnimations);

      // store old state
      m_ePrevEntityState = m_eCurEntityState;

      // change to jumping state
      m_eCurEntityState = EntityStates.ENTITY_STATES_JUMPING;
    }

    /**
     * Leap: High Jump
     */
    public virtual void doLeapCom()
    {
      // already in leaping state: return
      if(EntityStates.ENTITY_STATES_LEAPING == m_eCurEntityState)
      {
        return;
      }

      // play the leap animation
      playAnimationTrigger(LeapAnimations);
      
      // store old state
      m_ePrevEntityState = m_eCurEntityState;
      
      // change to leaping state
      m_eCurEntityState = EntityStates.ENTITY_STATES_LEAPING;
    }

    /**
     * Attack
     * Attack Animation will drive equipped weapon usage
     */
    public virtual void doAttackCom(AnimationData a_pAttackAnimation)
    {
      // already in attacking state: return
      if(EntityStates.ENTITY_STATES_ATTACKING == m_eCurEntityState)
      {
        return;
      }

      // play the attack animation
      // on success, store the given attack animation data
      if(true == playAnimationTrigger(a_pAttackAnimation))
      {
        m_pLastAttackAnimationData = a_pAttackAnimation as AttackAnimationData;
      }
      
      // store old state
      m_ePrevEntityState = m_eCurEntityState;
      
      // change to attacking state
      m_eCurEntityState = EntityStates.ENTITY_STATES_ATTACKING;
    }

    /**
     * Defend
     */
    // NOTE: defend will be played after HIT, so can NOT store previous state: keep it same as before HIT
    public virtual void doDefendCom()
    {
      // already in defending state: return
      if(EntityStates.ENTITY_STATES_DEFENDING == m_eCurEntityState)
      {
        return;
      }

      // stop using any weapon
      onStopUseWeapon(-1);

      // NO need to push down state: just use the pushed state from HIT

      // play the defend animation: turn on its flag
      playDefendAnimation(DefendAnimations);

      // NO need to store previous state: keep it the same as from HIT

      // change to defending state
      m_eCurEntityState = EntityStates.ENTITY_STATES_DEFENDING;
    }

    /**
     * UnDefend
     */
    public virtual void doUnDefendCom()
    {
      if(EntityStates.ENTITY_STATES_DEFENDING != m_eCurEntityState)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][doUnDefendCom][" + gameObject.ToString() + "]: NOT in defending");

        return;
      }

      // stop the current defend animation
      stopDefendAnimation();

      // roll back to the action before hit
      doLastCom();
    }

    /**
     * Shoot
     * Shoot Animation will drive equipped shoot weapon usage
     */
    public virtual void doShootCom()
    {
      // already in shooting state: return
      if(EntityStates.ENTITY_STATES_SHOOTING == m_eCurEntityState)
      {
        return;
      }

      // play the shoot animation
      playAnimationTrigger(ShootAnimations);
      
      // store old state
      m_ePrevEntityState = m_eCurEntityState;
      
      // change to shooting state
      m_eCurEntityState = EntityStates.ENTITY_STATES_SHOOTING;
    }

    /**
     * Hit: normal hit
     * a_pTrigger: the entity who has trigger this Hit
     * a_pCollidedPart: which part of the object has been hit
     */
    public override void doHitCom(Entity a_pTrigger,                  // trigger entity
                                  CollisionHandler a_pCollidedPart    // collide part
                                 )
    {
      // already in hit state: return
      if(EntityStates.ENTITY_STATES_HIT == m_eCurEntityState)
      {
        return;
      }

      // can NOT handle hit: return
      if(false == canHandleHit())
      {
        return;
      }

      // stop using any weapon
      onStopUseWeapon(-1);

      // deduct HP
      // TODO: consider damage type
      // CausedDamage = RealWeaponDamage * (1.0 - CurrentProFactor)
      float _fCausedDamage = ((Weapon)a_pTrigger).DamagePoints * (1.0f - CurrentProPFactor);

#if !RETAIL
      // DefaultGameLogger.Instance.dumpLogInfo("[Character][doHitCom][" + gameObject.ToString() + "]: [Hit] Weapon Damage: " + ((Weapon)a_pTrigger).DamagePoints + " Caused Damange: " + _fCausedDamage + " Monster HP: " + m_fCurHP + " --> " + (m_fCurHP - _fCausedDamage));
#endif

      m_fCurHP -= _fCausedDamage;

      // show HP bar
      doShowHPBarCom();

      // check HP
      // NO HP anymore
      if(m_fCurHP < 1.0e-5f)
      {
        doDeadCom();
      }
      else
      {
        // push state
        pushState();

        // if the character is currently moving: stop movement animation
        if(EntityStates.ENTITY_STATES_WALKING == m_eCurEntityState
           ||
           EntityStates.ENTITY_STATES_RUNNING == m_eCurEntityState
          )
        {
          stopMoveAnimation(); 
        }

        // play the hit animation
        playAnimationTrigger(HitAnimations);

        // store old state
        m_ePrevEntityState = m_eCurEntityState;

        // change to hit state
        m_eCurEntityState = EntityStates.ENTITY_STATES_HIT;
      }
    }

    /**
     * Struck: strong hit
     * a_pTrigger: the entity who has trigger this Hit
     * a_pCollidedPart: which part of the object has been hit
     */
    public override void doStruckCom(Entity a_pTrigger,                // trigger entity
                                     CollisionHandler a_pCollidedPart  // collided part
                                    )
    {
      // already in struck state: return
      if(EntityStates.ENTITY_STATES_STRUCK == m_eCurEntityState)
      {
        return;
      }

      // stop using any weapon
      onStopUseWeapon(-1);

      // deduct HP
      // TODO: consider damage type
      // CausedDamage = RealWeaponDamage * (1.0 - CurrentProFactor)
      float _fCausedDamage = ((Weapon)a_pTrigger).DamagePoints * (1.0f - CurrentProPFactor);

#if !RETAIL
      // DefaultGameLogger.Instance.dumpLogInfo("[Character][doStruckCom][" + gameObject.ToString() + "]: [Hit] Weapon Damage: " + ((Weapon)a_pTrigger).DamagePoints + " Caused Damange: " + _fCausedDamage + " Monster HP: " + m_fCurHP + " --> " + (m_fCurHP - _fCausedDamage));
#endif

      m_fCurHP -= _fCausedDamage;

      // show HP bar
      doShowHPBarCom();

      // check HP
      // NO HP anymore
      if(m_fCurHP < 1.0e-5f)
      {
        doDeadCom();
      }
      else
      {
        // push state
        pushState();

        // if the character is currently moving: stop movement animation
        if(EntityStates.ENTITY_STATES_WALKING == m_eCurEntityState
          ||
          EntityStates.ENTITY_STATES_RUNNING == m_eCurEntityState
        )
        {
          stopMoveAnimation(); 
        }

        // play the struck animation
        playAnimationTrigger(StruckAnimations);

        // store old state
        m_ePrevEntityState = m_eCurEntityState;

        // change to struck state
        m_eCurEntityState = EntityStates.ENTITY_STATES_STRUCK;
      }
    }

    /**
     * Dead: destroy this entity
     */
    public override void doDeadCom()
    {
      // already in dead state: return
      if(EntityStates.ENTITY_STATES_DEAD == m_eCurEntityState)
      {
        return;
      }

      base.doDeadCom();

      // stop using any weapon
      onStopUseWeapon(-1);

      // play the dead animation
      playAnimationTrigger(DeadAnimations);
      
      // store old state
      m_ePrevEntityState = m_eCurEntityState;
      
      // change to dead state
      m_eCurEntityState = EntityStates.ENTITY_STATES_DEAD;
    }

    /**
     * destroy this entity directly without doing any sort of animations: dead animation and unspawn animation
     */
    public override void doDestroy()
    {
      // already in dead state: return
      if(EntityStates.ENTITY_STATES_DEAD == m_eCurEntityState)
      {
        return;
      }

      base.doDestroy();

      // stop using any weapon
      onStopUseWeapon(-1);

      onAnimationDeadEnded();

      onAnimationUnSpawnEnded();
    }

    /**
     * Last Command: continue on the last command
     */
    public virtual void doLastCom()
    {
      // pup up stored former state
      popupState();
    }

    /**
     * show HP bar
     */
    public virtual void doShowHPBarCom()
    {
      
    }

    /**
     * hide HP bar
     */
    public virtual void doHideHPBarCom()
    {
      
    }
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
      // set the right entity type
      EntityType =  EntityTypes.ENTITY_TYPES_CHARACTER;
    }

    // initialize this entity: called within doSpawnCom() only
    // m_bIsInitialized will be SET in this method
    protected override void doInitialize()
    {
      base.doInitialize();

      // clear initialization flag before checking
      m_bIsInitialized = false;

      // reset others
      m_ePrevEntityState = EntityStates.ENTITY_STATES_UNKNOWN;
      m_iCurLevel = Level ;
      m_fCurHP = MaxHealth;
      m_fCurMP = MaxMagic;
      m_fCurRP = MaxRewards;
      m_fCurProP = MaxProtection;

      // last animation data related
      m_pLastAttackAnimationData = null;
      m_pLastMoveAnimationData = null;
      m_pLastDefendAnimationData = null;
      m_pLastRageAttackAnimationData = null;

      // move related
      m_eCurMoveType = MoveTypes.MOVE_TYPES_NO_MOVE;

      // sanity checking
      // check animator
      // each character MUST have an animator component
      if(null == m_pAnimatorComp)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][doInitialize][" + gameObject.ToString() + "]: No Animator Component");

        return;
      }

      // check name
      if(null == Name 
        ||
        0 == Name.Length
      )
      {
        generateRandomName(ref Name);
      }

      // check character type
      if(CharacterTypes.CHARACTER_TYPES_UNKNOWN == CharacterType)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][doInitialize][" + gameObject.ToString() + "]: No Character Type");

        return;
      }

      // check level
      if(Level <= 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][doInitialize][" + gameObject.ToString() + "]: No Level");

        return;
      }

      // check level up scale factor
      if(LevelUpScale <= 0.0f)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][doInitialize][" + gameObject.ToString() + "]: No Level UP Scale Factor");

        return;
      }

      // check health points
      if(MaxHealth <= 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][doInitialize][" + gameObject.ToString() + "]: No Maximal Health Points");

        return;
      }

      // check maximal magic points(Mana)
      if(MaxMagic < 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][doInitialize][" + gameObject.ToString() + "]: No Maximal Magic Points");

        return;
      }

      // check maximal rewards points
      if(MaxRewards < 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][doInitialize][" + gameObject.ToString() + "]: No Maximal Rewards Points");

        return;
      }

      // check maximal protection points
      if(MaxProtection < 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][doInitialize][" + gameObject.ToString() + "]: No Maximal Protection Points");

        return;
      }

      // check walk speed: >= 0
      if(WalkSpeed < 0.0f)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][doInitialize][" + gameObject.ToString() + "]: No Move Speed");

        return;
      }

      // check turn speed: > 0
      if(TurnSpeed <= 0.0f)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][doInitialize][" + gameObject.ToString() + "]: No Turn Speed");

        return;
      }

      // check run speed: >= 0
      if(RunSpeed < 0.0f)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][doInitialize][" + gameObject.ToString() + "]: No Run Speed");

        return;
      }

      // try to find mounted brain
      m_pMountedBrain = gameObject.GetComponent< Brain >();

      // found the mounted brain, initialize it
      if(m_pMountedBrain != null)
      {
        m_pMountedBrain.doInitialize(this);
      }

      // set initialization flag
      m_bIsInitialized = true;
    }
     
    /**
     * doUpdate() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last doUpdate() call
     * check false == m_bIsInitialized before doing the real thing
     */
    protected override void doUpdate()
    {
      // if NOT initialized: return
      if(false == m_bIsInitialized)
      {
        return;
      }

      switch(m_eCurEntityState)
      {
      case EntityStates.ENTITY_STATES_WALKING:
      case EntityStates.ENTITY_STATES_RUNNING:
        {
          // ---- walking and running ----
          // switch on move modes
          switch(m_eCurMoveType)
          {
            case MoveTypes.MOVE_TYPES_NO_MOVE:
            {
              // no move mode: return
              return;
            }

            case MoveTypes.MOVE_TYPES_ROT_TO_HEADINGDIR:
            {
              // check how close the current heading direction is to the expected one
              float _fCosAngle = Vector3.Dot(m_vInitialExpectedHeadingDirWs, transform.forward /* current heading */);

              // the radians we can trun in this frame
              float _fTurnedRadians = TurnSpeed * 0.017453292519f /* 3.1415926f/180.0f */ * Time.deltaTime; 

              // if we can turn more degrees than the current angle between current heading direction and the expected one
              if(_fCosAngle > (float)Math.Cos(_fTurnedRadians))
              {
                // change forward direction
                transform.forward = m_vInitialExpectedHeadingDirWs;

                // move along the current heading direction until reaching target position
                m_eCurMoveType = MoveTypes.MOVE_TYPES_MOVE_ALONG_FORWARDDIR_TO_TARGET;
              }
              else
              {
                // still to much for turning
                // turn it further to the expected heading direction
                gameObject.transform.forward = Vector3.RotateTowards(gameObject.transform.forward, m_vInitialExpectedHeadingDirWs, _fTurnedRadians, 1.0f);
              }
              
              break;
            }

            case MoveTypes.MOVE_TYPES_FIND_HEADINGDIR:
            case MoveTypes.MOVE_TYPES_MOVE_ALONG_FORWARDDIR_FOREVER:
            case MoveTypes.MOVE_TYPES_MOVE_ALONG_FORWARDDIR_TO_TARGET:
            {
              // finding heading direciton or moving along forward direciton : calculate movement

              // move speed used in this movement
              float _fMoveSpeed = 0.0f;
              if(EntityStates.ENTITY_STATES_WALKING == m_eCurEntityState)
              {
                // walk speed
                _fMoveSpeed = WalkSpeed;
              }
              else
              {
                // run speed
                _fMoveSpeed = RunSpeed;
              }

              _fMoveSpeed *= m_fMoveDirFactor;
              
              // move this character along current forward direction
              // move distance from previous frame
              float _fMovedDistance = Time.deltaTime * _fMoveSpeed;

              // make the movement vector along forward direction
              Vector3 _vCurPosWs = gameObject.transform.forward * _fMovedDistance;

              // accumulate the current movement
              gameObject.transform.position += _vCurPosWs;

              // on move forever mode: return
              if(MoveTypes.MOVE_TYPES_MOVE_ALONG_FORWARDDIR_FOREVER == m_eCurMoveType)
              {
                return;
              }

              // normal move along mode: check if this character has reached the target position
              // get current expected heading direction in world space: CurPos --> TargetPos
              Vector3 _vCurExpectedHeadingDirWs = m_vMoveTargetPosWs - gameObject.transform.position;

              // the character is close enough to the target position
              if(_vCurExpectedHeadingDirWs.sqrMagnitude < 1.0e-5f)
              {
#if !RETAIL
                // DefaultGameLogger.Instance.dumpLogInfo("[Character][" + gameObject.ToString() + "]: Target Reached");
#endif

                // put it to the real target position
                gameObject.transform.position = m_vMoveTargetPosWs;

                // start to rotate to the target orientation
                m_eCurMoveType = MoveTypes.MOVE_TYPES_ROT_TO_TARGETDIR;

                return;
              }

              // overshoot handling: check if the current position is moved over the target position: 
              // - check which side the current position is on relative to the target position along the initial expected heading direction
              // get the dot product between current expected heading direction and initial expected heading direction
              float _fCurExHDDotInitExHD = Vector3.Dot(m_vInitialExpectedHeadingDirWs, _vCurExpectedHeadingDirWs);

              // moved too far away: current position is on the right side of target position: negative dot product
              if(_fCurExHDDotInitExHD < 0.0f)
              {
#if !RETAIL
                DefaultGameLogger.Instance.dumpLogInfo("[Character][" + gameObject.ToString() + "]: Moved Too Far");
#endif

                // move to target position
                gameObject.transform.position = m_vMoveTargetPosWs;

                // start to rotate to the target orientation
                m_eCurMoveType = MoveTypes.MOVE_TYPES_ROT_TO_TARGETDIR;

                return;
              }

              // MOVE_TYPES_FIND_HEADINGDIR mode: find a heading direction
              if(MoveTypes.MOVE_TYPES_FIND_HEADINGDIR == m_eCurMoveType)
              {
                // normalize the current heading direction
                _vCurExpectedHeadingDirWs.Normalize();

                // check how close the current heading direction is to the expected one
                float _fCosAngle = Vector3.Dot(_vCurExpectedHeadingDirWs, transform.forward /* current heading */);

                // the radians we can trun in this frame
                float _fTurnedRadians = TurnSpeed * 0.017453292519f /* 3.1415926f/180.0f */ * Time.deltaTime; 

                  // if we can turn more degrees than the current angle between current heading direction and the expected one
                  if(_fCosAngle > (float)Math.Cos(_fTurnedRadians))
                  {
#if !RETAIL
                    // DefaultGameLogger.Instance.dumpLogInfo("[Character][" + gameObject.ToString() + "]: Found Heading Direction");
#endif

                    // change forward direction
                    transform.forward = _vCurExpectedHeadingDirWs;

                    // move along the current heading direction until reaching target position
                    m_eCurMoveType = MoveTypes.MOVE_TYPES_MOVE_ALONG_FORWARDDIR_TO_TARGET;
                  }
                  else
                  {
                    // still to much for turning
                    // turn it further to the expected heading direction
                    gameObject.transform.forward = Vector3.RotateTowards(gameObject.transform.forward, _vCurExpectedHeadingDirWs, _fTurnedRadians, 1.0f);
                  }
                }
                break;
              }

              case MoveTypes.MOVE_TYPES_ROT_TO_TARGETDIR:
              {
                // check if the current heading direction is close to the target's orientation
                float _fCosAngle = Vector3.Dot(m_vMoveTargetOrienWs, transform.forward);

                // turn the character's heading direction in 5 times turn speed
                float _fTurnedRadians = 5.0f * TurnSpeed * 0.017453292519f /* 3.1415926f/180.0f */ * Time.deltaTime; 

                // if we can trun more degrees thanthe current angle between current heading direction and the expected one
                if(_fCosAngle > (float)Math.Cos(_fTurnedRadians))
                {
#if !RETAIL
                  // DefaultGameLogger.Instance.dumpLogInfo("[Character][" + gameObject.ToString() + "]: Rotated to the Target Orientation");
#endif

                  // change forward direction
                  transform.forward = m_vMoveTargetOrienWs;

                  // stop the movement animation
                  stopMoveAnimation();
                
                  // change to no move mode
                  m_eCurMoveType = MoveTypes.MOVE_TYPES_NO_MOVE;

                  if(null != m_pMountedBrain)
                  {
                    // let brain handle this event
                    m_pMountedBrain.onTargetPosReached();
                  }
                  else
                  {
                    // for NO brain case
                    doIdleCom();
                  }
                }
                else
                {
                  // still to much for turning
                  // turn it further to expected heading direction
                  gameObject.transform.forward = Vector3.RotateTowards(gameObject.transform.forward, m_vMoveTargetOrienWs, _fTurnedRadians, 1.0f);
                }

                break;
              }

              default:
              {
                DefaultGameLogger.Instance.dumpLogError("[Character][" + gameObject.ToString() + "]: Unhandled Move Type: " + m_eCurMoveType.ToString());

                break;
              }
            }

          return;
        } //  case EntityStates.ENTITY_STATES_WALKING:
      }
    }
    
    /**
     * called from each ONE time animation, after it has been played back
     * a_iAnimationNameHash is the animation name hash ID: like:
     * - Spawn
     * - Jump
     * - Leap: High Jump
     * - Attack
     * - Melee
     * - Hit
     * - Struck: Powerful Hit
     * - Dead 
     * 
     * return true if handled
     */
    protected override bool doAnimationEndedHandle(int a_iAnimationNameHash)
    {
      // branching on different animations
      if(ANIMATION_JUMP == a_iAnimationNameHash)
      {
        onAnimationJumpEnded();
        
        return true;
      }
      
      else if(ANIMATION_LEAP == a_iAnimationNameHash)
      {
        onAnimationLeapEnded();
        
        return true;
      }
      
      else if(ANIMATION_ATTACK == a_iAnimationNameHash)
      {
        onAnimationAttackEnded();
        
        return true;
      }
      
      else if(true == base.doAnimationEndedHandle(a_iAnimationNameHash))
      {
        return true;
      }

      else
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][doAnimationEndedHandle][" + gameObject.ToString() + "]: UNKNOWN ANIMATION PLAYED. ID: " + a_iAnimationNameHash);

        return false;
      }
    }

    // Callbacks of Animation Playbacks
    // called when Spawn animation ended
    protected override void onAnimationSpawnEnded()
    {
      // if the character has changed to other state: return
      if(EntityStates.ENTITY_STATES_SPAWNING != m_eCurEntityState)
      {
        return;
      }

      // otherwise, tell brain to react
      if(null != m_pMountedBrain)
      {
        m_pMountedBrain.onSpawningEnded();
      }
      else
      {
        // for NO brain case
        doIdleCom();
      }
    }

    // called when UnSpawn animation ended
    protected override void onAnimationUnSpawnEnded()
    {
      // if the character has changed to other state: return
      if(EntityStates.ENTITY_STATES_UNSPAWNING != m_eCurEntityState)
      {
        return;
      }

      // otherwise, tell brain to react
      if(null != m_pMountedBrain)
      {
        // will destroy this character
        m_pMountedBrain.onUnSpawningEnded();

        // destroy the mounted brain
        m_pMountedBrain.doDestroy();
      }
      else
      {
        // for NO brain case
        DefaultGameLogger.Instance.dumpLogError("[Character][onAnimationUnSpawnEnded][" + gameObject.ToString() + "]: UNKNOWN REACTION");
      }

      // clear initialization flag
      m_bIsInitialized = false;
    }

    // called when Common Hit/GotDamaged animation ended: Hit/GotDamaged Animation will go automatically to Idle state when done
    protected override void onAnimationHitEnded()
    {
      // if the character has changed to other state: return
      if(EntityStates.ENTITY_STATES_HIT != m_eCurEntityState)
      {
        return;
      }

      if(null != m_pMountedBrain)
      {
        m_pMountedBrain.onHitEnded();
      }
      else
      {
        // for NO brain case
        doIdleCom();
      }
    }
    
    // called when Strong Hit/GotDamaged animation ended
    protected override void onAnimationStruckEnded()
    {
      // if the character has changed to other state: return
      if(EntityStates.ENTITY_STATES_STRUCK != m_eCurEntityState)
      {
        return;
      }

      if(null != m_pMountedBrain)
      {
        m_pMountedBrain.onStruckEnded();
      }
      else
      {
        // for NO brain case
        doIdleCom();
      }
    }
    
    // called when Dead animation ended
    protected override void onAnimationDeadEnded()
    {
      if(null != m_pMountedBrain)
      {
        m_pMountedBrain.onDeadEnded();
      }
    }

    // called when Jump animation ended
    protected virtual void onAnimationJumpEnded()
    {
      // if the character has changed to other state: return
      if(EntityStates.ENTITY_STATES_JUMPING != m_eCurEntityState)
      {
        return;
      }

      if(null != m_pMountedBrain)
      {
        m_pMountedBrain.onJumpEnded();
      }
      else
      {
        // for NO brain case
        doIdleCom();
      }
    }
    
    // called when Double Jump animation ended
    protected virtual void onAnimationLeapEnded()
    {
      // if the character has changed to other state: return
      if(EntityStates.ENTITY_STATES_LEAPING != m_eCurEntityState)
      {
        return;
      }

      if(null != m_pMountedBrain)
      {
        m_pMountedBrain.onLeapEnded();
      }
      else
      {
        // for NO brain case
        doIdleCom();
      }
    }
    
    // called when Attack(Normal and Melee) animation ended: when onStopUseWeapon event has NOT been fired, stop using weapon
    protected virtual void onAnimationAttackEnded()
    {
      // if the character has changed to other state: return
      if(EntityStates.ENTITY_STATES_ATTACKING != m_eCurEntityState)
      {
        return;
      }

      // if there is some equpped weapon, stop use any of them
      if(null != EquippedWeaponList
         &&
         EquippedWeaponList.Length > 0
        )
      {
        for(int _iWeaponIdx = 0; _iWeaponIdx < EquippedWeaponList.Length; _iWeaponIdx++)
        {
          EquippedWeaponList[_iWeaponIdx].doStopUseCom();              
        }        
      }

      if(null != m_pMountedBrain)
      {
        m_pMountedBrain.onAttackEnded();
      }
      else
      {
        // for NO brain case
        doIdleCom();
      }
    }
    
    /**
     * doLevelUp() will be called after ONE Level UP
     */
    // used to upgrade HP, MP, RP, ProP,...
    protected virtual void doLevelUp()
    {
      m_iCurLevel++;
      m_fCurHP   += m_fCurHP   * (m_iCurLevel - 1) * LevelUpScale;
      m_fCurMP   += m_fCurMP   * (m_iCurLevel - 1) * LevelUpScale;
      m_fCurRP   += m_fCurRP   * (m_iCurLevel - 1) * LevelUpScale;
      m_fCurProP += m_fCurProP * (m_iCurLevel - 1) * LevelUpScale;
    }

    // select one move animation from the given move animation list
    protected virtual void playMoveAnimation(AnimationData[] a_pMoveAnimationList)
    {
      if(null == m_pAnimatorComp            // NO animator at all
         ||                                 // or
         0 == a_pMoveAnimationList.Length   // NOTHING to play
        )
      {
        return;
      }

      // randomly choose an animation
      AnimationData _pSelectedMoveAnimation = getArrayRandomElement(a_pMoveAnimationList);

      // play the selected move animation
      playAnimationFlag(_pSelectedMoveAnimation);

      // store the selected move animation data
      m_pLastMoveAnimationData = _pSelectedMoveAnimation;
    }

    // select one defend animation from the given defend animation list
    protected virtual void playDefendAnimation(AnimationData[] a_pDefendAnimationList)
    {
      if(null == m_pAnimatorComp            // NO animator at all
         ||                                 // or
         0 == a_pDefendAnimationList.Length // NOTHING to play
      )
      {
        return;
      }

      // randomly choose an animation
      AnimationData _pSelectedDefendAnimation = getArrayRandomElement(a_pDefendAnimationList);

      // play the selected defend animation
      playAnimationFlag(_pSelectedDefendAnimation);

      // store the selected defende animation data
      m_pLastDefendAnimationData = _pSelectedDefendAnimation;
    }

    // called after doSpawnCom() has done
    protected override void onSpawnComDone()
    {
      base.onSpawnComDone();

      // if there is an equipped weapon
      if(EquippedWeaponList != null
         &&
         EquippedWeaponList.Length > 0
        )
      {
        for(int _iWeaponIdx = 0; _iWeaponIdx < EquippedWeaponList.Length; _iWeaponIdx++)
        {
          // if the current equipped weapon is a shoot weapon
          if(WeaponTypes.WEAPON_TYPES_SHOOTWEAPON == EquippedWeaponList[_iWeaponIdx].WeaponType)
          {
            // if there is some projectile needs to be equipped
            if(((ShootWeapon)EquippedWeaponList[_iWeaponIdx]).ProjectileID >= 0)
            {
              // equip the projectile
              List< int > _aProjectileList = new List<int>(1);
              _aProjectileList.Add(((ShootWeapon)EquippedWeaponList[_iWeaponIdx]).ProjectileID);

              ProjectileMgr.Instance.equipProjectiles(_aProjectileList);
            }
          }

          // spawn the current equipped weapon
          EquippedWeaponList[_iWeaponIdx].doSpawnCom();
        }
      }
    }
    #endregion // PROTECTED OVERRIDDENS

    // generate a random name string
    protected void generateRandomName(ref string a_szGeneratedName)
    {
      // create a new string object and assign it to the given parameter
      a_szGeneratedName = "__RandomName__";
    }

    // get current damage reduction factor
    protected float getCurrentProPFactor()
    {
      // affected by the current health situation
      float _fEffectiveProP = m_fCurProP * ((m_fCurHP) / (float)MaxHealth);

      // return a fractional number
      return _fEffectiveProP / (_fEffectiveProP + MAX_PROTECTION_POINTS);
    }

      /**
       * called via Animation Event when: start using a Melee Weapon or shooting a Shoot Weapon with the given weapon index
       */
    protected void onUseWeapon(int a_iWeaponIndex)
    {
      // if there is NO equipped weapon
      if(null == EquippedWeaponList)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][onUseWeapon][" + gameObject.ToString() + "]: No Equipped Weapon at all, But Animimation fired use weapon event with weapon index: " + a_iWeaponIndex);

        return;
      }

      // given index is out of bound
      if(EquippedWeaponList.Length <= a_iWeaponIndex)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][onUseWeapon][" + gameObject.ToString() + "]: Given Weapon Index is TOO large: " + a_iWeaponIndex);

        return;
      }

      // use the given weapon
      EquippedWeaponList[a_iWeaponIndex].doUseCom();
    }

    /**
     * called via Animation Event when: stop using a Melee Weapon or shooting a Shoot Weapon with the given weapon index
     * NOTE: a_iWeaponIndex == -1 : stop all weapons
     */
    protected void onStopUseWeapon(int a_iWeaponIndex)
    {
      // if there is NO equipped weapon
      if(null == EquippedWeaponList)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][onUseWeapon][" + gameObject.ToString() + "]: No Equipped Weapon at all, But Animimation fired use weapon event with weapon index: " + a_iWeaponIndex);

        return;
      }

      // given index is out of bound
      if(EquippedWeaponList.Length <= a_iWeaponIndex)
      {
        DefaultGameLogger.Instance.dumpLogError("[Character][onUseWeapon][" + gameObject.ToString() + "]: Given Weapon Index is TOO large: " + a_iWeaponIndex);

        return;
      }

      // stop the given weapon
      if(a_iWeaponIndex >= 0)
      {
        EquippedWeaponList[a_iWeaponIndex].doStopUseCom();
      }
      else
      {
        // stop all weapons
        for(int _iWeaponIdx = 0; _iWeaponIdx < EquippedWeaponList.Length; _iWeaponIdx++)
        {
          EquippedWeaponList[_iWeaponIdx].doStopUseCom();
        }
      }
    }

    // push down current state
    protected  void pushState()
    {
      switch(m_eCurEntityState)
      {
      case EntityStates.ENTITY_STATES_IDLE:
        {
          m_pIdleState.pushState(this);

          break;
        }

      case EntityStates.ENTITY_STATES_WALKING:
        {
          m_pWalkState.pushState(this);

          break;
        }

      case EntityStates.ENTITY_STATES_RUNNING:
        {
          m_pRunState.pushState(this);

          break;
        }

      case EntityStates.ENTITY_STATES_ATTACKING:
        {
          m_pAttackState.pushState(this);

          break;
        }

      case EntityStates.ENTITY_STATES_SHOOTING:
        {
          m_pShootState.pushState(this);

          break;
        }

      default:
        {
          break;
        }
      }
    }

    // pop up currently stored state
    protected void popupState()
    {
      // pop up state
      switch(m_ePrevEntityState)
      {
      case EntityStates.ENTITY_STATES_IDLE:
        {
          m_pIdleState.popState(this);

          break;
        }

      case EntityStates.ENTITY_STATES_WALKING:
        {
          m_pWalkState.popState(this);

          break;
        }

      case EntityStates.ENTITY_STATES_RUNNING:
        {
          m_pRunState.popState(this);

          break;
        }

      case EntityStates.ENTITY_STATES_ATTACKING:
        {
          m_pAttackState.popState(this);

          break;
        }

      case EntityStates.ENTITY_STATES_SHOOTING:
        {
          m_pShootState.popState(this);

          break;
        }

      default:
        {
          break;
        }
      }
    }

    // stop the current move animation
    protected void stopMoveAnimation()
    {
      if(null == m_pLastMoveAnimationData)
      {
        return;
      }

      stopLoopAnimation(m_pLastMoveAnimationData);

      m_pLastMoveAnimationData = null;

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[Character][stopMoveAnimation]: Character's Movement Stopped");
#endif
    }

    // stop the current defned animation
    protected void stopDefendAnimation()
    {
      if(null == m_pLastDefendAnimationData)
      {
        return;
      }

      stopLoopAnimation(m_pLastDefendAnimationData);

      m_pLastDefendAnimationData = null;
    }

    // stop the current rage attack animation
    protected void stopRageAttackAnimation()
    {
      if(null == m_pLastRageAttackAnimationData)
      {
        return;
      }

      stopLoopAnimation(m_pLastRageAttackAnimationData);

      m_pLastRageAttackAnimationData = null;
    }
    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    protected class IdleState
    {
      public void pushState(Character a_pTheCharacter)
      {

      }

      public void popState(Character a_pTheCharacter)
      {
        a_pTheCharacter.doIdleCom();
      }
    }

    protected class WalkState
    {
      public void pushState(Character a_pTheCharacter)
      {
        m_ePushedMoveType = a_pTheCharacter.m_eCurMoveType;
        m_vPushedMoveTargetPosWs = a_pTheCharacter.m_vMoveTargetPosWs;
        m_vPushedMoveTargetOrienWs = a_pTheCharacter.m_vMoveTargetOrienWs;
        m_vPushedInitialExpectedHeadingDirWs = a_pTheCharacter.m_vInitialExpectedHeadingDirWs;
      }

      public void popState(Character a_pTheCharacter)
      {
        a_pTheCharacter.m_eCurMoveType = m_ePushedMoveType;
        a_pTheCharacter.m_vMoveTargetPosWs = m_vPushedMoveTargetPosWs;
        a_pTheCharacter.m_vMoveTargetOrienWs = m_vPushedMoveTargetOrienWs;
        a_pTheCharacter.m_vInitialExpectedHeadingDirWs = m_vPushedInitialExpectedHeadingDirWs;

        // cache old state
        a_pTheCharacter.m_ePrevEntityState = a_pTheCharacter.m_eCurEntityState;

        // change to the walk state
        a_pTheCharacter.m_eCurEntityState = EntityStates.ENTITY_STATES_WALKING;

        a_pTheCharacter.playMoveAnimation(a_pTheCharacter.WalkAnimations);
      }

      // move type
      MoveTypes m_ePushedMoveType;
      // move target position in world space
      Vector3 m_vPushedMoveTargetPosWs;
      // target location's orientation
      Vector3 m_vPushedMoveTargetOrienWs;
      // initial expected heading direction
      Vector3 m_vPushedInitialExpectedHeadingDirWs;
    }

    protected class RunState
    {
      public void pushState(Character a_pTheCharacter)
      {
        m_ePushedMoveType = a_pTheCharacter.m_eCurMoveType;
        m_vPushedMoveTargetPosWs = a_pTheCharacter.m_vMoveTargetPosWs;
        m_vPushedMoveTargetOrienWs = a_pTheCharacter.m_vMoveTargetOrienWs;
        m_vPushedInitialExpectedHeadingDirWs = a_pTheCharacter.m_vInitialExpectedHeadingDirWs;
      }

      public void popState(Character a_pTheCharacter)
      {
        a_pTheCharacter.m_eCurMoveType = m_ePushedMoveType;
        a_pTheCharacter.m_vMoveTargetPosWs = m_vPushedMoveTargetPosWs;
        a_pTheCharacter.m_vMoveTargetOrienWs = m_vPushedMoveTargetOrienWs;
        a_pTheCharacter.m_vInitialExpectedHeadingDirWs = m_vPushedInitialExpectedHeadingDirWs;

        // cache old state
        a_pTheCharacter.m_ePrevEntityState = a_pTheCharacter.m_eCurEntityState;

        // change to the run state
        a_pTheCharacter.m_eCurEntityState = EntityStates.ENTITY_STATES_RUNNING;

        a_pTheCharacter.playMoveAnimation(a_pTheCharacter.RunAnimations);
      }

      // move type
      MoveTypes m_ePushedMoveType;
      // move target position in world space
      Vector3 m_vPushedMoveTargetPosWs;
      // target location's orientation
      Vector3 m_vPushedMoveTargetOrienWs;
      // initial expected heading direction
      Vector3 m_vPushedInitialExpectedHeadingDirWs;
    }

    protected class AttackState
    {
      public void pushState(Character a_pTheCharacter)
      {
        m_pPushedAttackAnimation = a_pTheCharacter.m_pLastAttackAnimationData;
      }

      public void popState(Character a_pTheCharacter)
      {
        a_pTheCharacter.doAttackCom(m_pPushedAttackAnimation);
      }

      AnimationData m_pPushedAttackAnimation;
    }

    protected class ShootState
    {
      public void pushState(Character a_pTheCharacter)
      {

      }

      public void popState(Character a_pTheCharacter)
      {
        a_pTheCharacter.doShootCom();
      }
    }

    protected static float MAX_PROTECTION_POINTS = 250.0f;
    // more animation name hash IDs: JUST for ONE TIME animations NO looped animations
    /**
     *  Jump, Leap, Attack(Normal and Melee Atacks)
     */
    protected static int ANIMATION_JUMP         = Animator.StringToHash("Jump");
    protected static int ANIMATION_LEAP         = Animator.StringToHash("Leap");
    protected static int ANIMATION_ATTACK       = Animator.StringToHash("Attack");

    // previous state: used to cache the old entity state
    protected EntityStates m_ePrevEntityState;
    // current level
    protected int m_iCurLevel;
    // XP: XP used for display: (int)(m_fCurXP + 0.5): round up
    protected float m_fCurXP;
    // HP: HP used for display: (int)(m_fCurHP + 0.5): round up
    protected float m_fCurHP;
    // MP: MP used for display: (int)(m_fCurMP + 0.5): round up
    protected float m_fCurMP;
    // RP: reward points: RP used for display: (int)(m_fCurRP + 0.5): round up
    protected float m_fCurRP;
    // ProP: protection points: ProP used for display: (int)(m_fCurProP + 0.5): round up
    protected float m_fCurProP;

    // move direction factor: positive move: 1 or negative move -1
    float m_fMoveDirFactor;
    // move type
    protected MoveTypes m_eCurMoveType;
    // move target position in world space
    protected Vector3 m_vMoveTargetPosWs;
    // target location's orientation
    protected Vector3 m_vMoveTargetOrienWs;
    // initial expected heading direction
    protected Vector3 m_vInitialExpectedHeadingDirWs;

    // weapon script
    protected Component m_pMountedWeapon;

    // brain script
    protected Brain m_pMountedBrain;

    // state cache
    // idle state
    protected IdleState m_pIdleState;
    // walk state
    protected WalkState m_pWalkState;
    // run state
    protected RunState m_pRunState;
    // attack state
    protected AttackState m_pAttackState;
    // shoot state
    protected ShootState m_pShootState;

    // the latest played attack animation data
    protected AttackAnimationData m_pLastAttackAnimationData;

    // LASTING/LOOP ANIMATION DATA
    // latest move animation data
    protected AnimationData m_pLastMoveAnimationData;
    // latest defend animation data
    protected AnimationData m_pLastDefendAnimationData;
    // latest rage animation data
    protected AnimationData m_pLastRageAttackAnimationData;
    #endregion // PROTECTED DATA MEMBERS

    #region PRIVATE METHODS
    #endregion // PRIVATE METHODS
  }
}

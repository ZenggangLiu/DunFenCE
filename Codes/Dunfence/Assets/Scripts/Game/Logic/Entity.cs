/**
 * Base class of all Functional Items in a Level: JUST Character can have BRAIN
 * - Building: Barrack, Black Smith
 * - Decoration: Tree, Grass, Rubbish Bin
 * - Loot: Coin Pack, Health Pack, Magic Pack, Reward Pack
 * - Weapon: Sword, Gun
 * - Character with Brain: Monster, Evil, Bummer, Toady, Hero
 * - Changed: Use UnityEngine.Random instead of System.Random
 *   + UnityEngine.Random is a SingleTon
 *   + System.Random has to be initialized with different seed, otherwise multiple instances will have SAME value distribution
 */

using System;
using UnityEngine;
using Framework.Utils;
using Framework.Mathe;
using Framework.Logger;

namespace Game.Logic
{
  // entity types
  public enum EntityTypes
  {
    ENTITY_TYPES_UNKNOWN = 0,
    ENTITY_TYPES_LOOT,
    ENTITY_TYPES_WEAPON,
    ENTITY_TYPES_CHARACTER,
    ENTITY_TYPES_DECOR,
    ENTITY_TYPES_BUILDING,
    ENTITY_TYPES_GROUND,
  }

  // entity states: combined states for loot, building, decor, weapon, character
  public enum EntityStates
  {
    ENTITY_STATES_UNKNOWN                     = 0,
    ENTITY_STATES_INITIALIZING                = 1 << 0,
    ENTITY_STATES_SPAWNING                    = 1 << 1,   // once animation
    ENTITY_STATES_IDLE                        = 1 << 2,   // loop animation
    ENTITY_STATES_WALKING                     = 1 << 3,   // loop animation
    ENTITY_STATES_RUNNING                     = 1 << 4,   // loop animation
    ENTITY_STATES_JUMPING                     = 1 << 5,   // once animation
    ENTITY_STATES_LEAPING                     = 1 << 6,   // once animation: high jump
    ENTITY_STATES_ATTACKING                   = 1 << 7,   // once animation
    ENTITY_STATES_MELEE                       = 1 << 8,   // once animation: strong attacking
    ENTITY_STATES_RELOADING                   = 1 << 9,   // once animation
    ENTITY_STATES_DEFENDING                   = 1 << 10,  // loop animation
    ENTITY_STATES_SHOOTING                    = 1 << 11,  // loop animation
    ENTITY_STATES_HIT                         = 1 << 12,  // once animation
    ENTITY_STATES_STRUCK                      = 1 << 13,  // once animation: strong hit
    ENTITY_STATES_DEAD                        = 1 << 14,  // once animation
    ENTITY_STATES_DESTROYING                  = 1 << 15,
    ENTITY_STATES_UNSPAWNING                  = 1 << 16,  // once animation
    ENTITY_STATES_RAGE_ATTACKING              = 1 << 17,  // loop animation

    ENTITY_STATES_ANY_JUMP                    = ENTITY_STATES_JUMPING      |
                                                ENTITY_STATES_LEAPING,

    ENTITY_STATES_ANY_ATTACKING               = ENTITY_STATES_ATTACKING    |
                                                ENTITY_STATES_MELEE        |
                                                ENTITY_STATES_SHOOTING     |
                                                ENTITY_STATES_RAGE_ATTACKING,

    ENTITY_STATES_ANY_HIT                     = ENTITY_STATES_HIT          |
                                                ENTITY_STATES_STRUCK,

    ENTITY_STATES_BUSY                        = ENTITY_STATES_INITIALIZING |
                                                ENTITY_STATES_SPAWNING     |
                                                ENTITY_STATES_IDLE         |
                                                ENTITY_STATES_WALKING      |
                                                ENTITY_STATES_RUNNING      |
                                                ENTITY_STATES_JUMPING      |
                                                ENTITY_STATES_LEAPING      |
                                                ENTITY_STATES_ATTACKING    |
                                                ENTITY_STATES_MELEE        |
                                                ENTITY_STATES_RELOADING    |
                                                ENTITY_STATES_DEFENDING    |
                                                ENTITY_STATES_SHOOTING     |
                                                ENTITY_STATES_HIT          |
                                                ENTITY_STATES_STRUCK       |
                                                ENTITY_STATES_DEAD         |
                                                ENTITY_STATES_DESTROYING   |
                                                ENTITY_STATES_UNSPAWNING   |
                                                ENTITY_STATES_RAGE_ATTACKING,
  }

  // animation data: trigger name and so on
  [Serializable]
  public class AnimationData
  {
    public AnimationData(string a_szTrigger)
    {
      Trigger = a_szTrigger;
    }

    // animation trigger name
    public string Trigger;
  }

  public class Entity : MonoBehaviour
  {
    #region PUBLIC DATA MEMBERS
    // entity type
    public EntityTypes EntityType = EntityTypes.ENTITY_TYPES_UNKNOWN;

    // common animation trigger name list among all sort of Entity
    // if there are more than ONE animation trigger names for one type of animation, resize the corresponding array
    // spawn
    public AnimationData[] SpawnAnimations = {new AnimationData("Spawn")};
    // hit: normal hit
    public AnimationData[] HitAnimations = {new AnimationData("Hit")};
    // struck: strong hit
    public AnimationData[] StruckAnimations = {new AnimationData("Struck")};
    // dead
    public AnimationData[] DeadAnimations = {new AnimationData("Dead")};
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    /**
     * return true is this entity can be considered as dead
     */
    public bool IsDead
    {
      get
      {
        return EntityStates.ENTITY_STATES_UNSPAWNING == m_eCurEntityState ||  EntityStates.ENTITY_STATES_DESTROYING == m_eCurEntityState || EntityStates.ENTITY_STATES_DEAD == m_eCurEntityState;
      }
    }

    /**
     * return true, if this entity can handle hit
     */
    public bool canHandleHit()
    {
      // if this entity is in initialize, spawn, jump, leap, reload, defend, destroy, dead, unspawn: return false
      switch(m_eCurEntityState)
      {
        case EntityStates.ENTITY_STATES_INITIALIZING:
        case EntityStates.ENTITY_STATES_SPAWNING:
        case EntityStates.ENTITY_STATES_JUMPING:
        case EntityStates.ENTITY_STATES_LEAPING:
        case EntityStates.ENTITY_STATES_RELOADING:
        case EntityStates.ENTITY_STATES_DEFENDING:
        case EntityStates.ENTITY_STATES_UNSPAWNING:
        case EntityStates.ENTITY_STATES_DESTROYING:
        case EntityStates.ENTITY_STATES_DEAD:
        case EntityStates.ENTITY_STATES_RAGE_ATTACKING:
        {
          return false;
        }

        default:
        {
          return true;
        }
      }
    }

    // get current entity state
    public EntityStates CurrentEntityState
    {
      get
      {
        return m_eCurEntityState;
      }
    }

    // if this entity is simulating spawn animation
    public bool IsSimulatingSpawn
    {
      get
      {
        return m_bIsSimulatingSpawn;
      }
    }

    #region PUBLIC OVERRIDDENS
    /**
     * Collsion Handler for any Entity
     * - Normal Entity(Except Weapon) will call weapon's onCollision() during its handling
     */
    public virtual void onCollision(Entity a_pTrigger,                   // who has triggered this Hit
                                    CollisionHandler a_pCollidedPart     // collided part
                                   )
    {

    }

    /**
     * Handle the given Collision within this Entity
     * called from Trigger in order to cause damage directly for instance
     */
    public virtual void handleCollision(Entity a_pTrigger,                   // who has triggered this Hit
                                        CollisionHandler a_pCollidedPart     // collided part
                                       )
    {
      
    }

    /**
     * ExitCollision Handler for any Entity
     */
    public virtual void onExitCollision(Entity a_pTrigger,                    // who had triggered this Hit
                                        CollisionHandler a_pFormerCollidedPart// former collided part
                                       )
    {

    }

    // animation related commands
    /**
     * Spawn at its current location with current orientation
     */
    public virtual void doSpawnCom()
    {
      // spawning: return
      if(EntityStates.ENTITY_STATES_SPAWNING == m_eCurEntityState)
      {
        return;
      }

      // initialize this entity
      doInitialize();

      // first spawn, store the initial local orientation of this entity
      if(false == m_bHasBeenSpawned)
      {
        // store the LOCAL orientation
        m_qInitLocalOrientation = transform.localRotation;

        m_bHasBeenSpawned = true;
      }

      onSpawnComDone();
    }

    /**
     * Spawn
     * a_vSpawnPosWs: the world space spawn position of this entity
     * a_vSpawnDirWs: the world space forward direction of this entity
     * a_vSpawnUpWs : the world space up direction of this entity
     */
    public virtual void doSpawnCom(Vector3 a_vSpawnPosWs,                 // spawn position in world space
                                   Vector3 a_vSpawnDirWs,                 // spawn heading direction in world space
                                   Vector3 a_vSpawnUpWs                   // spawn up direction in world space
                                  )
    {
      // spawning: return
      if(EntityStates.ENTITY_STATES_SPAWNING == m_eCurEntityState)
      {
        return;
      }

      // initialize this entity
      doInitialize();

      // first spawn, store the initial local orientation of this entity
      if(false == m_bHasBeenSpawned)
      {
        // store the LOCAL orientation
        m_qInitLocalOrientation = transform.localRotation;

        m_bHasBeenSpawned = true;
      }

      // store spawn orientation
      m_qSpawnOrientation = Quaternion.LookRotation(Vector3.Normalize(a_vSpawnDirWs), Vector3.Normalize(a_vSpawnUpWs)) * m_qInitLocalOrientation;

      // set to the given spawn position
      transform.position = a_vSpawnPosWs;

      // set orientation
      transform.rotation = m_qSpawnOrientation;

      onSpawnComDone();
    }

    /**
     * UnSpawn: last animation of whole entity's life
     */
    public virtual void doUnSpawnCom()
    {
      DefaultGameLogger.Instance.dumpLogWarning("[" + gameObject.ToString() + "]: [Entity][doUnSpawnCom]: NO IMPLEMENTATION");
    }

    /**
     * Hit: normal hit
     * a_pTrigger: the entity who has trigger this Hit
     * a_pCollidedPart: which part of the object has been hit
     */
    public virtual void doHitCom(Entity a_pTrigger,                       // trigger entity
                                 CollisionHandler a_pCollidedPart         // collided part
                                )
    {
      DefaultGameLogger.Instance.dumpLogWarning("[" + gameObject.ToString() + "]: [Entity][doHitCom]: NO IMPLEMENTATION");
    }
    
    /**
     * Struck: strong hit
     * a_pTrigger: the entity who has trigger this Hit
     * a_pCollidedPart: which part of the object has been hit
     */
    public virtual void doStruckCom(Entity a_pTrigger,                    // trigger entity
                                    CollisionHandler a_pCollidedPart      // collided part
                                   )
    {
      DefaultGameLogger.Instance.dumpLogWarning("[" + gameObject.ToString() + "]: [Entity][doStruckCom]: NO IMPLEMENTATION");
    }
    
    /**
     * Dead: destroy this entity
     */
    public virtual void doDeadCom()
    {
      // clear initialization flage
      m_bIsInitialized = false;
    }

    /**
     * destroy this entity directly without doing any sort of animations: dead animation and unspawn animation
     */
    public virtual void doDestroy()
    {
      // clear initialization flag
      m_bIsInitialized = false;
    }

    // return a randomly selected element from the given array
    public virtual AnimationData getArrayRandomElement(AnimationData[] a_aAnimationDataArray)
    {
      // if the given animation data array is empty: return null
      if(0 == a_aAnimationDataArray.Length)
      {
        return null;
      }

      // randomly select one element
      int _iSelectElementIndex = RandomNumberMgr.Instance.getIntegerVal(0, a_aAnimationDataArray.Length - 1); // [0, Length -1]

      // return the selected element
      return a_aAnimationDataArray[_iSelectElementIndex];
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
    protected virtual void doResetInEditor()
    {
      
    }
    
    // initialize this entity: called within doSpawnCom() only
    // m_bIsInitialized will be SET in this method
    protected virtual void doInitialize()
    {
      // sanity checking
      // check entity type
      if(EntityTypes.ENTITY_TYPES_UNKNOWN == EntityType)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "]: No Entity Type");

        return;
      }

      // initialize all data members
      // try to use keyframed spawn animation instead of simulating spawn animation via code
      m_bIsSimulatingSpawn = false;

      // set to initializing state
      m_eCurEntityState = EntityStates.ENTITY_STATES_INITIALIZING;

      // try to get reference to animator component
      m_pAnimatorComp = gameObject.GetComponent < Animator >();
      if(m_pAnimatorComp != null)
      {
        tryToFindBaseAnimatorLayer();
      }

      // set initialization flag
      m_bIsInitialized = true;
    }

    /**
     * doUpdate() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last doUpdate() call
     * check false == m_bIsInitialized before doing the real thing
     */
    protected virtual void doUpdate()
    {

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
    protected virtual bool doAnimationEndedHandle(int a_iAnimationNameHash)
    {
      // branching on different animations
      if(ANIMATION_SPAWN == a_iAnimationNameHash)
      {
        onAnimationSpawnEnded();
        
        return true;
      }
      
      else if(ANIMATION_HIT == a_iAnimationNameHash)
      {
        onAnimationHitEnded();
        
        return true;
      }
      
      else if(ANIMATION_STRUCK == a_iAnimationNameHash)
      {
        onAnimationStruckEnded();
        
        return true;
      }
      
      else if(ANIMATION_DEAD == a_iAnimationNameHash)
      {
        onAnimationDeadEnded();
        
        return true;
      }

      else if(ANIMATION_UNSPAWN == a_iAnimationNameHash)
      {
        onAnimationUnSpawnEnded();

        return true;
      }

      return false;
    }

    // Callbacks of Animation Playback
    // called when Spawn animation ended
    protected virtual void onAnimationSpawnEnded()
    {
      
    }

    // called when UnSpawn animation ended
    protected virtual void onAnimationUnSpawnEnded()
    {
      
    }

    // called when Common Hit/GotDamaged animation ended: Hit/GotDamaged Animation will go automatically to Idle state when done
    protected virtual void onAnimationHitEnded()
    {

    }
    
    // called when Strong Hit/GotDamaged animation ended
    protected virtual void onAnimationStruckEnded()
    {
      
    }
    
    // called when Dead animation ended
    protected virtual void onAnimationDeadEnded()
    {
      
    }

    // Once Animation uses Trigger to play it back
    // randomly play an animation from the given animation list using trigger
    // return true on success
    protected virtual bool playAnimationTrigger(AnimationData[] a_aAnimationList)
    {
      if(null == m_pAnimatorComp          // NO animator at all
         ||                               // or
         0 == a_aAnimationList.Length     // NOTHING to play
      )
      {
        return false;
      }

      // randomly choose an animation
      AnimationData _pSelectedAnimation = getArrayRandomElement(a_aAnimationList);

      // play the selected animation
      return playAnimationTrigger(_pSelectedAnimation);
    }

    // Looping Animation uses Flag to play it back
    // randomly play an animation from the given animation list using flag
    // return true on success
    protected virtual bool playAnimationFlag(AnimationData[] a_aAnimationList)
    {
      if(null == m_pAnimatorComp            // NO animator at all
         ||                                 // or
         0 == a_aAnimationList.Length       // NOTHING to play
      )
      {
        return false;
      }

      // randomly choose an animation
      AnimationData _pSelectedAnimation = getArrayRandomElement(a_aAnimationList);

      // play the selected animation
      return playAnimationFlag(_pSelectedAnimation);
    }

    // One Time Animation uses Trigger to play it back
    // randomly play an animation from the given animation data list using Trigger
    // return true on success
    protected virtual bool playAnimationTrigger(AnimationData a_pAnimationData)
    {
      if(null == m_pAnimatorComp      // NO animator at all
         ||                           // or
         null == a_pAnimationData     // NOTHING to play
        )
      {
        DefaultGameLogger.Instance.dumpLogError("[playAnimationTrigger][" + gameObject.ToString() + "]: try to play with ONE Empty Animation");

        return false;
      }

      // play the animation
      if(true == m_pAnimatorComp.HasState(m_iAnimatorBaseLayerIndex, Animator.StringToHash(a_pAnimationData.Trigger)))
      {
        m_pAnimatorComp.SetTrigger(Animator.StringToHash(a_pAnimationData.Trigger));

        return true;
      }

      DefaultGameLogger.Instance.dumpLogError("[playAnimationTrigger][" + gameObject.ToString() + "]: try to play with NON-Existing Animation: " + a_pAnimationData.Trigger);

      return false;
    }

    // Looping Animation uses Flag to play it back
    // randomly play an animation from the given animation data list using Trigger
    // return true on success
    protected virtual bool playAnimationFlag(AnimationData a_pAnimationData)
    {
      if(null == m_pAnimatorComp         // NO animator at all
         ||                              // or
         null == a_pAnimationData        // NOTHING to play
        )
      {
        DefaultGameLogger.Instance.dumpLogError("[playAnimationFlag][" + gameObject.ToString() + "]: try to play with ONE Empty Animation");

        return false;
      }

      // play the animation
      if(true == m_pAnimatorComp.HasState(m_iAnimatorBaseLayerIndex, Animator.StringToHash(a_pAnimationData.Trigger)))
      {
        m_pAnimatorComp.SetBool(Animator.StringToHash(a_pAnimationData.Trigger), true);

        return true;
      }

      DefaultGameLogger.Instance.dumpLogError("[playAnimationFlag][" + gameObject.ToString() + "]: try to play with NON-Existing Animation: " + a_pAnimationData.Trigger);

      return false;
    }

    // stop the given loop/lasting animation using boolean flag
    // return true on success
    protected virtual bool stopLoopAnimation(AnimationData a_pLoopAnimationData)
    {
      // stop the animation
      if(true == m_pAnimatorComp.HasState(m_iAnimatorBaseLayerIndex, Animator.StringToHash(a_pLoopAnimationData.Trigger)))
      {
        m_pAnimatorComp.SetBool(Animator.StringToHash(a_pLoopAnimationData.Trigger), false);

        return true;
      }

      DefaultGameLogger.Instance.dumpLogError("[playAnimationFlag][" + gameObject.ToString() + "]: try to stop a NON-Existing Animation: " + a_pLoopAnimationData.Trigger);

      return false;
    }

    // simulate the spawn animation
    protected virtual void doSpawnAnimation()
    {
      m_bIsSimulatingSpawn = true;
    }

    // check if this entity can handle the current collision
    protected virtual bool canHandleCollision(Entity a_pTrigger,                   // who has triggered this Hit
                                              CollisionHandler a_pCollidedPart     // collided part
                                             )
    {
      return canHandleHit();
    }

    // determine if spawn process will need to wait for completion of spawn animation: keyframed or simulated
    protected virtual bool waitForSpawnAnimation()
    {
      return true;
    }

    // called after doSpawnCom() has done
    protected virtual void onSpawnComDone()
    {
      m_bIsSimulatingSpawn = true;

      // check if this entity has a spawn animation: we are using the same string for one animator state and its trigger parameter
      AnimationData _pSelectedSpawnAnimation = null;

      // if the given spawn animation trigger name list is NOT empty
      if(m_pAnimatorComp != null
         &&
         SpawnAnimations.Length > 0
        )
      {
        // randomly choose a spawn animation
        _pSelectedSpawnAnimation = getArrayRandomElement(SpawnAnimations);

        // if the animator also has this state
        if(true == m_pAnimatorComp.HasState(m_iAnimatorBaseLayerIndex, Animator.StringToHash(_pSelectedSpawnAnimation.Trigger)))
        {
          m_bIsSimulatingSpawn = false;
        }
        else
        {
          // otherwise, iterate over whole name list one by one
          for(int _iCurSpawnTriggerIdx = 0; _iCurSpawnTriggerIdx < SpawnAnimations.Length; _iCurSpawnTriggerIdx++)
          {
            // animator has the current state
            if(true == m_pAnimatorComp.HasState(m_iAnimatorBaseLayerIndex, Animator.StringToHash(SpawnAnimations[_iCurSpawnTriggerIdx].Trigger)))
            {
              // use this spawn animation instead of stimulating it
              m_bIsSimulatingSpawn = false;

              break;
            }
          }
        }
      }

      // if we need to simulate the spawn animation
      if(true == m_bIsSimulatingSpawn)
      {
        // do it
        doSpawnAnimation();        
      }

      // otherwise, Spawn animation will ALWAYS be the Default Animation: NOTHING WILL BE DONE

      // we have to waiting for the completion of spawn animation: keyframed or simulated
      if(true == waitForSpawnAnimation())
      {
        // change to right state
        m_eCurEntityState = EntityStates.ENTITY_STATES_SPAWNING;
      }
      else
      {
        // NO need for waiting
        m_eCurEntityState = EntityStates.ENTITY_STATES_IDLE;
      }
    }
    #endregion // PROTECTED OVERRIDDENS

    // try to find the base layer of the attached Animator component
    protected void tryToFindBaseAnimatorLayer()
    {
      // get animator base layer's index
      m_iAnimatorBaseLayerIndex = m_pAnimatorComp.GetLayerIndex("Base Layer");
    }

    // check if this entity is in any sort of attacking
    protected bool isInAnyAttack()
    {
      return ((int)m_eCurEntityState & (int)EntityStates.ENTITY_STATES_ANY_ATTACKING) != 0;
    }

    // check if this entity is in any sort of hit
    protected bool isInAnyHit()
    {
      return ((int)m_eCurEntityState & (int)EntityStates.ENTITY_STATES_ANY_HIT) != 0;
    }

    // check if this entity is in any sort of busy state
    protected bool isBusy()
    {
      return ((int)m_eCurEntityState & (int)EntityStates.ENTITY_STATES_BUSY) != 0;
    }
    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    // common animation name hash IDs: JUST for ONE TIME animations NO looped animations
    /**
     *  Spawn, Hit, Struck, Dead, UnSpawn
     */
    protected static int ANIMATION_SPAWN        = Animator.StringToHash("Spawn");
    protected static int ANIMATION_UNSPAWN      = Animator.StringToHash("UnSpawn");
    protected static int ANIMATION_HIT          = Animator.StringToHash("Hit");
    protected static int ANIMATION_STRUCK       = Animator.StringToHash("Struck");
    protected static int ANIMATION_DEAD         = Animator.StringToHash("Dead");

    // state
    protected EntityStates m_eCurEntityState;

    // animator
    protected Animator m_pAnimatorComp;
    // animator base layer index
    protected int m_iAnimatorBaseLayerIndex;

    // indicates we are simulating the spawning animation
    protected bool m_bIsSimulatingSpawn;

    // indicates if this entity has been spawned before
    protected bool m_bHasBeenSpawned = false;
    // initial local orientation of this entity
    protected Quaternion m_qInitLocalOrientation;
    // spawn orientation
    protected Quaternion m_qSpawnOrientation;
    // initialization flag
    protected bool m_bIsInitialized;
    #endregion // PROTECTED DATA MEMBERS

    #region PRIVATE METHODS
    // PRIVIATE MONO INTERFACE: NOT USED IN SUBCLASSES
    // USE doXYZ() instead
    
    /**
     * Reset() is called when the user hits the Reset button in the Inspector's context menu
     * or when adding the component the first time 
     * This function is only called in editor mode 
     * Reset is most commonly used to give good default values in the inspector
     */
    void Reset()
    {
      doResetInEditor();
    }

    /**
     * Update() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last Update() call
     */
    void Update()
    {
      doUpdate();
    }

    /**
     * called from each ONE time animation, after it has been played back
     * a_szAnimationName is the animation name: like
     * - Spawn
     * - Jump
     * - Leap: High Jump
     * - Attack
     * - Melee
     * - Hit: Common Hit
     * - Struck: Strong Hit
     * - Dead 
     */
    void onAnimationEnded(string a_szAnimationName)
    {
#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogDebug("[" + gameObject.ToString() + "]: Animation Ended: " + a_szAnimationName);
#endif
      
      int _iAnimationNameHash = Animator.StringToHash(a_szAnimationName);
      
      doAnimationEndedHandle(_iAnimationNameHash);
    }

    #endregion // PRIVATE METHODS
  }

} // namespace Game.Logic

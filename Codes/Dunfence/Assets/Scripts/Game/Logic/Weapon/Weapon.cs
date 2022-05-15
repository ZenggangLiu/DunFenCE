/**
 * Base Class for all Sorts of Weapons: Weapon JUST has Attack property NO Defend property
 * - Melee Weapon: Has Bounding Box
 * - Shoot Weapon: NO Bounding Box
 * - Projectile: shot out of Shoot Weapon: Has Bounding Box
 */

using UnityEngine;
using Framework.Utils;
using Framework.Logger;

namespace Game.Logic
{
  // weapon types
  public enum WeaponTypes
  {
    WEAPON_TYPES_UNKNOWN = 0,
    // weapon used in hand to hand combat
    WEAPON_TYPES_MELEEWEAPON,
    // weapon used for shooting
    WEAPON_TYPES_SHOOTWEAPON,
    // projectile, which had been shot out of shoot weapon
    WEAPON_TYPES_PROJECTILE,
    // beam, which had been triggered by beam weapon
    WEAPON_TYPES_BEAM,
  }

  // weapon mount types
  public enum WeaponMountTypes
  {
    WEAPON_MOUNT_TYPES_UNKNOWN = 0,
    // body attack: no specific mounting
    WEAPON_MOUNT_TYPES_PUREBODY,
    // mounted to left hand: LEHMountPos
    WEAPON_MOUNT_TYPES_LEFT_HAND,
    // mounted to right hand: RIHMountPos
    WEAPON_MOUNT_TYPES_RIGHT_HAND,
    // mounted to double hand: DOHMountPos
    WEAPON__MOUNTTYPES_DOUBLE_HAND,
    // mounted to shoulder: SHDMountPos
    WEAPON__MOUNTTYPES_SHOULDER,
    // mounted to shoot pos point: ShootPos
    WEAPON__MOUNT_TYPES_SHOOTOUT,
  }

  // damage types
  public enum DamageTypes
  {
    DAMAGE_TYPES_UNKNOWN           = 0,
    // no damage at all
    DAMAGE_TYPES_NO                = 1,
    // one time damage
    DAMAGE_TYPES_ONETIME           = 1 << 1,
    // Lasting Damage
    DAMAGE_TYPES_LASTING           = 1 << 2,
    // range damage
    DAMAGE_TYPES_RANGE             = 1 << 7,
    // one time range damage
    DAMAGE_TYPES_RANGE_ONETIME     = DAMAGE_TYPES_RANGE | DAMAGE_TYPES_ONETIME,
    // lasting range damage
    DAMAGE_TYPES_RANGE_LASTING     = DAMAGE_TYPES_RANGE | DAMAGE_TYPES_LASTING,
  }

  // base class of different weapons
  public class Weapon : Entity
  {
    #region PUBLIC DATA MEMBERS
    // ALL TIMES ARE IN SECONDS
    // ALL DISTANCES ARE IN METERS
    // idle animation
    public string[] IdleAnimations = {"Idle"};
    // weapon type: MUST be set by code before using this weapon
    public WeaponTypes WeaponType = WeaponTypes.WEAPON_TYPES_UNKNOWN;
    // owner type
    public CharacterTypes OwnerType = CharacterTypes.CHARACTER_TYPES_UNKNOWN;
    // weapon mount type
    public WeaponMountTypes MountType = WeaponMountTypes.WEAPON_MOUNT_TYPES_UNKNOWN;
    // level >= 1
    public int Level = 1;
    // maximal use time: 0 or negative value means infinite usage
    public float UseTime = -1.0f;
    // attack distance: two circles indicating starting of attack(MinAttackDist) and stopping of attack(MaxAttackDist)
    // minimal attack distance: >= 0
    public float MinAttackDist = 0.0f;
    // maximal attack distance: != 0: negative value means NO maximal attack distance
    public float MaxAttackDist = -1.0f;

    // damage: forwarded to hit characters
    // damage type
    public DamageTypes DamageType = DamageTypes.DAMAGE_TYPES_UNKNOWN;
    // damage points: >= 0
    public int DamagePoints = -1;
    // damage maximal lasting time: 0 or negative value means one time damage
    // NOTE: by DamageLastTime > 0, this weapon will cause Hit Entity damage DamagePoints/Second
    public float DamageLastTime = -1.0f;
    // maximal damage range: 0 or negative value means non-range damage
    public float DamageRange = -1.0f;
    // damage upgarde
    // equation: current damage points + current level * DamageUpgradePoints * DamageScale
    // damage upgrade points: >= 0
    public int UpgradePoints = -1;
    // damage upgrade scale factor: > 0
    public float UpgradeScale = 1.0f;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    // check if this weapon is in use
    public bool isInUse
    {
      get
      {
        return isInAnyAttack();
      }
    }

    // get current level
    public int CurrentLevel
    {
      get
      {
        return m_iCurLevel;
      }
    }

    // get current use time
    public float CurrentUseTime
    {
      get
      {
        return m_fCurUseTime;
      }
    }

    // get current damage points
    public int CurrentDamagePoints
    {
      get
      {
        return m_iCurDamagePoints;
      }
    }

    // get current lasting time
    public float CurrentLastingTime
    {
      get
      {
        return m_fCurLastingTime;
      }
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
      // check owner type
      if(CharacterTypes.CHARACTER_TYPES_UNKNOWN == OwnerType)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "]: No Owner Type");
        
        return;
      }

      // create the hit effect
      switch(a_pTrigger.EntityType)
      {
      case EntityTypes.ENTITY_TYPES_CHARACTER:
        {
          // hit a character
          EffectMgr.Instance.spawnEffect(292, transform.position);

          break;
        }

      case EntityTypes.ENTITY_TYPES_BUILDING:
        {
          // hit the building
          EffectMgr.Instance.spawnEffect(296, transform.position);

          break;
        }

      case EntityTypes.ENTITY_TYPES_GROUND:
        {
          // hit the ground
          EffectMgr.Instance.spawnEffect(300, transform.position);

          break;
        }

      case EntityTypes.ENTITY_TYPES_LOOT:
        {
          // hit a loot
          EffectMgr.Instance.spawnEffect(292, transform.position);

          break;
        }

      default:
        {
          break;
        }
      }
    }

    // animation related commands
    /**
     * Hit: normal hit
     * a_pTrigger: the entity who has trigger this Hit
     * a_pCollidedPart: which part of the object has been hit
     */
    public override void doHitCom(Entity a_pTrigger,                       // trigger entity
                                  CollisionHandler a_pCollidedPart         // collided part
                                 )
    {
      // when this weapon has hit some object
    }
    
    /**
     * Struck: strong hit
     * a_pTrigger: the entity who has trigger this Hit
     * a_pCollidedPart: which part of the object has been hit
     */
    public override void doStruckCom(Entity a_pTrigger,                    // trigger entity
                                     CollisionHandler a_pCollidedPart      // collided part
                                    )
    {
      doHitCom(a_pTrigger, a_pCollidedPart);
    }

    /**
     * Dead: destroy this entity
     */
    public override void doDeadCom()
    {
      base.doDeadCom();
    }

    /**
     * UnSpawn: last animation of whole entity's life
     */
    public override void doUnSpawnCom()
    {
      
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

      // change to idle state
      m_eCurEntityState = EntityStates.ENTITY_STATES_IDLE;
    }

    /**
     * doLevelUp() will be called after ONE Level UP
     */
    // used to upgrade damage points, damage lasting time, ...
    public virtual void doLevelUp()
    {
      m_iCurLevel++;

      // upgrade damage points
      // upgrade equation: current damage points + current level * DamageUpgradePoints * DamageScale
      m_iCurDamagePoints += (int)((float)m_iCurLevel * (float)UpgradePoints * UpgradeScale);

      // upgrade damage lasting time, if we need
      if(isLastingDamage())
      {
        m_fCurLastingTime += (float)m_iCurLevel * (float)UpgradePoints * UpgradeScale * 0.5f;
      }
    }

    // commands
    // doUseCom() -> weapon changed to Attacking/Shooting state ... -> doStopUseCom() -> weapon changed to Idle state
    /**
     * use
     * Note:
     * - For Shoot Weapon:
     *   + local Z: flying direction
     *   + local Y: up direction
     *   + local X: right direction
     */
    public virtual void doUseCom()
    {
      // is already in usage: return
      if(true == isInUse)
      {
        return;
      }

      // not idle state: return
      if(EntityStates.ENTITY_STATES_IDLE != m_eCurEntityState)
      {
        return;
      }

      // change to any attacking state
      m_eCurEntityState = EntityStates.ENTITY_STATES_ANY_ATTACKING;
    }

    /**
     * stop use
     */
    public virtual void doStopUseCom()
    {
      // change back to idle state
      doIdleCom();
    }

    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS
    
    #region PROTECTED METHODS
    // return true if this weapon is a lasting weapon
    protected bool isLastingDamage()
    {
      return ((int)DamageTypes.DAMAGE_TYPES_LASTING & (int)DamageType) != 0;
    }

    // return true if this weapon is a range damage weapon
    protected bool isRangeDamage()
    {
      return ((int)DamageTypes.DAMAGE_TYPES_RANGE & (int)DamageType) != 0;
    }

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
      EntityType =  EntityTypes.ENTITY_TYPES_WEAPON;
      // remove all default animations
      SpawnAnimations = null;
      HitAnimations = null;
      StruckAnimations = null;
      DeadAnimations = null;
    }

    // initialize this entity: called within doSpawnCom() only
    // m_bIsInitialized will be SET in this method
    protected override void doInitialize()
    {
      base.doInitialize();

      // clear initialization flag before checking
      m_bIsInitialized = false;

      // set data members
      m_iCurLevel = Level;
      m_fCurUseTime = UseTime;
      m_iCurDamagePoints = DamagePoints;
      m_fCurLastingTime = DamageLastTime;
      m_eCurEntityState = EntityStates.ENTITY_STATES_IDLE;

      // sanity checking
      // check weapon type
      if(WeaponTypes.WEAPON_TYPES_UNKNOWN == WeaponType)
      {
        DefaultGameLogger.Instance.dumpLogError("[Weapon][doInitialize][" + gameObject.ToString() + "]: No Weapon Type");
        
        return;
      }

      // check maximal attack distance: !=0
      if((MaxAttackDist > 0.0f && MaxAttackDist < 1.0e-5f)   // tiny positive value
         ||                                                  // or
         (MaxAttackDist < 0.0f && MaxAttackDist > -1.0e-5f)  // tiny negative value
        )
      {
        DefaultGameLogger.Instance.dumpLogError("[Weapon][doInitialize][" + gameObject.ToString() + "]: No Maximal Attack Distance");

        return;
      }


      // check mount type
      if(WeaponMountTypes.WEAPON_MOUNT_TYPES_UNKNOWN == MountType)
      {
        DefaultGameLogger.Instance.dumpLogError("[Weapon][doInitialize][" + gameObject.ToString() + "]: No Mount Type");
        
        return;
      }

      // check level
      if(Level < 1.0f)
      {
        DefaultGameLogger.Instance.dumpLogError("[Weapon][doInitialize][" + gameObject.ToString() + "]: No Weapon Level");
        
        return;
      }

      // check damage type
      if(DamageTypes.DAMAGE_TYPES_UNKNOWN == DamageType)
      {
        DefaultGameLogger.Instance.dumpLogError("[Weapon][doInitialize][" + gameObject.ToString() + "]: No Damage Type");
        
        return;
      }
      
      // check damage points
      if(DamageTypes.DAMAGE_TYPES_NO != DamageType // NOT NO damage type
         &&
         DamagePoints <= 0                         // NO damage points
        )
      {
        DefaultGameLogger.Instance.dumpLogError("[Weapon][doInitialize][" + gameObject.ToString() + "]: No Damage Points");
        
        return;
      }
      
      // check damage upgrade points
      if(UpgradePoints < 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[Weapon][doInitialize][" + gameObject.ToString() + "]: No Damage Upgrade Points");
        
        return;
      }
      
      // check damage upgrade scale
      if(UpgradeScale < 1.0e-5f)
      {
        DefaultGameLogger.Instance.dumpLogError("[Weapon][doInitialize][" + gameObject.ToString() + "]: No Damage Upgrade Scale");
        
        return;
      }
      
      // check damage lasting time
      if(true == isLastingDamage())
      {
        // lasting damage
        if(DamageLastTime < 1.0e-5f)
        {
          DefaultGameLogger.Instance.dumpLogError("[Weapon][doInitialize][" + gameObject.ToString() + "]: No Damage Lasting Time");
          
          return;
        }
      }
      
      // check damage range
      if(isRangeDamage())
      {
        // range damage
        if(DamageRange <= 0.0f)
        {
          DefaultGameLogger.Instance.dumpLogError("[Weapon][doInitialize][" + gameObject.ToString() + "]: No Damage Range");
          
          return;
        }
      }

      if(WeaponTypes.WEAPON_TYPES_SHOOTWEAPON != WeaponType)
      {
        // check rigidbody component
        if(null == gameObject.GetComponentInChildren< Rigidbody >())
        {
          DefaultGameLogger.Instance.dumpLogError("[Weapon][doInitialize][" + gameObject.ToString() + "]: No RigidBody");

          return;
        }

        // check bounding box
        if(null == gameObject.GetComponent < Collider >())
        {
          DefaultGameLogger.Instance.dumpLogError("[Weapon][doInitialize][" + gameObject.ToString() + "]: No Bounding Box");
          
          return;
        }
        
        // confirm the bounding box is a trigger
        if(false == gameObject.GetComponent < Collider >().isTrigger)
        {
          DefaultGameLogger.Instance.dumpLogError("[Weapon][doInitialize][" + gameObject.ToString() + "]: Bounding Box is NOT a Trigger");
          
          return;
        }
      }

      // set initialization flag
      m_bIsInitialized = true;
    }

    /**
     * doUpdate() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last doUpdate() call
     * based on the current state do processing
     * check false == m_bIsInitialized before doing the real thing
     */
    protected override void doUpdate()
    {
      // NOT initialized: return
      if(false == m_bIsInitialized)
      {
        return;
      }

      // if this weapon is already dead: return
      if(IsDead == true)
      {
        return;
      }

      // if the weapon is NOT in use: return
      if(false == isInUse)
      {
        return;
      }

      // if there is a maiximal use time
      if(UseTime > 1.0e-5f)
      {
        // count down the use timer
        m_fCurUseTime -= Time.deltaTime;
        
        if(m_fCurUseTime < 1.0e-5f)
        {
          // set this weapon state to dead
          m_eCurEntityState = EntityStates.ENTITY_STATES_DEAD;

          // reclaim this weapon
        }
      }
    }

    // determine if spawn process will need to wait for completion of spawn animation: keyframed or simulated
    protected override bool waitForSpawnAnimation()
    {
      // if the given spawn animation trigger name list is NOT empty
      if(m_pAnimatorComp != null
         &&
         SpawnAnimations.Length > 0
        )
      {
        // randomly choose a spawn animation
        AnimationData _pSelectedSpawnAnimation = getArrayRandomElement(SpawnAnimations);

        // if the animator also has this state
        if(true == m_pAnimatorComp.HasState(m_iAnimatorBaseLayerIndex, Animator.StringToHash(_pSelectedSpawnAnimation.Trigger)))
        {
          return true;
        }
      }

      return false;
    }

    // simulate the spawn animation
    protected override void doSpawnAnimation()
    {
      // NO spawn animation
    }
    #endregion // PROTECTED OVERRIDDENS
    
    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    // current weapon level
    protected int m_iCurLevel;
    // current use time
    protected float m_fCurUseTime;
    // current damage points
    protected int m_iCurDamagePoints;
    // current lasting time
    protected float m_fCurLastingTime;
    #endregion // PROTECTED DATA MEMBERS

    #region PRIVATE METHODS 

    #endregion // PRIVATE METHODS
  }

}

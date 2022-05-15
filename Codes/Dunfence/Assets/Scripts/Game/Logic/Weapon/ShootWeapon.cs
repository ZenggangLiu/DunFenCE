/**
 * Base Class for Shoot Weapon, which will fire some pre-defined amount of projectils from pre-defined shoot position list
 */

using UnityEngine;
using Framework.Logger;
using System.Collections.Generic;  // List< T >

namespace Game.Logic
{
  public class ShootWeapon : Weapon
  {
    #region PUBLIC DATA MEMBERS
    // animations
    // reload animation
    public AnimationData[] ReloadAnimations = {new AnimationData("Reload")};
    // shoot animation
    public AnimationData[] ShootAnimations  = {new AnimationData("Shoot")};

    // shooting property
    // total projectile count: negative value means INFINITE: != 0
    public int TotalProjectileCount = 0;
    // clip capacity: > 0
    public int ClipCap = 0;
    // consumed projectile per shoot: > 0
    public int ProjectilePerShoot = -1;
    // time needs from one shoot to next: >= 0: 0 means no cool down time
    // ShootA --> ShootCollDown --> ShootB
    public float ShootCoolDownTime = -1.0f; // for a bow: 0(because animation will account the cool down);  for a machine gun: 1/300
    // ClipUsageA --> ReloadTime --> ClipUsageB
    // reload time(cool down time after ClipCap reached ZERO): >= 0: 0 means NO reload time
    public float ReloadTime = -1.0f;

    // shooting Fx
    public GameObject ShootFx = null;

    // projectile property
    // projectile ID
    /**
     * NOTE: by negative value: universal shoot weapon
     * - NO shoot out in doUseCom() 
     * - use doChangeProjectileCom(ID) to change to the given projectile with its ID
     */
    // equipped projectile of this ShootWeapon will be equipped during character's doSpawnCom()
    public int ProjectileID = -1;
    // projectile shoot position list: != null
    public GameObject[] ProjectileShootPos = null;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    /**
     * set the shoot aim helper instance
     */
    public void setShootAimHelper(ShootAimHelper a_pAimHelper)
    {
      if(null == a_pAimHelper)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][setShootAimHelper]: Zero Aim Helper");
      }

      m_pAimHelper = a_pAimHelper;
    }

    #region PUBLIC OVERRIDDENS
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
    public override void doUseCom()
    {
      // start shootting
      // shooting right now: return
      if(EntityStates.ENTITY_STATES_SHOOTING == m_eCurEntityState)
      {
        return;
      }

      // not idle state: return
      if(EntityStates.ENTITY_STATES_IDLE != m_eCurEntityState)
      {
        return;
      }

      useWeapon();
    }
    
    /**
     * stop use
     */
    public override void doStopUseCom()
    {
      // disable Shooting Fx
      if(null != ShootFx)
      {
        ShootFx.SetActive(false);
      }

      // stop shooting
      doIdleCom();
    }

    /**
     * aim this weapon: use the stored shoot aim helper to get a target position
     */
    public virtual void doAimCom()
    {
      // if there is NO Aim Helper: return: NO weapon aiming at all
      if(null == m_pAimHelper)
      {
        return;
      }

      // otherwise, get the shooting target position
      Vector3 _vAimedPos;
      m_pAimHelper.getAimedPos(out _vAimedPos);

      // orientate the parent of this ShootWeapon at first
      if(transform.parent != null)
      {
        transform.parent.transform.forward = Vector3.Normalize(_vAimedPos - transform.position);
      }
      else
      {
        // if there is NO parent: orientate the weapon
        transform.forward = Vector3.Normalize(_vAimedPos - transform.position);   
      }
    }

    /**
     * load this weapon with the given projectile count: increase currently left projectile count
     */
    public virtual void doLoadCom(int a_iLoadProjectileCount)
    {
      // if there is a total projectile count
      if(TotalProjectileCount > 0)
      {
        m_iCurProjectileCount += a_iLoadProjectileCount;
      }
    }

    /**
     * change projectile type
     */
    public virtual void doChangeProjectile(int a_iProjectileTypeID)
    {
      // equip the given projectile
      List< int > _aProjectileList = new List<int>(1);
      _aProjectileList.Add(a_iProjectileTypeID);

      ProjectileMgr.Instance.equipProjectiles(_aProjectileList);

      // store the given projectile ID
      ProjectileID = a_iProjectileTypeID;
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
      base.doResetInEditor();

      // remove idle animation
      IdleAnimations = null;

      // set the right weapon type
      WeaponType = WeaponTypes.WEAPON_TYPES_SHOOTWEAPON;

      // change damage type to NO damage
      DamageType = DamageTypes.DAMAGE_TYPES_NO;

      // set damage points to zero
      DamagePoints = 0;

      // set damage upgrade to zero
      UpgradePoints = 0;
    }

    // initialize this entity: called within doSpawnCom() only
    // m_bIsInitialized will be SET in this method
    protected override void doInitialize()
    {
      base.doInitialize();

      // clear initialization flag before checking
      m_bIsInitialized = false;

      m_iCurProjectileCount = TotalProjectileCount;
      m_iCurClipCap = ClipCap;
      m_fCurShootCoolDownTime = ShootCoolDownTime;
      m_fCurReloadTime = ReloadTime;
      m_pAimHelper = null;

      // sanity checking
      // check projectile count: != 0
      if(0 == TotalProjectileCount)
      {
        DefaultGameLogger.Instance.dumpLogError("[ShootWeapon][doInitialize][" + gameObject.ToString() + "]: No Projectile Count");
        
        return;
      }

      // clip capacity: > 0
      if(0 == ClipCap)
      {
        DefaultGameLogger.Instance.dumpLogError("[ShootWeapon][doInitialize][" + gameObject.ToString() + "]: No Clip Capacity");
        
        return;
      }
      
      // check consumed projectile per shoot: > 0
      if(ProjectilePerShoot <= 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[ShootWeapon][doInitialize][" + gameObject.ToString() + "]: No Consumed Projectile Per Shoot");
        
        return;
      }
      
      // check shoot cool down time: >= 0
      if(ShootCoolDownTime < 0.0f)
      {
        DefaultGameLogger.Instance.dumpLogError("[ShootWeapon][doInitialize][" + gameObject.ToString() + "]: No Shoot Cool Down Time");
        
        return;
      }
      
      // check reload time: >= 0
      if(ReloadTime < 0.0f)
      {
        DefaultGameLogger.Instance.dumpLogError("[ShootWeapon][doInitialize][" + gameObject.ToString() + "]: No Reload Time");
        
        return;
      }

      // check projectile shoot position list: != null
      if(null == ProjectileShootPos
         ||
         0 == ProjectileShootPos.Length
        )
      {
        DefaultGameLogger.Instance.dumpLogError("[ShootWeapon][doInitialize][" + gameObject.ToString() + "]: No Projectile Shoot Position");

        return;
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

      // doUpdate() from base class(Weapon) could change current state to Dead
      base.doUpdate();

      switch(m_eCurEntityState)
      {
      case EntityStates.ENTITY_STATES_SPAWNING:
        {
          // in spawning, if there is NO spawn animation
          if(0 == SpawnAnimations.Length
            ||
            false == m_pAnimatorComp.HasState(m_iAnimatorBaseLayerIndex, Animator.StringToHash(SpawnAnimations[0].Trigger))
          )
          {
            doIdleCom();
          }

          break;
        }

      case EntityStates.ENTITY_STATES_RELOADING:
      {
        // reload
        // count down
        m_fCurReloadTime -= Time.deltaTime;

        // done with the current reloading
        if(m_fCurReloadTime < 1.0e-5f)
        {
          // use weapon
          useWeapon();
        }
        return;
      }

      case EntityStates.ENTITY_STATES_SHOOTING:
      {
        // keep using weapon
        useWeapon();

        return;
      }

      default:
      {
        return;
      }
      }
    }

      /**
     * called from each ONE time animation, after it has been played back
     * a_iAnimationNameHash is the animation name hash ID: like:
     * - Spawn
     * - Shoot
     * - Hit
     * - Struck: Powerful Hit
     * - Dead 
     * 
     * return true if handled
     */
    protected override bool doAnimationEndedHandle(int a_iAnimationNameHash)
    {
      // branching on different animations
      if(ANIMATION_RELOAD == a_iAnimationNameHash)
      {
        onAnimationReloadEnded();
        
        return true;
      }
      
      else if(true == base.doAnimationEndedHandle(a_iAnimationNameHash))
      {
        return true;
      }
      
      else
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "]: UNKNOWN ANIMATION PLAYED");
        
        return false;
      }
    }
    
    // called when Reload animation ended
    protected virtual void onAnimationReloadEnded()
    {
      // if the character has changed to other state: return
      if(EntityStates.ENTITY_STATES_RELOADING != m_eCurEntityState)
      {
        return;
      }
      
      doIdleCom();
    }

    /**
     * use this weapon: shoot out projectiles
     */
    protected virtual void useWeapon()
    {
      // NO equipped projectile: return to idle state
      if(ProjectileID < 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][useWeapon]: Trying to shoot a weapon without equipping any Projectile. \nCall doChangeProjectile() to equip one");

        doIdleCom();

        return;
      }

      // check if we need to reload this weapon
      if(m_iCurClipCap <= 0)
      {
        // if there is NO projectile any more
        if(TotalProjectileCount > 0   // there is a total projectile count
           &&                         // and
           m_iCurProjectileCount <= 0 // the current projectile count <= 0
          )
        {
          // change to idle state
          doIdleCom();

          return;
        }
        else
        {
          // reload this weapon
          // take one full clip
          int _iProjectileTaken = ClipCap;

          if(TotalProjectileCount > 0                  // there is a total projectile count
             &&                                        // and
             _iProjectileTaken > m_iCurProjectileCount // no more left for the clip
          )
          {
            _iProjectileTaken = m_iCurProjectileCount;
          }

          // load the projectile
          m_iCurClipCap = _iProjectileTaken;

          // if there is a reload time
          if(ReloadTime > 0.0f)
          {
            // play reload animation
            playAnimationTrigger(ReloadAnimations);

            // store count down timer
            m_fCurReloadTime = ReloadTime;

            // change to reloading state
            m_eCurEntityState = EntityStates.ENTITY_STATES_RELOADING;

            return;
          }
        }
      }

      // if we are not in shooting state
      if(EntityStates.ENTITY_STATES_SHOOTING != m_eCurEntityState)
      {
        // shoot out straight away
        shootProjectile();

        // change to shooting state
        m_eCurEntityState = EntityStates.ENTITY_STATES_SHOOTING;
      }
      else
      {
        // if there is NO shoot cool down timer
        if(m_fCurShootCoolDownTime < 1.0e-5f)
        {
          // shoot out straight away
          shootProjectile();
        }
        else
        {
          // otherwise, count down the timer
          m_fCurShootCoolDownTime -= Time.deltaTime;
        }
      }
    }

    /**
     * shoot out projectiles according to ConsumedProjectilePerShoot
     */
    protected virtual void shootProjectile(List< GameObject > a_aShotProjectileList = null)
    {
      // before shoot the weapon, aim it at first
      doAimCom();

      // play the shoot animation
      playAnimationTrigger(ShootAnimations);

      // play shooting fx
      if(null != ShootFx)
      {
        ShootFx.SetActive(false);
        ShootFx.SetActive(true);
      }

      // try to fire the given amount of projectiles
      int _iFireProjectileCount = ProjectilePerShoot;

      // if not enough projectiles in clip
      if(_iFireProjectileCount > m_iCurClipCap)
      {
        _iFireProjectileCount = m_iCurClipCap;
      }

      // if no enough shoot pos
      if(_iFireProjectileCount > ProjectileShootPos.Length)
      {
        _iFireProjectileCount = ProjectileShootPos.Length;
      }

      for(int _iCurProjectileIndex = 0; _iCurProjectileIndex < _iFireProjectileCount; _iCurProjectileIndex++)
      {
        // get the current shoot position
        GameObject _pCurShootPos = ProjectileShootPos[_iCurProjectileIndex];

        // shoot out the projectile
        if(null == a_aShotProjectileList)
        {
          ProjectileMgr.Instance.shootProjectile(ProjectileID, OwnerType, _pCurShootPos.transform.position, _pCurShootPos.transform.forward, _pCurShootPos.transform.up, null);  
        }
        else
        {
          // shoot out and get the shot projectile
          ProjectileMgr.Instance.shootProjectile(ProjectileID, OwnerType, _pCurShootPos.transform.position, _pCurShootPos.transform.forward, _pCurShootPos.transform.up, a_aShotProjectileList);
        }
      }

      // decrease projectiles used in clip
      m_iCurClipCap -= _iFireProjectileCount;

      // decrease the total projectiles
      m_iCurProjectileCount -= _iFireProjectileCount;

      // set cool down between each shoot
      m_fCurShootCoolDownTime = ShootCoolDownTime;
    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    // common animation name hash IDs: JUST for ONE TIME animations NO looped animations
    /**
     *  Reload
     */
    protected static int ANIMATION_RELOAD = Animator.StringToHash("Reload");
    // current project count: negative value means INFINITE
    protected int m_iCurProjectileCount;
    // current clip capacity: negative value means INFINITE
    protected int m_iCurClipCap;
    // current shoot cool down time
    protected float m_fCurShootCoolDownTime;
    // current reload time
    protected float m_fCurReloadTime;
    // shoot aim helper instance
    protected ShootAimHelper m_pAimHelper;
    #endregion // PROTECTED DATA MEMBERS
  }

} // namespace Game.Logic

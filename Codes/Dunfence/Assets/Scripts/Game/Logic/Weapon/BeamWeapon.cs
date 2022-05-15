/**
 * Base Class of all Beam type weapon: Fire Thrower, Laser Gun, Lightening Cannon
 * NO extra projectile ID, the Beam Projectile MUST to be attached onto its ShootPos
 */

using UnityEngine;
using System.Collections.Generic;  // List< T >

namespace Game.Logic
{
  public class BeamWeapon : ShootWeapon
  {
    #region PUBLIC DATA MEMBERS
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS

    #region PUBLIC OVERRIDDENS
    // commands
    /**
     * use
     * spawn the given beam at ShootPos using its 
     *   + local Z: flying direction
     *   + local Y: up direction
     *   + local X: right direction
     */
    public override void doUseCom()
    {
      // start shootting beam
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

      // shoot out the beam
      shootProjectile();

      // force clip cap and total projectile count both equal to ONE
      m_iCurClipCap = 1;
      m_iCurProjectileCount = 1;

      // change to shooting state
      m_eCurEntityState = EntityStates.ENTITY_STATES_SHOOTING;
    }

    /**
     * stop use
     * destroy all spawned beams
     */
    public override void doStopUseCom()
    {
      // stop using this weapon
      base.doStopUseCom();

      // stop using all mounted beams
      // iterate all shoot positions
      for(int _iCurShootPosIdx = 0; _iCurShootPosIdx < ProjectileShootPos.Length; _iCurShootPosIdx++)
      {
        // iterate all mounted beams of the current shoot pos
        for(int _iCurBeamIdx = 0; _iCurBeamIdx < ProjectileShootPos[_iCurShootPosIdx].transform.childCount; _iCurBeamIdx++)
        {
          // the current mounted child is a beam
          if(ProjectileShootPos[_iCurShootPosIdx].transform.GetChild(_iCurBeamIdx).GetComponentInChildren< Beam >() == null)
          {
            // goto next mounted child
            continue;
          }

          // use the current beam
          ProjectileShootPos[_iCurShootPosIdx].transform.GetChild(_iCurBeamIdx).GetComponentInChildren< Beam >().doStopUseCom();
        }
      }
    }

    /**
     * load this weapon with the given projectile count: increase currently left projectile count
     * NO loading
     */
    public override void doLoadCom(int a_iLoadProjectileCount)
    {
      return;
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS
    protected void spawnAllBeams()
    {
      // iterate all shoot positions
      for(int _iCurShootPosIdx = 0; _iCurShootPosIdx < ProjectileShootPos.Length; _iCurShootPosIdx++)
      {
        // iterate all mounted beams of the current shoot pos
        for(int _iCurBeamIdx = 0; _iCurBeamIdx < ProjectileShootPos[_iCurShootPosIdx].transform.childCount; _iCurBeamIdx++)
        {
          // the current mounted child is a beam
          if(ProjectileShootPos[_iCurShootPosIdx].transform.GetChild(_iCurBeamIdx).GetComponentInChildren< Beam >() == null)
          {
            // goto next mounted child
            continue;
          }

          // set owner type of the current beam
          // set the owner type before enabling this projectile
          ProjectileShootPos[_iCurShootPosIdx].transform.GetChild(_iCurBeamIdx).GetComponentInChildren< Beam >().setOwnerType(OwnerType);
          // spawn the currrent beam
          ProjectileShootPos[_iCurShootPosIdx].transform.GetChild(_iCurBeamIdx).GetComponentInChildren< Beam >().doSpawnCom();
        }
      }
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
      base.doResetInEditor();

      // remove reload animation
      ReloadAnimations = null;

      // force set projectile id to -1
      ProjectileID = -1;

      // set reseanalble values
      TotalProjectileCount = 1;
      ClipCap = 1;
      ProjectilePerShoot = 1;
      ShootCoolDownTime = 0.0f;
      ReloadTime = 0.0f;
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
      //base.doUpdate();

      // NO keeping shooting and reloading
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

      default:
        {
          return;
        }
      }
    }

    /**
     * shoot out projectiles
     */
    // use all mounted beam projectiles
    protected override void shootProjectile(List< GameObject > a_aShotProjectileList = null)
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

      // use all mounted beams
      // iterate all shoot positions
      for(int _iCurShootPosIdx = 0; _iCurShootPosIdx < ProjectileShootPos.Length; _iCurShootPosIdx++)
      {
        // iterate all mounted beams of the current shoot pos
        for(int _iCurBeamIdx = 0; _iCurBeamIdx < ProjectileShootPos[_iCurShootPosIdx].transform.childCount; _iCurBeamIdx++)
        {
          // the current mounted child is a beam
          if(ProjectileShootPos[_iCurShootPosIdx].transform.GetChild(_iCurBeamIdx).GetComponentInChildren< Beam >() == null)
          {
            // goto next mounted child
            continue;
          }

          // use the current beam
          ProjectileShootPos[_iCurShootPosIdx].transform.GetChild(_iCurBeamIdx).GetComponentInChildren< Beam >().doUseCom();
        }
      }
    }

    // called after doSpawnCom() has done
    protected override void onSpawnComDone()
    {
      base.onSpawnComDone();

      // spawn all beams
      spawnAllBeams();
    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS
  }

} // namespace Game.Logic

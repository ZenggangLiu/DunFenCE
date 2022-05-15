/**
 * this class is used for any sort of Projectile
 * - Dart: 
 *   + flying along straight line NO   rotation around local Z: bullet, arrow, non-chasing missile
 *   + flying along straight line with rotation around local Z: thrown axe, thrown knife
 * - Chasing Bullet/Missile
 *   + flying along straight line and chasing the nearest object
 * - Grenade, Bomb, Cannon Shell, Flame Bottle
 *   + flying along curve
 * - Laser Beam, Lightning and Magic Plasma
 *   + flying slowly to the nearest object
 */

using UnityEngine;
using Framework.Logger;

namespace Game.Logic
{
  // local rotation types
  public enum RotationTypes
  {
    ROTATION_TYPES_UNKNOWN,
    ROTATION_TYPES_NO,           // no rotation
    ROTATION_TYPES_LOCAL_X,      // rotate around local x axis
    ROTATION_TYPES_LOCAL_Y,      // rotate around local y axis
    ROTATION_TYPES_LOCAL_Z,      // rotate around local z axis
  }

  // local translate types
  public enum TranslateTypes
  {
    TRANSLATE_TYPES_UNKNOWN,
    TRANSLATE_TYPES_NO,          // no translate
    TRANSLATE_TYPES_CONSTANT,    // constant translate: just consider MaxTranslate
    TRANSLATE_TYPES_CHANGING,    // changing translate: consider MaxTranslate, TranslateSpeed
  }

  public class Projectile : Weapon
  {
    #region PUBLIC DATA MEMBERS
    // flying speed: move speed along local z axis: >= 0
    public float FlySpeed = -1.0f;
    // local rotation type
    public RotationTypes RotType = RotationTypes.ROTATION_TYPES_UNKNOWN;
    // rotation speed: clockwise rotation is POSITIVE rotation: any value
    public float RotSpeed = -1.0f;
    // local translate type
    public TranslateTypes TransType = TranslateTypes.TRANSLATE_TYPES_UNKNOWN;
    // translate speed: any value
    public float TransSpeed = 0.0f;
    // maximal translate distance: any value: but same sign as TransSpeed
    public float MaxTransDist = 0.0f;
    // minimal fly speed for fast moving projectile
    public float MinFastFlySpeed = 1.0f;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    /**
     * set owner's type
     * MUST be called by shoot weapon before enable this script
     */
    public void setOwnerType(CharacterTypes a_eOwnerType)
    {
      OwnerType = a_eOwnerType;
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
      // create the hit effect
      base.onCollision(a_pTrigger, a_pCollidedPart);

      // destroy this projectile
      doDeadCom();
    }

    // animation related commands
    /**
     * Spawn
     * a_vSpawnPosWs: the world space spawn position of this entity
     * a_vSpawnDirWs: the world space forward direction of this entity
     * a_vSpawnUpWs : the world space up direction of this entity
     */
    public override void doSpawnCom(Vector3 a_vSpawnPosWs,                 // spawn position in world space
                                    Vector3 a_vSpawnDirWs,                 // spawn heading direction in world space
                                    Vector3 a_vSpawnUpWs                   // spawn up direction in world space
                                   )
    {
      base.doSpawnCom(a_vSpawnPosWs, a_vSpawnDirWs, a_vSpawnUpWs);

      // store the spawn translation
      m_vSpawnTranslation = a_vSpawnPosWs;

      // store fly direction
      m_vFlyDirection = a_vSpawnDirWs;

      // store effective maximal translate distance
      // because Unity use: TRS to compose the final local to world transform
      // if we want to apply a new local translate T_t: TRS * T_t: the final translate in world space will be scaled by S
      // we have to compensate that scaling
      m_fEffectiveMaxTranslateDistance = MaxTransDist * 1.0f / transform.localScale.x;

      // clear current rotated angle
      m_fCurRotatedAngle_Degree = 0.0f;

      // clear current local translated distance
      m_fCurLocalTranslateDistance = 0.0f;

      // clear current flyed distance
      m_fCurFlyedDistance = 0.0f;

      // set spawn position
      transform.position = m_vSpawnTranslation;

      // set spawn orientation
      transform.rotation = m_qSpawnOrientation;

      // active the current game object
      gameObject.SetActive(true);

      // change to attacking state
      m_eCurEntityState = EntityStates.ENTITY_STATES_ATTACKING;
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
    public override void doUseCom()
    {
      // make sure the current state of this projectile is ENTITY_STATES_ATTACKING
      m_eCurEntityState = EntityStates.ENTITY_STATES_ATTACKING;
    }

    /**
     * Dead: destroy this entity
     */
    public override void doDeadCom()
    {
      base.doDeadCom();

      // destroy this projectile
      // change state to destroying
      m_eCurEntityState = EntityStates.ENTITY_STATES_DESTROYING;

      // destroy this projectile via projectile manager
      ProjectileMgr.Instance.destroyProjectile(gameObject);

      // change state to dead
      m_eCurEntityState = EntityStates.ENTITY_STATES_DEAD;
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

      // set the right weapon type
      WeaponType = WeaponTypes.WEAPON_TYPES_PROJECTILE;
      // set the right mount type
      MountType = WeaponMountTypes.WEAPON__MOUNT_TYPES_SHOOTOUT;
      // remove idle animation
      IdleAnimations = null;
      // set default maximal attack distance
      MaxAttackDist = 3.0f;
    }

    // initialize this entity: called within doSpawnCom() only
    // m_bIsInitialized will be SET in this method
    protected override void doInitialize()
    {
      base.doInitialize();

      // clear initialization flag before checking
      m_bIsInitialized = false;

      // remove idle animations
      IdleAnimations = null;

      m_fCurRotatedAngle_Degree = 0.0f;
      m_fEffectiveMaxTranslateDistance = 0.0f;
      m_fCurLocalTranslateDistance = 0.0f;
      m_fCurFlyedDistance = 0.0f;
      m_bIsFastFlyingProjecitle = false;

      // sanity check
      // check fly speed: >= 0
      if(FlySpeed < 0.0f)
      {
        DefaultGameLogger.Instance.dumpLogError("[Projectile][doInitialize][" + gameObject.ToString() + "]: No Fly Speed");

        return;
      }

      // check if a fast moving projectile
      if(FlySpeed >= MinFastFlySpeed)
      {
        // disable collider component
        if(GetComponentInChildren< Collider >() != null)
        {
          GetComponentInChildren< Collider >().enabled = false;
        }

        // create hit info
        m_pCurHitInfo = new RaycastHit();

        // turn on fast flying flag
        m_bIsFastFlyingProjecitle = true;
      }

      // check rotation type
      if(RotationTypes.ROTATION_TYPES_UNKNOWN == RotType)
      {
        DefaultGameLogger.Instance.dumpLogError("[Projectile][doInitialize][" + gameObject.ToString() + "]: No Rotation Type");
        
        return;
      }

      // check rotation speed
      if(RotationTypes.ROTATION_TYPES_NO != RotType
         &&
         ((RotSpeed > 0.0f && RotSpeed < 1.0e-5f) || (RotSpeed < 0.0f && RotSpeed > -1.0e-5f))
        )
      {
        DefaultGameLogger.Instance.dumpLogError("[Projectile][doInitialize][" + gameObject.ToString() + "]: No Rotation Speed");
        
        return;
      }

      // check translate type
      if(TranslateTypes.TRANSLATE_TYPES_UNKNOWN == TransType)
      {
        DefaultGameLogger.Instance.dumpLogError("[Projectile][doInitialize][" + gameObject.ToString() + "]: No Translate Type");
        
        return;
      }

      // if there is some sort of local translate
      if(TranslateTypes.TRANSLATE_TYPES_NO != TransType)
      {
        // maximal translate != 0
       if((MaxTransDist > 0.0f && MaxTransDist < 1.0e-5f) 
          ||
          (MaxTransDist < 0.0f && MaxTransDist > -1.0e-5f)
         )
        {
          // ZERO MaxTransDist
          DefaultGameLogger.Instance.dumpLogError("[Projectile][doInitialize][" + gameObject.ToString() + "]: No Maximal Translate Distance");
          
          return;
        }

        if(TranslateTypes.TRANSLATE_TYPES_CHANGING == TransType)
        {
          if((TransSpeed > 0.0f && TransSpeed < 1.0e-5f) 
             ||
             (TransSpeed < 0.0f && TransSpeed > -1.0e-5f)
            )
          {
            // ZERO TransSpeed
            DefaultGameLogger.Instance.dumpLogError("[Projectile][doInitialize][" + gameObject.ToString() + "]: No Translate Speed");
            
            return;
          }

          if((MaxTransDist * TransSpeed) < 0.0f)
          {
            // different Sign of MaxTransDist and TransSpeed
            DefaultGameLogger.Instance.dumpLogError("[Projectile][doInitialize][" + gameObject.ToString() + "]: MaxTransDist and TransSpeed have Different Sign");
            
            return;
          }
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

      if(!isInUse)
      {
        return;
      }

      // check against maximal fly distance
      if(MaxAttackDist > 0.0f // if there is a maximal attack distance
         &&
         ((m_fCurFlyedDistance - MaxAttackDist) > 1.0e-5f)
        )
      {
        // done with the fly: destroy this projectile
        doDeadCom();

        return;
      }

      // calculate rotation
      Quaternion _qCurRot = m_qSpawnOrientation;

      // if there is some sort of local rotation: 
      // R_t0 * R_t
      // R_t0 is the initial orientation
      // R_t is the current local rotation
      if(RotType != RotationTypes.ROTATION_TYPES_NO)
      {
        // accumulate rotation angle
        m_fCurRotatedAngle_Degree += RotSpeed * Time.deltaTime;

        if(RotationTypes.ROTATION_TYPES_LOCAL_X == RotType)
        {
          _qCurRot = _qCurRot * Quaternion.AngleAxis(m_fCurRotatedAngle_Degree, Vector3.right);
        }

        else if(RotationTypes.ROTATION_TYPES_LOCAL_Y == RotType)
        {
          _qCurRot = _qCurRot * Quaternion.AngleAxis(m_fCurRotatedAngle_Degree, Vector3.up);
        }

        else if(RotationTypes.ROTATION_TYPES_LOCAL_Z == RotType)
        {
          _qCurRot = _qCurRot * Quaternion.AngleAxis(m_fCurRotatedAngle_Degree, Vector3.forward);
        }

        // adjust world rotation
        transform.rotation =  _qCurRot;
      }

      // calculate current world translate caused by local rightward translate
      Vector3 _vCurWorldTranslate = Vector3.zero;

      // if there is some sort of local translate
      if(TransType != TranslateTypes.TRANSLATE_TYPES_NO)
      {
        Vector3 _vCurLocalTranslate = Vector3.right * m_fEffectiveMaxTranslateDistance;

        // swing
        if(TranslateTypes.TRANSLATE_TYPES_CHANGING == TransType)
        {
          m_fCurLocalTranslateDistance += TransSpeed * Time.deltaTime;

          // moving left
          if(TransSpeed < 0.0f)
          {
            if(m_fCurLocalTranslateDistance < m_fEffectiveMaxTranslateDistance
               ||
               Mathf.Abs(m_fEffectiveMaxTranslateDistance - m_fCurLocalTranslateDistance) < 1.0e-5f
              )
            {
              // reached extreme
              m_fCurLocalTranslateDistance = m_fEffectiveMaxTranslateDistance;

              // revert the translate speed and maximal translate distance
              TransSpeed = -TransSpeed;
              m_fEffectiveMaxTranslateDistance = -m_fEffectiveMaxTranslateDistance;
            }
          }
          else
          {
            if(m_fCurLocalTranslateDistance > m_fEffectiveMaxTranslateDistance
               ||
               Mathf.Abs(m_fEffectiveMaxTranslateDistance - m_fCurLocalTranslateDistance) < 1.0e-5f
              )
            {
              // reached extreme
              m_fCurLocalTranslateDistance = m_fEffectiveMaxTranslateDistance;

              // revert the translate speed and maximal translate distance
              TransSpeed = -TransSpeed;
              m_fEffectiveMaxTranslateDistance = -m_fEffectiveMaxTranslateDistance;
            }
          }

          // calculate current local translate
          _vCurLocalTranslate = Vector3.right * m_fCurLocalTranslateDistance;
        }

        _vCurWorldTranslate = transform.TransformVector(_vCurLocalTranslate);
      }

      // fly along given forword direction
      m_fCurFlyedDistance += FlySpeed * Time.deltaTime;

      // calculate current expected position
      Vector3 _vExpectedPos = m_vSpawnTranslation + _vCurWorldTranslate + m_vFlyDirection * m_fCurFlyedDistance;

      // check if current projectile is a Fast Flying Projectile
      if(true == m_bIsFastFlyingProjecitle)
      {
        // we have to handle the collision by ourself: because unity seems have some trouble with fast moving object when it is NOT based on physic engine:
        // we just want to precisely control the position mathmatically

        // check if there is something in between

        // mask out the "Weapons" layer: ray hit check in all other layers
        // 1 bit means: Collide AGAINST that Layer
        int _iWeaponsLayer = 1 << LayerMask.NameToLayer("Weapons"); // NameToLayer() returns the layer INDEX
        int _iRestLayers = ~_iWeaponsLayer;

        // Raycast(origin, direction, hitInfo, maxCheckDistance, checkLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal);
        if(Physics.Raycast(transform.position, _vExpectedPos - transform.position, out m_pCurHitInfo, (_vExpectedPos - transform.position).magnitude, _iRestLayers, QueryTriggerInteraction.Collide))
        {
          // Hit something except weapons
          // move this projectile to the hit location before collision handling
          transform.position = m_pCurHitInfo.point;

          // handle the collision
          // call onCollision() of Collider Entity(ex. GroundFloor): 
          if(m_pCurHitInfo.collider.GetComponentInChildren< CollisionHandler >() == null)
          {
            DefaultGameLogger.Instance.dumpLogError("Projectile Hit [" + m_pCurHitInfo.collider.ToString() + "]: But it does NOT contain a CollisionHandler component");
          }
          else
          {
            Entity _pHitEntity = m_pCurHitInfo.collider.GetComponentInChildren< CollisionHandler >().TopMostNode.GetComponentInChildren< Entity >();
            _pHitEntity.onCollision(this, m_pCurHitInfo.collider.GetComponentInChildren< CollisionHandler >());
          }

          // move the object to real position: this projectile does NOT concert this kind of collision, it will continue flying
          transform.position = _vExpectedPos;
        }
        else
        {
          // NOTHING hit, move this projectile to real position
          transform.position = _vExpectedPos;
        }
      }
      else
      {
        // slow flying projectile
        transform.position = _vExpectedPos;
      }
    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    // initial translation
    protected Vector3 m_vSpawnTranslation;
    // current rotated angle in degree
    protected float m_fCurRotatedAngle_Degree;
    // effective maximal translate distance in effect: because there COULD be scaling, which will change the effective translate
    protected float m_fEffectiveMaxTranslateDistance;
    // current local translated distance
    protected float m_fCurLocalTranslateDistance;
    // fly direction
    protected Vector3 m_vFlyDirection;
    // current flyed distance
    protected float m_fCurFlyedDistance;

    // fast moving projectile related data
    // current hit info
    protected RaycastHit m_pCurHitInfo;
    // fast flying projectile flag
    protected bool m_bIsFastFlyingProjecitle;
    #endregion // PROTECTED DATA MEMBERS
  }
} // namespace Game.Logic

/**
 * Base Class of Beam Kind of Projectile
 * Each Beam has:
 * - FlySpeed: current flow speed: 0 means instantly reaching MaxAttackDist
 * - MaxAttackDist: the maixmal length this beam can be
 * - CurFlowDir: current beam flow direction: at start is same as the forward direction of its ShootPos
 * - CurAttackTarget: current entity under attack:
 *   + onCollision() will change this
 *   + periodically trace the hit part of that entity and call its onCollision() to induce damage: DamagePoints/Second
 */

using UnityEngine;
using Framework.Utils;
using Framework.Logger;

namespace Game.Logic
{
  public class Beam : Projectile
  {
    #region PUBLIC DATA MEMBERS
    // maximal beam width in world space: > 0
    public float BeamWidth = 1.0f;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    // set attack target
    public void setAttackTarget(Entity a_pAttackTarget)
    {
      // store the given attack target
      m_pCurAttackTarget = a_pAttackTarget;

      // adjust the beam length smoothly
      m_eCurLengthAdjustMode = LengthAdjustModes.LENGTH_ADJUST_MODES_SMOOTHLY;
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

      // this func sometime will be called multiple times
      // some attack target: return
      if(m_pCurAttackTarget == a_pTrigger)
      {
        return;
      }

      // record the hit entity
      m_pCurAttackTarget = a_pTrigger;

      // cause damage
      // when hit some entity, adjust the beam length instantly
      m_eCurLengthAdjustMode = LengthAdjustModes.LENGTH_ADJUST_MODES_INSTANTLY;

      // set count down timer to ONE second
      m_fCurDamageInduceTime = 1.0f;
    }

    /**
     * ExitCollision Handler for any Entity
     */
    public override void onExitCollision(Entity a_pTrigger,                    // who had triggered this Hit
                                         CollisionHandler a_pFormerCollidedPart// former collided part
                                        )
    {
      // if the exiting entity is the SAME as attacking entity
      if(m_pCurAttackTarget == a_pTrigger)
      {
        // remove the reference
        m_pCurAttackTarget = null;

        // clear count down timer
        m_fCurDamageInduceTime = 0.0f;

        // change length adjust mode
        m_eCurLengthAdjustMode = LengthAdjustModes.LENGTH_ADJUST_MODES_SMOOTHLY;
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
    public override void doUseCom()
    {
      base.doUseCom();

      // change length adjusting
      m_eCurLengthAdjustMode = LengthAdjustModes.LENGTH_ADJUST_MODES_SMOOTHLY;
    }

    /**
     * stop use
     */
    public override void doStopUseCom()
    {
      // change to no length adjust mode
      m_eCurLengthAdjustMode = LengthAdjustModes.LENGTH_ADJUST_MODES_NO;

      base.doStopUseCom();
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

      // trun off local rotation
      RotType = RotationTypes.ROTATION_TYPES_NO;
      RotSpeed = 0.0f;

      // trun off local translation
      TransType = TranslateTypes.TRANSLATE_TYPES_NO;
      TransSpeed = 0.0f;
      MaxTransDist = 0.0f;
    }

    // initialize this entity: called within doSpawnCom() only
    // m_bIsInitialized will be SET in this method
    protected override void doInitialize()
    {
      base.doInitialize();

      // clear initialization flag before checking
      m_bIsInitialized = false;

      m_vCurFlowDir = Vector3.forward;
      m_pCurAttackTarget = null;
      m_pCurCollider = null;
      m_pCurMesh = null;
      m_eCurLengthAdjustMode = LengthAdjustModes.LENGTH_ADJUST_MODES_NO;
      m_fCurDamageInduceTime = 0.0f;
      m_aVertexPosList = null;
      m_aTriangleList = null;

      // check beam length: > 0
      if(BeamWidth < 1.0e-5f)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "]: No Beam Length");

        return;
      }

      // get collider
      m_pCurCollider = null;
      Collider _pCurCollider = GetComponent< Collider >();

      // if there is some collider
      if(_pCurCollider != null)
      {
        // if the collider type is NOT MeshCollider: destroy it
        if(_pCurCollider.GetType() != typeof(MeshCollider))
        {
          DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "]: Collider of Beam MUST be MeshCollider");

          // destroy the wrong collider
          GameObject.Destroy(_pCurCollider);
        }
        else
        {
          m_pCurCollider = _pCurCollider as MeshCollider;
        }
      }

      // if there is NOT suitable collider: create a new one
      if(null == m_pCurCollider)
      {
        // create the mesh collider
        m_pCurCollider = gameObject.AddComponent< MeshCollider >();

        // set it as a convex mesh
        m_pCurCollider.convex = true;

        // set it as a trigger
        m_pCurCollider.isTrigger = true;
      }

      // create mesh
      m_pCurMesh = new Mesh();
      m_pCurMesh.name = "BeamMeshCollider";

      // assign mesh collider
      m_pCurCollider.sharedMesh= m_pCurMesh;

      // create Hit Info
      m_pCurHitInfo = new RaycastHit();

      // set initialization flag
      m_bIsInitialized = true;
    }

    /**
     * doUpdate() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last doUpdate() call
     * based on the current state do processing
     * check false == m_bIsInitialized before doing the real thing
     */
    // overwrite the Projectile's doUpdate(): NO that kind of MaxAttackDist checking and local rotation/translate
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

      // use m_pCurHitInfo to check the hit, if need
    }

    // adjust the beam current length
    protected virtual void adjustBeamLength()
    {
      // use m_pCurAttackTarget to adjust the beam length
    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    protected enum LengthAdjustModes
    {
      // no adjustment
      LENGTH_ADJUST_MODES_NO,
      // smoothly grow or shink this beam
      LENGTH_ADJUST_MODES_SMOOTHLY,
      // grow or shink the beam in the current frame
      LENGTH_ADJUST_MODES_INSTANTLY,
    }

    // current beam flow direction: at start is same as the forward direction of its ShootPos
    protected Vector3 m_vCurFlowDir;
    // current entity under attack
    protected Entity m_pCurAttackTarget;
    // current mesh collider
    protected MeshCollider m_pCurCollider;
    // current mesh
    protected Mesh m_pCurMesh;
    // current beam length adjust mode
    protected LengthAdjustModes m_eCurLengthAdjustMode;
    // current timer for inducing damage: always be ONE second
    protected float m_fCurDamageInduceTime;
    // position list of all vertex
    protected Vector3[] m_aVertexPosList;
    // triangle index list
    protected int[] m_aTriangleList;
    #endregion // PROTECTED DATA MEMBERS
  }

} // namespace Game.Logic

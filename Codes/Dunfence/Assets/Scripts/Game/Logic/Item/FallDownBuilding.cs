using UnityEngine;
using System; // Math.Abs
using Framework.Utils;
using Framework.Logger;

namespace Game.Logic
{
  public class FallDownBuilding : Building
  {
    #region PUBLIC DATA MEMBERS
    public delegate void FallDownCompletionHandler();

    // drop down height
    public float DropHeight = 1.0f;
    // drop down time until reaching the ground
    public float DropTime   = 1.5f;
    // initial drop down speed
    public float InitDropSpeed = 2.0f;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    // register a handler for the completion of the falling down
    // will unregister all handler, when this falldown building has finished falliing
    public void registerFallDownCompletionHandler(FallDownCompletionHandler a_pFallDownCompletionHandler)
    {
      m_pFallDownCompletionHandler += a_pFallDownCompletionHandler;
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
      // if the ground floor can NOT handle this collision: return;
      if(false == canHandleCollision(a_pTrigger, a_pCollidedPart))
      {
        return;
      }
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS

    #region PROTECTED OVERRIDDENS
    // initialize this entity: called within doSpawnCom() only
    // m_bIsInitialized will be SET in this method
    protected override void doInitialize()
    {
      base.doInitialize();

      // clear initialization flag before checking
      m_bIsInitialized = false;

      // sanity checking
      // check drop down height
      if(!(DropHeight > 0.0f))
      {
        DefaultGameLogger.Instance.dumpLogError("[FallDownBuilding][doInitialize][" + gameObject.ToString() + "]: No Drop Height");

        return;
      }

      // try to find HitGround Fx
      m_pHitGroundFx = GameObjectUtils.FindNode(gameObject, "HitGroundFx");

      // if we have fond the HitGroundFx, disable it
      if(m_pHitGroundFx != null)
      {
        m_pHitGroundFx.SetActive(false);
      }

      m_pFallDownCompletionHandler = null;

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
      // NOT initialized: return
      if(false == m_bIsInitialized)
      {
        return;
      }

      switch(m_eCurEntityState)
      {
      case EntityStates.ENTITY_STATES_SPAWNING:
        {
          // NOT simulating spawn
          if(false == m_bIsSimulatingSpawn)
          {
            return;
          }

          // accumulate time
          m_fCurAccumulatedTime += Time.deltaTime;

          // get current world space position
          Vector3 _vCurPosWs = gameObject.transform.position;

          // simulate dropping down for buildings: V0 * t - 1/2 * a * t*t
          float _fDroppedDistance = InitDropSpeed * m_fCurAccumulatedTime - 0.5f * m_fCurAccelerator * m_fCurAccumulatedTime * m_fCurAccumulatedTime;

          _vCurPosWs.y = DropHeight - _fDroppedDistance;

          // check if we have done with dropping
          if(_vCurPosWs.y < 0.001f) // less than 1cm
          {
            // reset Y
            _vCurPosWs.y = 0.0f;

            // play HitGroundFx if it does exist
            if(m_pHitGroundFx != null)
            {
              m_pHitGroundFx.SetActive(true);
            }

            // clear flag
            m_bIsSimulatingSpawn = false;

            // invoke spawn done handler
            if(null != m_pFallDownCompletionHandler)
            {
              // invoke handler
              m_pFallDownCompletionHandler();

              // reset handler
              m_pFallDownCompletionHandler = null;
            }

            // change to idle state
            m_eCurEntityState = EntityStates.ENTITY_STATES_IDLE;
          }

          // drop down building
          gameObject.transform.position = _vCurPosWs;

          return;
        }
      default:
        {
          return;
        }
      }
    }

    // simulate the spawn animation
    protected override void doSpawnAnimation()
    {
      base.doSpawnAnimation();

      // calculate accelerator used for this falling down
      // Height = V0 * DropTime - 0.5 * a * DropTime * DropTime
      // a = 2 * (V0 * DropTime - Height)/(DropTime * DropTime)
      m_fCurAccelerator = 2.0f * (InitDropSpeed * DropTime - DropHeight) / (DropTime * DropTime);

      // drag the building to the given height
      gameObject.transform.position = new Vector3(gameObject.transform.position.x, DropHeight, gameObject.transform.position.z);

      // clear accumlated time
      m_fCurAccumulatedTime = 0.0f;
    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    // current accelerator
    protected float m_fCurAccelerator;
    // current accumulated time
    protected float m_fCurAccumulatedTime;
    // falling down completion handler
    protected FallDownCompletionHandler m_pFallDownCompletionHandler;
    // hit ground smoke fx
    protected GameObject m_pHitGroundFx;
    #endregion // PROTECTED DATA MEMBERS
  }

} // namespace Game.Logic

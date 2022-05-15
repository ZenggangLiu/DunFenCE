/**
 * Class used for Fire Breath
 */

using UnityEngine;

namespace Game.Logic
{
  public class FireBreath : Beam
  {
    #region PROTECTED METHODS

    #region PROTECTED OVERRIDDENS
    // initialize this entity: called within doSpawnCom() only
    // m_bIsInitialized will be SET in this method
    protected override void doInitialize()
    {
      base.doInitialize();

      // clear initialization flag before checking
      m_bIsInitialized = false;

      // create vertex position list
      m_aVertexPosList = new Vector3[8];

      // create triangle index list
      m_aTriangleList = new int[12 * 3]; // 12 triangles

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

      // adjust beam length
      switch(m_eCurLengthAdjustMode)
      {
        case LengthAdjustModes.LENGTH_ADJUST_MODES_SMOOTHLY:
        case LengthAdjustModes.LENGTH_ADJUST_MODES_INSTANTLY:
        {
          // adjust the beam length instantly
          adjustBeamLength();

          // change to no length adjustment
          m_eCurLengthAdjustMode = LengthAdjustModes.LENGTH_ADJUST_MODES_NO;

          return;
        }

        default:
        {
          break;
        }
      }

      // count down timer is over: induce damage
      if(m_fCurDamageInduceTime < 1.0e-5f)
      {
        // if there is an entity under attack
        if(m_pCurAttackTarget != null)
        {
          // if the hit attack target is dead
          if(true == m_pCurAttackTarget.IsDead)
          {
#if !RETAIL
            //DefaultGameLogger.Instance.dumpLogInfo("[doUpdate][" + gameObject.ToString() + "]: Attacked Entity Dead:" + m_pCurAttackTarget.gameObject.ToString());
#endif

            // clear the reference to attack target
            m_pCurAttackTarget = null;

            // grow the beam further
            m_eCurLengthAdjustMode = LengthAdjustModes.LENGTH_ADJUST_MODES_SMOOTHLY;
          }
          else
          {
            // the attacked target is still alive
            // hit check
            // mask out the "Weapons" layer: ray hit check in all other layers
            // 1 bit means: Collide AGAINST that Layer
            int _iIgnoredLayers = 1 << LayerMask.NameToLayer("Weapons"); // ignore Weapons layer: NameToLayer() returns the layer's index

            // ignore characters of the same side
            _iIgnoredLayers |= (CharacterTypes.CHARACTER_TYPES_BOSS == OwnerType || CharacterTypes.CHARACTER_TYPES_MONSTER == OwnerType) ? 
                               (1 << LayerMask.NameToLayer("Monsters")) : (1 << LayerMask.NameToLayer("Heros"));

            int _iCheckLayers = ~_iIgnoredLayers;
            _iCheckLayers |= 1 << LayerMask.NameToLayer("Buildings"); // add Buildings layer

            // Raycast(origin, direction, hitInfo, maxCheckDistance, checkLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal);
            if(Physics.Raycast(transform.position, transform.forward, out m_pCurHitInfo, MaxAttackDist, _iCheckLayers, QueryTriggerInteraction.Collide))
            {
              // Hit something
              // hit a different entity
              if(m_pCurHitInfo.collider.GetComponentInChildren< CollisionHandler>().TopMostNode.GetComponentInChildren< Entity >() != m_pCurAttackTarget)
              {
                // store the entity
                m_pCurAttackTarget = m_pCurHitInfo.collider.GetComponentInChildren< CollisionHandler>().TopMostNode.GetComponentInChildren< Entity >();

                // adjust the beam length instantly
                adjustBeamLength();
              }

              // induce damage
#if !RETAIL
              //DefaultGameLogger.Instance.dumpLogInfo("[" + gameObject.ToString() + "]: induced damage onto: " + m_pCurAttackTarget.gameObject.ToString() + "onto part: " + m_pCurHitInfo.collider.GetComponentInChildren< CollisionHandler >().Type.ToString());
#endif

              m_pCurAttackTarget.handleCollision(this, m_pCurHitInfo.collider.GetComponentInChildren< CollisionHandler >());

              // set the count down timer
              m_fCurDamageInduceTime = 1.0f;
            }
            else
            {
              // nothing hit anymore: go further
              m_eCurLengthAdjustMode = LengthAdjustModes.LENGTH_ADJUST_MODES_SMOOTHLY;
            }
          }
        }
      }
      else
      {
        m_fCurDamageInduceTime -= Time.deltaTime;
      }
    }

    // adjust the beam current length
    protected override void adjustBeamLength()
    {
      // get the maiximal attack distance in local space
      // get world to local space scaling factor
      float _fWorldToLocalScaling = transform.InverseTransformVector(Vector3.forward).magnitude;
      float _fMaxAttackDistLS = MaxAttackDist * _fWorldToLocalScaling;

      // get the expected beam length in local space
      float _fExpectedAttackDistLS = 0.0f;

      // if there is an attacked entity: get the distance to that entity in local space
      if(m_pCurAttackTarget != null)
      {
        _fExpectedAttackDistLS = (m_pCurAttackTarget.transform.position - transform.position).magnitude * _fWorldToLocalScaling;
      }
      else
      {
        // otherwise, we use the maximal attack distance in local space
        _fExpectedAttackDistLS = _fMaxAttackDistLS;
      }

      // check if the beam is already in that expected length
      if((_fExpectedAttackDistLS - m_fCurFlyedDistance) < 1.0e-5f 
         &&
         (_fExpectedAttackDistLS - m_fCurFlyedDistance) > -1.0e-5f 
        )
      {
        // not length change
#if !RETAIL
        //DefaultGameLogger.Instance.dumpLogInfo("[adjustBeamLength][" + gameObject.ToString() + "]: Expected Attack Distance reached: " + (_fExpectedAttackDistLS / _fWorldToLocalScaling));
#endif

        return;
      }

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[adjustBeamLength][" + gameObject.ToString() + "]: Length Adjustment: " + (m_fCurFlyedDistance / _fWorldToLocalScaling) + "--> " + (_fExpectedAttackDistLS / _fWorldToLocalScaling));
#endif

      // we need to adjust the current beam length
      m_fCurFlyedDistance = _fExpectedAttackDistLS;

      // re-create the bounding box
      /**   5 +--------+ 6
       *     /|       / |
       *    / |      /  |
       * 1 +--------+ 2 / 7
       *   |  4     |  /
       *   |        | /
       *   +--------+/
       *   0        3
       */
      // get beam width in local space
      float _fBeamWidthLS = BeamWidth * _fWorldToLocalScaling;

      // vertex list: the normal at each vertex is the average of normals of its connected 3 planes
      m_aVertexPosList[0] = new Vector3(-0.5f * _fBeamWidthLS, -0.5f * _fBeamWidthLS, 0.0f);
      m_aVertexPosList[1] = new Vector3(-0.5f * _fBeamWidthLS,  0.5f * _fBeamWidthLS, 0.0f);
      m_aVertexPosList[2] = new Vector3( 0.5f * _fBeamWidthLS,  0.5f * _fBeamWidthLS, 0.0f);
      m_aVertexPosList[3] = new Vector3( 0.5f * _fBeamWidthLS, -0.5f * _fBeamWidthLS, 0.0f);

      m_aVertexPosList[4] = new Vector3(-0.5f * _fBeamWidthLS, -0.5f * _fBeamWidthLS, m_fCurFlyedDistance);
      m_aVertexPosList[5] = new Vector3(-0.5f * _fBeamWidthLS,  0.5f * _fBeamWidthLS, m_fCurFlyedDistance);
      m_aVertexPosList[6] = new Vector3( 0.5f * _fBeamWidthLS,  0.5f * _fBeamWidthLS, m_fCurFlyedDistance);
      m_aVertexPosList[7] = new Vector3( 0.5f * _fBeamWidthLS, -0.5f * _fBeamWidthLS, m_fCurFlyedDistance);

      // triangle list: Clockwise winding order
      /**
       * 0, 1, 2
       * 2, 3, 0
       * 3, 2, 6
       * 6, 7, 3
       * 7, 6, 4
       * 4, 6, 5
       * 4, 5, 1
       * 1, 0, 4
       * 1, 5, 2
       * 2, 5, 6
       * 3, 7, 4
       * 4, 0 ,3
       */
      // 0, 1, 2
      m_aTriangleList[0] = 0;
      m_aTriangleList[1] = 1;
      m_aTriangleList[2] = 2;

      // 2, 3, 0
      m_aTriangleList[3] = 2;
      m_aTriangleList[4] = 3;
      m_aTriangleList[5] = 0;

      // 3, 2, 6
      m_aTriangleList[6] = 3;
      m_aTriangleList[7] = 2;
      m_aTriangleList[8] = 6;

      // 6, 7, 3
      m_aTriangleList[9]  = 6;
      m_aTriangleList[10] = 7;
      m_aTriangleList[11] = 3;

      // 7, 6, 4
      m_aTriangleList[12] = 7;
      m_aTriangleList[13] = 6;
      m_aTriangleList[14] = 4;

      // 4, 6, 5
      m_aTriangleList[15] = 4;
      m_aTriangleList[16] = 6;
      m_aTriangleList[17] = 5;

      // 4, 5, 1
      m_aTriangleList[18] = 4;
      m_aTriangleList[19] = 5;
      m_aTriangleList[20] = 1;

      // 1, 0, 4
      m_aTriangleList[21] = 1;
      m_aTriangleList[22] = 0;
      m_aTriangleList[23] = 4;

      // 1, 5, 2
      m_aTriangleList[24] = 1;
      m_aTriangleList[25] = 5;
      m_aTriangleList[26] = 2;

      // 2, 5, 6
      m_aTriangleList[27] = 2;
      m_aTriangleList[28] = 5;
      m_aTriangleList[29] = 6;

      // 3, 7, 4
      m_aTriangleList[30] = 3;
      m_aTriangleList[31] = 7;
      m_aTriangleList[32] = 4;

      // 4, 0 ,3
      m_aTriangleList[33] = 4;
      m_aTriangleList[34] = 0;
      m_aTriangleList[35] = 3;

      // change the mesh data
      m_pCurMesh.vertices = m_aVertexPosList;
      m_pCurMesh.triangles = m_aTriangleList;
    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS
  }

} // namespace Game.Logic

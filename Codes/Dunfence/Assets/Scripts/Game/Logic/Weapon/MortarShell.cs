/**
 * Shell of Mortar
 * - NOTE: we can NOT set the speed of shell to a very high value: in the AR Game, it can NOT be larger than 0.15. 
 *         Otherwise, the Fly Time is TOO short to make the spline a smooth curve: JAGGY
 * adjust the spline with 
 */

// define this if we want to draw the curve
#define DRAW_CURVE

using UnityEngine;
using Framework.Mathe;  // spline
using Framework.Utils;
using Framework.Logger;

namespace Game.Logic
{
  public class MortarShell : Projectile
  {
    #region PUBLIC DATA MEMBERS
    // control point color
    public Color CtrlPointColor = Color.magenta;
    // curve color
    public Color CurveColor = Color.green;
    // control point radius in meter
    public float CtrlPointRadius = 0.005f; // 0.5cm
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    /**
     * set shoot target
     * MUST be called before spawning this mortar shell
     */
    public void setShootTarget(Entity a_pShootTarget)
    {
      // no shoot target given: return
      if(null == a_pShootTarget)
      {
        m_pShootOrbit = null;

        return;
      }

      // shoot vector
      Vector3 _vShootVector = a_pShootTarget.transform.position - transform.position;

      // shoot distance
      float _fShootDist = _vShootVector.magnitude;

      // normalize the shoot vector
      _vShootVector.Normalize();

      // store shoot time
      UseTime = _fShootDist / FlySpeed;

      // calcuate the apex control point: 84% along the whole way
      //Vector3 _vApex = transform.position + (_fShootDist * 0.84f) * _vShootVector;
      Vector3 _vApex = transform.position + (_fShootDist * 0.5f) * _vShootVector;

      // shift Apex 8cm above
      _vApex.y += 0.08f;

      // create control point list
      CtrlPointInfo[] _aCtrlPointInfoList = new CtrlPointInfo[3];

      _aCtrlPointInfoList[0].Location = transform.position;
      _aCtrlPointInfoList[0].Time = 0.0f;

      _aCtrlPointInfoList[1].Location = _vApex;
      _aCtrlPointInfoList[1].Time = UseTime * 0.5f;

      _aCtrlPointInfoList[2].Location = a_pShootTarget.transform.position;
      _aCtrlPointInfoList[2].Time = UseTime;

      // create shoot orbit
      m_pShootOrbit = new Spline();
      m_pShootOrbit.createSpline(_aCtrlPointInfoList, 0.0f);
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
      // explosion effect
      EffectMgr.Instance.spawnEffect(384, transform.position);

      // handle this collision
      base.onCollision(a_pTrigger, a_pCollidedPart);
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS

    #region PROTECTED OVERRIDDENS
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

      // if no shoot orbit: use base's implementation
      if(null == m_pShootOrbit)
      {
        base.doUpdate();

        return;
      }

      // there is some orbit
      // NOT in usage: return
      if(!isInUse)
      {
        return;
      }

      // finish shooting: destroy this projectile
      if(m_fCurUseTime > UseTime)
      {
        doDeadCom();

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

      // increase shoot time
      m_fCurUseTime += Time.deltaTime;

      // get the current location on shoot orbit
      transform.position = m_pShootOrbit.getCurvePoint(m_fCurUseTime);

      // calculate current flyed distance
      m_fCurFlyedDistance = Vector3.Distance(transform.position, m_vSpawnTranslation);

      // get the current orientation on shoot orbit
      transform.rotation = Quaternion.LookRotation(m_pShootOrbit.getCurveTangent(m_fCurUseTime));
    }

    // called after doSpawnCom() has done
    protected override void onSpawnComDone()
    {
      base.onSpawnComDone();

      // set shoot time
      m_fCurUseTime = 0.0f;
    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS

    #region PRIVATE METHODS
    /**
     * OnDrawGizmos() will be called each frame to draw a Gizoms for the script component
     */
    void OnDrawGizmos()
    {
#if DRAW_CURVE

#if UNITY_EDITOR
      if(m_pShootOrbit != null)
      {
        // set control point color
        Gizmos.color = CtrlPointColor;

        // draw control points
        for(int _iIdx = 0; _iIdx < m_pShootOrbit.CurrentJointCount; _iIdx++)
        {
          Gizmos.DrawSphere(m_pShootOrbit.CurrentJointList.Joints[_iIdx].Location, CtrlPointRadius);
        }

        // draw spline curve
        // set curve color
        Gizmos.color = CurveColor;

        // get the maximal shoot time
        float _fShootTime = m_pShootOrbit.CurrentJointList.Joints[m_pShootOrbit.CurrentJointCount - 1].Time;

        // total curve points
        int _iTotalCurvePointCount = (int)(_fShootTime * 20.0f);

        Vector3 _vPrevPos = m_pShootOrbit.CurrentJointList.Joints[0].Location;

        for(int _iPIdx = 1; _iPIdx < _iTotalCurvePointCount; _iPIdx++)
        {
          Vector3 _vCurPos = m_pShootOrbit.getCurvePoint((float)_iPIdx / 20.0f);

#if !RETAIL
          //DefaultGameLogger.Instance.dumpLogInfo("Point[" + _iPIdx + "]: " + _vCurPos);
#endif
  
          Gizmos.DrawLine(_vPrevPos, _vCurPos);

          _vPrevPos = _vCurPos;
        }
      }
#endif // #if UNITY_EDITOR

#endif // #if DRAW_CURVE
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // the flying orbit of the shell
    Spline m_pShootOrbit;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Logic

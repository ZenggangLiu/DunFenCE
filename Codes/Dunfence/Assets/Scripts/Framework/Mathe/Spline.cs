/**
 * Spline Interface
 * Bezier Spline can NOT go through all control points of each segment except the first (end) control point and last (end) control point
 * So we are using Hermite Spline internally.
 * 
 * But Hermite Spline will need an explicit tangent for each control point, so we are using Cardinal Spline to create an the curve on behaviour of hermite
 * 
 * - create a NON-UNIFORM cardinal spline with the given tension c: using cardinal spline interface
 * - get any curve position with the given spline time: [0, +infinite]: using hermite spline interface
 * - get the tangent of any curve position with the given spline time: [0, +infinite]: using hermite spline interface
 * - adjust any curve position's location: using hermite spline interface
 * - adjust the tangent of any curve position: using hermite spline interface
 */

using UnityEngine;

namespace Framework.Mathe
{
  // control point info: its location and its spline time in second
  public struct CtrlPointInfo
  {
    // control point's location
    public Vector3 Location;
    // control point's spline time
    public float Time;
  }

  // joint point of the spline
  public struct Joint
  {
    // joint's location
    public Vector3 Location;
    // joint's tangent
    public Vector3 Tangent;
    // joint's spline time
    public float Time;
  }

  // joint point list
  public struct JointList
  {
    public Joint[] Joints;
  }

  // how to sample the curve
  public enum CurveSamplingModes
  {
    // if curve has time span: [0, n]
    // [-infinit, 0] --> 0, [n, +infinit] -->n
    CURVE_SAMPLING_MODES_CAPPED,
    // [-infinit, 0] --> 0, [n, +infinit] --> t%n, t is the current spline time
    CURVE_SAMPLING_MODES_REPEATED,
  }

  public class Spline
  {
    #region PUBLIC METHODS
    /**
     * return the joint point count
     */
    public int CurrentJointCount
    {
      get
      {
        if(null == m_aJointList.Joints)
        {
          return 0;
        }

        return m_aJointList.Joints.Length;
      }
    }

    /**
     * return the whole joint list
     */
    public JointList CurrentJointList
    {
      get
      {
        return m_aJointList;
      }
    }

    /**
     * create a new spline using cardinal spline interface
     * \param a_aCtrlPointInfoList: all control points of the spline curve
     * \param a_fTension: tension, value in [0, 1]
     *                    - 1: straigh line
     *                    - 0: CatMull Spline
     * return true on success
     */
    public bool createSpline(CtrlPointInfo[] a_aCtrlPointInfoList, 
                             float a_fTension // [0, 1]
                            )
    {
      return CardinalSpline.generateJointList(a_aCtrlPointInfoList, a_fTension, out m_aJointList);
    }

    /**
     * return the position of a curve point on the given spline time in second: [0, + infinit]
     * \param a_fCurSplineTime: current spline time in second
     * \param a_eSamplingMode: curve sampling mode
     */
    public Vector3 getCurvePoint(float a_fCurSplineTime, CurveSamplingModes a_eSamplingMode = CurveSamplingModes.CURVE_SAMPLING_MODES_CAPPED)
    {
      return CardinalSpline.getCurvePoint(m_aJointList, a_fCurSplineTime, a_eSamplingMode);
    }

    /**
     * return the tangent of a curve point on the given spline time in second: [0, + infinit]
     * \param a_fCurSplineTime: current spline time in second
     * \param a_eSamplingMode: curve sampling mode
     */
    public Vector3 getCurveTangent(float a_fCurSplineTime, CurveSamplingModes a_eSamplingMode = CurveSamplingModes.CURVE_SAMPLING_MODES_CAPPED)
    {
      return CardinalSpline.getCurveTangent(m_aJointList, a_fCurSplineTime, a_eSamplingMode);
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE DATA MEMBERS
    // joint point list of this spline
    JointList m_aJointList;
    #endregion // PRIVATE DATA MEMBERS
  }
} // namespace Framework.Mathe

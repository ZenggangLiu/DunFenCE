/*
 * Cardinal Spline
 * 
 * Features: Cardinal Spline and CatMull Spline(tension == 0)
 * 1. P0, P1: ALWAYS be a straight line, c does NOT matter here
 * 2. P0, P1, P2: like parabola, c does matter here. c == 1, line segment
 * 
 * Usage:
 * 1. call CardinalSpline.generateJointList(), provide the whole end points and a Tension defined as following.
 *    the Time Interval will be the time length for each curve segment
 * 2. call CardinalSpline.getCurvePoint(), provide the current time to get the curve point
 * 
 * Implementation of Cardinal Spline: Cubic Hermite Curve with automatical Tangent Vector Generation for each Control Point Pk, with k: [0, n-1]
 * For First Joint: (1-c)*(P1 - P0)/2*(T1 - T0)
 * For Last  Joint: (1-c)*(Pn-1 - Pn-2)/2*(Tn-1 - Tn-2)
 * For Rest  Joint: (1-c)*(Pk+1 - Pk-1)/2*(Tk+1 - Tk-1)
 * with c is the Tension:
 * - 1: all staight lines
 * - 0: CatMull Spline
 */

using UnityEngine;
using Framework.Logger;

namespace Framework.Mathe
{
  internal class CardinalSpline
  {
    /**
     * generate a joint list for the given control/end point list
     * \param a_fTension: tension, value in [0, 1]
     *                    - 1: straigh line
     *                    - 0: CatMull Spline
     * return true on success
     */
    public static bool generateJointList(CtrlPointInfo[] a_aCtrlPointInfoList, 
                                         float a_fTension,         // [0, 1]
                                         out JointList a_pJointList                                         
                                        )
    {
      // create joint list
      a_pJointList = new JointList();

      // we need at least TWO control/end points
      if(a_aCtrlPointInfoList.Length < 2)
      {
        return false;
      }

      // create joints
      a_pJointList.Joints = new Joint[a_aCtrlPointInfoList.Length];

      // the tangent of the current curve control/end point
      Vector3 _pCurTangle;

      // iterate over all control/end points of the given list
      for(int _iIdx = 0; _iIdx < a_pJointList.Joints.Length; ++_iIdx)
      {
        // copy the given location and spline time of each control/end point
        // store the control/end point location
        a_pJointList.Joints[_iIdx].Location = a_aCtrlPointInfoList[_iIdx].Location;

        // store the control/end point's spline time
        a_pJointList.Joints[_iIdx].Time = a_aCtrlPointInfoList[_iIdx].Time;

        /**
         *calculate tangent of Pk
         * with k: [0, n-1]
         * First Joint: (1-c)*(P1 - P0)/2*(T1 - T0)
         * Last  Joint: (1-c)*(Pn-1 - Pn-2)/2*(Tn-1 - Tn-2)
         * Rest  Joint: (1-c)*(Pk+1 - Pk-1)/2*(Tk+1 - Tk-1)
         */
        // for first end point: (P1 - P0)/(T1 - T0)
        if(0 == _iIdx)
        {
          // calculate the time span of the first segment: T1 - T0
          float _fCurTimeSpan = a_aCtrlPointInfoList[1].Time - a_aCtrlPointInfoList[0].Time;

          // too tiny time span
          if(_fCurTimeSpan < 1.0e-5f)
          {
            DefaultGameLogger.Instance.dumpLogError("Too tiny Time Span for Segment: " + _iIdx);

            _fCurTimeSpan = 1.0e-5f;
          }

          _pCurTangle = (a_aCtrlPointInfoList[1].Location - a_aCtrlPointInfoList[0].Location) / _fCurTimeSpan;
        }
        else
        {
          // for last end point: (Pn-1 - Pn-2)/(Tn-1 - Tn-2)
          if(_iIdx == a_aCtrlPointInfoList.Length - 1)
          {
            // calculate the time span of the last segment: Tn-1 - Tn-2
            float _fCurTimeSpan = a_aCtrlPointInfoList[_iIdx].Time - a_aCtrlPointInfoList[_iIdx - 1].Time;

            // too tiny time span
            if(_fCurTimeSpan < 1.0e-5f)
            {
              DefaultGameLogger.Instance.dumpLogError("Too tiny Time Span for Segment: " + _iIdx);

              _fCurTimeSpan = 1.0e-5f;
            }

            _pCurTangle = (a_aCtrlPointInfoList[_iIdx].Location - a_aCtrlPointInfoList[_iIdx - 1].Location) / _fCurTimeSpan;
          }
          else
          {
            // internal end points: (Pk+1 - Pk_1)/(Tk+1 - Tk-1)
            // calculate the time span of internal segment: Tk+1 - Tk-1
            float _fCurTimeSpan = a_aCtrlPointInfoList[_iIdx + 1].Time - a_aCtrlPointInfoList[_iIdx -1].Time;

            // too tiny time span
            if(_fCurTimeSpan < 1.0e-5f)
            {
              DefaultGameLogger.Instance.dumpLogError("Too tiny Time Span for Segment: " + _iIdx);

              _fCurTimeSpan = 1.0e-5f;
            }

            _pCurTangle = (a_aCtrlPointInfoList[_iIdx + 1].Location - a_aCtrlPointInfoList[_iIdx - 1].Location) / _fCurTimeSpan;
          }
        }

        // tangent := (1-tension)/2 * CurTangent
        _pCurTangle *= 0.5f * (1.0f - a_fTension);
        
        // store the control/end point tangent
        a_pJointList.Joints[_iIdx].Tangent = _pCurTangle;
      }
      
      return true;
    }

    /**
     * return the position of a curve point at the given spline time in second: [0, + infinit]
     */
    public static Vector3 getCurvePoint(JointList a_pJointList, float a_fCurSplineTime, CurveSamplingModes a_eSamplingMode)
    {
      return HermiteSpline.getCurvePoint(a_pJointList, a_fCurSplineTime, a_eSamplingMode);
    }

    /**
     * return the tangent of a curve point at the given spline time in second:  [0, + infinit]
     */
    public static Vector3 getCurveTangent(JointList a_pJointList, float a_fCurSplineTime, CurveSamplingModes a_eSamplingMode)
    {
      return HermiteSpline.getCurveTangent(a_pJointList, a_fCurSplineTime, a_eSamplingMode);
    }
  }
} // namespace Framework.Mathe

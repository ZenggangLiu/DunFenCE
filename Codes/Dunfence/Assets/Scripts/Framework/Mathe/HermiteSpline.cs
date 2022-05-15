/**
 * Hermite Spline: https://en.wikipedia.org/wiki/Cubic_Hermite_spline
 * 
 * watch all equations with http://www.codecogs.com/latex/eqneditor.php
 * Curve Points defined as: \vec{P}(t) = (2u^{3} - 3u^{2} + 1)\vec{P}_{k} + (u^{3}-2u^{2}+u)(t_{k+1}-t_{k})\vec{m}_{k} +(-2u^{3}+3u^{2})\vec{P}_{k+1} + (u^{3}-u^{2})(t_{k+1}-t_{k})\vec{m}_{k+1}, with, u=\frac{(t-t_{k})}{t_{k+1}-t_{k}}
 * where t is the time given: a_fCurrentTime: [0, +infinite]
 *       u is the normalized time: [0, 1]: (a_fCurrentTime - Uk)/(Uk+1 - Uk)
 *       m_k is the tangent at kth point P_k
 * Curve Tangents defined as: \frac{\mathrm{d} \vec{P}(t)}{\mathrm{d} t} = \frac{\mathrm{d} \vec{P}(t)}{\mathrm{d} u}* \frac{\mathrm{d} u}{\mathrm{d} t} = [(6u^{2}-6u)\vec{P}_{k} + (3u^{2}-4u+1)(t_{k+1}-t_{k})\vec{m}_{k} +(-6u^{2}+6u)\vec{P}_{k+1}+(3u^{2}-2u)(t_{k+1}-t_{k})\vec{m}_{k+1}]*\frac{1}{t_{k+1}-t_{k}}
 * Pay attention to that factor: 1/(Tk+1 - Tk), which counts for the du/dt
 * more reference: http://math.stackexchange.com/questions/843595/how-can-i-calculate-the-derivative-of-a-catmull-rom-spline-with-nonuniform-param
 */

using UnityEngine;

namespace Framework.Mathe
{
  internal class HermiteSpline
  {
    /**
     * return the position of a curve point on the given spline time in second: [0, + infinit]
     * \param a_fCurSplineTime: current spline time in second
     * \param a_eSamplingMode: curve sampling mode
     */
    public static Vector3 getCurvePoint(JointList a_pJointList, float a_fCurSplineTime /* [0, + infinit] */, CurveSamplingModes a_eSamplingMode)
    {
      // calcuate the spline time used within this operation
      float _fEffectiveSplineTime = getEffectiveSplineTime(a_pJointList, a_fCurSplineTime, a_eSamplingMode);

      // get the curve point P(t)
      // select the joint point Pk, which is locating before the given time
      int _iK = -1; // k

      // iterate over all joint points
      for(int _iIdx = 0; _iIdx < a_pJointList.Joints.Length; _iIdx++)
      {
        // if the given spline time is ealier than the current joint's time
        if(_fEffectiveSplineTime < a_pJointList.Joints[_iIdx].Time)
        {
          _iK = _iIdx - 1;

          break;
        }
      }

      // if we end up to use the last joint point as Pk or beyond the last joint point, we use Pk_1
      if(_iK < 0.0f 
         ||
         _iK >= a_pJointList.Joints.Length - 1
        )
      {
        _iK = a_pJointList.Joints.Length - 2;
      }

      // (Tk+1 - Tk)
      float _fCurTimeSpan = (a_pJointList.Joints[_iK + 1].Time - a_pJointList.Joints[_iK].Time);

      // calculate H
      // the relative time u: [0, 1]: (T - Tk)/(Tk+1 - Tk)
      float _fU = (_fEffectiveSplineTime - a_pJointList.Joints[_iK].Time) / _fCurTimeSpan; // u
      
      // check validity of u <- [0, 1]
      if(_fU < 0.0f)
      {
        _fU = 0.0f;
      }
      else
      {
        if(_fU > 1.0f)
        {
          _fU = 1.0f;
        }
      }
      
      // H00 = (1+2u)(1-u)(1-u) = 2*u*u*u -3*u*u +1
      float _fH00 = ( 1 + 2 * _fU) * (1 - _fU) * (1 - _fU);
      // H10 = u(1-u)(1-u) = u*u*u -2*u*u + u
      float _fH10 = _fU * (1 - _fU) * (1 - _fU);
      // H01 = u*u*(3-2u) = -2*u*u*u + 3*u*u
      float _fH01 = _fU * _fU * (3 - 2 * _fU);
      // H11 = u*u*(u-1) = u*u*u - u*u
      float _fH11 = _fU * _fU * ( _fU - 1);
      
      // P(t) = H00(u)Pk + H10(u)(Tk+1 - Tk)Mk + H01(u)Pk+1 + H11(u)(Tk+1 - Tk)Mk+1
      // T is the given time, u is the normalized time
      Vector3 _vP_T = _fH00 * a_pJointList.Joints[_iK].Location 
                    +
                      _fH10 * _fCurTimeSpan * a_pJointList.Joints[_iK].Tangent
                    +
                      _fH01 * a_pJointList.Joints[_iK + 1].Location
                    + 
                      _fH11 * _fCurTimeSpan * a_pJointList.Joints[_iK + 1].Tangent;
      
      return _vP_T;
    }

    /**
     * return the tangent of a curve point on the given spline time in second: [0, + infinit]
     * \param a_fCurSplineTime: current spline time in second
     * \param a_eSamplingMode: curve sampling mode
     */
    public static Vector3 getCurveTangent(JointList a_pJointList, float a_fCurSplineTime, CurveSamplingModes a_eSamplingMode)
    {
      // calcuate the spline time used within this operation
      float _fEffectiveSplineTime = getEffectiveSplineTime(a_pJointList, a_fCurSplineTime, a_eSamplingMode);

      // get the tangent P'(t)
      // select the joint point Pk, which is locating before the given time
      int _iK = 0; // k

      // iterate over all joint points
      for(int _iIdx = 0; _iIdx < a_pJointList.Joints.Length; _iIdx++)
      {
        // if the given spline time is ealier than the current joint's time
        if(_fEffectiveSplineTime < a_pJointList.Joints[_iIdx].Time)
        {
          _iK = _iIdx - 1;

          break;
        }
      }

      // if we end up to use the last joint point as Pk or beyond the last joint point, we use Pk_1
      if(_iK < 0.0f 
         ||
         _iK >= a_pJointList.Joints.Length - 1
        )
      {
        _iK = a_pJointList.Joints.Length - 2;
      }

      // (Tk+1 - Tk)
      float _fCurTimeSpan = (a_pJointList.Joints[_iK + 1].Time - a_pJointList.Joints[_iK].Time);

      // calculate H'
      // the relative time u: [0, 1]
      float _fU = (_fEffectiveSplineTime - a_pJointList.Joints[_iK].Time) / _fCurTimeSpan; // u

      // check validity of u <- [0, 1]
      if(_fU < 0.0f)
      {
        _fU = 0.0f;
      }
      else
      {
        if(_fU > 1.0f)
        {
          _fU = 1.0f;
        }
      }
      
      // H'00 = 6*u*u - 6*u = (2*u*u*u -3*u*u +1)'
      float _fH00_Prime = 6.0f * _fU * _fU - 6.0f * _fU;
      // H'10 = 3*u*u - 4*u + 1 = (u*u*u -2*u*u + u)'
      float _fH10_Prime = 3.0f * _fU * _fU - 4.0f * _fU + 1.0f;
      // H'01 = -6*u*u* + 6*u = (-2*u*u*u + 3*u*u)'
      float _fH01_Prime = -6.0f * _fU * _fU + 6.0f * _fU;
      // H'11 = 3*u*u - 2*u = (u*u*u - u*u)'
      float _fH11_Prime = 3.0f * _fU * _fU - 2.0f * _fU;
      
      // P'(t) = [H'00(u)Pk + H'10(u)(Tk+1 - Tk)Mk + H01'(u)Pk+1 + H11'(u)(Tk+1 - Tk)Mk+1] * 1/(Tk+1 - Tk)
      Vector3 _vP_T_Prime = _fH00_Prime * a_pJointList.Joints[_iK].Location 
                          +
                            _fH10_Prime * _fCurTimeSpan * a_pJointList.Joints[_iK].Tangent
                          +
                            _fH01_Prime * a_pJointList.Joints[_iK + 1].Location
                          + 
                            _fH11_Prime * _fCurTimeSpan * a_pJointList.Joints[_iK + 1].Tangent;

      _vP_T_Prime /= _fCurTimeSpan;

      return _vP_T_Prime;
    }

    /**
     * return the effective spline time based on the sampling mode and given time
     */
    public static float getEffectiveSplineTime(JointList a_pJointList, float a_fCurSplineTime, CurveSamplingModes a_eSamplingMode)
    {
      // spline time must be >= 0
      if(a_fCurSplineTime < 0.0f)
      {
        return 0.0f;
      }

      // the given spline time > the maximal spline time
      if(a_fCurSplineTime > a_pJointList.Joints[a_pJointList.Joints.Length - 1].Time)
      {
        // based on different sampling mode
        switch(a_eSamplingMode)
        {
          // clamped to last joint point
          case CurveSamplingModes.CURVE_SAMPLING_MODES_CAPPED:
          {
            // always return the maximal spline time
            return a_pJointList.Joints[a_pJointList.Joints.Length - 1].Time;
          }

          case CurveSamplingModes.CURVE_SAMPLING_MODES_REPEATED:
          {
            // given spline time % the maximal spline time
            int _iRepeatedCount = (int)(a_fCurSplineTime / a_pJointList.Joints[a_pJointList.Joints.Length - 1].Time);

            return a_fCurSplineTime - _iRepeatedCount * a_pJointList.Joints[a_pJointList.Joints.Length - 1].Time;
          }

          default:
          {
            return 0.0f;
          }
        }
      }
      else
      {
        return a_fCurSplineTime;
      }
    }
  }

} // namespace Framework.Mathe

/**
 * Check Accelerometer's States with preset sensitivity: any tilt value change less than the preset sensitivity will be ignored
 * - call doCheck() to collect all tilt events
 */

using UnityEngine;
using Framework.Utils;
using Framework.Logger;

namespace Framework.Inputs
{
  public static class AccelerometerChecker
  {
    #region PUBLIC METHODS
    /**
     * check the current state of accelerometer
     * - collect all tilt events
     */
    public static void doCheck(Utils.ObjectPool < DeviceEvent > a_pEventQueue)
    {
      // if there is no sensitivity value
      if(ms_fCurTiltSensitivity < 0.0f)
      {
        // read in sensitivity
        ms_fCurTiltSensitivity = Config.ConfigMgr.Instance.TiltSetting.m_fTiltSensitivityValue;

#if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("[AccelerometerChecker] Sensitivity: " + ms_fCurTiltSensitivity);
#endif

        // clear all cached value
        ms_fLastSampleX = ms_fLastSampleY = ms_fLastSampleZ = 0.0f;
      }

      /**
       * Accelerometer's Value in X and Y Axies: 
       * All values are in [-1, +1], but X value sometime just be in [-0.5, + 0.5] in some case
       * NOTE: if we are steering the phone along Z Axis: X values also changing:
       * -X: steering to left
       * +X: steering to right
       * +-------------------------------+
       * |   |         +Y           |    |
       * |   |______________________|    |
       * |-X |                      | +X | <<<--------------------- SmartPhone placed on a Table and facing up
       * |   |        PHONE         |    |
       * |   |----------------------|    |
       * |   |        -Y            |    |  
       * +-------------------------------+
       * Accelerometer's Value in Z Axis: put the phone one border edge on a Table: Z value is 0
       * Rotate the phone to point the Screen to sky   : Negative Value
       * Rotate the phone to point the Screen to ground: Positive Value
       * left   part tilted away: -X
       * right  part tilted away: +X
       * top    part tilted away: +Y
       * bottom part tilted away: -Y
       * phone tilted  facing up: -Z
       * phone tilted  facing down: +Z
       */
      // check all tilts
      // read in all raw tilt values
      float _fRawXTilt = Input.acceleration.x;
      float _fRawYTilt = Input.acceleration.y;
      float _fRawZTilt = Input.acceleration.z;

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[AccelerometerChecker] X: " + _fRawXTilt + " Y: " + _fRawYTilt);
#endif

      // calculate the delta changing from last sampling
      float _fDeltaXTilt = Mathf.Abs(_fRawXTilt - ms_fLastSampleX);
      float _fDeltaYTilt = Mathf.Abs(_fRawYTilt - ms_fLastSampleY);
      float _fDeltaZTilt = Mathf.Abs(_fRawZTilt - ms_fLastSampleZ);

      // check if there is any valid event
      DeviceEvent _pCurTiltEvent = null;

      // check Left/Right Tilt
      // tilt changing from previous sampling larger than the sensitivity
      if(_fDeltaXTilt > ms_fCurTiltSensitivity)
      {
        // store the tilt event
        // the first trigger tilt sampling
        if(null == _pCurTiltEvent)
        {
          createNewTiltEvent(a_pEventQueue, out _pCurTiltEvent);
        }

        // store the raw tilt value
        _pCurTiltEvent.DataArray[0] = _fRawXTilt;

        // cache the current sample value
        ms_fLastSampleX = _fRawXTilt;
      }

      // check Bottom/Top Tilt
      // tilt changing from previous sampling larger than the sensitivity
      if(_fDeltaYTilt > ms_fCurTiltSensitivity)
      {
        // store the tilt event
        // the first trigger tilt sampling
        if(null == _pCurTiltEvent)
        {
          createNewTiltEvent(a_pEventQueue, out _pCurTiltEvent);
        }

        // store the raw tilt value
        _pCurTiltEvent.DataArray[1] = _fRawYTilt;

        // cache the current sample value
        ms_fLastSampleY = _fRawYTilt;
      }

      // check FacingUp/FacingDown Tilt
      // tilt changing from previous sampling larger than the sensitivity
      if(_fDeltaZTilt > ms_fCurTiltSensitivity)
      {
        // store the tilt event
        // the first trigger tilt sampling
        if(null == _pCurTiltEvent)
        {
          createNewTiltEvent(a_pEventQueue, out _pCurTiltEvent);
        }

        // store the raw tilt value
        _pCurTiltEvent.DataArray[2] = _fRawZTilt;

        // cache the current sample value
        ms_fLastSampleZ = _fRawZTilt;
      }
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // static constructor: used to initialize all static data members
    static AccelerometerChecker()
    {
      ms_fCurTiltSensitivity = -1.0f;
    }

    // create a new tilt event in the given event queue
    static void createNewTiltEvent(Utils.ObjectPool < DeviceEvent > a_pEventQueue, out DeviceEvent a_pCurTiltEvent)
    {
      a_pCurTiltEvent = a_pEventQueue.createObject();

      a_pCurTiltEvent.Type         = EventTypes.EVENT_TYPES_TILT;
      a_pCurTiltEvent.ID           = EventIDs.EVENT_IDS_WHOLE_TILT;
      a_pCurTiltEvent.DataArray[0] = ms_fLastSampleX;
      a_pCurTiltEvent.DataArray[1] = ms_fLastSampleY;
      a_pCurTiltEvent.DataArray[2] = ms_fLastSampleZ;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // tilt sensitivity
    static float ms_fCurTiltSensitivity;
    // last sampling values
    static float ms_fLastSampleX;
    static float ms_fLastSampleY;
    static float ms_fLastSampleZ;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Framework.Inputs

/**
 * Check Touch Events
 * - call doCheck() to collect all touch events
 */

using UnityEngine;
using System.Collections;
using Framework.Utils;

namespace Framework.Inputs
{
  /**
   * Unity already provides an unified way to read inputs from Keyboard, Mouse and Joystick using:
   * Axis: GetAxisRaw() will return [-1, +1] value without smoothing
   * - Movement Axis: the Horizontal/Vertical Movement Direction: Input.GetAxisRaw("Horizontal")/Input.GetAxisRaw("Vertical")
   * - Button   Axis: Input.GetAxisRaw("Fire1")
   * 
   * Button State: GetKey("A") return true, if the given key is hold down
   * 
   * But we still need
   * - unify the input reading method: without needing to know the exact Axis Name for example
   * - simulate touch events using keyboard, mouse and joystick
   */
  // call TouchEventChecker.doCheck() to collect touch events
  public static class TouchEventChecker
  {
    #region PUBLIC METHODS
    /**
     * collect all touch events
     */
    public static void doCheck(Utils.ObjectPool < DeviceEvent > a_pEventQueue)
    {
      // collect all real touch events
      // iterate over all hardware touch events
      for(int _iCurTouchIdx = 0; _iCurTouchIdx < Input.touches.Length; _iCurTouchIdx++)
      {
        if(Input.touches[_iCurTouchIdx].phase != TouchPhase.Ended       // finger NOT lift
           &&                                                           // and
           Input.touches[_iCurTouchIdx].phase != TouchPhase.Canceled    // NOT canceled
          )
        {
          // valid touch event
          // create a new touch event from the queue
          DeviceEvent _pTouchEvent = a_pEventQueue.createObject();
          _pTouchEvent.Type = EventTypes.EVENT_TYPES_TOUCH;

          // collect current event ID and swipe distance data
          float _fSwipeDistDx = 0.0f;
          float _fSwipeDistDy = 0.0f;

          // for tick event
          if(Input.touches[_iCurTouchIdx].tapCount > 1)
          {
            _pTouchEvent.ID = EventIDs.EVENT_IDS_TOUCH_TICK;
          }
          else
          {
            // if the swipe is too tiny: considered as tap event
            if(Input.touches[_iCurTouchIdx].deltaPosition.x < Config.ConfigMgr.Instance.TouchSetting.SwipeSensitiveValue
               && 
               Input.touches[_iCurTouchIdx].deltaPosition.y < Config.ConfigMgr.Instance.TouchSetting.SwipeSensitiveValue
              )
            {
              _pTouchEvent.ID = EventIDs.EVENT_IDS_TOUCH_TAP; // TODO: check should we use TouchPhase.Ended?
            }
            else
            {
              // swipe event
              _pTouchEvent.ID = EventIDs.EVENT_IDS_TOUCH_SWIPE;

              // store swipe distance data
              _fSwipeDistDx = Input.touches[_iCurTouchIdx].deltaPosition.x;
              _fSwipeDistDy = Input.touches[_iCurTouchIdx].deltaPosition.y;
            }
          }

          // store event data
          /**
           * data:
           * 
           * DataArray[0]: touch position along x axis
           * DataArray[1]: touch position along y axis
           * DataArray[2]: swipe distance along x axis
           * DataArray[3]: swipe distance along y axis
           * DataArray[4]: tick count
           */
          _pTouchEvent.DataArray[0] = Input.touches[_iCurTouchIdx].position.x;
          _pTouchEvent.DataArray[1] = Input.touches[_iCurTouchIdx].position.y;
          _pTouchEvent.DataArray[2] = _fSwipeDistDx;
          _pTouchEvent.DataArray[3] = _fSwipeDistDy;
          _pTouchEvent.DataArray[4] = (float)Input.touches[_iCurTouchIdx].tapCount;
        }
      }

      // simulate any touch events caused by keyboard, mouse and joystick
      // get the current key settings
      Config.InputConfig[] _aKeySettings = Config.ConfigMgr.Instance.InputSettings;

      // simulate swipe touch events
      // NO input smoothing: use GetAxis() if we need: GetAxis() returns [-1, +1] for Keyboard and Joystick and [-a, +a] for mouse delta movement

      // get the configs for left, right, up and down
      Config.MoveInputConfig _pRealHorizontalInputConfigLeft  = (Config.MoveInputConfig)_aKeySettings[(int)Config.InputType.INPUT_TYPE_MOVE_LEFT];
      Config.MoveInputConfig _pRealHorizontalInputConfigRight = (Config.MoveInputConfig)_aKeySettings[(int)Config.InputType.INPUT_TYPE_MOVE_RIGHT];
      Config.MoveInputConfig _pRealVerticalInputConfigUp      = (Config.MoveInputConfig)_aKeySettings[(int)Config.InputType.INPUT_TYPE_MOVE_UP];
      Config.MoveInputConfig _pRealVerticalInputConfigDown    = (Config.MoveInputConfig)_aKeySettings[(int)Config.InputType.INPUT_TYPE_MOVE_DOWN];

      // check if there is any horizontal movement event caused by keyboard, mouse or joystick
      bool _bHasMovementInput = false;


      if(checkMovementInput(_pRealHorizontalInputConfigLeft.InputNameKB,
                            _pRealHorizontalInputConfigLeft.IsPositiveDirection,
                            _pRealHorizontalInputConfigRight.InputNameKB,
                            _pRealHorizontalInputConfigRight.IsPositiveDirection,
                            _pRealVerticalInputConfigUp.InputNameKB,
                            _pRealVerticalInputConfigUp.IsPositiveDirection,
                            _pRealVerticalInputConfigDown.InputNameKB,
                            _pRealVerticalInputConfigDown.IsPositiveDirection,
                            _pRealHorizontalInputConfigLeft.ScaleValueKB,
                            _pRealHorizontalInputConfigRight.ScaleValueKB,
                            _pRealVerticalInputConfigUp.ScaleValueKB,
                            _pRealVerticalInputConfigDown.ScaleValueKB,
                            0.0f,
                            0.0f,
                            0.0f,
                            0.0f,
                            a_pEventQueue
                           ) 
         ||
         checkMovementInput(_pRealHorizontalInputConfigLeft.InputNameMS,
                            _pRealHorizontalInputConfigLeft.IsPositiveDirection,
                            _pRealHorizontalInputConfigRight.InputNameMS,
                            _pRealHorizontalInputConfigRight.IsPositiveDirection,
                            _pRealVerticalInputConfigUp.InputNameMS,
                            _pRealVerticalInputConfigUp.IsPositiveDirection,
                            _pRealVerticalInputConfigDown.InputNameMS,
                            _pRealVerticalInputConfigDown.IsPositiveDirection,
                            _pRealHorizontalInputConfigLeft.ScaleValueMS,
                            _pRealHorizontalInputConfigRight.ScaleValueMS,
                            _pRealVerticalInputConfigUp.ScaleValueMS,
                            _pRealVerticalInputConfigDown.ScaleValueMS,
                            _pRealHorizontalInputConfigLeft.SensitiveValueMS,
                            _pRealHorizontalInputConfigRight.SensitiveValueMS,
                            _pRealVerticalInputConfigUp.SensitiveValueMS,
                            _pRealVerticalInputConfigDown.SensitiveValueMS,
                            a_pEventQueue
                           )
         ||
         checkMovementInput(_pRealHorizontalInputConfigLeft.InputNameJY,
                            _pRealHorizontalInputConfigLeft.IsPositiveDirection,
                            _pRealHorizontalInputConfigRight.InputNameJY,
                            _pRealHorizontalInputConfigRight.IsPositiveDirection,
                            _pRealVerticalInputConfigUp.InputNameJY,
                            _pRealVerticalInputConfigUp.IsPositiveDirection,
                            _pRealVerticalInputConfigDown.InputNameJY,
                            _pRealVerticalInputConfigDown.IsPositiveDirection,
                            _pRealHorizontalInputConfigLeft.ScaleValueJY,
                            _pRealHorizontalInputConfigRight.ScaleValueJY,
                            _pRealVerticalInputConfigUp.ScaleValueJY,
                            _pRealVerticalInputConfigDown.ScaleValueJY,
                            _pRealHorizontalInputConfigLeft.SensitiveValueJY,
                            _pRealHorizontalInputConfigRight.SensitiveValueJY,
                            _pRealVerticalInputConfigUp.SensitiveValueJY,
                            _pRealVerticalInputConfigDown.SensitiveValueJY,
                            a_pEventQueue
                           )
         )
      {
        // just check one type of movement input
        _bHasMovementInput = true;
      }

      if(false == _bHasMovementInput)
      {
        // if there is NO movement input, we will simulate the normal tap input
        // if any key tapped
        if(checkTapInput() == true)
        {
          // any tap key has been pressed down
          // add the tap touch event
          // create a new tap event from the queue
          DeviceEvent _pTapEvent = a_pEventQueue.createObject();
          _pTapEvent.Type = EventTypes.EVENT_TYPES_TOUCH;
          _pTapEvent.ID = EventIDs.EVENT_IDS_TOUCH_TAP;

          // store event data
          /**
           * data:
           * 
           * DataArray[0]: touch position along x axis
           * DataArray[1]: touch position along y axis
           * DataArray[2]: swipe distance along x axis
           * DataArray[3]: swipe distance along y axis
           * DataArray[4]: tick count
           */
          _pTapEvent.DataArray[0] = Input.mousePosition.x;
          _pTapEvent.DataArray[1] = Input.mousePosition.y;
          _pTapEvent.DataArray[2] = 0.0f;
          _pTapEvent.DataArray[3] = 0.0f;
          _pTapEvent.DataArray[4] = 1.0f;
        }
      }
    }
    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS
    /**
     * return true, if one simulated swipe touch event has detected
     */
    static bool checkMovementInput(string a_sHorizontalAxisNameA, 
                                   bool   a_bIsHorizontalPosDirA, 
                                   string a_sHorizontalAxisNameB,
                                   bool   a_bIsHorizontalPosDirB,
                                   string a_sVerticalAxisNameA, 
                                   bool   a_bIsVerticalPosDirA, 
                                   string a_sVerticalAxisNameB,
                                   bool   a_bIsVerticalPosDirB,
                                   float  a_fHorizontalScaleValA,
                                   float  a_fHorizontalScaleValB,
                                   float  a_fVerticalScaleValA,
                                   float  a_fVerticalScaleValB,
                                   float  a_fHorizontalSensValA, 
                                   float  a_fHorizontalSensValB,
                                   float  a_fVerticalSensValA, 
                                   float  a_fVerticalSensValB,
                                   Utils.ObjectPool < DeviceEvent > a_pEventQueue
                                  )
    {
      // check if there any tap down hold down
      if(false == isAnyTapKeyHoldDown())
      {
        // NO: then NO swipe touch event
        return false;
      }

      // some tap key hold down
      // read in horizontal movement value
      float _fHorizontalMovementValue = 0.0f;
      if(a_sHorizontalAxisNameA.Equals(a_sHorizontalAxisNameB))
      {
        // same name for both Axises
        // get the movement value
        _fHorizontalMovementValue = getMovementValue(a_sHorizontalAxisNameA, a_bIsHorizontalPosDirA, a_fHorizontalScaleValA, a_fHorizontalSensValA);
      }
      else
      {
        float _fMovementValueA = getMovementValue(a_sHorizontalAxisNameA, a_bIsHorizontalPosDirA, a_fHorizontalScaleValA, a_fHorizontalSensValA);
        float _fMovementValueB = getMovementValue(a_sHorizontalAxisNameB, a_bIsHorizontalPosDirB, a_fHorizontalScaleValB, a_fHorizontalSensValB);
        
        if((_fMovementValueA < 0 && false == a_bIsHorizontalPosDirA)
           ||
           (_fMovementValueA > 0 && true  == a_bIsHorizontalPosDirA)
          )
        {
          // movement along direction axis A
          _fHorizontalMovementValue = _fMovementValueA;
        }
        
        if((_fMovementValueB < 0 && false == a_bIsHorizontalPosDirB)
           ||
           (_fMovementValueB > 0 && true  == a_bIsHorizontalPosDirB)
          )
        {
          // movement along direction axis B
          _fHorizontalMovementValue = _fMovementValueB;
        }
      }

      // read in the vertical movement value
      float _fVerticalMovementValue = 0.0f;
      if(a_sVerticalAxisNameA.Equals(a_sVerticalAxisNameB))
      {
        // same name for both Axises
        // get the movement value
        _fVerticalMovementValue = getMovementValue(a_sVerticalAxisNameA, a_bIsVerticalPosDirA, a_fVerticalScaleValA, a_fVerticalSensValA);
      }
      else
      {
        float _fMovementValueA = getMovementValue(a_sVerticalAxisNameA, a_bIsVerticalPosDirA, a_fVerticalScaleValA, a_fVerticalSensValA);
        float _fMovementValueB = getMovementValue(a_sVerticalAxisNameB, a_bIsVerticalPosDirB, a_fVerticalScaleValB, a_fVerticalSensValB);
        
        if((_fMovementValueA < 0 && false == a_bIsVerticalPosDirA)
           ||
           (_fMovementValueA > 0 && true  == a_bIsVerticalPosDirA)
          )
        {
          // movement along direction axis A
          _fVerticalMovementValue = _fMovementValueA;
        }
        
        if((_fMovementValueB < 0 && false == a_bIsVerticalPosDirB)
           ||
           (_fMovementValueB > 0 && true  == a_bIsVerticalPosDirB)
          )
        {
          // movement along direction axis B
          _fVerticalMovementValue = _fMovementValueB;
        }
      }

      if(0.0f == _fHorizontalMovementValue
         &&
         0.0f == _fVerticalMovementValue
        )
      {
        // NO movement
        return false;
      }

      // some swipe event
      // get mouse cursor postion
      // create a new swipe event from the queue
      DeviceEvent _pSwipeEvent = a_pEventQueue.createObject();
      _pSwipeEvent.Type = EventTypes.EVENT_TYPES_TOUCH;
      _pSwipeEvent.ID = EventIDs.EVENT_IDS_TOUCH_SWIPE;

      // store event data
      /**
       * data:
       * 
       * DataArray[0]: touch position along x axis
       * DataArray[1]: touch position along y axis
       * DataArray[2]: swipe distance along x axis
       * DataArray[3]: swipe distance along y axis
       * DataArray[4]: tick count
       */
      _pSwipeEvent.DataArray[0] = Input.mousePosition.x;
      _pSwipeEvent.DataArray[1] = Input.mousePosition.y;
      _pSwipeEvent.DataArray[2] = _fHorizontalMovementValue;
      _pSwipeEvent.DataArray[3] = _fVerticalMovementValue;
      _pSwipeEvent.DataArray[4] = 1.0f;

      return true;
    }

    static float getMovementValue(string a_sAxisName, 
                                  bool a_bIsPosDir, 
                                  float a_fScaleVal,
                                  float a_fSensVal
                                           )
    {
      float _fMovementValue = Input.GetAxisRaw(a_sAxisName);

      _fMovementValue *= a_fScaleVal;

      if(_fMovementValue < 0)
      {
        // negative movement value
        if(_fMovementValue >= -a_fSensVal)
        {
          // NO movement
          return 0.0f;
        }
      }
      else
      {
        // positive movement value
        if(_fMovementValue <= a_fSensVal)
        {
          // NO movement
          return 0.0f;
        }
      }

      return _fMovementValue;
    }

    // check if there any tap down hold down
     static bool isAnyTapKeyHoldDown()
    {
      // check tap key A
      Config.InputConfig _pCurrConfig = Config.ConfigMgr.Instance.InputSettings[(int)Config.InputType.INPUT_TYPE_TAPKEYA];
      if(true == isGivenTapKeyHoldDown(_pCurrConfig))
      {
        return true;
      }
      
      _pCurrConfig = Config.ConfigMgr.Instance.InputSettings[(int)Config.InputType.INPUT_TYPE_TAPKEYB];
      if(true == isGivenTapKeyHoldDown(_pCurrConfig))
      {
        return true;
      }
      
      _pCurrConfig = Config.ConfigMgr.Instance.InputSettings[(int)Config.InputType.INPUT_TYPE_TAPKEYC];
      if(true == isGivenTapKeyHoldDown(_pCurrConfig))
      {
        return true;
      }

      return false;
    }

    /**
     * return true if the key is pressed down
     */
    static bool isGivenTapKeyHoldDown(Config.InputConfig a_pKeyConfig)
    {
      if(true == Input.GetKey(a_pKeyConfig.InputKeyCodeKB)  // return true while the user holds down the key identified by name
         ||
         true == Input.GetKey(a_pKeyConfig.InputKeyCodeMS)
         ||
         true == Input.GetKey(a_pKeyConfig.InputKeyCodeJY)
        )
      {
        // any tap key has been hold down
        return true;
      }
      
      return false;
    }

    /**
     * return true, if anyone of the given 3 tap keys is pressed down during this frame
     */
    static bool checkTapInput()
    {
      // AKey has been pressed and released
      Config.InputConfig _pCurrConfig = Config.ConfigMgr.Instance.InputSettings[(int)Config.InputType.INPUT_TYPE_TAPKEYA];
      if(true == isGivenTapKeyPressed(_pCurrConfig))
      {
        return true;
      }

      // BKey has been pressed and released
      _pCurrConfig = Config.ConfigMgr.Instance.InputSettings[(int)Config.InputType.INPUT_TYPE_TAPKEYB];
      if(true == isGivenTapKeyPressed(_pCurrConfig))
      {
        return true;
      }

      // CKey has been pressed and released
      _pCurrConfig = Config.ConfigMgr.Instance.InputSettings[(int)Config.InputType.INPUT_TYPE_TAPKEYC];
      if(true == isGivenTapKeyPressed(_pCurrConfig))
      {
        return true;
      }

      return false;
    }

    static bool isGivenTapKeyPressed(Config.InputConfig a_pKeyConfig)
    {
      if(true == Input.GetKeyUp(a_pKeyConfig.InputKeyCodeKB)  // return true, if the key has been released during this frame
         ||
         true == Input.GetKeyUp(a_pKeyConfig.InputKeyCodeMS)
         ||
         true == Input.GetKeyUp(a_pKeyConfig.InputKeyCodeJY)
         )
      {
        return true;
      }

      return false;
    }
    #endregion // PROTECTED METHODS
  }

} // namespace Framework.Inputs

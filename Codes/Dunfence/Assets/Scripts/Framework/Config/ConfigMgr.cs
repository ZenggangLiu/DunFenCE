using System;      // FormatException
using System.IO;   // StringReader
using UnityEngine; // TextAsset, Resources.Load(), KeyCode
using Framework.Utils;
using Framework.Logger;

namespace Framework.Config
{
  // the key configure define
  public class InputConfig
  {
    public InputConfig()
    {
      m_sInputNameKB = "";
      m_sInputNameMS = "";
      m_sInputNameJY = "";

      m_eInputKeyCodeKB = KeyCode.None;
      m_eInputKeyCodeMS = KeyCode.None;
      m_eInputKeyCodeJY = KeyCode.None;
    }
    // return input name for keyboard
    public string InputNameKB
    {
      get
      {
        return m_sInputNameKB;
      }
    }

    // return input name for mouse
    public string InputNameMS
    {
      get
      {
        return m_sInputNameMS;
      }
    }

    // return the input name for joystick
    public string InputNameJY
    {
      get
      {
        return m_sInputNameJY;
      }
    }

    public KeyCode InputKeyCodeKB
    {
      get
      {
        return m_eInputKeyCodeKB;
      }
    }

    public KeyCode InputKeyCodeMS
    {
      get
      {
        return m_eInputKeyCodeMS;
      }
    }

    public KeyCode InputKeyCodeJY
    {
      get
      {
        return m_eInputKeyCodeJY;
      }
    }

    internal string m_sInputNameKB;     // input name for keyboard
    internal string m_sInputNameMS;     // input name for mouse
    internal string m_sInputNameJY;     // input name for joystick
    internal KeyCode m_eInputKeyCodeKB; // input key code for keyboard
    internal KeyCode m_eInputKeyCodeMS; // input key code for mouse
    internal KeyCode m_eInputKeyCodeJY; // input key code for joystick
  }

  public class MoveInputConfig : InputConfig
  {
    public MoveInputConfig()
      :
      base()
    {
      m_bIsPositiveDirection = false;

      m_fSensitiveValueMS = 0.0f;
      m_fSensitiveValueJY = 0.0f;

      m_fScaleValueKB = 0.0f;
      m_fScaleValueMS = 0.0f;
      m_fScaleValueJY = 0.0f;
    }

    // return the sensitive value, which defines the minimal movement value for this movement input
    // all values which are less than this value, will be considered as ZERO
    public float SensitiveValueMS
    {
      get
      {
        return m_fSensitiveValueMS;
      }
    }

    public float SensitiveValueJY
    {
      get
      {
        return m_fSensitiveValueJY;
      }
    }

    public float ScaleValueKB
    {
      get
      {
        return m_fScaleValueKB;
      }
    }

    public float ScaleValueMS
    {
      get
      {
        return m_fScaleValueMS;
      }
    }

    public float ScaleValueJY
    {
      get
      {
        return m_fScaleValueJY;
      }
    }

    // return true if the given movement input represents a positive value
    public bool IsPositiveDirection
    {
      get
      {
        return m_bIsPositiveDirection;
      }
    }

    internal float m_fSensitiveValueMS;
    internal float m_fSensitiveValueJY;

    internal float m_fScaleValueKB;
    internal float m_fScaleValueMS;
    internal float m_fScaleValueJY;

    internal bool m_bIsPositiveDirection;
  }

  // different input types
  public enum InputType
  {
    INPUT_TYPE_TAPKEYA = 0,
    INPUT_TYPE_TAPKEYB,
    INPUT_TYPE_TAPKEYC,

    INPUT_TYPE_MOVE_LEFT,
    INPUT_TYPE_MOVE_RIGHT,
    INPUT_TYPE_MOVE_UP,
    INPUT_TYPE_MOVE_DOWN,

    INPUT_TYPE_COUNT
  }

  public class TouchConfig
  {
    public TouchConfig()
    {
      m_fSwipeSensitiveValue = 0.0f;
    }

    public float SwipeSensitiveValue
    {
      get
      {
        return m_fSwipeSensitiveValue;
      }
    }

    internal float m_fSwipeSensitiveValue;
  }

  public class TiltConfig
  {
    public float m_fTiltSensitivityValue;
  }

  // reading configure.xml and provide accessors for the file contents
  // ConfigMgr.Instance.loadConfigFile()
  public class ConfigMgr : TManager < ConfigMgr >
  {
    #region PUBLIC METHODS
    // return the touch config
    public TouchConfig TouchSetting
    {
      get
      {
        return m_pTouchConfig;
      }
    }

    // return the tilt config
    public TiltConfig TiltSetting
    {
      get
      {
        return m_pTiltConfig;
      }
    }

    // return the input config cache
    public InputConfig[] InputSettings
    {
      get
      {
        return m_aInputConfigCache;
      }
    }

    public ConfigMgr()
    {
      // create the configure cache
      m_aInputConfigCache = new InputConfig[(int)InputType.INPUT_TYPE_COUNT];

      // zero all array elements
      for (int _iIdx = 0; _iIdx < m_aInputConfigCache.Length; ++_iIdx)
      {
        m_aInputConfigCache[_iIdx] = null;
      }

      // create touch config
      m_pTouchConfig = new TouchConfig();

      // create tilt config
      m_pTiltConfig = new TiltConfig();
    }

    #region PUBLIC OVERRIDDENS
    // the function called before using this manager
    // set m_bIsInitialized to true, on successful initialization
    public override void doInitialize()
    {
      // try to load config file
      m_bIsInitialized = loadConfigFile();
    }

    // pre-update function
    public override void doPreUpdate()
    {

    }

    // update function
    public override void doUpdate()
    {

    }

    // post-update function called after all update() functions had been called
    public override void doPostUpdate()
    {

    }

    // the function called before destroying this manager
    public override void doDestroy()
    {

    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // load in configure file
    // return true on success
    bool loadConfigFile()
    {
      // if this manager has been initialized: return
      if(true == m_bIsInitialized)
      {
        return true;
      }

      // try to load the config file
      TextAsset _pConfigDataJsonFile = Resources.Load(CONFIG_FILE_NAME) as TextAsset; // an TextAsset: the representation of a Textural or Binary File
      if(null == _pConfigDataJsonFile)
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][loadConfigFile]: Can NOT load config file: " + CONFIG_FILE_NAME);

        return false;
      }

      // create and parse the json data at the same time
      JSONObject _pConfigDataJson = new JSONObject(_pConfigDataJsonFile.text);

      // try to parse
      return parseConfigFile(_pConfigDataJson);
    }

    // parse the given config json data
    // return true on success
    bool parseConfigFile(JSONObject a_pConfigDataJson)
    {
      // try to find GameConfig node
      if(false == a_pConfigDataJson.HasField("GameConfig"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][parseConfig]: Can NOT find GameConfig node in " + CONFIG_FILE_NAME + "  file");

        return false;
      }

      JSONObject _pGameConfigJson = a_pConfigDataJson["GameConfig"];

      // try to find DeviceInput node under GameConfig node
      if(false == _pGameConfigJson.HasField("DeviceInput"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][parseConfig]: Can NOT find DeviceInput node under GameConfig node in " + CONFIG_FILE_NAME + "  file");

        return false;
      }

      // try to read TapKeyA node
      if(true == _pGameConfigJson["DeviceInput"].HasField("TapKeyA"))
      {
        InputConfig _pNewConfig = new InputConfig();

        if(false ==  readTapKeyConfig(_pGameConfigJson["DeviceInput"]["TapKeyA"], _pNewConfig))
        {
          DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][parseConfig]: Can NOT parese TapKeyA node");

          return false;
        }

        m_aInputConfigCache[(int)InputType.INPUT_TYPE_TAPKEYA] = _pNewConfig;
      }

      // try to read TapKeyB node
      if(true == _pGameConfigJson["DeviceInput"].HasField("TapKeyB"))
      {
        InputConfig _pNewConfig = new InputConfig();

        if(false == readTapKeyConfig(_pGameConfigJson["DeviceInput"]["TapKeyB"], _pNewConfig))
        {
          DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][parseConfig]: Can NOT parese TapKeyB node");

          return false;
        }

        m_aInputConfigCache[(int)InputType.INPUT_TYPE_TAPKEYB] = _pNewConfig;
      }

      // try to read TapKeyC node
      if(true == _pGameConfigJson["DeviceInput"].HasField("TapKeyC"))
      {
        InputConfig _pNewConfig = new InputConfig();

        if(false == readTapKeyConfig(_pGameConfigJson["DeviceInput"]["TapKeyC"], _pNewConfig))
        {
          DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][parseConfig]: Can NOT parese TapKeyC node");

          return false;
        }

        m_aInputConfigCache[(int)InputType.INPUT_TYPE_TAPKEYC] = _pNewConfig;
      }

      // try to read LeftKey node
      if(true == _pGameConfigJson["DeviceInput"].HasField("LeftKey"))
      {
        MoveInputConfig _pNewConfig = new MoveInputConfig();

        if(false == readMoveAxisConfig(_pGameConfigJson["DeviceInput"]["LeftKey"], _pNewConfig))
        {
          DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][parseConfig]: Can NOT parese LeftKey node");

          return false;
        }

        m_aInputConfigCache[(int)InputType.INPUT_TYPE_MOVE_LEFT] = _pNewConfig;
      }

      // try to read RightKey node
      if(true == _pGameConfigJson["DeviceInput"].HasField("RightKey"))
      {
        MoveInputConfig _pNewConfig = new MoveInputConfig();

        if(false == readMoveAxisConfig(_pGameConfigJson["DeviceInput"]["RightKey"], _pNewConfig))
        {
          DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][parseConfig]: Can NOT parese RightKey node");

          return false;
        }

        m_aInputConfigCache[(int)InputType.INPUT_TYPE_MOVE_RIGHT] = _pNewConfig;
      }

      // try to read UpKey node
      if(true == _pGameConfigJson["DeviceInput"].HasField("UpKey"))
      {
        MoveInputConfig _pNewConfig = new MoveInputConfig();

        if(false == readMoveAxisConfig(_pGameConfigJson["DeviceInput"]["UpKey"], _pNewConfig))
        {
          DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][parseConfig]: Can NOT parese UpKey node");

          return false;
        }

        m_aInputConfigCache[(int)InputType.INPUT_TYPE_MOVE_UP] = _pNewConfig;
      }

      // try to read DownKey node
      if(true == _pGameConfigJson["DeviceInput"].HasField("DownKey"))
      {
        MoveInputConfig _pNewConfig = new MoveInputConfig();

        if(false == readMoveAxisConfig(_pGameConfigJson["DeviceInput"]["DownKey"], _pNewConfig))
        {
          DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][parseConfig]: Can NOT parese DownKey node");

          return false;
        }

        m_aInputConfigCache[(int)InputType.INPUT_TYPE_MOVE_DOWN] = _pNewConfig;
      }

      // try to find TouchEvent node under GameConfig node
      if(false == _pGameConfigJson.HasField("TouchEvent"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][parseConfig]: Can NOT find TouchEvent node under GameConfig node in " + CONFIG_FILE_NAME + "  file");

        return false;
      }

      // read TouchEvent config
      if(false == readTouchConfig(_pGameConfigJson["TouchEvent"]))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][parseConfig]: Can NOT parese TouchEvent node");

        return false;
      }

      // try to find TiltEvent node under GameConfig node
      if(false == _pGameConfigJson.HasField("TiltEvent"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][parseConfig]: Can NOT find TiltEvent node under GameConfig node in " + CONFIG_FILE_NAME + "  file");

        return false;
      }

      // read TiltEvent config
      if(false == readTiltConfig(_pGameConfigJson["TiltEvent"]))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][parseConfig]: Can NOT parese TiltEvent node");

        return false;
      }

      return true;
    }

    // read in config for movement axis
    // return true on success
    bool readMoveAxisConfig(JSONObject a_pMovementAxisConfigJson, MoveInputConfig a_pNewConfig)
    {
      // try to find IsPosDir node
      if(false == a_pMovementAxisConfigJson.HasField("IsPosDir"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: Can NOT find IsPosDir node");

        return false;
      }

      // IsPosDir is NOT a boolean
      if(false == a_pMovementAxisConfigJson["IsPosDir"].IsBool)
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: IsPosDir node is NOT a boolean value");

        return false;
      }

      // store the value
      a_pNewConfig.m_bIsPositiveDirection = a_pMovementAxisConfigJson["IsPosDir"].b;

      // try to find Keyboard node
      if(false == a_pMovementAxisConfigJson.HasField("Keyboard"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: Can NOT find Keyboard node");

        return false;
      }

      // try to find Name node under Keyboard node
      if(false == a_pMovementAxisConfigJson["Keyboard"].HasField("Name"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: Can NOT find Name node under Keyboard node");

        return false;
      }

      // store the name
      a_pNewConfig.m_sInputNameKB = a_pMovementAxisConfigJson["Keyboard"]["Name"].str;

      // try to find ScaleValue node
      if(false == a_pMovementAxisConfigJson["Keyboard"].HasField("ScaleValue"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: Can NOT find ScaleValue node under Keyboard node");

        return false;
      }

      // not a number
      if(false == a_pMovementAxisConfigJson["Keyboard"]["ScaleValue"].IsNumber)
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: ScaleValue node is NOT a number");

        return false;
      }

      // store ScaleValue
      a_pNewConfig.m_fScaleValueKB = a_pMovementAxisConfigJson["Keyboard"]["ScaleValue"].f;

      // try to find Mouse node
      if(false == a_pMovementAxisConfigJson.HasField("Mouse"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: Can NOT find Mouse node");

        return false;
      }

      // try to find Name node under Mouse node
      if(false == a_pMovementAxisConfigJson["Mouse"].HasField("Name"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: Can NOT find Name node under Mouse node");

        return false;
      }

      // store the name
      a_pNewConfig.m_sInputNameMS = a_pMovementAxisConfigJson["Mouse"]["Name"].str;

      // try to find Sensitivity node
      if(false == a_pMovementAxisConfigJson["Mouse"].HasField("Sensitivity"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: Can NOT find Sensitivity node under Mouse node");

        return false;
      }

      // not a number
      if(false == a_pMovementAxisConfigJson["Mouse"]["Sensitivity"].IsNumber)
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: Sensitivity node is NOT a number");

        return false;
      }

      // store Sensitivity
      a_pNewConfig.m_fSensitiveValueMS = a_pMovementAxisConfigJson["Mouse"]["Sensitivity"].f;

      // try to find ScaleValue node
      if(false == a_pMovementAxisConfigJson["Mouse"].HasField("ScaleValue"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: Can NOT find ScaleValue node under Mouse node");

        return false;
      }

      // not a number
      if(false == a_pMovementAxisConfigJson["Mouse"]["ScaleValue"].IsNumber)
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: ScaleValue node is NOT a number");

        return false;
      }

      // store ScaleValue
      a_pNewConfig.m_fScaleValueMS = a_pMovementAxisConfigJson["Mouse"]["ScaleValue"].f;

      // try to find Joystick node
      if(false == a_pMovementAxisConfigJson.HasField("Joystick"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: Can NOT find Joystick node");

        return false;
      }

      // try to find Name node under Joysticknode
      if(false == a_pMovementAxisConfigJson["Joystick"].HasField("Name"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: Can NOT find Name node under Joystick node");

        return false;
      }

      // store the name
      a_pNewConfig.m_sInputNameJY = a_pMovementAxisConfigJson["Joystick"]["Name"].str;

      // try to find Sensitivity node
      if(false == a_pMovementAxisConfigJson["Joystick"].HasField("Sensitivity"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: Can NOT find Sensitivity node under Joystick node");

        return false;
      }

      // not a number
      if(false == a_pMovementAxisConfigJson["Joystick"]["Sensitivity"].IsNumber)
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: Sensitivity node is NOT a number");

        return false;
      }

      // store Sensitivity
      a_pNewConfig.m_fSensitiveValueJY = a_pMovementAxisConfigJson["Joystick"]["Sensitivity"].f;

      // try to find ScaleValue node
      if(false == a_pMovementAxisConfigJson["Joystick"].HasField("ScaleValue"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: Can NOT find ScaleValue node under Joystick node");

        return false;
      }

      // not a number
      if(false == a_pMovementAxisConfigJson["Joystick"]["ScaleValue"].IsNumber)
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readMoveAxisConfig]: ScaleValue node is NOT a number");

        return false;
      }

      // store ScaleValue
      a_pNewConfig.m_fScaleValueJY = a_pMovementAxisConfigJson["Joystick"]["ScaleValue"].f;

      return true;
    }

    // read config for tap key
    // return true on success
    bool readTapKeyConfig(JSONObject a_pKeyConfigJson, InputConfig a_pNewConfig)
    {
      // try to find Keyboard node
      if(false == a_pKeyConfigJson.HasField("Keyboard"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readTapKeyConfig]: Can NOT find Keyboard node");

        return false;
      }

      // store input name
      a_pNewConfig.m_sInputNameKB = a_pKeyConfigJson["Keyboard"].str;

      // parse the key code
      a_pNewConfig.m_eInputKeyCodeKB = (KeyCode)Enum.Parse(typeof(KeyCode), a_pNewConfig.m_sInputNameKB);

      // try to find Mouse node
      if(false == a_pKeyConfigJson.HasField("Mouse"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readTapKeyConfig]: Can NOT find Mouse node");

        return false;
      }

      // store input name
      a_pNewConfig.m_sInputNameMS = a_pKeyConfigJson["Mouse"].str;

      // parse the key code
      a_pNewConfig.m_eInputKeyCodeMS = (KeyCode)Enum.Parse(typeof(KeyCode), a_pNewConfig.m_sInputNameMS);

      // try to find Joystick node
      if(false == a_pKeyConfigJson.HasField("Joystick"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readTapKeyConfig]: Can NOT find Joystick node");

        return false;
      }

      // store input name
      a_pNewConfig.m_sInputNameJY = a_pKeyConfigJson["Joystick"].str;

      // parse the key code
      a_pNewConfig.m_eInputKeyCodeJY = (KeyCode)Enum.Parse(typeof(KeyCode), a_pNewConfig.m_sInputNameJY);

      return true;
    }

    // read config for touch event
    // return true on success
    bool readTouchConfig(JSONObject a_pTouchConfigJson)
    {
      // try to find Swipe node
      if(false == a_pTouchConfigJson.HasField("Swipe"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readTouchConfig]: Can NOT find Swipe node");

        return false;
      }

      // try to find Sensitivity node under Swipe node
      if(false == a_pTouchConfigJson["Swipe"].HasField("Sensitivity"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readTouchConfig]: Can NOT find Sensitivity node under Swipe node");

        return false;
      }

      // Sensitivity node is not a number
      if(false == a_pTouchConfigJson["Swipe"]["Sensitivity"].IsNumber)
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readTouchConfig]: Sensitivity node is NOT a number");

        return false;
      }

      // store Sensitivity value
      m_pTouchConfig.m_fSwipeSensitiveValue = a_pTouchConfigJson["Swipe"]["Sensitivity"].f;

      return true;
    }

    // read config for tilt event
    // return true on success
    bool readTiltConfig(JSONObject a_pTiltConfigJson)
    {
      // try to find Sensitivity node
      if(false == a_pTiltConfigJson.HasField("Sensitivity"))
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readTiltConfig]: Can NOT find Sensitivity node");

        return false;
      }

      // Sensitivity node is not a number
      if(false == a_pTiltConfigJson["Sensitivity"].IsNumber)
      {
        DefaultGameLogger.Instance.dumpLogError("[ConfigMgr][readTiltConfig]: Sensitivity node is NOT a number");

        return false;
      }

      m_pTiltConfig.m_fTiltSensitivityValue = a_pTiltConfigJson["Sensitivity"].f;

      return true;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // config file name
    static string CONFIG_FILE_NAME = "DeviceInputConfigData";
    // input config cache
    InputConfig[] m_aInputConfigCache;
    // touch config
    TouchConfig m_pTouchConfig;
    // tilt config
    TiltConfig m_pTiltConfig;
    #endregion // PRIVATE DATA MEMBERS
  }
} // namespace Framework.Config

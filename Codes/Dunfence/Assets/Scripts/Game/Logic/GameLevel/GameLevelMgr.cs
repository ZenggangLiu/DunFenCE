/**
 * Game Level Manager
 * - switch to 
 * - load in LevelInfoList from LevelData.json
 *   + which defines temporary Resource Asset ID: TODO: using GameAssetMgr
 *   + other parts of Game Logic using IDs to create that resource
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using Framework.Utils;
using Framework.Logger;
using Game.Camera;
using Game.Event;
using System.Collections.Generic; // List< T >

namespace Game.Logic
{
  public enum LevelSwitchingStates
  {
    // still switching level
    LEVEL_SWITCHING_STATES_PROCESSING,
    // done with level switching
    LEVEL_SWITCHING_STATES_DONE,
  }

  // main level info
  public class MainLevelInfo
  {
    #region PUBLIC DATA MEMBERS
    // main entry node
    public GameObject MainEntryNode;
    // main entry component
    public GameMain MainEntry;
    // scene node: ARScene or NormScene
    public GameObject SceneNode;
    // camera node within the current scene node: the node with camera controller attached to
    public GameObject CameraNode;
    // camera controller of the above camera node
    public Game.Camera.CameraController CameraController;
    // DropOffs node
    public GameObject DropOffsNode;
    // Effects node
    public GameObject EffectsNode;
    // ShootOuts node
    public GameObject ShootOutsNode;
    // main camera
    public GameObject GameMainCamera;
    // main gui
    public GameObject GameMainGUI;
    // event system for gui
    public GameObject GUIEventSystem;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    // default constructor
    public MainLevelInfo()
    {
      MainEntryNode = null;
      MainEntry = null;
      SceneNode = null;
      CameraNode = null;
      CameraController = null;
      GameMainGUI = null;
      GUIEventSystem = null;
    }
    #endregion // PUBLIC METHODS
  }

  // config data of each level as stored in LevelData.json
  public class LevelConfigData
  {
    #region PUBLIC DATA MEMBERS
    // level name
    public string Name;
    // discount key
    public string DiscountKey;
    // preload asset list
    public List< int > PreloadAssetList;
    // preload effect list
    public List< int > PreloadEffectList;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    public LevelConfigData(string a_szLevelName, string a_szDiscountKey)
    {
      Name = a_szLevelName;
      DiscountKey = a_szDiscountKey;
      PreloadAssetList = new List< int >(6);
      PreloadEffectList = new List< int >(4);
    }
    #endregion // PUBLIC METHODS
  }

  /**
     * call back during level switching: a_fProgress: [0, 100]
     * - .unity scene loading: [0, 50]
     * - game level warm up  : [50, 100]
    */
  public delegate void LevelSwitchingHandler(LevelSwitchingStates a_eCurState, float a_fProgress);

  public class LevelSwitchingData
  {
    #region PUBLIC DATA MEMBERS
    // current level config data
    public LevelConfigData CurrLevelConfigData;
    // current level switching handler
    public LevelSwitchingHandler CurLevelSwitchingHandler;
    // required camera mode
    public Game.Camera.CameraModes RequiredCameraMode;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    // default constructor
    public LevelSwitchingData()
    {
      // reset all data members
      CurrLevelConfigData = null;
      CurLevelSwitchingHandler = null;
      RequiredCameraMode = CameraModes.CAMERA_MODES_REGULAR;
    }
    #endregion // PUBLIC METHODS
  }

  public class GameLevelMgr : TManager< GameLevelMgr >
  {
    #region PUBLIC METHODS
    // return current game level we are playing
    public GameLevel CurrentGameLevel
    {
      get
      {
        return m_pCurGameLevel;
      }
    }

    // return game main level info
    public MainLevelInfo GameMainLevel
    {
      get
      {
        return m_pGameMainLevelInfo;
      }
    }

    // load in main level info
    public void loadMainLevelInfo(GameObject a_pMainLevel)
    {
      // create game main level info, if it does NOT exist
      if(null == m_pGameMainLevelInfo)
      {
        m_pGameMainLevelInfo = new MainLevelInfo();
      }

      // create root node list from main scene: MainScene.unity
      GameObject[] _aRootNodeList = new GameObject[a_pMainLevel.scene.rootCount];
      // read in all root nodes
      _aRootNodeList = a_pMainLevel.scene.GetRootGameObjects();

      // try to get NormScene, ARScene, GameMainGUI nodes
      // iterate over all root nodes
      for(int _iCurRootNodeIdx = 0; _iCurRootNodeIdx < _aRootNodeList.Length; _iCurRootNodeIdx++)
      {
        if(_aRootNodeList[_iCurRootNodeIdx].name.Equals("GameMain"))
        {
          m_pGameMainLevelInfo.MainEntryNode = _aRootNodeList[_iCurRootNodeIdx];
          m_pGameMainLevelInfo.MainEntry = m_pGameMainLevelInfo.MainEntryNode.GetComponentInChildren< GameMain >();
        }

        else if(_aRootNodeList[_iCurRootNodeIdx].name.Equals("GameMainGUI"))
        {
          m_pGameMainLevelInfo.GameMainGUI = _aRootNodeList[_iCurRootNodeIdx];
        }

        else if(_aRootNodeList[_iCurRootNodeIdx].name.Equals("GUIEventSystem"))
        {
          m_pGameMainLevelInfo.GUIEventSystem = _aRootNodeList[_iCurRootNodeIdx];
        }

        else if(_aRootNodeList[_iCurRootNodeIdx].name.Equals("GameMainCamera"))
        {
          m_pGameMainLevelInfo.GameMainCamera = _aRootNodeList[_iCurRootNodeIdx];
        }
      }

      // NO GameMainGUI node: errors
      if(null == m_pGameMainLevelInfo.GameMainGUI)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelMgr][loadMainLevelInfo]: NO GameMainGUI node");

        return;
      }

      // NO event system node: errors
      if(null == m_pGameMainLevelInfo.GUIEventSystem)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelMgr][loadMainLevelInfo]: NO GUIEventSystem node");

        return;
      }
    }

    /**
     * switch to the given level
     * - release currently loaded level: call doExit() and doCoolDown() of the currently loaded level if there is one
     * - load the given level
     *   + create a new SceneNode node based on the given a_eCameraMode
     *   + send out loading request: SceneManager.LoadSceneAsync(). NOTE: SceneManager.LoadScene() will NOT block the calling thread during loading, we have to spin on the loading flag
     *   + update function will do:
     *     # disable all camera nodes
     *     # disable all light nodes
     *     # attach level node onto SceneRoot node under Scene node
     *     # call doWarmUp() of the loaded level
     * return true: on success
     */
    public bool switchToLevel(string a_szLevelName, Game.Camera.CameraModes a_eCameraMode, LevelSwitchingHandler a_pLevelLoadingHandler)
    {
      // wrong state
      if(States.STATES_IDLE != m_eCurState)
      {
        return false;
      }

      // try to get the reqired level config data
      if(m_aLevelConfigDataList.ContainsKey(a_szLevelName) == false)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelMgr][switchToLevel]: Can NOT find the Level Config Data for the given level: " + a_szLevelName);

        return false;
      }

      // store level switching data
      m_pCurLevelSwitchingData.RequiredCameraMode = a_eCameraMode;                              // required camera mode
      m_pCurLevelSwitchingData.CurrLevelConfigData = m_aLevelConfigDataList[a_szLevelName];     // level config data
      m_pCurLevelSwitchingData.CurLevelSwitchingHandler = a_pLevelLoadingHandler;               // loading callback

      // check if we are trying to swith to a different level
      bool _bIsDiffLevelSwitch = !m_szCurLevelName.Equals(a_szLevelName); // NOT equal

      // store the given level name
      m_szCurLevelName = a_szLevelName;

      // set default state to prepare state
      m_eCurState = States.STATES_PREPARE_SWITCHING_LEVEL;

      // FIRST level to load: return
      if(null == m_pCurGameLevel)
      {
        return true;
      }

      // there is some level loaded so far
      // exit the loaded level
      m_pCurGameLevel.doExit();

      // fire level unloading event
      GameEventMgr.Instance.fireGlobalScopeEvent(Game.Event.EventIDs.EVENT_IDS_LOGIC_LEVEL_UNLOADING,
                                                 m_pCurGameLevel,
                                                 Game.Event.EventTriggerType.EVENT_TRIGGER_TYPE_LOCAL,
                                                 this
                                                );

      // cool down the loaded level
      m_pCurGameLevel.doCoolDown();

      // reset camera controller
      m_pGameMainLevelInfo.CameraController.doReset();

      // detach level node out of SceneNode
      // put it back to scene file: .unity file
      m_pCurGameLevel.gameObject.transform.parent = null;

      // destroy the SceneNode
      GameObject.Destroy(m_pGameMainLevelInfo.SceneNode);

      // reset all references in main level info
      m_pGameMainLevelInfo.SceneNode = null;
      m_pGameMainLevelInfo.CameraNode = null;
      m_pGameMainLevelInfo.CameraController = null;

      // clear the reference to the currently loaded game level
      m_pCurGameLevel = null;

      // release all un-used resources
      Resources.UnloadUnusedAssets();

      // garbage collection
      System.GC.Collect();

      // set cool down timer
      m_fCurCoolDownTime = MAX_COOLDOWN_TIME;

      // change to cool down state
      if(true == _bIsDiffLevelSwitch)
      {
        m_eCurState = States.STATES_COOLDOWN_BEFORE_DIFFLEVEL_SWITCHING;  
      }
      else
      {
        m_eCurState = States.STATES_COOLDOWN_BEFORE_SAMELEVEL_SWITCHING;
      }

      return true;
    }

    /**
     * attach current level node onto the given attach node
     */
    public void attachLevelNode(GameObject a_pAttachNode)
    {
      // SAME attach node: return
      if(m_pCurAttachNode == a_pAttachNode)
      {
        return;
      }

      // FIRST attach node: NO need to copy all subnodes
      if(null == m_pCurAttachNode)
      {
        // attach the current level onto the given attach node
        m_pCurGameLevel.transform.parent = a_pAttachNode.transform;  
      }

      // switch attach node: copy all subnodes of current attach node
      else
      {
        // store all subnodes
        GameObject[] _aSubNodeList = new GameObject[m_pCurAttachNode.transform.childCount];
        for(int _iCIdx = 0; _iCIdx < m_pCurAttachNode.transform.childCount; _iCIdx++)
        {
          _aSubNodeList[_iCIdx] = m_pCurAttachNode.transform.GetChild(_iCIdx).gameObject;
        }

        // move all subnodes of current attach node into the given attach node
        for(int _iCIdx = 0; _iCIdx < _aSubNodeList.Length; _iCIdx++)
        {
          _aSubNodeList[_iCIdx].transform.parent = a_pAttachNode.transform;
        }

        _aSubNodeList = null;
      }

      // read information of the given attach node
      readAttachNodeInfo(a_pAttachNode);

      // store the given attach node
      m_pCurAttachNode = a_pAttachNode;
    }
    #region PUBLIC OVERRIDDENS
    // the function called before using this manager
    // load in LevelData.json file
    public override void doInitialize()
    {
      // clear up game main level info
      m_pGameMainLevelInfo = null;

      // clear currently loaded level name
      m_szCurLevelName = "";

      // clear current game level
      m_pCurGameLevel = null;

      // reset the current attach node
      m_pCurAttachNode = null;

      // set to idle state
      m_eCurState = States.STATES_IDLE;

      // create the level info list
      m_aLevelConfigDataList = new  Dictionary< string, LevelConfigData >(4);

      // create level switching data
      m_pCurLevelSwitchingData = new LevelSwitchingData();

      // try to load in LevelData.json file
      TextAsset _pLevelDataJsonFile = Resources.Load("LevelData") as TextAsset; // an TextAsset: the representation of a Textural or Binary File
      if(null == _pLevelDataJsonFile)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelMgr][doInitialize]: Can NOT find LevelData.json file");

        return;
      }

      // create and parse the json data at the same time
      JSONObject _pLevelDataJson = new JSONObject(_pLevelDataJsonFile.text);

      // if LevelData json does NOT contain LevelInfoList element
      if(false == _pLevelDataJson.HasField("LevelInfoList"))
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelMgr][doInitialize]: Can NOT find LevelInfoList element in LevelData.json file");

        return;
      }

      // if LevelInfoList element is NOT an array
      if(false == _pLevelDataJson["LevelInfoList"].IsArray)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelMgr][doInitialize]: LevelInfoList element is NOT an array");

        return;
      }

      // get LevelInfoList block
      JSONObject _pLevelInfoListJsonBlock = _pLevelDataJson["LevelInfoList"];

      // iterate over all elements of LevelInfoList element
      for(int _iCurLevelInfoIdx = 0; _iCurLevelInfoIdx < _pLevelInfoListJsonBlock.Count; _iCurLevelInfoIdx++)
      {
        // get the current level info json block
        JSONObject _pCurLevelInfoJsonBlock = _pLevelInfoListJsonBlock[_iCurLevelInfoIdx];

        // if the current level info json block NOT has a Name field: go to next one
        if(false == _pCurLevelInfoJsonBlock.HasField("Name"))
        {
          DefaultGameLogger.Instance.dumpLogError("[GameLevelMgr][doInitialize]: No Name element in " + _iCurLevelInfoIdx + "th element");

          continue;
        }

        // if the current level info json block NOT has a DiscountKey field: go to next one
        if(false == _pCurLevelInfoJsonBlock.HasField("DiscountKey"))
        {
          DefaultGameLogger.Instance.dumpLogError("[GameLevelMgr][doInitialize]: No DiscountKey element in " + _iCurLevelInfoIdx + "th element");

          continue;
        }

        // create a new LevelInfo with the current level name
        LevelConfigData _pCurLevelInfo = new LevelConfigData(_pCurLevelInfoJsonBlock["Name"].str, _pCurLevelInfoJsonBlock["DiscountKey"].str);

        // try to read in PreloadAssetList
        if(_pCurLevelInfoJsonBlock.HasField("PreloadAssetList"))
        {
          JSONObject _pPreloadAssetListJsonBlock = _pCurLevelInfoJsonBlock["PreloadAssetList"];

          // PreloadAssetList is an array
          if(true == _pPreloadAssetListJsonBlock.IsArray)
          {
            // iterate over all assets in PreloadAssetList
            for(int _iCurAssetIdx = 0; _iCurAssetIdx < _pPreloadAssetListJsonBlock.Count; _iCurAssetIdx++)
            {
              // read in ID
              JSONObject _pCurPreloadAssetJsonBlock = _pPreloadAssetListJsonBlock[_iCurAssetIdx];
              if(true == _pCurPreloadAssetJsonBlock.IsNumber)
              {
                _pCurLevelInfo.PreloadAssetList.Add((int)_pCurPreloadAssetJsonBlock.i);
              }
            }
          }
        }

        // try to read in PreloadEffectList
        if(_pCurLevelInfoJsonBlock.HasField("PreloadEffectList"))
        {
          JSONObject _pPreloadEffectListJsonBlock = _pCurLevelInfoJsonBlock["PreloadEffectList"];

          // PreloadEffectList is an array
          if(true == _pPreloadEffectListJsonBlock.IsArray)
          {
            // iterate over all effects in PreloadEffectList
            for(int _iCurFxIdx = 0; _iCurFxIdx < _pPreloadEffectListJsonBlock.Count; _iCurFxIdx++)
            {
              // read in ID
              JSONObject _pCurPreloadEffectJsonBlock = _pPreloadEffectListJsonBlock[_iCurFxIdx];
              if(true == _pCurPreloadEffectJsonBlock.IsNumber)
              {
                _pCurLevelInfo.PreloadEffectList.Add((int)_pCurPreloadEffectJsonBlock.i);
              }
            }
          }
        }

        // store the current level info
        m_aLevelConfigDataList.Add(_pCurLevelInfo.Name, _pCurLevelInfo);
      }

      // set initialization flag
      m_bIsInitialized = true;
    }

    // update function
    public override void doUpdate()
    {
      switch(m_eCurState)
      {
        case States.STATES_COOLDOWN_BEFORE_DIFFLEVEL_SWITCHING:
        case States.STATES_COOLDOWN_BEFORE_SAMELEVEL_SWITCHING:
        {
          // decrease cool down time
          m_fCurCoolDownTime -= Time.deltaTime;

          // done with cool down
          if(m_fCurCoolDownTime < 1.0e-5f)
          {
            // change to prepare state
            if(States.STATES_COOLDOWN_BEFORE_DIFFLEVEL_SWITCHING ==  m_eCurState)
            {
              m_eCurState = States.STATES_PREPARE_SWITCHING_LEVEL;  
            }
            else
            {
              m_eCurState = States.STATES_SWITCHING_LEVEL;
            }
          }

          break;
        }

        case States.STATES_PREPARE_SWITCHING_LEVEL:
        {
          // start level switching
          // load the given level in background
          // NOTE: NOT use LoadScene(), because it is NOT a blocking operation: returns immediately with isLoaded == false: have to spin on this flag. Useless
          // NOTE II: LoadSceneXYZ() will unload the MainScene as well. So any SceneManager.GetSceneByName(), SceneManager.GetSceneAt() for MainScene will return invalid scene
          m_pLoadingOp = SceneManager.LoadSceneAsync(m_szCurLevelName);

          // change to level switching state
          m_eCurState = States.STATES_SWITCHING_LEVEL;

          break;
        }

        case States.STATES_SWITCHING_LEVEL:
        {
          // get the level instance
          Scene _pCurLoadedLevel = SceneManager.GetSceneByName(m_szCurLevelName);

          // still background loading of .unity level
          if(false == _pCurLoadedLevel.isLoaded)
          {
            if(m_pCurLevelSwitchingData.CurLevelSwitchingHandler != null)
            {
              m_pCurLevelSwitchingData.CurLevelSwitchingHandler(LevelSwitchingStates.LEVEL_SWITCHING_STATES_PROCESSING, m_pLoadingOp.progress * 50.0f); // level loading: [0, 50]
            }

            return;
          }

          // done with .unity level loading
          // set progress to 50%
          if(m_pCurLevelSwitchingData.CurLevelSwitchingHandler != null)
          {
            m_pCurLevelSwitchingData.CurLevelSwitchingHandler(LevelSwitchingStates.LEVEL_SWITCHING_STATES_PROCESSING, 50.0f); // level loading: [0, 50]
          }

          // create SceneNode Node
          // NOTE: the SceneNode PreFab asset(ARScene or NormScene) will be put into the currently loaded/active scene: for example FarmSummer-SunRise
          // because, MainScene is UNLOADED at this moment
          GameObject _pSceneNode = null;
          switch(m_pCurLevelSwitchingData.RequiredCameraMode)
          {
            case CameraModes.CAMERA_MODES_REGULAR:
            {
              // try to load up SceneNode PreFab directly from GameAssetMgr
              // NOTE: right now, CurrentGameLevel is still null
              if(false == GameAssetMgr.Instance.loadAsset(m_pGameMainLevelInfo.MainEntry.NormSceneNodeID, out _pSceneNode))
              {
#if !RETAIL
                DefaultGameLogger.Instance.dumpLogInfo("[GameLevelMgr][doUpdate][STATES_SWITCHING_LEVEL]: can NOT load NormScene PreFab: " + m_pGameMainLevelInfo.MainEntry.NormSceneNodeID);
#endif

                return;
              }

              break;
            }

          case CameraModes.CAMERA_MODES_AR_SUPPORT:
            {
              // try to load up SceneNode PreFab directly from GameAssetMgr
              // NOTE: right now, CurrentGameLevel is still null
              if(false == GameAssetMgr.Instance.loadAsset(m_pGameMainLevelInfo.MainEntry.ARSceneNodeID, out _pSceneNode))
              {
#if !RETAIL
                DefaultGameLogger.Instance.dumpLogInfo("[GameLevelMgr][doUpdate][STATES_SWITCHING_LEVEL]: can NOT load ARScene PreFab: " + m_pGameMainLevelInfo.MainEntry.ARSceneNodeID);
#endif

                return;
              }

              break;
            }

          case CameraModes.CAMERA_MODES_SUPPORT_VR:
            {
              // try to load up SceneNode PreFab directly from GameAssetMgr
              // NOTE: right now, CurrentGameLevel is still null
#if !RETAIL
              DefaultGameLogger.Instance.dumpLogInfo("[GameLevelMgr][doUpdate][STATES_SWITCHING_LEVEL]: NO implementation for VRScene");
#endif

              return;
            }
          }

          // clone the SceneNode also the name: put the loaded SceneNode into current Scene
          m_pGameMainLevelInfo.SceneNode = Object.Instantiate(_pSceneNode);
          m_pGameMainLevelInfo.SceneNode.name = _pSceneNode.name;

          // disable the cloned SceneNode
          m_pGameMainLevelInfo.SceneNode.SetActive(false);

          // loaded up SceneNode: read in the whole information of this SceneNode
          readSceneNodeInfo();

          // disable all camera nodes
          // disable all light nodes
          // get the root node list
          // create the root node list
          GameObject[] _aRootNodeList = new GameObject[_pCurLoadedLevel.rootCount];
          // read in all root nodes
          _aRootNodeList = _pCurLoadedLevel.GetRootGameObjects();

          // iterate over all root nodes
          for(int _iNodeIdx = 0; _iNodeIdx < _aRootNodeList.Length; _iNodeIdx++)
          {
            // disable camera node, which has NULL Target Texture: NOT disable cameras used for offscreen rendering
            // current root node has Camera Component
            if(null != _aRootNodeList[_iNodeIdx].GetComponentInChildren< UnityEngine.Camera >()
               &&
               null == _aRootNodeList[_iNodeIdx].GetComponentInChildren< UnityEngine.Camera >().targetTexture
              )
            {
              // if the required camera mode is NON-AR mode, copy this camera's setttings from the loaded game level into SceneNode's Camera within the game main level
              if(Game.Camera.CameraModes.CAMERA_MODES_REGULAR == m_pCurLevelSwitchingData.RequiredCameraMode)
              {
                // try to get the camera used during the game play
                UnityEngine.Camera _pGamePlayCamera = m_pGameMainLevelInfo.CameraNode.GetComponentInChildren< UnityEngine.Camera >();
                if(_pGamePlayCamera != null)
                {
                  // get the game level's camera
                  UnityEngine.Camera _pLevelCamera = _aRootNodeList[_iNodeIdx].GetComponentInChildren< UnityEngine.Camera >();

                  // copy all camera's settings
                  _pGamePlayCamera.cameraType         = _pLevelCamera.cameraType;
                  _pGamePlayCamera.clearFlags         = _pLevelCamera.clearFlags;
                  _pGamePlayCamera.backgroundColor    = _pLevelCamera.backgroundColor;
                  _pGamePlayCamera.cullingMask        = _pLevelCamera.cullingMask;
                  _pGamePlayCamera.fieldOfView        = _pLevelCamera.fieldOfView;
                  _pGamePlayCamera.nearClipPlane      = _pLevelCamera.nearClipPlane;
                  _pGamePlayCamera.farClipPlane       = _pLevelCamera.farClipPlane;
                  _pGamePlayCamera.transform.position = _pLevelCamera.transform.position;
                  _pGamePlayCamera.transform.rotation = _pLevelCamera.transform.rotation;

                  // copy camera controller's settings from the level camera
                  if(_pLevelCamera.GetComponent< Game.Camera.CameraController >())
                  {
                    m_pGameMainLevelInfo.CameraController.MaxHorzRotAngle = _pLevelCamera.GetComponent< Game.Camera.CameraController >().MaxHorzRotAngle;
                    m_pGameMainLevelInfo.CameraController.MaxVertRotAngle = _pLevelCamera.GetComponent< Game.Camera.CameraController >().MaxVertRotAngle;
                    m_pGameMainLevelInfo.CameraController.HorzRotScale    = _pLevelCamera.GetComponent< Game.Camera.CameraController >().HorzRotScale;
                    m_pGameMainLevelInfo.CameraController.VertRotScale    = _pLevelCamera.GetComponent< Game.Camera.CameraController >().VertRotScale;
                  }
                }
              }

              // disable the camera of the loaded game level
              if(true == _aRootNodeList[_iNodeIdx].activeSelf)
              {
#if !RETAIL
                DefaultGameLogger.Instance.dumpLogInfo("[GameLevelMgr][doUpdate][STATES_SWITCHING_LEVEL][" + _aRootNodeList[_iNodeIdx].ToString() + "]: node has Camera component. Disable it during scene loading");
#endif

                _aRootNodeList[_iNodeIdx].GetComponentInChildren< UnityEngine.Camera >().gameObject.SetActive(false);  
              }
            }  

            // disable light's containing node
            // current root node has Light Component
            if(null != _aRootNodeList[_iNodeIdx].GetComponentInChildren< Light >())
            {
              // it is enabled / acitve
              if(true == _aRootNodeList[_iNodeIdx].activeSelf)
              {
#if !RETAIL
                DefaultGameLogger.Instance.dumpLogInfo("[GameLevelMgr][doUpdate][STATES_SWITCHING_LEVEL][" + _aRootNodeList[_iNodeIdx].ToString() + "]: node has Light component. Disable it during scene loading");
#endif

                _aRootNodeList[_iNodeIdx].GetComponentInChildren< Light >().gameObject.SetActive(false);
              }
            }

            // try to find the GameLevel component of the currrently loaded game level
            if(_aRootNodeList[_iNodeIdx].GetComponent< GameLevel >() != null)
            {
              if(m_pCurGameLevel != null)
              {
                DefaultGameLogger.Instance.dumpLogError("[GameLevelMgr][doUpdate][STATES_SWITCHING_LEVEL]: Duplicated Game Level in Level: " + m_szCurLevelName);
              }

              // store game level
              m_pCurGameLevel = _aRootNodeList[_iNodeIdx].GetComponent< GameLevel >();

              // store the camera mode
              m_pCurGameLevel.CameraMode = m_pCurLevelSwitchingData.RequiredCameraMode;

              // attach level node onto SceneRoot node
              // REMOVE the level node out of scene file: .unity file
              // regular camera: attach to SceneRoot node
              if(CameraModes.CAMERA_MODES_REGULAR == m_pCurGameLevel.CameraMode)
              {
                // attach current level node onto SceneRoot node
                attachLevelNode(m_pGameMainLevelInfo.SceneNode.transform.Find("SceneRoot").gameObject);
              }

              // AR camera: attach to FIRST child of SceneRoot node
              else
              {
                // attach current level node onto the FIRST child of SceneRoot node
                attachLevelNode(m_pGameMainLevelInfo.SceneNode.transform.Find("SceneRoot").transform.GetChild(0).gameObject);
              }
            }
          }

          // if this scene does NOT contain a GameLevel node
          if(null == m_pCurGameLevel)
          {
            DefaultGameLogger.Instance.dumpLogError("[GameLevelMgr][doUpdate][STATES_SWITCHING_LEVEL]: " + m_szCurLevelName +" scene does NOT has a node having GameLevel component");

            return;
          }

          // warm up current game level: initial GUI and so on
          m_pCurGameLevel.doWarmUp(m_pCurLevelSwitchingData);

          // initialize camera controller based on camera mode: it needs Stats text field of current GUI
          m_pGameMainLevelInfo.CameraController.doInitialize();

          // switching done
          // tell switching handler
          if(m_pCurLevelSwitchingData.CurLevelSwitchingHandler != null)
          {
            m_pCurLevelSwitchingData.CurLevelSwitchingHandler(LevelSwitchingStates.LEVEL_SWITCHING_STATES_DONE, 100.0f);
          }

          // fire game level switched event
          GameEventMgr.Instance.fireGlobalScopeEvent(Game.Event.EventIDs.EVENT_IDS_LOGIC_LEVEL_SWITCHED,
                                                     m_pCurGameLevel,
                                                     Game.Event.EventTriggerType.EVENT_TRIGGER_TYPE_LOCAL,
                                                     this
                                                    );

          // clear up all temporary data
          // clear up level loading op
          m_pLoadingOp = null;

          // clear up current switching data
          m_pCurLevelSwitchingData.CurrLevelConfigData = null;
          m_pCurLevelSwitchingData.CurLevelSwitchingHandler = null;

          // change back to idle state
          m_eCurState = States.STATES_IDLE;

          return;
        }

        default:
        {
          return;
        }
      }
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS
    #endregion // PROTECTED METHODS

    #region PRIVATE METHODS
    // return in the currently loaded SceneNode's information
    // return true on success
    bool readSceneNodeInfo()
    {
      // try to find the camera controller
      m_pGameMainLevelInfo.CameraController = m_pGameMainLevelInfo.SceneNode.transform.GetComponentInChildren< CameraController >();
      if(null == m_pGameMainLevelInfo.CameraController)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelMgr][readSceneNodeInfo]: NO Camera Node under NormScene node");

        return false;
      }

      // store the normal camera node
      m_pGameMainLevelInfo.CameraNode = m_pGameMainLevelInfo.CameraController.gameObject;

      // check render camera component
      if(true == m_pGameMainLevelInfo.CameraController.GetComponentInChildren< UnityEngine.Camera >().enabled)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelMgr][readSceneNodeInfo]: Render Camera component under NormScene node PreFab is enabled. Please disable IT");

        return false;
      }

      return true;
    }

    // read in the information of the given attach node
    void readAttachNodeInfo(GameObject a_pAttachNode)
    {
      // try to find DropOffs node under the given attach node
      m_pGameMainLevelInfo.DropOffsNode = a_pAttachNode.transform.FindChild("DropOffs").gameObject;
      if(null == m_pGameMainLevelInfo.DropOffsNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelMgr][readAttachNodeInfo]: NO DropOffs node within Attach Node: " + a_pAttachNode.name);

        return;
      }

      // try to find Effects node under the given attach node
      m_pGameMainLevelInfo.EffectsNode = a_pAttachNode.transform.FindChild("Effects").gameObject;
      if(null == m_pGameMainLevelInfo.EffectsNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelMgr][readAttachNodeInfo]: NO Effects node within Attach Node: " + a_pAttachNode.name);

        return;
      }

      // try to find ShootOuts node under the given attach node
      m_pGameMainLevelInfo.ShootOutsNode = a_pAttachNode.transform.FindChild("ShootOuts").gameObject;
      if(null == m_pGameMainLevelInfo.ShootOutsNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelMgr][readAttachNodeInfo]: NO ShootOuts node within Attach Node: " + a_pAttachNode.name);

        return;
      }
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    enum States
    {
      STATES_IDLE,
      STATES_COOLDOWN_BEFORE_DIFFLEVEL_SWITCHING,
      STATES_COOLDOWN_BEFORE_SAMELEVEL_SWITCHING,
      STATES_PREPARE_SWITCHING_LEVEL,
      STATES_SWITCHING_LEVEL,
    }

    // maximal cool down time before loading a new level
    static float MAX_COOLDOWN_TIME = 1.0f; // 1 second

    // whole level config data list
    Dictionary< string, LevelConfigData > m_aLevelConfigDataList;
    // current state
    States m_eCurState;
    // level info for game main level: MainScene.unity
    MainLevelInfo m_pGameMainLevelInfo;

    // current level name
    string m_szCurLevelName;
    // current game level, we are playing
    GameLevel m_pCurGameLevel;

    // current attach node of the whole level
    GameObject m_pCurAttachNode;

    // level switching data
    LevelSwitchingData m_pCurLevelSwitchingData;
    // level loading operation
    AsyncOperation m_pLoadingOp;
    // current cool down timer
    float m_fCurCoolDownTime;
    #endregion // PRIVATE DATA MEMBERS
  }

}// namespace Game.Logic

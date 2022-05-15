/**
 * Game Main Entry Class
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Framework.Utils;
using Framework.Config;
using Framework.Inputs;
using Framework.Logger;
using Game.Logic;
using Game.Event;
using Game.Ads;
using System.Collections.Generic;

enum GameStates
{
  GAME_STATES_ERRORS = -1,

  // idle state
  GAME_STATES_IDLE,

  // waiting state
  GAME_STATES_WAITING_FOR_RESPONSE,

  // initializing game
  GAME_STATES_INITIALIZING,

  // GAME LEVEL
  // loading game level
  GAME_STATES_LOADING_GAMELEVEL,
  // finished game level loading
  GAME_STATES_FINISH_LOADING_GAMELEVEL,
  // playing game level
  GAME_STATES_PLAYING_GAMELEVEL,
  // won game level
  GAME_STATES_WON_GAMELEVEL,
  // failed game level
  GAME_STATES_FAILED_GAMELEVEL,
}

// everyplay record supporting states
public enum GameRecordStates
{
  // checking in progress
  GAME_RECORD_STATES_CHECKING,
  // NOT supported
  GAME_RECORD_STATES_NOT_AVAILABLE,
  // supported
  GAME_RECORD_STATES_SUPPORTED,
}

public delegate void EveryplayInitializationDoneDelegate(bool a_bIsSupported);

public class GameMain : MonoBehaviour
{
  #region PUBLIC DATA MEMBERS
  // normal scene node asset ID
  public int NormSceneNodeID = 400;
  // AR scene node asset ID
  public int ARSceneNodeID = 396;
  // time delay befor wave starting
  public float WaveStartDelay = 10.0f;
  // time delay between wave switching: WaveA --> Delay --> WaveB
  public float WaveSwitchDelay = 3.0f;
  #endregion // PUBLIC DATA MEMBERS
 
  #region PUBLIC METHODS
  /**
   * get everyplay game record state
   */
  public GameRecordStates GameRecordState
  {
    get
    {
      return m_eGameRecordState;
    }
  }

  /**
   * load the given game level scene
   */
  public void loadGameLevel(string a_szLevelName, Game.Camera.CameraModes a_eRequiredCameraMode)
  {
    // start loading a game level
    // set loading flag
    m_bIsGameLevelLoading = true;

    // reset all data member before loading
    // enable loading text
    m_pLoadingText.SetActive(true);

    // disable start button
    m_pStartBtn.SetActive(false);

    // change to idle state before start loading
    m_eCurState = GameStates.GAME_STATES_IDLE;

    // enable GameMainGUI in main scene
    m_pGameLevelMgr.GameMainLevel.GameMainGUI.SetActive(true);

    // set loading progress to 0%
    updateProgressBar(0.0f);

    // unpause game
    unPauseGamePlay();

    // loading the given level
    m_pGameLevelMgr.switchToLevel(a_szLevelName, a_eRequiredCameraMode, onLevelLoading);

    // enable game main camera
    m_pGameLevelMgr.GameMainLevel.GameMainCamera.SetActive(true);

    // set to loading state
    m_eCurState = GameStates.GAME_STATES_LOADING_GAMELEVEL;
  }

  // pause the game
  public void pauseGamePlay()
  {
    Time.timeScale = 0.0f;

    // set pause flag
    m_bIsGamePaused = true;

#if !RETAIL
    //DefaultGameLogger.Instance.dumpLogInfo("[GameMain][pauseGamePlay]");
#endif
  }

  // unpause the game
  public void unPauseGamePlay()
  {
    Time.timeScale = 1.0f;

    // reset pause flag
    m_bIsGamePaused = false;

#if !RETAIL
    //DefaultGameLogger.Instance.dumpLogInfo("[GameMain][unPauseGamePlay]");
#endif
  }

  // callback if game start button clicked
  public void onStartBtnClicked()
  {
    // disable GameMainGUI in main scene
    m_pGameLevelMgr.GameMainLevel.GameMainGUI.SetActive(false);

    // entry current loaded level
    m_pGameLevelMgr.CurrentGameLevel.doEnter();

    // enable the dedicated SceneNode based on AR mode
    m_pGameLevelMgr.GameMainLevel.SceneNode.SetActive(true);

    // NOT PAU: show banner before loading level
    if(false == Game.Logic.GameSaveMgr.Instance.CurrentGameSave.IsPAU)
    {
      m_pGameAdsMgr.showBanner();
    }

    // disable game main camera
    m_pGameLevelMgr.GameMainLevel.GameMainCamera.SetActive(false);

    // change to game playing state
    m_eCurState = GameStates.GAME_STATES_PLAYING_GAMELEVEL;
  }

  // callback if the current AR marker is lost
  public void onARMarkerLost(string a_szMarkerName, GameObject a_pAttachNode)
  {
    // if a game level is loading: return
    if(true == m_bIsGameLevelLoading)
    {
      return;
    }

    // fire marker lost event
    ARMarkerData _pMarkerData = new ARMarkerData();
    _pMarkerData.MarkerName = a_szMarkerName;
    _pMarkerData.AttachNode = a_pAttachNode;

    GameEventMgr.Instance.fireGlobalScopeEvent(Game.Event.EventIDs.EVENT_IDS_LOGIC_AR_MARKER_LOST,
                                               _pMarkerData,
                                               Game.Event.EventTriggerType.EVENT_TRIGGER_TYPE_LOCAL,
                                               this
                                              );

    // pause the game play
    pauseGamePlay();
  }

  // callback if an AR marker is found
  public void onARMarkerFound(string a_szMarkerName, GameObject a_pAttachNode)
  {
    // if a game level is loading: return
    if(true == m_bIsGameLevelLoading)
    {
      return;
    }

    // unpause the game play
    unPauseGamePlay();

    // fire marker found event
    ARMarkerData _pMarkerData = new ARMarkerData();
    _pMarkerData.MarkerName = a_szMarkerName;
    _pMarkerData.AttachNode = a_pAttachNode;

    GameEventMgr.Instance.fireGlobalScopeEvent(Game.Event.EventIDs.EVENT_IDS_LOGIC_AR_MARKER_FOUND,
                                               _pMarkerData,
                                               Game.Event.EventTriggerType.EVENT_TRIGGER_TYPE_LOCAL,
                                               this
                                              );
  }

  // callback if the sponsor button is click
  public void onShowSponsorPage()
  {
    Application.OpenURL("http://bittreasure.weebly.com"); 
  }

  // callback if the DunFenCE button is click
  public void onShowDunFenceCodePage()
  {
    Application.OpenURL("http://u3d.as/Bhv"); 
  }

  // callback if the gameplay guide panel will be shown
  public void onShowGamePlayGuidePanel()
  {
    // disable all nodes under ModalDialogPanel
    for(int _iNIdx = 0; _iNIdx < m_pModalDialogPanel.transform.childCount; _iNIdx++)
    {
      m_pModalDialogPanel.transform.GetChild(_iNIdx).gameObject.SetActive(false);
    }

    // enable ModalDialogPanel
    m_pModalDialogPanel.SetActive(true);

    // show gameplay guide panel
    m_pGamePlayGuidePanel.SetActive(true);
  }

  // callback if the gameplay guide panel will be closed
  public void onCloseGamePlayGuidePanel()
  {
    // hide the gameplay guide panel
    m_pGamePlayGuidePanel.SetActive(false);

    // disable ModelDialogPanel
    m_pModalDialogPanel.SetActive(false);
  }

  // toggle game play: normal <--> paused
  // return true, if the game has been paused
  public bool onToggleGamePlay()
  {
    // in normal game play
    if((1.0f - Time.timeScale) < 1.0e-5f)
    {
      // pause the game
      Time.timeScale = 0.0f;

      return true;
    }
    else
    {
      // otherwise, let it run in normal speed
      Time.timeScale = 1.0f;

      return false;
    }
  }

  // register everyplay initialization delegate
  public void registerEveryplayCheckDelegate(EveryplayInitializationDoneDelegate a_pDoneCB)
  {
    m_pEveryplayInitializationDoneCB += a_pDoneCB;
  }

  // unRegister everyplay initialization delegate
  public void unRegisterEveryplayCheckDelegate(EveryplayInitializationDoneDelegate a_pDoneCB)
  {
    m_pEveryplayInitializationDoneCB -= a_pDoneCB;
  }

   #region PUBLIC OVERRIDDENS
  /**
   * Checking function: check if all needed components are available
   * Awake() will be called when the script is initialized before game starts
   * Awake() will be called only once during the lifetime of the script instance
   * Awake() will be called after all objects are initialized
   * Awake() will be called out of order: in random order
   */
  public virtual void Awake()
  {
    // initialize ConfigMgr as the first step before the game logic starting
    // if ConfigMgr is NOT initialized, initialize it
    if(false == ConfigMgr.Instance.isInitialized)
    {
      ConfigMgr.Instance.doInitialize();
    }

    // initialize device input manager, before starting game logic
    m_pDeviceInputMgr = DeviceInputMgr.Instance;
    m_pDeviceInputMgr.doInitialize();
  }

  /**
   * Initialize function
   * Start() will be called exactly ONCE in the lifetime of the script
   * Start() will be called in TWO cases:
   * - Attached to any GameObject:
   *   + Start() will be called when the script is ENABLED
   * - Called directly from other code: 
   *   + do the initialization
   */
  public virtual void Start()
  {
    // set to initializing state
    m_eCurState = GameStates.GAME_STATES_INITIALIZING;
  }

  /**
   * Update function: called each frame
   * Update() will be called every frame, if the script is ENBALED
   * use Time.deltaTime to get the time elapsed from last Update() call
   */
  public virtual void Update()
  {
    // update device input manager
    if(null == m_pDeviceInputMgr)
    {
      m_pDeviceInputMgr = DeviceInputMgr.Instance;

      if(false == m_pDeviceInputMgr.isInitialized)
      {
        m_pDeviceInputMgr.doInitialize();
      }
    }

    m_pDeviceInputMgr.doUpdate();

    switch(m_eCurState)
    {
      case GameStates.GAME_STATES_INITIALIZING:
      {
        // --- INITIALIZE ALL MANAGERS ---
        // initialize game save manager at first
        GameSaveMgr.Instance.doInitialize();

        // initialize store item manager
        IAPItemPurchaseMgr.Instance.doInitialize();

        // initialize game ads manager
        m_pGameAdsMgr = GameAdsMgr.Instance;
        m_pGameAdsMgr.doInitialize();

        // initialize game event manager
        GameEventMgr.Instance.doInitialize();

        // initialize game asset manager
        GameAssetMgr.Instance.doInitialize();

        // initialize weapon manager
        WeaponMgr.Instance.doInitialize();

        // initialize game level manager
        m_pGameLevelMgr = GameLevelMgr.Instance;
        m_pGameLevelMgr.doInitialize();
        // load main level info
        GameLevelMgr.Instance.loadMainLevelInfo(gameObject);

        // initialize game wave manager
        GameWaveMgr.Instance.doInitialize();

        // initialize drop off manager
        DropOffMgr.Instance.doInitialize();

        // initialize monster manager
        MonsterMgr.Instance.doInitialize();

        // initialize projectile manager
        ProjectileMgr.Instance.doInitialize();

        // initialize effect manager
        EffectMgr.Instance.doInitialize();

        // register everyplay ReadyForRecording event
        Everyplay.ReadyForRecording += onEveryPlayInitializationDone;

        // set current gameplay record state
        m_eGameRecordState = GameRecordStates.GAME_RECORD_STATES_CHECKING;

        // check game main camera
        if(null == m_pGameLevelMgr.GameMainLevel.GameMainCamera)
        {
          DefaultGameLogger.Instance.dumpLogError("[GameMain]: Can NOT find GameMainCamera node");

          m_eCurState = GameStates.GAME_STATES_ERRORS;

          break;
        }

        // try to find LoadingText
        m_pLoadingText = GameObjectUtils.FindNode(m_pGameLevelMgr.GameMainLevel.GameMainGUI, "LoadingText");
        if(null == m_pLoadingText)
        {
          DefaultGameLogger.Instance.dumpLogError("[GameMain]: Can NOT find LoadingText node within GameMainGUI node");

          m_eCurState = GameStates.GAME_STATES_ERRORS;

          break;
        }

        // try to find StartBtn
        m_pStartBtn= GameObjectUtils.FindNode(m_pGameLevelMgr.GameMainLevel.GameMainGUI, "StartBtn");
        if(null == m_pStartBtn)
        {
          DefaultGameLogger.Instance.dumpLogError("[GameMain]: Can NOT find StartBtn node within GameMainGUI node");

          m_eCurState = GameStates.GAME_STATES_ERRORS;

          break;
        }

        // try to find progress panel
        GameObject _pGUINode = GameObjectUtils.FindNode(m_pGameLevelMgr.GameMainLevel.GameMainGUI, "ProgressPanel");
        if(null == _pGUINode)
        {
          DefaultGameLogger.Instance.dumpLogError("[GameMain]: Can NOT find ProgressPanel node within GameMainGUI node");

          m_eCurState = GameStates.GAME_STATES_ERRORS;

          break;
        }

        m_pProgressPanel = _pGUINode.GetComponent< Game.GUI.ProgressPanel >();
        if(null == m_pProgressPanel)
        {
          DefaultGameLogger.Instance.dumpLogError("[GameMain]: ProgressPanel node does NOT have ProgressPanel.cs attached");

          m_eCurState = GameStates.GAME_STATES_ERRORS;

          break;
        }

        // initialize progress panel
        m_pProgressPanel.doInitialize();

        // try to find ModalDialogPanel
        m_pModalDialogPanel = GameObjectUtils.FindNode(m_pGameLevelMgr.GameMainLevel.GameMainGUI, "ModalDialogPanel");
        if(null == m_pModalDialogPanel)
        {
          DefaultGameLogger.Instance.dumpLogError("[GameMain]: Can NOT find ModalDialogPanel Node");

          return;
        }

        // make ModalDialogPanel is the lastest child in its parent's children chain
        if(m_pModalDialogPanel.transform.GetSiblingIndex() != m_pModalDialogPanel.transform.parent.childCount - 1)
        {
          DefaultGameLogger.Instance.dumpLogWarning("[GameMain]: ModalDialogPanel Node is NOT the TOPPEST node in GameMain. Changed with Code");

          m_pModalDialogPanel.transform.SetAsLastSibling();
        }

        // disable the ModelDialogPanel by default
        m_pModalDialogPanel.SetActive(false);

        // try to find GamePlayGuidePanel
        m_pGamePlayGuidePanel = GameObjectUtils.FindNode(m_pModalDialogPanel, "GamePlayGuidePanel");
        if(null == m_pGamePlayGuidePanel)
        {
          DefaultGameLogger.Instance.dumpLogError("[GameMain]: Can NOT find GamePlayGuidePanel node within ModalDialogPanel Node");

          return;
        }

        /**
         * disable destroying on all nodes within MainScene.unity
         * - GameMain
         * - GameMainGUI
         * - GameMainCamera
         * - GUIEventSystem
         */
        Object.DontDestroyOnLoad(gameObject);   // GameMain                       // mark all hierarchy as NOT destroyable
        Object.DontDestroyOnLoad(m_pGameLevelMgr.GameMainLevel.GameMainCamera);   // mark all hierarchy as NOT destroyable
        Object.DontDestroyOnLoad(m_pGameLevelMgr.GameMainLevel.GameMainGUI);      // mark all hierarchy as NOT destroyable
        Object.DontDestroyOnLoad(m_pGameLevelMgr.GameMainLevel.GUIEventSystem);   // mark all hierarchy as NOT destroyable

        // load up inventory scene in normal mode
        loadGameLevel("DemoScene", Game.Camera.CameraModes.CAMERA_MODES_REGULAR);

        break;
      }

      case GameStates.GAME_STATES_LOADING_GAMELEVEL:
      {
        // still loading a game level
        // update game level manager: to finish loading up the given level and get feedback
        m_pGameLevelMgr.doUpdate();

        break;
      }

      case GameStates.GAME_STATES_FINISH_LOADING_GAMELEVEL:
      {
        // done with game level loading
        // store the current loaded level
        m_pCurLevel = GameLevelMgr.Instance.CurrentGameLevel;

        // disable loading text
        m_pLoadingText.SetActive(false);

        // enable start button
        m_pStartBtn.SetActive(true);

        // change to waiting state
        m_eCurState = GameStates.GAME_STATES_WAITING_FOR_RESPONSE;

        break;
      }

      case GameStates.GAME_STATES_WAITING_FOR_RESPONSE:
      {
        // waiting for user's response
        // handle all pending device events
        m_pDeviceInputMgr.handleAllEvents();

        break;
      }

      case GameStates.GAME_STATES_PLAYING_GAMELEVEL:
      {
        // still playing the game level
        // update the current level
        m_pCurLevel.doUpdate();

        break;
      }

      default:
      {
        break;
      }
    }
  }
  
  /**
   * Physical Update function: 50Hz or 0.02 second
   * FixedUpdate() will be called at fixed frame-rate, if the script is ENABLED
   * FixedUpdate() will be called at 50Hz: each 0.02 second
   * use Time.deltaTime to get the delta time instead of Time.fixedDeltaTime, 
   * because Time.deltaTime automatically returns the right delta time if you are inside a FixedUpdate() or Update()
   */
  public virtual void FixedUpdate()
  {

  }

  /**
   * Post-Update function: called each frame after all Update() functions
   * LateUpdate() will be called after ALL Update() functions called
   */
  public virtual void LateUpdate()
  {
    // post update device input manager
    if(null == m_pDeviceInputMgr)
    {
      m_pDeviceInputMgr = DeviceInputMgr.Instance;

      if(false == m_pDeviceInputMgr.isInitialized)
      {
        m_pDeviceInputMgr.doInitialize();
      }
    }

    m_pDeviceInputMgr.doPostUpdate();  

    switch(m_eCurState)
    {
      case GameStates.GAME_STATES_PLAYING_GAMELEVEL:
      {
        m_pCurLevel.doPostUpdate();

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

  #region PRIVATE METHODS
  // callback from GameLevelMgr during level loading
  void onLevelLoading(LevelSwitchingStates a_eCurState, float a_fProgress)
  {
    // update progress bar
    updateProgressBar(a_fProgress);

    if(LevelSwitchingStates.LEVEL_SWITCHING_STATES_DONE == a_eCurState)
    {
      switch(m_eCurState)
      {
      case GameStates.GAME_STATES_LOADING_GAMELEVEL:
        {
          // done with loading of game level
          // clear loading flag
          m_bIsGameLevelLoading = false;

          // change to finish loading state
          m_eCurState = GameStates.GAME_STATES_FINISH_LOADING_GAMELEVEL;

          break;
        }

        default:
        {
          break;
        }
      }
    }  
  }

  void updateProgressBar(float a_fProgress)
  {
    m_pProgressPanel.setProgress(a_fProgress);
  }

  // callback of everyplay: called when the Everyplay initialisation has been completed and the recording support of the current device has been analysed
  void onEveryPlayInitializationDone(bool a_bRecordSupported)
  {
    // set the state
    if(true == a_bRecordSupported)
    {
      m_eGameRecordState = GameRecordStates.GAME_RECORD_STATES_SUPPORTED;
    }
    else
    {
      m_eGameRecordState = GameRecordStates.GAME_RECORD_STATES_NOT_AVAILABLE;
    }

    // invoke callback if any
    if(null != m_pEveryplayInitializationDoneCB)
    {
      m_pEveryplayInitializationDoneCB(GameRecordStates.GAME_RECORD_STATES_SUPPORTED == GameRecordState);

      // clear up all registered callbacks
      m_pEveryplayInitializationDoneCB = null;
    }
  }
  #endregion // PRIVATE METHODS

  #region PRIVATE DATA MEMBERS
  // current state
  GameStates m_eCurState;
  // game ads manager
  GameAdsMgr m_pGameAdsMgr;
  // device input manager
  DeviceInputMgr m_pDeviceInputMgr;
  // game level manager
  GameLevelMgr m_pGameLevelMgr;
  // GUI
  // loading text
  GameObject m_pLoadingText;
  // game start button
  GameObject m_pStartBtn;
  // progress panel
  Game.GUI.ProgressPanel m_pProgressPanel;
  // current loaded game level
  GameLevel m_pCurLevel;
  // current everyplay record state
  GameRecordStates m_eGameRecordState;
  EveryplayInitializationDoneDelegate m_pEveryplayInitializationDoneCB;
  // modal dialog panel
  GameObject m_pModalDialogPanel;
  // gameplay guide panel
  GameObject m_pGamePlayGuidePanel;
  // game pause flag
  bool m_bIsGamePaused;
  // game level loading flag
  bool m_bIsGameLevelLoading;
  #endregion // PRIVATE DATA MEMBERS
}

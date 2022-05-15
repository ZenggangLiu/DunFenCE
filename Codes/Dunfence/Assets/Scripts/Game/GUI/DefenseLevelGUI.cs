/**
 * Game UI used within Defense Level
 */

using UnityEngine;
using UnityEngine.UI;
using Framework.Utils;
using Framework.Logger;
using Framework.Mathe;
using Game.Logic;
using Game.Ads;
using System.Collections.Generic; // Dictionary< SpriteName, Sprite >

namespace Game.GUI
{
  public class DefenseLevelGUI : GameLevelGUI
  {
    #region PUBLIC METHODS
    /**
     * go back to inventory scene
     */
    public void backToInventory()
    {
      // show message box
      showMessageBox(MessageBoxTypes.MESSAGEBOX_TYPES_INFO, "Do you really want back to Inventory?", onDecideOnBackToInventory, null);
    }

    /**
     * reTry the current level
     */
    public void reTryCurrentLevel()
    {
      // hide the DefendFailedDialog
      hideDefendFailedDialog();

      // load current level
      GameLevelMgr.Instance.GameMainLevel.MainEntry.loadGameLevel(GameLevelMgr.Instance.CurrentGameLevel.LevelName, GameLevelMgr.Instance.CurrentGameLevel.CameraMode);
    }

    /**
     * show DefendFailedDialog
     */
    public void showDefendFailedDialog()
    {
      // disable all nodes under ModalDialogPanel
      for(int _iNIdx = 0; _iNIdx < m_pModalDialogPanel.transform.childCount; _iNIdx++)
      {
        m_pModalDialogPanel.transform.GetChild(_iNIdx).gameObject.SetActive(false);
      }

      // popup defend failed dialog
      m_pDefendFailedDialog.SetActive(true);

      // show ModelDialogPanel
      m_pModalDialogPanel.SetActive(true);
    }

    /**
     * watch Ads incentivize video, no matter if PAU
     */
    public void forceWatchingAdsIncentivizeVideo()
    {
      // hide the DefendFailedDialog
      hideDefendFailedDialog();

      // play incentivize video
      GameAdsMgr.Instance.showIncentivizeVideo();
    }

    /**
     * toggle between pause mode and game play mode
     */
    public void onToggleGamePlay()
    {
      // if game play has been paused
      if(GameLevelMgr.Instance.GameMainLevel.MainEntry.onToggleGamePlay())
      {
        // turn on the button highlighter
        m_pPauseBtnHighLighter.startFlashing();
      }
      else
      {
        // turn off the button highlighter
        m_pPauseBtnHighLighter.stopFlashing();
      }
    }

    /**
     * toggle between starting video record and stop video record
     */
    public void onToggleGameRecord()
    {
      // toggle flag
      m_bIsRecording = !m_bIsRecording;

      // toggle flashing
      m_pRecordBtnHighLighter.toggleFlashing();

      // start recording
      if(true == m_bIsRecording)
      {
        Everyplay.StartRecording();
      }
      else
      {
        // stop recording
        Everyplay.StopRecording();

        // add meta data
        Everyplay.SetMetadata("LevelName", GameLevelMgr.Instance.CurrentGameLevel.LevelName);
        Everyplay.SetMetadata("Rewards", GameSaveMgr.Instance.CurrentGameSave.RP);

        // show share dialog
        Everyplay.ShowSharingModal();
      }
    }

    // callback for shooting weapon: when ShootBtn has been pressed
    public void onShootWeapon()
    {
      // NO equipped weapon
      if(null == m_pCurEquippedWeapon)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][onStopShootWeapon]: NO weapon to shoot");

        return;
      }

      // if ShootBtn is ready for use
      if(true == m_bIsShootBtnReady)
      {
        // set keep shooting flag
        m_bKeepShooting = true;

        // shoot the current weapon
        // use the weapon
        m_pCurEquippedWeapon.doUseCom();

        // and stop use the weapon
        m_pCurEquippedWeapon.doStopUseCom();

        // NO shoot cooldown: return
        if(m_pCurEquippedWeapon.ShootCoolDownTime < 1.0e-5f)
        {
          return;
        }

        // store shoot cooldown
        m_fCurShootCoolDown = m_pCurEquippedWeapon.ShootCoolDownTime;

        // start cool down spin for ShootBtn
        m_pShootBtnCoolDowner.startSpin(m_fCurShootCoolDown);

        // set ready flag
        m_bIsShootBtnReady = false;
      }
    }

    // callback for shooting weapon: when ShootBtn has been released
    public void onStopShootWeapon()
    {
      // NO equipped weapon
      if(null == m_pCurEquippedWeapon)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][onStopShootWeapon]: NO weapon to stop shooting");

        return;
      }

      // stop use the weapon
      m_pCurEquippedWeapon.doStopUseCom();

      // reset keep shooting flag
      m_bKeepShooting = false;
    }

    // callback for toggling the weapon inventory panel
    public void onToggleWeaponInventory()
    {
      if(true == m_pWeaponInventoryNode.activeSelf)
      {
        hideWeaponInventory();
      }
      else
      {
        showWeaponInventory();
      }
    }

    // callback for weapon equip
    public void onEquipWeapon(ShootWeapon a_pEquippedWeapon, WeaponSlot a_pEquippedSlot)
    {
      // store the equipped weapon
      m_pCurEquippedWeapon = a_pEquippedWeapon;

      // change the icon on shoot button
      GameObjectUtils.FindNode(m_pShootBtn, "WeaponIcon").GetComponent< Image >().sprite = a_pEquippedSlot.WeaponIcon;

      // hide the weapon inventory panel
      hideWeaponInventory();
    }

    // callback for toggling the toady inventory panel
    public void onToggleToadyInventory()
    {
      if(true == m_pToadyInventoryNode.activeSelf)
      {
        hideToadyInventory();
      }
      else
      {
        showToadyInventory();
      }
    }

    // callback for toady equip
    public void onEquipToady()
    {
      // hide the toady inventory panel
      hideToadyInventory();
    }

    // return the radius of the ZhunXing(aim pointer) in screen space
    public float AimRadius
    {
      get
      {
        // FIXED: scale the original AimPointer UI dimension with the current scale factor
        // NOT use CanvasScaler.scaleFacotr, which is ALWAYS 1
        return m_pCanvas.scaleFactor * (m_pAimPointer.rect.width * 0.5f - 20.0f); // modified from an externally surrounding square into an internally bounding square
      }
    }

    /*
     * return weapon inventory panel node
     */
    public GameObject WeaponInventoryNode
    {
      get
      {
        return m_pWeaponInventoryNode;
      }
    }

    /**
     * return weapon inventory panel component
     */
    public WeaponInventory WeaponInventory
    {
      get
      {
        return m_pWeaponInventory;
      }
    }

    /**
     * set maximal HP with the given current & maximal values
     */
    public void setHPBar(float a_fCurVal, float a_fMaxVal)
    {
      m_pHPBar.MaxValue = a_fMaxVal;

      m_pHPBar.CurrentValue = a_fCurVal;
    }

    /**
     * update HP with the given value
     * set the current HP to the given value
     */
    public void updateHPBar(float a_fCurVal)
    {
      m_pHPBar.CurrentValue = a_fCurVal;
    }

    /**
     * gain +/-HP with the given delta value
     */
    public void gainHPBar(float a_fDeltaVal)
    {
      m_pHPBar.CurrentValue += a_fDeltaVal;
    }

    /**
     * set maximal MP with the given current & maximal values
     */
    public void setMPBar(float a_fCurVal, float a_fMaxVal)
    {
      m_pMPBar.MaxValue = a_fMaxVal;

      m_pMPBar.CurrentValue = a_fCurVal;
    }

    /**
     * update MP with the given value
     * set current MP to the given value
     */
    public void updateMPBar(float a_fCurVal)
    {
      m_pMPBar.CurrentValue = a_fCurVal;
    }

    /**
     * gain +/-MP with the given delta value
     */
    public void gainMPBar(float a_fDeltaVal)
    {
      m_pMPBar.CurrentValue += a_fDeltaVal ; 
    }

    /**
     * update RP
     * set current RP to the given value
     */
    public void updateRPBar(int a_iCurReward)
    {
      m_pRPValue.text = a_iCurReward.ToString();
    }

    /**
     * update Coins
     * set current Coin amount the the given value
     */
    public void updateCoinsBar(int a_iCurCoins)
    {
      m_pCoinCount.text = a_iCurCoins.ToString();
    }

    #region PUBLIC OVERRIDDENS
    /**
     * Reset() is called when the user hits the Reset button in the Inspector's context menu
     * or when adding the component the first time 
     * This function is only called in editor mode 
     * Reset is most commonly used to give good default values in the inspector
     */
    public override void Reset()
    {
      
    }

    /**
     * Initialize function
     * NOTE:m_bIsInitialized MUST be SET in doInitialize() method of derived class
     */
    public override void doInitialize()
    {
      base.doInitialize();

      // create state map
      m_mGUINodeStateMap = new Dictionary< string, bool >(10);

      // push all states
      pushAllStates();

      // try to find AimPointer
      GameObject _pAimPointerGO = GameObjectUtils.FindNode(gameObject, "AimPointer");
      if(null == _pAimPointerGO)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: can NOT find AimPointer");

        return;
      }

      // confirm AimPointer is an UI element
      m_pAimPointer = _pAimPointerGO.GetComponent< RectTransform >();
      if(null == m_pAimPointer)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: AimPointer is NOT an UI Element: without RectTransform component");

        return;
      }

      // try to find ShootBtn
      m_pShootBtn = GameObjectUtils.FindNode(gameObject, "ShootBtn");
      if(null == m_pShootBtn)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: can NOT find ShootBtn");

        return;
      }

      // try to find BtnCoolDowner component of ShootBtn
      m_pShootBtnCoolDowner = m_pShootBtn.GetComponentInChildren< BtnCoolDowner >();
      if(null == m_pShootBtnCoolDowner)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: ShootBtn does NOT have BtnCoolDowner.cs attached");

        return;
      }

      // set ShootBtn ready flag
      m_bIsShootBtnReady = true;

      // reset keep shooting flag
      m_bKeepShooting = false;

      // try to find weapon inventory node
      m_pWeaponInventoryNode = GameObjectUtils.FindNode(gameObject, "WeaponInventory");
      if(null == m_pWeaponInventoryNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: can NOT find WeaponInventory Node");

        return;
      }

      m_pWeaponInventory = m_pWeaponInventoryNode.GetComponentInChildren< WeaponInventory >();
      if(null == m_pWeaponInventory)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: WeaponInventory node does NOT have WeaponInventory.cs");

        return;
      }

      // initialize weapon inventory
      m_pWeaponInventory.doInitialize(WInventoryWModes.WINVENTORY_WMODES_USING);

      // try ot find weapon inventory button node
      GameObject _pGUINode = GameObjectUtils.FindNode(gameObject, "WeaponInventoryBtn");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[[DefenseLevelGUI]][doInitialize]: can NOT find WeaponInventoryBtn node");

        return;
      }

      // store weapon inventory button's pressed color
      m_vWeaponInventoryBtnPressedColor = _pGUINode.GetComponentInChildren< Button >().colors.pressedColor;

      // try to find Icon node withing WeaponInventoryBtn node
      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "Icon");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[[DefenseLevelGUI]][doInitialize]: can NOT find Icon node within WeaponInventoryBtn node");

        return;
      }

      // try to get image component of weapon inventory button's icon
      m_pWeaponInventoryBtnIcon = _pGUINode.GetComponent< Image >();
      if(null == m_pWeaponInventoryBtnIcon)
      {
        DefaultGameLogger.Instance.dumpLogError("[[DefenseLevelGUI]][doInitialize]: Icon node does NOT have Image component");

        return;
      }

      // store weapon inventory button's normal color
      m_vWeaponInventoryBtnNormalColor = m_pWeaponInventoryBtnIcon.color;

      // hide the weapon inventory by default
      hideWeaponInventory();

      // try to find toady inventory node
      m_pToadyInventoryNode = GameObjectUtils.FindNode(gameObject, "ToadyInventory");
      if(null == m_pToadyInventoryNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: can NOT find ToadyInventory Node");

        return;
      }

      m_pToadyInventory = m_pToadyInventoryNode.GetComponentInChildren< ToadyInventory >();
      if(null == m_pToadyInventory)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: ToadyInventory node does NOT have ToadyInventory.cs");

        return;
      }

      // initialize toady inventory
      m_pToadyInventory.doInitialize();

      // try to find toady inventory button
      m_pToadyInventoryBtn = GameObjectUtils.FindNode(gameObject, "ToadyInventoryBtn");
      if(null == m_pToadyInventoryBtn)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: can NOT find ToadyInventoryBtn");

        return;
      }

      // store toady inventory button' normal color
      m_vToadyInventoryBtnNormalColor = GameObjectUtils.FindNode(m_pToadyInventoryBtn, "Icon").GetComponentInChildren< Image >().color;

      // try to find HP bar
      _pGUINode = GameObjectUtils.FindNode(gameObject, "HPPanel");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: can NOT find HPPanel");

        return;
      }

      // try to get HeartBar component
      m_pHPBar = _pGUINode.GetComponentInChildren< HeartBar >();
      if(null == m_pHPBar)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: HPPanel does NOT have HeartBar component");

        return;
      }

      // initialize the HP bar
      m_pHPBar.doInitialize();

      // try to find MP bar
      _pGUINode = GameObjectUtils.FindNode(gameObject, "MPPanel");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: can NOT find MPPanel");

        return;
      }

      // try to get HeartBar component
      m_pMPBar = _pGUINode.GetComponentInChildren< HeartBar >();
      if(null == m_pMPBar)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: MPPanel does NOT have HeartBar component");

        return;
      }

      // initialize the MP bar
      m_pMPBar.doInitialize();

      // try to get RP value text field
      _pGUINode = GameObjectUtils.FindNode(gameObject, "RPPanel");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: can NOT find RPPanel node");

        return;
      }

      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "RPValue");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: can NOT find RPPanel's RPValue node");

        return;
      }

      // try to get RP value text field
      m_pRPValue = _pGUINode.GetComponentInChildren< Text >();
      if(null == m_pRPValue)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: RPValue GUI node is NOT a Text field");

        return;
      }

      // set stored RP value
      updateRPBar(GameSaveMgr.Instance.CurrentGameSave.RP);

      // try to get coin count text field
      _pGUINode = GameObjectUtils.FindNode(gameObject, "CoinsPanel");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: can NOT find CoinsPanel node");

        return;
      }

      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "CoinsValue");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: can NOT find CoinsPanel's CoinsValue node");

        return;
      }

      m_pCoinCount = _pGUINode.GetComponentInChildren< Text >();
      if(null == m_pCoinCount)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: CoinsValue GUI node is NOT a Text field");

        return;
      }

      // set stored coins value
      updateCoinsBar(GameSaveMgr.Instance.CurrentGameSave.Coins);

      // try to find pause button
      m_pPauseBtn = GameObjectUtils.FindNode(gameObject, "PauseBtn");
      if(null == m_pPauseBtn)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: Can NOT find PauseBtn Node");

        return;
      }

      // try to find high lighter of pause button
      m_pPauseBtnHighLighter = m_pPauseBtn.GetComponentInChildren< IconHighLighter >();
      if(null == m_pPauseBtnHighLighter)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: PauseBtn does NOT have a HighLightButton component");

        return;
      }

      // initialize high lighter of pause button
      m_pPauseBtnHighLighter.doInitialize();

      // try to find record button
      m_pRecordBtn = GameObjectUtils.FindNode(gameObject, "RecordBtn");
      if(null == m_pRecordBtn)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: Can NOT find RecordBtn Node");

        return;
      }

      // hide it at first: wait until everyplay has been initialized
      m_pRecordBtn.SetActive(false);

      // set to NOT recording
      m_bIsRecording = false;

      // try to find high lighter of record button
      m_pRecordBtnHighLighter = m_pRecordBtn.GetComponentInChildren< IconHighLighter >();
      if(null == m_pRecordBtnHighLighter)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: RecordBtn does NOT have a HighLightButton component");

        return;
      }

      // initialize high lighter of record button
      m_pRecordBtnHighLighter.doInitialize();

      // try to find BackBtn node
      m_pBackBtnNode = GameObjectUtils.FindNode(gameObject, "BackBtn");
      if(null == m_pBackBtnNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: Can NOT find BackBtn Node");

        return;
      }

      // try to find AR marker lost warning panel
      m_pARMarkLostPanel = GameObjectUtils.FindNode(gameObject, "NoARMarkerPanel");
      if(null == m_pARMarkLostPanel)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: Can NOT find NoARMarkerPanel Node");

        return;
      }

      // try to find the color flipper for the AR marker lost warning panel
      m_pARMarkerColorFlipper = m_pARMarkLostPanel.GetComponentInChildren< ColorFlipper >();
      if(null == m_pARMarkerColorFlipper)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: NoARMarkerPanel does NOT have a ColorFlipper component");

        return;
      }

      // initialize color flipper for the AR marker lost warning panel
      m_pARMarkerColorFlipper.doInitialize();

      // try to find info panel
      m_pInfoPanel = GameObjectUtils.FindNode(gameObject, "InfoPanel");
      if(null == m_pARMarkLostPanel)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: Can NOT find InfoPanel Node");

        return;
      }

      // confirm the info panel does have animator and its has ShowInfoPanel state
      if(null == m_pInfoPanel.GetComponent < Animator >())
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: InfoPanel Node does NOT have an Animator");

        return;
      }

      // try to find DefendFailedDialog
      m_pDefendFailedDialog = GameObjectUtils.FindNode(m_pModalDialogPanel, "DefendFailedDialog");
      if(null == m_pDefendFailedDialog)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevelGUI][doInitialize]: Can NOT find DefendFailedDialog Node");

        return;
      }

      // check the gameplay record state
      if(GameRecordStates.GAME_RECORD_STATES_CHECKING == GameLevelMgr.Instance.GameMainLevel.MainEntry.GameRecordState)
      {
        GameLevelMgr.Instance.GameMainLevel.MainEntry.registerEveryplayCheckDelegate(onEveryPlayInitializationDone);
      }
      else if(GameRecordStates.GAME_RECORD_STATES_SUPPORTED == GameLevelMgr.Instance.GameMainLevel.MainEntry.GameRecordState)
      {
        m_pRecordBtn.SetActive(true);
      }

      // register all interesting game events
      registerInterestingEvents();

      // clear all handling flags
      m_bIsHandlingARMarkerLost = false;
      m_bIsHandlingARMarkerFound = false;

      // set initialized flag
      m_bIsInitialized = true;
    }

    /**
     * Update function: called each frame
     * Update() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last Update() call
     * check true == m_bIsInitialized before doing the real thing
     */
    public override void Update()
    {
      // NOT initialized: return
      if(false == m_bIsInitialized)
      {
        return;
      }

      // ShootBtn is ready for use: return
      if(true == m_bIsShootBtnReady)
      {
        return;
      }

      // decrease shoot cooldow time
      m_fCurShootCoolDown -= Time.deltaTime;

      // shoot cool down is over
      if(m_fCurShootCoolDown < 1.0e-5f)
      {
        // reset ShootBtn ready flag
        m_bIsShootBtnReady = true;

        // when keep shooting
        if(true == m_bKeepShooting)
        {
          // reshoot the weapon
          onShootWeapon();
        }
      }
    }

    /**
     * Post-Update function: called each frame after all Update() functions
     * LateUpdate() will be called after ALL Update() functions called
     */
    public override void LateUpdate()
    {

    }

    /*
     * the function called before destroying this GUI
     * NOTE:m_bIsInitialized MUST be CLEARED in doDestroy() method in derived class
     */
    public override void doDestroy()
    {
      // unregister all events
      unRegisterInterestingEvents();

      // destroy toady inventory
      m_pToadyInventory.doDestroy();

      // clear state map
      m_mGUINodeStateMap.Clear();

      m_pAimPointer = null;
      m_pPauseBtn = null;
      m_pPauseBtnHighLighter = null;
      m_pRecordBtn = null;
      m_pRecordBtnHighLighter = null;
      m_pShootBtn = null;
      m_pShootBtnCoolDowner = null;
      m_pWeaponInventoryBtnIcon = null;
      m_pCurEquippedWeapon = null;
      m_pWeaponInventoryNode = null;
      m_pWeaponInventory = null;
      m_pToadyInventoryBtn = null;
      m_pToadyInventoryNode = null;
      m_pToadyInventory = null;
      m_pHPBar = null;
      m_pMPBar = null;
      m_pRPValue = null;
      m_pCoinCount = null;
      m_pARMarkLostPanel = null;
      m_pARMarkerColorFlipper = null;
      m_pBackBtnNode = null;
      m_pInfoPanel = null;
      m_pDefendFailedDialog = null;
      m_mGUINodeStateMap = null;

      // clear all handling flags
      m_bIsHandlingARMarkerLost = false;
      m_bIsHandlingARMarkerFound = false;

      m_bKeepShooting = false;
      m_bIsShootBtnReady = true;

      // at last, call base's doDestroy()
      base.doDestroy();

      // clear the initialized flag
      m_bIsInitialized = false;
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // callback of everyplay initialization
    void onEveryPlayInitializationDone(bool a_bRecordSupported)
    {
      if(true == a_bRecordSupported)
      {
        m_pRecordBtn.SetActive(true);
      }
      else
      {
        m_pRecordBtn.SetActive(false);
      }

      // unregister callback
      GameLevelMgr.Instance.GameMainLevel.MainEntry.unRegisterEveryplayCheckDelegate(onEveryPlayInitializationDone);
    }

    void registerInterestingEvents()
    {
      // register for AR Marker Found and Lost events
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_AR_MARKER_FOUND, onARMarkerFound);
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_AR_MARKER_LOST, onARMarkerLost);

      // register for wave starting
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_PREPARE_ATTACKWARNING, onMonsterAttackPrepare);
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_WAVE_PREPARING, onOneWavePrepare);
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_LASTWAVE_PREPARING, onLastWavePrepare);

      // register Ads incentivize video
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_ADS_VIDEO_FINISHED, onFinshWatchingAdsVideo);
    }

    void unRegisterInterestingEvents()
    {
      // unregister for AR Marker Found and Lost events
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_AR_MARKER_FOUND, onARMarkerFound);
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_AR_MARKER_LOST, onARMarkerLost);

      // unregister for wave starting
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_PREPARE_ATTACKWARNING, onMonsterAttackPrepare);
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_WAVE_PREPARING, onOneWavePrepare);
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_LASTWAVE_PREPARING, onLastWavePrepare);

      // unregister Ads incentivze video
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_ADS_VIDEO_FINISHED, onFinshWatchingAdsVideo);
    }

    // callback for AR marker found
    void onARMarkerFound(Game.Event.GameEvent a_pEventRef)
    {
      Game.Event.ARMarkerData _pARMarkerData = (Game.Event.ARMarkerData)a_pEventRef.Data;

      // GUI is handling AR marker found event
      if(true == m_bIsHandlingARMarkerFound)
      {
        return;
      }

      // popup all states
      popAllStates();

      // stop color flipping
      m_pARMarkerColorFlipper.stopFlipping();

      // hide warning panel
      m_pARMarkLostPanel.SetActive(false);

      // clear lost handling flag
      m_bIsHandlingARMarkerLost = false;

      // set found handling flag
      m_bIsHandlingARMarkerFound = true;

#if !RETAIL
      DefaultGameLogger.Instance.dumpLogInfo("[DefenseLevelGUI][onARMarkerFound]");
#endif
    }

    // callback for AR marker lost
    void onARMarkerLost(Game.Event.GameEvent a_pEventRef)
    {
      Game.Event.ARMarkerData _pARMarkerData = (Game.Event.ARMarkerData)a_pEventRef.Data;

      // GUI is handling AR marker lost event
      if(true == m_bIsHandlingARMarkerLost)
      {
        return;
      }

      // push all states
      pushAllStates();

      // disable all GUI nodes
      for(int _iNIdx = 0; _iNIdx < transform.childCount; _iNIdx++)
      {
        transform.GetChild(_iNIdx).gameObject.SetActive(false);
      }

      // show BackBtn node
      m_pBackBtnNode.SetActive(true);

      // show warning panel
      m_pARMarkLostPanel.SetActive(true);

      // start color flipping
      m_pARMarkerColorFlipper.startFlipping();

      // clear found handling flag
      m_bIsHandlingARMarkerFound = false;

      // set lost handling flag
      m_bIsHandlingARMarkerLost = true;

#if !RETAIL
      DefaultGameLogger.Instance.dumpLogInfo("[DefenseLevelGUI][onARMarkerLost]");
#endif
    }

    // callback for prepare of whole monster attack
    void onMonsterAttackPrepare(Game.Event.GameEvent a_pEventRef)
    {
      // inactive all children
      for(int _iIdx = 0; _iIdx < m_pInfoPanel.transform.childCount; _iIdx++)
      {
        m_pInfoPanel.transform.GetChild(_iIdx).gameObject.SetActive(false);
      }

      // active the WaveStarts sub-panel
      GameObjectUtils.FindNode(m_pInfoPanel, "WaveStarts").SetActive(true);

      // enable info panel
      m_pInfoPanel.SetActive(true);

      // play the animation
      m_pInfoPanel.GetComponent < Animator >().SetTrigger(Animator.StringToHash("Show"));

      // NOT PAU: hide banner
      if(false == Game.Logic.GameSaveMgr.Instance.CurrentGameSave.IsPAU)
      {
        GameAdsMgr.Instance.hideBanner();
      }
    }

    // callback for prepare of one wave: event data: int: wave index: [1, n]
    void onOneWavePrepare(Game.Event.GameEvent a_pEventRef)
    {
      // inactive all children
      for(int _iIdx = 0; _iIdx < m_pInfoPanel.transform.childCount; _iIdx++)
      {
        m_pInfoPanel.transform.GetChild(_iIdx).gameObject.SetActive(false);
      }

      // get the default sub-panel
      GameObject _pSubPanelUsed = GameObjectUtils.FindNode(m_pInfoPanel, "WaveN");

      // get digitals of the wave number
      int _iWaveNumberN = (int)a_pEventRef.Data % 10;
      float _fWaveNumber = (float)(int)a_pEventRef.Data;
      int _iWaveNumberM = (int)(_fWaveNumber / 10.0f);

      // if more than 10
      if(_iWaveNumberM > 0)
      {
        _pSubPanelUsed = GameObjectUtils.FindNode(m_pInfoPanel, "WaveNM");
      }

      // set digital N
      GameObject _pDigital = GameObjectUtils.FindNode(_pSubPanelUsed, "N");
      _pDigital.GetComponent< Image >().sprite = m_mSpriteMap[_iWaveNumberN.ToString()];

      // set digital M, if any
      if(_iWaveNumberM > 0)
      {
        _pDigital = GameObjectUtils.FindNode(_pSubPanelUsed, "M");
        _pDigital.GetComponent< Image >().sprite = m_mSpriteMap[_iWaveNumberM.ToString()];
      }

      // enable sub-panel
      _pSubPanelUsed.SetActive(true);

      // enable info panel
      m_pInfoPanel.SetActive(true);

      // play the animation
      m_pInfoPanel.GetComponent < Animator >().SetTrigger(Animator.StringToHash("Show"));
    }

    // callback for prepare of the last wave
    void onLastWavePrepare(Game.Event.GameEvent a_pEventRef)
    {
      // inactive all children
      for(int _iIdx = 0; _iIdx < m_pInfoPanel.transform.childCount; _iIdx++)
      {
        m_pInfoPanel.transform.GetChild(_iIdx).gameObject.SetActive(false);
      }

      // active the LastWave sub-panel
      GameObjectUtils.FindNode(m_pInfoPanel, "LastWave").SetActive(true);

      // enable info panel
      m_pInfoPanel.SetActive(true);

      // play the animation
      m_pInfoPanel.GetComponent < Animator >().SetTrigger(Animator.StringToHash("Show"));
    }

    // callback for finishing watching an Ads incentize video
    void onFinshWatchingAdsVideo(Game.Event.GameEvent a_pEventRef)
    {
      // hide defend failed dialog
      hideDefendFailedDialog();

      if(GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel)
      {
        int _iGainedHP = (int)((float)((GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).TotalHP) * 0.5f);

        // data: rewards gained: int
        EconomyMgr.Instance.gainHP(_iGainedHP);

#if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("[DefenseLevelGUI][onFinshWatchingAdsVideo]: gained HP: " + _iGainedHP);
#endif
      }
    }

    // show weapon inventory
    void showWeaponInventory()
    {
      // let inventory class handle the show event
      m_pWeaponInventory.onShowPanel();

      // ACTIVE the inventory
      // NOTE: otherwise the SetTrigger() will return the error: Animator is not playing a Playable
      if(false == m_pWeaponInventoryNode.activeSelf)
      {
        m_pWeaponInventoryNode.SetActive(true);
      }

      // play show weapon inventory animation
      if(m_pWeaponInventory.GetComponent < Animator >() != null)
      {
        m_pWeaponInventory.GetComponent < Animator >().SetTrigger(Animator.StringToHash("Show")); 
      }

      // set weapon inventory button to the pressed color
      m_pWeaponInventoryBtnIcon.color = m_vWeaponInventoryBtnPressedColor;
    }

    // hide weapon inventory
    void hideWeaponInventory()
    {
      // play hide weapon inventory animation, if we can
      // then the animation will trigger onHidePanel()
      // - let the weapon inventory handle the hide event
      if(true == m_pWeaponInventoryNode.activeSelf                   // if the inventory panel is active
         &&                                                          // and
         m_pWeaponInventory.GetComponent < Animator >() != null      // there is an Animator
        )
      {
        m_pWeaponInventory.GetComponent < Animator >().SetTrigger(Animator.StringToHash("Hide")); 
      }
      else
      {
        // if there is NO hide animation or the panel is already hidden
        // let the weapon inventory handle the hide event directly
        m_pWeaponInventory.onHidePanel();
      }

      // set weapon inventory button to the normal color
      m_pWeaponInventoryBtnIcon.color = m_vWeaponInventoryBtnNormalColor;
    }

    // show toady inventory
    void showToadyInventory()
    {
      // let inventory class handle the show event
      m_pToadyInventory.onShowPanel();

      // ACTIVE the inventory
      // NOTE: otherwise the SetTrigger() will return the error: Animator is not playing a Playable
      if(false == m_pToadyInventoryNode.activeSelf)
      {
        m_pToadyInventoryNode.SetActive(true);
      }

      // play show toady inventory animation
      if(m_pToadyInventory.GetComponent < Animator >() != null)
      {
        m_pToadyInventory.GetComponent < Animator >().SetTrigger(Animator.StringToHash("Show")); 
      }

      // set toady inventory button to the pressed color
      GameObjectUtils.FindNode(m_pToadyInventoryBtn, "Icon").GetComponentInChildren< Image >().color = GetComponentInChildren< Button >().colors.pressedColor;
    }

    // hide toady inventory
    void hideToadyInventory()
    {
      // play hide toady inventory animation, if we can
      // then the animation will trigger onHidePanel()
      // - let the toady inventory handle the hide event
      if(true == m_pToadyInventoryNode.activeSelf                    // if the inventory panel is active
         &&                                                          // and
         m_pToadyInventory.GetComponent < Animator >() != null       // there is an Animator
        )
      {
        m_pToadyInventory.GetComponent < Animator >().SetTrigger(Animator.StringToHash("Hide")); 
      }
      else
      {
        // if there is NO hide animation or toady panel is already hidden
        // let the toady inventory handle the hide event directly
        m_pToadyInventory.onHidePanel();
      }

      // set toady inventory button to its normal color
      GameObjectUtils.FindNode(m_pToadyInventoryBtn, "Icon").GetComponentInChildren< Image >().color = m_vToadyInventoryBtnNormalColor;
    }

    // callback for back inventory dialog
    void onDecideOnBackToInventory(DialogResponseTypes a_eResponseType, System.Object a_pUserData)
    {
      // NO
      if(DialogResponseTypes.DIALOG_RESPONSE_TYPES_NO == a_eResponseType)
      {
        return;
      }

      // YES
      // NOT PAU + video available
      if(false == Game.Logic.GameSaveMgr.Instance.CurrentGameSave.IsPAU
         &&
         true == GameAdsMgr.Instance.isAnyVideoAvailable()
        )
      {
        // register all video related events
        Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_ADS_VIDEO_SKIPPED, onVideoAdsClosed);
        Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_ADS_VIDEO_FAILED, onVideoAdsClosed);
        Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_ADS_VIDEO_FINISHED, onVideoAdsClosed);

        // show video Ads
        GameAdsMgr.Instance.showIncentivizeVideo();
      }

      // PAU or NO video Ads at all
      else
      {
        // load the demo scene directly
        GameLevelMgr.Instance.GameMainLevel.MainEntry.loadGameLevel("DemoScene", Game.Camera.CameraModes.CAMERA_MODES_REGULAR);
      }
    }

    // hide DefendFailedDialog
    void hideDefendFailedDialog()
    {
      // hide the defend failed dialog
      m_pDefendFailedDialog.SetActive(false);

      // hide modal panel
      m_pModalDialogPanel.SetActive(false);
    }

    // callback when an video Ads has closed
    void onVideoAdsClosed(Game.Event.GameEvent a_pEventRef)
    {
      // unregister all video related events
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_ADS_VIDEO_SKIPPED, onVideoAdsClosed);
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_ADS_VIDEO_FAILED, onVideoAdsClosed);
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_ADS_VIDEO_FINISHED, onVideoAdsClosed);

      // load the demo scene
      GameLevelMgr.Instance.GameMainLevel.MainEntry.loadGameLevel("DemoScene", Game.Camera.CameraModes.CAMERA_MODES_REGULAR);
    }

    // push states of all GUI nodes
    void pushAllStates()
    {
      for(int _iNIdx = 0; _iNIdx < transform.childCount; _iNIdx++)
      {
        m_mGUINodeStateMap[transform.GetChild(_iNIdx).name] = transform.GetChild(_iNIdx).gameObject.activeSelf;
      }
    }

    // popup stored states of all GUI nodes
    void popAllStates()
    {
      for(int _iNIdx = 0; _iNIdx < transform.childCount; _iNIdx++)
      {
        if(m_mGUINodeStateMap.ContainsKey(transform.GetChild(_iNIdx).name))
        {
          transform.GetChild(_iNIdx).gameObject.SetActive(m_mGUINodeStateMap[transform.GetChild(_iNIdx).name]);
        }
      }
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // aim pointer
    RectTransform m_pAimPointer;

    // pause button
    GameObject m_pPauseBtn;
    // high lighter of pause button
    IconHighLighter m_pPauseBtnHighLighter;

    // record button
    GameObject m_pRecordBtn;
    // high lighter of record button
    IconHighLighter m_pRecordBtnHighLighter;
    // indicate if we are recording game play
    bool m_bIsRecording;

    // weapon shoot button
    GameObject m_pShootBtn;
    // cooldowner of weapon shoot button
    BtnCoolDowner m_pShootBtnCoolDowner;

    // weapon inventory button's icon
    Image m_pWeaponInventoryBtnIcon;
    // weapon inventory button's normal color
    Color m_vWeaponInventoryBtnNormalColor;
    // weapon inventory button's pressed color
    Color m_vWeaponInventoryBtnPressedColor;
    // currently equipped weapon
    ShootWeapon m_pCurEquippedWeapon;
    // current weapon inventory panel node
    GameObject m_pWeaponInventoryNode;
    // current weapon inventory component
    WeaponInventory m_pWeaponInventory;

    // toady inventory button
    GameObject m_pToadyInventoryBtn;
    // toady inventory button's normal color
    Color m_vToadyInventoryBtnNormalColor;
    // toady inventory node
    GameObject m_pToadyInventoryNode;
    // current toady inventory component
    ToadyInventory m_pToadyInventory;

    // HP bar
    HeartBar m_pHPBar;
    // MP bar
    HeartBar m_pMPBar;
    // RP value
    Text m_pRPValue;
    // coin count
    Text m_pCoinCount;

    // AR marker lost warning panel
    GameObject m_pARMarkLostPanel;
    // color flipper for AR marker lost warning panel
    ColorFlipper m_pARMarkerColorFlipper;

    // BackBtn node
    GameObject m_pBackBtnNode;

    // info panel
    GameObject m_pInfoPanel;

    // defend failed dialog
    GameObject m_pDefendFailedDialog;

    // state map of all GUI nodes
    Dictionary< string, bool > m_mGUINodeStateMap;

    // AR marker lost handling flag
    bool m_bIsHandlingARMarkerLost;
    // AR marker found handling flag
    bool m_bIsHandlingARMarkerFound;

    // current shooting cooldown
    float m_fCurShootCoolDown;
    // keep shooting flag
    bool m_bKeepShooting;
    // ShootBtn ready flag
    bool m_bIsShootBtnReady;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.GUI

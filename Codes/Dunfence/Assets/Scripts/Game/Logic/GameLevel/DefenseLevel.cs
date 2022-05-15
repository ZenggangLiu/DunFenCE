/**
 * Game Level used for main defense game play
 * - Provides the Main Game Logic Side Controls
 */

using UnityEngine;
using Framework.Inputs;
using Framework.Utils;
using Framework.Logger;

namespace Game.Logic
{
  public class DefenseLevel : FarmFenceLevel
  {
    #region PUBLIC METHODS
 
    #region PUBLIC OVERRIDDENS
    /**
     * called after this level has been loaded up within GameLevelMgr
     * - do some sort of initialization works
     * - use the given a_pLevelSwitchingData to preload any resources and set progress accordingly
     * - used to spawn all serialized Entity game objects
     */
    public override void doWarmUp(LevelSwitchingData a_pLevelSwitchingData)
    {
      base.doWarmUp(a_pLevelSwitchingData);

      // try to find CeleFireworks node
      m_pCeleFireworksNode = GameObjectUtils.FindNode(gameObject, "CeleFireworks");
      if(null == m_pCeleFireworksNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseLevel][doWarmUp][" + gameObject.ToString() + "]: Can NOT find CeleFireworks Child Node");

        return;
      }

      // disable the CeleFireworks node
      m_pCeleFireworksNode.SetActive(false);

      // reset state
      m_eCurState = GameLevelStates.GAMELEVEL_STATES_IDLE;

      // store all managers
      m_pDeviceInputMgr = DeviceInputMgr.Instance;
      m_pGameWaveMgr = GameWaveMgr.Instance;
      m_pEffectMgr = EffectMgr.Instance;
    }

    /**
     * called after calling doWarmUp()
     * called after clicking start button
     */
    public override void doEnter()
    {
      // as first call base's doEnter()
      base.doEnter();

      // register for AR Marker Found and Lost events
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_AR_MARKER_FOUND, onARMarkerFound);
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_AR_MARKER_LOST,  onARMarkerLost);

      // start this level
      m_eCurState = GameLevelStates.GAMELEVEL_STATES_START_GAMELEVEL;
    }

    /**
     * update function
     */
    public override void doUpdate()
    {
      // as first call base's doUpdate()
      base.doUpdate();

      switch(m_eCurState)
      {
        case GameLevelStates.GAMELEVEL_STATES_IDLE:
        {
          return;
        }

        case GameLevelStates.GAMELEVEL_STATES_START_GAMELEVEL:
        {
          m_fWaveStartDelay = GameLevelMgr.Instance.GameMainLevel.MainEntry.WaveStartDelay;

          // change to waiting for wave start state
          m_eCurState = GameLevelStates.GAMELEVEL_STATES_WAITING_FOR_WAVE_START;

          break;
        }

        case GameLevelStates.GAMELEVEL_STATES_WAITING_FOR_WAVE_START:
        {
          // count down waiting timer
          m_fWaveStartDelay -= Time.deltaTime;

          if(m_fWaveStartDelay < 1.0e-5f)
          {
            // start wave manager
            m_pGameWaveMgr.startWave(m_szLevelName);

            // change to playing state
            m_eCurState = GameLevelStates.GAMELEVEL_STATES_PLAYING;
          }

          break;
        }

        case GameLevelStates.GAMELEVEL_STATES_PLAYING:
        {
          break;
        }

        case GameLevelStates.GAMELEVEL_STATES_CELEBRATING:
        {
          break;
        }
      }

      // handle all pending device events
      m_pDeviceInputMgr.handleAllEvents();

      // update all managers
      // update game wave manager
      m_pGameWaveMgr.doUpdate();

      // update effect manager
      m_pEffectMgr.doUpdate();
    }

    /**
     * post update function
     */
    public override void doPostUpdate()
    {
      // as first call base's doPostUpdate()
      base.doPostUpdate();

      // post update game wave manager
      m_pGameWaveMgr.doPostUpdate();

      // post update effect manager
      m_pEffectMgr.doPostUpdate();
    }

    /**
     * called before calling doCoolDown()
     * 
     */
    public override void doExit()
    {
      // unregister for AR Marker Found and Lost events
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_AR_MARKER_FOUND, onARMarkerFound);
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_AR_MARKER_LOST,  onARMarkerLost);

      // at last, call base's doExit()
      base.doExit();
    }

    /**
     * called before this level will be released within GameLevelMgr
     * - do some sort of clean up works
     */
    public override void doCoolDown()
    {
      // reset all members
      m_pDeviceInputMgr = null;
      m_pGameWaveMgr = null;
      m_pEffectMgr = null;
      m_pCeleFireworksNode = null;
      m_eCurState = GameLevelStates.GAMELEVEL_STATES_IDLE;

      // at last, call base's doCoolDown()
      base.doCoolDown();
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS

    #region PROTECTED OVERRIDDENS
    // called when the farm has been raided
    protected override void onFarmRaided()
    {
      base.onFarmRaided();
    }

    // called when the player has succeeded a farm defense
    protected override void onSuccessDefense()
    {
      base.onSuccessDefense();

      // give the reward coins
      EconomyMgr.Instance.gainCoins(m_iDefenseReward);

      // enable the celebration fireworks
      m_pCeleFireworksNode.SetActive(true);

      // show message box
      string _szMessage = "\n<color=lime>Congratulation.\n\n</color>You defended the farm, gained " + m_iDefenseReward +" coins as rewards and 5% discount of <color=lime>BitTreasure</color> products providing this key:\n " + GameLevelMgr.Instance.CurrentGameLevel.DiscountKey;

      m_pGameLevelGUI.showMessageBox(Game.GUI.MessageBoxTypes.MESSAGEBOX_TYPES_INFO_SINGLEBTN, _szMessage, null, null);

      // change to celebrating state
      m_eCurState = GameLevelStates.GAMELEVEL_STATES_CELEBRATING;
    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS

    // callback for AR marker found
    void onARMarkerFound(Game.Event.GameEvent a_pEventRef)
    {
      Game.Event.ARMarkerData _pARMarkerData = (Game.Event.ARMarkerData)a_pEventRef.Data;

      // attach current level onto the given atach node
      GameLevelMgr.Instance.attachLevelNode(_pARMarkerData.AttachNode);
    }

    // callback for AR marker lost
    void onARMarkerLost(Game.Event.GameEvent a_pEventRef)
    {
      Game.Event.ARMarkerData _pARMarkerData = (Game.Event.ARMarkerData)a_pEventRef.Data;
    }

    #region PRIVATE DATA MEMBERS
    enum GameLevelStates
    {
      // nothing to do
      GAMELEVEL_STATES_IDLE,
      // just start this game level
      GAMELEVEL_STATES_START_GAMELEVEL,
      // waiting for wave start
      GAMELEVEL_STATES_WAITING_FOR_WAVE_START,
      // playing this game level
      GAMELEVEL_STATES_PLAYING,
      // celebrating the success defense
      GAMELEVEL_STATES_CELEBRATING,
    }

    // device input manager
    DeviceInputMgr m_pDeviceInputMgr;
    // game wave manager
    GameWaveMgr m_pGameWaveMgr;
    // effect manager
    EffectMgr m_pEffectMgr;
    // CeleFireworks node
    GameObject m_pCeleFireworksNode;
    // current wave start delay
    float m_fWaveStartDelay;
    // current state
    GameLevelStates m_eCurState;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Logic

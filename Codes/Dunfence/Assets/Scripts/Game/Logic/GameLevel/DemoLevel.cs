/**
 * Demo Level with AI controlled defense
 * - Provides the Main Game Logic Side Controls
 */

using UnityEngine;
using Framework.Inputs;
using Framework.Mathe;

namespace Game.Logic
{
  public class DemoLevel : FarmFenceLevel
  {
    #region PUBLIC DATA MEMBERS
    // cooldown timer for restart
    public float CooldownTime = 5.0f;
    // minimal delay time for defense tower
    public float MinDefendDelay = 3.0f;
    // maximal delay time for defense tower
    public float MaxDefendDelay = 20.0f;
    #endregion // PUBLIC DATA MEMBERS

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

      // clear defense flag
      m_bIsStartingDefense = false;

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
          // if we have started the tower defense
          if(true == m_bIsStartingDefense)
          {
            // decrease delay timer
            m_fCurDefenseDelay -= Time.deltaTime;

            // delay time is over
            if(m_fCurDefenseDelay < 1.0e-5f)
            {
              // randomly select a defense tower
              int _iTowerIndex = RandomNumberMgr.Instance.getIntegerVal(0, m_aPlayerDefenseBuildingList.Count - 1);

              // start the choosen tower
              m_aPlayerDefenseBuildingList[_iTowerIndex].doDefendCom();

              // clear starting flag
              m_bIsStartingDefense = false;
            }
          }

          break;
        }

        case GameLevelStates.GAMELEVEL_STATES_COOLDOWN_BEFORE_RESTART:
        {
          // count down waiting timer
          m_fCurCoolDownTime -= Time.deltaTime;

          // clear cache total HP
          m_fCurPlayerBuildingHP = 0.0f;

          if(m_fCurCoolDownTime < 1.0e-5f)
          {
            // let all player's building gain the maximal HP
            for(int _iBIdx = 0; _iBIdx < m_aPlayerAttackTargetBuildingList.Count; _iBIdx++)
            {
              m_aPlayerAttackTargetBuildingList[_iBIdx].gainHP(m_aPlayerAttackTargetBuildingList[_iBIdx].MaxHealth);
            }

            // re-gain the maximal HP in the cache version
            m_fCurPlayerBuildingHP = m_fTotalPlayerBuildingHP;

            // reset game wave manager
            m_pGameWaveMgr.doReset();

            // change to start game play mode
            m_eCurState = GameLevelStates.GAMELEVEL_STATES_START_GAMELEVEL;
          }

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
      m_eCurState = GameLevelStates.GAMELEVEL_STATES_IDLE;

      // at last, call base's doCoolDown()
      base.doCoolDown();
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS

    #region PROTECTED OVERRIDDENS
    // register all interesting events
    protected override void registerInterestingEvents()
    {
      // register all events of base class
      base.registerInterestingEvents();

      // register flag falling done event
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_ATTACKWARNING_FINISHED, onMonsterFlagFallDone);

      // register defense tower service done event
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_DEFENSE_TOWER_SERVICE_DONE, onDefenseTowerServiceDone);
    }

    // unregister all interesting events
    protected override void unRegisterInterestingEvents()
    {
      // unregister flag falling done event
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_ATTACKWARNING_FINISHED, onMonsterFlagFallDone);
      // unregister defense tower service done event
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_DEFENSE_TOWER_SERVICE_DONE, onDefenseTowerServiceDone);

      // unregister all events of base class
      base.unRegisterInterestingEvents();
    }

    // called when the farm has been raided
    protected override void onFarmRaided()
    {
      // restart the game play
      restartGamePlay();
    }

    // called when the player has succeeded a farm defense
    protected override void onSuccessDefense()
    {
      // restart the game play
      restartGamePlay();
    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS

    #region PRIVATE METHODS
    // restart the game play
    void restartGamePlay()
    {
      // destroy all monsters
      for(int _iMIdx = 0; _iMIdx < MonsterMgr.Instance.CurrentMonsterList.Count; _iMIdx++)
      {
        MonsterMgr.Instance.CurrentMonsterList[_iMIdx].GetComponentInChildren< Entity >().doDeadCom();
      }

      // destroy all bosses
      for(int _iMIdx = 0; _iMIdx < MonsterMgr.Instance.CurrentBossList.Count; _iMIdx++)
      {
        MonsterMgr.Instance.CurrentBossList[_iMIdx].GetComponentInChildren< Entity >().doDeadCom();
      }

      // pause game wave manager
      m_pGameWaveMgr.pauseWave();

      // clear defense flag
      m_bIsStartingDefense = false;

      // set cooldown timer
      m_fCurCoolDownTime = CooldownTime;

      // change to cooldown state
      m_eCurState = GameLevelStates.GAMELEVEL_STATES_COOLDOWN_BEFORE_RESTART;
    }

    // prepare tower defense
    void prepareTowerDefense()
    {
      // NO defense tower at all: return
      if(0 == m_aPlayerDefenseBuildingList.Count)
      {
        m_bIsStartingDefense = false;

        return;
      }

      // get randomly a delay time before starting any defense tower
      m_fCurDefenseDelay = UnityEngine.Random.Range(MinDefendDelay, MaxDefendDelay);

      // set defense flag
      m_bIsStartingDefense = true;      
    }

    // callback when the monster flag touched the ground
    void onMonsterFlagFallDone(Game.Event.GameEvent a_pEventRef)
    {
      // NOT PAU: hide banner
      if(false == Game.Logic.GameSaveMgr.Instance.CurrentGameSave.IsPAU)
      {
        Game.Ads.GameAdsMgr.Instance.hideBanner();
      }

      // prepare for a tower defense
      prepareTowerDefense();
    }

    // callback when one defense tower has done its service
    void onDefenseTowerServiceDone(Game.Event.GameEvent a_pEventRef)
    {
      // start next defend
      prepareTowerDefense();
    }
    #endregion // PRIVATE METHODS

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
      // cool down before restart game play
      GAMELEVEL_STATES_COOLDOWN_BEFORE_RESTART,
    }

    // device input manager
    DeviceInputMgr m_pDeviceInputMgr;
    // game wave manager
    GameWaveMgr m_pGameWaveMgr;
    // effect manager
    EffectMgr m_pEffectMgr;
    // current wave start delay
    float m_fWaveStartDelay;
    // current cool down timer for gameplay restart
    float m_fCurCoolDownTime;
    // current delay before starting defense
    float m_fCurDefenseDelay;
    // defense flag
    bool m_bIsStartingDefense;
    // current state
    GameLevelStates m_eCurState;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Logic

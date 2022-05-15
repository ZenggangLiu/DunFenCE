/**
 * game wave manager
 * - control how to send out monsters: find spawn position and set attack target position
 * - define an assortment of Teleports
 * - switch from one wave to next wave
 * - using MonsterMgr to track the count of currently attacking monsters to decide if send more monsters
 */
using UnityEngine;
using Framework.Utils;
using Framework.Logger;
using System.Collections.Generic; // List< T >
using Framework.Mathe;

namespace Game.Logic
{
  // info data of each wave
  public class WaveInfo
  {
    // length of wave in seconds
    public float WaveLength;
    // what time boss will come
    public float BossChargeTime;
    // boss IDs for this wave
    public List< int > BossIDList;
    // total monster count without boss
    public int MonsterCount;
    // what time monsters will come in 
    public float MonsterChargeTime;
    // how many monsters will come in the first charge
    public int MonsterChargeCount;
    // delay between each recharge
    public float MonsterRechargeDelay;
    // monster IDs for this wave
    public List< int > MonsterIDList;
  }

  // data of whole wave list
  public class CWaveInfoList
  {
    public CWaveInfoList(string a_szLevelName, int a_iMonsterFlagID, int a_iTelePortID, int a_iAllowedMaxMonsterCount)
    {
      // store level name
      LevelName = a_szLevelName;

      // store monster flag
      MonsterFlagID = a_iMonsterFlagID;

      // store teleport ID
      TelePortID = a_iTelePortID;

      // store maximal allowed monster count
      AllowedMaxMonsterCount = a_iAllowedMaxMonsterCount;

      // create WaveInfo list
      WaveInfoList = new List< WaveInfo >(8);
    }

    // level name this WaveInfo List belonging to
    public string LevelName;
    // monster flag ID
    public int MonsterFlagID;
    // teleport ID
    public int TelePortID;
    // maximal allowed monster count: normal monster + boss
    public int AllowedMaxMonsterCount;
    // whole WaveInfo list
    public List< WaveInfo > WaveInfoList;
  }

  public class GameWaveMgr : TManager < GameWaveMgr >
  {
    #region PUBLIC METHODS
    /**
     * return current level name
     */
    public string CurrentLevelName
    {
      get
      {
        return m_szCurLevelName;
      }
    }

    /**
     * start the whole wave from the beginning step
     * return true on success
     */
    public bool startWave(string a_szLevelName)
    {
      // if the given level does NOT have a wave info list
      if(m_mLevelMap.ContainsKey(a_szLevelName) == false)
      {
        return false;
      }

      reset();

      // get current monster flag ID
      m_iCurMonsterFlagID = m_mLevelMap[a_szLevelName].MonsterFlagID;

      // get current teleport ID
      m_iCurTelePortID = m_mLevelMap[a_szLevelName].TelePortID;

      // store the given level name
      m_szCurLevelName = a_szLevelName;

      // get wave info list of the given level
      m_aCurWaveInfoList = m_mLevelMap[a_szLevelName];

      // set right state
      m_ePrevState = m_eCurState = States.STATES_PREPARE_ATTACKWARNING;

      return true;
    }

    /**
     * check if this wave manager is paused
     */
    public bool IsPaused
    {
      get
      {
        return States.STATES_PAUSED == m_eCurState;
      }
    }

    // pause the wave
    public void pauseWave()
    {
      m_ePrevState = m_eCurState;

      m_eCurState = States.STATES_PAUSED;
    }

    // resume the wave from previous step
    public void resumeWave()
    {
      States _eCurState = m_eCurState;

      m_eCurState = m_ePrevState;

      m_ePrevState = _eCurState;
    }

    #region PUBLIC OVERRIDDENS
    /**
     * the function called before using this manager
     * set m_bIsInitialized to true, on successful initialization
     * load in WaveData.json
     */
    public override void doInitialize()
    {
      // set initializing state
      m_ePrevState = m_eCurState = States.STATES_INITIALIZING;

      // get the game main entry
      m_pGameMain = GameObject.Find("GameMain").GetComponent< GameMain >();

      // create level map
      m_mLevelMap = new Dictionary< string, CWaveInfoList >(6);

      reset();

      // try to load in WaveData.json file
      TextAsset _pWaveDataJsonFile = Resources.Load("WaveData") as TextAsset; // an TextAsset: the representation of a Textural or Binary File
      if(null == _pWaveDataJsonFile)
      {
        DefaultGameLogger.Instance.dumpLogError("Can NOT find WaveData.json file");

        return;
      }

      // create and parse the json data at the same time
      JSONObject _pWaveDataJson = new JSONObject(_pWaveDataJsonFile.text);

      // if WaveData json does NOT contain LevelList element
      if(false == _pWaveDataJson.HasField("LevelList"))
      {
        DefaultGameLogger.Instance.dumpLogError("Can NOT find LevelList element in WaveData.json file");

        return;
      }

      // if LevelList element is NOT an array
      if(false == _pWaveDataJson["LevelList"].IsArray)
      {
        DefaultGameLogger.Instance.dumpLogError("LevelList element is NOT an array");

        return;
      }

      // get the LevelList json block
      JSONObject _pLevelListJsonBlock = _pWaveDataJson["LevelList"];

      // iterate over whole LevelList
      for(int _iCurLevelIdx = 0; _iCurLevelIdx < _pLevelListJsonBlock.Count; _iCurLevelIdx++)
      {
        // get the current level json block
        JSONObject _pCurLevelJsonBlock = _pLevelListJsonBlock[_iCurLevelIdx];

        // if there is NO LevelName: go to next level
        if(false == _pCurLevelJsonBlock.HasField("LevelName"))
        {
          DefaultGameLogger.Instance.dumpLogError("No LevelName element in " + _iCurLevelIdx + "th element");

          continue;
        }

        // if there is NO MonsterFlag or MonsterFlag is NOT a number: go to next level
        if(false == _pCurLevelJsonBlock.HasField("MonsterFlag")
           ||
           false == _pCurLevelJsonBlock["MonsterFlag"].IsNumber
          )
        {
          DefaultGameLogger.Instance.dumpLogError("No MonsterFlag element in " + _iCurLevelIdx + "th element");

          continue;
        }

        // if there is NO TelePort or TelePort is NOT a number: go to next level
        if(false == _pCurLevelJsonBlock.HasField("TelePort")
           ||
           false == _pCurLevelJsonBlock["TelePort"].IsNumber
          )
        {
          DefaultGameLogger.Instance.dumpLogError("No TelePort element in " + _iCurLevelIdx + "th element");

          continue;
        }

        // if there is NO MaxAllowedMonsterCount or MaxAllowedMonsterCount is NOT a number: go to next level
        if(false == _pCurLevelJsonBlock.HasField("MaxAllowedMonsterCount")
           ||
           false == _pCurLevelJsonBlock["MaxAllowedMonsterCount"].IsNumber
          )
        {
          DefaultGameLogger.Instance.dumpLogError("No MaxAllowedMonsterCount element in " + _iCurLevelIdx + "th element");

          continue;
        }

        // if there is NO WaveInfoList or WaveInfoList is NOT an array
        if(false == _pCurLevelJsonBlock.HasField("WaveInfoList")
           ||
           false == _pCurLevelJsonBlock["WaveInfoList"].IsArray
          )
        {
          DefaultGameLogger.Instance.dumpLogError("No WaveInfoList element in " + _iCurLevelIdx + "th element");

          continue;
        }

        // store level name
        string _szLevelName = _pCurLevelJsonBlock["LevelName"].str;

        // store monster flag
        int _iMonsterFlagID = (int)_pCurLevelJsonBlock["MonsterFlag"].i;

        // store teleport
        int _iTelePortID = (int)_pCurLevelJsonBlock["TelePort"].i;

        // store maximal allowed monster count
        int _iMaxAllowedMonsterCount = (int)_pCurLevelJsonBlock["MaxAllowedMonsterCount"].i;

        // read in WaveInfoList
        // create a new WaveInfo list
        CWaveInfoList _pCurWaveInfoList = new CWaveInfoList(_szLevelName, _iMonsterFlagID, _iTelePortID, _iMaxAllowedMonsterCount);

        // iterate over the whole WaveInfoList
        for(int _iCurWaveInfoIdx = 0; _iCurWaveInfoIdx < _pCurLevelJsonBlock["WaveInfoList"].Count; _iCurWaveInfoIdx++)
        {
          JSONObject _pCurWaveInfoJsonBlock = _pCurLevelJsonBlock["WaveInfoList"][_iCurWaveInfoIdx];

          WaveInfo _pCurWaveInfo = new WaveInfo();

          // store wave length
          if(false == _pCurWaveInfoJsonBlock.HasField("WaveLength")
             || 
             false == _pCurWaveInfoJsonBlock["WaveLength"].IsNumber
            )
          {
            DefaultGameLogger.Instance.dumpLogError("No valid WaveLength element in " + _iCurWaveInfoIdx + "th wave info");

            continue;
          }

          _pCurWaveInfo.WaveLength = (int)_pCurWaveInfoJsonBlock["WaveLength"].i;

          // store boss charge time
          if(false == _pCurWaveInfoJsonBlock.HasField("BossChargeTime")
             || 
             false == _pCurWaveInfoJsonBlock["BossChargeTime"].IsNumber
            )
          {
            DefaultGameLogger.Instance.dumpLogError("No valid BossChargeTime element in " + _iCurWaveInfoIdx + "th wave info");

            continue;
          }

          _pCurWaveInfo.BossChargeTime = _pCurWaveInfoJsonBlock["BossChargeTime"].f;

          // store boss ID list
          if(false == _pCurWaveInfoJsonBlock.HasField("BossIDList")
             || 
             false == _pCurWaveInfoJsonBlock["BossIDList"].IsArray
            )
          {
            DefaultGameLogger.Instance.dumpLogError("No valid BossIDList element in " + _iCurWaveInfoIdx + "th wave info");

            continue;
          }

          // create a new boss ID list
          _pCurWaveInfo.BossIDList = new List< int >(4);

          // read in all boss IDs
          for(int _iCurBossIDIdx = 0; _iCurBossIDIdx < _pCurWaveInfoJsonBlock["BossIDList"].Count; _iCurBossIDIdx++)
          {
            _pCurWaveInfo.BossIDList.Add((int)_pCurWaveInfoJsonBlock["BossIDList"][_iCurBossIDIdx].i);
          }

          // store monster count
          if(false == _pCurWaveInfoJsonBlock.HasField("MonsterCount")
             || 
             false == _pCurWaveInfoJsonBlock["MonsterCount"].IsNumber
            )
          {
            DefaultGameLogger.Instance.dumpLogError("No valid MonsterCount element in " + _iCurWaveInfoIdx + "th wave info");

            continue;
          }

          _pCurWaveInfo.MonsterCount = (int)_pCurWaveInfoJsonBlock["MonsterCount"].i;

          // store monster charge time
          if(false == _pCurWaveInfoJsonBlock.HasField("MonsterChargeTime")
             || 
             false == _pCurWaveInfoJsonBlock["MonsterChargeTime"].IsNumber
            )
          {
            DefaultGameLogger.Instance.dumpLogError("No valid MonsterChargeTime element in " + _iCurWaveInfoIdx + "th wave info");

            continue;
          }

          _pCurWaveInfo.MonsterChargeTime = _pCurWaveInfoJsonBlock["MonsterChargeTime"].f;

          // store monster charge count
          if(false == _pCurWaveInfoJsonBlock.HasField("MonsterChargeCount")
             || 
             false == _pCurWaveInfoJsonBlock["MonsterChargeCount"].IsNumber
            )
          {
            DefaultGameLogger.Instance.dumpLogError("No valid MonsterChargeCount element in " + _iCurWaveInfoIdx + "th wave info");

            continue;
          }

          _pCurWaveInfo.MonsterChargeCount = (int)_pCurWaveInfoJsonBlock["MonsterChargeCount"].i;

          // store monster recharge delay
          if(false == _pCurWaveInfoJsonBlock.HasField("MonsterRechargeDelay")
             || 
             false == _pCurWaveInfoJsonBlock["MonsterRechargeDelay"].IsNumber
            )
          {
            DefaultGameLogger.Instance.dumpLogError("No valid MonsterRechargeDelay element in " + _iCurWaveInfoIdx + "th wave info");

            continue;
          }

          _pCurWaveInfo.MonsterRechargeDelay = _pCurWaveInfoJsonBlock["MonsterRechargeDelay"].f;

          // store monster ID list
          if(false == _pCurWaveInfoJsonBlock.HasField("MonsterIDList")
             || 
             false == _pCurWaveInfoJsonBlock["MonsterIDList"].IsArray
            )
          {
            DefaultGameLogger.Instance.dumpLogError("No valid MonsterIDList element in " + _iCurWaveInfoIdx + "th wave info");

            continue;
          }

          // create a new monster ID list
          _pCurWaveInfo.MonsterIDList = new List< int >(10);

          // read in all monster IDs
          for(int _iCurMonsterIDIdx = 0; _iCurMonsterIDIdx < _pCurWaveInfoJsonBlock["MonsterIDList"].Count; _iCurMonsterIDIdx++)
          {
            _pCurWaveInfo.MonsterIDList.Add((int)_pCurWaveInfoJsonBlock["MonsterIDList"][_iCurMonsterIDIdx].i);
          }

          // insert the new wave info
          _pCurWaveInfoList.WaveInfoList.Add(_pCurWaveInfo);
        } // iterate over the whole WaveInfoList

        // insert the whole wave info list
        m_mLevelMap.Add(_szLevelName, _pCurWaveInfoList);
      } // iterate over whole LevelList

      m_bIsInitialized = true;

      // set initialized state
      m_ePrevState = m_eCurState = States.STATES_INITIALIZED;
    }

    /**
     * reset this manager
     * clear all internal data members: roll back to the SAME state as after calling doInitialize()
     * STILL marked as initialized
     */
    public override void doReset()
    {
      // reset all variables
      reset();

      // set initialized state
      m_ePrevState = m_eCurState = States.STATES_INITIALIZED;
    }

    // update function
    public override void doUpdate()
    {
      switch(m_eCurState)
      {
        case States.STATES_INITIALIZING:
        {
          break;
        }

        case States.STATES_INITIALIZED:
        {
          break;
        }

        case States.STATES_PREPARE_ATTACKWARNING:
        {
          // start attack warning: flag dropping down
          // create monster flag
          // can NOT get the monster flag asset: go back to initialized state
          GameObject _pMonsterFlagNode = null;
          if(false == GameLevelMgr.Instance.CurrentGameLevel.getAsset(m_iCurMonsterFlagID, out _pMonsterFlagNode))
          {
            DefaultGameLogger.Instance.dumpLogError("[GameWaveMgr]: Can NOT find Monster Flag[" + m_iCurMonsterFlagID + "]");

            m_eCurState = States.STATES_INITIALIZED;

            break;
          }
          else
          {
            // enable this flag
            _pMonsterFlagNode.SetActive(true);

            // attach the monster flag under Level/MonsterBuildings node
            _pMonsterFlagNode.transform.parent = (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).MonsterBuildingsNode.transform;

            // spawn monster flag
            // try to find the FallDownBuilding component of monster flag
            m_pCurMonsterFlag = _pMonsterFlagNode.GetComponent< FallDownBuilding >();

            // if we can find the FallingDownBuiding component
            if(m_pCurMonsterFlag != null)
            {
              // get a random spawn pos
              Transform _pCurFlagTrans = null;

              // if we can get a spawn pos
              if(true == (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).getMonsterSpawnPos(out _pCurFlagTrans))
              {
                // store current position of monster flag
                m_vCurFlagPos = _pCurFlagTrans.position;

                // [-15, +15] degrees
                float _fRandomRotDegrees = Random.value /* [0, 1] */ * 30.0f /*[0, 30] */ - 15.0f; // [-15, 15]

                // a random rotation around up axis
                Quaternion _qRandomRot = Quaternion.AngleAxis(_fRandomRotDegrees, Vector3.up);

                // rotate the heading vector of the current spawn pos
                Vector3 _vSpawnHeadingVector = _qRandomRot * _pCurFlagTrans.forward;

                // if the flag's heading vector is pointing away from eye vector: dot(Eye, Heading) > 0
                if(Vector3.Dot(GameLevelMgr.Instance.GameMainLevel.CameraNode.transform.forward, _vSpawnHeadingVector) > 0.0f)
                {
                  _vSpawnHeadingVector = -_vSpawnHeadingVector;
                }

                // initialize and spawn the monster flag: aligned with the spawn pos' frame
                m_pCurMonsterFlag.doSpawnCom(m_vCurFlagPos, _vSpawnHeadingVector, Vector3.up);

                // register a handler on spawn end
                m_pCurMonsterFlag.registerFallDownCompletionHandler(onMonsterFlagSpawnDone);

                // fire game event
                Event.GameEventMgr.Instance.fireGlobalScopeEvent(Event.EventIDs.EVENT_IDS_ATTACK_PREPARE_ATTACKWARNING,
                                                                 null,
                                                                 Event.EventTriggerType.EVENT_TRIGGER_TYPE_AI,
                                                                 this
                                                                );
                // change to waiting state
                m_eCurState = States.STATES_WARNING_ATTACK;

                break;
              }
            }
          }

          // can NOT spawn the monst flag
          m_eCurState = States.STATES_PREPARE_WAVE;
          break;
        }

        case States.STATES_WARNING_ATTACK:
        {
          // waiting for warning completion
          break;
        }

        case States.STATES_ATTACKWARNING_FINISHED:
        {
          // attack warning has been done
          // wait a while before starting the first wave
          // count down the delay timer
          m_fCurWaitingTime -= Time.deltaTime;

          // check if we have done with the waiting
          if(m_fCurWaitingTime < 1.0e-5f)
          {
            // done
            // clear current wave index
            m_iCurWaveIndex = 0;

            // change state to prepare next wave
            m_eCurState = States.STATES_PREPARE_WAVE;
          }

          break;
        }

        case States.STATES_PREPARE_WAVE:
        {
          // check if we have finished all waves
          if(m_iCurWaveIndex >= m_aCurWaveInfoList.WaveInfoList.Count)
          {
            // done with all waves
            // fire game event
            Event.GameEventMgr.Instance.fireGlobalScopeEvent(Event.EventIDs.EVENT_IDS_ATTACK_ALLWAVES_ENDED,
                                                             null,
                                                             Event.EventTriggerType.EVENT_TRIGGER_TYPE_AI,
                                                             this
                                                            );

            // change to done state
            m_eCurState = States.STATES_FINISHED_ALLWAVES_PLAYBACK;

            break;
          }

          // if we still can playback a wave
          // change to check alive monster state
          m_eCurState = States.STATES_PREPARE_CHECK_ALIVEMONSTER_COUNT;

          break;
        }

        case States.STATES_PREPARE_CHECK_ALIVEMONSTER_COUNT:
        {
          // NOT free monster slot
          if((MonsterMgr.Instance.CurrentBossCount + MonsterMgr.Instance.CurrentMonsterCount) >= m_aCurWaveInfoList.AllowedMaxMonsterCount)
          {
            break;
          }

          // there is SOME free monster slots
          // fire game event
          // the last wave
          if(m_iCurWaveIndex == m_aCurWaveInfoList.WaveInfoList.Count - 1)
          {
            Event.GameEventMgr.Instance.fireGlobalScopeEvent(Event.EventIDs.EVENT_IDS_ATTACK_LASTWAVE_PREPARING,
                                                             null,
                                                             Event.EventTriggerType.EVENT_TRIGGER_TYPE_AI,
                                                             this
                                                            );
          }
          else
          {
            Event.GameEventMgr.Instance.fireGlobalScopeEvent(Event.EventIDs.EVENT_IDS_ATTACK_WAVE_PREPARING,
                                                             m_iCurWaveIndex + 1,
                                                             Event.EventTriggerType.EVENT_TRIGGER_TYPE_AI,
                                                             this
                                                            );
          }

          // get current wave info
          m_pCurWaveInfo = m_aCurWaveInfoList.WaveInfoList[m_iCurWaveIndex];

          // set maximal wave attack time
          m_fCurAttackTime = m_pCurWaveInfo.WaveLength;

          // set monster charge time
          m_fCurWaitingTime = m_pCurWaveInfo.MonsterChargeTime;

          // clear boss chaged flag
          m_bBossCharged = false;

          // change to finish state
          m_eCurState = States.STATES_PREPARE_WAVE_FINISHED;            

          break;
        }

        case States.STATES_PREPARE_WAVE_FINISHED:
        {
          // wait if there is a monster charge delay

          // update wave attack time
          m_fCurAttackTime -= Time.deltaTime;

          // update monster charge time
          m_fCurWaitingTime -= Time.deltaTime;

          // check if we have done with monster charge time
          if(m_fCurWaitingTime < 1.0e-5f)
          {
            // change to prepare monster charge state
            m_eCurState = States.STATES_PREPARE_MONSTER_CHARGE;
          }

          break;
        }

        case States.STATES_PREPARE_MONSTER_CHARGE:
        {
          // start monster charge
          // get free monster slot count
          int _iFreeMonsterSlot = m_aCurWaveInfoList.AllowedMaxMonsterCount - MonsterMgr.Instance.CurrentBossCount - MonsterMgr.Instance.CurrentMonsterCount;

          // there is some free slots
          if(_iFreeMonsterSlot > 0)
          {
            // spawn as many monster as possible
            int _iChargedMonsterCount = m_pCurWaveInfo.MonsterChargeCount < _iFreeMonsterSlot ? m_pCurWaveInfo.MonsterChargeCount : _iFreeMonsterSlot;

            spawnMonster(_iChargedMonsterCount);
          }

          // change to attacking state
          m_eCurState = States.STATES_MONSTER_ATTACKING;

          break;
        }

        case States.STATES_MONSTER_ATTACKING:
        {
          // attacking state
          // count down wave attack time
          m_fCurAttackTime -= Time.deltaTime;

          // check if we have done with the current wave
          if(m_fCurAttackTime < 1.0e-5f)
          {
            // YES: go to the next wave
            ++m_iCurWaveIndex;

            m_eCurState = States.STATES_PREPARE_WAVE;

            break;
          }

          // check if we need to charge boss
          // T_total - T_cur >= T_boss --> T_total - T_cur - T_boss >= 0
          if(false == m_bBossCharged                                                                        // have NOT charged the boss so far
             &&                                                                                             // and
             ((m_pCurWaveInfo.WaveLength - m_fCurAttackTime - m_pCurWaveInfo.BossChargeTime) > 1.0e-5f)     // boss charge time is over
            )
          {
            // charge boss
            m_eCurState = States.STATES_CHARGEING_BOSS;

            break;
          }

          if((m_aCurWaveInfoList.AllowedMaxMonsterCount - MonsterMgr.Instance.CurrentBossCount - MonsterMgr.Instance.CurrentMonsterCount) > 0   // there are some free slots
              &&                                                                                                                                // and
              MonsterMgr.Instance.CurrentMonsterCount < m_pCurWaveInfo.MonsterCount                                                             //current monster count less than expected
            )
          {
            // recharge monster
            m_eCurState = States.STATES_PREPARE_RECHARGE_MONSTER;

            break;
          }

          break;
        }

        case States.STATES_CHARGEING_BOSS:
        {
          // charge boss
          // spawn ONE boss
          spawnBoss();

          // set charged flag
          m_bBossCharged = true;

          // change back to attacking state
          m_eCurState = States.STATES_MONSTER_ATTACKING;

          break;
        }

        case States.STATES_PREPARE_RECHARGE_MONSTER:
        {
          // set the count down timer before recharging monster
          m_fCurWaitingTime = m_pCurWaveInfo.MonsterRechargeDelay;

          // go to recharge state
          m_eCurState = States.STATES_RECHARGING_MONSTER;

          break;
        }

        case States.STATES_RECHARGING_MONSTER:
        {
          // recharge monsters
          // count down delay timer
          m_fCurWaitingTime -= Time.deltaTime;

          if(m_fCurWaitingTime < 1.0e-5f)
          {
            // get free slots
            int _iFreeMonsterSlot = m_aCurWaveInfoList.AllowedMaxMonsterCount - MonsterMgr.Instance.CurrentBossCount - MonsterMgr.Instance.CurrentMonsterCount;

            // there is some free slots
            if(_iFreeMonsterSlot > 0)
            {
              // get expected recharged monster count
              int _iExpectedRechargedMonsterCount = m_pCurWaveInfo.MonsterCount - MonsterMgr.Instance.CurrentMonsterCount;

              // spawn as many monster as possible
              int _iRechargedMonsterCount = _iExpectedRechargedMonsterCount < _iFreeMonsterSlot ? _iExpectedRechargedMonsterCount : _iFreeMonsterSlot;

              spawnMonster(_iRechargedMonsterCount);
            }

            // change back to attacking state
            m_eCurState = States.STATES_MONSTER_ATTACKING;
          }

          break;
        }

        case States.STATES_FINISHED_ALLWAVES_PLAYBACK:
        {
          // we have done with all attack waves
          // NOTHING to do here: GameMain logic will determine the attack result
          break;
        }

        default:
        {
          break;
        }
      }
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // reset all variables
    void reset()
    {
      // clear current level name
      m_szCurLevelName = "";

      // clear current monster flag ID
      m_iCurMonsterFlagID = -1;

      // clear current teleport ID
      m_iCurTelePortID = -1;

      // if the monster flag falled down
      if(null != m_pCurMonsterFlag)
      {
        // disable the monster flag
        m_pCurMonsterFlag.gameObject.SetActive(false);

        // remove the monster flag out of MonsterBuildings node
        m_pCurMonsterFlag.gameObject.transform.parent = null;

        // destroy the current monster flag
        m_pCurMonsterFlag.doDestroy();
        GameObject.Destroy(m_pCurMonsterFlag.gameObject);

        // clear current monster flag reference
        m_pCurMonsterFlag = null;
      }

      // clear current wave info list
      m_aCurWaveInfoList = null;

      // clear current wave index
      m_iCurWaveIndex = -1;

      // clear current wave info
      m_pCurWaveInfo = null;

      // clear current position for monster's flag
      m_vCurFlagPos = Vector3.zero;

      // clear boss charged flag
      m_bBossCharged = false;
    }

    /**
     * spawn a_iMonsterCount random monsters from m_pCurWaveInfo.MonsterIDList
     */
    void spawnMonster(int a_iMonsterCount)
    {
      // NOTHING to be spawned: return
      if(a_iMonsterCount <= 0)
      {
        return;
      }

      // how many monsters left to be spawned
      int _iMonsterLeft = a_iMonsterCount;

      // teleport info list
      List< TelePortInfo > _aTelePortInfoList = new List < TelePortInfo >(3);

      // each time get a random teleport info: random number of monsters
      while(true)
      {
        // create a new teleport info
        TelePortInfo _pCurTelePortInfo = new TelePortInfo();

        // get random number of monsters to be spawned: 1, 2, 3
        int _iExpectedSpawnCount = RandomNumberMgr.Instance.getIntegerVal(1, 3); // 1, 2, 3

        // get the real number of monsters to be spawned
        int _iCurSpawnCount = _iExpectedSpawnCount <= _iMonsterLeft ? _iExpectedSpawnCount : _iMonsterLeft;

        // safe guard
        if(_iCurSpawnCount > 3)
        {
          _iCurSpawnCount = 3;
        }

        // store the current spawn count
        _pCurTelePortInfo.MonsterCount = _iCurSpawnCount;

        // get random monster IDs
        for(int _iMonsterIdx = 0; _iMonsterIdx < _pCurTelePortInfo.MonsterCount; _iMonsterIdx++)
        {
          _pCurTelePortInfo.MonsterList[_iMonsterIdx] = m_pCurWaveInfo.MonsterIDList[RandomNumberMgr.Instance.getIntegerVal(0, m_pCurWaveInfo.MonsterIDList.Count -1 /* [0, Count -1] */ )];
        }

        // insert into teleport info list
        _aTelePortInfoList.Add(_pCurTelePortInfo);

        // decrease the number of monsters left
        _iMonsterLeft -= _iCurSpawnCount;

        // if we have done with setting up of teleport info list
        if(_iMonsterLeft <= 0)
        {
          break;
        }
      }

      // spawn all teleports
      spawnTeleport(_aTelePortInfoList, false);
    }

    void spawnBoss()
    {
      // teleport info list
      List< TelePortInfo > _aTelePortInfoList = new List < TelePortInfo >(1);

      // create a new teleport info
      TelePortInfo _pCurTelePortInfo = new TelePortInfo();

      _pCurTelePortInfo.MonsterCount = 1;
      _pCurTelePortInfo.MonsterList[0] = m_pCurWaveInfo.BossIDList[RandomNumberMgr.Instance.getIntegerVal(0, m_pCurWaveInfo.BossIDList.Count -1 /* [0, Count -1] */ )];

      // insert into teleport info list
      _aTelePortInfoList.Add(_pCurTelePortInfo);

      // spawn the teleport
      spawnTeleport(_aTelePortInfoList, true);
    }

    // return a spawn pos for monster
    bool getMonsterSpawnPos(out Transform a_pSpawnPos)
    {
      // clear return spawn pos
      a_pSpawnPos = null;

      // can NOT get a spawn pos
      if(false == (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).getMonsterSpawnPos(out a_pSpawnPos))
      {
        return false;
      }

      return true;
    }

    void spawnTeleport(List< TelePortInfo > a_aTelePortInfoList, bool a_bIsBoss)
    {
      // iterate over all teleports and get spawn postions for them
      for(int _iTelePortIdx = 0; _iTelePortIdx < a_aTelePortInfoList.Count; _iTelePortIdx++)
      {
        // get a random spawn pos transform for the current teleport
        Transform _pTelePortSpawnPosTrans = null;

        // try to get a spawn position for the current teleport
        // if we can NOT get a spawn pos: go to the next teleport
        if(false == getMonsterSpawnPos(out _pTelePortSpawnPosTrans))
        {
          DefaultGameLogger.Instance.dumpLogError("Can NOT get spawn pos for " + _iTelePortIdx + "th teleport");

          continue;
        }

        // make a tiny random shift: [-0.005, 0.005]  // -0.5cm <--> +0.5cm
        float _fXShift = Random.value /* [0, 1] */ * 0.01f /* [0, 0.01] */ - 0.005f; // [-0.005, +0.005]
        float _fZShift = Random.value /* [0, 1] */ * 0.01f /* [0, 0.01] */ - 0.005f; // [-0.005, +0.005]

        // create the current real spawn pos for the current teleport
        Vector3 _vCurTelePortSpawnPos = _pTelePortSpawnPosTrans.position;
        _vCurTelePortSpawnPos.x += _fXShift;
        _vCurTelePortSpawnPos.z += _fZShift;

        // spawn teleport
        GameObject _pCurTelePort = null;

        // can NOT create one teleport: go to the next teleport
        if(false == GameLevelMgr.Instance.CurrentGameLevel.getAsset(m_iCurTelePortID, out _pCurTelePort))
        {
          DefaultGameLogger.Instance.dumpLogError("Can NOT create" + _iTelePortIdx + "th teleport");

          continue;
        }

        // current teleport is NOT a Building: go to the next teleport
        if(null == _pCurTelePort.GetComponent< Building >())
        {
          DefaultGameLogger.Instance.dumpLogError("[" + _pCurTelePort.ToString() + "] " +  _iTelePortIdx + "th teleport does NOT have Building component");

          continue;
        }

        // enable this teleport
        _pCurTelePort.SetActive(true);

        // spawn this teleport: aligned with the spawn pos' frame
        _pCurTelePort.GetComponent< Building >().doSpawnCom(_vCurTelePortSpawnPos, _pTelePortSpawnPosTrans.forward, Vector3.up);

        // attach current teleport onto MonsterBuildings node
        _pCurTelePort.transform.parent = (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).MonsterBuildingsNode.transform;

        // get SpawnPosList from current TelePort node
        Transform _pSpawnPosList = _pCurTelePort.transform.Find("SpawnPosList");
        if(null == _pSpawnPosList)
        {
          DefaultGameLogger.Instance.dumpLogError("Can NOT find SpawnPosList in the TelePort");

          continue;
        }

        // iterate over monster ID list of current teleport
        for(int _iMonsterIdx = 0; _iMonsterIdx < a_aTelePortInfoList[_iTelePortIdx].MonsterCount; _iMonsterIdx++)
        {
          // get current spawn pos from teleport object
          Transform _pCurSpawnPos = _pSpawnPosList.GetChild(_iMonsterIdx);

          // spawn all monsters of the current teleport
          GameObject _pMonster = null;

          // if we are going to spawn boss
          if(true == a_bIsBoss)
          {
            if(false == MonsterMgr.Instance.spawnBoss(a_aTelePortInfoList[_iTelePortIdx].MonsterList[_iMonsterIdx], 
                                                      _pCurSpawnPos.position, 
                                                      _pCurSpawnPos.forward, 
                                                      Vector3.up, 
                                                      out _pMonster
                                                     )
              )
            {
              DefaultGameLogger.Instance.dumpLogError("Can NOT spawn" + _iMonsterIdx + "th Boss in " + _iTelePortIdx + "th teleport");

              continue;
            }
          }
          else
          {
            if(false == MonsterMgr.Instance.spawnMonster(a_aTelePortInfoList[_iTelePortIdx].MonsterList[_iMonsterIdx], 
                                                         _pCurSpawnPos.position, 
                                                         _pCurSpawnPos.forward, 
                                                         Vector3.up,
                                                         out _pMonster
                                                        )
              )
            {
              DefaultGameLogger.Instance.dumpLogError("Can NOT spawn" + _iMonsterIdx + "th monster in " + _iTelePortIdx + "th teleport");

              continue;
            }
          }
        }
      }
    }

    // callbacks
    void onMonsterFlagSpawnDone()
    {
      // after the monster flag has touched ground, fire game event
      Event.GameEventMgr.Instance.fireGlobalScopeEvent(Event.EventIDs.EVENT_IDS_ATTACK_ATTACKWARNING_FINISHED,
                                                       null,
                                                       Event.EventTriggerType.EVENT_TRIGGER_TYPE_AI,
                                                       this
                                                      );

      // set wave switching time
      m_fCurWaitingTime = m_pGameMain.WaveSwitchDelay;

      // change to finished state
      m_eCurState = States.STATES_ATTACKWARNING_FINISHED;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    class TelePortInfo
    {
      public TelePortInfo()
      {
        MonsterCount = 0;
        MonsterList = new int[3];
      }

      public int MonsterCount;
      public int[] MonsterList;
    }

    Dictionary< string, CWaveInfoList > m_mLevelMap;

    // main game entry
    GameMain m_pGameMain;

    enum States
    {
      // within doInitialize()
      STATES_INITIALIZING,
      // done with doInitialize()
      STATES_INITIALIZED,

      // warning given before start attack waves: like dropping down the monster flag
      STATES_PREPARE_ATTACKWARNING,
      // waiting for warning to finish
      STATES_WARNING_ATTACK,
      // done with warning: do the waiting before sending out monsters(starting the first wave)
      STATES_ATTACKWARNING_FINISHED,

      // wave playback
      // before playing back a wave
      STATES_PREPARE_WAVE,
      // check the total alive mounst count
      STATES_PREPARE_CHECK_ALIVEMONSTER_COUNT,
      // done with wave prepare: like waiting if there is a monster charge delay
      STATES_PREPARE_WAVE_FINISHED,
      // before charging the monster group in the current wave
      STATES_PREPARE_MONSTER_CHARGE,
      // charging the mounster group in the current wave
      STATES_CHARGING_MONSTER,
      // charging one boss
      STATES_CHARGEING_BOSS,
      // idle state during a wave playback
      STATES_MONSTER_ATTACKING,
      // before RE-charging some monsters
      STATES_PREPARE_RECHARGE_MONSTER,
      // RE-charging some monsters
      STATES_RECHARGING_MONSTER,
      // done with all waves
      STATES_FINISHED_ALLWAVES_PLAYBACK,

      // paused
      STATES_PAUSED,
    }

    // previous state
    States m_ePrevState;
    // current state
    States m_eCurState;
    // current level name
    string m_szCurLevelName;
    // current monster flag ID
    int m_iCurMonsterFlagID;
    // current teleport ID
    int m_iCurTelePortID;
    // current monster flag
    FallDownBuilding m_pCurMonsterFlag;
    // current position of monster flag
    Vector3 m_vCurFlagPos;
    // current waiting time
    float m_fCurWaitingTime;
    // current wave info list
    CWaveInfoList m_aCurWaveInfoList;
    // current wave index
    int m_iCurWaveIndex;
    // current wave info
    WaveInfo m_pCurWaveInfo;
    // current wave attack time
    float m_fCurAttackTime;
    // indicates if the boss has been charged for the current wave
    bool m_bBossCharged;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Logic

/**
 * monster manager class
 * - load in MonsterLevelData, which stores the PreFab of monsters
 *   + during the monster creation, we can use: Resources.Load("Materials/Props/DebugPinkMat") as Material;
 * - create a new monster related class
 *   + attach this monster onto MonsterDefenders node in the current scene
 * - release the given monster
 *   + dettach the given monster out of MonsterDefenders node in the current scene
 */
using UnityEngine;
using Framework.Utils;
using Framework.Logger;
using System.Collections.Generic;

namespace Game.Logic
{
  public class MonsterMgr : TManager < MonsterMgr >
  {
    #region PUBLIC METHODS
    // return true if there is some attacker: Monster or Boss
    public bool isAnyAttacker
    {
      get
      {
        return (0 == m_aMonsterList.Count) && (0 == m_aBossList.Count);
      }
    }

    /**
     * return true, if there is any monster can be attacked
     */
    public bool isAnyMonsterCanHandleHit()
    {
      if(0 == m_aMonsterList.Count)
      {
        return false;
      }

      for(int _iIdx = 0; _iIdx < m_aMonsterList.Count; _iIdx++)
      {
        if(true == m_aMonsterList[_iIdx].GetComponentInChildren< Entity >().canHandleHit())
        {
          return true;
        }
      }

      return false;
    }

    /**
     * return true, if there is any boss can be attacked
     */
    public bool isAnyBossCanHandleHit()
    {
      if(0 == m_aBossList.Count)
      {
        return false;
      }

      for(int _iIdx = 0; _iIdx < m_aBossList.Count; _iIdx++)
      {
        if(true == m_aBossList[_iIdx].GetComponentInChildren< Entity >().canHandleHit())
        {
          return true;
        }
      }

      return false;
    }

    // just return current monster count
    public int CurrentMonsterCount
    {
      get
      {
        return m_aMonsterList.Count;
      }
    }

    // just return current boss count
    public int CurrentBossCount
    {
      get
      {
        return m_aBossList.Count;
      }
    }

    // return current monster list
    public List< GameObject > CurrentMonsterList
    {
      get
      {
        return m_aMonsterList;
      }
    }

    // return current boss list
    public List< GameObject > CurrentBossList
    {
      get
      {
        return m_aBossList;
      }
    }

    // spawn a monster using the given ID and spawn settings
    // return true on success
    public bool spawnMonster(int a_iMonsterID, Vector3 a_vSpawnPosWs, Vector3 a_vSpawnForwardDirWs, Vector3 a_vSpawnUpDirWs, out GameObject a_pCreatedMonster)
    {
      // set null to returned monster reference
      a_pCreatedMonster = null;

      // try to create the monster from current game level

      // if we can NOT create the given monster: return false
      if(GameLevelMgr.Instance.CurrentGameLevel.getAsset(a_iMonsterID, out a_pCreatedMonster) == false)
      {
        DefaultGameLogger.Instance.dumpLogError("[MonsterMgr][spawnMonster]: Can NOT create Monster: " + a_iMonsterID);

        return false;
      }

      if(null == a_pCreatedMonster.GetComponent< Monster >())
      {
        DefaultGameLogger.Instance.dumpLogError("[MonsterMgr][spawnMonster]: [" + a_pCreatedMonster.ToString() + "] does NOT have Monster component");

        return false;
      }

      // enable the created monster
      a_pCreatedMonster.SetActive(true);

      // spawn this monster: aligned with the spawn pos' frame
      a_pCreatedMonster.GetComponent< Monster >().doSpawnCom(a_vSpawnPosWs, a_vSpawnForwardDirWs, a_vSpawnUpDirWs);

      // attach the current monster onto MonsterDefenders node
      a_pCreatedMonster.transform.parent = (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).MonsterDefendersNode.transform;

      // insert the new monster onto monster list
      m_aMonsterList.Add(a_pCreatedMonster);

      // fire monster spawned event: data Monster reference
      Event.GameEventMgr.Instance.fireGlobalScopeEvent(Event.EventIDs.EVENT_IDS_ATTACK_MONSTER_SPAWNED,
                                                       a_pCreatedMonster.GetComponent< Monster >(),
                                                       Event.EventTriggerType.EVENT_TRIGGER_TYPE_AI,
                                                       this
                                                      );

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[MonsterMgr][spawnMonster]: Monster Count: " + m_aMonsterList.Count);
#endif

      return true;
    }

    // spawn a boss using the given ID using the given spawn settings
    // return true on success
    public bool spawnBoss(int a_iBossID, Vector3 a_vSpawnPosWs, Vector3 a_vSpawnForwardDirWs, Vector3 a_vSpawnUpDirWs, out GameObject a_pCreatedBoss)
    {
      // set null to returned boss reference
      a_pCreatedBoss = null;

      // try to create the boss from current game level
      // if we can NOT create the given boss: return false
      if(GameLevelMgr.Instance.CurrentGameLevel.getAsset(a_iBossID, out a_pCreatedBoss) == false)
      {
        DefaultGameLogger.Instance.dumpLogError("[MonsterMgr][spawnBoss]: Can NOT create Boss: " + a_iBossID);

        return false;
      }

      if(null == a_pCreatedBoss.GetComponent< Monster >())
      {
        DefaultGameLogger.Instance.dumpLogError("[MonsterMgr][spawnBoss]: [" + a_pCreatedBoss.ToString() + "] does NOT have Monster component");

        return false;
      }

      // enable the created boss
      a_pCreatedBoss.SetActive(true);

      // spawn this boss: aligned with the spawn pos' frame
      a_pCreatedBoss.GetComponent< Monster >().doSpawnCom(a_vSpawnPosWs, a_vSpawnForwardDirWs, a_vSpawnUpDirWs);

      // attach the current boss onto MonsterDefenders node
      a_pCreatedBoss.transform.parent = (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).MonsterDefendersNode.transform;

      // insert the new boss onto boss list
      m_aBossList.Add(a_pCreatedBoss);

      // fire boss spawned event: data Boss reference
      Event.GameEventMgr.Instance.fireGlobalScopeEvent(Event.EventIDs.EVENT_IDS_ATTACK_BOSS_SPAWNED,
                                                       a_pCreatedBoss.GetComponent< Boss >(),
                                                       Event.EventTriggerType.EVENT_TRIGGER_TYPE_AI,
                                                       this
                                                      );

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[MonsterMgr][spawnBoss]: Boss Count: " + m_aBossList.Count);
#endif

      return true;
    }

    /**
     * destroy the given monster
     * return true on success
     */
    public bool destroyMonster(GameObject a_pMonster)
    {
      // try to find the given monster instance in the monster list
      int _iMonsterIndex = m_aMonsterList.IndexOf(a_pMonster);

      // can NOT find the given monster
      if(_iMonsterIndex < 0)
      {
        return false;
      }

      // fire monster dead event: data Monster reference
      Event.GameEventMgr.Instance.fireGlobalScopeEvent(Event.EventIDs.EVENT_IDS_ATTACK_MONSTER_DEAD,
                                                       a_pMonster.GetComponent< Monster >(),
                                                       Event.EventTriggerType.EVENT_TRIGGER_TYPE_AI,
                                                       this
                                                      );

      // found the given monster
      // remove it from MonsterDefenders node
      a_pMonster.transform.parent = null;

      // remove the given monster out of the monster list
      m_aMonsterList.RemoveAt(_iMonsterIndex);

      // destroy the given monster instance
      GameObject.Destroy(a_pMonster);

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[MonsterMgr][destroyMonster]: Monster Count: " + m_aMonsterList.Count);
#endif

      return true;
    }

    /**
     * destroy the given boss
     * return true on success
     */
    public bool destroyBoss(GameObject a_pBoss)
    {
      // try to find the given boss instance in the boss list
      int _iBossIndex = m_aBossList.IndexOf(a_pBoss);

      // can NOT find the given boss
      if(_iBossIndex < 0)
      {
        return false;
      }

      // fire boss dead event: data Boss reference
      Event.GameEventMgr.Instance.fireGlobalScopeEvent(Event.EventIDs.EVENT_IDS_ATTACK_BOSS_DEAD,
                                                       a_pBoss.GetComponent< Boss >(),
                                                       Event.EventTriggerType.EVENT_TRIGGER_TYPE_AI,
                                                       this
                                                      );

      // found the given boss
      // remove it from MonsterDefenders node
      a_pBoss.transform.parent = null;

      // remove the given boss out of the boss list
      m_aBossList.RemoveAt(_iBossIndex);

      // destroy the given boss instance
      GameObject.Destroy(a_pBoss);

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[MonsterMgr][destroyBoss]: Boss Count: " + m_aBossList.Count);
#endif

      return true;
    }

    // clear up all lists
    public void clearUp()
    {
      m_aMonsterList.Clear();
      m_aBossList.Clear();
    }

    #region PUBLIC OVERRIDDENS
    // the function called before using this manager
    public override void doInitialize()
    {
      // create monster list
      m_aMonsterList = new List< GameObject >(16);

      // create boss list
      m_aBossList = new List< GameObject >(2);

      // set initialization flag
      m_bIsInitialized = true;
    }

    // rest this manager
    public override void doReset()
    {
      // collect all monsters
      List<Character> _aCurMonsterList = new List<Character>(6);

      // store all spawned monsters, before destroy them
      for(int _iMIdx = 0; _iMIdx < m_aMonsterList.Count; _iMIdx++)
      {
        _aCurMonsterList.Add(m_aMonsterList[_iMIdx].GetComponent< Character >());
      }

      // destroy all monsters directly
      for(int _iMIdx = 0; _iMIdx < _aCurMonsterList.Count; _iMIdx++)
      {
        _aCurMonsterList[_iMIdx].doDestroy();
      }

      // m_aMonsterList is supposed to be cleared up within doDestroy()
      // here, we make a double check
      if(m_aMonsterList.Count != 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[MonsterMgr][doReset]: has NOT destroy ALL Monsters");
      }

      _aCurMonsterList.Clear();

      // collect all bosses
      for(int _iBIdx = 0; _iBIdx < m_aBossList.Count; _iBIdx++)
      {
        _aCurMonsterList.Add(m_aBossList[_iBIdx].GetComponent< Character >());
      }

      // destroy all bosses directly
      for(int _iBIdx = 0; _iBIdx < _aCurMonsterList.Count; _iBIdx++)
      {
        _aCurMonsterList[_iBIdx].doDestroy();
      }

      // m_aBossList is supposed to be cleared up within doDestroy()
      // here, we make a double check
      if(m_aBossList.Count != 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[MonsterMgr][doReset]: has NOT destroy ALL Bosses");
      }

      _aCurMonsterList.Clear();
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PRIVATE DATA MEMBERS
    // current monster list
    List< GameObject > m_aMonsterList;
    // current boss list
    List< GameObject > m_aBossList;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Logic

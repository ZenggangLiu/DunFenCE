/**
 * DunFencE Game Level Class
 * Calling Workflow: doWarmUP() --> doEnter() --> doExit() --> doCoolDown()
 * - doWarmUp:
 *   + preload assets, store all spawn pos and attack pos: called after the level has been loaded up within GameLevelMgr
 *   + spawn all serialized entity game objects
 * - doEnter: setup before entering a level
 * - doExit: all clear up work before unloading all assets
 * - doCoolDown: unload all assets: called before this level will be released within GameLevelMgr
 * 
 * Handling on 
 * - Farm Raided
 * - Success Defense
 * 
 * Spawn Pos:
 * - Z is the facing direction
 * 
 * Attack Pos:
 * - Z is the facing direction
 * 
 * NOTES:
 * 1. Each Game Level can have its own Camera(with NULL Target Texture): NOT Rendering To Texture 
 *    - its settings will be copied into NormScene' Camera, if the game is working in NON-AR mode
 * 2. Each Game Level can have Lights. But it will be disabled after loading this level
 * 
 * ----------- FROM HERE, EVERYTHING MUST BE UNDER Level Node -----------
 * 3. Monster Spawn Pos List: stored as SpawnPosList beneath MonsterBuildings Node
 * 4. Monster Attack Pos List: stored as AttackPosList beneath PlayerBuildings Node
 * 5. GUI node MUST be attached a LevelGUI derived class
 * 6. All Monster Buildings MUST be attached under MonsterBuildings Node
 *    + each Monster Building can have SpawnPosList
 * 7. All Player Buildings MUST be attached under PlayerBuildings Node
 *    + each Player Building can have AttackPosList
 * 8. All pre-created Monsters MUST be attached under MonsterDefenders Node
 * 9. All pre-created Player part characters MUST be attached under PlayerDefenders Node
 * 10. All drop offs MUST be attached to DropOffs Node
 */

//#define SHOW_ATTACK_TARGET // show debug information on next attack target

using UnityEngine;
using Framework.Utils;
using Framework.Mathe;
using Framework.Logger;
using System.Collections.Generic; // List< T >

namespace Game.Logic
{
  // attack target info
  public struct AttackTargetInfo
  {
    // index of owner building within the cache
    public int OwnerIndex;
    // index of this attack target
    public int Index;
    // world space position
    public Vector3 WorldSpacePos;
    // world space forward direction
    public Vector3 ForwardDirWS;
    // world space right direction
    public Vector3 RightDirWS;
  }

  // attack position info
  public struct AttackPosInfo
  {
    // index of owner building within the cache
    public int OwnerBuildingIndex;
    // index of this attack pos
    public int SelfIndex;
    // real attack position reference
    public AttackPos RealAttackPos;

    // constructor
    public AttackPosInfo(int a_iOwnerBuildingIndex, int a_iSelfIndex, AttackPos a_AttackPos)
    {
      OwnerBuildingIndex = a_iOwnerBuildingIndex;
      SelfIndex          = a_iSelfIndex;
      RealAttackPos      = a_AttackPos;
    }
  }

  // attack pos info of ONE building
  public struct BuildingAttackPosInfo
  {
    // the owner building reference
    public Building OwnerBuilding;
    // all attack targets of this building
    public AttackPosInfo[] AttackTargetList;
  }

  // attach this class onto the Root Node(Level) of the current Game Level
  public class FarmFenceLevel : GameLevel
  {
    #region PUBLIC DATA MEMBERS
    // minimal reward coin for a success defense
    public int MinRewardCoins = 0;
    // maximal reward coin for a success defense
    public int MaxRewardCoins = 0;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    /**
     * return MonsterBuildings node
     */
    public GameObject MonsterBuildingsNode
    {
      get
      {
        return m_pMonsterBuildingsNode;
      }
    }

    /**
     * return MonsterDefenders node
     */
    public GameObject MonsterDefendersNode
    {
      get
      {
        return m_pMonsterDefendersNode;
      }
    }

    /**
     * return PlayerBuildings node: defense building + attack target building
     */
    public GameObject PlayerBuildingsNode
    {
      get
      {
        return m_pPlayerBuildingsNode;
      }
    }

    /**
     * return PlayerDefenders node
     */
    public GameObject PlayerDefendersNode
    {
      get
      {
        return m_pPlayerDefendersNode;
      }
    }

    /**
     * return player's defense building list: different towers
     */
    public List< DefenseBuilding > PlayerDefenseBuildingList
    {
      get
      {
        return m_aPlayerDefenseBuildingList;
      }
    }

    /**
     * return player's barrack list
     */
    public List< Barrack > PlayerBarrackList
    {
      get
      {
        return m_aPlayerBarrackList;
      }
    }

    /**
     * return player's attack target building list
     */
    public List< Building > PlayerAttackTargetBuildingList
    {
      get
      {
        return m_aPlayerAttackTargetBuildingList;
      }
    }

    /**
     * return integer total HP
     */
    public int TotalHP
    {
      get
      {
        return (int)(m_fTotalPlayerBuildingHP + 0.5f);
      }
    }

    /**
     * return integer current HP
     */
    public int CurrentHP
    {
      get
      {
        return (int)(m_fCurPlayerBuildingHP + 0.5f);
      }
    }
   
    /**
     * return current building attack pos info cache
     */
    public BuildingAttackPosInfo[] CurrentBuildingAttackPosCache
    {
      get
      {
        return m_mBuildingAttackPosInfoCacheM;
      }
    }

    /**
     * gain HP
     */
    public void gainHP(int a_iHPGained)
    {
      // zero or negative gained HP: return
      if(a_iHPGained <= 0)
      {
        return;
      }

      // try to find all alive buildings
      int _iTargetBuildingCount = 0;

      // iterate over all building having HP
      for(int _iBIdx = 0; _iBIdx < m_mBuildingAttackPosInfoCacheM.Length; ++_iBIdx)
      {
        if(m_mBuildingAttackPosInfoCacheM[_iBIdx].OwnerBuilding.CurrentHP > 0)
        {
          _iTargetBuildingCount++;
        }
      }

      // there is NO building still alive: find all buildings having attachers
      if(0 == _iTargetBuildingCount)
      {
        // iterate over all buildings having attackers
        for(int _iBIdx = 0; _iBIdx < m_mBuildingAttackPosInfoCacheM.Length; ++_iBIdx)
        {
          if(m_mBuildingAttackPosInfoCacheM[_iBIdx].OwnerBuilding.CurrentAttackerCounter > 0)
          {
            _iTargetBuildingCount++;
          }
        }
      }

      // NO building found
      if(0 == _iTargetBuildingCount)
      {
#if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("[FarmFenCELevel][gainHP]: NO buidling will gain the given HP:" + a_iHPGained);
#endif

        return;
      }

      // found some buiding
      // split the entire HP among all buildings
      int _iHPGainedEachBuilding = (int)((float)a_iHPGained / (float)_iTargetBuildingCount);

      // let each building gain the HPT
      for(int _iBIdx = 0; _iBIdx < m_mBuildingAttackPosInfoCacheM.Length; ++_iBIdx)
      {
        if(m_mBuildingAttackPosInfoCacheM[_iBIdx].OwnerBuilding.CurrentHP > 0                  // a building having HP
           ||                                                                                  // or 
           m_mBuildingAttackPosInfoCacheM[_iBIdx].OwnerBuilding.CurrentAttackerCounter > 0     // a building having attackers
          )
        {
          m_mBuildingAttackPosInfoCacheM[_iBIdx].OwnerBuilding.gainHP((float)_iHPGainedEachBuilding);
        }
      }

      // update the cached version
#if !RETAIL
      //float _fPrevHP = m_fCurPlayerBuildingHP;
#endif

      // recalculate the current HP of all buildings
      m_fCurPlayerBuildingHP = 0.0f;
      for(int _iBIdx = 0; _iBIdx < m_mBuildingAttackPosInfoCacheM.Length; ++_iBIdx)
      {
        m_fCurPlayerBuildingHP += m_mBuildingAttackPosInfoCacheM[_iBIdx].OwnerBuilding.CurrentHP;
      }

#if !RETAIL
      /*
      DefaultGameLogger.Instance.dumpLogInfo("[FarmFenCELevel][gainHP]: Gained HP: " + (_iHPGainedEachBuilding * _iTargetBuildingCount) + ", Whole Building HP: " + _fPrevHP + " ---> " + m_fCurPlayerBuildingHP);
      for(int _iBIdx = 0; _iBIdx < m_mBuildingAttackPosInfoCacheM.Length; ++_iBIdx)
      {
        DefaultGameLogger.Instance.dumpLogInfo("[FarmFenCELevel][gainHP][" + m_mBuildingAttackPosInfoCacheM[_iBIdx].OwnerBuilding.gameObject.ToString() + "]: HP"  + m_mBuildingAttackPosInfoCacheM[_iBIdx].OwnerBuilding.CurrentHP);
      }
      */
#endif

      // resume the wave manager if needed
      if(true == GameWaveMgr.Instance.IsPaused)
      {
        GameWaveMgr.Instance.resumeWave(); 
      }
    }

    /**
     * return a monster spawn position randomly
     * true on success
     */
    public bool getMonsterSpawnPos(out Transform a_pSpawnPos)
    {
      if(0 == m_aMonsterSpawnPosList.Count)
      {
        DefaultGameLogger.Instance.dumpLogError("[FarmFenCELevel][getMonsterSpawnPos][" + gameObject.ToString() + "]: No Monster Spawn Position at all");

        a_pSpawnPos = null;

        return false;
      }
      else
      {
        int _iSpawnPosElementIndex =  RandomNumberMgr.Instance.getIntegerVal(0, m_aMonsterSpawnPosList.Count - 1); // [0, Count -1]

        a_pSpawnPos = m_aMonsterSpawnPosList[_iSpawnPosElementIndex];

        return true;
      }
    }

    /**
     * return a randomly chosen attack target info for monster: ALREADY shifted for Shoot Weapon
     * true on success
     */
    public bool getMonsterAttackTarget(Weapon a_pEquippedWeapon, ref AttackTargetInfo a_pAttackTarget)
    {
      // clear the returned attack position info
      a_pAttackTarget.OwnerIndex    = -1;
      a_pAttackTarget.Index         = -1;
      a_pAttackTarget.WorldSpacePos = Vector3.zero;
      a_pAttackTarget.ForwardDirWS  = Vector3.forward;
      a_pAttackTarget.RightDirWS    = Vector3.right;

      // all buildings are destroyed: return false
      if(m_fCurPlayerBuildingHP < 1.0e-5f)
      {
        return false;
      }

      // release all slots of working index pool
      m_aWorkingIndexPool.destroyAll();

      // release all slots in buiding index pool
      m_aAttackBuildingIndexPool.destroyAll();

      // release all slots in attack target index pool
      m_aAttackTargetIndexPool.destroyAll();

      // regenrate the working index pool
      for(int _iIdx = 0; _iIdx < m_mBuildingAttackPosInfoCacheM.Length; _iIdx++)
      {
        // create a new index info
        IndexInfo _pCurIndexInfo = m_aWorkingIndexPool.createObject();

        // store index of each building
        _pCurIndexInfo.Index = _iIdx;
      }

      // create a random building index pool
      for(int _iIdx = 0; _iIdx < m_mBuildingAttackPosInfoCacheM.Length; _iIdx++)
      {
        // get the current index slot
        IndexInfo _pCurIndexInfo = m_aAttackBuildingIndexPool.createObject();

        // randomly choose an index of working pool
        int _iSlotIndex = RandomNumberMgr.Instance.getIntegerVal(0, m_aWorkingIndexPool.ActiveObjectCount - 1);

        // store the index value of the chosen working pool slot
        _pCurIndexInfo.Index = m_aWorkingIndexPool[_iSlotIndex].Index;

        // destroy the chosen slot of working pool
        m_aWorkingIndexPool.destroyObject(_iSlotIndex);
      }

      // find the building with the least attacker count
      int _iLeastAttackerCount = 0x7FFFFFFF;

      // index of the building with least attacker count
      int _iBuildingIndex = -1;

      // go over the building index pool
      for(int _iIdx = 0; _iIdx < m_aAttackBuildingIndexPool.ActiveObjectCount; _iIdx++)
      {
        // NO HP in the current building
        if(m_mBuildingAttackPosInfoCacheM[m_aAttackBuildingIndexPool[_iIdx].Index].OwnerBuilding.CurrentHP <= 0)
        {
          continue;
        }

        // found a building with less attacker count
        if(m_mBuildingAttackPosInfoCacheM[m_aAttackBuildingIndexPool[_iIdx].Index].OwnerBuilding.CurrentAttackerCounter < _iLeastAttackerCount)
        {
          // update the currently least attacker count
          _iLeastAttackerCount = m_mBuildingAttackPosInfoCacheM[m_aAttackBuildingIndexPool[_iIdx].Index].OwnerBuilding.CurrentAttackerCounter;

          // store the current building index
          _iBuildingIndex = m_aAttackBuildingIndexPool[_iIdx].Index;
        }
      }

      // NO ideal building found
      if(_iBuildingIndex < 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[FarmFenCELevel][getMonsterAttackPos][" + gameObject.ToString() + "]: NO building with Attacker Count less than: " + _iLeastAttackerCount);

        return false;
      }

#if SHOW_ATTACK_TARGET && !RETAIL
      DefaultGameLogger.Instance.dumpLogInfo("[FarmFenCELevel][getMonsterAttackPos]: Found Building: " + m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].OwnerBuilding.gameObject.ToString() + " HP: " +  m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].OwnerBuilding.CurrentHP + " #Attacker: " + m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].OwnerBuilding.CurrentAttackerCounter);
#endif

      // increase the buidling's attacker count
      m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].OwnerBuilding.CurrentAttackerCounter++;

      // release the working index pool
      m_aWorkingIndexPool.destroyAll();

      // regenerate the working pool
      for(int _iIdx = 0; _iIdx < m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].AttackTargetList.Length; _iIdx++)
      {
        // create a new index info
        IndexInfo _pCurIndexInfo = m_aWorkingIndexPool.createObject();

        // store index of each building
        _pCurIndexInfo.Index = _iIdx;
      }

      // create a random attack target index pool
      for(int _iIdx = 0; _iIdx < m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].AttackTargetList.Length; _iIdx++)
      {
        // get the current index slot
        IndexInfo _pCurIndexInfo = m_aAttackTargetIndexPool.createObject();

        // randomly choose an index of working pool
        int _iSlotIndex = RandomNumberMgr.Instance.getIntegerVal(0, m_aWorkingIndexPool.ActiveObjectCount - 1);

        // store the index value of the chosen working pool slot
        _pCurIndexInfo.Index = m_aWorkingIndexPool[_iSlotIndex].Index;

        // destroy the chosen slot of working pool
        m_aWorkingIndexPool.destroyObject(_iSlotIndex);
      }

      // find the attach position with the least attacker count
      _iLeastAttackerCount = 0x7FFFFFFF;

      // index of the attack position with least attacker count
      int _iAttackPosIndex = -1;

      // just need any attack target type
      bool _bAnyAttackTarget = (null == a_pEquippedWeapon) ? true : (WeaponTypes.WEAPON_TYPES_MELEEWEAPON == a_pEquippedWeapon.WeaponType);

      // iterate over the attack target index pool
      // go over the building index pool
      for(int _iASIdx = 0; _iASIdx < m_aAttackTargetIndexPool.ActiveObjectCount; _iASIdx++)
      {
        // found an attack target with less attacker count
        if((_bAnyAttackTarget || (true == m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].AttackTargetList[m_aAttackTargetIndexPool[_iASIdx].Index].RealAttackPos.IsSupportingShoot)) // any attack target or shoot support
            &&                                                                                                                                                                        // and
            m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].AttackTargetList[m_aAttackTargetIndexPool[_iASIdx].Index].RealAttackPos.CurrentAttackerCount < _iLeastAttackerCount       // less attacker count
          )
        {
          // update the currently least attacker count
          _iLeastAttackerCount = m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].AttackTargetList[m_aAttackTargetIndexPool[_iASIdx].Index].RealAttackPos.CurrentAttackerCount;

          // store the current index
          _iAttackPosIndex = m_aAttackTargetIndexPool[_iASIdx].Index;
        }
      }

      // NO ideal attack target found
      if(_iAttackPosIndex < 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[FarmFenCELevel][getMonsterAttackPos]: NO attack target with Attacker Count less than: " + _iLeastAttackerCount + " within buidling: " + m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].OwnerBuilding.gameObject.ToString());

        return false;
      }

      // increase the attacker count of the chosen attack pos
      m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].AttackTargetList[_iAttackPosIndex].RealAttackPos.CurrentAttackerCount++;

#if SHOW_ATTACK_TARGET && !RETAIL
      DefaultGameLogger.Instance.dumpLogInfo("[FarmFenCELevel][getMonsterAttackPos]: Building: " + m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].OwnerBuilding.gameObject.ToString() + " #Attacker: --> " + m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].OwnerBuilding.CurrentAttackerCounter + " Target Index: " + _iAttackPosIndex + " #Attacker: --> " + m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].AttackTargetList[_iAttackPosIndex].RealAttackPos.CurrentAttackerCount);
#endif

      // copy owner index
      a_pAttackTarget.OwnerIndex = m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].AttackTargetList[_iAttackPosIndex].OwnerBuildingIndex;

      // copy attack target index
      a_pAttackTarget.Index = m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].AttackTargetList[_iAttackPosIndex].SelfIndex;

      // copy world position
      a_pAttackTarget.WorldSpacePos = m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].AttackTargetList[_iAttackPosIndex].RealAttackPos.transform.position;

      // copy right direction
      a_pAttackTarget.RightDirWS = m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].AttackTargetList[_iAttackPosIndex].RealAttackPos.transform.right;

      // decide which side the shift will be performed
      int _iShiftSideness = RandomNumberMgr.Instance.getIntegerVal(0, 1) == 0 ? -1 : 1;

      a_pAttackTarget.WorldSpacePos += a_pAttackTarget.RightDirWS * _iShiftSideness * ATTACK_POS_SHIFT_DISTANCE;

      // calculate the attack orientation: BuildingCenter - AttackPos
      a_pAttackTarget.ForwardDirWS = m_mBuildingAttackPosInfoCacheM[_iBuildingIndex].OwnerBuilding.transform.position - a_pAttackTarget.WorldSpacePos;

      // project the forward direction vector onto the ground plane
      a_pAttackTarget.ForwardDirWS = new Vector3(a_pAttackTarget.ForwardDirWS.x, 0.0f, a_pAttackTarget.ForwardDirWS.z);

      // normalize the forward direction
      a_pAttackTarget.ForwardDirWS.Normalize();

      // there is a minimal attack distance
      if(null != a_pEquippedWeapon                  // if there is ONE equipped weapon
         &&                                         // and
         a_pEquippedWeapon.MinAttackDist > 1.0e-5f  // with NON-ZERO minimal attack distance
        )
      {
        // shift the attack pos along its forward direction backwards
        a_pAttackTarget.WorldSpacePos -= a_pAttackTarget.ForwardDirWS * a_pEquippedWeapon.MinAttackDist;
      }

      return true;
    }

    #region PUBLIC OVERRIDDENS
    /**
     * called after this level has been loaded up within GameLevelMgr
     * - do some sort of initialization works
     * - use the given a_pLevelSwitchingData to preload any resources and set progress accordingly
     * - used to spawn all serialized Entity game objects
     */
    public override void doWarmUp(LevelSwitchingData a_pLevelSwitchingData)
    {
      // as first call base's doWarmUp()
      base.doWarmUp(a_pLevelSwitchingData);

      // create monster related lists
      m_aMonsterSpawnPosList            = new List< Transform >   (20);

      // player related lists
      m_aPlayerDefenseBuildingList      = new List< DefenseBuilding >(2);
      m_aPlayerBarrackList              = new List< Barrack >(1);
      m_aPlayerAttackTargetBuildingList = new List< Building >(2);

      // create index pool of attack buildings
      m_aAttackBuildingIndexPool        = new ObjectPool<IndexInfo>(2);

      // create index pool of attack targets of a building
      m_aAttackTargetIndexPool          = new ObjectPool<IndexInfo>(10);

      // create working index pool
      m_aWorkingIndexPool               = new ObjectPool<IndexInfo>(10);

      // reset all variables
      reset();

      // try to find MonsterBuildings node
      m_pMonsterBuildingsNode = GameObjectUtils.FindNode(gameObject, "MonsterBuildings");
      if(null == m_pMonsterBuildingsNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[FarmFenCELevel][doWarmUp][" + gameObject.ToString() + "]: Can NOT find MonsterBuildings Child Node");

        return;
      }

      // try to find MonsterDefenders node
      m_pMonsterDefendersNode = GameObjectUtils.FindNode(gameObject, "MonsterDefenders");

      // try to find PlayerBuildings child node
      m_pPlayerBuildingsNode = GameObjectUtils.FindNode(gameObject, "PlayerBuildings");
      if(null == m_pPlayerBuildingsNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[FarmFenCELevel][doWarmUp][" + gameObject.ToString() + "]: Can NOT find PlayerBuildings Child Node");

        return;
      }

      // check if all objects beneath PlayerBuildings node have Building.cs
      for(int _iBuildingIdx = 0; _iBuildingIdx < m_pPlayerBuildingsNode.transform.childCount; ++_iBuildingIdx)
      {
        if(null == m_pPlayerBuildingsNode.transform.GetChild(_iBuildingIdx).GetComponent < Building >())
        {
          DefaultGameLogger.Instance.dumpLogError("[FarmFenCELevel][doWarmUp][" +  m_pPlayerBuildingsNode.transform.GetChild(_iBuildingIdx).gameObject.ToString() + "]: is NOT a Building: NO Building.cs attached");

          return;
        }
      }

      // try to find PlayerDefenders node
      m_pPlayerDefendersNode = GameObjectUtils.FindNode(gameObject, "PlayerDefenders");

      // read in all spawn positions
      // iterate over all monster's buildings
      for(int _iMBuildingIdx = 0; _iMBuildingIdx < m_pMonsterBuildingsNode.transform.childCount; _iMBuildingIdx++)
      {
        // try to find SpawnPosList node within the current building node
        GameObject _pSpawnPosListNode = GameObjectUtils.FindNode(m_pMonsterBuildingsNode.transform.GetChild(_iMBuildingIdx).gameObject, "SpawnPosList");

        // if current building does NOT have any spawn position: go to the next building
        if(null == _pSpawnPosListNode)
        {
          continue;
        }

        // store all spawn positions
        for(int _iSpawnPosIdx = 0; _iSpawnPosIdx < _pSpawnPosListNode.transform.childCount; _iSpawnPosIdx++)
        {
          m_aMonsterSpawnPosList.Add(_pSpawnPosListNode.transform.GetChild(_iSpawnPosIdx)) ; 
        }
      }

      // if there is NO spawn pos at all
      if(0 == m_aMonsterSpawnPosList.Count)
      {
        DefaultGameLogger.Instance.dumpLogError("[FarmFenCELevel][doWarmUp][" + m_pMonsterBuildingsNode.ToString() + "]: does NOT have any SpawnPosList Child Node");

        return;
      }

      // read in all attack pos info
      // update loading progress to 90%
      if(a_pLevelSwitchingData.CurLevelSwitchingHandler != null)
      {
        a_pLevelSwitchingData.CurLevelSwitchingHandler(LevelSwitchingStates.LEVEL_SWITCHING_STATES_PROCESSING, 90.0f);
      }

      // read in all types of monster attack pos
      // iterate over all player's buildings
      for(int _iBuildingIdx = 0; _iBuildingIdx < m_pPlayerBuildingsNode.transform.childCount; ++_iBuildingIdx)
      {
        // if the current building node is a defense building
        if(m_pPlayerBuildingsNode.transform.GetChild(_iBuildingIdx).GetComponentInChildren< DefenseBuilding >() != null)
        {
          // insert it into list
          m_aPlayerDefenseBuildingList.Add(m_pPlayerBuildingsNode.transform.GetChild(_iBuildingIdx).GetComponentInChildren< DefenseBuilding >());
        }

        // if the current building node is a barrack building
        else if(m_pPlayerBuildingsNode.transform.GetChild(_iBuildingIdx).GetComponentInChildren< Barrack >() != null)
        {
          // insert it into list
          m_aPlayerBarrackList.Add(m_pPlayerBuildingsNode.transform.GetChild(_iBuildingIdx).GetComponentInChildren< Barrack >());
        }

        // get AttackPosList node from the current building node
        GameObject _pAttackPosListNode = GameObjectUtils.FindNode(m_pPlayerBuildingsNode.transform.GetChild(_iBuildingIdx).gameObject, "AttackPosList");

        // NO AttackPosList node: go to the next building
        if(null == _pAttackPosListNode)
        {
          continue;
        }

        // attack target building
        m_aPlayerAttackTargetBuildingList.Add(m_pPlayerBuildingsNode.transform.GetChild(_iBuildingIdx).GetComponentInChildren< Building >());
      }

      // create buliding's attack pos info cache
      // create the cache
      m_mBuildingAttackPosInfoCacheM = new BuildingAttackPosInfo[m_aPlayerAttackTargetBuildingList.Count];

      // iterate over all attack target buildings from player side
      for(int _iBIdx = 0; _iBIdx < m_aPlayerAttackTargetBuildingList.Count; _iBIdx++)
      {
        // store owner building
        m_mBuildingAttackPosInfoCacheM[_iBIdx].OwnerBuilding = m_aPlayerAttackTargetBuildingList[_iBIdx];

        // get AttackPosList node from the current building node
        GameObject _pCurAttackPosListNode = GameObjectUtils.FindNode(m_aPlayerAttackTargetBuildingList[_iBIdx].gameObject, "AttackPosList");

        // create attack pos info list for the current building
        m_mBuildingAttackPosInfoCacheM[_iBIdx].AttackTargetList = new AttackPosInfo[_pCurAttackPosListNode.transform.childCount];

        // store all attack pos
        // iterate over all attack pos of the current building
        for(int _iAPIdx = 0; _iAPIdx < _pCurAttackPosListNode.transform.childCount; _iAPIdx++)
        {
          // try to get the AttackPos component
          AttackPos _pCurAttackPos = _pCurAttackPosListNode.transform.GetChild(_iAPIdx).GetComponentInChildren< AttackPos >();

          // NO attack pos found
          if(null == _pCurAttackPos)
          {
            DefaultGameLogger.Instance.dumpLogError("[FarmFenCELevel][doWarmUp][" + _pCurAttackPosListNode.transform.GetChild(_iAPIdx).gameObject.ToString() + "]: does NOT have an AttackPos.cs component");

            continue;
          }

          // initialize this attack pos
          _pCurAttackPos.doInitialize();

          // store this attack pos into melee attack pos list
          m_mBuildingAttackPosInfoCacheM[_iBIdx].AttackTargetList[_iAPIdx] = new AttackPosInfo(_iBIdx,
                                                                                               _iAPIdx,
                                                                                               _pCurAttackPos
                                                                                              );
        }
      }

      // read HP, MP values
      m_fTotalPlayerBuildingHP = 0.0f;
      for(int _iIdx = 0; _iIdx < m_aPlayerAttackTargetBuildingList.Count; _iIdx++)
      {
        if(m_aPlayerAttackTargetBuildingList[_iIdx].MaxHealth > 0)
        {
          m_fTotalPlayerBuildingHP += m_aPlayerAttackTargetBuildingList[_iIdx].MaxHealth;
        }
      }

      // gain the maximal HP
      m_fCurPlayerBuildingHP = m_fTotalPlayerBuildingHP;

      // in defense level: equip the default weapon and update HP Bar and MP Bar
      if(m_pGameLevelGUI as Game.GUI.DefenseLevelGUI)
      {
        // equip the first weapon slot in weapon inventory by default
        (m_pGameLevelGUI as Game.GUI.DefenseLevelGUI).WeaponInventory.equipWeapon(0);

        // update HP Bar
        (m_pGameLevelGUI as Game.GUI.DefenseLevelGUI).setHPBar(m_fCurPlayerBuildingHP, m_fTotalPlayerBuildingHP);

        // update MP Bar
        (m_pGameLevelGUI as Game.GUI.DefenseLevelGUI).setMPBar(100.0f, 100.0f);
      }

      // update loading progress to 100%
      if(a_pLevelSwitchingData.CurLevelSwitchingHandler != null)
      {
        a_pLevelSwitchingData.CurLevelSwitchingHandler(LevelSwitchingStates.LEVEL_SWITCHING_STATES_DONE, 100.0f);
      }
    }

    /**
     * called after calling doWarmUp()
     * called after clicking start button
     */
    public override void doEnter()
    {
      // as first call base's doEnter()
      base.doEnter();

      // register all interesting events
      registerInterestingEvents();
    }

    /**
     * called before calling doCoolDown()
     * 
     */
    public override void doExit()
    {
      reset();

      // unregister all interesting events
      unRegisterInterestingEvents();

      // at last, call base's doExit()
      base.doExit();
    }

    /**
     * called before this level will be released within GameLevelMgr
     * - do some sort of clean up works
     */
    public override void doCoolDown()
    {
      // reset all other managers
      MonsterMgr.Instance.doReset();
      GameWaveMgr.Instance.doReset();
      WeaponMgr.Instance.doReset();
      ProjectileMgr.Instance.doReset();
      DropOffMgr.Instance.doReset();

      // at last, call base's doCoolDown()
      base.doCoolDown();
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS

    #region PROTECTED OVERRIDDENS
    // register all interesting events
    protected virtual void registerInterestingEvents()
    {
      // register building hit event
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_PLAYERBUILDING_HIT, onPlayerBuildingHit);
      // register all waves ended event: no event data
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_ALLWAVES_ENDED, onAllWavesEnded);
      // register monster spawned event: data Monster reference
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_MONSTER_SPAWNED, onMonsterSpawned);
      // register boss spawned event: data Boss reference
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_BOSS_SPAWNED, onMonsterSpawned);
      // register monster dead event: data Monster reference
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_MONSTER_DEAD, onMonsterDead);
      // register boss dead event: data Boss reference
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_BOSS_DEAD, onMonsterDead);
    }

    // unregister all interesting events
    protected virtual void unRegisterInterestingEvents()
    {
      // unregister building hit event
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_PLAYERBUILDING_HIT, onPlayerBuildingHit);
      // unregister all waves ended event: no event data
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_ALLWAVES_ENDED, onAllWavesEnded);
      // unregister monster spawned event: data Monster reference
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_MONSTER_SPAWNED, onMonsterSpawned);
      // unregister boss spawned event: data Boss reference
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_BOSS_SPAWNED, onMonsterSpawned);
      // unregister monster dead event: data Monster reference
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_MONSTER_DEAD, onMonsterDead);
      // unregister boss dead event: data Boss reference
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_ATTACK_BOSS_DEAD, onMonsterDead);
    }

    // called when the farm has been raided
    protected virtual void onFarmRaided()
    {
      // pause game wave manager
      GameWaveMgr.Instance.pauseWave();

      // pop up defend failed dialog
      if(m_pGameLevelGUI as Game.GUI.DefenseLevelGUI)
      {
        // stop shooting
        (m_pGameLevelGUI as Game.GUI.DefenseLevelGUI).onStopShootWeapon();

        // show defense failed dialog
        (m_pGameLevelGUI as Game.GUI.DefenseLevelGUI).showDefendFailedDialog();
      }

#if !RETAIL
      DefaultGameLogger.Instance.dumpLogInfo("[FarmFenCELevel][onFarmRaid]: Failed on Defend");
#endif
    }

    // called when the player has succeeded a farm defense
    protected virtual void onSuccessDefense()
    {
      // get a random reward coins for a success defense
      m_iDefenseReward = RandomNumberMgr.Instance.getIntegerVal(MinRewardCoins, MaxRewardCoins);

#if !RETAIL
      DefaultGameLogger.Instance.dumpLogInfo("[FarmFenCELevel][onSuccessDefense]: Well Done. You are the Hero");
#endif
    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    // the distance shift along attack pos's right vector: in order to separate monsters which have got the same attack pos
    protected static float ATTACK_POS_SHIFT_DISTANCE = 0.02f; // 2cm

    // monster related nodes
    // MonsterBuildings
    // used to get all spawn positions
    protected GameObject m_pMonsterBuildingsNode;
    // MonsterDefenders
    // used to get all spawned monsters
    protected GameObject m_pMonsterDefendersNode;
    // monster spawn position list
    protected List< Transform > m_aMonsterSpawnPosList;
    // monster attack position info cache
    protected BuildingAttackPosInfo[] m_mBuildingAttackPosInfoCacheM;

    // player related nodes
    // PlayerBuildings
    // used to get all player's buildings with attack position lists
    protected GameObject m_pPlayerBuildingsNode;
    // PlayerDefenders
    // used to get all spawned player related entities: hero, toady,...
    protected GameObject m_pPlayerDefendersNode;
    // player's defense building list
    protected List< DefenseBuilding > m_aPlayerDefenseBuildingList;
    // player's barrack building list
    protected List< Barrack > m_aPlayerBarrackList;
    // player's attack target buidling list
    protected List< Building > m_aPlayerAttackTargetBuildingList;

    // total player building's HP value
    protected float m_fTotalPlayerBuildingHP;
    // current player building's HP value
    protected float m_fCurPlayerBuildingHP;

    // reward coins got after a success defense
    protected int m_iDefenseReward;
    #endregion // PROTECTED DATA MEMBERS

    #region PRIVATE METHODS
    // callback for player building hit
    void onPlayerBuildingHit(Game.Event.GameEvent a_pEventRef)
    {
      // EVENT_IDS_ATTACK_PLAYERBUILDING_HIT
      // one player's building is hit: data: decreased HP(float)
      float _fDecreasedHP = (float)a_pEventRef.Data;

#if !RETAIL
      /*
      DefaultGameLogger.Instance.dumpLogInfo("[FarmFenCELevel][onPlayerBuildingHit]: Whole Building HP: " + m_fCurPlayerBuildingHP + " ---> " + (m_fCurPlayerBuildingHP - _fDecreasedHP));
      for(int _iBIdx = 0; _iBIdx < m_mBuildingAttackPosInfoCacheM.Length; ++_iBIdx)
      {
        DefaultGameLogger.Instance.dumpLogInfo("[FarmFenCELevel][onPlayerBuildingHit][" + m_mBuildingAttackPosInfoCacheM[_iBIdx].OwnerBuilding.gameObject.ToString() + "]: HP"  + m_mBuildingAttackPosInfoCacheM[_iBIdx].OwnerBuilding.CurrentHP);
      }
      */
#endif

      // decrease player building's HP
      m_fCurPlayerBuildingHP -= _fDecreasedHP;

      // update HP bar, if we are in defense level
      if(m_pGameLevelGUI as Game.GUI.DefenseLevelGUI)
      {
        (m_pGameLevelGUI as Game.GUI.DefenseLevelGUI).updateHPBar(m_fCurPlayerBuildingHP);
      }

      // NO enough HP
      if(m_fCurPlayerBuildingHP < 1.0e-5f)
      {
        // set to ZERO
        m_fCurPlayerBuildingHP = 0.0f;

        // fire raided event
        Event.GameEventMgr.Instance.fireGlobalScopeEvent(Event.EventIDs.EVENT_IDS_ATTACK_RAIDED,
                                                         null,
                                                         Event.EventTriggerType.EVENT_TRIGGER_TYPE_AI,
                                                         this
                                                        );

        // handle raided event
        onFarmRaided();
      }
    }

    // callback for completion of all waves
    void onAllWavesEnded(Game.Event.GameEvent a_pEventRef)
    {
      // if NO monster anymore, well done
      if(0 == m_iTotalMonsterCount)
      {
        onSuccessDefense();
      }
      // else turn on the flag
      else
      {
        // set all waves done flag
        m_bAllWavesDone = true;
      }
    }

    // callback when one monster/boss spawned
    void onMonsterSpawned(Game.Event.GameEvent a_pEventRef)
    {
      // increase the total monster(all kinds) count
      ++m_iTotalMonsterCount;

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[FarmFenCELevel][onMonsterSpawned]: Monster Count: " + m_iTotalMonsterCount);
#endif
    }

    // callback when one monster/boss dead
    void onMonsterDead(Game.Event.GameEvent a_pEventRef)
    {
      Monster _pAttacker = a_pEventRef.Data as Monster;

      // find the attack buiding for this monster
      if(_pAttacker.MountedBrain != null
         &&
         _pAttacker.MountedBrain as MonsterBrain
        )
      {
        // decrease the attacker count of the given attack target
        m_mBuildingAttackPosInfoCacheM[(_pAttacker.MountedBrain as MonsterBrain).CurrentAttackBuildingIndex].AttackTargetList[(_pAttacker.MountedBrain as MonsterBrain).CurrentAttackTargetIndex].RealAttackPos.CurrentAttackerCount--;

        // decrease the attacker count of the given building
        m_mBuildingAttackPosInfoCacheM[(_pAttacker.MountedBrain as MonsterBrain).CurrentAttackBuildingIndex].OwnerBuilding.CurrentAttackerCounter--;

#if SHOW_ATTACK_TARGET && !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("[FarmFenCELevel][onMonsterDead]: Building: " + m_mBuildingAttackPosInfoCacheM[(_pAttacker.MountedBrain as MonsterBrain).CurrentAttackBuildingIndex].OwnerBuilding.ToString() + " #Attacker: --> " + m_mBuildingAttackPosInfoCacheM[(_pAttacker.MountedBrain as MonsterBrain).CurrentAttackBuildingIndex].OwnerBuilding.CurrentAttackerCounter + "Target Index: " + (_pAttacker.MountedBrain as MonsterBrain).CurrentAttackTargetIndex + " #Attacker -->: " + m_mBuildingAttackPosInfoCacheM[(_pAttacker.MountedBrain as MonsterBrain).CurrentAttackBuildingIndex].AttackTargetList[(_pAttacker.MountedBrain as MonsterBrain).CurrentAttackTargetIndex].RealAttackPos.CurrentAttackerCount);
#endif
      }

      // decrease the total monster(all kinds) count
      --m_iTotalMonsterCount;

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[FarmFenCELevel][onMonsterDead]: Monster Count: " + m_iTotalMonsterCount);
#endif

      // negative monster count: strange
      if(m_iTotalMonsterCount < 0)
      {
        DefaultGameLogger.Instance.dumpLogWarning("[FarmFenCELevel][onMonsterDead]: Negative Monster Count");

        m_iTotalMonsterCount = 0;
      }

      if(true == m_bAllWavesDone      // all waves done
         &&                           // and
         0 == m_iTotalMonsterCount    // no a single monster
        )
      {
        onSuccessDefense();
      }
    }

    // reset all variables
    void reset()
    {
      // clear up all spawn positions
      m_aMonsterSpawnPosList.Clear();
      // clear up buiding attack pos info cache
      m_mBuildingAttackPosInfoCacheM = null;
      // clear up player's defense building list
      m_aPlayerDefenseBuildingList.Clear();
      // clear up index pool of attack buildings
      m_aAttackBuildingIndexPool.destroyAll();
      // clear up index pool of attack targets
      m_aAttackTargetIndexPool.destroyAll();
      // clear up working index pool
      m_aWorkingIndexPool.destroyAll();
      // clear up player's barrack building list
      m_aPlayerBarrackList.Clear();
      // clear up player's attack target building list
      m_aPlayerAttackTargetBuildingList.Clear();
      // clear monster count
      m_iTotalMonsterCount = 0;
      // clear total HP
      m_fTotalPlayerBuildingHP = 0.0f;
      // clear current HP
      m_fCurPlayerBuildingHP = 0.0f;
      // clear flag
      m_bAllWavesDone = false;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    class IndexInfo
    {
      public int Index;
    }

    // index pool for all attack buildings
    ObjectPool< IndexInfo > m_aAttackBuildingIndexPool;
    // index pool for all attack target of a given building
    ObjectPool< IndexInfo > m_aAttackTargetIndexPool;
    // working index pool
    ObjectPool< IndexInfo > m_aWorkingIndexPool;
    // total count of all kinds of monsters
    int m_iTotalMonsterCount;
    // indicate if all waves has been completed
    bool m_bAllWavesDone;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Logic

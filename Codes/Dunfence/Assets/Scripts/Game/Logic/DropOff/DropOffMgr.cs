/**
 * DropOff Manager Class
 */

using UnityEngine;
using Framework.Utils;
using Framework.Logger;
using Framework.Mathe;
using System.Collections.Generic; // List< T >

namespace Game.Logic
{
  public class DropOffMgr : TManager< DropOffMgr >
  {
    #region PUBLIC METHODS
    /**
     * return current drop off list
     */
    public List< GameObject > CurrentDropOffList
    {
      get
      {
        return m_aDropOffList;
      }
    }

    /**
     * create a random drop off
     */
    public void createDropOff(Character a_pDeadCharacter)
    {
      // for inventory level: NO drop off
      if(true == (GameLevelMgr.Instance.CurrentGameLevel as DemoLevel))
      {
        return;
      }

      // get a drop off ID
      int _iDropOffID = getDropOffID(a_pDeadCharacter);

      // NO drop off at all: return
      if(_iDropOffID < 0)
      {
        return;
      }

      // create the new dropoff
      GameObject _pDropOffNode = null;
      if(GameLevelMgr.Instance.CurrentGameLevel.getAsset(_iDropOffID, out _pDropOffNode) == false)
      {
        DefaultGameLogger.Instance.dumpLogError("[DropOffMgr][createDropOff]: Can NOT create DropOff asset: " + _iDropOffID);

        return;
      }

      // get Loot component
      Loot _pDropOff = _pDropOffNode.GetComponentInChildren< Loot >();
      if(null == _pDropOff)
      {
        DefaultGameLogger.Instance.dumpLogError("[DropOffMgr][createDropOff][" + _pDropOffNode.ToString() + "]: drop off does NOT have Loot.cs attached");

        return;
      }

      // add into drop off list
      m_aDropOffList.Add(_pDropOffNode);

      // spawn and initialize this dropoff
      _pDropOff.doSpawnCom(a_pDeadCharacter.transform.position, a_pDeadCharacter.transform.forward, a_pDeadCharacter.transform.up);

      // attach the new dropoff onto DropOffs node
      _pDropOffNode.transform.parent = GameLevelMgr.Instance.GameMainLevel.DropOffsNode.transform;

      // enable the new dropoff
      _pDropOffNode.SetActive(true);
    }

    /**
     * destroy a dropoff
     */
    public void destroyDropOff(GameObject a_pDropOffNode)
    {
      // try to find the given dropoff instance in the dropoff list
      int _iDropOffIndex = m_aDropOffList.IndexOf(a_pDropOffNode);

      // can NOT find the given dropoff
      if(_iDropOffIndex < 0)
      {
        return;
      }

      // remove the given monster out of the monster list
      m_aDropOffList.RemoveAt(_iDropOffIndex);

      // remove the given drop off out of DropOffs node
      a_pDropOffNode.transform.parent = null;

      // disable this drop off
      a_pDropOffNode.SetActive(false);

      // destroy this drop off
      GameObject.Destroy(a_pDropOffNode);
    }

    #region PUBLIC OVERRIDDENS
    /**
     * the function called before using this manager
     * set m_bIsInitialized to true, on successful initialization
     */
    public override void doInitialize()
    {
      // load up DropOffData.json and initialize the distribution array
      // try to load in DropOffData.json file
      TextAsset _pDropOffDataJsonFile = Resources.Load("DropOffData") as TextAsset; // an TextAsset: the representation of a Textural or Binary File
      if(null == _pDropOffDataJsonFile)
      {
        DefaultGameLogger.Instance.dumpLogError("[DropOffMgr][doInitialize]: Can NOT find DropOffData.json file");

        return;
      }

      // create and parse the json data at the same time
      JSONObject _pDropOffDataJson = new JSONObject(_pDropOffDataJsonFile.text);

      // NO MonsterDropOff node
      if(false == _pDropOffDataJson.HasField("MonsterDropOff"))
      {
        DefaultGameLogger.Instance.dumpLogError("[DropOffMgr][doInitialize]: Can NOT find MonsterDropOff node in DropOffData.json file");

        return;
      }

      // try to read in monster drop off config
      if(false == readDropOffConfig(_pDropOffDataJson["MonsterDropOff"], ref m_aDistributionArrayMonster))
      {
        DefaultGameLogger.Instance.dumpLogError("[DropOffMgr][doInitialize]: Can NOT read in Monster Drop Off Config");

        return;
      }

      // NO BossDropOff node
      if(false == _pDropOffDataJson.HasField("BossDropOff"))
      {
        DefaultGameLogger.Instance.dumpLogError("[DropOffMgr][doInitialize]: Can NOT find BossDropOff node in DropOffData.json file");

        return;
      }

      // try to read in boss drop off config
      if(false == readDropOffConfig(_pDropOffDataJson["BossDropOff"], ref m_aDistributionArrayBoss))
      {
        DefaultGameLogger.Instance.dumpLogError("[DropOffMgr][doInitialize]: Can NOT read in Boss Drop Off Config");

        return;
      }

      // create drop off list
      m_aDropOffList = new List< GameObject >(6);

      // set the initialization flag
      m_bIsInitialized = true;
    }

    /**
     * reset this manager
     * clear all internal data members: roll back to the SAME state as after calling doInitialize()
     * STILL marked as initialized
     */
    public override void doReset()
    {
      // destroy all dropoffs
      for(int _iIdx = 0; _iIdx < m_aDropOffList.Count; _iIdx++)
      {
        // remove the given drop off out of DropOffs node
        m_aDropOffList[_iIdx].transform.parent = null;

        // disable this drop off
        m_aDropOffList[_iIdx].SetActive(false);

        // destroy this drop off
        GameObject.Destroy(m_aDropOffList[_iIdx]);
      }

      // clear the drop off list
      m_aDropOffList.Clear();
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // read in drop off config data from the given Json Block, and create/initialize the given distribution array
    // return true on success
    bool readDropOffConfig(JSONObject a_pDropOffData, ref int[] a_aDistributionArray)
    {
      // NO TotalSlotCount node
      if(false == a_pDropOffData.HasField("TotalSlotCount"))
      {
        DefaultGameLogger.Instance.dumpLogError("[DropOffMgr][readDropOffConfig]: Can NOT find TotalSlotCount node");

        return false;
      }

      // TotalSlotCount node is NOT a number
      if(false == a_pDropOffData["TotalSlotCount"].IsNumber)
      {
        DefaultGameLogger.Instance.dumpLogError("[DropOffMgr][readDropOffConfig]: TotalSlotCount node is NOT a number");

        return false;
      }

      // ZERO total slot count
      if(0 == a_pDropOffData["TotalSlotCount"].i)
      {
        DefaultGameLogger.Instance.dumpLogError("[DropOffMgr][readDropOffConfig]: TotalSlotCount is ZERO");

        return false;
      }

      // NO Distribution node
      if(false == a_pDropOffData.HasField("Distribution"))
      {
        DefaultGameLogger.Instance.dumpLogError("[DropOffMgr][readDropOffConfig]: Can NOT find Distribution node");

        return false;
      }

      // Distribution node is NOT an array
      if(false == a_pDropOffData["Distribution"].IsArray)
      {
        DefaultGameLogger.Instance.dumpLogError("[DropOffMgr][readDropOffConfig]: Distribution node is NOT an array");

        return false;
      }

      // create distribution array
      a_aDistributionArray = new int[a_pDropOffData["TotalSlotCount"].i];

      // create free slot index list 
      List< int > _aFreeSlotIndexList = new List<int>(a_aDistributionArray.Length);

      // initialize distribution array and free slot index list
      for(int _iIdx = 0; _iIdx < a_aDistributionArray.Length; _iIdx++)
      {
        // initialize the whole array with -1: NOTHING in this slot
        a_aDistributionArray[_iIdx] = -1;

        // insert the index current slot
        _aFreeSlotIndexList.Add(_iIdx);
      }

      // read in the distribution of each drop off(loot)
      // get the Distribution json block
      JSONObject _pDistributionListJsonBlock = a_pDropOffData["Distribution"];

      // iterate over all elements of Distribution
      for(int _iCurDistribIdx = 0; _iCurDistribIdx < _pDistributionListJsonBlock.Count; _iCurDistribIdx++)
      {
        // get the current asset json block
        JSONObject _pCurDistribJsonBlock = _pDistributionListJsonBlock[_iCurDistribIdx];

        if(false == _pCurDistribJsonBlock.HasField("ID")           // NO ID node
           ||                                                      // or
           false == _pCurDistribJsonBlock["ID"].IsNumber           // ID node is NOT a number
          )
        {
          continue;
        }

        int _iCurDropOffID = (int)_pCurDistribJsonBlock["ID"].i;

        if(false == _pCurDistribJsonBlock.HasField("SlotCount")    // NO SlotCount node
           ||                                                      // or
           false == _pCurDistribJsonBlock["SlotCount"].IsNumber    // SlotCount node is NOT a number
          )
        {
          continue;
        }

        int _iCurSlotCount = (int)_pCurDistribJsonBlock["SlotCount"].i;

        // distribute this drop off
        for(int _iC = 0; _iC < _iCurSlotCount; _iC++)
        {
          // randomly select a free slot's index within free slot list
          int _iCurFreeSlotIndex = RandomNumberMgr.Instance.getIntegerVal(0, _aFreeSlotIndexList.Count - 1);

          // store the drop into the chosen slot
          a_aDistributionArray[_aFreeSlotIndexList[_iCurFreeSlotIndex]] = _iCurDropOffID;

          // remove this slot out of free slot list
          _aFreeSlotIndexList.Remove(_aFreeSlotIndexList[_iCurFreeSlotIndex]);
        }
      }

      return true;
    }

    // get a random drop off's ID
    int getDropOffID(Character a_pDeadCharacter)
    {
      // randomly choose a rolling power
      int _iRollPower = RandomNumberMgr.Instance.getIntegerVal(10, MAX_ROLL_POWER);

      switch(a_pDeadCharacter.CharacterType)
      {
      // normal monster
      case CharacterTypes.CHARACTER_TYPES_MONSTER:
        {
          // randomly choose a start slot
          int _iStartSlot = RandomNumberMgr.Instance.getIntegerVal(0, m_aDistributionArrayMonster.Length - 1);

          // get the end slot index
          int _iEndSlotIndex = (_iStartSlot + _iRollPower) % (m_aDistributionArrayMonster.Length - 1);

          // store the drop off ID
          return m_aDistributionArrayMonster[_iEndSlotIndex];
        }

        // boss
      case CharacterTypes.CHARACTER_TYPES_BOSS:
        {
          // randomly choose a start slot
          int _iStartSlot = RandomNumberMgr.Instance.getIntegerVal(0, m_aDistributionArrayBoss.Length - 1);

          // get the end slot index
          int _iEndSlotIndex = (_iStartSlot + _iRollPower) % (m_aDistributionArrayBoss.Length - 1);

          // store the drop off ID
          return m_aDistributionArrayBoss[_iEndSlotIndex];
        }

      default:
        {
          break;
        }
      }

      // return invalid ID
      return -1;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // maximal rolling power
    static int MAX_ROLL_POWER = 100;
    // the monster's distribution array: each slot is the asset ID: -1 means NOTHING
    int[] m_aDistributionArrayMonster;
    // the boss' distribution array: each slot is the asset ID: -1 means NOTHING
    int[] m_aDistributionArrayBoss;
    // current drop off list
    List< GameObject > m_aDropOffList;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Logic

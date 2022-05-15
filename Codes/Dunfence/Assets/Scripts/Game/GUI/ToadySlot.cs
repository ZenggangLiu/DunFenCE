/**
 * Slot within Toady Inventory: each slot UI element has ONE ToadySlot.cs attached
 */
using UnityEngine;
using UnityEngine.UI;
using Game.Logic;
using Framework.Utils;
using Framework.Logger;
using System.Collections.Generic;

namespace Game.GUI
{
  public class ToadySlot : MonoBehaviour
  {
    #region PUBLIC DATA MEMBERS
    // belonging tower type
    public TowerTypes TowerType = TowerTypes.TOWER_TYPES_UNKNOWN;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    // initialize this slot
    public void doInitialize()
    {
      // check belong tower type
      if(TowerTypes.TOWER_TYPES_UNKNOWN == TowerType)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][ToadySlot]: No Belong Tower Type");

        return;
      }

      // try to find the UI for free tower count
      GameObject _pGUINode = GameObjectUtils.FindNode(gameObject, "Cap");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][ToadySlot]: No Cap Node");

        return;
      }

      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "CValue");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][ToadySlot]: No CValue Node under Cap Node");

        return;
      }

      m_pCurFreeTowerUI = _pGUINode.GetComponentInChildren< Text >();
      if(null == m_pCurFreeTowerUI)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][ToadySlot]: CValue Node does NOT contain Text UI component");

        return;
      }

      // try to find the UI for tower service time
      _pGUINode = GameObjectUtils.FindNode(gameObject, "Time");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][ToadySlot]: No Time Node");

        return;
      }

      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "TValue");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][ToadySlot]: No TValue Node under Time Node");

        return;
      }

      m_pTowerServiceTimeUI = _pGUINode.GetComponentInChildren< Text >();
      if(null == m_pTowerServiceTimeUI)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][ToadySlot]: TValue Node does NOT contain Text UI component");

        return;
      }

      // set the default service time
      m_pTowerServiceTimeUI.text = "-";

      // try to find the UI for reward point required
      _pGUINode = GameObjectUtils.FindNode(gameObject, "Reward");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][ToadySlot]: No Reward Node");

        return;
      }

      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "RValue");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][ToadySlot]: No RValue Node under Reward Node");

        return;
      }

      m_pRPRequiredUI = _pGUINode.GetComponentInChildren< Text >();
      if(null == m_pRPRequiredUI)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][ToadySlot]: RValue Node does NOT contain Text UI component");

        return;
      }

      // set default reward point required
      m_pRPRequiredUI.text = "-";

      // initialize the tower list
      m_aTowerList = new List< DefenseBuilding >(2);

      // based on the given belong tower type, try to find all belong defense towers from current level
      // get all towers
      // wrong game level type
      if((GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel) == null)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][ToadySlot]: Current Game Level is NOT a DunFenCE Game Level");

        return;
      }

      List< DefenseBuilding > _aAllTowers = (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).PlayerDefenseBuildingList;  
      // iterate over all towers
      for(int _iIdx = 0; _iIdx < _aAllTowers.Count; _iIdx++)
      {
        // the same tower type as defined in this slot
        if(TowerType == _aAllTowers[_iIdx].TowerType)
        {
          // insert the tower
          m_aTowerList.Add(_aAllTowers[_iIdx]);

          // change service time
          m_pTowerServiceTimeUI.text = _aAllTowers[_iIdx].ServiceTime.ToString();

          // change reward point required
          m_pRPRequiredUI.text = _aAllTowers[_iIdx].RPRequired.ToString();
        }
      }

      // set free tower count
      m_iCurFreeTower = m_aTowerList.Count;

      // adjust UI elements
      adjustSlotUI();

      // register for service complete event of the given tower
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_DEFENSE_TOWER_SERVICE_DONE, onTowerServiceDone);

      // set initialization flag
      //m_bIsInitialized = true;
    }

    // destroy this slot
    public void doDestroy()
    {
      // unregister for service complete event of the given tower
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_DEFENSE_TOWER_SERVICE_DONE, onTowerServiceDone);

      // clear initialization flag
      //m_bIsInitialized = false;
    }

    // callback when this slot is pressed
    public void onSlotEquipped()
    {
      // NO tower: return
      if(0 == m_aTowerList.Count)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][ToadySlot][onSlotEquipped]: NO tower, But Slot Button pressed");
      }
      else
      {
        // find the first free tower
        for(int _iIdx = 0; _iIdx < m_aTowerList.Count; _iIdx++)
        {
          // in service: go to next one
          if(m_aTowerList[_iIdx].CurrentState != TowerServiceStates.TOWER_SERVICE_STATES_IDLE)
          {
            continue;
          }

          // trigger the found tower
          defendWithTower(m_aTowerList[_iIdx]);

          break;
        }

        // hide the toady inventory panel
        (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as DefenseLevelGUI).onEquipToady();
      }
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // adjust the slot's UI outlook
    void adjustSlotUI()
    {
      // free tower count
      m_pCurFreeTowerUI.text = m_iCurFreeTower.ToString();

      // color of free tower count
      // by 0, RED
      if(0 == m_iCurFreeTower)
      {
        m_pCurFreeTowerUI.color = Color.red;
      }

      // otherwise, GREEN
      else
      {
        m_pCurFreeTowerUI.color = Color.green;
      }

      // NO Sign
      // if no tower available
      if(0 == m_iCurFreeTower)
      {
        // disable the current slot's button behaviour
        if(GetComponentInChildren< Button >() != null)
        {
          GetComponentInChildren< Button >().enabled = false;
        }

        // try to find the NoSign node
        // Found: active it
        if(GameObjectUtils.FindNode(gameObject, "NoSign") != null)
        {
          GameObjectUtils.FindNode(gameObject, "NoSign").SetActive(true);
        }
      }
      else
      {
        // enable the current slot's button behaviour
        if(GetComponentInChildren< Button >() != null)
        {
          GetComponentInChildren< Button >().enabled = true;
        }

        // try to find the NoSign node
        // Found: deactive it
        if(GameObjectUtils.FindNode(gameObject, "NoSign") != null)
        {
          GameObjectUtils.FindNode(gameObject, "NoSign").SetActive(false);
        }
      }
    }

    // defending with the given tower
    void defendWithTower(DefenseBuilding a_pTower)
    {
      // check if user has enough RP
      // NO enough RP: return
      if(EconomyMgr.Instance.CurrentRP < a_pTower.RPRequired)
      {
#if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("user failed on starting defend tower. Required RP: " + a_pTower.RPRequired + ". But user JUST has: " + EconomyMgr.Instance.CurrentRP);
#endif

        // popup the error message box
        GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.showMessageBox(Game.GUI.MessageBoxTypes.MESSAGEBOX_TYPES_ERROR_SINGLEBTN, "No enough Reward Points!\nYou are trying to start a defend tower, which requires " + a_pTower.RPRequired + " rewards points.\nBut you JUST has " + EconomyMgr.Instance.CurrentRP, null, null);

        return;
      }

      // decrease the current available tower count
      --m_iCurFreeTower;

      // ajust the slot's UI
      adjustSlotUI();

      // defending with the given tower
      a_pTower.doDefendCom();

#if !RETAIL
      DefaultGameLogger.Instance.dumpLogInfo("[" + gameObject.ToString() + "][ToadySlot][defendWithTower]: Defending with Tower[" + a_pTower.TowerType.ToString() + "]. Free Tower: " + m_iCurFreeTower);
#endif
    }

    // callback for tower service done event
    void onTowerServiceDone(Game.Event.GameEvent a_pEventRef)
    {
      // wrong tower type: return
      if((a_pEventRef.Data as DefenseBuilding).TowerType != TowerType)
      {
        return;
      }

      // increase the available tower count
      ++m_iCurFreeTower;

      // adjust the slot's UI 
      adjustSlotUI();

#if !RETAIL
      DefaultGameLogger.Instance.dumpLogInfo("[" + gameObject.ToString() + "][ToadySlot][onTowerServiceDone]: Free Tower: " + m_iCurFreeTower);
#endif
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // defense tower list
    List< DefenseBuilding > m_aTowerList;
    // current available tower count
    int m_iCurFreeTower;
    // current available tower count UI element
    Text m_pCurFreeTowerUI;
    // service time
    Text m_pTowerServiceTimeUI;
    // reward point required
    Text m_pRPRequiredUI;
    // initialization flag
    //bool m_bIsInitialized;
    #endregion // PRIVATE DATA MEMBERS
  }
} // namespace Game.GUI

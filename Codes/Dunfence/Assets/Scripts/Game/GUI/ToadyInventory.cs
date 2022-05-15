/**
 * Toady Inventory Panel class
 * - initialize: find all slots
 */

using UnityEngine;
using Game.Logic;
using Framework.Utils;
using Framework.Logger;

namespace Game.GUI
{
  public class ToadyInventory : MonoBehaviour
  {
    #region PUBLIC METHODS
    // initialize this inventory
    public void doInitialize()
    {
      // try to find all slots
      GameObject _pContentNode = GameObjectUtils.FindNode(gameObject, "Content");
      if(null == _pContentNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "]: Can NOT find Content Node within Toady Inventory Panel");

        return;
      }

      if(0 == _pContentNode.transform.childCount)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "]: Content Node has NO toady slot");

        return;
      }

      // because the intialization of each slot will base on the data of current game level, we have to register on game level switched event
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_LEVEL_SWITCHED, onGameLevelSwitched);

      // set initialization flag
      //m_bIsInitialized = true;
    }

    /**
     * destroy this inventory
     */
    public void doDestroy()
    {
      // reset all slots
      // find the Content node
      GameObject _pContentNode = GameObjectUtils.FindNode(gameObject, "Content");

      // iterate over all toady slots under the Content node: intialize all slots
      for(int _iCIdx = 0; _iCIdx < _pContentNode.transform.childCount; _iCIdx++)
      {
        ToadySlot _pCurSlot = _pContentNode.transform.GetChild(_iCIdx).GetComponentInChildren< ToadySlot >();

        // NO ToadySlot component
        if(null == _pCurSlot)
        {
          DefaultGameLogger.Instance.dumpLogError("[" + _pContentNode.transform.GetChild(_iCIdx).gameObject.ToString() + "]: Slot does NOT have ToadySlot.cs");

          return;
        }

        // destroy the slot
        _pCurSlot.doDestroy();
      }

      // unregister event handler
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_LEVEL_SWITCHED, onGameLevelSwitched);

      // clear initialization flag
      //m_bIsInitialized = false;
    }

    // callback when the weapon inventory panel is about to be shown
    // re-layout all weapon inventory slots
    public void onShowPanel()
    {
      gameObject.SetActive(true);
    }

    // callback when the weapon inventory panel is about to be hiden
    // called from hide animation if exists or directly from code
    public void onHidePanel()
    {
      gameObject.SetActive(false);
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // callback for game level switched event
    void onGameLevelSwitched(Game.Event.GameEvent a_pEventRef)
    {
      // find the Content node
      GameObject _pContentNode = GameObjectUtils.FindNode(gameObject, "Content");

      // iterate over all toady slots under the Content node: intialize all slots
      for(int _iCIdx = 0; _iCIdx < _pContentNode.transform.childCount; _iCIdx++)
      {
        ToadySlot _pCurSlot = _pContentNode.transform.GetChild(_iCIdx).GetComponentInChildren< ToadySlot >();

        // NO ToadySlot component
        if(null == _pCurSlot)
        {
          DefaultGameLogger.Instance.dumpLogError("[" + _pContentNode.transform.GetChild(_iCIdx).gameObject.ToString() + "]: Slot does NOT have ToadySlot.cs");

          return;
        }

        // initialize the slot
        _pCurSlot.doInitialize();
      }
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // initialization flag
    //bool m_bIsInitialized;
    #endregion // PRIVATE DATA MEMBERS
  }
} // namespace Game.GUI

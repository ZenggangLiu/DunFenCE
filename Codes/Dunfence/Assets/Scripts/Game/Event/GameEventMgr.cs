using UnityEngine;
using Framework.Utils;
using Framework.Logger;
using System.Collections.Generic; // Dictionary< Key, Value >

namespace Game.Event
{
  public class GameEventMgr : TManager < GameEventMgr >
  {
    #region PUBLIC METHODS
    // callback
    public delegate void EventHandler(Game.Event.GameEvent a_pEventRef);

    /**
     * register a event handler for the given global scope event
     * return true: on success
     */
    public bool registerGlobalScopeEventHandler(Game.Event.EventIDs a_eEventID, 
                                                EventHandler a_pEventHandler
                                               )
    {
      if(false == m_mEventHandlerMap.ContainsKey(a_eEventID))
      {
        m_mEventHandlerMap.Add(a_eEventID, a_pEventHandler);
      }
      else
      {
        m_mEventHandlerMap[a_eEventID] += a_pEventHandler;  
      }

      return true;
    }

    /**
     * unRegister the given event handler for the given global scope event
     * return ture: on success
     */
    public bool unRegisterGlobalScopeEventHandler(Game.Event.EventIDs a_eEventID, 
                                                  EventHandler a_pEventHandler
                                                 )
    {
      if(false == m_mEventHandlerMap.ContainsKey(a_eEventID))
      {
        DefaultGameLogger.Instance.dumpLogError("[GameEventMgr][unRegisterGlobalScopeEventHandler]: try to Unregister NOT EXISTING event: " + a_eEventID.ToString());

        return false;
      }

      m_mEventHandlerMap[a_eEventID] -= a_pEventHandler;

      return true;
    }

    /**
     * fire the given event
     * return true: on success
     */
    public bool fireGlobalScopeEvent(Game.Event.EventIDs a_eEventID,
                                     System.Object a_pEventData,
                                     Game.Event.EventTriggerType a_eTriggerType,
                                     System.Object a_pTrigger
                                    )
    {
      if(false == m_mEventHandlerMap.ContainsKey(a_eEventID)  // NOT contains the given key
         ||                                                   // or
         null == m_mEventHandlerMap[a_eEventID]               // NO event handler
        )
      {
        return false;
      }

      // create the game event on stack
      GameEvent _pNewGameEvent = new GameEvent(a_eEventID, a_pEventData, a_eTriggerType, a_pTrigger);

      m_mEventHandlerMap[a_eEventID](_pNewGameEvent);

      return true;
    }

    #region PUBLIC OVERRIDDENS
    // the function called before using this manager
    public override void doInitialize()
    {
      // create the event handler map
      m_mEventHandlerMap = new Dictionary< Game.Event.EventIDs, EventHandler >(10);
    }

    // rest this manager
    public override void doReset()
    {
      // clear up all collected events
      m_mEventHandlerMap.Clear();
    }

    // update function
    public override void doUpdate()
    {

    }

    // post update function called after all update() functions had been called
    public override void doPostUpdate()
    {

    }

    // the function called before destroying this manager
    public override void doDestroy()
    {
      // reset this manager
      doReset();

      // destroy the event handler map
      m_mEventHandlerMap = null;
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED DATA MEMBERS
    // map of event handlers
    protected Dictionary< Game.Event.EventIDs, EventHandler > m_mEventHandlerMap;
    #endregion // PROTECTED DATA MEMBERS
  }
} // namespace Game.Event

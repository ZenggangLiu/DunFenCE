/**
 * Device Input Manager
 * - collect all events within doUpdate(), which MUST be called within GameMain's Update()
 *   + collect touch events: real device touch events, simulated touch events from keyboard, mouse and joystick
 *   + collect tilt events
 * - call handleAllEvents() to process all collected device events during GameMain's Update(): like GameMain.Update() --> DeviceInputMgr.handleAllEvents()
 * - clear event collection within doPostUpdate()
 * 
 * Usage:
 *   - attach this code as a component to any Game Object
 *   - copy DeviceInputMgrExecOrderChecker.cs into Editor folder: it will set execution order
 */

// define this if we want to have a full log on all collected events of each frame
//#define REPORT_COLLECTED_EVENTS

using Framework.Utils;
using Framework.Logger;

namespace Framework.Inputs
{
  public class DeviceInputMgr : TManager< DeviceInputMgr >
  {
    #region PUBLIC METHODS
    // device event handler
    public delegate void DeviceEventHandler(Framework.Inputs.DeviceEvent a_pEvent);

    /**
     * register the given device event handler
     */
    public void registerDeviceEventHanlder(DeviceEventHandler a_pDeviceEventHandler)
    {
      if(null == a_pDeviceEventHandler)
      {
        return;
      }

      m_aDeviceEventHandlerList += a_pDeviceEventHandler;
    }

    /**
     * unregister the given device event handler
     */
    public void unRegisterDeviceEventHandler(DeviceEventHandler a_pDeviceEventHandler)
    {
      m_aDeviceEventHandlerList -= a_pDeviceEventHandler;
    }

    /**
     * handle all collected events
     * - call all registered event handlers
     */
    public void handleAllEvents()
    {
      // no event: return
      if(0 == m_pDeviceEventQueue.ActiveObjectCount)
      {
        return;
      }

      // no handler: return
      if(null == m_aDeviceEventHandlerList)
      {
        return;
      }

      // handle all events
      for(int _iIdx = 0; _iIdx < m_pDeviceEventQueue.ActiveObjectCount; _iIdx++)
      {
        m_aDeviceEventHandlerList(m_pDeviceEventQueue[_iIdx]);
      }
    }

    #region PUBLIC OVERRIDDENS
    /**
     * the function called before using this manager
     * set m_bIsInitialized to true, on successful initialization
     */
    public override void doInitialize()
    {
      // create device event queue
      m_pDeviceEventQueue = new Utils.ObjectPool< DeviceEvent >();

      // initialize device event queue
      m_pDeviceEventQueue.initialize(20);  // default 20 device events per frame

      // set initialization flag
      m_bIsInitialized = true;
    }

    /**
     * update function
     */
    public override void doUpdate()
    {
      // NOT initialized: return
      if(false == m_bIsInitialized)
      {
        return;
      }

      // collect all device input events
      // check tilt events
      AccelerometerChecker.doCheck(m_pDeviceEventQueue);

      // check touch events
      TouchEventChecker.doCheck(m_pDeviceEventQueue);

#if REPORT_COLLECTED_EVENTS
      // report events collected
      for(int _iIdx = 0; _iIdx < m_pDeviceEventQueue.ActiveObjectCount; _iIdx++)
      {
#if !RETAIL
        Logger.DefaultGameLogger.Instance.dumpLogInfo("[Frame: " + Time.frameCount + "]" + m_pDeviceEventQueue[_iIdx].ToString());
#endif
      }
#endif // #if REPORT_COLLECTED_EVENTS
    }

    // post-update function called after all update() functions had been called
    public override void doPostUpdate()
    {
      // clear up the whole input event collection
      m_pDeviceEventQueue.destroyAll();
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PRIVATE DATA MEMBERS
    // device event queue
    Utils.ObjectPool < DeviceEvent > m_pDeviceEventQueue;
    // event handler list
    DeviceEventHandler m_aDeviceEventHandlerList;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Framework.Inputs

/**
 * Logging Manager
 * 
 *  Usage: 
 *    - Get Single Instance: Framework.Utils.LoggingMgr.Instance
 *    - Register Logging Event Handlers: registerLoggingEventHandler(...). for example, registerLoggingEventHandler(ConsoleLoggingEventHandler.onLoggingEvent);
 *    - UnRegister Logging Event Handlers: unRegisterLoggingEventHandler() 
 *    - dump log with Framework.Utils.LoggingMgr.Instance.dumpLogVerbose("ABC", "def", 1);
 */

using UnityEngine;

namespace Framework.Logger
{
  public class LoggingMgr : Framework.Utils.TManager< LoggingMgr >
  {
    #region PUBLIC METHODS
    // Log Level define
    public enum LogLevel
    {
      LOGLEVEL_ERROR_LOGGING_LEVEL      = 1,
      LOGLEVEL_WARNING_LOGGING_LEVEL    = 2,
      LOGLEVEL_INFO_LOGGING_LEVEL       = 3,
      LOGLEVEL_DEBUG_LOGGING_LEVEL      = 4,
      LOGLEVEL_MAX_LOGGING_LEVEL        = LOGLEVEL_DEBUG_LOGGING_LEVEL
    };

    // callback of logging event
    public delegate void LoggingEventHandler(LoggingMgr.LogLevel a_eLogLevel, params System.Object[] a_aParameterList);

    // callback of destroy event
    public delegate void DestroyEventHandler();

    // default constructor
    public LoggingMgr()
    {
      m_aCurLoggingeEventHandlerList = null;
      m_aCurDestroyEventHandlerList = null;
#if DEBUG && !RETAIL
      m_eCurLogLevel = LogLevel.LOGLEVEL_MAX_LOGGING_LEVEL;
#else
      m_eCurLogLevel = LogLevel.LOGLEVEL_ERROR_LOGGING_LEVEL;
#endif
    }

    // get and set current logging level
    public LogLevel CurrentLogLevel
    {
      get
      {
        return m_eCurLogLevel;
      }

      set
      {
        if (value > LogLevel.LOGLEVEL_MAX_LOGGING_LEVEL)
        {
          return;
        }

        m_eCurLogLevel = value;
      }
    }

    // register a logging event handler
    public void registerLoggingEventHandler(LoggingEventHandler a_pLoggingEventHandler)
    {
      registerEventHandlers(a_pLoggingEventHandler, null);
    }

    // register a logging event handler and a destroy event handler
    public void registerEventHandlers(LoggingEventHandler a_pLoggingEventHandler,
                                      DestroyEventHandler a_pDestroyEventHandler
                                     )
    {
      if(null != a_pLoggingEventHandler)
      {
        m_aCurLoggingeEventHandlerList -= a_pLoggingEventHandler;  
        m_aCurLoggingeEventHandlerList += a_pLoggingEventHandler;
      }

      if(null != a_pDestroyEventHandler)
      {
        m_aCurDestroyEventHandlerList  -= a_pDestroyEventHandler;
        m_aCurDestroyEventHandlerList  += a_pDestroyEventHandler;
      }
    }

    // unregister the given logging event handler
    public void unRegisterLoggingEventHandler(LoggingEventHandler a_pLoggingEventHandler)
    {
      unRegisterEventHandlers(a_pLoggingEventHandler, null);
    }

    // unregister the given logging event handler and destory event handler
    public void unRegisterEventHandlers(LoggingEventHandler a_pLoggingEventHandler,
                                        DestroyEventHandler a_pDestroyEventHandler
                                       )
    {
      if(null != a_pLoggingEventHandler)
      {
        m_aCurLoggingeEventHandlerList -= a_pLoggingEventHandler;
      }

      if(null != a_pDestroyEventHandler)
      {
        m_aCurDestroyEventHandlerList -= a_pDestroyEventHandler;  
      }
    }

    // dump the different kinds of logs
    // dump error
    public void dumpLogError(params object[] a_aParameterList)
    {
      dumpLog(LogLevel.LOGLEVEL_ERROR_LOGGING_LEVEL, a_aParameterList);
    }

    // dump warning
    public void dumpLogWarning(params object[] a_aParameterList)
    {
      dumpLog(LogLevel.LOGLEVEL_WARNING_LOGGING_LEVEL, a_aParameterList);
    }

    // dump info
    public void dumpLogInfo(params object[] a_aParameterList)
    {
      dumpLog(LogLevel.LOGLEVEL_INFO_LOGGING_LEVEL, a_aParameterList);
    }

    // dump debug info
    public void dumpLogDebug(params object[] a_aParameterList)
    {
#if DEBUG || _DEBUG
      dumpLog (LogLevel.LOGLEVEL_DEBUG_LOGGING_LEVEL, a_aParameterList);
#endif
    }

    #region PUBLIC OVERRIDDENS
    // the function called before using this manager
    public override void doInitialize()
    {

    }

    // pre-update function
    public override void doPreUpdate()
    {

    }

    // update function
    public override void doUpdate()
    {

    }

    // post-update function called after all update() functions had been called
    public override void doPostUpdate()
    {

    }

    // the function called before destroying this manager
    public override void doDestroy()
    {
      if (null != m_aCurDestroyEventHandlerList)
      {
        m_aCurDestroyEventHandlerList();

        m_aCurDestroyEventHandlerList = null;
      }
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    /**
     * dump given information using all registered Logging Event Handlers
     */
    void dumpLog(LogLevel a_eLogLevel, params object[] a_aParameterList)
    {
      // given log level is too high: return
      if (a_eLogLevel > m_eCurLogLevel)
      {
        return;
      }

      // dump information
      // call all registered logging event handlers one by one
      if (null != m_aCurLoggingeEventHandlerList)
      {
        m_aCurLoggingeEventHandlerList(a_eLogLevel, a_aParameterList);
      }
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // current logging message event handler list
    LoggingEventHandler m_aCurLoggingeEventHandlerList;
    // current shut down event handler list
    DestroyEventHandler m_aCurDestroyEventHandlerList;
    // current log level
    LogLevel m_eCurLogLevel;
    #endregion // PRIVATE DATA MEMBERS
  };

} // namespace Framework.Logger

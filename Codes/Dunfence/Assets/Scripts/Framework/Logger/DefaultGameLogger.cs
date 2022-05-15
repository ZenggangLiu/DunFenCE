/**
 * Default Implementation of a Game Logger
 * - read in logger_settings.json
 * - register given logger implementation based on the content of logger_settings.json file
 */

namespace Framework.Logger
{
  public class DefaultGameLogger : Framework.Utils.TSingleTon< DefaultGameLogger >
  {
    #region PUBLIC METHODS
    // dump the different kinds of logs
    // dump error
    public void dumpLogError(params object[] a_aParameterList)
    {
      m_pLoggingMgr.dumpLogError(a_aParameterList);
    }

    // dump warning
    public void dumpLogWarning(params object[] a_aParameterList)
    {
      m_pLoggingMgr.dumpLogWarning(a_aParameterList);
    }

    // dump info
    public void dumpLogInfo(params object[] a_aParameterList)
    {
      m_pLoggingMgr.dumpLogInfo(a_aParameterList);
    }

    // dump debug info
    public void dumpLogDebug(params object[] a_aParameterList)
    {
      m_pLoggingMgr.dumpLogDebug(a_aParameterList);
    }

    // default constructor: for generic usage TSingleTon< T >
    public DefaultGameLogger()
    {
      // get logging manager
      m_pLoggingMgr = LoggingMgr.Instance;

      // register event handlers
      m_pLoggingMgr.registerLoggingEventHandler(ConsoleLogger.onLoggingEvent);
      m_pLoggingMgr.registerEventHandlers(FileLogger.onLoggingEvent, FileLogger.onDestroyEvent);
    }

    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // destructor
    ~DefaultGameLogger()
    {      
      // called destroy event
      FileLogger.onDestroyEvent();

      // unregister event handlers
      m_pLoggingMgr.unRegisterLoggingEventHandler(ConsoleLogger.onLoggingEvent);
      m_pLoggingMgr.unRegisterEventHandlers(FileLogger.onLoggingEvent, FileLogger.onDestroyEvent);

      // release logging manager reference
      m_pLoggingMgr = null;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // logging manager
    LoggingMgr m_pLoggingMgr;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Framework.Logger

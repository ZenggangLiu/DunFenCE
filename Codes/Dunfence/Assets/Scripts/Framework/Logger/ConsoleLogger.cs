/**
 * Console Window Logger: dump all logging messages into console window
 */

using UnityEngine;

namespace Framework.Logger
{
  public static class ConsoleLogger
  {
    #region PUBLIC METHODS
    // callback for handling logging event
    public static void onLoggingEvent(LoggingMgr.LogLevel a_eLogLevel,
                                      params System.Object[] a_aParameterList
                                     )
    {
      // nothing to be dumped: return
      if (0 == a_aParameterList.Length)
      {
        return;
      }

      // generate the logging message
      string _szLoggingMessage = "";
      for(int _iIdx = 0; _iIdx < a_aParameterList.Length; _iIdx++)
      {
        _szLoggingMessage += a_aParameterList[_iIdx].ToString();
      }

      // branching on different logging level
      switch (a_eLogLevel)
      {
        case LoggingMgr.LogLevel.LOGLEVEL_ERROR_LOGGING_LEVEL:
          {
            _szLoggingMessage = "<color=red><b>[LOG][ERROR][" + Time.time + "]: </b></color>" + _szLoggingMessage;
            Debug.LogError(_szLoggingMessage);
          }
          break;

        case LoggingMgr.LogLevel.LOGLEVEL_WARNING_LOGGING_LEVEL:
          {
            _szLoggingMessage = "<color=yellow><b>[LOG][WARN][" + Time.time + "]: </b></color>" + _szLoggingMessage;
            Debug.LogWarning(_szLoggingMessage);
          }
          break;

        case LoggingMgr.LogLevel.LOGLEVEL_INFO_LOGGING_LEVEL:
          {
            _szLoggingMessage = "<color=#FF8C00><b>[LOG][INFO][" + Time.time + "]: </b></color>" + _szLoggingMessage;
            Debug.Log(_szLoggingMessage);
          }
          break;

        case LoggingMgr.LogLevel.LOGLEVEL_DEBUG_LOGGING_LEVEL:
          {
            _szLoggingMessage = "<color=blue><b>[LOG][DEBUG][" + Time.time + "]: </b></color>" + _szLoggingMessage;
            Debug.Log(_szLoggingMessage);
          }
          break;

        default:
          {

          }
          break;
      }
    }
    #endregion // PUBLIC METHODS
  };

} // namespace Framework.Logger

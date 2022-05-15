/**
 * File Based Logger: dump all logging messages into log.txt file by default
 */

using UnityEngine;

namespace Framework.Logger
{
  public static class FileLogger
  {
    #region PUBLIC METHODS
    // set the log file name
    public static void setLogFileName(string a_szLogFileName)
    {
      if(null == a_szLogFileName          // if null given
         ||                               // or
         0 == a_szLogFileName.Length      // log file name is empty
        )
      {
        return;
      }

      // if the old log file is using: close it
      if(null != ms_pDataStream)
      {
        ms_pDataStream.Close();
      }

      // store the given log file name
      ms_szCurLogFileName = a_szLogFileName;

      // open the given file name
      openLogFile(a_szLogFileName);
    }

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
          _szLoggingMessage = "[LOG][ERROR][" + Time.time + "]: " + _szLoggingMessage;
        }
        break;

        case LoggingMgr.LogLevel.LOGLEVEL_WARNING_LOGGING_LEVEL:
        {
          _szLoggingMessage = "[LOG][WARN][" + Time.time + "]: " + _szLoggingMessage;
        }
        break;

        case LoggingMgr.LogLevel.LOGLEVEL_INFO_LOGGING_LEVEL:
        {
          _szLoggingMessage = "[LOG][INFO][" + Time.time + "]: " + _szLoggingMessage;
        }
        break;

        case LoggingMgr.LogLevel.LOGLEVEL_DEBUG_LOGGING_LEVEL:
        {
          _szLoggingMessage = "[LOG][DEBUG][" + Time.time + "]: " + _szLoggingMessage;
        }
        break;

        default:
        {

        }
        break;
      }

      // stream out logging message
      if(null != ms_pDataStream)
      {
        // adding return
        _szLoggingMessage += "\n";

        ms_pDataStream.Write(ms_pUTF8Encoder.GetBytes(_szLoggingMessage), 0, ms_pUTF8Encoder.GetByteCount(_szLoggingMessage));
        ms_pDataStream.Flush();
      }
    }

    // callback for destroy event
    public static void onDestroyEvent()
    {
      // close file stream
      if(null != ms_pDataStream)
      {
        ms_pDataStream.Close();

        ms_pDataStream = null;
      }

      // release UTF8 encoder
      ms_pUTF8Encoder = null;
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    static FileLogger()
    {
      // set default log file name
      ms_szCurLogFileName = "log.txt";

      // reset current file data stream
      ms_pDataStream = null;

      // open file data stream
      openLogFile(ms_szCurLogFileName);

      // create UTF8 encoder
      ms_pUTF8Encoder = new System.Text.UTF8Encoding(true);
    }

    /**
     * open the given log file
     * return true on success
     */
    static bool openLogFile(string a_szLogFileName)
    {
      string _szLogFilePath = Application.persistentDataPath + "/" + a_szLogFileName;

      try
      {
        // the given log file does exist
        if(System.IO.File.Exists(_szLogFilePath) == true)
        {
          // delete the old log file
          System.IO.File.Delete(_szLogFilePath);
        }

        // open/create for write and support share reading
        ms_pDataStream = new System.IO.FileStream(_szLogFilePath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Read);
      }
      catch(System.Exception)
      {
        return false;
      }

      return true;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // current log file name
    static string ms_szCurLogFileName;
    // data stream
    static System.IO.FileStream ms_pDataStream;
    // utf8 encoder
    static System.Text.UTF8Encoding ms_pUTF8Encoder;
    #endregion // PRIVATE DATA MEMBERS
  };

} // namespace Framework.Logger

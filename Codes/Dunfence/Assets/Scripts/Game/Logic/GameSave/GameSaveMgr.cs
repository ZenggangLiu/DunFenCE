/**
 * Game Save Manager Class
 * - during initialization, it loads gamesave.dat and initialize the GameSave
 * - provides access to initialized game save via CurrentGameSave
 * - provides dumping out game save via dumpGameSave(): write current game save into file
 */

using UnityEngine;
using Framework.Utils;
using Framework.Logger;

namespace Game.Logic
{
  public class GameSaveMgr : TManager < GameSaveMgr >
  {
    #region PUBLIC METHODS
    /**
     * return current game save data
     * use this to change game save's value: RP, Coins and BoughtWeaponList
     */
    public GameSave CurrentGameSave
    {
      get
      {
        return m_pCurGameSave;
      }
    }

    /**
     * dump out the game save
     * return true on success
     */
    public bool dumpGameSave()
    {
      // create write stream each time, when dump
      string _szGameSaveFilePath = Application.persistentDataPath + "/" + GAMESAVE_DATAFILE_NAME;

      // try to create the game save file
      System.IO.FileStream _pGameSaveDataStream = null;
      try
      {
        // create the game save file
        _pGameSaveDataStream = new System.IO.FileStream(_szGameSaveFilePath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Read);
      }
      // can NOT create the game save
      catch(System.Exception e)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameSaveMgr][dumpGameSave] Can NOT create game save file: " + e.ToString());

        return false;
      }

      // utf8 encoder
      System.Text.UTF8Encoding _pUTF8Encoder = new System.Text.UTF8Encoding(true);

      // try to write game save
      try
      {
        string _szGameSaveDataString = m_pCurGameSave.getData();

        _pGameSaveDataStream.Write(_pUTF8Encoder.GetBytes(_szGameSaveDataString), 0, _pUTF8Encoder.GetByteCount(_szGameSaveDataString));
        _pGameSaveDataStream.Flush();
        _pGameSaveDataStream.Close();

        return true;
      }
      catch(System.Exception e)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameSaveMgr][dumpGameSave] Can NOT dump game save file: " + e.ToString());

        return false;
      }
    }

    #region PUBLIC OVERRIDDENS
    // the function called before using this manager
    // set m_bIsInitialized to true, on successful initialization
    public override void doInitialize()
    {
      // create game save
      m_pCurGameSave = new GameSave();

      // load current game save
      m_bIsInitialized = loadGameSave();
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
      m_bIsInitialized = false;
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // load up current game save or create a default one
    // return true on success
    bool loadGameSave()
    {
      // the path for game save file
      string _szGameSaveFilePath = Application.persistentDataPath + "/" + GAMESAVE_DATAFILE_NAME;

      // game save stream
      System.IO.FileStream _pGameSaveDataStream = null;

      // check if there is a game save file
      // NO game save
      if(System.IO.File.Exists(_szGameSaveFilePath) == false)
      {
        // create the default game save
        if(createDefaultGameSave() == true)
        {
          // dump the default game save
          return dumpGameSave();
        }

        // can NOT create a default game save
        else
        {
          return false;
        }
      }

      // there is a game save
      else
      {
        // try to read the game save
        try
        {
          // open the game save file
          _pGameSaveDataStream = new System.IO.FileStream(_szGameSaveFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
        }
        // can NOT open the game save
        catch(System.Exception e)
        {
          DefaultGameLogger.Instance.dumpLogError("[GameSaveMgr][loadGameSave] Can NOT open game save file: " + e.ToString());

          return false;
        }

        // read in the game save file
        // create the byte array
        byte[] _aGameSaveFileContent = new byte[_pGameSaveDataStream.Length];

        // read
        int _iReadBytes = 0;
        try
        {
          _iReadBytes = _pGameSaveDataStream.Read(_aGameSaveFileContent, 0, _aGameSaveFileContent.Length);
        }
        // can NOT read the game save
        catch(System.Exception e)
        {
          // create the default game save
          createDefaultGameSave();

          // close the stream
          _pGameSaveDataStream.Close();
          _pGameSaveDataStream = null;

          DefaultGameLogger.Instance.dumpLogError("[GameSaveMgr][loadGameSave] Can NOT read game save file: " + e.ToString());

          return false;
        }

        if(_iReadBytes != _pGameSaveDataStream.Length)
        {
          createDefaultGameSave();

          // close the stream
          _pGameSaveDataStream.Close();
          _pGameSaveDataStream = null;

          DefaultGameLogger.Instance.dumpLogError("[GameSaveMgr][loadGameSave] Can NOT read whole game save file");

          return false;
        }

        // done with reading, close the stream
        _pGameSaveDataStream.Close();
        _pGameSaveDataStream = null;

        // parse the game save file
        if(parseGameSave(_aGameSaveFileContent) == false)
        {
          DefaultGameLogger.Instance.dumpLogError("[GameSaveMgr][loadGameSave] Can NOT parse game save file");

          return false;
        }

        // done with parsing
        return true;
      }
    }

    // create the default game save file, if it does NOT exist
    // return true on success
    bool createDefaultGameSave()
    {
      /**
       * default game save data
       * {
       *   "IsPAU": false,
       *   "Economy": {
       *     "RP": 0,
       *     "Coin": 0
       *   },
       *  "BoughtWeaponList": [
       *     4,
       *     14,
       *     24,
       *     34,
       *     44, 
       *     54, 
       *     64,
       *     74
       *   ]
       * }
       */
      // try to load in InitialGameSave.json file
      TextAsset _pGameSaveDataJsonFile = Resources.Load("InitialGameSave") as TextAsset; // an TextAsset: the representation of a Textural or Binary File
      if(null == _pGameSaveDataJsonFile)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameSaveMgr][createDefaultGameSave] Can NOT find InitialGameSave.json file");

        return false;
      }

      return parseGameSave(_pGameSaveDataJsonFile.text);
    }

    // parse the game save from the given buffer in length a_iBufferLength
    // return true on success
    bool parseGameSave(byte[] a_aGameSaveDataBuffer)
    {
      // empty buffer
      if(0 == a_aGameSaveDataBuffer.Length)
      {
        return false;
      }

      // convert buffer into string
      // utf8 encoder
      System.Text.UTF8Encoding _pUTF8Encoder = new System.Text.UTF8Encoding(true);

      string _szGameSaveString = _pUTF8Encoder.GetString(a_aGameSaveDataBuffer);

      return parseGameSave(_szGameSaveString);
    }

    // parse game save from the given string
    // return true on success
    bool parseGameSave(string a_szGameSaveDataString)
    {
      // reset the game save
      m_pCurGameSave.reset();

      // create and parse the json data at the same time
      JSONObject _pGameSaveDataJson = new JSONObject(a_szGameSaveDataString);

      // read in PAU flag
      m_pCurGameSave.IsPAU = false;

      if(true == _pGameSaveDataJson.HasField("IsPAU")     // has PAU flag
         &&                                               // and
         true == _pGameSaveDataJson["IsPAU"].IsBool       // boolean value
        )
      {
        m_pCurGameSave.IsPAU = _pGameSaveDataJson["IsPAU"].b;
      }

      // read in Economy
      if(false == _pGameSaveDataJson.HasField("Economy"))
      {
        DefaultGameLogger.Instance.dumpLogError("[GameSaveMgr][parseGameSave] Can NOT find Economy Block");

        return false;
      }

      // read in RP value
      // Economy does NOT have RP field
      if(false == _pGameSaveDataJson["Economy"].HasField("RP"))
      {
        DefaultGameLogger.Instance.dumpLogError("[GameSaveMgr][parseGameSave] Economy block does NOT have RP field");

        return false;
      }

      // RP field is NOT a number
      if(false == _pGameSaveDataJson["Economy"]["RP"].IsNumber)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameSaveMgr][parseGameSave] RP field in NOT a number");

        return false;
      }

      // store RP
      m_pCurGameSave.RP = (int)_pGameSaveDataJson["Economy"]["RP"].i;

      // read in Coins value
      // Economy does NOT have Coins field
      if(false == _pGameSaveDataJson["Economy"].HasField("Coins"))
      {
        DefaultGameLogger.Instance.dumpLogError("[GameSaveMgr][parseGameSave] Economy block does NOT have Coins field");

        return false;

      }

      // Coins field is NOT a number
      if(false == _pGameSaveDataJson["Economy"]["Coins"].IsNumber)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameSaveMgr][parseGameSave] Coins field in NOT a number");

        return false;
      }

      // store Coins
      m_pCurGameSave.Coins = (int)_pGameSaveDataJson["Economy"]["Coins"].i;

      // read in bought weapon list
      if(false == _pGameSaveDataJson.HasField("BoughtWeaponList"))
      {
        DefaultGameLogger.Instance.dumpLogError("[GameSaveMgr][parseGameSave] NO BoughtWeaponList Block");

        return false;
      }

      // BoughtWeaponList is NOT an array
      if(false == _pGameSaveDataJson["BoughtWeaponList"].IsArray)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameSaveMgr][parseGameSave] BoughtWeaponList block is NOT an array");

        return false;
      }

      // get the BoughtWeaponList json block
      JSONObject _pWeaponListJsonBlock = _pGameSaveDataJson["BoughtWeaponList"];

      // iterate over whole BoughtWeaponList
      for(int _iCurWeaponIdx = 0; _iCurWeaponIdx < _pWeaponListJsonBlock.Count; _iCurWeaponIdx++)
      {
        // get the current weapon UID json block
        JSONObject _pCurWeaponUIDJsonBlock = _pWeaponListJsonBlock[_iCurWeaponIdx];

        // UID is NOT a number
        if(false == _pCurWeaponUIDJsonBlock.IsNumber)
        {
          continue;
        }

        // insert the weapon UID
        m_pCurGameSave.BoughtWeaponList.Add((int)_pCurWeaponUIDJsonBlock.i);
      }

      return true;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // the name of game save file
    static string GAMESAVE_DATAFILE_NAME = "GameSave.dat";
    // current game save
    GameSave m_pCurGameSave;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Logic

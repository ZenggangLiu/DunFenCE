/**
 * Game Asset Manager
 * - provide game asset loading using a given asset ID: TODO: use async op
 */

using UnityEngine;
using Framework.Utils;
using Framework.Logger;
using System.Collections.Generic; // Dictionary<TKey, TVal>

namespace Game.Logic
{
  public class GameAssetMgr : TManager< GameAssetMgr >
  {
    #region PUBLIC METHODS
    /**
     * blocking load up the given asset using its ID
     * return loaded asset in a_pLoadedAsset, null on failure
     * NOTE: the loaded asset will belong to the currently loaded Scene
     * But we can NOT put it into unity's scene hierarchy, 
     * we have to clone this asset: Object.Instantiate()
     * return true on success
     */
    // NOTE: NOT use this function directly, 
    // USE the UNBLOCKING GameLevelMgr.Instance.CurrentGameLevel.getAsset() within Game Logic
    public bool loadAsset(int a_iID, out GameObject a_pLoadedAsset)
    {
      a_pLoadedAsset = null;

      // can NOT find the given asset: return
      if(m_mAssetMap.ContainsKey(a_iID) == false)
      {
        return false;
      }

      string _szAssetPreFabName = m_mAssetMap[a_iID];

      a_pLoadedAsset = Resources.Load(_szAssetPreFabName) as GameObject;

      return null != a_pLoadedAsset;
    }

    #region PUBLIC OVERRIDDENS
    // the function called before using this manager
    // load in AssetData.json file
    public override void doInitialize()
    {
      // create asset map
      m_mAssetMap = new Dictionary< int, string >(32);

      // try to load in AssetData.json file
      TextAsset _pAssetDataJsonFile = Resources.Load("AssetData") as TextAsset; // an TextAsset: the representation of a Textural or Binary File
      if(null == _pAssetDataJsonFile)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameAssetMgr][doInitialize]: Can NOT find AssetData.json file");

        return;
      }

      // create and parse the json data at the same time
      JSONObject _pAssetDataJson = new JSONObject(_pAssetDataJsonFile.text);

      // if AssetData json does NOT contain AssetList node
      if(false == _pAssetDataJson.HasField("AssetList"))
      {
        DefaultGameLogger.Instance.dumpLogError("[GameAssetMgr][doInitialize]: Can NOT find AssetList node in AssetData.json file");

        return;
      }

      // if AssetList node is NOT an array
      if(false == _pAssetDataJson["AssetList"].IsArray)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameAssetMgr][doInitialize]: AssetList node is NOT an array");

        return;
      }

      // get the AssetList json block
      JSONObject _pAssetListJsonBlock = _pAssetDataJson["AssetList"];

      // iterate over all elements of AssetList
      for(int _iCurAssetIdx = 0; _iCurAssetIdx < _pAssetListJsonBlock.Count; _iCurAssetIdx++)
      {
        // get the current asset json block
        JSONObject _pCurAssetJsonBlock = _pAssetListJsonBlock[_iCurAssetIdx];

        // if ID and Path fields both exist
        if(_pCurAssetJsonBlock.HasField("ID")
           &&
           _pCurAssetJsonBlock["ID"].IsNumber
           &&
           _pCurAssetJsonBlock.HasField("Path")
          )
        {
          // try to read in ID and Path
          int _iAssetID = -1;
          string _szAssetPath = "";
          _pCurAssetJsonBlock.GetField(ref _iAssetID, "ID");
          _pCurAssetJsonBlock.GetField(ref _szAssetPath, "Path");

          // if ID and Path both have values
          if(_iAssetID != -1 && false == _szAssetPath.Equals(""))
          {
            // insert them into asset map
            m_mAssetMap.Add(_iAssetID, _szAssetPath);
          }
        }
      }
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PRIVATE DATA MEMBERS
    Dictionary< int, string > m_mAssetMap;

    #endregion // PRIVATE DATA MEMBERS
  }
} // namespace Game.Logic

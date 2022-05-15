/**
 * Game Level Base Class: Attached to Root Node(Level) of the current Game Level
 * Calling Workflow: doWarmUP() --> doEnter() --> doExit() --> doCoolDown()
 * - doWarmUp: preload assets: 
 *   + called after the level has been loaded up within GameLevelMgr
 *   + spawn all serialized entity game objects
 * - doEnter: setup before entering a level
 * - doExit: all clear up work before unloading all assets
 * - doCoolDown: unload all assets: called before this level will be released within GameLevelMgr
 * 
 * NOTES:
 * 1. Each Game Level can have its own Camera(with NULL Target Texture): NOT Rendering To Texture 
 *    - its settings will be copied into NormScene' Camera, if the game is working in NON-AR mode
 * 2. Each Game Level can have Lights. But it will be disabled after loading this level
 * 
 */

using UnityEngine;
using Framework.Logger;
using System.Collections.Generic; // Dictionary< TKey, TVal >

namespace Game.Logic
{
  public class GameLevel : MonoBehaviour
  {
    #region PUBLIC METHODS
    /**
     * return level name
     */
    public string LevelName
    {
      get
      {
        return m_szLevelName;
      }
    }

    /**
     * return discount key
     */
    public string DiscountKey
    {
      get
      {
        return m_szDiscountKey;
      }
    }

    /**
     * return the game level GUI node
     */
    public GameObject GameLevelGUINode
    {
      get
      {
        return m_pGameLevelGUI.gameObject;
      }
    }

    /**
     * return the game level GUI
     */
    public Game.GUI.GameLevelGUI GameLevelGUI
    {
      get
      {
        return m_pGameLevelGUI;
      }
    }

    /**
     * get and set camera mode
     */
    public Game.Camera.CameraModes CameraMode
    {
      get
      {
        return m_eCurCameraMode;
      }

      set
      {
        m_eCurCameraMode = value;
      }
    }

    /**
     * return a DISABLED cloned game asset
     * return true on success
     */
    public bool getAsset(int a_iAssetID, out GameObject a_pClonedAsset)
    {
      // clear the returned cloned asset
      a_pClonedAsset = null;

      // if we can NOT find the given asset in cache
      if(m_mAssetCache.ContainsKey(a_iAssetID) == false)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevel][getAsset]: Try to get a NON-CACHED asset[" + a_iAssetID + "]. Using Blocking loadAsset(). Please add " + a_iAssetID + " into PreloadAssetList block within LevelData.json file");

        // try to load this asset directly
        GameObject _pRequiredAsset = null;

        // if we can NOT load in the required asset
        if(false == GameAssetMgr.Instance.loadAsset(a_iAssetID, out _pRequiredAsset)
           ||
           null == _pRequiredAsset
          )
        {
          DefaultGameLogger.Instance.dumpLogError("[GameLevel][getAsset]: Can NOT load the given asset[" + a_iAssetID + "]");

          return false;
        }

        // successful asset loading
        // insert into asset cache
        m_mAssetCache[a_iAssetID] = _pRequiredAsset;
      }

      // clone the required asset
      a_pClonedAsset = Object.Instantiate(m_mAssetCache[a_iAssetID]);

      // disable the cloned asset
      a_pClonedAsset.SetActive(false);

      return true;
    }

    #region PUBLIC OVERRIDDENS
    /**
     * called after this level has been loaded up within GameLevelMgr
     * - do some sort of initialization works
     * - use the given a_pLevelSwitchingData to preload any resources and set progress accordingly
     */
    public virtual void doWarmUp(LevelSwitchingData a_pLevelSwitchingData)
    {
      // clear game level GUI
      m_pGameLevelGUI = null;

      // clear entity list
      m_aEntityList = null;

      // store game level manager
      m_pGameLevelMgr = GameLevelMgr.Instance;

      // create game asset cache
      m_mAssetCache = new Dictionary< int, GameObject >(10);

      // store level name
      m_szLevelName = a_pLevelSwitchingData.CurrLevelConfigData.Name;

      // store discount key
      m_szDiscountKey = a_pLevelSwitchingData.CurrLevelConfigData.DiscountKey;

      // try to find all game objects which have an Entity component
      m_aEntityList = gameObject.GetComponentsInChildren < Entity >();

      // try to find GameLevelGUI node: a node with GameLevelGUI.cs derived class component
      // iterate over all subnodes
      for(int _iIdx = 0; _iIdx < transform.childCount; _iIdx++)
      {
        // if the current subnode has LevelGUI derived class
        if(transform.GetChild(_iIdx).GetComponentInChildren< Game.GUI.GameLevelGUI >() != null)
        {
          m_pGameLevelGUI = transform.GetChild(_iIdx).GetComponentInChildren< Game.GUI.GameLevelGUI >();

          break;
        }
      }

      // can NOT find GameLevelGUI
      if(null == m_pGameLevelGUI)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][GameLevel][doWarmUp]: Can NOT find LevelGUI Node. Please make sure one node has LevelGUI.cs derived class attached on");

        return;
      }

      // quick check mode: try to find the Game Main Entry: GameMain game object
      if(null == GameObject.Find("GameMain"))
      {
#if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("[" + gameObject.ToString() + "][GameLevel][doWarmUp]: Running in Level Checking Mode!");
#endif

        return;
      }

      // clear up the asset cache
      m_mAssetCache.Clear();

      // if there is NO preload asset
      if(0 == a_pLevelSwitchingData.CurrLevelConfigData.PreloadAssetList.Count)
      {
        if(a_pLevelSwitchingData.CurLevelSwitchingHandler != null)
        {
          a_pLevelSwitchingData.CurLevelSwitchingHandler(LevelSwitchingStates.LEVEL_SWITCHING_STATES_PROCESSING, 60.0f);
        }
      }
      else
      {
        // preload all assets: 20% --> 60%
        // load in all assets from PreloadAssetList
        int _iAssetCount         = a_pLevelSwitchingData.CurrLevelConfigData.PreloadAssetList.Count;
        float _fProgressIncrease = 40.0f / _iAssetCount;
        float _fCurrentProgress  = 20.0f;

        // before loading set loading progress: 20%
        if(a_pLevelSwitchingData.CurLevelSwitchingHandler != null)
        {
          a_pLevelSwitchingData.CurLevelSwitchingHandler(LevelSwitchingStates.LEVEL_SWITCHING_STATES_PROCESSING, _fCurrentProgress);
        }

        // loaded asset
        GameObject _pCurAsset = null;

        // iterate over the whole asset list
        for(int _iCurAssetIdx = 0; _iCurAssetIdx < _iAssetCount; _iCurAssetIdx++)
        {
          // store current asset id
          int _iCurAssetID = a_pLevelSwitchingData.CurrLevelConfigData.PreloadAssetList[_iCurAssetIdx];

          // can NOT load this asset: go to next asset
          if(false == GameAssetMgr.Instance.loadAsset(_iCurAssetID, out _pCurAsset))
          {
            DefaultGameLogger.Instance.dumpLogError("[GameLevel][doWarmUp]: Can NOT load the given asset[" + _iCurAssetID + "]");
          }
          else
          {
            m_mAssetCache[_iCurAssetID] = _pCurAsset;
          }

          // update progress value
          _fCurrentProgress += _fProgressIncrease;

          // update loading progress
          if(a_pLevelSwitchingData.CurLevelSwitchingHandler != null)
          {
            a_pLevelSwitchingData.CurLevelSwitchingHandler(LevelSwitchingStates.LEVEL_SWITCHING_STATES_PROCESSING, _fCurrentProgress);
          }
        }
      }

      // preload all effects
      // iterate over the whole asset list
      for(int _iCurFxIdx = 0; _iCurFxIdx < a_pLevelSwitchingData.CurrLevelConfigData.PreloadEffectList.Count; _iCurFxIdx++)
      {
        EffectMgr.Instance.preloadWorldEffect(a_pLevelSwitchingData.CurrLevelConfigData.PreloadEffectList[_iCurFxIdx]);
      }

      // initial the GameLevelGUI at last, because it needs some game assets: 80%
      m_pGameLevelGUI.doInitialize();

      // update loading progress to 70%
      if(a_pLevelSwitchingData.CurLevelSwitchingHandler != null)
      {
        a_pLevelSwitchingData.CurLevelSwitchingHandler(LevelSwitchingStates.LEVEL_SWITCHING_STATES_PROCESSING, 70.0f);
      }

      if(m_aEntityList != null)
      {
        // spawn all serialized Entity game objects
        for(int _iEntityIdx = 0; _iEntityIdx < m_aEntityList.Length; _iEntityIdx++)
        {
          Entity _pCurEntity = m_aEntityList[_iEntityIdx];

          // spawn the current entity at its current location with current orientation
          _pCurEntity.doSpawnCom();
        }
      }

      // enable this level
      gameObject.SetActive(true);

      // update loading progress to 80%
      if(a_pLevelSwitchingData.CurLevelSwitchingHandler != null)
      {
        a_pLevelSwitchingData.CurLevelSwitchingHandler(LevelSwitchingStates.LEVEL_SWITCHING_STATES_PROCESSING, 80.0f);
      }
    }

    /**
     * called after calling doWarmUp()
     * called after clicking start button
     */
    public virtual void doEnter()
    {

    }

    /**
     * update function
     */
    public virtual void doUpdate()
    {
      // update current camera controller
      m_pGameLevelMgr.GameMainLevel.CameraController.doUpdate();
    }

    /**
     * post update function
     */
    public virtual void doPostUpdate()
    {
      
    }

    /**
     * called before calling doCoolDown()
     * 
     */
    public virtual void doExit()
    {
      // dump out game save
      GameSaveMgr.Instance.dumpGameSave();
    }

    /**
     * called before this level will be released within GameLevelMgr
     * - do some sort of clean up works
     */
    public virtual void doCoolDown()
    {
      // destory GUI's internal members
      m_pGameLevelGUI.doDestroy();

      // clear entity list
      for(int _iEIdx = 0; _iEIdx < m_aEntityList.Length; _iEIdx++)
      {
        m_aEntityList[_iEIdx].doDeadCom();
      }
      m_aEntityList = null;

      // clear asset cache
      m_mAssetCache.Clear();
      m_mAssetCache = null;

      // set default camera mode
      m_eCurCameraMode = Game.Camera.CameraModes.CAMERA_MODES_REGULAR;

      // reset all managers
      Game.Event.GameEventMgr.Instance.doReset();
      EffectMgr.Instance.doReset();
      GameSaveMgr.Instance.doReset();

      // disable this level
      gameObject.SetActive(false);
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED DATA MEMBERS
    // level name
    protected string m_szLevelName;
    // discount key
    protected string m_szDiscountKey;
    // game level manager
    protected GameLevelMgr m_pGameLevelMgr;
    // game level GUI
    protected Game.GUI.GameLevelGUI m_pGameLevelGUI;
    // Entity game object list
    protected Entity[] m_aEntityList;
    // asset cache
    protected Dictionary< int, GameObject > m_mAssetCache;
    // current camera mode: modified within switchToLevel()
    protected Game.Camera.CameraModes m_eCurCameraMode;
    #endregion // PROTECTED DATA MEMBERS
  }

} // namespace Game.Logic

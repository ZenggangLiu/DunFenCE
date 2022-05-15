/**
 * Base Class of GUI used during any game level
 * - GameLevelGUI must be attached to the Root Cavas of the GUI node
 * - CharacterHPBars: used to attach all character's HP bars(Heart Bars)
 * - BuildingHPBars:  used to attach all building's HP bars
 * - TowerTimerBars:  used to attach all defense tower's timer bars
 * - ModalDialogPanel: used to attach a modal dialog GUI
 */

using UnityEngine;
using UnityEngine.UI;
using Framework.Logger;
using Framework.Utils;
using Game.Logic;
using System.Collections.Generic; // Dictionary< Key, Value>

namespace Game.GUI
{
  // response type of dialog or message box
  public enum DialogResponseTypes
  {
    // Yes button has been clicked
    DIALOG_RESPONSE_TYPES_YES,
    // NO button has been clicked
    DIALOG_RESPONSE_TYPES_NO,
  }

  // callback for response of a dialog/messagebox
  public delegate void DialogResponseHandler(DialogResponseTypes a_eResponseType, System.Object a_pUserData);

  public class GameLevelGUI : MonoBehaviour
  {
    #region PUBLIC METHODS
    /**
     * return Root GUI Canvas
     */
    public Canvas RootCanvas
    {
      get
      {
        return m_pCanvas;
      }
    }

    /**
     * return stats text field
     */
    public Text Stats
    {
      get
      {
        return m_pStats;
      }
    }

    /**
     * return the sprite for the given name
     * return true on success
     */
    public bool getSprite(string a_szSpriteName, out Sprite a_pFoundSprite)
    {
      a_pFoundSprite = null;

      if(false == m_mSpriteMap.ContainsKey(a_szSpriteName))
      {
        return false;
      }

      a_pFoundSprite = m_mSpriteMap[a_szSpriteName];

      return true;
    }

    /**
     * popup a singleton message box
     */
    public void showMessageBox(MessageBoxTypes a_eType, string a_szDesc, DialogResponseHandler a_pResponseHandler, System.Object a_pUserData)
    {
      // disable all nodes under ModalDialogPanel
      for(int _iNIdx = 0; _iNIdx < m_pModalDialogPanel.transform.childCount; _iNIdx++)
      {
        m_pModalDialogPanel.transform.GetChild(_iNIdx).gameObject.SetActive(false);
      }

      // enable ModalDialogPanel
      m_pModalDialogPanel.SetActive(true);

      // pop up message box
      m_pCommonMessageBox.show(a_eType, a_szDesc, a_pResponseHandler, a_pUserData);
    }

    /**
     * hide the singleton message box
     */
    public void hideMessageBox()
    {
      // hide the message box
      m_pCommonMessageBox.hide();

      // disable ModelDialogPanel
      m_pModalDialogPanel.SetActive(false);
    }

    #region PUBLIC OVERRIDDENS
    /**
     * Reset() is called when the user hits the Reset button in the Inspector's context menu
     * or when adding the component the first time 
     * This function is only called in editor mode 
     * Reset is most commonly used to give good default values in the inspector
     */
    public virtual void Reset()
    {

    }

    /**
     * Initialize function
     * NOTE:m_bIsInitialized MUST be SET in doInitialize() method of derived class
     */
    public virtual void doInitialize()
    {
      // try to find canvas
      m_pCanvas = GetComponentInChildren< Canvas >();
      if(null == m_pCanvas)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelGUI][doInitialize]: Can NOT find Canvas Component. Attach LevelGUI.cs onto a UI Canvas");

        return;
      }

      // try to load in all information related sprites
      // create sprite map
      m_mSpriteMap = new Dictionary< string, Sprite >(10);

      // load all sprites
      loadAllSprites("Textures/GUIs/GUIMapA");

      // collect all sorts of subnodes
      GameObject _pGUINode = null;

      // try to find CharacterHPBars node
      m_pCharacterHPBars = GameObjectUtils.FindNode(gameObject, "CharacterHPBars");
      if(null == m_pCharacterHPBars)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelGUI][doInitialize]: Can NOT find CharacterHPBars Node");

        return;
      }

      // try to find BuildingHPBars node
      m_pBuildingHPBars = GameObjectUtils.FindNode(gameObject, "BuildingHPBars");
      if(null == m_pBuildingHPBars)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelGUI][doInitialize]: Can NOT find BuildingHPBars Node");

        return;
      }

      // try to find TowerTimerBars node
      m_pTowerTimerBars = GameObjectUtils.FindNode(gameObject, "TowerTimerBars");
      if(null == m_pTowerTimerBars)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelGUI][doInitialize]: Can NOT find TowerTimerBars Node");

        return;
      }

      // try to find Stats node
      m_pStats = null;
      _pGUINode = GameObjectUtils.FindNode(gameObject, "Stats");
      if(_pGUINode != null)
      {
        m_pStats = _pGUINode.GetComponentInChildren< Text >();
        if(null == m_pStats)
        {
          DefaultGameLogger.Instance.dumpLogError("[GameLevelGUI][doInitialize]: Stats does NOT have a Text component");

          return;
        }
      }

      // try to find ModalDialogPanel
      m_pModalDialogPanel = GameObjectUtils.FindNode(gameObject, "ModalDialogPanel");
      if(null == m_pModalDialogPanel)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelGUI][doInitialize]: Can NOT find ModalDialogPanel Node");

        return;
      }

      // make ModalDialogPanel is the lastest child in its parent's children chain
      if(m_pModalDialogPanel.transform.GetSiblingIndex() != m_pModalDialogPanel.transform.parent.childCount - 1)
      {
        DefaultGameLogger.Instance.dumpLogWarning("[GameLevelGUI][doInitialize]: ModalDialogPanel Node is NOT the TOPPEST node in GameLevelGUI. Changed with Code");

        m_pModalDialogPanel.transform.SetAsLastSibling();
      }

      // disable the ModelDialogPanel by default
      m_pModalDialogPanel.SetActive(false);

      // try to find MessageBox
      _pGUINode = GameObjectUtils.FindNode(m_pModalDialogPanel, "MessageBox");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelGUI][doInitialize]: Can NOT find MessageBox node within ModalDialogPanel Node");

        return;
      }

      // disalbe the MessageBox
      _pGUINode.SetActive(false);

      // try to get MessageBox component
      m_pCommonMessageBox = _pGUINode.GetComponent< MessageBox >();
      if(null == m_pCommonMessageBox)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameLevelGUI][doInitialize]: MessageBox node does NOT have MessageBox.cs attached");

        return;
      }

      // initialize the message box
      m_pCommonMessageBox.doInitialize();

      // enable the GUI
      gameObject.SetActive(true);
    }

    /**
     * Update function: called each frame
     * Update() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last Update() call
     * check true == m_bIsInitialized before doing the real thing
     */
    public virtual void Update()
    {

    }

    /**
     * Physical Update function: 50Hz or 0.02 second
     * FixedUpdate() will be called at fixed frame-rate, if the script is ENABLED
     * FixedUpdate() will be called at 50Hz: each 0.02 second
     * use Time.deltaTime to get the delta time instead of Time.fixedDeltaTime, 
     * because Time.deltaTime automatically returns the right delta time if you are inside a FixedUpdate() or Update()
     */
    public virtual void FixedUpdate()
    {

    }

    /**
     * Post-Update function: called each frame after all Update() functions
     * LateUpdate() will be called after ALL Update() functions called
     */
    public virtual void LateUpdate()
    {

    }

    /*
     * the function called before destroying this GUI
     * NOTE:m_bIsInitialized MUST be CLEARED in doDestroy() method in derived class
     */
    public virtual void doDestroy()
    {
      // disable the GUI
      gameObject.SetActive(false);

      // collect all sprite names
      List<string> _aSpriteNameList = new List<string>(50);

      // NOTE: foreach is a CONSTER ACCESSER
      foreach(string _szSpriteName in m_mSpriteMap.Keys)
      {
        _aSpriteNameList.Add(_szSpriteName);
      }

      // unload all sprites
      for(int _iSIdx = 0; _iSIdx < _aSpriteNameList.Count; _iSIdx++)
      {
        m_mSpriteMap[_aSpriteNameList[_iSIdx]] = null;
      }

      // clear the sprite map
      m_mSpriteMap.Clear();
      m_mSpriteMap = null;
    }

    // attach the given character's HPBar UI: attach onto CharacterHPBars node
    public virtual void attachCharacterHP(GameObject a_pHPBarUI)
    {
      // attach the given UI
      a_pHPBarUI.transform.SetParent(m_pCharacterHPBars.transform);
    }

    // remove the given character's HPBar UI: remove out of CharacterHPBars node
    public virtual void removeCharacterHP(GameObject a_pHPBarUI)
    {
      // remove the given UI
      a_pHPBarUI.transform.SetParent(null);
    }

    // attach the given building's HPBar UI: attach onto BuildingHPBars node
    public virtual void attachBuildingHP(GameObject a_pHPBarUI)
    {
      // attach the given UI
      a_pHPBarUI.transform.SetParent(m_pBuildingHPBars.transform);
    }

    // remove the given building's HPBar UI: remove out of BuildingHPBars node
    public virtual void removeBuildingHP(GameObject a_pHPBarUI)
    {
      // remove the given UI
      a_pHPBarUI.transform.SetParent(null);
    }

    // attach the given tower's TimerBar UI: attach onto TowerTimerBars node
    public virtual void attachTowerTimerBar(GameObject a_pTimerBarUI)
    {
      // attach the given UI
      a_pTimerBarUI.transform.SetParent(m_pTowerTimerBars.transform);
    }

    // remove the given tower's TimerBar UI: remove out of TowerTimerBars node
    public virtual void removeTowerTimerBar(GameObject a_pTimerBarUI)
    {
      // remove the given UI
      a_pTimerBarUI.transform.SetParent(null);
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED DATA MEMBERS
    // canvas: used to get scaling factor: Canvas.scaleFactor
    protected Canvas m_pCanvas;
    // sprite map
    protected Dictionary< string, Sprite > m_mSpriteMap;  // < SpriteName, SpriteInstance >
    // CharacterHPBars node
    protected GameObject m_pCharacterHPBars;
    // BuildingHPBars node
    protected GameObject m_pBuildingHPBars;
    // TowerTimerBars node
    protected GameObject m_pTowerTimerBars;
    // stats text field
    protected Text m_pStats;
    // modal dialog panel
    protected GameObject m_pModalDialogPanel;
    // initialization flag
    protected bool m_bIsInitialized;
    #endregion // PROTECTED DATA MEMBERS

    #region PRIVATE METHODS
    // load in all sprites stored in the game file
    void loadAllSprites(string a_szSpriteFileName)
    {
      // load the texture from asset file
      Sprite[] _aInfoSpriteList = Resources.LoadAll< Sprite >(a_szSpriteFileName);

      // if there are some sprites
      if(_aInfoSpriteList != null
         &&
         _aInfoSpriteList.Length != 0
        )
      {
        // insert all sprites into map
        for(int _iIdx = 0; _iIdx < _aInfoSpriteList.Length; _iIdx++)
        {
          if(false == m_mSpriteMap.ContainsKey(_aInfoSpriteList[_iIdx].name))
          {
            m_mSpriteMap.Add(_aInfoSpriteList[_iIdx].name, _aInfoSpriteList[_iIdx]);
          }
          else
          {
            m_mSpriteMap[_aInfoSpriteList[_iIdx].name] = _aInfoSpriteList[_iIdx];
          }
        }
      }
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // common message box dialog
    MessageBox m_pCommonMessageBox;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.GUI

/**
 * Common Usage Message Box
 */

using UnityEngine;
using UnityEngine.UI;
using Framework.Utils;
using Framework.Logger;
using Game.Logic;

namespace Game.GUI
{
  // types of message box
  public enum MessageBoxTypes
  {
    // message box has single button: by default, message box has double button
    MESSAGEBOX_TYPES_SINGLEBTN = 1 << 8,

    // double button message box
    // message box showing error message
    MESSAGEBOX_TYPES_ERROR = 0,
    // message box showing warning message
    MESSAGEBOX_TYPES_WARNING = 1,
    // message box showing informative message
    MESSAGEBOX_TYPES_INFO = 2,

    // single button message box
    MESSAGEBOX_TYPES_ERROR_SINGLEBTN = MESSAGEBOX_TYPES_ERROR | MESSAGEBOX_TYPES_SINGLEBTN,
    MESSAGEBOX_TYPES_WARNING_SINGLEBTN = MESSAGEBOX_TYPES_WARNING | MESSAGEBOX_TYPES_SINGLEBTN,
    MESSAGEBOX_TYPES_INFO_SINGLEBTN = MESSAGEBOX_TYPES_INFO | MESSAGEBOX_TYPES_SINGLEBTN,
  }

  public class MessageBox : MonoBehaviour
  {
    #region PUBLIC METHODS
    /**
     * show this message box
     */
    public void show(MessageBoxTypes a_eType, string a_szDesc, DialogResponseHandler a_pYesBtnCallback, System.Object a_pUserData)
    {
      // store handler
      m_pBtnClickHandler = a_pYesBtnCallback;

      // store user data
      m_pBtnClickHandlerData = a_pUserData;

      // set desc
      m_pDesc.text = a_szDesc;

      // based on message box type, set icon and button
      // single button message box
      if((int)(a_eType & MessageBoxTypes.MESSAGEBOX_TYPES_SINGLEBTN) != 0)
      {
        // disable double button panel
        m_pDoubleBtnPanel.SetActive(false);

        // enable single button panel
        m_pSingleBtnPanel.SetActive(true);

        // enable conform button
        m_pConfirmBtn.gameObject.SetActive(true);
      }

      // double button message box
      else
      {
        // disable single button panel
        m_pSingleBtnPanel.SetActive(false);

        // enable double button panel
        m_pDoubleBtnPanel.SetActive(true);

        // enable yes button
        m_pYesBtn.gameObject.SetActive(true);

        // enable no button
        m_pNoBtn.gameObject.SetActive(true);
      }

      // set icon
      m_pErrorIcon.SetActive(false);
      m_pWarningIcon.SetActive(false);
      m_pInfoIncon.SetActive(false);

      switch(a_eType & (~MessageBoxTypes.MESSAGEBOX_TYPES_SINGLEBTN))
      {
        case MessageBoxTypes.MESSAGEBOX_TYPES_ERROR:
        {
          // error message box
          m_pErrorIcon.SetActive(true);

          break;
        }

        case MessageBoxTypes.MESSAGEBOX_TYPES_WARNING:
        {
          // warning message box
          m_pWarningIcon.SetActive(true);

          break;
        }

        case MessageBoxTypes.MESSAGEBOX_TYPES_INFO:
        {
          // info message box
          m_pInfoIncon.SetActive(true);

          break;
        }

        default:
        {
          break;
        }
      }

      // enable this GUI
      gameObject.SetActive(true);
    }

    /**
     * hide this message box
     */
    public void hide()
    {
      // disable this GUI
      gameObject.SetActive(false);
    }

    /**
     * initialize this message box
     */
    public void doInitialize()
    {
      // try to find ErrorIcon node
      m_pErrorIcon = GameObjectUtils.FindNode(gameObject, "ErrorIcon");
      if(null == m_pErrorIcon)
      {
        DefaultGameLogger.Instance.dumpLogError("[MessageBox][doInitialize]: Can NOT find ErrorIcon Node");

        return;
      }

      // try to find WarningIcon node
      m_pWarningIcon = GameObjectUtils.FindNode(gameObject, "WarningIcon");
      if(null == m_pWarningIcon)
      {
        DefaultGameLogger.Instance.dumpLogError("[MessageBox][doInitialize]: Can NOT find WarningIcon Node");

        return;
      }

      // try to find InfoIcon node
      m_pInfoIncon = GameObjectUtils.FindNode(gameObject, "InfoIcon");
      if(null == m_pInfoIncon)
      {
        DefaultGameLogger.Instance.dumpLogError("[MessageBox][doInitialize]: Can NOT find InfoIcon Node");

        return;
      }

      // try to find Desc node
      GameObject _pGUINode = GameObjectUtils.FindNode(gameObject, "Desc");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[MessageBox][doInitialize]: Can NOT find Desc Node");

        return;
      }

      // try to get text component from Desc node
      m_pDesc = _pGUINode.GetComponent< Text >();
      if(null == m_pDesc)
      {
        DefaultGameLogger.Instance.dumpLogError("[MessageBox][doInitialize]: Desc Node does NOT have Text component");

        return;
      }

      // try to find DoubleBtnPanel
      m_pDoubleBtnPanel = GameObjectUtils.FindNode(gameObject, "DoubleBtnPanel");
      if(null == m_pDoubleBtnPanel)
      {
        DefaultGameLogger.Instance.dumpLogError("[MessageBox][doInitialize]: Can NOT find DoubleBtnPanel Node");

        return;
      }

      // try to find YesBtn node within DoubleBtnPanel
      _pGUINode = GameObjectUtils.FindNode(m_pDoubleBtnPanel, "YesBtn");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[MessageBox][doInitialize]: Can NOT find YesBtn Node within DoubleBtnPanel node");

        return;
      }

      // try to find button component within YesBtn node
      m_pYesBtn = _pGUINode.GetComponent< Button >();
      if(null == m_pYesBtn)
      {
        DefaultGameLogger.Instance.dumpLogError("[MessageBox][doInitialize]: YesBtn node does NOT have Button component");

        return;
      }

      // try to find NoBtn node within DoubleBtnPanel
      _pGUINode = GameObjectUtils.FindNode(m_pDoubleBtnPanel, "NoBtn");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[MessageBox][doInitialize]: Can NOT find NoBtn Node within DoubleBtnPanel node");

        return;
      }

      // try to find button component within NoBtn node
      m_pNoBtn = _pGUINode.GetComponent< Button >();
      if(null == m_pNoBtn)
      {
        DefaultGameLogger.Instance.dumpLogError("[MessageBox][doInitialize]: NoBtn node does NOT have Button component");

        return;
      }

      // register click handler for Yes button
      m_pYesBtn.onClick.AddListener(onYesClicked); // Listener's prototype: public delegate void UnityAction();

      // register click handler for No button
      m_pNoBtn.onClick.AddListener(onNoClicked);

      // try to find SingleBtnPanel
      m_pSingleBtnPanel = GameObjectUtils.FindNode(gameObject, "SingleBtnPanel");
      if(null == m_pSingleBtnPanel)
      {
        DefaultGameLogger.Instance.dumpLogError("[MessageBox][doInitialize]: Can NOT find SingleBtnPanel Node");

        return;
      }

      // try to find ConfirmBtn node within SingleBtnPanel
      _pGUINode = GameObjectUtils.FindNode(m_pSingleBtnPanel, "ConfirmBtn");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[MessageBox][doInitialize]: Can NOT find ComfirmBtn Node within SingleBtnPanel node");

        return;
      }

      // try to find button component within Comfirm node
      m_pConfirmBtn = _pGUINode.GetComponent< Button >();
      if(null == m_pConfirmBtn)
      {
        DefaultGameLogger.Instance.dumpLogError("[MessageBox][doInitialize]: ConfirmBtn node does NOT have Button component");

        return;
      }

      // register click handler for Confirm button
      m_pConfirmBtn.onClick.AddListener(onYesClicked); // Listener's prototype: public delegate void UnityAction();
    }
    #endregion //PUBLIC METHODS

    #region PRIVATE METHODS
    // callback of Yes/Confirm button click
    void onYesClicked()
    {
      // hide this message box
      GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.hideMessageBox();

      // no handler: return
      if(null == m_pBtnClickHandler)
      {
        return;
      }

      // invoke callback
      m_pBtnClickHandler(DialogResponseTypes.DIALOG_RESPONSE_TYPES_YES, m_pBtnClickHandlerData);

      // reset callback
      m_pBtnClickHandler = null;

      // reset user data
      m_pBtnClickHandlerData = null;
    }

    // callback of No button click
    void onNoClicked()
    {
      // hide this message box
      GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.hideMessageBox();

      // no handler: return
      if(null == m_pBtnClickHandler)
      {
        return;
      }

      // invoke callback
      m_pBtnClickHandler(DialogResponseTypes.DIALOG_RESPONSE_TYPES_NO, m_pBtnClickHandlerData);

      // reset callback
      m_pBtnClickHandler = null;

      // reset user data
      m_pBtnClickHandlerData = null;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // different sorts of icons
    GameObject m_pErrorIcon;
    GameObject m_pWarningIcon;
    GameObject m_pInfoIncon;
    // desc text field
    Text m_pDesc;
    // double button panel
    GameObject m_pDoubleBtnPanel;
    // Yes button within double button panel
    Button m_pYesBtn;
    // No button within double button panel
    Button m_pNoBtn;
    // single button panel
    GameObject m_pSingleBtnPanel;
    // Confirm button within single button panel
    Button m_pConfirmBtn;
    // button click handler
    DialogResponseHandler m_pBtnClickHandler;
    // user data of button click handler
    System.Object m_pBtnClickHandlerData;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.GUI

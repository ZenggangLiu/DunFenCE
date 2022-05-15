/**
 * Purchase Dialog
 */

using UnityEngine;
using UnityEngine.UI;
using Framework.Utils;
using Framework.Logger;
using Game.Logic;

namespace Game.GUI
{
  public class PurchaseDialog : MonoBehaviour
  {
    #region PUBLIC METHODS
    /**
     * show this purchase dialog
     */
    public void show(Sprite a_pItemIcon, string a_szDesc, string a_szYesBtnLable, string a_szNoBtnLable, DialogResponseHandler a_pYesBtnCallback, System.Object a_pUserData)
    {
      // store handler
      m_pBtnClickHandler = a_pYesBtnCallback;

      // store user data
      m_pBtnClickHandlerData = a_pUserData;

      // set item icon
      m_pItemIcon.sprite = a_pItemIcon;

      // set desc
      m_pDesc.text = a_szDesc;

      // set Yes button's lable
      m_pYesBtnLable.text = a_szYesBtnLable;

      // set No button's lable
      m_pNoBtnLable.text = a_szNoBtnLable;

      // enable this GUI
      gameObject.SetActive(true);
    }

    /**
     * hide this purchase dialog
     */
    public void hide()
    {
      // disable this GUI
      gameObject.SetActive(false);
    }

    /**
     * initialize this purchase dialog
     */
    public void doInitialize()
    {
      // try to find ItemPanel  
      GameObject _pGUINode = GameObjectUtils.FindNode(gameObject, "ItemPanel");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[PurchaseDialog][doInitialize]: Can NOT find ItemPanel Node");

        return;
      }

      // try to find item icon
      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "Icon");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[PurchaseDialog][doInitialize]: Can NOT find Icon Node within ItemPanel Node");

        return;
      }

      // try to find image component within item icon node
      m_pItemIcon = _pGUINode.GetComponent< Image >();
      if(null == m_pItemIcon)
      {
        DefaultGameLogger.Instance.dumpLogError("[PurchaseDialog][doInitialize]: Icon Node does NOT have Image component");

        return;
      }

      // try to find Desc node
      _pGUINode = GameObjectUtils.FindNode(gameObject, "Desc");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[PurchaseDialog][doInitialize]: Can NOT find Desc Node");

        return;
      }

      // try to find text field within Desc node
      m_pDesc = _pGUINode.GetComponent< Text >();
      if(null == m_pDesc)
      {
        DefaultGameLogger.Instance.dumpLogError("[PurchaseDialog][doInitialize]: Desc Node does NOT have Text Component");

        return;
      }

      // try to find Yes button node
      _pGUINode = GameObjectUtils.FindNode(gameObject, "YesBtn");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[PurchaseDialog][doInitialize]: Can NOT find YesBtn Node");

        return;
      }

      // try to find button component of Yes button
      m_pYesBtn = _pGUINode.GetComponent< Button >();
      if(null == m_pYesBtn)
      {
        DefaultGameLogger.Instance.dumpLogError("[PurchaseDialog][doInitialize]: YesBtn Node does NOT have Button Component");

        return;
      }

      // try to find Yes button's lable node
      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "Lable");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[PurchaseDialog][doInitialize]: YesBtn Node does NOT have Lable node");

        return;
      }

      // try to get text field of the lable
      m_pYesBtnLable = _pGUINode.GetComponent< Text >();
      if(null == m_pYesBtnLable)
      {
        DefaultGameLogger.Instance.dumpLogError("[PurchaseDialog][doInitialize]: Lable of YesBtn Node does NOT have Text component");

        return;
      }

      // try to find No button node
      _pGUINode = GameObjectUtils.FindNode(gameObject, "NoBtn");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[PurchaseDialog][doInitialize]: Can NOT find NoBtn Node");

        return;
      }

      // try to find button component of No button
      m_pNoBtn = _pGUINode.GetComponent< Button >();
      if(null == m_pNoBtn)
      {
        DefaultGameLogger.Instance.dumpLogError("[PurchaseDialog][doInitialize]: NoBtn Node does NOT have Button Component");

        return;
      }

      // try to find No button's lable node
      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "Lable");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[PurchaseDialog][doInitialize]: NoBtn Node does NOT have Lable node");

        return;
      }

      // try to get text field of the lable
      m_pNoBtnLable = _pGUINode.GetComponent< Text >();
      if(null == m_pNoBtnLable)
      {
        DefaultGameLogger.Instance.dumpLogError("[PurchaseDialog][doInitialize]: Lable of NoBtn Node does NOT have Text component");

        return;
      }

      // register click handler for Yes button
      m_pYesBtn.onClick.AddListener(onYesClicked); // Listener's prototype: public delegate void UnityAction();

      // register click handler for No button
      m_pNoBtn.onClick.AddListener(onNoClicked);
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // callback of Yes button click
    void onYesClicked()
    {
      // hide this dialog
      if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as InventoryLevelGUI)
      {
        (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as InventoryLevelGUI).hidePurchaseDialog();  
      }

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
      // hide this dialog
      if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as InventoryLevelGUI)
      {
        (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as InventoryLevelGUI).hidePurchaseDialog();  
      }

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
    // item icon GUI
    Image m_pItemIcon;
    // desc text field
    Text m_pDesc;
    // Yes button
    Button m_pYesBtn;
    // Yes button's lable
    Text m_pYesBtnLable;
    // No button
    Button m_pNoBtn;
    // No button's lable
    Text m_pNoBtnLable;
    // button click handler
    DialogResponseHandler m_pBtnClickHandler;
    // user data of button click handler
    System.Object m_pBtnClickHandlerData;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.GUI

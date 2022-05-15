/**
 * GUI used during Inventory Level
 */

using UnityEngine;
using UnityEngine.UI;
using Framework.Logger;
using Framework.Utils;
using Game.Logic;

namespace Game.GUI
{
  public class InventoryLevelGUI : GameLevelGUI
  {
    #region PUBLIC METHODS
    // callback for toggling the weapon inventory panel
    public void onToggleWeaponInventory()
    {
      if(true == m_pWeaponInventoryNode.activeSelf)
      {
        hideWeaponInventory();
      }
      else
      {
        showWeaponInventory();
      }
    }

    /**
     * select game level with the given index
     */
    public void selectGameLevel(GameSceneSlot a_pSlot)
    {
      // store level name
      m_szSelectedLevelName = a_pSlot.LevelName;

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[InventoryLevelGUI][selectGameLevel]: Level Selected: " + a_pSlot.LevelName);
#endif
    }

    /**
     * update RP
     */
    public void updateRPBar(int a_iCurReward)
    {
      m_pRPValue.text = a_iCurReward.ToString();
    }

    /**
     * update Coins
     */
    public void updateCoinsBar(int a_iCurCoins)
    {
      m_pCoinCount.text = a_iCurCoins.ToString();
    }

    // callback for buying a weapon
    public void onBuyWeapon(WeaponSlot a_pEquippedSlot)
    {

    }

    /**
     * popup a dialog to buy coin chest
     */
    public void showPurchaseCoinChestDialog()
    {
      // JUST support coin chest
      // get the item ID of the coin chest
      int _iCoinChestID = -1;
      foreach(int _iItemID in IAPItemPurchaseMgr.Instance.CurrentItemInfoCache.Keys)
      {
        _iCoinChestID = _iItemID;

        break;
      }

      // show the purchase dialog
      showPurchaseDialog(IAPItemPurchaseMgr.Instance.CurrentItemInfoCache[_iCoinChestID].SpriteName, "Do you want to buy a " + IAPItemPurchaseMgr.Instance.CurrentItemInfoCache[_iCoinChestID].Name + " full of " + IAPItemPurchaseMgr.Instance.CurrentItemInfoCache[_iCoinChestID].GainedCoinAmount + " coins for " + IAPItemPurchaseMgr.Instance.CurrentItemInfoCache[_iCoinChestID].PriceString + " ?", "Yes", "No", onIAPItemPurchaseDialogClosed, _iCoinChestID); 
    }

    /**
     * popup a singleton purchase dialog with the given item icon, text, yes and no button
     */
    public void showPurchaseDialog(string a_szItemIconName, string a_szDesc, string a_szYesBtnLable, string a_szNoBtnLable, DialogResponseHandler a_pResponseHandler, System.Object a_pUserData)
    {
      // disable all nodes under ModalDialogPanel
      for(int _iNIdx = 0; _iNIdx < m_pModalDialogPanel.transform.childCount; _iNIdx++)
      {
        m_pModalDialogPanel.transform.GetChild(_iNIdx).gameObject.SetActive(false);
      }

      // enable ModalDialogPanel
      m_pModalDialogPanel.SetActive(true);

      // try to get the item icon
      Sprite _pItemIcon = null;
      getSprite(a_szItemIconName, out _pItemIcon);

      // pop up purchase dialog
      m_pPurchaseDialog.show(_pItemIcon, a_szDesc, a_szYesBtnLable, a_szNoBtnLable, a_pResponseHandler, a_pUserData);
    }

    /**
     * hide the purchase dialog
     */
    public void hidePurchaseDialog()
    {
      // hide the purchase dialog
      m_pPurchaseDialog.hide();

      // disable ModelDialogPanel
      m_pModalDialogPanel.SetActive(false);
    }

    /**
     * show about dialog
     */
    public void showAboutDialog()
    {
      // disable all nodes under ModalDialogPanel
      for(int _iNIdx = 0; _iNIdx < m_pModalDialogPanel.transform.childCount; _iNIdx++)
      {
        m_pModalDialogPanel.transform.GetChild(_iNIdx).gameObject.SetActive(false);
      }

      // enable ModalDialogPanel
      m_pModalDialogPanel.SetActive(true);

      // popup the about dialog
      m_pAboutDialog.show();

      // set about dialog button to the pressed color
      m_pAboutDialogBtnIcon.color = m_vAboutDialogBtnPressedColor;
    }

    /**
     * hide about dialog
     */
    public void hideAboutDialog()
    {
      // hide the about dialog
      m_pAboutDialog.hide();

      // disable ModelDialogPanel
      m_pModalDialogPanel.SetActive(false);

      // set about dialog button to the normal color
      m_pAboutDialogBtnIcon.color = m_vAboutDialogBtnNormalColor;
    }

    /**
     * show level choose dialog
     */
    public void showLevelSelectDialog()
    {
      // disable all nodes under ModalDialogPanel
      for(int _iNIdx = 0; _iNIdx < m_pModalDialogPanel.transform.childCount; _iNIdx++)
      {
        m_pModalDialogPanel.transform.GetChild(_iNIdx).gameObject.SetActive(false);
      }

      // enable ModalDialogPanel
      m_pModalDialogPanel.SetActive(true);

      // show the level select dialog
      m_pLevelSelectDialogNode.SetActive(true);

      // set level select dialog button to the pressed color
      m_pLevelSelectDialogBtnIcon.color = m_vLevelSelectDialogBtnPressedColor;
    }

    /**
     * hide level choose dialog
     */
    public void hideLevelSelectDialog()
    {
      // hide the level select dialog
      m_pLevelSelectDialogNode.SetActive(false);

      // disable ModelDialogPanel
      m_pModalDialogPanel.SetActive(false);

      // set level select dialog button to the normal color
      m_pLevelSelectDialogBtnIcon.color = m_vLevelSelectDialogBtnNormalColor;
    }

    /**
     * start defend:
     * - called when Defend Button has been clicked
     * - called directly from code
     */
    public void startDefend()
    {
      // hide the game level select dialog
      hideLevelSelectDialog();

      // load selected level
      GameLevelMgr.Instance.GameMainLevel.MainEntry.loadGameLevel(m_szSelectedLevelName, Game.Camera.CameraModes.CAMERA_MODES_AR_SUPPORT);
    }

    #region PUBLIC OVERRIDDENS
    /**
     * Initialize function
     * NOTE:m_bIsInitialized MUST be SET in doInitialize() method of derived class
     */
    public override void doInitialize()
    {
      base.doInitialize();

      // try to get RP value text field
      GameObject _pGUINode = GameObjectUtils.FindNode(gameObject, "RPPanel");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: can NOT find RPPanel node");

        return;
      }

      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "RPValue");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: can NOT find RPPanel's RPValue node");

        return;
      }

      // try to get RP value text field
      m_pRPValue = _pGUINode.GetComponentInChildren< Text >();
      if(null == m_pRPValue)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: RPValue GUI node is NOT a Text field");

        return;
      }

      // set stored RP value
      updateRPBar(GameSaveMgr.Instance.CurrentGameSave.RP);

      // try to get coin count text field
      _pGUINode = GameObjectUtils.FindNode(gameObject, "CoinsPanel");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: can NOT find CoinsPanel node");

        return;
      }

      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "CoinsValue");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: can NOT find CoinsPanel's CoinsValue node");

        return;
      }

      m_pCoinCount = _pGUINode.GetComponentInChildren< Text >();
      if(null == m_pCoinCount)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: CoinsValue GUI node is NOT a Text field");

        return;
      }

      // set stored coins value
      updateCoinsBar(GameSaveMgr.Instance.CurrentGameSave.Coins);

      // try to find weapon inventory node
      m_pWeaponInventoryNode = GameObjectUtils.FindNode(gameObject, "WeaponInventory");
      if(null == m_pWeaponInventoryNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: can NOT find WeaponInventory Node");

        return;
      }

      m_pWeaponInventory = m_pWeaponInventoryNode.GetComponentInChildren< WeaponInventory >();
      if(null == m_pWeaponInventory)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: WeaponInventory node does NOT have WeaponInventory.cs attached");

        return;
      }

      // initialize weapon inventory
      m_pWeaponInventory.doInitialize(WInventoryWModes.WINVENTORY_WMODES_SELLING);

      // try ot find weapon inventory button node
      _pGUINode = GameObjectUtils.FindNode(gameObject, "WeaponInventoryBtn");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: can NOT find WeaponInventoryBtn node");

        return;
      }

      // store weapon inventory button's pressed color
      m_vWeaponInventoryBtnPressedColor = _pGUINode.GetComponentInChildren< Button >().colors.pressedColor;

      // try to find Icon node withing WeaponInventoryBtn node
      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "Icon");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: can NOT find Icon node within WeaponInventoryBtn node");

        return;
      }

      // try to get image component of weapon inventory button's icon
      m_pWeaponInventoryBtnIcon = _pGUINode.GetComponent< Image >();
      if(null == m_pWeaponInventoryBtnIcon)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: Icon node does NOT have Image component");

        return;
      }

      // store weapon inventory button's normal color
      m_vWeaponInventoryBtnNormalColor = m_pWeaponInventoryBtnIcon.color;

      // hide the weapon inventory by default
      hideWeaponInventory();

      // try to find PurchaseDialog
      _pGUINode = GameObjectUtils.FindNode(m_pModalDialogPanel, "PurchaseDialog");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: Can NOT find PurchaseDialog node within ModalDialogPanel Node");

        return;
      }

      // disable the PurchaseDialog
      _pGUINode.SetActive(false);

      // try to get PurchaseDialog component
      m_pPurchaseDialog = _pGUINode.GetComponent< PurchaseDialog >();
      if(null == m_pPurchaseDialog)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: PurchaseDialog node does NOT have PurchaseDialog.cs attached");

        return;
      }

      // initialize the purchase dialog
      m_pPurchaseDialog.doInitialize();

      // try to find AboutDialog node
      _pGUINode = GameObjectUtils.FindNode(m_pModalDialogPanel, "AboutDialog");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: Can NOT find AboutDialog node within ModalDialogPanel Node");

        return;
      }

      // try to get AboutDialog component
      m_pAboutDialog = _pGUINode.GetComponent< AboutDialog >();
      if(null == m_pAboutDialog)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: AboutDialog node does NOT have AboutDialog.cs attached");

        return;
      }

      // initialize AboutDialog
      m_pAboutDialog.doInitialize();

      // try ot find about dialog button node
      _pGUINode = GameObjectUtils.FindNode(gameObject, "AboutDialogBtn");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: can NOT find AboutDialogBtn node");

        return;
      }

      // store about dialog button's pressed color
      m_vAboutDialogBtnPressedColor = _pGUINode.GetComponentInChildren< Button >().colors.pressedColor;

      // try to find Icon node withing AboutDialogBtn node
      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "Icon");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: can NOT find Icon node within AboutDialogBtn node");

        return;
      }

      // try to get image component of about dialog button's icon
      m_pAboutDialogBtnIcon = _pGUINode.GetComponent< Image >();
      if(null == m_pAboutDialogBtnIcon)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: Icon node does NOT have Image component");

        return;
      }

      // store about dialog button's normal color
      m_vAboutDialogBtnNormalColor = m_pAboutDialogBtnIcon.color;

      // try to find LevelSelectDialog node
      m_pLevelSelectDialogNode = GameObjectUtils.FindNode(gameObject, "LevelSelectDialog");
      if(null == m_pLevelSelectDialogNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: can NOT find LevelSelectDialog node");

        return;
      }

      // try to get Content node within LevelSelectDialog
      _pGUINode = GameObjectUtils.FindNode(m_pLevelSelectDialogNode, "Content");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: can NOT find Content node within LevelSelectDialog node");

        return;
      }

      // initialize all scene slot
      for(int _iIdx = 0; _iIdx < _pGUINode.transform.childCount; _iIdx++)
      {
        if(_pGUINode.transform.GetChild(_iIdx).GetComponent< GameSceneSlot >() == null)
        {
          DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: LevelSelectDialog node's " + (_iIdx + 1) + "th slot does NOT have GameSceneSlot.cs attached");

          return;
        }

        _pGUINode.transform.GetChild(_iIdx).GetComponent< GameSceneSlot >().doInitialize(_iIdx);
      }

      // try ot find level select dialog button node
      _pGUINode = GameObjectUtils.FindNode(gameObject, "LevelSelectBtn");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: can NOT find LevelSelectBtn node");

        return;
      }

      // store level select dialog button's pressed color
      m_vLevelSelectDialogBtnPressedColor = _pGUINode.GetComponentInChildren< Button >().colors.pressedColor;

      // try to find Icon node withing LevelSelectBtn node
      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "Icon");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: can NOT find Icon node within LevelSelectBtn node");

        return;
      }

      // try to get image component of level select dialog button's icon
      m_pLevelSelectDialogBtnIcon = _pGUINode.GetComponent< Image >();
      if(null == m_pLevelSelectDialogBtnIcon)
      {
        DefaultGameLogger.Instance.dumpLogError("[InventoryLevelGUI][doInitialize]: Icon node does NOT have Image component");

        return;
      }

      // store level select dialog button's normal color
      m_vLevelSelectDialogBtnNormalColor = m_pLevelSelectDialogBtnIcon.color;

      // set default game level name
      m_szSelectedLevelName = "FarmSummer-SunRise";

      // register IAP item purchase callback
      // failed to buy an item: data: game specific item id
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_IAP_PURCHASE_FAILED, onIAPItemPurchaseFailed);
      // item purchase succeeded: data: bought game specific item id
      Game.Event.GameEventMgr.Instance.registerGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_IAP_PURCHASE_SUCCEEDED, onIAPItemPurchaseSucceeded);

      // set initialization flag
      m_bIsInitialized = true;
    }

    /*
     * the function called before destroying this GUI
     * NOTE:m_bIsInitialized MUST be CLEARED in doDestroy() method in derived class
     */
    public override void doDestroy()
    {
      base.doDestroy();

      // unregister IAP item purchase callback
      // failed to buy an item: data: game specific item id
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_IAP_PURCHASE_FAILED, onIAPItemPurchaseFailed);
      // item purchase succeeded: data: bought game specific item id
      Game.Event.GameEventMgr.Instance.unRegisterGlobalScopeEventHandler(Game.Event.EventIDs.EVENT_IDS_LOGIC_IAP_PURCHASE_SUCCEEDED, onIAPItemPurchaseSucceeded);

      // clear initialization flag
      m_bIsInitialized = false;
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // show weapon inventory
    void showWeaponInventory()
    {
      // let inventory class handle the show event
      m_pWeaponInventory.onShowPanel();

      // ACTIVE the inventory
      // NOTE: otherwise the SetTrigger() will return the error: Animator is not playing a Playable
      if(false == m_pWeaponInventoryNode.activeSelf)
      {
        m_pWeaponInventoryNode.SetActive(true);
      }

      // play show weapon inventory animation
      if(m_pWeaponInventory.GetComponent < Animator >() != null)
      {
        m_pWeaponInventory.GetComponent < Animator >().SetTrigger(Animator.StringToHash("Show")); 
      }

      // set weapon inventory button to the pressed color
      m_pWeaponInventoryBtnIcon.color = m_vWeaponInventoryBtnPressedColor;
    }

    // hide weapon inventory
    void hideWeaponInventory()
    {
      // play hide weapon inventory animation, if we can
      // then the animation will trigger onHidePanel()
      // - let the weapon inventory handle the hide event
      if(true == m_pWeaponInventoryNode.activeSelf                   // if the inventory panel is active
         &&                                                          // and
         m_pWeaponInventory.GetComponent < Animator >() != null      // there is an Animator
        )
      {
        m_pWeaponInventory.GetComponent < Animator >().SetTrigger(Animator.StringToHash("Hide")); 
      }
      else
      {
        // if there is NO hide animation or the panel is already hidden
        // let the weapon inventory handle the hide event directly
        m_pWeaponInventory.onHidePanel();
      }

      // set weapon inventory button to the normal color
      m_pWeaponInventoryBtnIcon.color = m_vWeaponInventoryBtnNormalColor;
    }

    // callback for coin chest purchase dialog close
    void onIAPItemPurchaseDialogClosed(DialogResponseTypes a_eResponseType, System.Object a_pUserData)
    {
      // read the item ID
      int _iItemID = (int)a_pUserData;

      // NOT want to buy: return
      if(DialogResponseTypes.DIALOG_RESPONSE_TYPES_NO == a_eResponseType)
      {
#if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("user has cancelled the purchase of IAP item[" + _iItemID + "]: " + IAPItemPurchaseMgr.Instance.CurrentItemInfoCache[_iItemID].Name);
#endif
        return;
      }
      else
      {
        // try to buy this iap item
#if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("user is trying to purchase IAP item[" + _iItemID + "]: " + IAPItemPurchaseMgr.Instance.CurrentItemInfoCache[_iItemID].Name);
#endif
        IAPItemPurchaseMgr.Instance.buyItem(_iItemID);
      }
    }

    // callback when IAP item purchase failed
    void onIAPItemPurchaseFailed(Game.Event.GameEvent a_pEventRef)
    {
      // read the item ID
      int _iItemID = (int)a_pEventRef.Data;

      // pop error message box
      GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.showMessageBox(MessageBoxTypes.MESSAGEBOX_TYPES_ERROR_SINGLEBTN, "Sorry we can not buy the " + IAPItemPurchaseMgr.Instance.CurrentItemInfoCache[_iItemID].Name + ".\nPlease try again.", null, null);
    }

    // callback when IAP item purchase succeeded
    void onIAPItemPurchaseSucceeded(Game.Event.GameEvent a_pEventRef)
    {
      // read the item ID
      int _iItemID = (int)a_pEventRef.Data;

      // pop message box
      GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.showMessageBox(MessageBoxTypes.MESSAGEBOX_TYPES_INFO_SINGLEBTN, "You have successfully bought the " + IAPItemPurchaseMgr.Instance.CurrentItemInfoCache[_iItemID].Name +".\nThank you very much.", null, null);

      // add the coins
      EconomyMgr.Instance.gainCoins(IAPItemPurchaseMgr.Instance.CurrentItemInfoCache[_iItemID].GainedCoinAmount);
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // RP value
    Text m_pRPValue;
    // coin count
    Text m_pCoinCount;
    // weapon inventory button's icon
    Image m_pWeaponInventoryBtnIcon;
    // weapon inventory button's normal color
    Color m_vWeaponInventoryBtnNormalColor;
    // weapon inventory button's pressed color
    Color m_vWeaponInventoryBtnPressedColor;
    // current weapon inventory panel node
    GameObject m_pWeaponInventoryNode;
    // current weapon inventory component
    WeaponInventory m_pWeaponInventory;
    // purchase dialog
    PurchaseDialog m_pPurchaseDialog;
    // about dialog
    AboutDialog m_pAboutDialog;
    // about dialog button's icon
    Image m_pAboutDialogBtnIcon;
    // about dialog button's normal color
    Color m_vAboutDialogBtnNormalColor;
    // about dialog button's pressed color
    Color m_vAboutDialogBtnPressedColor;
    // game level dialog node
    GameObject m_pLevelSelectDialogNode;
    // level select dialog button's icon
    Image m_pLevelSelectDialogBtnIcon;
    // level select dialog button's normal color
    Color m_vLevelSelectDialogBtnNormalColor;
    // level select dialog button's pressed color
    Color m_vLevelSelectDialogBtnPressedColor;
    // selected game level name
    string m_szSelectedLevelName;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.GUI

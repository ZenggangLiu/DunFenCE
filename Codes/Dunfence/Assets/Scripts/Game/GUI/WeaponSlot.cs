/**
 * Slot within Weapon Inventory: each slot UI element has ONE WeaponSlot.cs attached
 */
using UnityEngine;
using UnityEngine.UI;
using Game.Logic;
using Game.Camera;
using Framework.Utils;
using Framework.Logger;
using System.Collections.Generic;

namespace Game.GUI
{
  public class WeaponSlot : MonoBehaviour
  {
    #region PUBLIC METHODS
    // return the icon used in this slot
    public Sprite WeaponIcon
    {
      get
      {
        return m_pWeaponIcon.sprite;
      }
    }

    /**
     * initialize this slot with the given working mode, weapon UID and slot index
     */
    public void doInitialize(WInventoryWModes a_eWorkingMode, int a_iWeaponUID, int a_iSlotIndex, ToggleGroup a_pToggleGroup)
    {
      // NO weapon UID
      if(a_iWeaponUID < 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][WeaponSlot][doInitialize]: No Weapon UID");

        return;
      }

      // NO slot index
      if(a_iSlotIndex < 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][WeaponSlot][doInitialize]: Negative Weapon Slot Index");

        return;
      }

      // store working mode
      m_eCurMode = a_eWorkingMode;

      // store slot index
      m_iSlotIndex = a_iSlotIndex;

      // try to find weapon icon
      m_pWeaponIcon = GetComponent< Image >();
      if(null == m_pWeaponIcon)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][WeaponSlot][doInitialize]: can No find Weapon Icon");

        return;
      }

      // try to find price panel
      m_pPricePanel = GameObjectUtils.FindNode(gameObject, "PricePanel");
      if(null == m_pPricePanel)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][WeaponSlot][doInitialize]: can No find PricePanel node");

        return;
      }

      // try to find value panel within price panel
      GameObject _pGUINode = GameObjectUtils.FindNode(m_pPricePanel, "Value");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][WeaponSlot][doInitialize]: PricePanel does NOT have a Value node");

        return;
      }

      // try to find price value text field within value panel
      m_pPriceValue = _pGUINode.GetComponent< Text >();
      if(null == m_pPriceValue)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][WeaponSlot][doInitialize]: PricePanel's Value panel node does NOT have a Text field");

        return;
      }

      // try to find damage panel
      _pGUINode = GameObjectUtils.FindNode(gameObject, "DamagePanel");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][WeaponSlot][doInitialize]: can No find DamagePanel node");

        return;
      }

      // try to find value panel within damage panel
      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "Value");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][WeaponSlot][doInitialize]: DamagePanel does NOT have a Value node");

        return;
      }

      // try to find damage value text field within value panel
      m_pDamageValue = _pGUINode.GetComponent< Text >();
      if(null == m_pDamageValue)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][WeaponSlot][doInitialize]: DamagePanel's Value panel node does NOT have a Text field");

        return;
      }

      // try to find shoot rate panel
      _pGUINode = GameObjectUtils.FindNode(gameObject, "ShootRatePanel");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][WeaponSlot][doInitialize]: can No find ShootRatePanel node");

        return;
      }

      // try to find value panel within shoot rate panel
      _pGUINode = GameObjectUtils.FindNode(_pGUINode, "Value");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][WeaponSlot][doInitialize]: ShootRatePanel does NOT have a Value node");

        return;
      }

      // try to find shoot rate value text field within value panel
      m_pShootRateValue = _pGUINode.GetComponent< Text >();
      if(null == m_pShootRateValue)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][WeaponSlot][doInitialize]: ShootRatePanel's Value panel node does NOT have a Text field");

        return;
      }

      // try to find lock icon panel
      m_pLockIconPanel = GameObjectUtils.FindNode(gameObject, "Lock");
      if(null == m_pLockIconPanel)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][WeaponSlot][doInitialize]: can NOT find Lock node");

        return;
      }

      // get weapon define using given UID
      if(false == WeaponMgr.Instance.getWeaponDefine(a_iWeaponUID, out m_pDefine))
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][WeaponSlot][doInitialize]: can NOT get Weapon Define from Weapon DataBase: "+ a_iWeaponUID);

        return;
      }

      // set weapon icon
      Sprite _pCurWeaponIconSprite = null;
      if(true == GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.getSprite(m_pDefine.IconName, out _pCurWeaponIconSprite))
      {
        m_pWeaponIcon.sprite = _pCurWeaponIconSprite;
      }

      // store damage value
      m_pDamageValue.text = m_pDefine.DamagePoint.ToString();

      // store shoot rate value
      m_pShootRateValue.text = m_pDefine.ShootRate.ToString();

      // selling mode
      if(WInventoryWModes.WINVENTORY_WMODES_SELLING == a_eWorkingMode)
      {
        // set price value
        m_pPriceValue.text = m_pDefine.Price.ToString();

        // disable Tick's image component
        if(GameObjectUtils.FindNode(gameObject, "Tick") != null
           &&
           GameObjectUtils.FindNode(gameObject, "Tick").GetComponent< Image >() != null
          )
        {
          GameObjectUtils.FindNode(gameObject, "Tick").GetComponent< Image >().enabled = false;
        }
      }
      else
      {
        // using mode
        // enable the price panel
        m_pPricePanel.SetActive(false);

        // set toggle group
        if(GetComponent< Toggle >() != null)
        {
          GetComponent< Toggle >().group = a_pToggleGroup;
        }
      }

      // set lock panel
      // locked by default
      m_bIsLocked = true;

      // iterate over all already bought weapons
      for(int _iWIdx = 0; _iWIdx < GameSaveMgr.Instance.CurrentGameSave.BoughtWeaponList.Count; _iWIdx++)
      {
        // different weapon: go to next one
        if(m_pDefine.UID != GameSaveMgr.Instance.CurrentGameSave.BoughtWeaponList[_iWIdx])
        {
          continue;
        }

        // this weapon has been bought
        m_bIsLocked = false;

        break;
      }

      // set lock panel based on if this weapon is locked
      m_pLockIconPanel.SetActive(m_bIsLocked);

      // in using mode, locked slot, disable toggle behaviour
      if(WInventoryWModes.WINVENTORY_WMODES_USING == a_eWorkingMode
         &&
         true == m_bIsLocked
        )
      {
        if(GetComponent< Toggle >() != null)
        {
          GetComponent< Toggle >().interactable = false;
        }
      }
      else
      {
        if(GetComponent< Toggle >() != null)
        {
          GetComponent< Toggle >().interactable = true;
        }
      }

      // enable this slot
      gameObject.SetActive(true);
    }

    /**
     * bought this slot's weapon
     */
    public void buyWeapon()
    {
      
    }

    /**
     * equip this slot's weapon
     * return the equipped weapon game object
     */
    public GameObject equipWeapon()
    {
      // try to use locked weapon
      if(true == m_bIsLocked)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][equipWeapon]: try to equip LOCKED Weapon[" + m_pDefine.WeaponID + "]");

        return null;
      }

      // equip the weapon slot: weapon ID and spearate projectile ID
      // equip weapon: also equip the mounted weapon projectile
      if(false == WeaponMgr.Instance.equipWeapon(m_pDefine.WeaponID, GameLevelMgr.Instance.GameMainLevel.CameraNode, out m_pEquippedWeapon))
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][equipWeapon]: can No equip Weapon[" + m_pDefine.WeaponID + "]");

        return null;
      }

      // confirm the equipped weapon is ShootWeapon
      if(null == m_pEquippedWeapon.GetComponentInChildren< ShootWeapon >())
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][equipWeapon]: Weapon[" + m_pDefine.WeaponID + "] does NOT contain ShootWeapon.cs");

        return null;
      }

      // equip spearate projectile
      if(m_pDefine.ProjectileID >= 0)
      {
        // load the spearate projectile: this will also equip the given projectile
        m_pEquippedWeapon.GetComponentInChildren< ShootWeapon >().doChangeProjectile(m_pDefine.ProjectileID);
      }

      // set the aim helper of the defense camera controller
      m_pEquippedWeapon.GetComponentInChildren< ShootWeapon >().setShootAimHelper(GameLevelMgr.Instance.GameMainLevel.CameraNode.GetComponentInChildren< CameraController >());

      // set equipped flag
      m_bIsEquipped = true;

      return m_pEquippedWeapon;
    }

    // unEquip this slot's weapon
    public void unEquipWeapon()
    {
      // NOTHING is equipped: return
      if(null == m_pEquippedWeapon)
      {
        return;
      }

      // if there is a separatte projectile: unequip it
      if(m_pDefine.ProjectileID >= 0)
      {
        ProjectileMgr.Instance.unEquipProjectile(m_pDefine.ProjectileID);
      }

      // unequip the weapon: it will also unEquip its mounted projectile
      if(false == WeaponMgr.Instance.unEquipWeapon(m_pEquippedWeapon, GameLevelMgr.Instance.GameMainLevel.CameraNode))
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][unEquipWeapon]: can No UnEquip Weapon[" + m_pDefine.WeaponID + "]");

        return;
      }

      // clear weapon reference
      m_pEquippedWeapon = null;

      // clear equip flag
      m_bIsEquipped = false;
    }

    // callback when this slot is toggled
    // NOTE: each slot is a toggle button, system will give true in a_bIsOn when this slot is toggled on
    public void onSlotToggled(bool a_bIsOn) // WITHOUT ToggleGroup, Each Click will Flip this Toggle Button: On <--> Off
    {
      // selling mode
      if(WInventoryWModes.WINVENTORY_WMODES_SELLING == m_eCurMode)
      {
        // if already unlocked: return
        if(false == m_bIsLocked)
        {
          return;
        }

        // want to unlock this weapon
        // show unlock dialog
        showWantUnlockWeaponDialog();
      }

      // using mode
      else if(WInventoryWModes.WINVENTORY_WMODES_USING == m_eCurMode)
      {
        // locked weapon: return
        if(true == m_bIsLocked)
        {
          return;
        }

        if(true == a_bIsOn        // want to equip the given slot
           &&                     // and
           false == m_bIsEquipped // have not equipped it yet
          )
        {
          // equip this slot
          (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.DefenseLevelGUI).WeaponInventory.equipWeapon(m_iSlotIndex);
        }
        else
        {
          // unEquip this slot: weapon inventory will be responsible to unequip old weapon
        }
      }
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // pop up the want ot unlock weapon dialog
    void showWantUnlockWeaponDialog()
    {
      // for inventory GUI, show purchase dialog
      if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as InventoryLevelGUI)
      {
        // show purchase dialog using GameLevelGUI
        (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as InventoryLevelGUI).showPurchaseDialog(m_pDefine.IconName, "Do you want to buy this weapon with " + m_pDefine.Price + " coins ?", "Yes", "No", onPurchaseDialogClosed, m_pDefine.IconName);
      }
    }

    // callback for purchase dialog close
    void onPurchaseDialogClosed(DialogResponseTypes a_eResponseType, System.Object a_pUserData)
    {
      // NOT want to buy: return
      if(DialogResponseTypes.DIALOG_RESPONSE_TYPES_NO == a_eResponseType)
      {
#if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("user has cancelled purchase: " + (a_pUserData as string));
#endif

        return;
      }
      else
      {
        // try to buy this weapon
        if(true == EconomyMgr.Instance.purchaseWeaponWithCoins(m_pDefine.Price, m_pDefine.UID))
        {
#if !RETAIL
          DefaultGameLogger.Instance.dumpLogInfo("user has BOUGHT: " + (a_pUserData as string));
#endif

          // set this weapon to unlocked
          m_bIsLocked = false;

          // hide lock panel
          m_pLockIconPanel.SetActive(false);
        }
        else
        {
#if !RETAIL
          DefaultGameLogger.Instance.dumpLogInfo("user's purchase FAILED(" + (a_pUserData as string) + "). No enough coins");
#endif

          // pop error message box
          GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.showMessageBox(MessageBoxTypes.MESSAGEBOX_TYPES_ERROR_SINGLEBTN, "No enough coins!\nYou are trying to buy a weapon, which needs " + m_pDefine.Price +" coins.\nBut you just have: " + GameSaveMgr.Instance.CurrentGameSave.Coins + " coins.", onNoEnoughtCoinDialogClosed, null);
        }
      }
    }

    // callback when no enought coin dialog closed during weapon purchase
    void onNoEnoughtCoinDialogClosed(DialogResponseTypes a_eResponseType, System.Object a_pUserData)
    {
      // for inventory GUI, pop up IAP purchase dialog
      if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as InventoryLevelGUI)
      {
        // pop up IAP purchase dialog
        (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as InventoryLevelGUI).showPurchaseCoinChestDialog();
      }
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // current working mode
    WInventoryWModes m_eCurMode;
    // weapon define
    WeaponDefine m_pDefine;
    // slot index
    int m_iSlotIndex;

    // weapon icon
    Image m_pWeaponIcon;
    // price panel
    GameObject m_pPricePanel;
    // price value text field within price panel
    Text m_pPriceValue;
    // damage value text field within damage panel
    Text m_pDamageValue;
    // shoot rate text field within shoot rate panel
    Text m_pShootRateValue;
    // lock icon panel
    GameObject m_pLockIconPanel;

    // equipped weapon
    GameObject m_pEquippedWeapon;
    // equipped flag
    bool m_bIsEquipped;
    // if this slot's weapon is locked
    bool m_bIsLocked;
    #endregion // PRIVATE DATA MEMBERS
  }
} // namespace Game.GUI

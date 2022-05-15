/**
 * Weapon Inventory Panel class
 * - initialize: find all slots
 * - show: re-layout all slots
 * - equip a given slot: 
 *   + find the given WeaponSlot and equit it
 *   + change the shoot button's weapon icon
 * 
 * Equip Weapon Workflow:
 *  Clicked Weapon Slot Toggle Button
 *  --> WeaponSlot.onSlotToggled(is_toggled)
 *    --> WeaponInventory.equipWeapon(currentSlotIndex) [0, N]
 *       --> WeaponSlot.equipWeapon()
 *           --> DefenseLevelGUI.onEquipWeapon(EquippedWeaponInstance, EquippedWeaponSlot)
 *               --> change ShootBtn's weapon icon and done with weapon equippment
 */

using UnityEngine;
using UnityEngine.UI;
using Game.Logic;
using Framework.Utils;
using Framework.Logger;

namespace Game.GUI
{
  // the different working modes of weapon inventory
  public enum WInventoryWModes
  {
    // selling different weapons
    WINVENTORY_WMODES_SELLING,
    // using different weapons
    WINVENTORY_WMODES_USING,
  }

  public class WeaponInventory : MonoBehaviour
  {
    #region PUBLIC METHODS
    /**
     * initialize a weapon inventory with given working mode
     */
    public void doInitialize(WInventoryWModes a_eWorkingMode)
    {
      // store working mode
      m_eCurMode = a_eWorkingMode;

      // reset variables
      m_iCurEquippedSlotIndex = -1;

      // try to find Content node
      m_pContentNode = GameObjectUtils.FindNode(gameObject, "Content");
      if(null == m_pContentNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[WeaponInventory][doInitialize]: [" + gameObject.ToString() + "]: Can NOT find Content Node within Weapon Inventory Panel");

        return;
      }

      // counting the number of shoot weapons
      int _iTotalShootWeapon = 0;

      // iterate over the whole weapon database
      for(int _iWIdx = 0; _iWIdx < WeaponMgr.Instance.CurrentWeaponDefineList.Count; _iWIdx++)
      {
        // if the current weapon define is for a shoot weapon
        if(true == WeaponMgr.Instance.CurrentWeaponDefineList[_iWIdx].IsShootWeapon)
        {
          ++_iTotalShootWeapon;
        }
      }

      // NO shoot weapon at all
      if(0 == _iTotalShootWeapon)
      {
        DefaultGameLogger.Instance.dumpLogError("[WeaponInventory][doInitialize]: [" + gameObject.ToString() + "]: NO Shoot Weapon in Weapon DataBase");

        return;
      }

      // create weapon slot list and initialize all slots
      m_aSlotList = new WeaponSlot[_iTotalShootWeapon];

      // create all weapon slots
      float _fCurX = WEAPONSLOT_START_X;
      GameObject _pWeaponSlotGUI = null;
      int _iCurSlotIndex = 0;

      // Using Weapon Mode: insert all bought/equipped weapons at first
      if(WInventoryWModes.WINVENTORY_WMODES_USING == a_eWorkingMode)
      {
        // create slots for all bought/equipped shoot weapons
        for(int _iWIdx = 0; _iWIdx < GameSaveMgr.Instance.CurrentGameSave.BoughtWeaponList.Count; _iWIdx++)
        {
          // try to create a new weapon slot GUI
          if(false == createWeaponSlot(GameSaveMgr.Instance.CurrentGameSave.BoughtWeaponList[_iWIdx], _fCurX, _iCurSlotIndex, out _pWeaponSlotGUI))
          {
            DefaultGameLogger.Instance.dumpLogError("[WeaponInventory][doInitialize]: [" + gameObject.ToString() + "]: can NOT create a Weapon Slot");

            return;
          }

          // successful creation
          // shift to next slot GUI
          // GUI scale factor * GUI width
          _fCurX += _pWeaponSlotGUI.GetComponent< RectTransform >().rect.width * GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.RootCanvas.scaleFactor;

          // add margin
          _fCurX += WEAPONSLOT_MARGIN;

          // increase slot index
          ++_iCurSlotIndex;
        }
      }

      // iterate over the whole weapon database
      for(int _iWIdx = 0; _iWIdx < WeaponMgr.Instance.CurrentWeaponDefineList.Count; _iWIdx++)
      {
        // if the current weapon define is NOT for a shoot weapon
        if(false == WeaponMgr.Instance.CurrentWeaponDefineList[_iWIdx].IsShootWeapon)
        {
          continue;
        }

        // in weapon using mode and already equipped: go to next weapon
        if(WInventoryWModes.WINVENTORY_WMODES_USING == a_eWorkingMode
           &&
           true == isWeaponAlreadyEquipped(WeaponMgr.Instance.CurrentWeaponDefineList[_iWIdx].UID)
          )
        {
          continue;
        }

        // try to create a new weapon slot GUI
        if(false == createWeaponSlot(WeaponMgr.Instance.CurrentWeaponDefineList[_iWIdx].UID, _fCurX, _iCurSlotIndex, out _pWeaponSlotGUI))
        {
          DefaultGameLogger.Instance.dumpLogError("[WeaponInventory][doInitialize]: [" + gameObject.ToString() + "]: can NOT create a Weapon Slot");

          return;
        }

        // successful creation
        // shift to next slot GUI
        // GUI scale factor * GUI width
        _fCurX += _pWeaponSlotGUI.GetComponent< RectTransform >().rect.width * GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.RootCanvas.scaleFactor;

        // add margin
        _fCurX += WEAPONSLOT_MARGIN;

        // increase slot index
        ++_iCurSlotIndex;
      }
    }

    /**
     * equip the given weapon
     */
    public void equipWeapon(int a_iSlotIndex)
    {
      // check if the given slot index is valid
      if(a_iSlotIndex > m_aSlotList.Length)
      {
        DefaultGameLogger.Instance.dumpLogError("[WeaponInventory][equipWeapon]: wrong slot index. slot total count: " + m_aSlotList.Length + ", given slot index: " + a_iSlotIndex);

        return;
      }

      // unEquip the old weapon slot
      if(m_iCurEquippedSlotIndex >= 0)
      {        
        m_aSlotList[m_iCurEquippedSlotIndex].unEquipWeapon();

        m_iCurEquippedSlotIndex = -1;
      }

      // equip the given weapon slot
      GameObject _pEquippedWeaponGO = m_aSlotList[a_iSlotIndex].equipWeapon();

      // can NOT equip the given weapon slot: return
      if(null == _pEquippedWeaponGO)
      {
        return;
      }

      // change GUI part related to the equipped weapon
      (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as DefenseLevelGUI).onEquipWeapon(_pEquippedWeaponGO.GetComponentInChildren< ShootWeapon >(), 
                                                                                             m_aSlotList[a_iSlotIndex]
                                                                                            );

      // store the currently equipped slot index
      m_iCurEquippedSlotIndex = a_iSlotIndex;
    }

    // callback when the weapon inventory panel is about to be shown
    // re-layout all weapon inventory slots
    public void onShowPanel()
    {
      gameObject.SetActive(true);
    }

    // callback when the weapon inventory panel is about to be hiden
    // called from hide animation if exists or directly from code
    public void onHidePanel()
    {
      gameObject.SetActive(false);
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // return true, if the given weapon already equipped
    bool isWeaponAlreadyEquipped(int a_iWeaponID)
    {
      for(int _iWIdx = 0; _iWIdx < GameSaveMgr.Instance.CurrentGameSave.BoughtWeaponList.Count; _iWIdx++)
      {
        if(a_iWeaponID == GameSaveMgr.Instance.CurrentGameSave.BoughtWeaponList[_iWIdx])
        {
          return true;
        }
      }

      return false;
    }

    // create a new weapon slot GUI
    // and advance the given slot x position
    // return true on success
    bool createWeaponSlot(int a_iWeaponID, float a_fSlotXPos, int a_iSlotIndex, out GameObject a_pNewWeaponSlotGUI)
    {
      // clear up the returned slot
      a_pNewWeaponSlotGUI = null;

      // current weapon define
      WeaponDefine _pCurWeaponDefine = null;

      // try to get the weapon define of the given weapon
      if(false == WeaponMgr.Instance.getWeaponDefine(a_iWeaponID, out _pCurWeaponDefine))
      {
        DefaultGameLogger.Instance.dumpLogError("[WeaponInventory][createWeaponSlot]: [" + gameObject.ToString() + "]: NOT Weapon Define for Weapon: " + a_iWeaponID);

        return false;
      }

      // try to create a WeaponSlot GUI
      if(GameLevelMgr.Instance.CurrentGameLevel.getAsset(WEAPONSLOT_PREFAB_ID, out a_pNewWeaponSlotGUI) == false)
      {
        DefaultGameLogger.Instance.dumpLogError("[WeaponInventory][createWeaponSlot]: [" + gameObject.ToString() + "]: Can NOT load WeaponSlot PreFab");

        return false;
      }

      // confirm the WeaponSlot GUI has WeaponSlot.cs attached
      if(a_pNewWeaponSlotGUI.GetComponentInChildren< WeaponSlot >() == null)
      {
        DefaultGameLogger.Instance.dumpLogError("[WeaponInventory][createWeaponSlot]: [" + gameObject.ToString() + "]: WeaponSlot PreFab does NOT have WeaponSlot.cs attached");

        return false;
      }

      // position the weapon slot GUI
      // attach onto Content node
      a_pNewWeaponSlotGUI.transform.SetParent(m_pContentNode.transform);

      // set position
      a_pNewWeaponSlotGUI.GetComponent< RectTransform >().anchoredPosition = new Vector2(a_fSlotXPos, 0.0f);

      // initialize the current WeaponSlot GUI with the given working mode and weapon's UID
      a_pNewWeaponSlotGUI.GetComponentInChildren< WeaponSlot >().doInitialize(m_eCurMode, 
                                                                              a_iWeaponID,
                                                                              a_iSlotIndex,
                                                                              m_pContentNode.GetComponentInChildren< ToggleGroup >()
                                                                             );

      // insert into weapon slot list
      m_aSlotList[a_iSlotIndex] = a_pNewWeaponSlotGUI.GetComponentInChildren< WeaponSlot >();

      return true;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // the asset ID of WeaponSlot PreFab
    static int WEAPONSLOT_PREFAB_ID = 392;
    // the x coordinate of first weapon slot GUI
    static float WEAPONSLOT_START_X = 78.0F;
    // the distance between two sequential weapon slot GUIs
    static float WEAPONSLOT_MARGIN = 4.0F;
    // current working mode
    WInventoryWModes m_eCurMode;
    // currently equipped weapon slot index
    int m_iCurEquippedSlotIndex;
    // content node
    GameObject m_pContentNode;
    // slot list
    WeaponSlot[] m_aSlotList;
    #endregion // PRIVATE DATA MEMBERS
  }
} // namespace Game.GUI

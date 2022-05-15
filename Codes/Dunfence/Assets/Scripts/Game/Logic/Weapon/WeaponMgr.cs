/**
 * Weapon Manager:
 * - load weapon define database
 * - get whole weapon define list
 * - get weapon define based on given UID
 * - equip weapon: 
 *   + for ShootWeapon: load its attached projectile ID from ProjectileID property
 *   + mount to its required mount point using the given node: find the following subnode according to mount types
 *     WEAPON_MOUNT_TYPES_LEFT_HAND    : LEHMountPos
 *     WEAPON_MOUNT_TYPES_RIGHT_HAND   : RIHMountPos
 *     WEAPON__MOUNTTYPES_DOUBLE_HAND  : DOHMountPos
 *     WEAPON__MOUNTTYPES_SHOULDER     : SHDMountPos
 *   + return this cached weapon
 * - unEquip weapon:
 *   + unmount the given weapon
 *   + destroy the given weapon
 */

using UnityEngine;
using Framework.Utils;
using Framework.Logger;
using System.Collections.Generic; // List<T>, Dictionary< TKey, TVal >

namespace Game.Logic
{
  // definition of weapon
  public class WeaponDefine
  {
    // common part
    public int UID;
    public int DamagePoint;

    // melee/shoot weapon
    public string IconName;
    public int WeaponID;
    public int ProjectileID;
    public int Price;

    public float ShootRate;

    // projectile
    public float FlySpeed;

    public bool IsShootWeapon;
  }

  public class WeaponMgr : TManager< WeaponMgr >
  {
    #region PUBLIC METHODS
    /**
     * return current weapon define list
     */
    public List< WeaponDefine > CurrentWeaponDefineList
    {
      get
      {
        return m_aWeaponDefineList;
      }
    }

    /**
     * return weapon define for the given UID
     * return true on success
     */
    public bool getWeaponDefine(int a_iUID, out WeaponDefine a_pFoundDefine)
    {
      if(m_mWeaponDefineDataBase.ContainsKey(a_iUID) == false)
      {
        a_pFoundDefine = null;

        return false;
      }

      a_pFoundDefine = m_mWeaponDefineDataBase[a_iUID];

      return true;
    }

    /**
     * equip the given weapon onto a_pTargetObject
     * - clone the given weapon from the current scene cache and put into weapon cache onto the given ID
     * - according to the equipped weapon's mount type, attach to the given mount point of a_pAttachToObject
     * - for ShootWeapon: load its attached projectile ID from ProjectileID property
     * - spawn the given weapon
     * - return the equipped weapon
     */
    public bool equipWeapon(int a_iWeaponID, GameObject a_pAttachToObject, out GameObject a_pEquippedWeapon)
    {
      // store the given mountie object
      m_pCurAttachedObject = a_pAttachToObject;

      // clear the returned equipped weapon reference
      a_pEquippedWeapon = null;

      // try to clone the given weapon
      // can NOT create the given weapon
      if(false == GameLevelMgr.Instance.CurrentGameLevel.getAsset(a_iWeaponID, out m_pCurEquippedWeapon))
      {
        DefaultGameLogger.Instance.dumpLogError("[WeaponMgr][equipWeapon]: can NOT create the given weapon: " + a_iWeaponID);

        return false;
      }

      // try to get the weapon component
      Weapon _pCurWeaponComp = m_pCurEquippedWeapon.GetComponentInChildren< Weapon >();

      // can NOT find the weapon component
      if(null == _pCurWeaponComp)
      {
        DefaultGameLogger.Instance.dumpLogError("[WeaponMgr][equipWeapon]: Weapon[" + a_iWeaponID + "][" + m_pCurEquippedWeapon.ToString() + "] does NOT contain Weapon.cs");

        return false;
      }

      // mount the currently equipped weapon
      // try to find the mount point
      GameObject _pMountPoint = null;
      string _szMountPointName = "";
      switch(_pCurWeaponComp.MountType)
      {
      case WeaponMountTypes.WEAPON_MOUNT_TYPES_LEFT_HAND:
        {
          _szMountPointName = "LEHMountPos";
          _pMountPoint = GameObjectUtils.FindNode(a_pAttachToObject, _szMountPointName);

          break;
        }

      case WeaponMountTypes.WEAPON_MOUNT_TYPES_RIGHT_HAND:
        {
          _szMountPointName = "RIHMountPos";
          _pMountPoint = GameObjectUtils.FindNode(a_pAttachToObject, _szMountPointName);

          break;
        }

      case WeaponMountTypes.WEAPON__MOUNTTYPES_DOUBLE_HAND:
        {
          _szMountPointName = "DOHMountPos";
          _pMountPoint = GameObjectUtils.FindNode(a_pAttachToObject, _szMountPointName);

          break;
        }

      case WeaponMountTypes.WEAPON__MOUNTTYPES_SHOULDER:
        {
          _szMountPointName = "SHDMountPos";
          _pMountPoint = GameObjectUtils.FindNode(a_pAttachToObject, _szMountPointName);

          break;
        }

        default:
        {
          DefaultGameLogger.Instance.dumpLogError("[WeaponMgr][equipWeapon]: Weapon[" + a_iWeaponID + "][" + m_pCurEquippedWeapon.ToString() + "] has Wrong mount type: " + _pCurWeaponComp.MountType);

          return false;
        }
      }

      if(null == _pMountPoint)
      {
        DefaultGameLogger.Instance.dumpLogError("[WeaponMgr][equipWeapon]: [" + a_pAttachToObject.ToString() + "] does NOT contain mount point for mount type:" + _pCurWeaponComp.MountType + ". It needs " + _szMountPointName + "node");

        return false;
      }

      // mount the weapon: weapon's PreFab store all transforms in Local Space
      m_pCurEquippedWeapon.transform.SetParent(_pMountPoint.transform, false); // mount in local space

      // for ShootWeapon load needed projectile
      if(WeaponTypes.WEAPON_TYPES_SHOOTWEAPON == _pCurWeaponComp.WeaponType)
      {
        // equip the given projectile
        if(((ShootWeapon)_pCurWeaponComp).ProjectileID >= 0)
        {
          List< int > _aProjectileList = new List<int>(1);
          _aProjectileList.Add(((ShootWeapon)_pCurWeaponComp).ProjectileID);

          ProjectileMgr.Instance.equipProjectiles(_aProjectileList);
        }
      }

      // set the returned weapon reference
      a_pEquippedWeapon = m_pCurEquippedWeapon;

      // enable this equipped weapon
      m_pCurEquippedWeapon.SetActive(true);

      // spawn the equipped weapon
      _pCurWeaponComp.doSpawnCom(_pMountPoint.transform.position, _pMountPoint.transform.forward, _pMountPoint.transform.up);

      return true;
    }

    /**
     * unEquip the given weapon: 
     * - unmount the given weapon out of the given attached game object
     * - destroy the given weapon
     */
    public bool unEquipWeapon(GameObject a_pEquppedWeapon, GameObject a_pAttachedObject)
    {
      // NO weapon given: return
      if(null == a_pEquppedWeapon)
      {
        DefaultGameLogger.Instance.dumpLogError("[WeaponMgr][unEquipWeapon]: NO weapon given");

        return false;
      }

      // for ShootWeapon
      if(a_pEquppedWeapon.GetComponentInChildren< ShootWeapon >() != null)
      {
        // if there is a mounted projectile: unEquip it
        if(a_pEquppedWeapon.GetComponentInChildren< ShootWeapon >().ProjectileID >= 0)
        {
          ProjectileMgr.Instance.unEquipProjectile(a_pEquppedWeapon.GetComponentInChildren< ShootWeapon >().ProjectileID);
        }
      }

      // unmount the given weapon
      a_pEquppedWeapon.transform.parent = null;

      // clear the internal references
      m_pCurEquippedWeapon = null;
      m_pCurAttachedObject = null;

      // destroy the given weapon
      GameObject.Destroy(a_pEquppedWeapon);

     return true;
    }

    #region PUBLIC OVERRIDDENS
    // the function called before using this manager
    public override void doInitialize()
    {
      // create weapon define database
      m_mWeaponDefineDataBase = new Dictionary<int, WeaponDefine>(26);

      // create weapon define list
      m_aWeaponDefineList = new List<WeaponDefine>(26);

      // load weapon defines
      // and set initialization flag
      m_bIsInitialized =  loadWeaponDataBase();
    }

    // rest this manager
    public override void doReset()
    {
      // unequip the weapon if needed
      if(m_pCurEquippedWeapon != null)
      {
        unEquipWeapon(m_pCurEquippedWeapon, m_pCurAttachedObject);
      }
    }

    // update function
    public override void doUpdate()
    {

    }

    // post update function called after all update() functions had been called
    public override void doPostUpdate()
    {

    }

    // the function called before destroying this manager
    public override void doDestroy()
    {
      // clear initialization flag
      m_bIsInitialized = false;
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // load all weapon defines from WeaponDataBase.json
    // return true on success
    bool loadWeaponDataBase()
    {
      // try to load in WeaponDataBase.json file
      TextAsset _pWeaponDataBaseJsonFile = Resources.Load("WeaponDataBase") as TextAsset; // an TextAsset: the representation of a Textural or Binary File
      if(null == _pWeaponDataBaseJsonFile)
      {
        DefaultGameLogger.Instance.dumpLogError("[WeaponMgr][loadWeaponDataBase] Can NOT find WeaponDataBase.json file");

        return false;
      }

      // create and parse the json data at the same time
      JSONObject _pWeaponDataBaseJson = new JSONObject(_pWeaponDataBaseJsonFile.text);

      // read in WeaponList
      // NO WeaponList block
      if(false == _pWeaponDataBaseJson.HasField("WeaponList"))
      {
        DefaultGameLogger.Instance.dumpLogError("[WeaponMgr][loadWeaponDataBase] Can NOT find WeaponList Block");

        return false;
      }

      // WeaponList block is NOT an array
      if(false == _pWeaponDataBaseJson["WeaponList"].IsArray)
      {
        DefaultGameLogger.Instance.dumpLogError("[WeaponMgr][loadWeaponDataBase] WeaponList Block is NOT an array");

        return false;
      }

      // clear weapon define database
      m_mWeaponDefineDataBase.Clear();

      // clear weapon define list
      m_aWeaponDefineList.Clear();

      // get the WeaponList json block
      JSONObject _pWeaponListJsonBlock = _pWeaponDataBaseJson["WeaponList"];

      // read in all weapon defines
      for(int _iWIdx = 0; _iWIdx < _pWeaponListJsonBlock.Count; _iWIdx++)
      {
        // get the current weapon define json block
        JSONObject _pCurWeaponJsonBlock = _pWeaponListJsonBlock[_iWIdx];

        // NO UID field
        if(false == _pCurWeaponJsonBlock.HasField("UID"))
        {
          DefaultGameLogger.Instance.dumpLogError("[WeaponMgr][loadWeaponDataBase] Weapon Define[" + _iWIdx + "] does NOT have UID field");

          continue;
        }

        if(false == _pCurWeaponJsonBlock["UID"].IsNumber)
        {
          DefaultGameLogger.Instance.dumpLogError("[WeaponMgr][loadWeaponDataBase] Weapon Define[" + _iWIdx + "]'s UID field is NOT a number");

          continue;
        }

        // NO IsShootWeapon field
        if(false == _pCurWeaponJsonBlock.HasField("IsShootWeapon"))
        {
          DefaultGameLogger.Instance.dumpLogError("[WeaponMgr][loadWeaponDataBase] Weapon Define[" + _iWIdx + "] does NOT have IsShootWeapon field");

          continue;
        }

        if(false == _pCurWeaponJsonBlock["IsShootWeapon"].IsBool)
        {
          DefaultGameLogger.Instance.dumpLogError("[WeaponMgr][loadWeaponDataBase] Weapon Define[" + _iWIdx + "]'s IsShootWeapon field is NOT a boolean");

          continue;
        }

        // get the UID: key of the database
        int _iCurUID = (int)_pCurWeaponJsonBlock["UID"].i;

        // create a new weapon define
        WeaponDefine _pCurWeaponDefine = new WeaponDefine();

        // store UID
        _pCurWeaponDefine.UID = _iCurUID;

        // get IsShootWeapon
        _pCurWeaponDefine.IsShootWeapon = _pCurWeaponJsonBlock["IsShootWeapon"].b;

        // has IconName field
        if(true == _pCurWeaponJsonBlock.HasField("IconName")
           &&
           true == _pCurWeaponJsonBlock["IconName"].IsString
          )
        {
          _pCurWeaponDefine.IconName = _pCurWeaponJsonBlock["IconName"].str;
        }
        else
        {
          _pCurWeaponDefine.IconName = "";
        }

        // has WeaponID field
        if(true == _pCurWeaponJsonBlock.HasField("WeaponID")
           &&
           true == _pCurWeaponJsonBlock["WeaponID"].IsNumber
          )
        {
          _pCurWeaponDefine.WeaponID = (int)_pCurWeaponJsonBlock["WeaponID"].i;
        }
        else
        {
          _pCurWeaponDefine.WeaponID = -1;
        }

        // has ProjectileID field
        if(true == _pCurWeaponJsonBlock.HasField("ProjectileID")
           &&
           true == _pCurWeaponJsonBlock["ProjectileID"].IsNumber
          )
        {
          _pCurWeaponDefine.ProjectileID = (int)_pCurWeaponJsonBlock["ProjectileID"].i;
        }
        else
        {
          _pCurWeaponDefine.ProjectileID = -1;
        }

        // has Price field
        if(true == _pCurWeaponJsonBlock.HasField("Price")
           &&
           true == _pCurWeaponJsonBlock["Price"].IsNumber
          )
        {
          _pCurWeaponDefine.Price = (int)_pCurWeaponJsonBlock["Price"].i;
        }
        else
        {
          _pCurWeaponDefine.Price = 0;
        }

        // has Damage field
        if(true == _pCurWeaponJsonBlock.HasField("Damage")
           &&
           true == _pCurWeaponJsonBlock["Damage"].IsNumber
          )
        {
          _pCurWeaponDefine.DamagePoint = (int)_pCurWeaponJsonBlock["Damage"].i;
        }
        else
        {
          _pCurWeaponDefine.DamagePoint = 0;
        }

        // has ShootRate field
        if(true == _pCurWeaponJsonBlock.HasField("ShootRate")
           &&
           true == _pCurWeaponJsonBlock["ShootRate"].IsNumber
          )
        {
          _pCurWeaponDefine.ShootRate = _pCurWeaponJsonBlock["ShootRate"].f;
        }
        else
        {
          _pCurWeaponDefine.ShootRate = 0.0f;
        }

        // has FlySpeed field
        if(true == _pCurWeaponJsonBlock.HasField("FlySpeed")
           &&
           true == _pCurWeaponJsonBlock["FlySpeed"].IsNumber
          )
        {
          _pCurWeaponDefine.FlySpeed = _pCurWeaponJsonBlock["FlySpeed"].f;
        }
        else
        {
          _pCurWeaponDefine.FlySpeed = 0.0f;
        }

        // insert this define into database
        m_mWeaponDefineDataBase.Add(_iCurUID, _pCurWeaponDefine);

        // insert this define into list
        m_aWeaponDefineList.Add(_pCurWeaponDefine);
      }

      // done
      return true;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // currently equipped weapon
    GameObject m_pCurEquippedWeapon;
    // currently attached object of that equipped weapon
    GameObject m_pCurAttachedObject;
    // database for weapon define: key is UID
    Dictionary< int, WeaponDefine > m_mWeaponDefineDataBase;
    // list of all weapon defines: stored in the order as in WeaponDataBase.json file
    List< WeaponDefine > m_aWeaponDefineList;
    #endregion // PRIVATE DATA MEMBERS

  }

} // namespace Game.Logic

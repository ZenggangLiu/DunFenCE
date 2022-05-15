/**
 * Projectile Manager:
 * - equip the given projectile: cache and mount the given projectile onto ShootOut node
 * - shoot projectile: get a free cached projectile and use it
 * - destroy projectile: put the given projectile into the cache
 */

using UnityEngine;
using Framework.Utils;
using Framework.Logger;
using System.Collections.Generic; // List< T >

namespace Game.Logic
{
  public class ProjectileMgr : TManager < ProjectileMgr >
  {
    #region PUBLIC METHODS
    /**
     * equip all given projectiles and mount them under ShootOuts node
     */
    public void equipProjectiles(List< int > a_aProjectileIDList)
    {
      // if nothing to do
      if(0 == a_aProjectileIDList.Count)
      {
        DefaultGameLogger.Instance.dumpLogError("[ProjectileMgr][equipProjectiles]: called with empty projectiile ID list");

        return;
      }

      // create predefined count of projectiles of each kind
      for(int _iProjIDIdx = 0; _iProjIDIdx < a_aProjectileIDList.Count; _iProjIDIdx++)
      {
        // if the given projectile type DOES exist: go to the next projectile
        if(m_mProjectileCache.ContainsKey(a_aProjectileIDList[_iProjIDIdx]) == true)
        {
          // if the current projectile will be unequipped, but still NOT finishes usage
          if(false == m_mProjectileCache[a_aProjectileIDList[_iProjIDIdx]].IsEquipped)
          {
            // change it back to already equipped
            m_mProjectileCache[a_aProjectileIDList[_iProjIDIdx]].IsEquipped = true;
          }

          continue;
        }

        // create a new list of projectiles
        ProjectileList _aProjectileList = new ProjectileList();

        // clone the given projectile using its ID
        for(int _iNonUsedProjIdx = 0; _iNonUsedProjIdx < INITIAL_PROJECTILE_INSTANCE_COUNT; _iNonUsedProjIdx++)
        {
          GameObject _pClonedProjectileGO = null;

          if(false ==  createProjectile(a_aProjectileIDList[_iProjIDIdx], out _pClonedProjectileGO))
          {
            DefaultGameLogger.Instance.dumpLogError("Can NOT create projectile[" + a_aProjectileIDList[_iProjIDIdx] + "]");

            return;
          }

          // put the new cloned projectile onto non-used projectile list
          _aProjectileList.NonUsedProjectileList.Add(_pClonedProjectileGO);
        }

        // insert the list of projectiles onto projectile cache
        m_mProjectileCache[a_aProjectileIDList[_iProjIDIdx]] = _aProjectileList;
      }
    }

    /**
     * shoot out the given projectile using its ID from the given a_vShootPos position, following a_vShootDir direction with up pointing to a_vShootUp
     * return true on success
     */
    public bool shootProjectile(int a_iProjectileID, CharacterTypes a_eOwnerType, Vector3 a_vShootPos, Vector3 a_vShootDir, Vector3 a_vShootUp, List< GameObject > a_aShotProjectileList)
    {
      // try to find the projectile in cache
      if(false == m_mProjectileCache.ContainsKey(a_iProjectileID))
      {
        DefaultGameLogger.Instance.dumpLogError("[ProjectileMgr][shootProjectile]: can NOT shoot out the given projectile[" + a_iProjectileID + "]. Projectile cache does NOT contain this projectile. Call equipProjectiles() before calling shootProjectile()");

        return false;
      }

      // get one non-used projectile from cache
      GameObject _pProjectileGO = getFreeProjectileFromCache(a_iProjectileID);

      // if we need to return the shot out projectile
      if(null != a_aShotProjectileList)
      {
        a_aShotProjectileList.Add(_pProjectileGO);
      }

      // get the Projectile.cs component
      Projectile _pProjectileComp = _pProjectileGO.GetComponent< Projectile >();

      // set the owner type before enabling this projectile
      _pProjectileComp.setOwnerType(a_eOwnerType);

      // spawn this projectile: aligned with shoot pos' frame
      _pProjectileComp.doSpawnCom(a_vShootPos, a_vShootDir, a_vShootUp);

      // use this weapon
      _pProjectileComp.doUseCom();

      return true;
    }

    /**
     * destroy the given projectile
     * - put it back into NON-Used Projectile List
     * - inactivate it
     */
    public void destroyProjectile(GameObject a_pProjectileInstance)
    {
      // current projectile list
      ProjectileList _aCurProjectileList = null;
      // the projectile ID for the given projectile instance
      int _iFoundProjectileID = -1;

      // iterate over all projectile IDs
      foreach(int _iCurProjectileID in m_mProjectileCache.Keys)
      {
        // get the current projectile list
        _aCurProjectileList = m_mProjectileCache[_iCurProjectileID];

        // try to find the index of the given projectile instance in the current projectile list
        int _iSlotIndex = _aCurProjectileList.UsedProjectileList.IndexOf(a_pProjectileInstance);

        // if we can NOT find the given projectile: go to the next projectile list
        if(_iSlotIndex < 0)
        {
          continue;
        }

        // found the given projectile
        // assign the ID
        _iFoundProjectileID = _iCurProjectileID;

        // disable the given projectile
        a_pProjectileInstance.SetActive(false);

        // insert into non-used/free projectile instance list
        _aCurProjectileList.NonUsedProjectileList.Add(a_pProjectileInstance);

        // remove the given projectile out of used projectile instance list
        _aCurProjectileList.UsedProjectileList.RemoveAt(_iSlotIndex);

        break;
      }

      // have NOT found the given projectile
      if(-1 == _iFoundProjectileID)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + a_pProjectileInstance.ToString() + "]: can NOT destroy the given Projectile");
        
        return;
      }

      if(false == _aCurProjectileList.IsEquipped             // if this projectile has been unEquipped
         &&                                                  // and
         0 == _aCurProjectileList.UsedProjectileList.Count   // NO projectile still in usage
        )
      {
        // iterate over all freed projectile instances
        for(int _iProjIDIdx = 0; _iProjIDIdx < _aCurProjectileList.NonUsedProjectileList.Count; _iProjIDIdx++)
        {
          // remove the current free projectile from ShootOuts node
          _aCurProjectileList.NonUsedProjectileList[_iProjIDIdx].transform.parent = null;

          // destroy the current free projectile
          GameObject.Destroy(_aCurProjectileList.NonUsedProjectileList[_iProjIDIdx]);
        }

        // release the free projectile list 
        _aCurProjectileList.NonUsedProjectileList.Clear();
        _aCurProjectileList.NonUsedProjectileList = null;

        // remove this projectile out of cache
        m_mProjectileCache.Remove(_iFoundProjectileID);
      }
    }

    /**
     * unEquip the given projectil and remove them out of ShootOuts node
     */
    public void unEquipProjectile(int a_iProjectileID)
    {
      // if the given projectile has NOT been already equipped: return
      if(m_mProjectileCache.ContainsKey(a_iProjectileID) == false)
      {
        return;
      }

      // set unequipped flag
      m_mProjectileCache[a_iProjectileID].IsEquipped = false;

      // if there is still some projectile in usage: return
      if(m_mProjectileCache[a_iProjectileID].UsedProjectileList.Count > 0)
      {
        return;
      }

      // otherwise, iterate over all freed projectile instances
      for(int _iProjIDIdx = 0; _iProjIDIdx < m_mProjectileCache[a_iProjectileID].NonUsedProjectileList.Count; _iProjIDIdx++)
      {
        // remove the current free projectile from ShootOuts node
        m_mProjectileCache[a_iProjectileID].NonUsedProjectileList[_iProjIDIdx].transform.parent = null;

        // destroy the current free projectile
        GameObject.Destroy(m_mProjectileCache[a_iProjectileID].NonUsedProjectileList[_iProjIDIdx]);
      }

      // release the free projectile list 
      m_mProjectileCache[a_iProjectileID].NonUsedProjectileList.Clear();
      m_mProjectileCache[a_iProjectileID].NonUsedProjectileList = null;

      // remove this projectile out of cache
      m_mProjectileCache.Remove(a_iProjectileID);
    }

    #region PUBLIC OVERRIDDENS
    // the function called before using this manager
    public override void doInitialize()
    {
      // create the projectile cache
      m_mProjectileCache = new Dictionary< int, ProjectileList >(INITIAL_PROJECTILE_TYPE_COUNT);
    }

    // rest this manager
    public override void doReset()
    {
      // collect all equipped projectils
      List< int > _aEquippedProjectilList = new List<int>(5);

      foreach(int _iCurProjectileID in m_mProjectileCache.Keys)
      {
        _aEquippedProjectilList.Add(_iCurProjectileID);  
      }

      // unequip all projectils
      for(int _iPIdx = 0; _iPIdx < _aEquippedProjectilList.Count; _iPIdx++)
      {
        unEquipProjectile(_aEquippedProjectilList[_iPIdx]);
      }

      _aEquippedProjectilList.Clear();
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
      m_mProjectileCache.Clear();
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS

    /**
     * return a free projectile instance from projectile cache using the given projectile ID
     */
    GameObject getFreeProjectileFromCache(int a_iProjectileID)
    {
      // can NOT find the given projectile in the cache
      if(false == m_mProjectileCache.ContainsKey(a_iProjectileID))
      {
        return null;
      }

      // get the projectile list of the given projectile
      ProjectileList _aFoundProjectileList = m_mProjectileCache[a_iProjectileID];

      // the found non-used/free projectile instance
      GameObject _pFreeProjectile = null;

      // check if there is some non-used/free projectile instance
      if(_aFoundProjectileList.NonUsedProjectileList.Count != 0)
      {
        // YES
        // get the head item
        _pFreeProjectile = _aFoundProjectileList.NonUsedProjectileList[0];

        // remove the head item out of non-used/free projectile instance list
        _aFoundProjectileList.NonUsedProjectileList.RemoveAt(0);
      }
      else
      {
        // NO non-used/free projectile instance
        // create a new non-used/free projectile instance
        createProjectile(a_iProjectileID, out _pFreeProjectile);

        // if we can NOT create a new non-used/free projectile instance
        if(null == _pFreeProjectile)
        {
          return null;
        }

        // enable this projectile
        _pFreeProjectile.SetActive(true);
      }

      // insert into used projectile instance list
      _aFoundProjectileList.UsedProjectileList.Add(_pFreeProjectile);

      // return the found non-used/free projectile instance
      return _pFreeProjectile;
    }

    /**
     * create the given projectile using its ID
     * mount it onto ShootOuts node of the current SceneNode from MainScene.unity
     * return true on success
     */
    bool createProjectile(int a_iProjectileID, out GameObject a_pCreatedProjectile)
    {
      // try to clone the given projectile
      if(false == GameLevelMgr.Instance.CurrentGameLevel.getAsset(a_iProjectileID, out a_pCreatedProjectile))
      {
        return false;
      }

      // check if the currently cloned projectile has Projectile.cs
      Projectile _pProjectileComp = a_pCreatedProjectile.GetComponent< Projectile >();

      // if there is NO Projectile.cs
      if(null == _pProjectileComp)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + a_pCreatedProjectile.ToString() + "]: is NOT a Projectile. It does NOT contain Projectile.cs");

        return false;
      }

      // mount it onto ShootOuts node
      a_pCreatedProjectile.transform.parent = GameLevelMgr.Instance.GameMainLevel.ShootOutsNode.transform;

      return true;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // initial projectile type count
    static int INITIAL_PROJECTILE_TYPE_COUNT = 3;
    // initial count of projectile instances of same type
    static int INITIAL_PROJECTILE_INSTANCE_COUNT = 10;

    // projectile list: ONE free projectile list + ONE shot projectile list
    class ProjectileList
    {
      // default constructor
      public ProjectileList()
      {
        // create the shot projectile list
        UsedProjectileList    = new List< GameObject >(INITIAL_PROJECTILE_INSTANCE_COUNT);

        // create the free projectile list
        NonUsedProjectileList = new List< GameObject >(INITIAL_PROJECTILE_INSTANCE_COUNT);

        // mark it as equipped
        IsEquipped = true;
      }

      // the list of all currently used/shot projectiles
      public List< GameObject > UsedProjectileList;

      // the list of all already released/free projectiles
      public List< GameObject > NonUsedProjectileList;

      // equipped flag
      public bool IsEquipped;
    }

    // current projectile cache
    Dictionary< int, ProjectileList > m_mProjectileCache; // < Projectile ID, ProjectileList >
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Logic

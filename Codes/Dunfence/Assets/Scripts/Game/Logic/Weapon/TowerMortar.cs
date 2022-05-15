/**
 * Mortar Gun of a Tower Defense Building
 * - provides an automatic shooting aiming around Y axis
 */

using UnityEngine;
using Framework.Utils;
using Framework.Logger;
using System.Collections.Generic; // List< T >

namespace Game.Logic
{
  public class TowerMortar : ShootWeapon
  {
    #region PUBLIC DATA MEMBERS
    // the head part of this mortar: by null, code will try to find "HeadPart" child node
    public GameObject MortarHead = null;
    // the base part of this mortar: by null, code will try to find "BasePart" child node
    public GameObject MortarBase = null;
    #endregion // PUBLIC DATA MEMBERS

    #region PROTECTED METHODS

    #region PROTECTED OVERRIDDENS
    // initialize this entity: called within doSpawnCom() only
    // m_bIsInitialized will be SET in this method
    protected override void doInitialize()
    {
      base.doInitialize();

      // clear initialization flag before checking
      m_bIsInitialized = false;

      // try to find the mortar base
      if(null == MortarBase)
      {
        MortarBase = GameObjectUtils.FindNode(gameObject, "BasePart");
        if(null == MortarBase)
        {
          DefaultGameLogger.Instance.dumpLogError("[TowerMortar][doInitialize][" + gameObject.ToString() + "]: No Base Part");

          return;
        }
      }

      // try to find the mortar head
      if(null == MortarHead)
      {
        MortarHead = GameObjectUtils.FindNode(gameObject, "HeadPart");
        if(null == MortarHead)
        {
          DefaultGameLogger.Instance.dumpLogError("[TowerMortar][doInitialize][" + gameObject.ToString() + "]: No Head Part");

          return;
        }
      }

      // set initialization flag
      m_bIsInitialized = true;
    }

    /**
     * doUpdate() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last doUpdate() call
     * based on the current state do processing
     * check false == m_bIsInitialized before doing the real thing
     */
    protected override void doUpdate()
    {
      // NOT initialized: return
      if(false == m_bIsInitialized)
      {
        return;
      }

      base.doUpdate();

      // if the weapon is in usage: aim the weapon
      if(true == isInUse)
      {
        aimWeapon();
      }
    }

    /**
     * shoot out projectiles according to ConsumedProjectilePerShoot
     */
    protected override void shootProjectile(List< GameObject > a_aShotProjectileList = null)
    {
      // if there is NO amied entity: try to aim the weapon
      if(null == m_pCurAimedEntity)
      {
        aimWeapon();
      }

      // shoot out the shell
      List< GameObject > _aShotProjectileList = new List<GameObject>(1);
      base.shootProjectile(_aShotProjectileList);

      // set shell's shoot target
      if(_aShotProjectileList[0].GetComponentInChildren< MortarShell >() == null)
      {
        DefaultGameLogger.Instance.dumpLogError("[TowerMortar][shootProjectile][" + gameObject.ToString() + "][shootProjectile] trying to shoot out a projectile: " + ProjectileID + ". It is NOT Mortar Shell");
      }
      else
      {
        _aShotProjectileList[0].GetComponentInChildren< MortarShell >().setShootTarget(m_pCurAimedEntity);
      }
    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS

    #region PRIVATE METHODS
    /**
     * return the aimed position
     * return true on success
     */
    bool getAimedPos(out Vector3 a_vAimedPos)
    {
      // clear up the aimed entity
      m_pCurAimedEntity = null;

      // clear up returned aimed position
      a_vAimedPos = Vector3.zero;

      // the nearest monster
      GameObject _pNearestCharacter = null;
      // the currently minimal distance squared to the nearest monster
      float _fCurMinDistSqr = 100.0f;
      // the current distance squared to some monster
      float _fCurDistSqr = 0.0f;

      // get current monster list
      List< GameObject > _aMonsterList = MonsterMgr.Instance.CurrentMonsterList;

      // get current boss list
      List< GameObject > _aBossList = MonsterMgr.Instance.CurrentBossList;

      // iterater over all monsters
      for(int _iIdx = 0; _iIdx < _aMonsterList.Count; _iIdx++)
      {
        // if the current monster can NOT handle hit: we try to shoot another one
        if(_aMonsterList[_iIdx].GetComponentInChildren< Character >().canHandleHit() == false)
        {
          continue;
        }

        // if the current monster staying closer
        if((_fCurDistSqr = (transform.position - _aMonsterList[_iIdx].transform.position).sqrMagnitude) < _fCurMinDistSqr)
        {
          // store this monster
          _pNearestCharacter = _aMonsterList[_iIdx];

          // store its distance squared
          _fCurMinDistSqr = _fCurDistSqr;
        }
      }

      // iterater over all bosses
      for(int _iIdx = 0; _iIdx < _aBossList.Count; _iIdx++)
      {
        // if the current boss can NOT handle hit: we try to shoot another one
        if(_aBossList[_iIdx].GetComponentInChildren< Character >().canHandleHit() == false)
        {
          continue;
        }

        // if the current boss staying closer
        if((_fCurDistSqr = (transform.position - _aBossList[_iIdx].transform.position).sqrMagnitude) < _fCurMinDistSqr)
        {
          // store this monster
          _pNearestCharacter = _aBossList[_iIdx];

          // store its distance squared
          _fCurMinDistSqr = _fCurDistSqr;
        }
      }

      // NOT found any monster: return false
      if(null == _pNearestCharacter)
      {
        return false;
      }

      // otherwise, store the center location of the shoot target
      // store the aimed entity
      m_pCurAimedEntity = _pNearestCharacter.GetComponentInChildren< Entity >();

      // NO Render component, use its world location: maybe Foot Point
      if(_pNearestCharacter.GetComponentInChildren< Renderer >() == null)
      {
        DefaultGameLogger.Instance.dumpLogError("[TowerMortar][getAimedPos][" + _pNearestCharacter.ToString() + "]: has NO Render Component");

        a_vAimedPos = _pNearestCharacter.transform.position;
      }
      else
      {
        a_vAimedPos = _pNearestCharacter.GetComponentInChildren< Renderer >().bounds.center;  
      }

      return true;
    }

    /**
     * aim this weapon: use the stored shoot aim helper to get a target position
     */
    void aimWeapon()
    {
      // try to get the shoot target
      Vector3 _vShootTargetPos;

      // NOT found any monster: just return
      if(getAimedPos(out _vShootTargetPos) == false)
      {
        return;
      }

      // orientate the mortar base: JUST Y rotation
      // get the expected forward vector for mortar base
      Vector3 _vExpectedForward = _vShootTargetPos - MortarBase.transform.position;

      // JUST for special case: Defense Tow has been placed vertically
      _vExpectedForward.y = 0.0f; // TODO: calculate the expected forward vector in general case: may use the local space, because the current Z value is zero

      // rotate the mortar base
      MortarBase.transform.forward = _vExpectedForward;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // currently aimed entity
    Entity m_pCurAimedEntity;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Logic

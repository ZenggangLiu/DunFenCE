/**
 * Effect Manager:
 * - create a world space effect using the given ID and position
 */

using UnityEngine;
using Framework.Utils;
using Framework.Logger;
using System.Collections.Generic;

namespace Game.Logic
{
  public class EffectMgr : TManager< EffectMgr >
  {
    #region PUBLIC METHODS
    /**
     * preload the given world space effect
     * return true on success
     */
    public bool preloadWorldEffect(int a_iEffectID)
    {
      // already loaded: return
      if(true == m_mWorldEffectCache.ContainsKey(a_iEffectID))
      {
        return true;
      }

      // otherwise, try to load this world space effect
      GameObject _pWorldEffectNode = null;

      createEffect(a_iEffectID, out _pWorldEffectNode);

      // NOT succeeded
      if(null == _pWorldEffectNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[EffectMgr][preloadEffect]: Can NOT load the given world space effect[" + a_iEffectID + "]");

        return false;
      }

      // effect node loaded
      // check if it is a partical effect
      ParticleSystem[] _aEmitterList = _pWorldEffectNode.GetComponentsInChildren< ParticleSystem >();

      // NOT a particle effect
      if(null == _aEmitterList
         ||
         0 == _aEmitterList.Length
        )
      {
        DefaultGameLogger.Instance.dumpLogError("[EffectMgr][preloadEffect]: [" + _pWorldEffectNode.ToString() + "] is NOT a Particle Effect.");

        return false;
      }

      // multiple emitter within one particle effect: NOT supported
      // TODO: support this feature
      if(1 != _aEmitterList.Length)
      {
#if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("[EffectMgr][preloadEffect]: [" + _pWorldEffectNode.ToString() + "] has MULTIPLE emitter: " + _aEmitterList.Length + ". NO support");
#endif
      }

      // check if the simulation space of this particle system: MUST be world space
      if(_aEmitterList[0].simulationSpace != ParticleSystemSimulationSpace.World)
      {
        DefaultGameLogger.Instance.dumpLogError("[EffectMgr][preloadEffect]: [" + _pWorldEffectNode.ToString() + "]'s simulation space is NOT world space! changed by code");

        _aEmitterList[0].simulationSpace = ParticleSystemSimulationSpace.World;
      }

      // disable play on awake setting
      _aEmitterList[0].playOnAwake = false;

      // insert into cache
      m_mWorldEffectCache[a_iEffectID] = _aEmitterList[0];

      return true;
    }

    /**
     * spawn the given effect and place it in the given world position
     */
    public void spawnEffect(int a_iEffectID, Vector3 a_vSpawnPosWs)
    {
      // NOT preloaded: preload it
      if(false == m_mWorldEffectCache.ContainsKey(a_iEffectID))
      {
#if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("[EffectMgr][spawnEffect]: spawn a NON-preloaded particle effect[" + a_iEffectID + "]");
#endif

        // can NOT preload the given effect
        if(preloadWorldEffect(a_iEffectID) == false)
        {
          return;
        }
      }

      // spawn the effect
      // set the spawn position at first, otherwise, PS will use previous position within Emit()
      m_mWorldEffectCache[a_iEffectID].transform.position = a_vSpawnPosWs;

      // if this effect is NOT activated
      if(false == m_mWorldEffectCache[a_iEffectID].gameObject.activeSelf)
      {
        // activate this effect node: otherwise, we can NOT Play() it
        m_mWorldEffectCache[a_iEffectID].gameObject.SetActive(true);

        // set it to play mode
        m_mWorldEffectCache[a_iEffectID].Play();
      }

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[EffectMgr][spawnEffect]: #Particle: " + m_mWorldEffectCache[a_iEffectID].particleCount);
#endif

      // create the emit parameters
      ParticleSystem.EmitParams _pEmitParams = new ParticleSystem.EmitParams();
      _pEmitParams.applyShapeToPosition = true;
      _pEmitParams.position = a_vSpawnPosWs;

      // emit particles in the maximal count
      m_mWorldEffectCache[a_iEffectID].Emit(_pEmitParams, m_mWorldEffectCache[a_iEffectID].maxParticles);

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[EffectMgr][spawnEffect]: #Particle: --> " + m_mWorldEffectCache[a_iEffectID].particleCount);
#endif
    }

    #region PUBLIC OVERRIDDENS
    /**
     * the function called before using this manager
     */
    public override void doInitialize()
    {
      // create the effect cache
      m_mWorldEffectCache = new Dictionary< int, ParticleSystem >(INITIAL_WORLDEFFECT_TYPE_COUNT);

      // set initialization flag
      m_bIsInitialized = true;
    }

    /**
     * rest this manager
     */
    public override void doReset()
    {
      // unmount all effects out of SceneRoot's Effects node
      foreach(int _iCurEffectID in m_mWorldEffectCache.Keys)
      {
        // stop the current particle effect
        m_mWorldEffectCache[_iCurEffectID].Pause();

        // remove the current effect node out of Effects node of the current SceneNode
        m_mWorldEffectCache[_iCurEffectID].transform.parent = null;

        // destroy the current effect node
        GameObject.Destroy(m_mWorldEffectCache[_iCurEffectID].gameObject);
      }

      // clear up the effect cache
      m_mWorldEffectCache.Clear();
    }

    /**
     * the function called before destroying this manager
     */
    public override void doDestroy()
    {
      // release all effects
      doReset();

      // clear initialization flag
      m_bIsInitialized = false;
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // create the given effect using its ID
    // mount it onto Effects node of the current SceneNode
    // return true on success
    bool createEffect(int a_iEffectID, out GameObject a_pCreatedEffect)
    {
      // try to clone the given effect
      if(false == GameLevelMgr.Instance.CurrentGameLevel.getAsset(a_iEffectID, out a_pCreatedEffect))
      {
        return false;
      }

      // mount it onto Effects node
      a_pCreatedEffect.transform.parent = GameLevelMgr.Instance.GameMainLevel.EffectsNode.transform;

      return true;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // initial world space effect type count
    static int INITIAL_WORLDEFFECT_TYPE_COUNT = 6;
    // world space particle effect cache
    Dictionary< int, ParticleSystem > m_mWorldEffectCache; // < Effect ID, ParticleSystem >
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Logic

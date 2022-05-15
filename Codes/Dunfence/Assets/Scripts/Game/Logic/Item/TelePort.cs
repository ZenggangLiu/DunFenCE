using UnityEngine;

namespace Game.Logic
{
  public class TelePort : Building
  {
    #region PUBLIC DATA MEMBERS
    // life time
    public float LifeTime = 10.0f;
    #endregion // PUBLIC DATA MEMBERS


    #region PUBLIC METHODS

    #region PUBLIC OVERRIDDENS
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS

    #region PROTECTED OVERRIDDENS
    /**
     * doUpdate() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last doUpdate() call
     * check false == m_bIsInitialized before doing the real thing
     */
    protected override void doUpdate()
    {
      // NOT initialized: return
      if(false == m_bIsInitialized)
      {
        return;
      }

      switch(m_eCurEntityState)
      {
      case EntityStates.ENTITY_STATES_SPAWNING:
        {
          // count down the life time
          m_fCurLifeTime -= Time.deltaTime;

          // done with the spawn
          if(m_fCurLifeTime < 1.0e-5f)
          {
            // set state to dead
            m_eCurEntityState = EntityStates.ENTITY_STATES_DEAD;

            // detach it out of MonsterBuildings node
            transform.parent = null;

            // destroy this game object
            GameObject.Destroy(gameObject);
          }
          break;
        }

        default:
        {
          break;
        }
      }
    }

    // called after doSpawnCom() has done
    protected override void onSpawnComDone()
    {
      base.onSpawnComDone();

      // set up life time
      m_fCurLifeTime = LifeTime;
    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    // current life time
    protected float m_fCurLifeTime;
    #endregion // PROTECTED DATA MEMBERS
  }
} // namespace Game.Logic

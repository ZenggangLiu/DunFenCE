/**
 * Defense Building of Player
 */

using UnityEngine;
using Framework.Logger;
using Framework.Utils;

namespace Game.Logic
{
  // defense tower type
  public enum TowerTypes
  {
    TOWER_TYPES_UNKNOWN = 0,
    TOWER_TYPES_SPOTLIGHT_TOWER,   // projector tower
    TOWER_TYPES_CROSSBOW_TOWER,    // cross bow tower
    TOWER_TYPES_MORTAR_TOWER,      // mortar tower
    TOWER_TYPES_CANNON_TOWER,      // cannon tower
    TOWER_TYPES_MISSILE_TOWER,     // missile launcher tower/launcher
    TOWER_TYPES_POWER_TOWER,       // lighten tower/tesla
  }

  // defense tower service state
  public enum TowerServiceStates
  {
    TOWER_SERVICE_STATES_IDLE,
    TOWER_SERVICE_STATES_IN_SERVICE,
  }

  public class DefenseBuilding : Building
  {
    #region PUBLIC DATA MEMBERS
    // tower type
    public TowerTypes TowerType = TowerTypes.TOWER_TYPES_UNKNOWN;
    // required reward points: >= 0
    public int RPRequired = -1;
    // total service time in seconds
    public float ServiceTime = 10.0f;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    /**
     * return current service state
     */
    public TowerServiceStates CurrentState
    {
      get
      {
        return m_eCurState;
      }
    }

    #region PUBLIC OVERRIDDENS
    // commands
    // start defending
    public virtual void doDefendCom()
    {
      // deduce the required RP
      // NO enough RP: return
      if(EconomyMgr.Instance.deductRP(RPRequired) == false)
      {
        return;
      }

      // try to find monster: if there is some monster: try to shoot
      if(m_pEquippedWeapon != null                                                                       // there is an equipped weapon
         &&                                                                                              // and
         (MonsterMgr.Instance.isAnyMonsterCanHandleHit() || MonsterMgr.Instance.isAnyBossCanHandleHit()) // there is any monster or any boss
        )
      {
        // use the equipped weapon
        m_pEquippedWeapon.doUseCom();
      }

      // show timer GUI
      m_pTimerBar.showIt(ServiceTime, m_pTimerBarMountPoint);

      // start the service count down timer
      m_fCurServiceTime = ServiceTime;

      // change to service state
      m_eCurState = TowerServiceStates.TOWER_SERVICE_STATES_IN_SERVICE;
    }

    // animation related commands
    /**
     * Dead: destroy this entity
     */
    public override void doDeadCom()
    {
      // already in dead state: return
      if(EntityStates.ENTITY_STATES_DEAD == m_eCurEntityState)
      {
        return;
      }

      base.doDeadCom();

      // play the dead animation
      playAnimationTrigger(DeadAnimations);

      // propagate this event and show explosion and blaze

      // hide the timer bar
      m_pTimerBar.hideIt();

      // let timer bar destroy its internal members
      m_pTimerBar.doDestroy();

      // remove it out of game GUI
      GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.removeTowerTimerBar(m_pTimerBar.gameObject);

      // destroy timer bar UI
      GameObject.Destroy(m_pTimerBar.gameObject);

      // reset the reference
      m_pTimerBar = null;

      // change to dead state
      m_eCurEntityState = EntityStates.ENTITY_STATES_DEAD;
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS

    #region PROTECTED OVERRIDDENS
    // initialize this entity: called within doSpawnCom() only
    // m_bIsInitialized will be SET in this method
    protected override void doInitialize()
    {
      base.doInitialize();

      // clear initialization flag before checking
      m_bIsInitialized = false;

      // reset all data members
      m_pEquippedWeapon = null;
      m_fCurServiceTime = 0.0f;
      m_eCurState = TowerServiceStates.TOWER_SERVICE_STATES_IDLE;

      // check tower type
      if(TowerTypes.TOWER_TYPES_UNKNOWN == TowerType)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseBuilding][doStart][" + gameObject.ToString() + "]: No Tower Type");

        return;
      }

      // check reward points required: >= 0
      if(RPRequired < 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseBuilding][doStart][" + gameObject.ToString() + "]: No Reward Points Required given");

        return;
      }

      // try to find timer bar' mount point
      m_pTimerBarMountPoint = GameObjectUtils.FindNode(gameObject, "TimerBar");
      if(null == m_pTimerBarMountPoint)
      {
        DefaultGameLogger.Instance.dumpLogError("[DefenseBuilding][doStart][" + gameObject.ToString() + "]: No TimerBar node");

        return;
      }

      // try to find the equipped weapon
      m_pEquippedWeapon = GetComponentInChildren< Weapon >();
      // if NO weapon equipped
      if(null == m_pEquippedWeapon)
      {
        DefaultGameLogger.Instance.dumpLogWarning("[DefenseBuilding][doStart][" + gameObject.ToString() + "]: No Equipped Weapon");
      }

      if(m_pEquippedWeapon != null                                                                             // some weapon equipped
         &&                                                                                                    // and
         WeaponTypes.WEAPON_TYPES_SHOOTWEAPON == m_pEquippedWeapon.WeaponType                                  // for ShootWeapon
         &&                                                                                                    // and
         ((ShootWeapon)m_pEquippedWeapon).ProjectileID > 0                                                     // there is some projectile
        )
      {
        // equip it
        ((ShootWeapon)m_pEquippedWeapon).doChangeProjectile(((ShootWeapon)m_pEquippedWeapon).ProjectileID);
      }

      // set initialization flag
      m_bIsInitialized = true;
    }

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

      switch(m_eCurState)
      {
        // in service
        case TowerServiceStates.TOWER_SERVICE_STATES_IN_SERVICE:
        {
          // decrease timer
          m_fCurServiceTime -= Time.deltaTime;

          // if service time is over
          if(m_fCurServiceTime < 1.0e-5f)
          {
            // hide timer GUI
            m_pTimerBar.hideIt();

            // stop using the equipped weapon
            if(m_pEquippedWeapon != null)
            {
              m_pEquippedWeapon.doStopUseCom();  
            }

            // fire service done event
            Game.Event.GameEventMgr.Instance.fireGlobalScopeEvent(Game.Event.EventIDs.EVENT_IDS_LOGIC_DEFENSE_TOWER_SERVICE_DONE,
                                                                  this,
                                                                  Game.Event.EventTriggerType.EVENT_TRIGGER_TYPE_LOCAL,
                                                                  this
                                                                 );

            // change back to Idle state
            m_eCurState = TowerServiceStates.TOWER_SERVICE_STATES_IDLE;
          }
          else
          {
            // still in service

            // there is an equipped weapon
            if(m_pEquippedWeapon != null)
            {
              // if the equipped weapon is in usage
              if(true == m_pEquippedWeapon.isInUse)
              {
                if(false == MonsterMgr.Instance.isAnyBossCanHandleHit()    // NO boss can handle hit
                   &&                                                      // and
                   false == MonsterMgr.Instance.isAnyMonsterCanHandleHit() // NO monster can handle hit
                  )
                {
                  // stop using weapon
                  m_pEquippedWeapon.doStopUseCom();
                }
              }
              else
              {
                if(true == MonsterMgr.Instance.isAnyBossCanHandleHit()      // any boss can handle hit
                   ||                                                       // or
                   true == MonsterMgr.Instance.isAnyMonsterCanHandleHit()   // any monster can handle hit
                  )    
                {
                  // use the equipped weapon
                  m_pEquippedWeapon.doUseCom();
                }
              }
            }
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

      // create TowerTimerBar
      if(null == m_pTimerBar)
      {
        GameObject _pTimerBarGO = null;
        if(GameLevelMgr.Instance.CurrentGameLevel.getAsset(TOWER_TIMERBAR_ID, out _pTimerBarGO) == false)
        {
          DefaultGameLogger.Instance.dumpLogError("[DefenseBuilding][onSpawnComDone][" + gameObject.ToString() + "]: Can NOT create TowerTimerBar");

          return;
        }

        // check the created TowerTimerBar
        m_pTimerBar = _pTimerBarGO.GetComponentInChildren< Game.GUI.TowerTimerBar >();
        if(null == m_pTimerBar)
        {
          DefaultGameLogger.Instance.dumpLogError("[DefenseBuilding][onSpawnComDone][" + _pTimerBarGO.ToString() + "]: TowerTimerBar does NOT have TowerTimerBar component");

          return;
        }

        // initialize the timer bar
        m_pTimerBar.doInitialize();

        // mount this timer bar
        GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.attachTowerTimerBar(m_pTimerBar.gameObject);
      }

      // fire spawn event
      Game.Event.GameEventMgr.Instance.fireGlobalScopeEvent(Event.EventIDs.EVENT_IDS_LOGIC_DEFENSETOWER_SPAWNED, 
                                                            MaxHealth,
                                                            Event.EventTriggerType.EVENT_TRIGGER_TYPE_AI,
                                                            this
                                                           );
    }
    #endregion // PROTECTED OVERRIDDENS 

    #endregion // PROTECTED METHODS

    #region PRIVATE DATA MEMBERS
    // tower timerbar ID
    static int TOWER_TIMERBAR_ID = 376;
    // the equipped weapon: any sort of weapon: shoot weapon, melee weapon or projector
    Weapon m_pEquippedWeapon;
    // TowerTimerBar component
    Game.GUI.TowerTimerBar m_pTimerBar;
    // timer bar's mount point
    GameObject m_pTimerBarMountPoint;
    // current service timer
    float m_fCurServiceTime;
    // current service state
    TowerServiceStates m_eCurState;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Logic

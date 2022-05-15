/**
 * Building for Player
 */

using UnityEngine;
using Framework.Utils;
using Framework.Logger;
using Game.Event;

namespace Game.Logic
{
  public class PlayerBuilding : Building
  {
    #region PUBLIC DATA MEMBERS
    // HP bar show time
    public float HPBarShowTime = 3.0f;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS

    #region PUBLIC OVERRIDDENS
    /**
     * Collsion Handler for any Entity
     * - Normal Entity(Except Weapon) will call weapon's onCollision() during its handling
     */
    public override void onCollision(Entity a_pTrigger,                   // who has triggered this Hit
                                     CollisionHandler a_pCollidedPart     // collided part
                                    )
    {
      switch(a_pTrigger.EntityType)
      {
      // when a weapon hit this ground
      case EntityTypes.ENTITY_TYPES_WEAPON:
        {
          // JUST for Monsters
          if((CharacterTypes.CHARACTER_TYPES_MONSTER != ((Weapon)a_pTrigger).OwnerType)
             &&
             (CharacterTypes.CHARACTER_TYPES_BOSS != ((Weapon)a_pTrigger).OwnerType)
            )
          {
            return;
          }

          if(WeaponTypes.WEAPON_TYPES_SHOOTWEAPON != ((Weapon)a_pTrigger).WeaponType   // NOT shooting weapon
             &&                                                                        // and
             true == ((Weapon)a_pTrigger).isInUse                                      // In Any Attacking state
            )
          {
            // let the weapon handle this hit
            ((Weapon)a_pTrigger).onCollision(this, a_pCollidedPart);

            // if this building can NOT handle this collision: return;
            if(false == canHandleCollision(a_pTrigger, a_pCollidedPart))
            {
              return;
            }

            // NOT beam, which will call handleCollision() directly
            if(WeaponTypes.WEAPON_TYPES_BEAM != ((Weapon)a_pTrigger).WeaponType)
            {
              // handle this hit
              handleCollision(a_pTrigger, a_pCollidedPart);
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

    /**
     * Handle the given Collision within this Entity
     * called from Trigger in order to cause damage directly for instance
     */
    public override void handleCollision(Entity a_pTrigger,                   // who has triggered this Hit
                                         CollisionHandler a_pCollidedPart     // collided part
                                        )
    {
      // by zero HP: return
      if(m_fCurHP < 1.0e-5f)
      {
        return;
      }

      // TODO: consider damage type
      // read in damage points
      // CausedDamage = RealWeaponDamage * (1.0 - CurrentProFactor)
      float _fCausedDamage = ((Weapon)a_pTrigger).DamagePoints;

      // check damage points
      if(_fCausedDamage > m_fCurHP)
      {
        _fCausedDamage = m_fCurHP;
      }

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[" + gameObject.ToString() + "]: [" + a_pTrigger.gameObject.ToString() + " Hit] caused Damage: " + _fCausedDamage + " Building HP: " + m_fCurHP + " --> " + (m_fCurHP - _fCausedDamage));
#endif

      // deduct HP
      m_fCurHP -= _fCausedDamage;

      // fire hit event
      GameEventMgr.Instance.fireGlobalScopeEvent(Event.EventIDs.EVENT_IDS_ATTACK_PLAYERBUILDING_HIT,
                                                 _fCausedDamage, 
                                                 Event.EventTriggerType.EVENT_TRIGGER_TYPE_AI,
                                                 this
                                                );

      // adjust damage Fx
      adjustDamageFx();

      // show HP bar
      showHPBar();

      // check HP
      // NO HP anymore
      if(m_fCurHP < 1.0e-5f)
      {
        // fire building destroyed event
        GameEventMgr.Instance.fireGlobalScopeEvent(Event.EventIDs.EVENT_IDS_LOGIC_PLAYER_BUILDING_DESTROYED,
                                                   this,
                                                   Event.EventTriggerType.EVENT_TRIGGER_TYPE_AI,
                                                   this
                                                  );
      }
    }

    /**
     * gain HP
     */
    public override void gainHP(float a_fGainedHP)
    {
#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[PlayerBuilding][gainHP]: Gained HP: " +a_fGainedHP);
#endif

      // sum up the given HP
      m_fCurHP += a_fGainedHP;

      // check the validity of current HP
      if(m_fCurHP > MaxHealth)
      {
        m_fCurHP = MaxHealth;
      }

      // show HP bar to indicate player
      showHPBar();

      // adjust damage fx
      adjustDamageFx();
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

      // change to dead state
      m_eCurEntityState = EntityStates.ENTITY_STATES_DEAD;

      // destroy HP bar
      destroyHPBar();
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

      // MaxHealth: > 0
      if(MaxHealth < 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[PlayerBuilding][doStart][" + gameObject.ToString() + "]: No Heath Points");

        return;
      }

      // try to find HeartBar node
      m_pHeartBarNode = GameObjectUtils.FindNode(gameObject, "HeartBar");
      if(null == m_pHeartBarNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[PlayerBuilding][doStart][" + gameObject.ToString() + "]: Can NOT find HeartBar node in the Bone");

        return;
      }

      // try to find damage fx nodes
      m_pHalfDamagedFxNode  = GameObjectUtils.FindNode(gameObject, "HalfDamagedFx");
      m_pWholeDamagedFxNode = GameObjectUtils.FindNode(gameObject, "WholeDamagedFx");

      // disable all damage fx nodes
      m_pHalfDamagedFxNode.SetActive(false);
      m_pWholeDamagedFxNode.SetActive(false);

      // set initialization flag
      m_bIsInitialized = true;
    }

    // simulate the spawn animation
    protected override void doSpawnAnimation()
    {
      // no spawn animation simulation
    }

    // determine if spawn process will need to wait for completion of spawn animation: keyframed or simulated
    protected override bool waitForSpawnAnimation()
    {
      return false;
    }

    // called after doSpawnCom() has done
    protected override void onSpawnComDone()
    {
      base.onSpawnComDone();

      // create HPBar
      if(null == m_pHPBar)
      {
        // try to load the building HP bar
        GameObject _pBuildingHPBarNode = null;
        if(GameLevelMgr.Instance.CurrentGameLevel.getAsset(BUILDING_HPBAR_ID, out _pBuildingHPBarNode) == false)
        {
          DefaultGameLogger.Instance.dumpLogError("[PlayerBuilding][onSpawnComDone][" + gameObject.ToString() + "]: Can NOT create BuildingHPBar");

          return;
        }

        // check the created HPBar
        m_pHPBar = _pBuildingHPBarNode.GetComponentInChildren< Game.GUI.BuildingHPBar >();
        if(null == m_pHPBar)
        {
          DefaultGameLogger.Instance.dumpLogError("[PlayerBuilding][onSpawnComDone][" + _pBuildingHPBarNode.ToString() + "]: HPBar does NOT have BuildingHPBar component");

          return;
        }

        // initialize the HPBar
        m_pHPBar.doInitialize();

        // mount this HPBar
        GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.attachBuildingHP(m_pHPBar.gameObject);
      }

      // fire spawn event
      GameEventMgr.Instance.fireGlobalScopeEvent(Event.EventIDs.EVENT_IDS_LOGIC_PLAYERBUILDING_SPAWNED, 
                                                 MaxHealth,
                                                 Event.EventTriggerType.EVENT_TRIGGER_TYPE_AI,
                                                 this
                                                );
    }
    #endregion // PROTECTED OVERRIDDENS 

    #endregion // PROTECTED METHODS

    #region PRIVATE METHODS
    /**
     * show HP bar based on current HP in different colors
     */
    void showHPBar()
    {
      // set the current HP
      m_pHPBar.setProgress(m_fCurHP, MaxHealth);

      // set show time
      m_fCurHPBarShowTime = HPBarShowTime;

      // start chasing
      m_pHPBar.startChase(m_pHeartBarNode);

      // enable HP bar
      m_pHPBar.gameObject.SetActive(true);

      // set show flag
      m_pHPBar.IsShown = true;
    }

    /**
     * hide HP bar
     */
    void hideHPBar()
    {
      // stop chasing
      m_pHPBar.stopChase();

      // disable the given UI
      m_pHPBar.gameObject.SetActive(false);

      // set show flag
      m_pHPBar.IsShown = false;
    }

    /**
     * destroy HP bar
     */
    void destroyHPBar()
    {
      // hide HP bar at first
      hideHPBar();

      // let HP Bar destroy its internal members
      m_pHPBar.doDestroy();

      // remove it out of game GUI
      GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.removeBuildingHP(m_pHPBar.gameObject);

      // destroy HPBar UI
      GameObject.Destroy(m_pHPBar.gameObject);

      // reset the reference
      m_pHPBar = null;
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

      // base update
      base.doUpdate();

      if(false == IsDead             // this monster is NOT dead
         &&                          // and
         true == m_pHPBar.IsShown    // HP bar is shown
        )
      {
        // if there is NO show time: return
        if(HPBarShowTime < 0.0f)
        {
          return;
        }

        // otherwise, decrease the current show time
        m_fCurHPBarShowTime -= Time.deltaTime;

        // if show time is done
        if(m_fCurHPBarShowTime < 1.0e-5f)
        {
          // hide the HP bar
          hideHPBar();
        }
      }
    }

    // adjust damage effect
    void adjustDamageFx()
    {
      // current HP < 60%
      if(m_fCurHP / MaxHealth < 0.6f)
      {
        // enable HalfDamagedFx
        if(m_pHalfDamagedFxNode != null                    // has damage effect for half damage
           &&                                              // and
           false == m_pHalfDamagedFxNode.activeSelf        // not enabled so far
          )
        {
          // enable the HalfDamagedFx
          m_pHalfDamagedFxNode.SetActive(true);
        }
      }

      // current HP >= 60%
      else
      {
        // if HalfDamagedFx is ON: turn off it
        if(m_pHalfDamagedFxNode != null
           &&
           true == m_pHalfDamagedFxNode
          )
        {
          m_pHalfDamagedFxNode.SetActive(false);
        }
      }

      // current HP < 40%
      if(m_fCurHP / MaxHealth < 0.4f) // < 40%
      {
        // enable WholeDamagedFx
        if(m_pWholeDamagedFxNode != null                   // has damage effect for whole damage
           &&                                              // and
           false == m_pWholeDamagedFxNode.activeSelf       // not enabled so far
          )
        {
          // enable the WholeDamagedFx
          m_pWholeDamagedFxNode.SetActive(true);
        }
      }

      // current HP >= 40%
      else
      {
        // if WholeDamagedFx is ON: turn off it
        if(m_pWholeDamagedFxNode != null
           &&
           true == m_pWholeDamagedFxNode
          )
        {
          m_pWholeDamagedFxNode.SetActive(false);
        }
      }
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // buidling hp bar ID
    static int BUILDING_HPBAR_ID = 332;
    // HPBar attach node
    GameObject m_pHeartBarNode;
    // HPBar component
    Game.GUI.BuildingHPBar m_pHPBar;
    // HP bar current show time
    float m_fCurHPBarShowTime;
    // the damage fx node used when HP is below 60%
    GameObject m_pHalfDamagedFxNode;
    // the damaged fx node used when HP is below 40%
    GameObject m_pWholeDamagedFxNode;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Logic

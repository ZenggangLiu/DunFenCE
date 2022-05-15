using System;
using UnityEngine;
using Framework.Utils;
using Framework.Logger;

namespace Game.Logic
{
  public class Monster : Character
  {
    #region PUBLIC DATA MEMBERS
    // JUST NEEDED IF SPAWN ANIMATION IS MISSING
    // time used during spawning
    public float SpawnTime = 2.0f; // default 2 seconds
    // drag speed: meter per second
    public float DragSpeed = 0.1f; // default 10cm
    // HP bar show time
    public float HPBarShowTime = 3.0f;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    // get and set current drag time
    public float CurrentDragTime
    {
      get
      {
        return m_fCurDragTime;
      }

      set
      {
        m_fCurDragTime = value;
      }
    }

    #region PUBLIC OVERRIDDENS
    /**
     * UnSpawn: last animation of whole entity's life
     */
    public override void doUnSpawnCom()
    {
      // already in unspawn state: return
      if(EntityStates.ENTITY_STATES_UNSPAWNING == m_eCurEntityState)
      {
        return;
      }

      // if there is NOT spawn simulation: do not simulate UnSpawn
      if(false == m_bIsSimulatingSpawn)
      {
        onAnimationUnSpawnEnded();
      }
      else
      {
        // set timer
        m_fCurDragTime = SpawnTime;

        // change state to UnSpawn
        m_ePrevEntityState = m_eCurEntityState;

        m_eCurEntityState = EntityStates.ENTITY_STATES_UNSPAWNING;
      }

      // destroy HP bar
      destroyHPBar();

      // unspawn the base
      base.doUnSpawnCom();
    }

    /**
     * Dead: destroy this entity
     */
    public override void doDeadCom()
    {
      // choose drop off
      DropOffMgr.Instance.createDropOff(this);

      // at last call the base's version
      base.doDeadCom(); 
    }

    /**
     * show HP bar
     */
    public override void doShowHPBarCom()
    {
      // set the current HP
      m_pHPBar.setProgress(m_fCurHP);

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
    public override void doHideHPBarCom()
    {
      // stop chasing
      m_pHPBar.stopChase();

      // disable the given UI
      m_pHPBar.gameObject.SetActive(false);

      // set show flag
      m_pHPBar.IsShown = false;
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS
    /**
     * destroy HP bar
     */
    protected void destroyHPBar()
    {
      // hide HP bar at first
      doHideHPBarCom();

      // let HP Bar destroy internal members
      m_pHPBar.doDestroy();

      // remove it out of game GUI
      // mount this HPBar
      GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.removeCharacterHP(m_pHPBar.gameObject);

      // destroy HPBar UI
      GameObject.Destroy(m_pHPBar.gameObject);

      // reset the reference
      m_pHPBar = null;
    }

    #region PROTECTED OVERRIDDENS
    /**
     * doResetInEditor() is called when the user hits the Reset button in the Inspector's context menu
     * or when adding the component the first time 
     * This function is only called in editor mode 
     * doResetInEditor() is most commonly used to give good default values in the inspector
     */
    protected override void doResetInEditor()
    {
      base.doResetInEditor();

      // set correct character type
      CharacterType = CharacterTypes.CHARACTER_TYPES_MONSTER;
    }

    // initialize this entity: called within doSpawnCom() only
    // m_bIsInitialized will be SET in this method
    protected override void doInitialize()
    {
      base.doInitialize();

      // clear initialization flag before checking
      m_bIsInitialized = false;
      
      // sanity checking
      // must have weapon
      if(null == EquippedWeaponList
         ||
         0 == EquippedWeaponList.Length
        )
      {
        DefaultGameLogger.Instance.dumpLogError("[Monster][doInitialize][" + gameObject.ToString() + "]: No a Single Equipped Weapon");

        return;
      }

      // must have brain
      if(null == m_pMountedBrain) // got within base class
      {
        DefaultGameLogger.Instance.dumpLogError("[Monster][doInitialize][" + gameObject.ToString() + "]: No Brain");

        return;
      }

      // try to find HeartBar node
      m_pHeartBarNode = GameObjectUtils.FindNode(gameObject, "HeartBar");
      if(null == m_pHeartBarNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[Monster][doInitialize][" + gameObject.ToString() + "]: Can NOT find HeartBar node in the Bone");

        return;
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
      // if NOT initialized: return
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
          doHideHPBarCom();
        }
      }
    }

    // simulate the spawn animation
    protected override void doSpawnAnimation()
    {
      base.doSpawnAnimation();
      
      // drag the monster beneath water level
      gameObject.transform.position = new Vector3(gameObject.transform.position.x, -Math.Abs(SpawnTime) * Math.Abs(DragSpeed), gameObject.transform.position.z);

      // set timer
      m_fCurDragTime = SpawnTime;

      // create HPBar
      if(null == m_pHPBar)
      {
        GameObject _pHPBarNode = null;
        if(GameLevelMgr.Instance.CurrentGameLevel.getAsset(304, out _pHPBarNode) == false)
        {
          DefaultGameLogger.Instance.dumpLogError("[Monster][doSpawnAnimation][" + gameObject.ToString() + "]: Can NOT create HPBar Node");

          return;
        }

        // check the created HPBar
        m_pHPBar = _pHPBarNode.GetComponentInChildren< Game.GUI.HeartBar >();
        if(null == m_pHPBar)
        {
          DefaultGameLogger.Instance.dumpLogError("[Monster][doSpawnAnimation][" + _pHPBarNode.ToString() + "]: HPBar does NOT have HeartBar component");

          return;
        }

        // initialize the HPBar
        m_pHPBar.doInitialize(MaxHealth);

        // mount this HPBar
        GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.attachCharacterHP(m_pHPBar.gameObject);
      }
    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    // current drag time
    protected float m_fCurDragTime;
    // HeartBar component
    protected Game.GUI.HeartBar m_pHPBar;
    // HP bar current show time
    protected float m_fCurHPBarShowTime;
    // HeartBar node
    protected GameObject m_pHeartBarNode;
    #endregion // PROTECTED DATA MEMBERS
  }
} // namespace Game.Logic

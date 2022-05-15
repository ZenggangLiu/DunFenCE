/**
 * Game Specific High Level Event:
 * - Global Scope Events: Raining, Attack Starting,...
 * - Local Scope Events: Move To,...
 */

using UnityEngine;

namespace Game.Event
{
  public enum EventIDs
  {
    EVENT_IDS_UNKNOWN = -1,

    // GLOBAL SCOPE EVENTS
    // environmental events
    // start raining
    EVENT_IDS_ENV_START_RAINNING    = 0,
    // stop raining
    EVENT_IDS_ENV_STOP_RAINING,
    // start snowing
    EVENT_IDS_ENV_START_SNOWING,
    // stop snowing
    EVENT_IDS_ENV_STOP_SNOWING,

    // monster attack related events
    // prepare attack warning:like monster flag falling down
    EVENT_IDS_ATTACK_PREPARE_ATTACKWARNING = 128, // no event data
    // done with attack warning
    EVENT_IDS_ATTACK_ATTACKWARNING_FINISHED, // no event data
    // attack wave preparing 
    EVENT_IDS_ATTACK_WAVE_PREPARING, // event data: int: wave index: [1, n]
    // last attack wave preparing
    EVENT_IDS_ATTACK_LASTWAVE_PREPARING, // no event data
    // one attack wave starting
    EVENT_IDS_ATTACK_WAVE_STARTING,
    // one attack wave finished
    EVENT_IDS_ATTACK_WAVE_FINISHED,
    // all waves ended: no event data
    EVENT_IDS_ATTACK_ALLWAVES_ENDED,
    // won: player is successful on defending
    EVENT_IDS_ATTACK_SUCCESS_DEFEND,
    // raided: player failed on defending: no event data
    EVENT_IDS_ATTACK_RAIDED,
    // one player's building is hit: data: decreased HP(float)
    EVENT_IDS_ATTACK_PLAYERBUILDING_HIT,
    // one monster spawned: data Monster reference
    EVENT_IDS_ATTACK_MONSTER_SPAWNED,
    // one monster dead: data Monster reference
    EVENT_IDS_ATTACK_MONSTER_DEAD,
    // one boss spawned: data Boss reference
    EVENT_IDS_ATTACK_BOSS_SPAWNED,
    // one boss dead: data Boss reference
    EVENT_IDS_ATTACK_BOSS_DEAD,

    // loot related events
    // opened a coin pack: data: coin amount
    EVENT_IDS_LOOT_OPENED_COINPACK  = 158,
    // opened a health pack: data: +health amount
    EVENT_IDS_LOOT_OPENED_HEALTHPACK,
    // opened a magic pack: data: mana amount
    EVENT_IDS_LOOT_OPENED_MAGICPACK,
    // opened a rewards pack: data: rewards amount
    EVENT_IDS_LOOT_OPENED_REWARDPACK,
    // opened a damage pack: data: -health amount
    EVENT_IDS_LOOT_OPENED_DAMAGEPACK,

    // game logic related event
    // one player building has spawned: data: total HP, trigger: the spawned building
    EVENT_IDS_LOGIC_PLAYERBUILDING_SPAWNED = 168,
    // one defense tower has spawned: data: total HP, trigger: the spawned tower
    EVENT_IDS_LOGIC_DEFENSETOWER_SPAWNED,
    // lost AR marker: data: ARMarkerData
    EVENT_IDS_LOGIC_AR_MARKER_LOST,
    // found AR marker: data: ARMarkerData
    EVENT_IDS_LOGIC_AR_MARKER_FOUND,
    // user has closed an interstitial Ads: no event data
    EVENT_IDS_LOGIC_ADS_INTERSTITIAL_CLOSED,
    // user has skipped Ads video: NO data
    EVENT_IDS_LOGIC_ADS_VIDEO_SKIPPED,
    // Ads video playback failed: No data
    EVENT_IDS_LOGIC_ADS_VIDEO_FAILED,
    // user has completed Ads video playback: data: rewards gained: int
    EVENT_IDS_LOGIC_ADS_VIDEO_FINISHED,
    // game is unloading the given game level: data: GameLevel reference
    EVENT_IDS_LOGIC_LEVEL_UNLOADING,
    // game level is changing: data: percentage: float
    EVENT_IDS_LOGIC_LEVEL_CHANGING,
    // game has switched to the given level: data: GameLevel reference
    EVENT_IDS_LOGIC_LEVEL_SWITCHED,
    // the given tower has completed its service: data: DefenseBuilding reference
    EVENT_IDS_LOGIC_DEFENSE_TOWER_SERVICE_DONE,
    // one player's building is destroyed: data: reference to the building
    EVENT_IDS_LOGIC_PLAYER_BUILDING_DESTROYED,
    // failed to buy an item: data: game specific item id
    EVENT_IDS_LOGIC_IAP_PURCHASE_FAILED,
    // item purchase succeeded: data: bought game specific item id
    EVENT_IDS_LOGIC_IAP_PURCHASE_SUCCEEDED,

    // LOCAL SCOPE EVENTS: key: UID of the given character instance
    // character related events
    // general character control events
    // move to: data: target position
    EVENT_IDS_CHARACTER_MOVETO      = 256,
    // rotate to: data: target orientation
    EVENT_IDS_CHARACTER_ROTATO,
    // play the given animation: data: animation name string
    EVENT_IDS_CHARACTER_PLAYANIM,

    // bummer related events
    // one bummer is idle: data: source bummer reference
    EVENT_IDS_BUMMER_IDLE          = 384,
    // one bummer is talking: data: source bummer reference
    EVENT_IDS_BUMMER_TALKING,
    // one bummer is happy: data: source bummer reference
    EVENT_IDS_BUMMER_HAPPY,
    // one bummer is angry: data: source bummer reference
    EVENT_IDS_BUMMER_ANGRY,
    // one bummer is scarced: data: source bummer reference
    ENVET_IDS_BUMMER_SCARED,
    // one bummer is noding head: data: source bummer reference
    EVENT_IDS_BUMMER_NOD_HEAD,
    // one bummer is shaking head: data: source bummer reference
    EVENT_IDS_BUMMER_SHAKE_HEAD,
    // one bummer is waving head: data: source bummer reference
    EVENT_IDS_BUMMER_WAVE_HAND,

  }
  
  public enum EventTriggerType
  {
    EVENT_TRIGGER_TYPE_UNKNOWN = -1 ,
    EVENT_TRIGGER_TYPE_LOCAL   = 0,
    EVENT_TRIGGER_TYPE_AI      = 1,
    EVENT_TRIGGER_TYPE_REMOTE  = 2,
  }

  public struct ARMarkerData
  {
    public string MarkerName;
    public GameObject AttachNode;
  }

  // stack object
  public struct GameEvent
  {
    #region PUBLIC DATA MEMBERS
    // event id
    public EventIDs ID;
    // event data
    public System.Object Data;
    // trigger type
    public EventTriggerType TriggerType;
    // trigger object
    public System.Object Trigger;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    public GameEvent(EventIDs a_eID,
                     System.Object a_pData,
                     EventTriggerType a_eTriggerType, 
                     System.Object a_pTrigger
                    )
    {
      // set the given event id
      ID = a_eID;

      // store the given event data
      Data = a_pData;

      // set the given trigger type
      TriggerType = a_eTriggerType;

      Trigger = a_pTrigger;
    }

    // initialize a real GameEvent with the given values
    public void initWith(EventIDs a_eID,
                         System.Object a_pData,
                         EventTriggerType a_eTriggerType, 
                         System.Object a_pTrigger
                        )
    {
      // set the given event id
      ID = a_eID;
      
      // store the given event data
      Data = a_pData;

      // set the given trigger type
      TriggerType = a_eTriggerType;

      Trigger = a_pTrigger;
    }
    #endregion // PUBLIC METHODS
  }

} // namespace Framework.GameEvents

/**
 * class used to describe Loots, which has predefined attributes: like coin, health pack or damage point
 */

using UnityEngine;
using Framework.Logger;

namespace Game.Logic
{
  public enum LootTypes
  {
    LOOT_TYPES_UNKNOWN = 0,
    LOOT_TYPES_COINPACK,
    LOOT_TYPES_HEALTHPACK,
    LOOT_TYPES_MAGICPACK,
    LOOT_TYPES_REWARDPACK,  
    // it will cause damage on the picker: like a bomb
    LOOT_TYPES_DAMAGEPACK,
  }

  public class Loot : Entity
  {
    #region PUBLIC DATA MEMBERS
    // loot type
    public LootTypes LootType = LootTypes.LOOT_TYPES_UNKNOWN;
    // for coin pack: the coin amount
    public int CoinAmount = -1;
    // for health pack: the health amount
    public int HealthAmount = -1;
    // for magic pack: the magic amount
    public int MagicAmount = -1;
    // for reward pack: the reward amount
    public int RewardAmount = -1;
    // for damage pack: the damage amount
    public int DamageAmount = -1;
    // maximal life time: in second
    public float LifeTime = 30.0f;
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
      // if current loot has being or already consumed: return
      if(EntityStates.ENTITY_STATES_IDLE != m_eCurEntityState)
      {
        return;
      }

      // check the trigger's type
      switch(a_pTrigger.EntityType)
      {
      // when a weapon hit this loot
      case EntityTypes.ENTITY_TYPES_WEAPON:
      {
        // the weapon is NOT dead
        if(false == a_pTrigger.IsDead)
        {
          // if the weapon is from hero
          if(CharacterTypes.CHARACTER_TYPES_HERO == ((Weapon)a_pTrigger).OwnerType)
          {
            // JUST weapon can trigger the hit
            // if we can NOT get this loot: return
            if(false == canGetLoot((Weapon)a_pTrigger))
            {
              return;
            }

            // send hit command
            doHitCom(a_pTrigger, a_pCollidedPart);

            // tell a_pTrigger to handle this hit: Entity.onCollision()
            ((Weapon)a_pTrigger).onCollision(this, a_pCollidedPart);
          }
        }

        break;
      }

      default:
      {
        // other entity's types can not
        break;
      }
      }
    }

    // animation related commands
    /**
     * Hit: normal hit
     */
    public override void doHitCom(Entity a_pTrigger,                   // trigger entity
                                  CollisionHandler a_pCollidedPart     // collided part
                                 )
    {
      // if already hit: return
      if(EntityStates.ENTITY_STATES_HIT == m_eCurEntityState)
      {
        return;
      }

      // handle the event internally
      // get this loot
      // change state to hit
      m_eCurEntityState = EntityStates.ENTITY_STATES_HIT;

      // tell EconomyMgr about the got loot
      int _iLootData = 0;
      switch(LootType)
      {
      case LootTypes.LOOT_TYPES_COINPACK:
      {
        _iLootData = CoinAmount;
        
        break;
      }
        
      case LootTypes.LOOT_TYPES_HEALTHPACK:
      {
        _iLootData = HealthAmount;
        
        break;
      }
        
      case LootTypes.LOOT_TYPES_MAGICPACK:
      {
        _iLootData = MagicAmount;
        
        break;
      }
        
      case LootTypes.LOOT_TYPES_DAMAGEPACK:
      {
        _iLootData = DamageAmount;
        
        break;
      }
        
      case LootTypes.LOOT_TYPES_REWARDPACK:
      {
        _iLootData = RewardAmount;
        
        break;
      }
        
      default:
      {
        break;
      }
      }
      
      EconomyMgr.Instance.onGotLoot(LootType, _iLootData);

      // play hit animation if needed
      // NO Hit animation
      if(false == playAnimationTrigger(HitAnimations))
      {
        doDeadCom();
      }
    }
    
    /**
     * Struck: strong hit
     */
    public override void doStruckCom(Entity a_pTrigger,                  // trigger entity
                                     CollisionHandler a_pCollidedPart    // collided part
                                    )
    {
      doHitCom(a_pTrigger, a_pCollidedPart);
    }

    /**
     * Dead: destroy this entity
     */
    public override void doDeadCom()
    {
      // play dead animation
      // if failed: set to dead state straight away without waiting for animation completion
      if(false == playAnimationTrigger(DeadAnimations))
      {
        doDestroy();
      }
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS

    #region PROTECTED OVERRIDDENS
    /**
     * doResetInEditor() is called when the user hits the Reset button in the Inspector's context menu
     * or when adding the component the first time 
     * This function is only called in editor mode 
     * doResetInEditor() is most commonly used to give good default values in the inspector
     */
    protected override void doResetInEditor()
    {
      EntityType = EntityTypes.ENTITY_TYPES_LOOT; 
    }

    // initialize this entity: called within doSpawnCom() only
    // m_bIsInitialized will be SET in this method
    protected override void doInitialize()
    {
      base.doInitialize();

      // clear initialization flag before checking
      m_bIsInitialized = false;

      // sanity checking
      // check loot type
      if(LootTypes.LOOT_TYPES_UNKNOWN == LootType)
      {
        DefaultGameLogger.Instance.dumpLogError("[Loot][doInitialize][" + gameObject.ToString() + "]: No Loot Type");
        
        return;
      }

      // check amount based on loot type
      switch(LootType)
      {
        case LootTypes.LOOT_TYPES_COINPACK:
        {
          if(CoinAmount < 0)
          {
            DefaultGameLogger.Instance.dumpLogError("[Loot][doInitialize][" + gameObject.ToString() + "]: No Coin Amount");
          
            return;
          }

          break;
        }

        case LootTypes.LOOT_TYPES_HEALTHPACK:
        {
          if(HealthAmount < 0)
          {
            DefaultGameLogger.Instance.dumpLogError("[Loot][doInitialize][" + gameObject.ToString() + "]: No Health Amount");
          
            return;
          }

          break;
        }

        case LootTypes.LOOT_TYPES_MAGICPACK:
        {
          if(MagicAmount < 0)
          {
            DefaultGameLogger.Instance.dumpLogError("[Loot][doInitialize][" + gameObject.ToString() + "]: No Magic Amount");
            
            return;
          }
  
          break;
        }

        case LootTypes.LOOT_TYPES_REWARDPACK:
        {
          if(RewardAmount < 0)
          {
            DefaultGameLogger.Instance.dumpLogError("[Loot][doInitialize][" + gameObject.ToString() + "]: No Reward Amount");
            
            return;
          }

          break;
        }

        case LootTypes.LOOT_TYPES_DAMAGEPACK:
        {
          if(DamageAmount < 0)
          {
            DefaultGameLogger.Instance.dumpLogError("[Loot][doInitialize][" + gameObject.ToString() + "]: No Damage Amount");
          
            return;
          }

          break;
        }

        default:
        {
          break;
        }
      }

      m_eCurEntityState = EntityStates.ENTITY_STATES_IDLE;

      // set initialization flag
      m_bIsInitialized = true;
    }

    /**
     * destroy this entity directly without doing any sort of animations: dead animation and unspawn animation
     */
    public override void doDestroy()
    {
      // already in dead state: return
      if(EntityStates.ENTITY_STATES_DEAD == m_eCurEntityState)
      {
        return;
      }

      // chaged to dead state straightaway
      m_eCurEntityState = EntityStates.ENTITY_STATES_DEAD;

      // clear the initialization flag
      m_bIsInitialized = false;

      // destroy the loot
      DropOffMgr.Instance.destroyDropOff(gameObject);

      base.doDestroy();
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

      switch(m_eCurEntityState)
      {
        case EntityStates.ENTITY_STATES_IDLE:
        {
          // count down life time
          m_fCurLifeTime -= Time.deltaTime;

          if(m_fCurLifeTime < 1.0e-5f)
          {
            doDeadCom();
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
     * called from each ONE time animation, after it has been played back
     * a_iAnimationNameHash is the animation name hash ID: like:
     * - Spawn
     * - Jump
     * - Leap: High Jump
     * - Attack
     * - Melee
     * - Hit
     * - Struck: Powerful Hit
     * - Dead 
     * 
     * return true if handled
     */
    protected override bool doAnimationEndedHandle(int a_iAnimationNameHash)
    {
      if(true == base.doAnimationEndedHandle(a_iAnimationNameHash))
      {
        return true;
      }

      DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "]: UNKNOWN ANIMATION PLAYED");

      return false;
    }

    // Callbacks of Animation Playback
    // called when Common Hit/GotDamaged animation ended: Hit/GotDamaged Animation will go automatically to Idle state when done
    protected override void onAnimationHitEnded()
    {
      // done with Hit animation: destroy this loot
      doDeadCom();
    }

    // called when Dead animation ended
    protected override void onAnimationDeadEnded()
    {
      doDestroy();
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

      // set life time
      m_fCurLifeTime = LifeTime;
    }
    #endregion // PROTECTED OVERRIDDENS

    // if the current loot can be got
    protected bool canGetLoot(Weapon a_pTriggerWeapon)
    {
      // shoot weapon can NOT get the loot
      if(WeaponTypes.WEAPON_TYPES_SHOOTWEAPON == a_pTriggerWeapon.WeaponType)
      {
        return false;
      }

      // only hero or remote hero can get loot
      bool _bCanGet = CharacterTypes.CHARACTER_TYPES_HERO == a_pTriggerWeapon.OwnerType
                      ||
                      CharacterTypes.CHARACTER_TYPES_HERO_REMOTE == a_pTriggerWeapon.OwnerType;

      return _bCanGet;
    }
    #endregion // PROTECTED METHODS

    // current life time
    float m_fCurLifeTime;
  }

} // namespace Game.Logic

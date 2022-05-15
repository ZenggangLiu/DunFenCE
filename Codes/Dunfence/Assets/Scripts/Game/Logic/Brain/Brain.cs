/**
 * Base Class of all sorts of Brains
 * Reset by Owner Character
 */

using UnityEngine;
using Framework.Utils;
using Framework.Logger;

namespace Game.Logic
{
  // brain types
  public enum BrainTypes
  {
    BRAIN_TYPES_UNKNOWN = 0,
    BRAIN_TYPES_AI_CONTROLLED,
    BRAIN_TYPES_USER_CONTROLLED,
    BRAIN_TYPES_REMOTE_CONTROLLED,
  }

  public enum BrainEventIDs
  {
    BRAIN_EVENT_IDS_NO,
    // expect doing attack
    BRAIN_EVENT_IDS_EXPECTING_ATTACK,        // event data: the animation data
    // expect doing rage attack
    BRAIN_EVENT_IDS_EXPECTING_RAGE_ATTACK,   // event data: the animation data
  }

  // brain event
  public class BrainEvent : Clonable < BrainEvent >
  {
    #region PUBLIC DATA MEMBERS
    // current event id
    public BrainEventIDs EventID;
    // current event data
    public System.Object EventData;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    // default constructor
    public BrainEvent()
    {
      EventID = BrainEventIDs.BRAIN_EVENT_IDS_NO;
      EventData = null;
    }

    // constructor with parameters
    public BrainEvent(BrainEventIDs a_eID, System.Object a_pData)
    {
      EventID = a_eID;
      EventData = a_pData;
    }

    // clone function implementing Clonable< T > interface
    public void clone(BrainEvent a_pSource)
    {
      EventID = a_pSource.EventID;
      EventData = a_pSource.EventData;
    }
    #endregion // PUBLIC METHODS
  }

  public class Brain : MonoBehaviour
  {
    #region PUBLIC DATA MEMBERS
    // brain type
    public BrainTypes BrainType = BrainTypes.BRAIN_TYPES_UNKNOWN;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    // register the character model
    public Character CharacterModel
    {
      set
      {
        m_pCharacterModel = value;
      }
    }

    #region PUBLIC OVERRIDDENS
    /**
     * initialize this brain
     * NOTE:m_bIsInitialized MUST be SET in doInitialize() method of derived class
     */
    public virtual void doInitialize(Character a_pCharacterModel)
    {
      // sanity checking
      // check brain type
      if(BrainTypes.BRAIN_TYPES_UNKNOWN == BrainType)
      {
        DefaultGameLogger.Instance.dumpLogError("[Brain][doInitialize][" + gameObject.ToString() + "]: No Brain Type");

        return;
      }

      // store character model
      m_pCharacterModel = a_pCharacterModel;
    }

    /**
     * destroy this brain
     * NOTE:m_bIsInitialized MUST be CLEARED in doDestroy() method in derived class
     */
    public virtual void doDestroy()
    {
      
    }

    /**
     * called when a collision happened
     */
    public virtual void onCollision(Entity a_pTrigger,                   // who has triggered this Hit
                                    CollisionHandler a_pCollidedPart     // collided part
                                   )
    {

    }

    /**
     * called when a collision happened and the mounted character can handle this collision
     */
    public virtual void onExitCollision(Entity a_pTrigger,                   // who has triggered this Hit
                                        CollisionHandler a_pCollidedPart     // collided part
                                       )
    {

    }

    /**
     * called when doHeadToCom() has ended
     */
    public virtual void onHeadToEnded()
    {

    }

    #region CHARACTER STATE CHANGE END CALLBACKS
    /**
     * called when Spawn animation has done: keyframed animation or simulated animation
     * normally brain is supposed to submit: doIdleCom()
     */
    public virtual void onSpawningEnded()
    {

    }

    /**
     * called when UnSpawn animation has done: JUST simulated
     */
    public virtual void onUnSpawningEnded()
    {
      
    }

    /**
     * called when Hit animation has done: Hit Animation will go automatically to Idle state when done
     * normally brain is supposed to submit: doIdleCom()
     */
    public virtual void onHitEnded()
    {
      
    }

    /**
     * called when Struck animation has done
     * normally brain is supposed to submit: doIdleCom()
     */
    public virtual void onStruckEnded()
    {
      
    }

    /**
     * called when Dead animation has done
     * normally brain is supposed to submit: doIdleCom()
     */
    public virtual void onDeadEnded()
    {
      
    }

    /**
     * called when Jump animation has done
     * normally brain is supposed to submit: doIdleCom()
     */
    public virtual void onJumpEnded()
    {
      
    }

    /**
     * called when Leap animation has done
     * normally brain is supposed to submit: doIdleCom()
     */
    public virtual void onLeapEnded()
    {
      
    }

    /**
     * called when Attack or Melee animation has done
     * normally brain is supposed to submit: doIdleCom() then doAttackCom() based on some sort of strategy and current state
     */
    public virtual void onAttackEnded()
    {
      
    }

    /**
     * called when Target Position has been reached
     * normally brain is supposed to submit: doIdleCom() then doAttackCom() based on some sort of strategy and current state
     */
    public virtual void onTargetPosReached()
    {
      
    }
    #endregion // CHARACTER STATE CHANGE END CALLBACKS

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
    protected virtual void doResetInEditor()
    {
      
    }

    /**
     * doUpdate() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last doUpdate() call
     * check true == m_bIsInitialized before doing the real thing
     */
    protected virtual void doUpdate()
    {

    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    // character model
    protected Character m_pCharacterModel;
    // initialization flag
    protected bool m_bIsInitialized;
    #endregion // PROTECTED DATA MEMBERS

    #region PRIVATE METHODS
    /**
     * Reset() is called when the user hits the Reset button in the Inspector's context menu
     * or when adding the component the first time 
     * This function is only called in editor mode 
     * Reset is most commonly used to give good default values in the inspector
     */
    void Reset()
    {
      doResetInEditor();
    }

    /**
     * Update() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last Update() call
     */
    void Update()
    {
      doUpdate();
    }
    #endregion // PRIVATE METHODS
  }

} // namespace Game.Logic

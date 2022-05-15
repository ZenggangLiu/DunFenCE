using UnityEngine;
using Framework.Logger;

namespace Game.Logic
{
  public class Boss : Monster
  {
    #region PUBLIC DATA MEMBERS
    // animation trigger name list
    // rage mode animations: loop animation list: need to call doIdleCom() to get out of the animation
    public AttackAnimationData[] RageAnimations = null;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS

    #region PUBLIC OVERRIDDENS
    /**
     * rage attack
     */
    public virtual void doRageAttackCom(AnimationData a_pRageAnimation)
    {
      // already in rage attacking state: return
      if(EntityStates.ENTITY_STATES_RAGE_ATTACKING == m_eCurEntityState)
      {
        return;
      }

      // stop using any weapon
      onStopUseWeapon(-1);

      // NO need to push down state: just use the pushed state from HIT

      // clear the stored rage attack animation data
      m_pLastRageAttackAnimationData = null;

      // play the rage animation: turn on its flag
      if(true == playAnimationFlag(a_pRageAnimation))
      {
        // store the latest played attack animation data
        m_pLastAttackAnimationData = a_pRageAnimation as AttackAnimationData;

        // store the latest played rage attack animation data
        m_pLastRageAttackAnimationData = a_pRageAnimation;
      }

      // store old state
      m_ePrevEntityState = m_eCurEntityState;

      // change to rage attacking state
      m_eCurEntityState = EntityStates.ENTITY_STATES_RAGE_ATTACKING;
    }

    /**
     * UnRage attack
     */
    public virtual void doUnRageAttackCom()
    {
      if(EntityStates.ENTITY_STATES_RAGE_ATTACKING != m_eCurEntityState)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "]: NOT in Rage Attack");

        return;
      }

      // stop the current rage attack animation
      stopRageAttackAnimation();

      // handle the attack ending
      if(null != m_pMountedBrain)
      {
        // let brain handle this event
        m_pMountedBrain.onAttackEnded();
      }
      else
      {
        // for NO brain case
        doIdleCom();
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
      base.doResetInEditor();

      // set correct character type
      CharacterType = CharacterTypes.CHARACTER_TYPES_BOSS;
    }

    // initialize this entity: called within doSpawnCom() only
    // m_bIsInitialized will be SET in this method
    protected override void doInitialize()
    {
      base.doInitialize();

      // clear initialization flag before checking
      m_bIsInitialized = false;
      // sanity checking
      // must have brain
      if(null == m_pMountedBrain) // got within base class
      {
        DefaultGameLogger.Instance.dumpLogError("[Boss][doInitialize][" + gameObject.ToString() + "]: No mounted Brain");

        return;
      }

      // set initialization flag
      m_bIsInitialized = true;
    }
    #endregion // PROTECTED OVERRIDDENS

    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    #endregion // PROTECTED DATA MEMBERS
  }

} // namespace Game.Logic

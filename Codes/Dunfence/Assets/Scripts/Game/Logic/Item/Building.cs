/**
 * Base Class of all Scene Buildings: Barrack, Black Smith
 */
using UnityEngine;
using Framework.Logger;

namespace Game.Logic
{
  public class Building : Entity
  {
    #region PUBLIC DATA MEMBERS
    // maximal health points: <=0 means NO health
    public int MaxHealth = -1;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    /**
     * return current integer HP
     */
    public int CurrentHP
    {
      get
      {
        return (int)(m_fCurHP + 0.5f); // round up the floating point HP
      }
    }

    /**
     * get/set current attacker count
     */
    public int CurrentAttackerCounter
    {
      get
      {
        return m_iCurAttackerCount;
      }

      set
      {
        m_iCurAttackerCount = value;
      }
    }

    #region PUBLIC OVERRIDDENS
    /**
     * gain HP
     */
    public virtual void gainHP(float a_fGainedHP)
    {
      
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
      EntityType = EntityTypes.ENTITY_TYPES_BUILDING;
    }

    // initialize this entity: called within doSpawnCom() only
    // m_bIsInitialized will be SET in this method
    protected override void doInitialize()
    {
      base.doInitialize();

      // clear initialization flag before checking
      m_bIsInitialized = false;

      // check if the current game object is in Buildings layer
      if(gameObject.layer != LayerMask.NameToLayer("Buildings"))
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "]: Building is NOT in Buildings Layer");

        return;
      }

      // check if each collider is trigger and has CollisionHandler.cs attached
      // get all colliders
      Collider[] _aColliderList = GetComponentsInChildren< Collider >();

      // iterate over all colliders
      for(int _iIdx = 0; _iIdx < _aColliderList.Length; _iIdx++)
      {
        // make sure each collider is of Trigger type
        if(false == _aColliderList[_iIdx].isTrigger)
        {
          DefaultGameLogger.Instance.dumpLogError("[" + _aColliderList[_iIdx].ToString() + "]: Building Collider is NOT a Trigger");
        }

        // make sure each collider also has attached CollisionHanlder.cs
        if(null == _aColliderList[_iIdx].GetComponentInChildren< CollisionHandler >())
        {
          DefaultGameLogger.Instance.dumpLogError("[" + _aColliderList[_iIdx].ToString() + "]: Building Collider does NOT have attached a CollisionHanlder.cs");
        }
      }

      // clear attacker count
      m_iCurAttackerCount = 0;

      // store HP given
      m_fCurHP = MaxHealth;

      // set initialization flag
      m_bIsInitialized = true;
    }

    // simulate the spawn animation
    protected override void doSpawnAnimation()
    {
      
    }
    #endregion // PROTECTED OVERRIDDENS 

    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    // current HP
    protected float m_fCurHP;
    // current atacker count: monsters + boss
    protected int m_iCurAttackerCount;
    #endregion // PROTECTED DATA MEMBERS
  }

} // namespace Game.Logic

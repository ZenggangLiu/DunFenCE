/**
 * this class defines the attributes of a hit object & its handler
 * It needs the following values:
 * - Type: Building, Body, Head,...
 * - Reward Point
 * - TopMost Node: where script can find the top most node: it will use this to find the Entity.cs script if needed 
 */

using UnityEngine;
using Framework.Utils;
using Framework.Logger;

namespace Game.Logic
{
  public enum HitObjectTypes
  {
    HIT_OBJECT_TYPES_UNKNOWN = 0,
    HIT_OBJECT_TYPES_BUILDING,       // the building of scene: destroy walls, barrack,...
    HIT_OBJECT_TYPES_PROP,           // the props
    HIT_OBJECT_TYPES_HEAD,           // the head part of a character
    HIT_OBJECT_TYPES_BODY,           // the body part of a character
    HIT_OBJECT_TYPES_LIMB,           // the leg or arm part of a character
  }
  
  public class CollisionHandler : MonoBehaviour
  {
    #region PUBLIC DATA MEMBERS
    // type of this hit object
    public HitObjectTypes Type = HitObjectTypes.HIT_OBJECT_TYPES_UNKNOWN;
    // reward points for this hit object
    public int Rewards = 0;
    // topmost node
    public GameObject TopMostNode = null;
    #endregion // PUBLIC DATA MEMBERS

    #region PROTECTED DATA MEMBERS
    // entity script component
    protected Entity m_pEntityComp = null;
    #endregion // PROTECTED DATA MEMBERS

    #region PRIVATE METHODS 
    /**
   * Start() will be called when the script is ENABLED
   * Start() will be called exactly once in the lifetime of the script
   */
    void Start()
    {
      // sanity checking
      // check hit object type
      if(HitObjectTypes.HIT_OBJECT_TYPES_UNKNOWN == Type)
      {
        DefaultGameLogger.Instance.dumpLogError("[CollisionHandler][Start][" + gameObject.ToString() + "]: No Hit Object Type");
        
        return;
      }
      
      // check rewards
      if(Rewards < 0)
      {
        DefaultGameLogger.Instance.dumpLogError("[CollisionHandler][Start][" + gameObject.ToString() + "]: Negative Rewards");
        
        return;
      }

      // check topmost node
      if(null == TopMostNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[CollisionHandler][Start][" + gameObject.ToString() + "]: No TopMost Node");
        
        return;
      }

      m_pEntityComp = TopMostNode.GetComponentInChildren < Entity >();

        // check bounding box
      if(null == gameObject.GetComponent < BoxCollider >())
      {
        DefaultGameLogger.Instance.dumpLogError("[CollisionHandler][Start][" + gameObject.ToString() + "]: No Bounding Box");
        
        return;
      }

      // confirm the bounding box is a trigger
      if(false == gameObject.GetComponent < BoxCollider >().isTrigger)
      {
        DefaultGameLogger.Instance.dumpLogError("[CollisionHandler][Start][" + gameObject.ToString() + "]: Bounding Box is NOT a Trigger");
        
        return;
      }
    }

    /**
     * OnTriggerEnter() is called when the a_pCollider enters trigger of this game object
     * in Frame 1, the a_pCollider is OUTSIDE of this Trigger
     * in Frame 2, the a_pCollider is INSIDE  of this Trigger
     * NOTE: 
     * 1. either Collidee or Collider MUST have a Rigidbody attached
     * 2. set Rigidbody as Kinematic to avoid having physical interaction with other objects
     */
    void OnTriggerEnter(Collider a_pTrigger /* who triggered this event */)
    {
      // if there is NO entity component: return
      if(null == m_pEntityComp)
      {
        return;
      }

      // try to get trigger's entity component
      Entity _pTriggerEntityComp = a_pTrigger.gameObject.GetComponentInChildren < Entity >();
      // trigger has NO entity: strange but return
      if(null == _pTriggerEntityComp)
      {
        return;
      }

      // else call onCollision() of the current entity
      m_pEntityComp.onCollision(_pTriggerEntityComp, this);
    }

    /**
     * OnTriggerExit() is called when the a_pCollider exited trigger of this game object
     * in Frame 1, the a_pCollider is INSIDE  of this Trigger
     * in Frame 2, the a_pCollider is OUTSIDE of this Trigger
     * NOTE: 
     * 1. either Collidee or Collider MUST have a Rigidbody attached
     * 2. set Rigidbody as Kinematic to avoid having physical interaction with other objects
     */
    void OnTriggerExit(Collider a_pTrigger /* who triggered this event */)
    {
      // if there is NO entity component: return
      if(null == m_pEntityComp)
      {
        return;
      }

      // try to get trigger's entity component
      Entity _pTriggerEntityComp = a_pTrigger.gameObject.GetComponentInChildren < Entity >();
      // trigger has NO entity: strange but return
      if(null == _pTriggerEntityComp)
      {
        return;
      }

      // else call onExitCollision() of the current entity
      m_pEntityComp.onExitCollision(_pTriggerEntityComp, this);
    }
    #endregion //PRIVATE METHODS 
  }

} // namespace Game.Logic

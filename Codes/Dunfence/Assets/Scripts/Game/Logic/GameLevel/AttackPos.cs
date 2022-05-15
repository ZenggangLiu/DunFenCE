/**
 * AttackPos represents any Attack Position as stored beneath AttackPosList of each Building asset
 */

using UnityEngine;
using Framework.Logger;
using Game.Data;

namespace Game.Logic
{
  public class AttackPos : MonoBehaviour
  {
    #region PUBLIC DATA MEMBERS
    public AttackPosData PosData = null;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    /**
     * return true, if the current attack position can accomodate monster shooting
     */
    public bool IsSupportingShoot
    {
      get
      {
        return PosData.SupportShoot;
      }
    }

    /**
     * get/set current attacker count
     */
    public int CurrentAttackerCount
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

    /**
     * initialize this attack pos
     */
    public void doInitialize()
    {
      if(null == PosData)
      {
        DefaultGameLogger.Instance.dumpLogError("[AttackPos][doInitialize][" + gameObject.ToString() + "]: NO Position Data");

        return;
      }

      // clear attacker count
      m_iCurAttackerCount = 0;

      // set initialization flag
      m_bIsInitialized = true;
    }

    /**
     * Update() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last Update() call
     * check false == m_bIsInitialized before doing the real thing
     */
    public void Update()
    {
      // NOT initialized: return
      if(false == m_bIsInitialized)
      {
        return;
      }      
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE DATA MEMBERS
    // current attacker count: monsters + boss
    int m_iCurAttackerCount;
    // initialization flag
    bool m_bIsInitialized;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Logic

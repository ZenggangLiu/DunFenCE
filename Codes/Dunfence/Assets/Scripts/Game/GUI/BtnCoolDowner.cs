/**
 * Cooldown spin of a button
 */

using UnityEngine;
using UnityEngine.UI;
using Framework.Logger;

namespace Game.GUI
{
  public class BtnCoolDowner : MonoBehaviour
  {
    #region PUBLIC METHODS
    /**
     * start spinning
     */
    public void startSpin(float a_fSpinTime)
    {
      // store spin time
      m_fTotalSpinTime = a_fSpinTime;

      // set current spin time
      m_fCurCountDownTime = m_fTotalSpinTime;

      // set radial pie
      setRadialPie();

      // set spin flag
      m_bIsSpinning = true;
    }

    /**
     * Initialize function
     * Start() will be called exactly ONCE in the lifetime of the script
     * Start() will be called in TWO cases:
     * - Attached to any GameObject:
     *   + Start() will be called when the script is ENABLED
     * - Called directly from other code: 
     *   + do the initialization
     */
    public void Start()
    {
      m_pCoverGlass = GetComponentInChildren< Image >();
      if(null == m_pCoverGlass)
      {
        DefaultGameLogger.Instance.dumpLogError("[BtnCoolDowner][Start][" + gameObject.ToString() + "]: does NOT have Image component");

        return;
      }

      // reset spin flag
      m_bIsSpinning = false;
    }

    /**
     * Update function: called each frame
     * Update() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last Update() call
     */
    public void Update()
    {
      // NOT spinning: return
      if(false == m_bIsSpinning)
      {
        return;
      }

      // decrease countdown time
      m_fCurCountDownTime -= Time.deltaTime;

      // set radial pie
      setRadialPie();

      // done with spin
      if(m_fCurCountDownTime < 1.0e-5f)
      {
        // reset spin flag
        m_bIsSpinning = false;
      }
    }
    #endregion // PUBLIC METHODS

    // set fill part of the radial pie
    void setRadialPie()
    {
      m_pCoverGlass.fillAmount = m_fCurCountDownTime / m_fTotalSpinTime;
    }

    // the radial spinner/pie
    Image m_pCoverGlass;
    // total spin time
    float m_fTotalSpinTime;
    // current count down time
    float m_fCurCountDownTime;
    // spin flag
    bool m_bIsSpinning;
  }

} // namespace Game.GUI

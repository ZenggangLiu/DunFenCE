/**
 * HighLight an Icon within Icon Node between Normal Color and HighLight Color
 */

using UnityEngine;
using UnityEngine.UI;
using Game.Logic;
using Framework.Utils;
using Framework.Logger;

namespace Game.GUI
{
  public class IconHighLighter : MonoBehaviour
  {
    #region PUBLIC DATA MEMBERS
    // high light color
    public Color HighLightColor;
    // flash time
    public float FlashTime = 0.6f;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    /**
     * toggle flashing
     */
    public void toggleFlashing()
    {
      if(true == m_bIsFlashing)
      {
        stopFlashing();
      }  
      else
      {
        startFlashing();
      }
    }

    /**
     * start flashing
     */
    public void startFlashing()
    {
      // set time mark
      m_fPrevTimeMark = Time.realtimeSinceStartup;

      // reset accumulated time
      m_fCurAccuFlashTime = 0.0f;

      // set flop flag
      m_iFlipDir = 1;

      // set flag
      m_bIsFlashing = true;
    }

    /**
     * stop flashing
     */
    public void stopFlashing()
    {
      // reset to normal color
      m_pIcon.color = m_pNormalColor;

      m_bIsFlashing = false;
    }

    /**
     * initialize Icon High Lighter
     */
    public void doInitialize()
    {
      // try to get Icon Node
      GameObject _pIconNode = GameObjectUtils.FindNode(gameObject, "Icon");
      if(null == _pIconNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "]: node does NOT have a Icon node");

        return;
      }

      m_pIcon = _pIconNode.GetComponentInChildren< Image >();
      if(null == m_pIcon)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "]: 's Icon node does NOT have a Image component");

        return;
      }

      // store its normal color
      m_pNormalColor = m_pIcon.color;

      // not working from start up
      m_bIsFlashing = false;

      m_bIsInitialized = true;
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    /**
     * Update() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last Update() call
     * check true == m_bIsInitialized before doing the real thing
     */
    void Update()
    {
      // if NOT initialized: return
      if(false == m_bIsInitialized)
      {
        return;
      }

      // NOT working: return
      if(false == m_bIsFlashing)
      {
        return;
      }

      // it also works if game's time is paused
      // get delta time
      float _fTimeDelta = Time.realtimeSinceStartup - m_fPrevTimeMark;

      // increase current flash time
      m_fCurAccuFlashTime += m_iFlipDir * _fTimeDelta;

      // accumulated time MUST be in [0, FlashTime]
      if(m_fCurAccuFlashTime > FlashTime)
      {
        // set maximal time
        m_fCurAccuFlashTime = FlashTime;

        // flip the direction
        m_iFlipDir = -m_iFlipDir;
      }

      else if(m_fCurAccuFlashTime < 1.0e-5f)
      {
        // set the minimal time
        m_fCurAccuFlashTime = 0.0f;

        // flip the direction
        m_iFlipDir = -m_iFlipDir;
      }

      // interplate between normal and highlight color
      float _fRatio = m_fCurAccuFlashTime / FlashTime;

      m_pIcon.color = Color.Lerp(m_pNormalColor, HighLightColor, _fRatio);

      // update time mark
      m_fPrevTimeMark = Time.realtimeSinceStartup;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // button's icon image
    Image m_pIcon;
    // normal color
    Color m_pNormalColor;
    // previous time mark
    float m_fPrevTimeMark;
    // currently accumulated flash time
    float m_fCurAccuFlashTime;
    // current flip direction
    int m_iFlipDir; // +1, -1, +1
    // indicate if this button is currently flashing
    bool m_bIsFlashing;
    // initialization flag
    bool m_bIsInitialized;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.GUI

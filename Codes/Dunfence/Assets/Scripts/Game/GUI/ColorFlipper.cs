/**
 * Flip between its original color and given color using smoothing
 */
using UnityEngine;
using UnityEngine.UI;
using Game.Logic;
using Framework.Utils;
using Framework.Logger;

namespace Game.GUI
{
  public class ColorFlipper : MonoBehaviour
  {
    #region PUBLIC DATA MEMBERS
    // secondary color: primary color <--> secondary color
    public Color SecondaryColor;
    // flip time: primary color --> wait for FlipTime --> secondary color --> wait for FlipTime --> primary color
    public float FlipTime = 1.0f;
    // if using smoothing
    public bool Smooth = false;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    /**
     * start flippping
     */
    public void startFlipping()
    {
      // set time mark
      m_fPrevTimeMark = Time.realtimeSinceStartup;

      // reset accumulated time
      m_fCurAccuFlipTime = 0.0f;

      // set flip direction
      m_iFlipDir = 1;

      // set flipping flag
      m_bIsFlipping = true;
    }

    /**
     * stop flipping
     */
    public void stopFlipping()
    {
      // reset to primary color
      m_pImage.color = m_pPrimaryColor;

      m_bIsFlipping = false;
    }

    /**
     * initialize this color flipper
     */
    public void doInitialize()
    {
      m_pImage = GetComponentInChildren< Image >();
      if(null == m_pImage)
      {
        DefaultGameLogger.Instance.dumpLogError("[ColorFipper][doInitialize][" + gameObject.ToString() + "]: does NOT have an Image component");

        return;
      }

      // store its primary color
      m_pPrimaryColor = m_pImage.color;

      // not working from start up
      m_bIsFlipping = false;

      // set initalization flag
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
      if(false == m_bIsFlipping)
      {
        return;
      }

      // it also works if game's time is paused
      // get delta time
      float _fTimeDelta = Time.realtimeSinceStartup - m_fPrevTimeMark;

      // increase current flip time
      m_fCurAccuFlipTime += m_iFlipDir * _fTimeDelta;

      // NOT smooth mode
      if(false == Smooth)
      {
        // get the correct color on flipping moment
        if(m_fCurAccuFlipTime > FlipTime
           ||
           m_fCurAccuFlipTime < 1.0e-5f
          )
        {
          m_pImage.color = m_iFlipDir > 0.0f ? m_pPrimaryColor : SecondaryColor;
        }
      }

      // accumulated time MUST be in [0, FlipTime]
      if(m_fCurAccuFlipTime > FlipTime)
      {
        // set maximal time
        m_fCurAccuFlipTime = FlipTime;

        // flip the direction
        m_iFlipDir = -m_iFlipDir;
      }

      else if(m_fCurAccuFlipTime < 1.0e-5f)
      {
        // set the minimal time
        m_fCurAccuFlipTime = 0.0f;

        // flip the direction
        m_iFlipDir = -m_iFlipDir;
      }

      // for smooth mode
      if(true == Smooth)
      {
        // interplate between primary and secondary color
        float _fRatio = m_fCurAccuFlipTime / FlipTime;

        m_pImage.color = Color.Lerp(m_pPrimaryColor, SecondaryColor, _fRatio);
      }

      // update time mark
      m_fPrevTimeMark = Time.realtimeSinceStartup;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // image component
    Image m_pImage;
    // primary color
    Color m_pPrimaryColor;
    // previous time mark
    float m_fPrevTimeMark;
    // currently accumulated flip time
    float m_fCurAccuFlipTime;
    // current flip direction
    int m_iFlipDir; // +1, -1, +1
    // flipping flag
    bool m_bIsFlipping;
    // initalization flag
    bool m_bIsInitialized;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.GUI

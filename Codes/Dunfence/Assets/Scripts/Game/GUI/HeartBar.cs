using UnityEngine;
using UnityEngine.UI;
using Framework.Utils;
using Framework.Logger;
using Game.Logic;

namespace Game.GUI
{
  public class HeartBar : MonoBehaviour
  {
    #region PUBLIC METHODS
    /** 
     * get and set the shown flag
     */
    public bool IsShown
    {
      get
      {
        return m_bIsShown;
      }

      set
      {
        m_bIsShown = value;
      }
    }

    /**
     * get and set maximal value
     */
    public float MaxValue
    {
      get
      {
        return m_fMaxValue;
      }

      set
      {
        if(value < 1.0e-5f)
        {
          DefaultGameLogger.Instance.dumpLogError("[HeartBar][MaxValue][" + gameObject.ToString() + "][HeartBar]: Try to set TOO Tiny maximal value");

          return;
        }

        m_fMaxValue = value;

        // update progress
        setProgress(m_fCurValue);
      }
    }

    /**
     * get and set current value
     */
    public float CurrentValue
    {
      get
      {
        return m_fCurValue;
      }

      set
      {
        setProgress(value);
      }
    }

    // constructor
    public HeartBar()
    {
      m_bIsInitialized = false;
      m_bIsShown = false;
      m_pHeartUI = null;
      m_pCamera = null;
      m_pChasedObject = null;
      m_bIsChasing = false;
    }

    /**
     * initialize this heart bar with the given maximal value
     */
    public void doInitialize(float a_fMaxValue = 0.01f)
    {
      // try to find the heart bar node
      GameObject _pHeartNode = GameObjectUtils.FindNode(gameObject, "Heart");
      if(null == _pHeartNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[HeartBar][doInitialize][" + gameObject.ToString() + "]: Can NOT find Heart Node");

        return;
      }

      // try to find the heart UI
      m_pHeartUI = _pHeartNode.GetComponentInChildren< Image >();
      if(null == m_pHeartUI)
      {
        DefaultGameLogger.Instance.dumpLogError("[HeartBar][doInitialize][" + gameObject.ToString() + "]: Heart node does NOT have a Image component");

        return;
      }

      // check heart UI image type: Filled
      if(Image.Type.Filled != m_pHeartUI.type)
      {
        DefaultGameLogger.Instance.dumpLogError("[HeartBar][doInitialize][" + gameObject.ToString() + "]: Heart UI does NOT have Filled Image Type");

        return;
      }

      // store current value with maximal value
      m_fCurValue = a_fMaxValue;

      // update maximal value and progress
      MaxValue = a_fMaxValue;

      m_bIsInitialized = true;
    }

    /**
     * destroy this heart bar
     */
    public void doDestroy()
    {
      m_bIsShown = false;
      m_pHeartUI = null;
      m_pCamera = null;
      m_pChasedObject = null;
      m_bIsChasing = false;
      m_bIsInitialized = false;
    }

    // set the current progress
    public void setProgress(float a_fCurVal)
    {
      // store current value
      m_fCurValue = a_fCurVal;

      // if current value near zero: set fill amount == 0
      if(a_fCurVal < 1.0e-5f)
      {
        a_fCurVal = 0.0f;

        m_pHeartUI.fillAmount = 0.0f;
      }

      // if current value greater than maximal value: set fill amount == 1
      else if(a_fCurVal > m_fMaxValue)
      {
        a_fCurVal = m_fMaxValue;

        m_pHeartUI.fillAmount = 1.0f;
      }

      // get the percentage
      else
      {
        m_pHeartUI.fillAmount = a_fCurVal / m_fMaxValue;
      }
    }

    // start chasing the given game object
    public void startChase(GameObject a_pChasePoint)
    {
      // get camera
      m_pCamera = GameLevelMgr.Instance.GameMainLevel.CameraNode.GetComponentInChildren< UnityEngine.Camera >();

      m_pChasedObject = a_pChasePoint;

      if(m_pChasedObject != null)
      {
        // get the screen position of the chased object
        Vector3 _vScreenPos = m_pCamera.WorldToScreenPoint(m_pChasedObject.transform.position);
        _vScreenPos.z = 0.0f;

        GetComponentInChildren< RectTransform >().position = _vScreenPos;
      }

      m_bIsChasing = true;
    }

    // stop chasing any game object
    public void stopChase()
    {
      m_bIsChasing = false;

      m_pChasedObject = null;

      m_pCamera = null;
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

      // if this heart bar is NOT shown: return
      if(false == m_bIsShown)
      {
        return;
      }

      if(false == m_bIsChasing          // NO chasing
        ||                              // or
         null == m_pChasedObject        // chased object is null
        )
      {
        return;
      }

      // get the screen position of the chased object
      Vector3 _vScreenPos = m_pCamera.WorldToScreenPoint(m_pChasedObject.transform.position);
      _vScreenPos.z = 0.0f;

      GetComponentInChildren< RectTransform >().position = _vScreenPos;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // the heart image UI
    Image m_pHeartUI;
    // maximal value
    float m_fMaxValue;
    // current value
    float m_fCurValue;
    // camera used for chasing object
    UnityEngine.Camera m_pCamera;
    // the game object being chased
    GameObject m_pChasedObject;
    // chasing flag
    bool m_bIsChasing;
    // initialization flag
    bool m_bIsInitialized;
    // shown flag
    bool m_bIsShown;
    #endregion // PRIVATE DATA MEMBERS    
  }

} // namespace Game.GUI

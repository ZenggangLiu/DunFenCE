using UnityEngine;
using UnityEngine.UI;
using Framework.Utils;
using Framework.Logger;
using Game.Logic;

namespace Game.GUI
{
  public class TowerTimerBar : MonoBehaviour
  {
    #region PUBLIC DATA MEMBERS
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    // constructor
    public TowerTimerBar()
    {
      m_bIsInitialized = false;
      m_fCurShowTime = 1.0f;
      m_fTotalShowTime = 1.0f;
      m_pTimerUsageUI = null;
      m_pCamera = null;
      m_pMountPointObject = null;
    }

    /**
     * initialize this timer bar
     */
    public void doInitialize()
    {
      // try to find the timer usage game object
      GameObject _pTimerUsageGO = GameObjectUtils.FindNode(gameObject, "Usage");
      if(null == _pTimerUsageGO)
      {
        DefaultGameLogger.Instance.dumpLogError("[TowerTimerBar][doInitialize][" + gameObject.ToString() + "]: Can NOT find Usage Node");

        return;
      }

      // try to find the timer usage image UI
      m_pTimerUsageUI = _pTimerUsageGO.GetComponentInChildren< Image >();
      if(null == m_pTimerUsageUI)
      {
        DefaultGameLogger.Instance.dumpLogError("[TowerTimerBar][doInitialize][" + gameObject.ToString() + "]: Usage node does NOT have a Image component");

        return;
      }

      // make sure fill type is Filled
      if(Image.Type.Filled != m_pTimerUsageUI.type)
      {
        DefaultGameLogger.Instance.dumpLogError("[TowerTimerBar][doInitialize][" + gameObject.ToString() + "]: Usage node does NOT have Filled Image Type");

        return;
      }

      // make sure fill method is Radial 360
      if(Image.FillMethod.Radial360 != m_pTimerUsageUI.fillMethod)
      {
        DefaultGameLogger.Instance.dumpLogError("[TowerTimerBar][doInitialize][" + gameObject.ToString() + "]: Usage node does NOT have Radial 360 Fill Method");

        return;
      }

      // set initial progress
      setProgress();

      m_bIsInitialized = true;
    }

    /**
     * destroy this timer bar
     */
    public void doDestroy()
    {
      m_pTimerUsageUI = null;
      m_pCamera = null;
      m_pMountPointObject = null;
      m_bIsInitialized = false;
    }

    /**
     * show this bar
     */
    public void showIt(float a_fTotalShowTime, GameObject a_pMountPointObject)
    {
      // store given total show time
      m_fTotalShowTime = a_fTotalShowTime;

      // set current show time to the given total show time
      m_fCurShowTime = a_fTotalShowTime;

      // store given mount point
      m_pMountPointObject = a_pMountPointObject;

      // get camera
      m_pCamera = GameLevelMgr.Instance.GameMainLevel.CameraNode.GetComponentInChildren< UnityEngine.Camera >();

      if(m_pMountPointObject != null)
      {
        // get the screen position of the mount point object
        Vector3 _vScreenPos = m_pCamera.WorldToScreenPoint(m_pMountPointObject.transform.position);
        _vScreenPos.z = 0.0f;

        // set the timer bar's position based on current camera
        GetComponentInChildren< RectTransform >().position = _vScreenPos;
      }

      // enable this timer bar
      gameObject.SetActive(true);
    }

    /**
     * hide this bar
     */
    public void hideIt()
    {
      // clear up the current show time
      m_fCurShowTime = 0.0f;

      // clear the mount point object
      m_pMountPointObject = null;

      // clear the camera object
      m_pCamera = null;

      // hide this timer bar
      gameObject.SetActive(false);
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
      // NOT initialized: return
      if(false == m_bIsInitialized)
      {
        return;
      }

      // if this timer bar is NOT shown: return
      if(false == gameObject.activeSelf)
      {
        return;
      }

      // if there is NO mount point object: return
      if(null == m_pMountPointObject)
      {
        return;
      }

      // decrease timer
      m_fCurShowTime -= Time.deltaTime;

      // set progress
      setProgress();

      // done with this timer bar: hide it
      if(m_fCurShowTime < 1.0e-5f)
      {
        hideIt();
      }

      // get the screen position of the mount point object
      Vector3 _vScreenPos = m_pCamera.WorldToScreenPoint(m_pMountPointObject.transform.position);
      _vScreenPos.z = 0.0f;

      GetComponentInChildren< RectTransform >().position = _vScreenPos;
    }

    // set the current progress based on current show time
    void setProgress()
    {
      // if any value is near zero: set fill amount == 0
      if(m_fCurShowTime < 1.0e-5f
         ||
         m_fTotalShowTime < 1.0e-5f
        )
      {
        m_pTimerUsageUI.fillAmount = 1.0f;
      }
      else if(m_fCurShowTime > m_fTotalShowTime)
      {
        m_pTimerUsageUI.fillAmount = 0.0f;
      }
      else
      {
        m_pTimerUsageUI.fillAmount = 1.0f - m_fCurShowTime / m_fTotalShowTime;
      }
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // indicate if this HP bar has been initialized already
    bool m_bIsInitialized;
    // the timer bar image UI
    Image m_pTimerUsageUI;
    // camera used for showing this timer bar
    UnityEngine.Camera m_pCamera;
    // total show time
    float m_fTotalShowTime;
    // current show time
    float m_fCurShowTime;
    // the mount point object
    GameObject m_pMountPointObject;
    #endregion // PRIVATE DATA MEMBERS    
  }
} // namespace Game.GUI

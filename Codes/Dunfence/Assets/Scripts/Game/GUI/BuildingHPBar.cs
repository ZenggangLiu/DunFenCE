using UnityEngine;
using UnityEngine.UI;
using Framework.Utils;
using Framework.Logger;
using Game.Logic;

namespace Game.GUI
{
  public class BuildingHPBar : MonoBehaviour
  {
    #region PUBLIC DATA MEMBERS
    public Color HealthColor    = new Color(0.5f, 0.925f, 0.0235f);
    public Color WeakColor      = new Color(1.0f, 0.788f, 0.0549f);
    public Color ExhaustedColor = new Color(1.0f, 0.498f, 0.1529f);
    public Color DeadColor      = new Color(1.0f, 0.1098f, 0.1411f);
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    // constructor
    public BuildingHPBar()
    {
      m_bIsInitialized = false;
      m_pHPUI = null;
      m_pCamera = null;
      m_pBuildingObject = null;
    }

    /**
     * initialize this heart bar
     */
    public void doInitialize()
    {
      // try to find the HP node
      GameObject _pHPNode = GameObjectUtils.FindNode(gameObject, "HP");
      if(null == _pHPNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[BuildingHPBar][doInitialize][" + gameObject.ToString() + "]: Can NOT find HP Node");

        return;
      }

      // try to find the HP image UI
      m_pHPUI = _pHPNode.GetComponentInChildren< Image >();
      if(null == m_pHPUI)
      {
        DefaultGameLogger.Instance.dumpLogError("[BuildingHPBar][doInitialize][" + gameObject.ToString() + "]: HP node does NOT have a Image component");

        return;
      }

      // check HP image UI type: Filled
      if(Image.Type.Filled != m_pHPUI.type)
      {
        DefaultGameLogger.Instance.dumpLogError("[BuildingHPBar][doInitialize][" + gameObject.ToString() + "]: HP UI does NOT have Filled Image Type");

        return;
      }

      // set 100% progress
      setProgress(1.0f, 1.0f);

      m_bIsInitialized = true;
    }

    /**
     * destroy this heart bar
     */
    public void doDestroy()
    {
      m_pHPUI = null;
      m_pCamera = null;
      m_pBuildingObject = null;
      m_bIsInitialized = false;
    }

    // shown flag
    public bool IsShown
    {
      get
      {
        return gameObject.activeSelf;
      }

      set
      {
        gameObject.SetActive(value);
      }
    }

    // set the current progress
    public void setProgress(float a_fCurVal, float a_fTotalVal)
    {
      // if any value is near zero: set fill amount == 0
      if(a_fCurVal < 1.0e-5f
        ||
        a_fTotalVal < 1.0e-5f
      )
      {
        m_pHPUI.fillAmount = 0.0f;
      }
      else if(a_fCurVal > a_fTotalVal)
      {
        m_pHPUI.fillAmount = 1.0f;
      }
      else
      {
        m_pHPUI.fillAmount = a_fCurVal / a_fTotalVal;
      }

      // adjust color according to current progress
      if(m_pHPUI.fillAmount > 0.6f)
      {
        m_pHPUI.color = HealthColor;
      }

      else if(m_pHPUI.fillAmount > 0.4f)
      {
        m_pHPUI.color = WeakColor;
      }

      else if(m_pHPUI.fillAmount > 0.25f)
      {
        m_pHPUI.color = ExhaustedColor;
      }
      else
      {
        m_pHPUI.color = DeadColor;
      }
    }

    // start chasing the given game object
    public void startChase(GameObject a_pChasePoint)
    {
      // get camera
      m_pCamera = GameLevelMgr.Instance.GameMainLevel.CameraNode.GetComponentInChildren< UnityEngine.Camera >();

      m_pBuildingObject = a_pChasePoint;

      if(m_pBuildingObject != null)
      {
        // get the screen position of the chased object
        Vector3 _vScreenPos = m_pCamera.WorldToScreenPoint(m_pBuildingObject.transform.position);
        _vScreenPos.z = 0.0f;

        GetComponentInChildren< RectTransform >().position = _vScreenPos;
      }
    }

    // stop chasing any game object
    public void stopChase()
    {
      m_pBuildingObject = null;

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
      // if this heart bar is NOT initialized: return
      if(false == m_bIsInitialized)
      {
        return;
      }

      // if this heart bar is NOT shown: return
      if(false == gameObject.activeSelf)
      {
        return;
      }

      if(null == m_pBuildingObject)
      {
        return;
      }

      // get the screen position of the chased object
      Vector3 _vScreenPos = m_pCamera.WorldToScreenPoint(m_pBuildingObject.transform.position);
      _vScreenPos.z = 0.0f;

      GetComponentInChildren< RectTransform >().position = _vScreenPos;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // the HP image UI
    Image m_pHPUI;
    // camera used for showing this HP bar
    UnityEngine.Camera m_pCamera;
    // the building game object
    GameObject m_pBuildingObject;
    // initialization flag
    bool m_bIsInitialized;
    #endregion // PRIVATE DATA MEMBERS    
  }
} // namespace Game.GUI

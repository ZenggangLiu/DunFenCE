/**
 * Camera Controller
 * - supports tilting with Accelerometer
 * - provides the Shoot Aim Helper interface
 */
// define SHOW_STATS in Player Settings Pannel to show stats
#define CALCULATE_FPS   // if calculate FPS

using System;
using UnityEngine;
using UnityEngine.UI;
using Framework.Utils;
using Framework.Mathe;
using Framework.Logger;
using Framework.Inputs;
using Game.Logic;
using System.Collections.Generic;

namespace Game.Camera
{
  public enum CameraModes
  {
    // regular 3D camera
    CAMERA_MODES_REGULAR,
    // AR camera
    CAMERA_MODES_AR_SUPPORT,
    // VR camera
    CAMERA_MODES_SUPPORT_VR,
  }

  public class CameraController : MonoBehaviour, ShootAimHelper
  {
    #region PUBLIC DATA MEMBERS
    // maximal horizontal rotate angle around up axis
    public float MaxHorzRotAngle = 60.0f;
    // maximal vertical rotate angle around right axis
    public float MaxVertRotAngle = 60.0f;
    // horizontal rotate scale
    public float HorzRotScale = 25.0f;
    // vertical rotate scale
    public float VertRotScale = 15.0f;

#if UNITY_EDITOR
    // move speed: units / second
    public float MoveSpeed = 0.05f;
    // rotation speed: degrees / second
    public float RotSpeed = 10.0f;
#endif // #if UNITY_EDITOR

    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    /**
     * return the aimed position
     * return true on success
     */
    public bool getAimedPos(out Vector3 a_vAimedPos)
    {
      // clear up returned aimed position
      a_vAimedPos = Vector3.zero;

      // get aimed entity list
      getAimedEntityList();

      // if there is some entity aimed at
      if(m_aAimedEntityList.Count > 0)
      {
        // if there is JUST one aimed entity
        if(1 == m_aAimedEntityList.Count)
        {
          a_vAimedPos = m_aAimedEntityList[0].transform.position;
        }
        else
        {
          // randomly select one entity
          GameObject _pChoosenEntity = m_aAimedEntityList[RandomNumberMgr.Instance.getIntegerVal(0, m_aAimedEntityList.Count -1)];

          // NO Render component, use its world location: maybe Foot Point
          if(_pChoosenEntity.GetComponentInChildren< Renderer >() == null)
          {
            DefaultGameLogger.Instance.dumpLogError("[" + _pChoosenEntity.ToString() + "]: has NO Render Component");

            a_vAimedPos = _pChoosenEntity.transform.position;
          }
          else
          {
            a_vAimedPos = _pChoosenEntity.GetComponentInChildren< Renderer >().bounds.center;  
          }
        }
      }
      else
      {
        // NO entity aimed at all
        // find the intersection of view direction and ground floor
        // t = -Eye_y / View_y
        float _fT = -transform.position.y / transform.forward.y;
        a_vAimedPos = transform.position + (transform.forward * _fT);
      }

      return true;
    }

    /**
     * reset this camera controller
     */
    public void doReset()
    {
      // unRegister event callback for tilt events
      DeviceInputMgr.Instance.unRegisterDeviceEventHandler(onDeviceEvent);

      // disable the attached real camera
      m_pCamera.enabled = false;

      m_pCamera = null;
      m_eCameraMode = CameraModes.CAMERA_MODES_REGULAR;
      m_pStatsText = null;
      m_pWeaponFrustum = null;
      if(m_aAimedEntityList != null)
      {
        m_aAimedEntityList.Clear();
        m_aAimedEntityList = null;
      }

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[CameraController][doReset]: [" + gameObject.ToString() + "]");
#endif
    }

    /**
     * initialize this camera controller
     */
    public void doInitialize()
    {
      // get currently attached camera
      m_pCamera = GetComponentInChildren< UnityEngine.Camera >();
      if(null == m_pCamera)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "]: Can NOT find Camera");
      }

      // store initial camera orientation
      m_qInitCameraOrien = transform.rotation;

      // clear weapon frustum
      m_pWeaponFrustum = null;

      // for defense level, create its own weapon frustum
      if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.DefenseLevelGUI)
      {
        // create weapon frustum in world space
        // get the aim pointer's radius in screen space
        float _fAimRadiusSS = (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.DefenseLevelGUI).AimRadius;

        // get the camear radius in screen space
        float _fCameraRadiusSS = m_pCamera.pixelWidth * 0.5f;

        // calculate the radius ratio
        // because affine transform will keep the ratios among points on a line: https://en.wikipedia.org/wiki/Affine_transformation#Properties
        // Ratios of vectors along a line; i.e., for distinct collinear points p_1, p_2, p_3, the ratio of \overrightarrow{p_1p_2} and \overrightarrow{p_2p_3} is the same as that of \overrightarrow{f(p_1)f(p_2)} and \overrightarrow{f(p_2)f(p_3)}.
        // N/R = m00 -> N = m00 * R
        // N/Ra = m00 * R/Ra = m00 * Rss/Rass = m00 * Ratio
        float _fRatio = _fCameraRadiusSS / _fAimRadiusSS;

        m_pWeaponFrustum = Frustum.createWeaponFrustumInWorldSpace(m_pCamera, _fRatio);
      }

      // check if we are using ARCamera
#if VUFORIA_ANDROID_SETTINGS || VUFORIA_IOS_SETTINGS
      m_eCameraMode = GetComponent < Vuforia.VuforiaBehaviour >() != null ? CameraModes.CAMERA_MODES_AR_SUPPORT : CameraModes.CAMERA_MODES_REGULAR;
#else
      m_eCameraMode = CameraModes.CAMERA_MODES_REGULAR;
#endif // #if VUFORIA_ANDROID_SETTINGS || VUFORIA_IOS_SETTINGS

      // if we are using normal camera: register tilt event handler
      if(CameraModes.CAMERA_MODES_REGULAR == m_eCameraMode)
      {
        // register event callback for tilt events
        DeviceInputMgr.Instance.registerDeviceEventHanlder(onDeviceEvent);
      }

#if VUFORIA_ANDROID_SETTINGS || VUFORIA_IOS_SETTINGS
      else
      {
        // AR mode: check all ImageTargetBehaviour in order to make sure their "Enable Extended Tracking" are enabled
        Vuforia.ImageTargetBehaviour[] _aImageTargetList = GameLevelMgr.Instance.GameMainLevel.SceneNode.GetComponentsInChildren< Vuforia.ImageTargetBehaviour >(true);
        for(int _iIdx = 0; _iIdx < _aImageTargetList.Length; _iIdx++)
        {
          // start extended tracking
          if(false == ((Vuforia.IEditDataSetBehaviour)_aImageTargetList[_iIdx]).ExtendedTracking)
          {
            DefaultGameLogger.Instance.dumpLogError("[" + _aImageTargetList[_iIdx].gameObject.ToString() + "]: Image Target has NOT enabled \"Enable Extended Tracking\" ! Turn on It please");
          }
        }
      }
#endif // #if VUFORIA_ANDROID_SETTINGS || VUFORIA_IOS_SETTINGS

      // clear measured frame time so far
      m_fMeasuredFrameTime = 0.0f;

      // if the system support GyroScope, enable it
      // NOTE: by default, the GyroScope is DISABLED
      if(true == SystemInfo.supportsGyroscope)
      {
        Input.gyro.enabled = true;
      }

      // try to find the stats text
      m_pStatsText = GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI.Stats;

      // create aimed entity list
      m_aAimedEntityList = new List< GameObject >(6);

      // enable the attached camera
      m_pCamera.enabled = true;

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("[CameraController][doInitialize]: [" + gameObject.ToString() + "]");
#endif
    }

    /**
     * update function
     */
    public void doUpdate()
    {
      /**
       *    GyroSocpe       ==>          Unity
       *        ^ Y                       ^ Y / Z
       *        |                         | /
       *        + ---> X                  + ---> X
       *      / 
       *    / Z
       *
       *   {<x, y, z>, w} ==> {<x, y, -z>, w} ==> {<-x, -y, z>, w} with negated angle and correct Z-axis
       */
#if SHOW_STATS
      // check if there is stats text field
      if(m_pStatsText != null)
      {
        m_pStatsText.text = "";
      }
#endif // SHOW_STATS

#if CALCULATE_FPS
      // FPS
      // update measured frame time so far
      // Time.deltaTime: The time in seconds it took to complete the last frame: Last Frame Time
      // normally deltaTime will be varying each time: for each frame
      // NewMeasuredFrameTime = CurrentMeasuredFrameTime + 10% * CurrentFrameTimeDelta
      m_fMeasuredFrameTime += (Time.deltaTime - m_fMeasuredFrameTime) * 0.1f; // consider the time vibration: but just consider 10% influence

      // show fps and cpu time
      // convert time from seconds into milli-seconds
      float _fFrameTimeMS = m_fMeasuredFrameTime * 1000.0f;

      // FPS: #Frame / Second: == One Second Over Frame Time
      float _fFPS = 0.0f;
      // m_fMeasuredFrameTime >> 0: reasonably larger than ZERO
      if(m_fMeasuredFrameTime > 1.0e-5f)
      {
        _fFPS = 1.0f / m_fMeasuredFrameTime;
      }
#endif // #if CALCULATE_FPS
      
#if SHOW_STATS
      // check if there is stats text field
      if(m_pStatsText != null)
      {
#if CALCULATE_FPS
        // FPS
        /**
         * public static string Format(string format, params object[] args)
         * take parameter list from args
         * format: {index[,alignment][:formatString]}
         * index: parameter specifier, is a number starting from 0 that identifies a corresponding item in the list of objects. 
         */
        m_pStatsText.text += string.Format("<color=lime>FPS:{0:0.0} | CPU Time:{1:0.0} ms</color>\n", _fFPS, _fFrameTimeMS);
#endif // #if CALCULATE_FPS
      }
#endif // SHOW_STATS

      // GyroScope
      // NO GyroScope
      if(false == SystemInfo.supportsGyroscope)
      {
#if SHOW_STATS
        if(m_pStatsText != null)
        {
          m_pStatsText.text += string.Format("<color=lime>[GyroScope]: </color><color=red>NO</color>\n");
        }
#endif // #if SHOW_STATS
      }
      else if(CameraModes.CAMERA_MODES_REGULAR == m_eCameraMode) // NOT using AR camera
      {
        // get current device orientation according to GyroScope: RHS with X facing to East, Y facing to North, Z facing to Sky
        /**
         *    Sky / Z
         *      ^
         *      |  /  North / Y
         *      | /
         *      |/      East
         *      +-------> X
         */
      }

      // TODO: find a way to show runtime stats on rendered mesh: MeshFilter.sharedMesh NOT working

      // adjust camera within Editor
#if UNITY_EDITOR

#if SHOW_STATS
      if(m_pStatsText != null)
      {
        m_pStatsText.text += "<color=lime>Move: Delete/PageDown(X), Home/End(Y), Insert/PageUp(Z) </color>\n<color=yellow>Rot: W/S(X), A/D(Y), Q/E(Z) </color>\n";
      }
#endif // SHOW_STATS

      // move keys: home/end along local Y, delete/pagedown along local X, insert/pageup along local Z
      // return true while the user holds down the key identified by name
      // up and down
      if(Input.GetKey(KeyCode.Home) == true)
      {
        transform.position += transform.up * MoveSpeed * Time.deltaTime;
      }

      if(Input.GetKey(KeyCode.End) == true)
      {
        transform.position -= transform.up * MoveSpeed * Time.deltaTime;
      }

      // left and right
      if(Input.GetKey(KeyCode.Delete) == true)
      {
        transform.position -= transform.right * MoveSpeed * Time.deltaTime;
      }

      if(Input.GetKey(KeyCode.PageDown) == true)
      {
        transform.position += transform.right * MoveSpeed * Time.deltaTime;
      }

      // in and out
      if(Input.GetKey(KeyCode.Insert) == true)
      {
        transform.position += transform.forward * MoveSpeed * Time.deltaTime;
      }

      if(Input.GetKey(KeyCode.PageUp) == true)
      {
        transform.position -= transform.forward * MoveSpeed * Time.deltaTime;
      }

      // rotation keys: w/s along X, a/d along Y, q/e along Z
      // up and down
      if(Input.GetKey(KeyCode.W) == true)
      {
        transform.localRotation *= Quaternion.AngleAxis(-RotSpeed * Time.deltaTime, Vector3.right);
      }

      if(Input.GetKey(KeyCode.S) == true)
      {
        transform.localRotation *= Quaternion.AngleAxis(RotSpeed * Time.deltaTime, Vector3.right);
      }

      // left and right
      if(Input.GetKey(KeyCode.A) == true)
      {
        transform.localRotation *= Quaternion.AngleAxis(-RotSpeed * Time.deltaTime, Vector3.up);
      }

      if(Input.GetKey(KeyCode.D) == true)
      {
        transform.localRotation *= Quaternion.AngleAxis(RotSpeed * Time.deltaTime, Vector3.up);
      }

      if(Input.GetKey(KeyCode.Q) == true)
      {
        transform.localRotation *= Quaternion.AngleAxis(RotSpeed * Time.deltaTime, Vector3.forward);
      }

      if(Input.GetKey(KeyCode.E) == true)
      {
        transform.localRotation *= Quaternion.AngleAxis(-RotSpeed * Time.deltaTime, Vector3.forward);
      }
#endif // UNITY_EDITOR
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    /**
     * OnDrawGizmos() will be called each frame to draw a Gizoms for the script component
     */
    void OnDrawGizmos()
    {
#if UNITY_EDITOR
      if(null == m_pCamera            // NO camera
         ||                           // or
         null == m_pWeaponFrustum     // NO weapon frustum
        )
      {
        return;
      }

      // set gizmos' color
      Gizmos.color = Color.yellow;

      // set location and orientation: same as the current camera
      Gizmos.matrix = m_pCamera.transform.localToWorldMatrix;

      // get projection matrix in view/eye space
      Matrix4x4 _pProjMat = m_pCamera.projectionMatrix;

      // get the aim pointer's radius in screen space
      float _fAimRadiusSS = (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.DefenseLevelGUI).AimRadius;

      // get the camear radius in screen space
      float _fCameraRadiusSS = m_pCamera.pixelWidth * 0.5f;

      // calculate the radius ratio
      // because affine transform will keep the ratios among points on a line: https://en.wikipedia.org/wiki/Affine_transformation#Properties
      // Ratios of vectors along a line; i.e., for distinct collinear points p_1, p_2, p_3, the ratio of \overrightarrow{p_1p_2} and \overrightarrow{p_2p_3} is the same as that of \overrightarrow{f(p_1)f(p_2)} and \overrightarrow{f(p_2)f(p_3)}.
      // N/R = m00 -> N = m00 * R
      // N/Ra = m00 * R/Ra = m00 * Rss/Rass = m00 * Ratio
      float _fRatio = _fCameraRadiusSS / _fAimRadiusSS;

      // modify the projection matrix according to the given aim radius
      /**
       * projection matrix used within unity: 
       * [n/r         0             0            0
       *   0         n/t            0            0
       *   0          0          f(f+n)/f-n   -2fn/f-n
       *   0          0            -1            0]
       * 
       * ==>
       * [n/R         0             0            0
       *   0         n/R            0            0
       *   0          0          f(f+n)/f-n   -2fn/f-n
       *   0          0            -1            0]
       * with R is the radius of the weapon's aim
       * 
       * n/r = m00 -> n = m00 * r
       * n/R = m00 * r/R = m00 * Ratio
       */
      _pProjMat.m00 = _pProjMat.m00 * _fRatio;
      _pProjMat.m11 = _pProjMat.m00;

      // calculate the vertical FOV
      float _fFOV = 2.0f * (float)Math.Atan(1.0f / _pProjMat.m11) * 180.0f / 3.1415f;

      Gizmos.DrawFrustum(Vector3.zero, _fFOV, m_pCamera.farClipPlane, m_pCamera.nearClipPlane, 1.0f);
#endif // #if UNITY_EDITOR
    }

    // set the given points into the given line renderer component
    void setLinePointsDebugDraw(LineRenderer a_pLineRendererComp, Framework.Mathe.Plane a_pClipPlane)
    {
      // store the normalized clip plane
      Framework.Mathe.Plane _pCurPlane = Framework.Mathe.Plane.normalizeAPlane(a_pClipPlane);

      // get tangent vector
      Vector3 _vTangent = Helper.getPerpendicularVector(_pCurPlane.getNormal());

      // get binormal vector
      Vector3 _vBinormal = Vector3.Cross(_vTangent, _pCurPlane.getNormal());

      // get the origin of the plane
      Vector3 _vOrigin = -_pCurPlane.d * _pCurPlane.getNormal();

      Vector3[] _aVertexList = new Vector3[5];
      _aVertexList[0] = _vOrigin - 2.0f * _vTangent - 2.0f * _vBinormal;
      _aVertexList[1] = _vOrigin + 2.0f * _vTangent - 2.0f * _vBinormal;
      _aVertexList[2] = _vOrigin + 2.0f * _vTangent + 2.0f * _vBinormal;
      _aVertexList[3] = _vOrigin - 2.0f * _vTangent + 2.0f * _vBinormal;
      _aVertexList[4] = _aVertexList[0];

      for(int _iIdx = 0; _iIdx < _aVertexList.Length; ++_iIdx)        
      {
        a_pLineRendererComp.SetPosition(_iIdx, _aVertexList[_iIdx]);
      }
    }

    /**
     * get all aimed entity
     * return true, if we have found some entity
     */
    bool getAimedEntityList()
    {
      // clear the given list
      m_aAimedEntityList.Clear();

      // get the aim pointer's radius in screen space
      float _fAimRadiusSS = (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.DefenseLevelGUI).AimRadius;

      // get the camear radius in screen space
      float _fCameraRadiusSS = m_pCamera.pixelWidth * 0.5f;

      // calculate the radius ratio
      // because affine transform will keep the ratios among points on a line: https://en.wikipedia.org/wiki/Affine_transformation#Properties
      // Ratios of vectors along a line; i.e., for distinct collinear points p_1, p_2, p_3, the ratio of \overrightarrow{p_1p_2} and \overrightarrow{p_2p_3} is the same as that of \overrightarrow{f(p_1)f(p_2)} and \overrightarrow{f(p_2)f(p_3)}.
      float _fRatio = _fCameraRadiusSS / _fAimRadiusSS;

      // at first check boss
      for(int _iIdx = 0; _iIdx < MonsterMgr.Instance.CurrentBossList.Count; _iIdx++)
      {
        // current boss reference
        GameObject _pCurBoss = MonsterMgr.Instance.CurrentBossList[_iIdx];

        // try to get its BoxCollider
        // get the box collider of the current game object
        BoxCollider _pColliderComp = _pCurBoss.GetComponentInChildren< BoxCollider >();

        // if the current boss does NOT have a BoxCollider: goto the next one
        if(null == _pColliderComp)
        {
          DefaultGameLogger.Instance.dumpLogError("[" + _pCurBoss.ToString() + "]: Can NOT have a BoxCollider!");

          continue;
        }

        // update weapon's frustum
        m_pWeaponFrustum.updateWeaponFrustumInLocalSpace(m_pCamera, _fRatio, _pColliderComp.gameObject);

        // if the current boss is within the weapon's frustum
        if(true == m_pWeaponFrustum.isInsideFrustum(_pColliderComp.center, _pColliderComp.size * 0.5f))
        {
          m_aAimedEntityList.Add(_pCurBoss);
        }
      }

      // then check monster
      for(int _iIdx = 0; _iIdx < MonsterMgr.Instance.CurrentMonsterList.Count; _iIdx++)
      {
        // current monster reference
        GameObject _pCurMonster = MonsterMgr.Instance.CurrentMonsterList[_iIdx];

        // try to get its BoxCollider
        // get the box collider of the current game object
        BoxCollider _pColliderComp = _pCurMonster.GetComponentInChildren< BoxCollider >();

        // if the current monster does NOT have a BoxCollider: goto the next one
        if(null == _pColliderComp)
        {
          DefaultGameLogger.Instance.dumpLogError("[" + _pCurMonster.ToString() + "]: Can NOT have a BoxCollider!");

          continue;
        }

        // update weapon's frustum
        m_pWeaponFrustum.updateWeaponFrustumInLocalSpace(m_pCamera, _fRatio, _pColliderComp.gameObject);

        // if the current monster is within the weapon's frustum
        if(true == m_pWeaponFrustum.isInsideFrustum(_pColliderComp.center, _pColliderComp.size * 0.5f))
        {
          m_aAimedEntityList.Add(_pCurMonster);
        }
      }

      // at last check dropoffs
      for(int _iIdx = 0; _iIdx < DropOffMgr.Instance.CurrentDropOffList.Count; _iIdx++)
      {
        // current dropoff reference
        GameObject _pCurDropOff = DropOffMgr.Instance.CurrentDropOffList[_iIdx];

        // try to get its BoxCollider
        // get the box collider of the current game object
        BoxCollider _pColliderComp = _pCurDropOff.GetComponentInChildren< BoxCollider >();

        // if the current dropoff does NOT have a BoxCollider: goto the next one
        if(null == _pColliderComp)
        {
          DefaultGameLogger.Instance.dumpLogError("[" + _pCurDropOff.ToString() + "]: Can NOT have a BoxCollider!");

          continue;
        }

        // update weapon's frustum
        m_pWeaponFrustum.updateWeaponFrustumInLocalSpace(m_pCamera, _fRatio, _pColliderComp.gameObject);

        // if the current dropoff is within the weapon's frustum
        if(true == m_pWeaponFrustum.isInsideFrustum(_pColliderComp.center, _pColliderComp.size * 0.5f))
        {
          m_aAimedEntityList.Add(_pCurDropOff);
        }
      }

      // return true, if we have found some entity
      return 0 != m_aAimedEntityList.Count;
    }

    // callback of device event
    void onDeviceEvent(DeviceEvent a_pEvent)
    {
      if(m_eCameraMode != CameraModes.CAMERA_MODES_REGULAR  // NOT regular camear mode
         ||                                                 // or 
         a_pEvent.Type != EventTypes.EVENT_TYPES_TILT       // other device event
        )
      {
        return;
      }

      // based on left and right tilt value create a rotation around Up axis
      // left  side tilt(-X): POSITIVE rotation
      // right side tilt(+X): NEGATIVE rotation
      float _fHorzRotAngle = -a_pEvent.DataArray[0] * HorzRotScale;
      // we can ALSO consider the tilt value as sine value
      //float _fHorzRotAngle = (float)-Math.Asin(a_pEvent.DataArray[0]) * 180.0f / 3.1415926f;
      // NOTE: BOTH methods provide smooth rotation but too much shake
      if(_fHorzRotAngle > MaxHorzRotAngle)
      {
        _fHorzRotAngle = MaxHorzRotAngle;
      }
      else if(_fHorzRotAngle < -MaxHorzRotAngle)
      {
        _fHorzRotAngle = -MaxHorzRotAngle;
      }

      // bottom side tilt(-Y): NEGATIVE rotation
      // top    side tilt(+Y): POSITIVE rotation
      float _fVertRotAngle = a_pEvent.DataArray[1] * VertRotScale;
      //float _fVertRotAngle = (float)Math.Asin(a_pEvent.DataArray[1]) * 180.0f / 3.1415926f;

      if(_fVertRotAngle > MaxVertRotAngle)
      {
        _fVertRotAngle = MaxVertRotAngle;
      }
      else if(_fVertRotAngle < -MaxVertRotAngle)
      {
        _fVertRotAngle = -MaxVertRotAngle;
      }

#if !RETAIL
      //DefaultGameLogger.Instance.dumpLogInfo("Rot X: " + _fHorzRotAngle + " Rot Y: " + _fVertRotAngle);
#endif

      // create the orizontial rotation: around up axis
      Quaternion _qHorzRot = Quaternion.AngleAxis(_fHorzRotAngle, Vector3.up);

      // create the vertical rotation: aound right axis
      Quaternion _qVertRot = Quaternion.AngleAxis(_fVertRotAngle, Vector3.right);

      // rotation order: (third)vertical rotation --> (second) horizontal rotation --> (first) initial rotation
      transform.rotation = m_qInitCameraOrien * _qHorzRot * _qVertRot;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // camera component
    UnityEngine.Camera m_pCamera;
    // current camera mode
    CameraModes m_eCameraMode;
    // measured frame time so far
    float m_fMeasuredFrameTime;
    // the text used to show statics
    Text m_pStatsText;
    // inital camera orientation: camera local space --> world space
    Quaternion m_qInitCameraOrien;

    // weapon aim frustum
    Frustum m_pWeaponFrustum;

    // aim pointer
    // currently aimed entity list
    List< GameObject > m_aAimedEntityList;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Camera

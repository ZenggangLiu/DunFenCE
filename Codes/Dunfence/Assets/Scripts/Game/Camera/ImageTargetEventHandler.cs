using UnityEngine;
using Framework.Logger;
using Game.Logic;

namespace Game.Camera
{
  #if VUFORIA_ANDROID_SETTINGS || VUFORIA_IOS_SETTINGS
  // JUST defined if Vuforia SDK available
  public class ImageTargetEventHandler : MonoBehaviour, Vuforia.ITrackableEventHandler
  {
    #region PUBLIC METHODS
    /**
     * callback for Image Target Events
     */
    public void OnTrackableStateChanged(Vuforia.TrackableBehaviour.Status a_ePreviousStatus,
                                        Vuforia.TrackableBehaviour.Status a_eNewStatus
                                       )
    {
      if(a_eNewStatus == Vuforia.TrackableBehaviour.Status.DETECTED
         ||
         a_eNewStatus == Vuforia.TrackableBehaviour.Status.TRACKED
         ||
         a_eNewStatus == Vuforia.TrackableBehaviour.Status.EXTENDED_TRACKED
        )
      {
        OnTrackingFound();
      }
      else
      {
        OnTrackingLost();
      }
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    void OnTrackingFound()
    {
      // Found Marker
      // firstly, notify the main game entry
      if(GameLevelMgr.Instance.GameMainLevel != null
         &&
         GameLevelMgr.Instance.GameMainLevel.MainEntry != null
        )
      {
  #if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("[ImageTargetEventHandler][OnTrackingFound]: Found Marker: " + m_pTrackableBehaviour.TrackableName);
  #endif

        GameLevelMgr.Instance.GameMainLevel.MainEntry.onARMarkerFound(m_pTrackableBehaviour.name, gameObject);
      }

      // secondly, enable all renderers and colliders
      Renderer[] _aRendererList = GetComponentsInChildren< Renderer >(true);
      Collider[] _aColliderList = GetComponentsInChildren< Collider >(true);

      // enable renderers
      for(int _iIdx = 0; _iIdx < _aRendererList.Length; _iIdx++)
      {
        _aRendererList[_iIdx].enabled = true;
      }

      // enable colliders
      for(int _iIdx = 0; _iIdx < _aColliderList.Length; _iIdx++)
      {
        _aColliderList[_iIdx].enabled = true;
      }
    }

    private void OnTrackingLost()
    {
      // Lost Marker
      // firstly, disable all renderers and colliders
      Renderer[] _aRendererList = GetComponentsInChildren< Renderer >(true);
      Collider[] _aColliderList = GetComponentsInChildren< Collider >(true);

      // disable renderers
      for(int _iIdx = 0; _iIdx < _aRendererList.Length; _iIdx++)
      {
        _aRendererList[_iIdx].enabled = false;
      }

      // disable colliders
      for(int _iIdx = 0; _iIdx < _aColliderList.Length; _iIdx++)
      {
        _aColliderList[_iIdx].enabled = false;
      }

      // secondly, notify the main game entry
      if(GameLevelMgr.Instance.GameMainLevel != null
         &&
         GameLevelMgr.Instance.GameMainLevel.MainEntry != null
        )
      {

  #if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("[ImageTargetEventHandler][OnTrackingLost]: Lost Marker: " + m_pTrackableBehaviour.TrackableName);
  #endif
        
        GameLevelMgr.Instance.GameMainLevel.MainEntry.onARMarkerLost(m_pTrackableBehaviour.name, gameObject);
      }
    }

    void Start()
    {
      m_pTrackableBehaviour = GetComponent< Vuforia.TrackableBehaviour >();
      if (m_pTrackableBehaviour)
      {
        m_pTrackableBehaviour.RegisterTrackableEventHandler(this);
      }
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    Vuforia.TrackableBehaviour m_pTrackableBehaviour;
    #endregion // PRIVATE DATA MEMBERS
  }

  #else
  public class ImageTargetEventHandler : MonoBehaviour
  {

  }
  #endif // #if VUFORIA_ANDROID_SETTINGS

}

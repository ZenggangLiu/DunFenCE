/**
 * Hander for Vufoia global Events
 * - attached to camera of vuforia scene
 */

using UnityEngine;
using Framework.Logger;

namespace Game.Camera
{
  public class VuforiaSDKEventHandler : MonoBehaviour
  {
    void Start()
    {
      // register all interesting events
      registerInterestingEvents();

      // set camera focus mode
      setCameraFocusMode();
    }

    void registerInterestingEvents()
    {
#if VUFORIA_ANDROID_SETTINGS || VUFORIA_IOS_SETTINGS
      // register start and pause callbacks for vuforia
      // NOT use  it is NULL when the camera node is deactived
      Vuforia.VuforiaBehaviour.Instance.RegisterVuforiaStartedCallback(onVuforiaStarted);
      Vuforia.VuforiaBehaviour.Instance.RegisterOnPauseCallback(onVuforiaPaused);
#endif // #if VUFORIA_ANDROID_SETTINGS || VUFORIA_IOS_SETTINGS
    }

    // set the focus mode for the vuforia camera
    void setCameraFocusMode()
    {
#if VUFORIA_ANDROID_SETTINGS || VUFORIA_IOS_SETTINGS
      // try to set continuous focus mode
      // NOT possible: try touch to focus mode: FOCUS_MODE_TRIGGERAUTO: user touch the focus location
      if(false == Vuforia.CameraDevice.Instance.SetFocusMode(Vuforia.CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO))
      {
#if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("[VuforiaSDKEventHandler][setCameraFocusMode]: Can NOT set FOCUS_MODE_CONTINUOUSAUTO focus mode");
#endif
        
        if(false == Vuforia.CameraDevice.Instance.SetFocusMode(Vuforia.CameraDevice.FocusMode.FOCUS_MODE_TRIGGERAUTO))
        {
#if !RETAIL
          DefaultGameLogger.Instance.dumpLogInfo("[VuforiaSDKEventHandler][setCameraFocusMode]: Can NOT set FOCUS_MODE_TRIGGERAUTO focus mode");
#endif          
        }
      }
#endif // #if VUFORIA_ANDROID_SETTINGS || VUFORIA_IOS_SETTINGS
    }

    // callback for vuforia start up
    void onVuforiaStarted()
    {
      // set the camera focus mode
      setCameraFocusMode();
    }

    // callback for vuforia pause/resume
    void onVuforiaPaused(bool a_bIsPaused)
    {
      // vuforia resumed
      if (false == a_bIsPaused)
      {
        // set the focus mode for the vuforia camera once again
        setCameraFocusMode();
      }
    }
  }

} // namespace Game.Camera

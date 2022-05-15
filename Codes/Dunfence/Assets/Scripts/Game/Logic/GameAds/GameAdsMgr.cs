/**
 * Ads manager used to show different kinds of Ads
 * - call doInitialize() to initialize this Ads manager
 * - show Banner directly within showBanner(): using Google AdMob banner
 * - show Interstitial Image/Video after it is ready within showInterstitial(): using Google AdMob interstitial
 * - show Incentivize/Reward Video within showIncentivizeVideo() in this order:
 *   + Unity Reward Video
 *   + Unity Normal Video
 *   + Google Reward Video
 */
// define this if we want to have a full log on all loaded Ads configs
//#define REPORT_LOADED_CONFIG

#if UNITY_ADS
// only compile Ads code on supported platforms
using UnityEngine.Advertisements;
#endif // #if UNITY_ADS
using UnityEngine;
using Framework.Utils;
using Framework.Logger;
using GoogleMobileAds.Api;
using System.Collections.Generic; // List < T >

namespace Game.Ads
{
  public class GameAdsMgr : TManager< GameAdsMgr >
  {
    #region PUBLIC METHODS
    /**
     * check if unity ads' incentivize/rewards video available
     */
    public bool isUnityAdsIncentivizeVideoAvailable()
    {
#if UNITY_ADS
      return Advertisement.IsReady(m_szUnityAdsIncentivizeVideoName);

#else
      return false;
#endif // #if UNITY_ADS
    }

    /**
     * check if unity ads' interstitial video available
     */
    public bool isUnityAdsInterstitialVideoAvailable()
    {
#if UNITY_ADS
      return Advertisement.IsReady(m_szUnityAdsInterstitialVideoName);

#else
      return false;
#endif // #if UNITY_ADS
    }

    /**
     * check if google ads' incentivize/reward video available
     * checked in this order:
     *   + Unity Reward Video
     *   + Unity Normal Video
     *   + Google Reward Video
     */
    public bool isGoogleAdsIncentivizeVideoAvailable()
    {
      return m_pGoogleAdsIncentivizeVideo != null && m_pGoogleAdsIncentivizeVideo.IsLoaded();
    }

    /**
     * check if any sort of video available
     */
    public bool isAnyVideoAvailable()
    {
      return isUnityAdsIncentivizeVideoAvailable() || isUnityAdsInterstitialVideoAvailable() || isGoogleAdsIncentivizeVideoAvailable();
    }

    /**
     * check if google ads' interstitial available
     */
    public bool isGoogleAdsInterstitialAvailable()
    {
      return m_pGoogleAdsInterstitial != null && m_pGoogleAdsInterstitial.IsLoaded();
    }

    /**
     * show banner
     * return true on success
     */
    public bool showBanner()
    {
      if(m_pGoogleAdsBanner != null)
      {
        m_pGoogleAdsBanner.Destroy();

        m_pGoogleAdsBanner = null;
      }

      string _szUIDUsed = "";

#if UNITY_ANDROID
      _szUIDUsed = m_szGoogleAdsBannerAndroidUID;

#elif UNITY_IPHONE
      _szUID = m_szGoogleAdsBanneriOSUID;
#endif // #if UNITY_ANDROID

      // create a 320x50 banner at the top of the screen.
      m_pGoogleAdsBanner = new BannerView(_szUIDUsed, AdSize.Banner, AdPosition.Top);

      // create builder
      AdRequest.Builder _pBuilder = new AdRequest.Builder();

      // add Android Emulator
      _pBuilder.AddTestDevice(AdRequest.TestDeviceSimulator);

      // add all listed test devices
      for(int _iIdx = 0; _iIdx < m_aTestDeviceList.Count; _iIdx++)
      {
        _pBuilder.AddTestDevice(m_aTestDeviceList[_iIdx]);
      }

      // create request
      AdRequest _pRequest = _pBuilder.Build();

      // load the banner
      m_pGoogleAdsBanner.LoadAd(_pRequest);

      // there is NO way to check the validity of banner
      return true;
    }

    /**
     * hide current banner
     */
    public void hideBanner()
    {
      // NO banner
      if(null == m_pGoogleAdsBanner)
      {
        return;
      }

      // hide it
      m_pGoogleAdsBanner.Hide();
    }

    /**
     * show interstitial
     * return true on success
     */
    public bool showInterstitial()
    {
      if(null == m_pGoogleAdsInterstitial               // can NOT initialize google ads' interstitial
         ||                                             // or
         false == m_pGoogleAdsInterstitial.IsLoaded()   // interstitial is NOT loaded
        )
      {
        if(null == m_pGoogleAdsInterstitial)
        {
          DefaultGameLogger.Instance.dumpLogWarning("[GameAdsMgr][showInterstitial]: Can NOT show Interstitial: NO Google Ads Interstitial Instance");  
        }

        if(false == m_pGoogleAdsInterstitial.IsLoaded())
        {
          DefaultGameLogger.Instance.dumpLogWarning("[GameAdsMgr][showInterstitial]: Can NOT show Interstitial: Google Ads Interstitial is NOT loaded");  
        }

        return false;
      }

#if !RETAIL
      DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][showInterstitial]: Google Ads Interstitial will be played");
#endif

      m_pGoogleAdsInterstitial.Show();

      return true;
    }

    /**
     * show incentivize/reward video
     * - show Incentivize/Reward Video in this order:
     *   + Unity Reward Video
     *   + Unity Normal Video
     *   + Google Reward Video
     * 
     * return true on success
     */
    public bool showIncentivizeVideo()
    {
      // unity ads' incentivize video is ready: try to play it
      if(isUnityAdsIncentivizeVideoAvailable() == true)
      {
#if UNITY_ADS

#if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][showIncentivizeVideo]: try to play UnityAds' Incentivize Video");
#endif

        Advertisement.Show(m_szUnityAdsIncentivizeVideoName, new ShowOptions{ resultCallback =  onUnityAdsPresentVideoDone });

        return true;
#endif // #if UNITY_ADS
      }
      // unity ads' interstitial video is ready: try to play it
      if(isUnityAdsInterstitialVideoAvailable() == true)
      {
#if UNITY_ADS
#if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][showIncentivizeVideo]: try to play UnityAds' Interstitial Video");
#endif

        Advertisement.Show(m_szUnityAdsInterstitialVideoName, new ShowOptions{ resultCallback =  onUnityAdsPresentVideoDone });

        return true;
#endif // #if UNITY_ADS
      }

      // google ads' incentivize vidoe is ready: try to play it
      if(isGoogleAdsIncentivizeVideoAvailable() == true)
      {
#if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][showIncentivizeVideo]: try to play GoogleAds' Incentivize Video");
#endif

        m_pGoogleAdsIncentivizeVideo.Show();

        return true;
      }

      // nothing has been played: return false;
      DefaultGameLogger.Instance.dumpLogWarning("[GameAdsMgr][showIncentivizeVideo]: NO Incentivize Video ready to play");

      return false;
    }

    #region PUBLIC OVERRIDDENS
    // the function called before using this manager
    // set m_bIsInitialized to true, on successful initialization
    public override void doInitialize()
    {
      // reset all data members
      doReset();

      // read in AdsData.json
      loadConfigFile();

      // according to the config, preload google interstial and google incentivize video
      // if google ads' interstitial enabled
      if(true == m_bIsGoogleAdsInterstitialEnabled)
      {
        string _szUIDUsed = "";
#if UNITY_ANDROID
        _szUIDUsed = m_szGoogleAdsInterstitialAndroidUID;
#elif UNITY_IPHONE
        _szUIDUsed = m_szGoogleAdsInterstitialiOSUID;
#endif // #if UNITY_ANDROID

        // create google ads interstitial instance
        m_pGoogleAdsInterstitial = new InterstitialAd(_szUIDUsed);

        // create builder
        AdRequest.Builder _pBuilder = new AdRequest.Builder();

        // add Android Emulator
        _pBuilder.AddTestDevice(AdRequest.TestDeviceSimulator);

        // add all listed test devices
        for(int _iIdx = 0; _iIdx < m_aTestDeviceList.Count; _iIdx++)
        {
          _pBuilder.AddTestDevice(m_aTestDeviceList[_iIdx]);
        }

        // create request
        AdRequest _pRequest = _pBuilder.Build();

        // load the interstitial with the request.
        m_pGoogleAdsInterstitial.LoadAd(_pRequest);

        // register event callback for closed
        m_pGoogleAdsInterstitial.OnAdClosed += onGoogleAdsPresentInterstitialDone;
      }

      // if google ads's incentivize video enabled
      if(true == m_bIsGoogleAdsIncentivizeVideoEnabled)
      {
        string _szUIDUsed = "";
#if UNITY_ANDROID
        _szUIDUsed = m_szGoogleAdsIncentivizeVideoAndroidUID;
#elif UNITY_IPHONE
        _szUIDUsed = m_szGoogleAdsIncentivizeVideoiOSUID;
#endif // #if UNITY_ANDROID

        // store the solo instance
        m_pGoogleAdsIncentivizeVideo = RewardBasedVideoAd.Instance;

        // create builder
        AdRequest.Builder _pBuilder = new AdRequest.Builder();

        // add Android Emulator
        _pBuilder.AddTestDevice(AdRequest.TestDeviceSimulator);

        // add all listed test devices
        for(int _iIdx = 0; _iIdx < m_aTestDeviceList.Count; _iIdx++)
        {
          _pBuilder.AddTestDevice(m_aTestDeviceList[_iIdx]);
        }

        // create request
        AdRequest _pRequest = _pBuilder.Build();

        // load the incentivize video
        m_pGoogleAdsIncentivizeVideo.LoadAd(_pRequest, _szUIDUsed);

        // register event callback for gaing rewards
        m_pGoogleAdsIncentivizeVideo.OnAdRewarded += onGoogleAdsPresentVideoDone;
      }

      // set initialization flag
      m_bIsInitialized = true;
    }

    // rest this manager
    public override void doReset()
    {
      m_bIsUnityAdsEnabled = false;
      m_aTestDeviceList = new List< string >(2);
      m_szUnityAdsIncentivizeVideoName = "rewardedVideo";
      m_szUnityAdsInterstitialVideoName = "video";
      m_bIsGoogleAdsBannerEnabled = false;
      m_bIsGoogleAdsInterstitialEnabled = false;;
      m_bIsGoogleAdsIncentivizeVideoEnabled = false;
    }

    // the function called before destroying this manager
    // clear m_bIsInitialized flag
    public override void doDestroy()
    {
      // clear initialization flag
      m_bIsInitialized = false;
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    /**
     * load in AdsData.json
     * return true on success
     */
    bool loadConfigFile()
    {
      // try to load in AdsData.json file
      TextAsset _pAdsDataJsonFile = Resources.Load("AdsData") as TextAsset; // an TextAsset: the representation of a Textural or Binary File

      // can NOT load AdsData.json
      if(null == _pAdsDataJsonFile)
      {
        DefaultGameLogger.Instance.dumpLogError("[GameAdsMgr][loadConfigFile]: Can NOT load AdsData.json file");

        return false;
      }

      // create and parse the json data at the same time
      JSONObject _pAdsDataJson = new JSONObject(_pAdsDataJsonFile.text);

      if(true == _pAdsDataJson.HasField("TestDeviceList")   // AdsData.json does contain TestDeviceList array
         &&                                                 // and
         true == _pAdsDataJson["TestDeviceList"].IsArray    // TestDeviceList element is an array
         &&                                                 // and
         0 != _pAdsDataJson["TestDeviceList"].Count         // TestDeviceList is NOT empty
        )
      {
        // clear the test device list
        m_aTestDeviceList.Clear();

        // insert all test devices into the list
        for(int _iIdx = 0; _iIdx < _pAdsDataJson["TestDeviceList"].Count; _iIdx++)
        {
#if !RETAIL
          DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] Added a Test Device[ID]: " + _pAdsDataJson["TestDeviceList"][_iIdx].str);
#endif

          m_aTestDeviceList.Add(_pAdsDataJson["TestDeviceList"][_iIdx].str); 
        }
      }

      // if AdsData.json does contain UnityAds element
      if(true == _pAdsDataJson.HasField("UnityAds"))
      {
        // if UnityAds contains Enabled element and it is a boolean
        if(_pAdsDataJson["UnityAds"].HasField("Enabled")
           && 
           _pAdsDataJson["UnityAds"]["Enabled"].IsBool
          )
        {
          _pAdsDataJson["UnityAds"].GetField(ref m_bIsUnityAdsEnabled, "Enabled");
        }

        // if UnityAds contains IncentivizeVideoName element
        if(_pAdsDataJson["UnityAds"].HasField("IncentivizeVideoName"))
        {
          _pAdsDataJson["UnityAds"].GetField(ref m_szUnityAdsIncentivizeVideoName, "IncentivizeVideoName");
        }

        // if UnityAds contains InterstitialVideoName element
        if(_pAdsDataJson["UnityAds"].HasField("InterstitialVideoName"))
        {
          _pAdsDataJson["UnityAds"].GetField(ref m_szUnityAdsInterstitialVideoName, "InterstitialVideoName");
        }

#if REPORT_LOADED_CONFIG
        if(true == m_bIsUnityAdsEnabled)
        {
#if !RETAIL
          DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] UnityAds: ENABLED");
          DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] UnityAds: Incentivize Video: " + m_szUnityAdsIncentivizeVideoName);
          DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] UnityAds: Interstitial Vidoe: " + m_szUnityAdsInterstitialVideoName);
#endif
        }
        else
        {
#if !RETAIL
          DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr] UnityAds: DISABLED");
#endif
        }
#endif // #if REPORT_LOADED_CONFIG
      }

      // if AdsData.json does contain GoogleAds element
      if(true == _pAdsDataJson.HasField("GoogleAds"))
      {
        if(_pAdsDataJson["GoogleAds"].HasField("Banner"))
        {
          if(_pAdsDataJson["GoogleAds"]["Banner"].HasField("Enabled")
             &&
             _pAdsDataJson["GoogleAds"]["Banner"]["Enabled"].IsBool
            )
          {
            _pAdsDataJson["GoogleAds"]["Banner"].GetField(ref m_bIsGoogleAdsBannerEnabled, "Enabled");
          }

          if(_pAdsDataJson["GoogleAds"]["Banner"].HasField("AndroidUID"))
           {
            _pAdsDataJson["GoogleAds"]["Banner"].GetField(ref m_szGoogleAdsBannerAndroidUID, "AndroidUID");
          }

          if(_pAdsDataJson["GoogleAds"]["Banner"].HasField("iOSUID"))
          {
            _pAdsDataJson["GoogleAds"]["Banner"].GetField(ref m_szGoogleAdsBanneriOSUID, "iOSUID");
          }

#if REPORT_LOADED_CONFIG
          if(true == m_bIsGoogleAdsBannerEnabled)
          {
#if !RETAIL
            DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] GoogleAds' Banner: ENABLED");
            DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] GoogleAds' Banner Android Unit ID: " + m_szGoogleAdsBannerAndroidUID);
            DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] GoogleAds' Banner Android Unit ID: " + m_szGoogleAdsBanneriOSUID);
#endif
          }
          else
          {
#if !RETAIL
            DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] GoogleAds' Banner: DISABLED");
#endif
          }
#endif // #if REPORT_LOADED_CONFIG
        }

        if(_pAdsDataJson["GoogleAds"].HasField("Interstitial"))
        {
          if(_pAdsDataJson["GoogleAds"]["Interstitial"].HasField("Enabled")
             &&
             _pAdsDataJson["GoogleAds"]["Interstitial"]["Enabled"].IsBool
            )
          {
            _pAdsDataJson["GoogleAds"]["Interstitial"].GetField(ref m_bIsGoogleAdsInterstitialEnabled, "Enabled");
          }

          if(_pAdsDataJson["GoogleAds"]["Interstitial"].HasField("AndroidUID"))
          {
            _pAdsDataJson["GoogleAds"]["Interstitial"].GetField(ref m_szGoogleAdsInterstitialAndroidUID, "AndroidUID");
          }

          if(_pAdsDataJson["GoogleAds"]["Interstitial"].HasField("iOSUID"))
          {
            _pAdsDataJson["GoogleAds"]["Interstitial"].GetField(ref m_szGoogleAdsInterstitialiOSUID, "iOSUID");
          }

#if REPORT_LOADED_CONFIG
          if(true == m_bIsGoogleAdsInterstitialEnabled)
          {
#if !RETAIL
            DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] GoogleAds' Interstitial: ENABLED");
            DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] GoogleAds' Interstitial Android Unit ID: " + m_szGoogleAdsInterstitialAndroidUID);
            DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] GoogleAds' Interstitial Android Unit ID: " + m_szGoogleAdsInterstitialiOSUID);
#endif
          }
          else
          {
#if !RETAIL
            DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] GoogleAds' Interstitial: DISABLED");
#endif
          }
#endif // #if REPORT_LOADED_CONFIG
        }

        if(_pAdsDataJson["GoogleAds"].HasField("IncentivizeVideo"))
        {
          if(_pAdsDataJson["GoogleAds"]["IncentivizeVideo"].HasField("Enabled")
            &&
            _pAdsDataJson["GoogleAds"]["IncentivizeVideo"]["Enabled"].IsBool
          )
          {
            _pAdsDataJson["GoogleAds"]["IncentivizeVideo"].GetField(ref m_bIsGoogleAdsIncentivizeVideoEnabled, "Enabled");
          }

          if(_pAdsDataJson["GoogleAds"]["IncentivizeVideo"].HasField("AndroidUID"))
          {
            _pAdsDataJson["GoogleAds"]["IncentivizeVideo"].GetField(ref m_szGoogleAdsIncentivizeVideoAndroidUID, "AndroidUID");
          }

          if(_pAdsDataJson["GoogleAds"]["IncentivizeVideo"].HasField("iOSUID"))
          {
            _pAdsDataJson["GoogleAds"]["IncentivizeVideo"].GetField(ref m_szGoogleAdsIncentivizeVideoiOSUID, "iOSUID");
          }

#if REPORT_LOADED_CONFIG
          if(true == m_bIsGoogleAdsIncentivizeVideoEnabled)
          {
#if !RETAIL
            DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] GoogleAds' Incentivize Video: ENABLED");
            DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] GoogleAds' Incentivize Video Android Unit ID: " + m_szGoogleAdsIncentivizeVideoAndroidUID);
            DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] GoogleAds' Incentivize Video Android Unit ID: " + m_szGoogleAdsIncentivizeVideoiOSUID);
#endif
          }
          else
          {
#if !RETAIL
            DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][loadConfigFile] GoogleAds' Incentivize Video: DISABLED");
#endif
          }
#endif // #if REPORT_LOADED_CONFIG
        }
      }

      // done
      return true;
    }

    // callback for GoogleAds when its interstitial has finished its display
    void onGoogleAdsPresentInterstitialDone(object a_pSender, System.EventArgs a_pArgs)
    {
#if !RETAIL
      DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][onGoogleAdsPresentInterstitialDone]");
#endif

      // reword the user
      Event.GameEventMgr.Instance.fireGlobalScopeEvent(Game.Event.EventIDs.EVENT_IDS_LOGIC_ADS_INTERSTITIAL_CLOSED,
                                                       null,
                                                       Game.Event.EventTriggerType.EVENT_TRIGGER_TYPE_LOCAL,
                                                       this
                                                      );
    }

    // callback for GoogleAds when its incentivize video has done
    void onGoogleAdsPresentVideoDone(object a_pSender, Reward a_pArgs)
    {
      // get reward type and amount
      string _szRewardType = a_pArgs.Type;
      int _iRewardAmount = (int)a_pArgs.Amount;

#if !RETAIL
      DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][onGoogleAdsPresentVideoDone] Type: " + _szRewardType + " Amount: " + _iRewardAmount);
#endif

      // fire playback completed event
      Event.GameEventMgr.Instance.fireGlobalScopeEvent(Game.Event.EventIDs.EVENT_IDS_LOGIC_ADS_VIDEO_FINISHED,
                                                       _iRewardAmount,
                                                       Game.Event.EventTriggerType.EVENT_TRIGGER_TYPE_LOCAL,
                                                       this
                                                      );
    }

#if UNITY_ADS
    // callback for UnityAds when its video has done
    void onUnityAdsPresentVideoDone(ShowResult a_ePlaybackResult)
    {
      switch (a_ePlaybackResult)
      {
        case ShowResult.Finished:
        {
#if !RETAIL
          DefaultGameLogger.Instance.dumpLogInfo("[GameAdsMgr][onShowVideoDone] Ads Video completed with rewards: 1");
#endif

          // playback completed
          Event.GameEventMgr.Instance.fireGlobalScopeEvent(Game.Event.EventIDs.EVENT_IDS_LOGIC_ADS_VIDEO_FINISHED,
                                                           1,
                                                           Game.Event.EventTriggerType.EVENT_TRIGGER_TYPE_LOCAL,
                                                           this
                                                          );
          break;
        }

        case ShowResult.Skipped:
        {
          DefaultGameLogger.Instance.dumpLogWarning("[GameAdsMgr][onShowVideoDone] Ads Video SKIPPED");

          Event.GameEventMgr.Instance.fireGlobalScopeEvent(Game.Event.EventIDs.EVENT_IDS_LOGIC_ADS_VIDEO_SKIPPED,
                                                           null,
                                                           Game.Event.EventTriggerType.EVENT_TRIGGER_TYPE_LOCAL,
                                                           this
                                                          );
          break;
        }

        case ShowResult.Failed:
        {
          DefaultGameLogger.Instance.dumpLogError("[GameAdsMgr][onShowVideoDone] Ads Video playback FAILED");

          Event.GameEventMgr.Instance.fireGlobalScopeEvent(Game.Event.EventIDs.EVENT_IDS_LOGIC_ADS_VIDEO_FAILED,
                                                           null,
                                                           Game.Event.EventTriggerType.EVENT_TRIGGER_TYPE_LOCAL,
                                                           this
                                                          );
          break;
        }
      }
    }
#endif // #if UNITY_ADS

    #region PRIVATE DATA MEMBERS
    // test device list
    List< string > m_aTestDeviceList;

    // Unity Ads //
    // indicate if UnityAds is enabled
    bool m_bIsUnityAdsEnabled;
    // Unity incentivize video name
    string m_szUnityAdsIncentivizeVideoName;
    // Unity interstitial video name
    string m_szUnityAdsInterstitialVideoName;


    // Google Ads //
    // banner
    // google ads' banner instance
    BannerView m_pGoogleAdsBanner;
    bool m_bIsGoogleAdsBannerEnabled;
    string m_szGoogleAdsBannerAndroidUID;
    string m_szGoogleAdsBanneriOSUID;

    // interstitial
    // google ads' interstitial instance
    InterstitialAd m_pGoogleAdsInterstitial;
    bool m_bIsGoogleAdsInterstitialEnabled;
    string m_szGoogleAdsInterstitialAndroidUID;
    string m_szGoogleAdsInterstitialiOSUID;

    // incentivize video
    // google ads's incentivize video instance
    RewardBasedVideoAd m_pGoogleAdsIncentivizeVideo;
    bool m_bIsGoogleAdsIncentivizeVideoEnabled;
    string m_szGoogleAdsIncentivizeVideoAndroidUID;
    string m_szGoogleAdsIncentivizeVideoiOSUID;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Ads

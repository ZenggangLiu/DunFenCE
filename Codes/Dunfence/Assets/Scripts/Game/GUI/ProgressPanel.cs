using UnityEngine;
using Framework.Logger;
using Game.Logic;

namespace Game.GUI
{
  public class ProgressPanel : MonoBehaviour
  {
    #region PUBLIC METHODS
    /**
     * initialize this progress panel
     */
    public void doInitialize()
    {
      // generate the percentage bar list
      m_aPercentageBarList = new GameObject[10];

      if(transform.childCount > m_aPercentageBarList.Length)
      {
        DefaultGameLogger.Instance.dumpLogError("[ProgressPanel][doInitialize][" + gameObject.ToString() + "]: Too Many Children under ProgressPanel");

        return;
      }

      // store all percentage bars, disable filled bars and enable all un-filled bars
      for(int _iPerIdx = 0; _iPerIdx < transform.childCount; _iPerIdx++)
      {
        int _iPercentage = System.Convert.ToInt32(transform.GetChild(_iPerIdx).name);

        m_aPercentageBarList[_iPercentage / 10 - 1] = transform.GetChild(_iPerIdx).gameObject;

        // disable filled bar
        transform.GetChild(_iPerIdx).Find(_iPercentage + "F").gameObject.SetActive(false);

        // enable un-filled bar
        transform.GetChild(_iPerIdx).Find(_iPercentage + "N").gameObject.SetActive(true);
      }

      // set initialization flag
      m_bIsInitialized = true;
    }

    /**
     * reset this progress panel
     */
    public void doReset()
    {
      // iterate over all progress bars: [0, 9]: 0:10%, 9:100%
      for(int _iCurPerIdx = 0; _iCurPerIdx < m_aPercentageBarList.Length; _iCurPerIdx++)
      {
        // turn off filled progress bar
        m_aPercentageBarList[_iCurPerIdx].transform.Find((_iCurPerIdx + 1) * 10 + "F").gameObject.SetActive(false);

        // turn on un-filled progress bar
        m_aPercentageBarList[_iCurPerIdx].transform.Find((_iCurPerIdx + 1) * 10 + "N").gameObject.SetActive(true);
      }
    }

    /**
     * destroy this progress panel
     */
    public void doDestroy()
    {
      m_aPercentageBarList = null;

      m_bIsInitialized = false;
    }

    /**
     * set progress percentage value
     */
    public void setProgress(float a_fProgressVal)
    {
      // reset all progress bars before setting
      doReset();

      // get the integer progress value: 1, 2, 3, 4, 5 ,6 ,7, 8, 9, 10: corresponding: 10%, 20%,...,100%
      int _iCurProgressVal = (int)(a_fProgressVal / 10.0f);

      // if current progress < 1: the real progress is less than 10
      if(_iCurProgressVal < 1)
      {
        return;
      }

      // if current progress value > 10: set it to 10
      if(_iCurProgressVal > m_aPercentageBarList.Length)
      {
        _iCurProgressVal = m_aPercentageBarList.Length;
      }

      // iterate over all progress bars: [0, 9]: 0:10%, 9:100%
      for(int _iCurPerIdx = 0; _iCurPerIdx < _iCurProgressVal; _iCurPerIdx++)
      {
        // turn on filled progress bar
        m_aPercentageBarList[_iCurPerIdx].transform.Find((_iCurPerIdx + 1) * 10 + "F").gameObject.SetActive(true);

        // turn off un-filled progress bar
        m_aPercentageBarList[_iCurPerIdx].transform.Find((_iCurPerIdx + 1) * 10 + "N").gameObject.SetActive(false);
      }
    }

    /**
     * Update() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last Update() call
     * check false == m_bIsInitialized before doing the real thing
     */
    public void Update()
    {
      // NOT initialized: return
      if(false == m_bIsInitialized)
      {
        return;
      }      
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE DATA MEMBERS
    // percentage bar list
    GameObject[] m_aPercentageBarList;
    // initialization flag
    bool m_bIsInitialized;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.GUI

/**
 * About Dialog
 * - NOTE: each time, the whole Content scrolled above, its RecTTransform will be reset to ZERO. WHY?
 */

using UnityEngine;
using UnityEngine.UI;
using Framework.Utils;
using Framework.Logger;
using Game.Logic;

namespace Game.GUI
{
  public class AboutDialog : MonoBehaviour
  {
    #region PUBLIC DATA MEMBERS
    // end Y position
    public float EndPosY = 2280.0f;
    // shift speed in Y
    public float ShiftSpeed = 36.0f;
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    /**
     * initialize this about dialog
     */
    public void doInitialize()
    {
      // try to find Content node
      GameObject _pGUINode = GameObjectUtils.FindNode(gameObject, "Content");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[AboutDialog][doInitialize]: Can NOT find Content Node");

        return;
      }

      m_pContentPos = _pGUINode.GetComponent< RectTransform >();
      if(null == m_pContentPos)
      {
        DefaultGameLogger.Instance.dumpLogError("[AboutDialog][doInitialize]: Content Node is NOT a GUI node");

        return;
      }

      // try to find OkBtn
     _pGUINode = GameObjectUtils.FindNode(gameObject, "OkBtn");
      if(null == _pGUINode)
      {
        DefaultGameLogger.Instance.dumpLogError("[AboutDialog][doInitialize]: Can NOT find OkBtn Node");

        return;
      }

      // try to get button component
      if(_pGUINode.GetComponent< Button >() == null)
      {
        DefaultGameLogger.Instance.dumpLogError("[AboutDialog][doInitialize]: OkBtn Node does NOT have Button component");

        return;
      }

      // register callback
      _pGUINode.GetComponent< Button >().onClick.AddListener(onOkClicked); // Listener's prototype: public delegate void UnityAction();
    }

    /**
     * show this about dialog
     */
    public void show()
    {
      // set Content node it the given start position
      m_pContentPos.anchoredPosition = new Vector2(m_pContentPos.anchoredPosition.x, 0.0f);

      // enable this GUI
      // ACTIVE this panel
      // NOTE: otherwise the SetTrigger() will return the error: Animator is not playing a Playable
      gameObject.SetActive(true);
    }

    /**
     * hide this about dialog
     */
    public void hide()
    {
      // disable this GUI
      gameObject.SetActive(false);
    }

    public void Update()
    {
      // NO shown
      if(false == gameObject.activeSelf)
      {
        return;
      }

      // increase the current Y
      m_pContentPos.anchoredPosition += new Vector2(0.0f, Time.deltaTime * ShiftSpeed);

      // check if we has reached the end
      if(m_pContentPos.anchoredPosition.y > EndPosY)
      {
        // set Content node it the given start position
        m_pContentPos.anchoredPosition = new Vector2(m_pContentPos.anchoredPosition.x, 0.0f);
      }
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // callback of Ok button click
    void onOkClicked()
    {
      // hide this about dialog
      if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as InventoryLevelGUI)
      {
        (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as InventoryLevelGUI).hideAboutDialog();
      }
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // current GUI position of the Content node
    RectTransform m_pContentPos;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.GUI

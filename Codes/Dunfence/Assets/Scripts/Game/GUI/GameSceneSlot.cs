/**
 * Slot within Game Level Select Dialog
 */

using UnityEngine;
using UnityEngine.UI;
using Game.Logic;
using Framework.Utils;
using Framework.Logger;

namespace Game.GUI
{
  public class GameSceneSlot : MonoBehaviour
  {
    // level name
    public string LevelName;

    /**
     * initialize this slot with the given slot index
     */
    public void doInitialize(int a_iSlotIndex)
    {
      // try to find CoverNode
      m_pCoverNode = GameObjectUtils.FindNode(gameObject, "Cover");
      if(null == m_pCoverNode)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][GameSceneSlot][doInitialize]: can NOT find Cover Node");

        return;
      }

      // try to find the Toggle component
      m_pToggle = GetComponent< Toggle >();
      if(null == m_pToggle)
      {
        DefaultGameLogger.Instance.dumpLogError("[" + gameObject.ToString() + "][GameSceneSlot][doInitialize]: this slot does NOT have Toggle component");

        return;
      }

      // store given slot index
      m_iSlotIndex = a_iSlotIndex;

      // store current toggle state
      m_bIsToggled = m_pToggle.isOn;

      // adjust Cover node based on the current toggle state
      // NOT toggled, trun on Cover node
      if(false == m_bIsToggled)
      {
        m_pCoverNode.SetActive(true);
      }
      else
      {
        m_pCoverNode.SetActive(false);
      }
    }

    // callback when this slot is toggled
    // NOTE: each slot is a toggle button, system will give true in a_bIsOn when this slot is toggled on
    public void onSlotToggled(bool a_bIsOn) // WITHOUT ToggleGroup, Each Click will Flip this Toggle Button: On <--> Off
    {
      // same toggle state: return
      if(a_bIsOn == m_bIsToggled)
      {
        return;
      }

      // store the current toggle state
      m_bIsToggled = a_bIsOn;

      // adjust Cover node
      // NOT toggled, trun on Cover node
      if(false == m_bIsToggled)
      {
        m_pCoverNode.SetActive(true);
      }
      else
      {
        m_pCoverNode.SetActive(false);

        // tell GUI about the toggle
        if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as InventoryLevelGUI)
        {
          (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as InventoryLevelGUI).selectGameLevel(this);
        }
      }
    }

    // slot index
    int m_iSlotIndex;
    // cover node
    GameObject m_pCoverNode;
    // troggle button
    Toggle m_pToggle;
    bool m_bIsToggled;
  }

} // namespace Game.GUI

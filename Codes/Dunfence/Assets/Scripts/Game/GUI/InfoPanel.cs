/**
 * Infomation Panel within Game UI
 */

using UnityEngine;
using UnityEngine.UI;

namespace Game.GUI
{
  public class InfoPanel : MonoBehaviour
  {
    #region PUBLIC METHODS
    // callback from show panel animation, when the animation has finished
    public void onShowPanelDone()
    {
      // hide this info panel
      gameObject.SetActive(false);
    }
    #endregion //PUBLIC METHODS
  }
} // namespace Game.GUI

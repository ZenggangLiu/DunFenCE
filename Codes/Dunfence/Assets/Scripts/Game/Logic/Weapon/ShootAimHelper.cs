/**
 * Shoot Aim Helper Interface
 * - provide the current aiming position in world space
 */

using UnityEngine;

namespace Game.Logic
{
  public interface ShootAimHelper
  {
    #region PUBLIC METHODS
    // public abstract method
    /**
     * return the aimed position
     * return true on success
     */
    bool getAimedPos(out Vector3 a_vAimedPos);
    #endregion // PUBLIC METHODS
  }

} // namespace Game.Logic

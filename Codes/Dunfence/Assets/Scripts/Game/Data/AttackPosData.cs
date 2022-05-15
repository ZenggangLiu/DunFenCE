/**
 * AttackPosData used to store all data related to a AttackPos
 */

using UnityEngine;

namespace Game.Data
{
  public class AttackPosData : ScriptableObject
  {
    // if this AttackPos als supports Monster Shooting: has enough space to accomodate monster shooting
    public bool SupportShoot = true; 
  }

} // namespace Game.Data

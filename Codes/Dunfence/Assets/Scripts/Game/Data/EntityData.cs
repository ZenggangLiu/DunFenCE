/**
 * Used to store Entity Data
 */
using UnityEngine;
using Game.Logic;

namespace Game.Data
{
  public class EntityData : ScriptableObject // MUST be a ScriptableObject in order to save it into file
  {
    #region PUBLIC DATA MEMBERS
    // entity type
    public EntityTypes EntityType = EntityTypes.ENTITY_TYPES_UNKNOWN;

    // common animation trigger name list among all sort of Entity
    // if there are more than ONE animation trigger names for one type of animation, resize the corresponding array
    // spawn
    public string[] SpawnAnimations  = {"Spawn"};
    // hit: normal hit
    public string[] HitAnimations    = {"Hit"};
    // struck: strong hit
    public string[] StruckAnimations = {"Struck"};
    // dead
    public string[] DeadAnimations   = {"Dead"};
    #endregion // PUBLIC DATA MEMBERS
  }

} // namespace Game.Data

/**
 * Used to store Weapon Data
 */

using UnityEngine;
using Game.Logic;

namespace Game.Data
{
  public class WeaponData : EntityData
  {
    #region PUBLIC DATA MEMBERS
    // idle animation
    public string[] IdleAnimations = {"Idle"};

    // ALL TIMES ARE IN SECONDS
    // ALL DISTANCES ARE IN METERS
    // weapon type: MUST be set by code before using this weapon
    public WeaponTypes WeaponType = WeaponTypes.WEAPON_TYPES_UNKNOWN;
    // owner type
    public CharacterTypes OwnerType = CharacterTypes.CHARACTER_TYPES_UNKNOWN;
    // weapon mount type
    public WeaponMountTypes MountType = WeaponMountTypes.WEAPON_MOUNT_TYPES_UNKNOWN;
    // level >= 1
    public int Level = 1;
    // maximal use time: 0 or negative value means infinite usage
    public float UseTime = -1.0f;
    // attack distance: two circles indicating starting of attack(MinAttackDist) and stopping of attack(MaxAttackDist)
    // minimal attack distance: >= 0
    public float MinAttackDist = 0.0f;
    // maximal attack distance: negative value means NO maximal attack distance
    public float MaxAttackDist = -1.0f;

    // damage: forwarded to hit characters
    // damage type
    public DamageTypes DamageType = DamageTypes.DAMAGE_TYPES_UNKNOWN;
    // damage points: >= 0
    public int DamagePoints = -1;
    // damage maximal lasting time: 0 or negative value means one time damage
    public float DamageLastTime = -1.0f;
    // maximal damage range: 0 or negative value means non-range damage
    public float DamageRange = -1.0f;
    // damage upgarde
    // equation: current damage points + current level * DamageUpgradePoints * DamageScale
    // damage upgrade points: >= 0
    public int UpgradePoints = -1;
    // damage upgrade scale factor: > 0
    public float UpgradeScale = 1.0f;
    #endregion // PUBLIC DATA MEMBERS
  }

} // namespace Game.Data

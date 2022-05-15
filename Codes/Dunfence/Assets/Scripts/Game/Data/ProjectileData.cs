using UnityEngine;
using Game.Logic;

namespace Game.Data
{
  public class ProjectileData : WeaponData
  {
    #region PUBLIC DATA MEMBERS
    // flying speed: move speed along local z axis: >= 0
    public float FlySpeed = -1.0f;
    // maximal flying distance: use MaxAttackDist
    // local rotation type
    public RotationTypes RotType = RotationTypes.ROTATION_TYPES_UNKNOWN;
    // rotation speed: clockwise rotation is POSITIVE rotation: any value
    public float RotSpeed = -1.0f;
    // local translate type
    public TranslateTypes TransType = TranslateTypes.TRANSLATE_TYPES_UNKNOWN;
    // translate speed: any value
    public float TransSpeed = 0.0f;
    // maximal translate distance: any value: but same sign as TransSpeed
    public float MaxTransDist = 0.0f;
    // minimal fly speed for fast moving projectile
    public float MinFastFlySpeed = 1.0f;
    #endregion // PUBLIC DATA MEMBERS
  }

} // namespace Game.Data

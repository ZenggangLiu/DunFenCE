using UnityEngine;
using Game.Logic;

namespace Game.Data
{
  public class ShootWeaponData : WeaponData
  {
    #region PUBLIC DATA MEMBERS
    // animations
    // reload animation
    public string[] ReloadAnimations = {"Reload"};
    // shoot animation
    public string[] ShootAnimations  = {"Shoot"};

    // shooting property
    // total projectile count: negative value means INFINITE: != 0
    public int TotalProjectileCount = 0;
    // clip capacity: > 0
    public int ClipCap = 0;
    // consumed projectile per shoot: > 0
    public int ProjectilePerShoot = -1;
    // time needs from one shoot to next: >= 0: 0 means no cool down time
    // ShootA --> ShootCollDown --> ShootB
    public float ShootCoolDownTime = -1.0f; // for a bow: 0(because animation will account the cool down);  for a machine gun: 1/300
    // ClipUsageA --> ReloadTime --> ClipUsageB
    // reload time(cool down time after ClipCap reached ZERO): >= 0: 0 means NO reload time
    public float ReloadTime = -1.0f;

    // shooting Fx
    public GameObject ShootFx = null;

    // projectile property
    // projectile ID
    /**
     * NOTE: by negative value: universal shoot weapon
     * - NO shoot out in doUseCom() 
     * - use doChangeProjectileCom(ID) to change to the given projectile with its ID
     */
    // NO creation and attaching of this projectile within doSpawnCom() in ShootWeapon
    // for Bow, we need to subclass ShootWeapon to do this creation and attaching
    public int ProjectileID = -1;
    // projectile shoot position list: != null
    public GameObject[] ProjectileShootPos = null;
    #endregion // PUBLIC DATA MEMBERS
  }

} // namespace Game.Data

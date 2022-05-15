using UnityEngine;
using UnityEditor;
using Game.Data;

namespace Framework.Utils
{
  public class ShootWeaponDataCreater
  {
    [MenuItem("Assets/Create/GameData/ShootWeaponData")]
    public static void CreateAsset ()
    {
      GameDataUtil.CreateAsset< ShootWeaponData >();
    }
  }

} // namespace Framework.Utils

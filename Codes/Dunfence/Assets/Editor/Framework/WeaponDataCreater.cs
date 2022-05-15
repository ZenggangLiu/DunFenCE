using UnityEngine;
using UnityEditor;
using Game.Data;

namespace Framework.Utils
{
  public class WeaponDataCreater
  {
    [MenuItem("Assets/Create/GameData/WeaponData")]
    public static void CreateAsset ()
    {
      GameDataUtil.CreateAsset< WeaponData >();
    }
  }

} // namespace Framework.Utils

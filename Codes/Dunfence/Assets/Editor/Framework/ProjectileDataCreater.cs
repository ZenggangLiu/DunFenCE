using UnityEngine;
using UnityEditor;
using Game.Data;

namespace Framework.Utils
{
  public class ProjectileDataCreater
  {
    [MenuItem("Assets/Create/GameData/ProjectileData")]
    public static void CreateAsset ()
    {
      GameDataUtil.CreateAsset< ProjectileData >();
    }
  }

} // namespace Framework.Utils

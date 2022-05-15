using UnityEngine;
using UnityEditor;
using Game.Data;

namespace Framework.Utils
{
  public class EntityDataCreater
  {
    [MenuItem("Assets/Create/GameData/EntityData")]
    public static void CreateAsset ()
    {
      GameDataUtil.CreateAsset< EntityData >();
    }
  }

} // namespace Framework.Utils

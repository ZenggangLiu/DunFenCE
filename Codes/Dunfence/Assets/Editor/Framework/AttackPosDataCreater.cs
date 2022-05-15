using UnityEngine;
using UnityEditor;
using Game.Data;

namespace Framework.Utils
{
  public class AttackPosDataCreater
  {
    [MenuItem("Assets/Create/GameData/AttackPosData")]
    public static void CreateAsset ()
    {
      GameDataUtil.CreateAsset< AttackPosData >();
    }
  }

} // namespace Framework.Utils

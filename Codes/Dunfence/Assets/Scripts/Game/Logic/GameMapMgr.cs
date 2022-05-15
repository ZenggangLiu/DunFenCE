using UnityEngine;
using Framework.Utils;

namespace Game.Logic
{
  public class GameMapMgr : TSingleTon < GameMapMgr >
  {
    // PUBLIC ----------
    /**
     * given shoot position, shoot direction and maximal shoot distance
     * it returns the hit position
     * return true, if we can find that hit position
     * NOTE: all positions are in World Coordinate System
     */
    public bool getHitLocation(Vector3 a_vShootPos, Vector3 a_vShootDir, float a_fMaxShootDist, ref Vector3 a_HitPos)
    {
      a_HitPos = new Vector3(10.0f, 10.0f, 10.0f);
      return true;
    }
  }
} // namespace Game.Logic

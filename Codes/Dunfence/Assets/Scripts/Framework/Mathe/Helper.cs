using UnityEngine;

namespace Framework.Mathe
{
  public static class Helper
  {
    /**
     * get the perpendicular vector for the given value a_vOrigVector: <x, y, z>
     * - return: <-y-z, x, y> if z == 0 and x == -y
     * - return: <z ,z ,-x-y> otherwise
     */
    public static Vector3 getPerpendicularVector(Vector3 a_vOrigVector)
    {
      // if z == 0 and x == -y
      if(0 == a_vOrigVector.z
      &&
        a_vOrigVector.x == -a_vOrigVector.y
        )
      {
        return new Vector3(-a_vOrigVector.y - a_vOrigVector.z, a_vOrigVector.x, a_vOrigVector.y);
      }

      // otherwise
      return new Vector3(a_vOrigVector.z, a_vOrigVector.z, -a_vOrigVector.x - a_vOrigVector.y);
    }

    /**
     * transform the given plane a_pOrigPlane with the given matrix
     * 
     * For any plane point V: <Vx, Vy, Vz, 1>, then P dot V == 0 with P represented as <a, b, c, d>
     * P dot V = 0 
     * --> P^t * V = 0, consider P and V as a row matrix and one column matrix respectively
     *
     * If we want to transform a Plane P, and we will get a new Plane P', 
     * - then we want P' dot V' = 0 = P' dot M*V, with M is the transformation and V' any point on Plane P'
     *
     * P' dot (M*V) = P'^t * M * V = 0 = P^t * V
     * ---> P'^t * M == P^t --> P'^t = P^t * M^-1 ---> 
     * P' = (P^t * M^-1)^t = (M^-1)^t * P
     * Then, if we want to transform a Plane P with M, then we have to transform P with
     * 
     * (M^-1)^t
     * just multiply <a, b, c, d> with the Transposes of the Inverse Matrix of M
     */
    public static Plane transformPlane(Matrix4x4 a_mTransformMat, Plane a_pOrigPlane, bool a_bNeedInvertMatrix = true)
    {
      if(true == a_bNeedInvertMatrix)
      {
        Matrix4x4 _mInvMat = a_mTransformMat.inverse;

        Vector4 _vNewPlane = _mInvMat.transpose * a_pOrigPlane.getVector4();

        return new Plane(_vNewPlane);
      }
      else
      {
        Vector4 _vNewPlane = a_mTransformMat.transpose * a_pOrigPlane.getVector4();

        return new Plane(_vNewPlane);
      }
    }
  }
} // namespace Framework.Mathe

/**
 * Plane: if there is a Fix Point P on Plane and a Normal Vector(N: NOT always normalized) and for all other one plane points X
 *            ^
 *         ---|-------
 *        /   | p    /
 *       /  x       /
 *      /_________ /

 * N dot (X - P) = 0 because of the Orthogonality between N and any vector on plane: X - P
 * --> N dot X - N dot P = 0
 * --> N dot X + d = 0 with d == - N dot P: the negative distance of P along N
 * <a, b, c> dot X + d = 0 with Normal is <a, b, c> and NOT always be an unit vector
 * if we represent X as <x, y, z, 1> for any point in 4D space
 * --> <x, y, z, 1> dot < a, b, c, d> = 0: means <x, y, z, 1> is ON the plane
 * Plane is define as a 4D Vector: Vector4, which stores <a, b, c, d>
 * - <a, b, c> is the Normal N of the Plane
 * - d is the distance along the Normal from the Original: d = N dot Q
 *   with N is the Plane's normal and Q is any point lying on this Plane
 *
 * For any point V: <Vx, Vy, Vz, 1>, if P dot V == 0, then V is on the Plane P represented as <a, b, c, d>
 * we can rewrite P dot V = 0 as P^t * V = 0, consider P and V as a row matrix and one column matrix respectively
 *
 * If we want to transform a Plane P, and we will get a new Plane P', 
 * - then we want P' dot V' = 0 = P' dot M*V, with M is the transformation and V' any point on Plane P'
 *
 * P' dot (M*V) = P'^t * M * V = 0 = P^t * V
 * ---> P'^t * M == P^t --> P'^t = P^t * M^-1 ---> 
 * P' = (P^t * M^-1)^t = (M^-1)^t * P
 * Then, if we want to transform a Plane P with M, then we have to transform P with
 * (M^-1)^t
 * just multiply <a, b, c, d> with the Transposes of the Inverse Matrix of M
 */

using UnityEngine;

namespace Framework.Mathe
{
  public class Plane
  {
    #region PUBLIC METHODS
    public enum InclusionTypes
    {
      INCLUSION_TYPES_NEGATIVE_SIDE,
      INCLUSION_TYPES_ON_PLANE,
      INCLUSION_TYPES_POSITIVE_SIDE,
    }

    // default constructor: XZ Plane: <0, 1, 0, 0>: using Y Axis: <0, 1, 0> as the Normal N and 0 as the distance D
    public Plane()
    {
      m_fA = 0.0f;
      m_fB = 1.0f;
      m_fC = 0.0f;
      m_fD = 0.0f;
    }

    // create a plane with the given normal vector and a point on this plane
    public Plane(Vector3 a_vNormal, Vector3 a_vPointOnPlane)
    {
      // store plane normal
      m_fA = a_vNormal.x;
      m_fB = a_vNormal.y;
      m_fC = a_vNormal.z;

      // calculate d: - N dot P
      m_fD = -(Vector3.Dot(a_vNormal, a_vPointOnPlane));
    }

    // create a plane directly with a, b, c, d
    public Plane(float a_fA, float a_fB, float a_fC, float a_fD)
    {
      m_fA = a_fA;
      m_fB = a_fB;
      m_fC = a_fC;
      m_fD = a_fD;
    }

    public Plane(Vector4 a_vPlaneDesc)
    {
      m_fA = a_vPlaneDesc.x;
      m_fB = a_vPlaneDesc.y;
      m_fC = a_vPlaneDesc.z;
      m_fD = a_vPlaneDesc.w;
    }

    // access a
    public float a
    {
      get
      {
        return m_fA;
      }

      set
      {
        m_fA = value;
      }
    }

    // access b
    public float b
    {
      get
      {
        return m_fB;
      }

      set
      {
        m_fB = value;
      }
    }

    // access c
    public float c
    {
      get
      {
        return m_fC;
      }

      set
      {
        m_fC = value;
      }
    }

    // access d
    public float d
    {
      get
      {
        return m_fD;
      }

      set
      {
        m_fD = value;
      }
    }

    // return the plane normal
    public Vector3 getNormal()
    {
      return new Vector3(m_fA, m_fB, m_fC);
    }

    // return the 4D representation of this Plane
    public Vector4 getVector4()
    {
      return new Vector4(m_fA, m_fB, m_fC, m_fD);
    }

    /**
     * normalize the plane equation
     * - normalize the plane normal
     * - normalize the plane distance d
     */
    public static Plane operator! (Plane a_pPlane)  // !Plane
    {
      return a_pPlane.normalize();
    }

    public Plane normalize()
    {
      // get magnitude of the plane's normal vector
      float _fMag = getNormal().magnitude;

      // normalize the normal
      m_fA /= _fMag;
      m_fB /= _fMag;
      m_fC /= _fMag;

      // normalize the distance d
      m_fD /= _fMag;

      // return modified itself
      return this;
    }

    // return the normalized version of the given plane
    public static Plane normalizeAPlane(Plane a_pOrigPlane)
    {
      // clone the given plane
      Plane _pClonedPlane = new Plane(a_pOrigPlane.getVector4());

      // normalize the cloned plane
      _pClonedPlane.normalize();

      return _pClonedPlane;
    }

    // determine the inclusion for the given point
    // 3D point version: <x, y, z>
    public InclusionTypes whichSide(float a_fPx, float a_fPy, float a_fPz)
    {
      // get the dot production of the given point and this plane represented in 4D
      // N dot a_vPoint
      float _fSignedDistance = a_fPx * m_fA + a_fPy * m_fB + a_fPz * m_fC;
      // - N dot PointOnPlane: d
      _fSignedDistance += m_fD;

      return whichSide(_fSignedDistance);
    }

    public InclusionTypes whichSide(Vector3 a_vPoint)
    {
      // get the dot production of the given point and this plane represented in 4D
      // N dot a_vPoint
      float _fSignedDistance = Vector3.Dot(a_vPoint, getNormal());
      // - N dot PointOnPlane: d
      _fSignedDistance += m_fD;

      return whichSide(_fSignedDistance);
    }

    // homogeneous point version: <x, y, z, 1>
    public InclusionTypes whichSide(Vector4 a_vPoint)
    {
      // get the dot production of the given point and this plane represented in 4D
      // N dot a_vPoint - N dot PointOnPlane, if a_vPoint is on Plane, this equation ~= 0
      float _fSignedDistance = Vector4.Dot(a_vPoint, getVector4());

      return whichSide(_fSignedDistance);
    }

    // get the real distance to a plane: the distance to a normalized plane equation
    // 3D point version: <x, y, z>
    public float getDistance(Vector3 a_vPoint)
    {
      // N dot a_vPoint
      float _fSignedDistance = Vector3.Dot(a_vPoint, getNormal());
      // - N dot PointOnPlane: d
      _fSignedDistance += m_fD;

      // get magnitude of the plane's normal vector
      float _fMag = getNormal().magnitude;

      // scale down the signed distance with the magnitude of normal vector
      return _fSignedDistance / _fMag;
    }

    // homogeneous point version: <x, y, z, 1>
    public float getDistance(Vector4 a_vPoint)
    {
      // get the signed distance
      float _fSignedDistance = Vector4.Dot(a_vPoint, getVector4());

      // get magnitude of the plane's normal vector
      float _fMag = getNormal().magnitude;

      // scale down the signed distance with the magnitude of normal vector
      return _fSignedDistance / _fMag;
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // given the signed distance
    // return the inclusion types
    InclusionTypes whichSide(float a_fSignedDistance)
    {
      // if the distance is negative
      if(a_fSignedDistance < 0.0f)
      {
        return InclusionTypes.INCLUSION_TYPES_NEGATIVE_SIDE;
      }

      // if the distance greater than epsilon
      if(a_fSignedDistance > 1.0e-8f)
      {
        return InclusionTypes.INCLUSION_TYPES_POSITIVE_SIDE;
      }

      // otherwise, it has been considered as staying on plane: with positive distance < RSLEPSILON
      return InclusionTypes.INCLUSION_TYPES_ON_PLANE;
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS
    // plane equation: N dot (X - P): N dot X - N dot P = 0
    // <a, b, c> is the plane normal N
    // a
    float m_fA;
    // b
    float m_fB;
    // c
    float m_fC;

    // d := - N dot P
    float m_fD; // the negative distance of P along N
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Framework.Mathe

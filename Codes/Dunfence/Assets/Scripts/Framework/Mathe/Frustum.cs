/**
 * A Frustum contains 6 different planes in the space defined when updating it
 *          \             /
 *         --\-----------/-- far plane
 *            \         /                   *Top view*
 * left plane  \       / right plane
 *              \     /
 *             --\---/-- near plane
 *                \ /
 *                 * camera
 */

using UnityEngine;

namespace Framework.Mathe
{
  public class Frustum
  {
    #region PUBLIC METHODS
    // return a frustum in a_pGameObject's local space using the given camera
    public static Frustum createFrustumInLocalSpace(Camera a_pCamera, GameObject a_pGameObject)
    {
      // create frustum
      Frustum _pFrustum = new Frustum();

      // update the frustum
      _pFrustum.updateFrustumInLocalSpace(a_pCamera, a_pGameObject);

      return _pFrustum;
    }

    // return a ZhunXing(weapon target aim)'s frustum in a_pGameObject's local space using the given camera
    // NOTE: a_fWeaponRadiusRatio: Camera's Radius / WeaponAim's Radius
    public static Frustum createWeaponFrustumInLocalSpace(Camera a_pCamera, float a_fWeaponRadiusRatio, GameObject a_pGameObject)
    {
      // create frustum
      Frustum _pFrustum = new Frustum();

      // update the frustum
      _pFrustum.updateWeaponFrustumInLocalSpace(a_pCamera, a_fWeaponRadiusRatio, a_pGameObject);

      return _pFrustum;
    }

    // return a frustum in world space using the given camera
    public static Frustum createFrustumInWorldSpace(Camera a_pCamera)
    {
      // create frustum
      Frustum _pFrustum = new Frustum();

      // update the frustum
      _pFrustum.updateFrustumInWorldSpace(a_pCamera);

      return _pFrustum;
    }

    // return a ZhunXing(weapon target aim)'s frustum in world space using the given camera
    // NOTE: a_fWeaponRadiusRatio: Camera's Radius / WeaponAim's Radius
    public static Frustum createWeaponFrustumInWorldSpace(Camera a_pCamera, float a_fWeaponRadiusRatio)
    {
      // create frustum
      Frustum _pFrustum = new Frustum();

      // update the frustum
      _pFrustum.updateWeaponFrustumInWorldSpace(a_pCamera, a_fWeaponRadiusRatio);

      return _pFrustum;
    }

    // constructors
    // default constructor
    public Frustum()
    {
      // create clip plane list
      m_aClipPlanes = new Plane[6];

      // create 6 clip planes
      for(int _iPIdx = 0; _iPIdx < m_aClipPlanes.Length; _iPIdx++)
      {
        m_aClipPlanes[_iPIdx] = new Plane();
      }
    }

    // construct a frustum with the given projection matrix in the space given in that matrix
    public Frustum(Matrix4x4 a_mCombinedProjMat)
    {
      // create clip plane list
      m_aClipPlanes = new Plane[6];

      // create 6 clip planes
      for(int _iPIdx = 0; _iPIdx < m_aClipPlanes.Length; _iPIdx++)
      {
        m_aClipPlanes[_iPIdx] = new Plane();
      }

      // update all six clip planes
      updateFrustum(a_mCombinedProjMat);
    }

    /**
     * check if the given point is inside this frustum
     * NOTE: a_vPoint MUST be in the same space as frustum planes
     */
    public bool isInsideFrustum(Vector3 a_vPoint)
    {
      // iterate over all planes
      for(int _iPIdx = 0; _iPIdx < m_aClipPlanes.Length; _iPIdx++)
      {
        // if the given point is on positive side of any clip plane: go to the next plane
        if(Plane.InclusionTypes.INCLUSION_TYPES_POSITIVE_SIDE == m_aClipPlanes[_iPIdx].whichSide(a_vPoint))
        {
          continue;
        }

        // if the given point is outside of any clip plane: the given point is outside of this frustum
        return false;
      }

      // if the given point is on positive side of all clip planes: the given point is inside of this frustum
      return true;
    }

    /**
     * check if the given OBB(Local Bounds) is inside this frustum
     * NOTE: all clip planes must be in the OBB's local space
     */
    public bool isInsideFrustum(Bounds a_pOBB)
    {
      // get the local center and local extents
      return isInsideFrustum(a_pOBB.center, a_pOBB.size * 0.5f);
    }

    /**
     * check if the bounding box created with the given center and extents is inside this frustum
     * NOTE: a_vCenter and a_vExtents must be in the same space as clip planes
     */
    public bool isInsideFrustum(Vector3 a_vCenter, Vector3 a_vExtents)
    {
      /**
       * checking method:
       * - OUSIDE: when ALL corners are outside of ANY clip plane: the given bounding box is outside of the Frustum
       *  \             /
       * --\-----------/-- 
       *    \         /    
       *     \       /    +---+
       *      \     /     |   |
       *     --\---/--    +---+
       *        \ /
       *         v
       * 
       * - otherwise, it is IN. if any corner is on the positive side of any clip plane, goto the next clip plane
       *  \             /
       * --\-----------/-- 
       *    \    +----/-+    
       *     \   |   /  | 
       *      \  +--/---+
       *     --\---/--
       *        \ /
       *         v
       * 
       *  \             /
       * --\-----------/-- 
       * +--\---------/--+    
       * |   \       /   |
       * +----\-----/----+
       *     --\---/--
       *        \ /
       *         v
       *
       *  Wrong Result:
       *      / \
       *    /    \
       *   /   \ /           /
       *   \  --\-----------/-- 
       *    \ /  \         /    
       *          \       /
       *           \     /
       *          --\---/--
       *             \ /
       *              v
       */
      // iterate over all clip planes
      for(int _iPIdx = 0; _iPIdx < m_aClipPlanes.Length; _iPIdx++)
      {
        // check each corner one after another
        if(Plane.InclusionTypes.INCLUSION_TYPES_POSITIVE_SIDE == m_aClipPlanes[_iPIdx].whichSide(a_vCenter.x + a_vExtents.x,
                                                                                                 a_vCenter.y + a_vExtents.y,
                                                                                                 a_vCenter.z + a_vExtents.z)
          )
        {
          // if this corner is on positive side of the current clip plane: go to check against the next clip plane
          continue;
        }

        if(Plane.InclusionTypes.INCLUSION_TYPES_POSITIVE_SIDE == m_aClipPlanes[_iPIdx].whichSide(a_vCenter.x + a_vExtents.x,
                                                                                                 a_vCenter.y + a_vExtents.y,
                                                                                                 a_vCenter.z - a_vExtents.z)
          )
        {
          // if this corner is on positive side of the current clip plane: go to check against the next clip plane
          continue;
        }

        if(Plane.InclusionTypes.INCLUSION_TYPES_POSITIVE_SIDE == m_aClipPlanes[_iPIdx].whichSide(a_vCenter.x + a_vExtents.x,
                                                                                                 a_vCenter.y - a_vExtents.y,
                                                                                                 a_vCenter.z + a_vExtents.z)
          )
        {
          // if this corner is on positive side of the current clip plane: go to check against the next clip plane
          continue;
        }

        if(Plane.InclusionTypes.INCLUSION_TYPES_POSITIVE_SIDE == m_aClipPlanes[_iPIdx].whichSide(a_vCenter.x + a_vExtents.x,
                                                                                                 a_vCenter.y - a_vExtents.y,
                                                                                                 a_vCenter.z - a_vExtents.z)
          )
        {
          // if this corner is on positive side of the current clip plane: go to check against the next clip plane
          continue;
        }

        if(Plane.InclusionTypes.INCLUSION_TYPES_POSITIVE_SIDE == m_aClipPlanes[_iPIdx].whichSide(a_vCenter.x - a_vExtents.x,
                                                                                                 a_vCenter.y + a_vExtents.y,
                                                                                                 a_vCenter.z + a_vExtents.z)
          )
        {
          // if this corner is on positive side of the current clip plane: go to check against the next clip plane
          continue;
        }

        if(Plane.InclusionTypes.INCLUSION_TYPES_POSITIVE_SIDE == m_aClipPlanes[_iPIdx].whichSide(a_vCenter.x - a_vExtents.x,
                                                                                                 a_vCenter.y + a_vExtents.y,
                                                                                                 a_vCenter.z - a_vExtents.z)
          )
        {
          // if this corner is on positive side of the current clip plane: go to check against the next clip plane
          continue;
        }

        if(Plane.InclusionTypes.INCLUSION_TYPES_POSITIVE_SIDE == m_aClipPlanes[_iPIdx].whichSide(a_vCenter.x - a_vExtents.x,
                                                                                                 a_vCenter.y - a_vExtents.y,
                                                                                                 a_vCenter.z + a_vExtents.z)
          )
        {
          // if this corner is on positive side of the current clip plane: go to check against the next clip plane
          continue;
        }

        if(Plane.InclusionTypes.INCLUSION_TYPES_POSITIVE_SIDE == m_aClipPlanes[_iPIdx].whichSide(a_vCenter.x - a_vExtents.x,
                                                                                                 a_vCenter.y - a_vExtents.y,
                                                                                                 a_vCenter.z - a_vExtents.z)
          )
        {
          // if this corner is on positive side of the current clip plane: go to check against the next clip plane
          continue;
        }

        // if ALL corners are outside of the current clip plane: the bounding box is outside of this frustum
        return false;
      }

      // if ONE corner is inside all clip planes: the bounding box is inside of this frustum
      return true;
    }

    // update the clip planes with the world space camera and the given game object's local space
    public void updateFrustumInLocalSpace(Camera a_pCamera, GameObject a_pGameObject)
    {
      updateFrustum(a_pCamera.projectionMatrix * a_pCamera.worldToCameraMatrix * a_pGameObject.transform.localToWorldMatrix);
      /*
      // get local to world transform
      Matrix4x4 _pModelMat = a_pGameObject.transform.localToWorldMatrix;

      // get world to view transform
      Matrix4x4 _pEyeMat = a_pCamera.worldToCameraMatrix;

      // get projection matrix in view/eye space
      Matrix4x4 _pProjMat = a_pCamera.projectionMatrix;

      // combine the view and projection matrix: ProjMat * EyeMat * ModelMat
      Matrix4x4 _pProjEyeModelMat = _pProjMat * _pEyeMat * _pModelMat;

      updateFrustum(_pProjEyeModelMat);
      */
    }

    // update the clip planes with the world space camera, weapon aim and game object's local space
    // NOTE: a_fWeaponRadiusRatio: Camera's Radius / WeaponAim's Radium
    public void updateWeaponFrustumInLocalSpace(Camera a_pCamera, float a_fWeaponRadiusRatio, GameObject a_pGameObject)
    {
      // get projection matrix in view/eye space
      Matrix4x4 _pProjMat = a_pCamera.projectionMatrix;

      // modify the projection matrix according to the given aim radius
      /**
       * projection matrix used within unity: 
       * [n/r         0             0            0
       *   0         n/t            0            0
       *   0          0          f(f+n)/f-n   -2fn/f-n
       *   0          0            -1            0]
       * 
       * ==>
       * [n/R         0             0            0
       *   0         n/R            0            0
       *   0          0          f(f+n)/f-n   -2fn/f-n
       *   0          0            -1            0]
       * with R is the radius of the weapon's aim
       * 
       * n/r = m00 -> n = m00 * r
       * n/R = m00 * r/R = m00 * Ratio
       */
      _pProjMat.m00 = _pProjMat.m00 * a_fWeaponRadiusRatio;
      _pProjMat.m11 = _pProjMat.m00;

      updateFrustum(_pProjMat * a_pCamera.worldToCameraMatrix * a_pGameObject.transform.localToWorldMatrix);

      /*
      // get local to world transform
      Matrix4x4 _pModelMat = a_pGameObject.transform.localToWorldMatrix;

      // get world to view transform
      Matrix4x4 _pEyeMat = a_pCamera.worldToCameraMatrix;

      // get projection matrix in view/eye space
      Matrix4x4 _pProjMat = a_pCamera.projectionMatrix;
      */
      // modify the projection matrix according to the given aim radius
      /**
       * projection matrix used within unity: 
       * [n/r         0             0            0
       *   0         n/t            0            0
       *   0          0          f(f+n)/f-n   -2fn/f-n
       *   0          0            -1            0]
       * 
       * ==>
       * [n/R         0             0            0
       *   0         n/R            0            0
       *   0          0          f(f+n)/f-n   -2fn/f-n
       *   0          0            -1            0]
       * with R is the radius of the weapon's aim
       * 
       * n/r = m00 -> n = m00 * r
       * n/R = m00 * r/R = m00 * Ratio
       */
      /*
      _pProjMat.m00 = _pProjMat.m00 * a_fWeaponRadiusRatio;
      _pProjMat.m11 = _pProjMat.m00;

      // combine the view and projection matrix: ProjMat * EyeMat * ModelMat
      Matrix4x4 _pProjEyeModelMat = _pProjMat * _pEyeMat * _pModelMat;

      updateFrustum(_pProjEyeModelMat);
      */
    }

    // update the clip planes in world space with the given camera
    public void updateFrustumInWorldSpace(Camera a_pCamera)
    {
      updateFrustum(a_pCamera.projectionMatrix * a_pCamera.worldToCameraMatrix);

      /*
      // get world to view transform
      Matrix4x4 _pEyeMat = a_pCamera.worldToCameraMatrix;

      // get projection matrix in view/eye space
      Matrix4x4 _pProjMat = a_pCamera.projectionMatrix;

      // combine the view and projection matrix: ProjMat * EyeMat
      Matrix4x4 _pProjEyeMat = _pProjMat * _pEyeMat;

      // update all clip planes
      updateFrustum(_pProjEyeMat);
      */
    }

    // update the clip planes in world space with the given camera and weapon aim
    // NOTE: a_fWeaponRadiusRatio: Camera's Radius / WeaponAim's Radium
    public void updateWeaponFrustumInWorldSpace(Camera a_pCamera, float a_fWeaponRadiusRatio)
    {
      // get projection matrix in view/eye space
      Matrix4x4 _pProjMat = a_pCamera.projectionMatrix;

      /**
       * projection matrix used within unity: 
       * [n/r         0             0            0
       *   0         n/t            0            0
       *   0          0          f(f+n)/f-n   -2fn/f-n
       *   0          0            -1            0]
       * 
       * ==>
       * [n/R         0             0            0
       *   0         n/R            0            0
       *   0          0          f(f+n)/f-n   -2fn/f-n
       *   0          0            -1            0]
       * with R is the radius of the weapon's aim
       * 
       * n/r = m00 -> n = m00 * r
       * n/R = m00 * r/R = m00 * Ratio
       */
      _pProjMat.m00 = _pProjMat.m00 * a_fWeaponRadiusRatio;
      _pProjMat.m11 = _pProjMat.m00;

      updateFrustum(_pProjMat * a_pCamera.worldToCameraMatrix);

      /*
      // get world to view transform
      Matrix4x4 _pEyeMat = a_pCamera.worldToCameraMatrix;

      // get projection matrix in view/eye space
      Matrix4x4 _pProjMat = a_pCamera.projectionMatrix;
      */
      /**
       * projection matrix used within unity: 
       * [n/r         0             0            0
       *   0         n/t            0            0
       *   0          0          f(f+n)/f-n   -2fn/f-n
       *   0          0            -1            0]
       * 
       * ==>
       * [n/R         0             0            0
       *   0         n/R            0            0
       *   0          0          f(f+n)/f-n   -2fn/f-n
       *   0          0            -1            0]
       * with R is the radius of the weapon's aim
       * 
       * n/r = m00 -> n = m00 * r
       * n/R = m00 * r/R = m00 * Ratio
       */
      /*
      _pProjMat.m00 = _pProjMat.m00 * a_fWeaponRadiusRatio;
      _pProjMat.m11 = _pProjMat.m00;

      // combine the view and projection matrix: ProjMat * EyeMat
      Matrix4x4 _pProjEyeMat = _pProjMat * _pEyeMat;

      // update all clip planes
      updateFrustum(_pProjEyeMat);
      */
    }

    /**
     * update 6 clip planes using the given combined projection matrix
     * because Unity matrix can be considered as the following layout
     * [ m00, m01, m02, m03           x          x'
     *   m10, m11, m12, m13      *    y    =     y'
     *   m20, m21, m22, m23           z          z'
     *   m30, m31, m32, m33]          w          w'
     * <x, y, z, w> is a vertex in any kind of space: world space, model
     * <x', y', z', w'> is that vertex in clip space
     * if we calculate that equation out:
     * x' = m00*x + m01y + m02*z + m03*w
     * y' = m10*x + m11y + m12*z + m13*w
     * z' = m20*x + m21y + m22*z + m23*w
     * w' = m30*x + m31y + m32*z + m33*w
     * 
     * And because in the clip space, we have: according to the projection matrix used in unity:
     * [cot(Fov/2)/AspectRatio    0               0                    0
     *                0                      cot(Fov/2)      0                    0
     *                0                            0          f(f+n)/f-n      -2fn/f-n
     *                0                            0              -1                    0]
     * 
     * 
     * -w' < x' < +w' 
     * -w' < y' < +w' 
     * -w' < z' < +w' 
     * 
     * For the left clip plane, we use -w' < x', we can have x' + w' > 0, so 
     * x' + w' = 0 is the plane equation for the left clip plane
     * (Row3 + Row0) dot <x, y, z, w> -> a = m30 + m00, b = m31 + m01, c = m32 + m02, d = m33 + m03
     * 
     * For the right clip plane, we use x' < w', we can have w' - x' > 0, so
     * w' - x' = 0 is the plane equation for the right clip plane
     * (Row3 - Row0) dot <x, y, z, w> -> a = m30 - m00, b = m31 - m01, c = m32 - m02, d = m33 - m03
     * 
     * For the bottom clip plane, we use -w' < y', we can have y' + w' > 0, so 
     * y' + w' = 0 is the plane equation for the bottom clip plane
     * (Row3 + Row1) dot <x, y, z, w> -> a = m30 + m10, b = m31 + m11, c = m32 + m12, d = m33 + m13
     * 
     * For the top clip plane, we use y' < w', we can have w' - y' > 0, so
     * w' - y' = 0 is the plane equation for the top clip plane
     * (Row3 - Row1) dot <x, y, z, w> -> a = m30 - m10, b = m31 - m11, c = m32 - m12, d = m33 - m13
     * 
     * For the near clip plane, we use -w' < z', we can have z' + w' > 0, so 
     * z' + w' = 0 is the plane equation for the near clip plane
     * (Row3 + Row2) dot <x, y, z, w> -> a = m30 + m20, b = m31 + m21, c = m32 + m22, d = m33 + m23
     * 
     * For the far clip plane, we use z' < w', we can have w' - z' > 0, so
     * w' - z' = 0 is the plane equation for the far clip plane
     * (Row3 - Row2) dot <x, y, z, w> -> a = m30 - m20, b = m31 - m21, c = m32 - m22, d = m33 - m23
     */
    // update the clip planes with the given projection matrix in the space defined in that matrix
    public void updateFrustum(Matrix4x4 a_mCombinedProjMat)
    {
      float _fA, _fB, _fC, _fD;

      // update left clip plane
      // a = m30 + m00, b = m31 + m01, c = m32 + m02, d = m33 + m03
      _fA = a_mCombinedProjMat.m30 + a_mCombinedProjMat.m00;
      _fB = a_mCombinedProjMat.m31 + a_mCombinedProjMat.m01;
      _fC = a_mCombinedProjMat.m32 + a_mCombinedProjMat.m02;
      _fD = a_mCombinedProjMat.m33 + a_mCombinedProjMat.m03;
      m_aClipPlanes[0].a = _fA;
      m_aClipPlanes[0].b = _fB;
      m_aClipPlanes[0].c = _fC;
      m_aClipPlanes[0].d = _fD;

      // update right clip plane
      // a = m30 - m00, b = m31 - m01, c = m32 - m02, d = m33 - m03
      _fA = a_mCombinedProjMat.m30 - a_mCombinedProjMat.m00;
      _fB = a_mCombinedProjMat.m31 - a_mCombinedProjMat.m01;
      _fC = a_mCombinedProjMat.m32 - a_mCombinedProjMat.m02;
      _fD = a_mCombinedProjMat.m33 - a_mCombinedProjMat.m03;
      m_aClipPlanes[1].a = _fA;
      m_aClipPlanes[1].b = _fB;
      m_aClipPlanes[1].c = _fC;
      m_aClipPlanes[1].d = _fD;

      // update bottom clip plane
      // a = m30 + m10, b = m31 + m11, c = m32 + m12, d = m33 + m13
      _fA = a_mCombinedProjMat.m30 + a_mCombinedProjMat.m10;
      _fB = a_mCombinedProjMat.m31 + a_mCombinedProjMat.m11;
      _fC = a_mCombinedProjMat.m32 + a_mCombinedProjMat.m12;
      _fD = a_mCombinedProjMat.m33 + a_mCombinedProjMat.m13;
      m_aClipPlanes[2].a = _fA;
      m_aClipPlanes[2].b = _fB;
      m_aClipPlanes[2].c = _fC;
      m_aClipPlanes[2].d = _fD;

      // update top clip plane
      // a = m30 - m10, b = m31 - m11, c = m32 - m12, d = m33 - m13
      _fA = a_mCombinedProjMat.m30 - a_mCombinedProjMat.m10;
      _fB = a_mCombinedProjMat.m31 - a_mCombinedProjMat.m11;
      _fC = a_mCombinedProjMat.m32 - a_mCombinedProjMat.m12;
      _fD = a_mCombinedProjMat.m33 - a_mCombinedProjMat.m13;
      m_aClipPlanes[3].a = _fA;
      m_aClipPlanes[3].b = _fB;
      m_aClipPlanes[3].c = _fC;
      m_aClipPlanes[3].d = _fD;

      // update near clip plane
      // a = m30 + m20, b = m31 + m21, c = m32 + m22, d = m33 + m23
      _fA = a_mCombinedProjMat.m30 + a_mCombinedProjMat.m20;
      _fB = a_mCombinedProjMat.m31 + a_mCombinedProjMat.m21;
      _fC = a_mCombinedProjMat.m32 + a_mCombinedProjMat.m22;
      _fD = a_mCombinedProjMat.m33 + a_mCombinedProjMat.m23;
      m_aClipPlanes[4].a = _fA;
      m_aClipPlanes[4].b = _fB;
      m_aClipPlanes[4].c = _fC;
      m_aClipPlanes[4].d = _fD;

      // update far clip plane
      // a = m30 - m20, b = m31 - m21, c = m32 - m22, d = m33 - m23
      _fA = a_mCombinedProjMat.m30 - a_mCombinedProjMat.m20;
      _fB = a_mCombinedProjMat.m31 - a_mCombinedProjMat.m21;
      _fC = a_mCombinedProjMat.m32 - a_mCombinedProjMat.m22;
      _fD = a_mCombinedProjMat.m33 - a_mCombinedProjMat.m23;
      m_aClipPlanes[5].a = _fA;
      m_aClipPlanes[5].b = _fB;
      m_aClipPlanes[5].c = _fC;
      m_aClipPlanes[5].d = _fD;
    }

    // return all clip planes in world space
    // NOTE: all clip planes MUST be created within the given game object's local space
    public Plane[] getClipPlanesInWorldSpace(GameObject a_pParentGO)
    {
      // create new clip plane list in world space
      Plane[] _aCLipPlanesWS = new Plane[m_aClipPlanes.Length];

      // iterate over all clip planes of this frustum
      for(int _iIdx = 0; _iIdx < m_aClipPlanes.Length; _iIdx++)
      {
        // transform the current clip plane from local space into world space
        _aCLipPlanesWS[_iIdx] = Framework.Mathe.Helper.transformPlane(a_pParentGO.transform.worldToLocalMatrix, m_aClipPlanes[_iIdx], false);
      }

      return _aCLipPlanesWS;
    }

    // return all clip planes in their own space
    public Plane[] getClipPlanes()
    {
      return m_aClipPlanes;
    }
    #endregion // PUBLIC METHODS

    #region PRIVATE DATA MEMBERS
    // clip plane list: stored order: left, right, bottom, top, near, far
    Plane [] m_aClipPlanes;
    #endregion // PRIVATE DATA MEMBERS
  }
} // namespace Framework.Mathe

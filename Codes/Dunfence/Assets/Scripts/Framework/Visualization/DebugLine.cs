using UnityEngine;

namespace Framework.Visualization
{
  public class DebugLine : MonoBehaviour
  {
    #region PUBLIC DATA MEMBERS
    public Color LineColor = Color.green;
    public float LineLength = 2.0f;
    #endregion // PUBLIC DATA MEMBERS

    #region PRIVATE METHODS
    /**
     * OnDrawGizmos() will be called each frame to draw a Gizoms for the script component
     */
    void OnDrawGizmos()
    {
#if UNITY_EDITOR
      // set gizmos' color
      Gizmos.color = LineColor;

      Vector3 _vAimedPos = transform.position + transform.forward * LineLength;

      Gizmos.DrawLine(transform.position, _vAimedPos);
#endif // #if UNITY_EDITOR
    }
    #endregion // PRIVATE METHODS
  }

} // namespace Framework.Visualization
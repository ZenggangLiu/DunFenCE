using UnityEngine;

namespace Game.Logic
{
  public class RotatingItem : MonoBehaviour
  {
    #region PUBLIC DATA MEMBERS
    public RotationTypes RotType = RotationTypes.ROTATION_TYPES_UNKNOWN;
    // rotation speed: clockwise rotation is POSITIVE rotation: any value
    public float RotSpeed = -1.0f;
    #endregion // PUBLIC DATA MEMBERS

    #region PROTECTED DATA MEMBERS
    // initial orientation
    protected Quaternion m_qInitOrientation;
    // current rotated angle in degree
    protected float m_fCurRotatedAngle_Degree;
    #endregion // PROTECTED DATA MEMBERS

    #region PRIVATE METHODS
    /**
     * Awake() will be called when the script is initialized before game starts
     * Awake() will be called only once during the lifetime of the script instance
     * Awake() will be called after all objects are initialized
     * Awake() will be called out of order: in random order
     */
    void Awake()
    {
      
    }

    /**
     * Start() will be called when the script is ENABLED
     * Start() will be called exactly once in the lifetime of the script
     */
    void Start()
    {
      // store the initial orientation
      m_qInitOrientation = transform.rotation;

      // clear current rotated angle
      m_fCurRotatedAngle_Degree = 0.0f;
    }

    /**
     * Update() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last Update() call
     */
    void Update()
    {
      if(RotType != RotationTypes.ROTATION_TYPES_NO)
      {
        // calculate rotation
        Quaternion _qCurRot = m_qInitOrientation;
        // accumulate rotation angle
        m_fCurRotatedAngle_Degree += RotSpeed * Time.deltaTime;

        if(RotationTypes.ROTATION_TYPES_LOCAL_X == RotType)
        {
          _qCurRot = _qCurRot * Quaternion.AngleAxis(m_fCurRotatedAngle_Degree, Vector3.right);
        }

        else if(RotationTypes.ROTATION_TYPES_LOCAL_Y == RotType)
        {
          _qCurRot = _qCurRot * Quaternion.AngleAxis(m_fCurRotatedAngle_Degree, Vector3.up);
        }

        else if(RotationTypes.ROTATION_TYPES_LOCAL_Z == RotType)
        {
          _qCurRot = _qCurRot * Quaternion.AngleAxis(m_fCurRotatedAngle_Degree, Vector3.forward);
        }

        // adjust world rotation
        transform.rotation = _qCurRot;
      }

    }
    #endregion // PRIVATE METHODS
  }

} // namespace Game.Logic

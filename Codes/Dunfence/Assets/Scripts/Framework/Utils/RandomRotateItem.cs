using UnityEngine;
using System.Collections;

#if UNITY_EDITOR 
using UnityEditor;
#endif

namespace Framework.Utils
{
  [ExecuteInEditMode]
  public class RandomRotateItem : MonoBehaviour 
  {
    void _Transform()
    {
      this.gameObject.transform.Rotate( Vector3.up, Random.Range(0.0f, 360.0f)); 
      this.enabled = false;
    }

    // Use this for initialization
    void Start () 
    {
      this._Transform();
    }

    // Update is called once per frame
    void Update () 
    {

    }

    void OnEnable()
    {
      this._Transform();
    }
  }

} // namespace Framework.Utils

/**
 * the Game Object used AS Logic Object: render component just needed within Editor
 * to show the logic objects(pink cubes) define 
 * SHOW_LOGICOBJECTS in "Scripting Define Symbols" from Player Settings inspector
 */
using UnityEngine;

namespace Framework.Utils
{
  public class LogicObject : MonoBehaviour
  {
    
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
      // want to show logic objects
#if SHOW_LOGICOBJECTS
      // in Game Mode: remove them
      if(false == Application.isEditor)
#endif // SHOW_LOGICOBJECTS
      {
        Component _pRenderComp = gameObject.GetComponent< Renderer >();
        if(null != _pRenderComp)
        {
          GameObject.Destroy(_pRenderComp);
        }
      }
    }
    
    /**
     * Update() will be called every frame, if the script is ENBALED
     * use Time.deltaTime to get the time elapsed from last Update() call
     */
    void Update()
    {

    }
    
    /**
     * LateUpdate() will be called after ALL Update() functions called
     */
    void LateUpdate()
    {

    }
  }

} // namespace Framework.Utils

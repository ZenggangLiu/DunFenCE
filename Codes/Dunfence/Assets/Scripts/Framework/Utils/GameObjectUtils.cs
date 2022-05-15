using UnityEngine;

namespace Framework.Utils
{
  static class GameObjectUtils
  {
    #region PUBLIC METHODS
    /**
     * find the first child having the given a_szChildNodeName
     * Depth-First
     * - the found child on success
     * - null on failure
     */
    public static GameObject FindNode(GameObject a_pRootNode, string a_szChildNodeName)
    {
      if(a_pRootNode.name.Equals(a_szChildNodeName) == true)
      {
        return a_pRootNode;
      }
      else
      {
        for(int _iChildIdx = 0; _iChildIdx < a_pRootNode.transform.childCount; _iChildIdx++)
        {
          GameObject _pFoundNode = FindNode(a_pRootNode.transform.GetChild(_iChildIdx).gameObject, a_szChildNodeName);
          if(null != _pFoundNode)
          {
            return _pFoundNode;
          }
        }

        return null;
      }
    }
    #endregion // PUBLIC METHODS

  }

} // namespace Framework.Utils

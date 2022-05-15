/**
 * ObjectPool: UO, UO, UO, FO, FO,....:
 * - UO means Used Object
 * - FO means Freed Object: all freed object will be put on the tail
 * - ObjectPool functions in this way:
 *   + each time you want an object it will return a freed one: the FIRST FO next to the LAST UO
 *   + each time you want to destroy one, it will put it next to the last UO
 * 
 * Usage: 
 * - allocate the ObjectPool: Utils.ObjectPool< SomeDetailedType > _pObjectPool = new Utils.ObjectPool< SomeDetailedType >();
 * - initialize the object pool: give it a maximal slot count:  _pObjectPool.initialize(IntegerValue);
 */
namespace Framework.Utils
{
  public class ObjectPool < T > where T : class, new() // T must be a class and has a default constructor: T()
  {
    #region PUBLIC METHODS
    // return if the object pool has been initialized
    public bool IsInitialized
    {
      get
      {
        return m_bInitialized;
      }
    }

    // return current active object count
    public int ActiveObjectCount
    {
      get
      {
        return m_iActiveObjectCount;
      }
    }

    // return the current pool size
    public int PoolSize
    {
      get
      {
        return m_iPoolSize;
      }
    }

    // constructor 
    // default
    public ObjectPool()
    {
      // reset all data members
      m_aObjectPool = null;
      m_iPoolSize = 0;
      m_iActiveObjectCount = 0;
      m_bInitialized = false;
    }

    /**
     * create with the given pool size
     */
    public ObjectPool(int a_iPoolSize)
      :
      this() // call the default constructor at first
    {
      initialize(a_iPoolSize);
    }

    /**
     * initialize the object pool with the given size
     */
    public void initialize(int a_iPoolSize)
    {
      // store the pool size
      m_iPoolSize = a_iPoolSize;
      
      // create the object pool
      m_aObjectPool = new T[a_iPoolSize];

      // create all objects stored in this object pool
      for(int _iIdx = 0; _iIdx < a_iPoolSize; ++_iIdx)
      {
        m_aObjectPool[_iIdx] = new T();
      }
      
      // zero the active object count
      m_iActiveObjectCount = 0;

      // set the initialized flag
      m_bInitialized = true;
    }

    /**
     * indexer: ObjectPool < T > pool = new ObjectPool < T >(16); T _pInstance = pool[2];
     * get the active object in the given index
     */
    public T this [int a_iSlotIndex]
    {
      get
      {
        // check if the given index pointing to an active object
        if(a_iSlotIndex >= m_iActiveObjectCount)
        {
          // NO
          return null;
        }

        return m_aObjectPool[a_iSlotIndex];
      }
    }

    /**
     * create a new object: return the first freed object
     */
    public T createObject()
    {
      if(false == m_bInitialized)
      {
        // if NOT initialized
        return null;
      }

      // check if there is a freed object
      if(m_iActiveObjectCount >= m_iPoolSize)
      {
        // if there is NO freed object, resize the object pool
        int _iNewObjectPoolSize = m_iPoolSize << 1; // doubled size

        resizeObjectPool(_iNewObjectPoolSize);

        // store the new pool size
        m_iPoolSize = _iNewObjectPoolSize;
      }

      // return the first freed object and increment active object count
      return m_aObjectPool[m_iActiveObjectCount++];
    }

    /**
     * destroy the object stored in the given slot index
     */
    public bool destroyObject(int a_iSlotIndex)
    {
      if(false == m_bInitialized                  // NOT initialized
         ||                                       // or
         a_iSlotIndex >= m_iActiveObjectCount     // invalid slot index of active objects
        )
      {
        // NOT initialized or out of range
        return false;
      }

      // temporarily store the given object
      T _pDestroyObject = m_aObjectPool[a_iSlotIndex];

      // shift all objects afterwards down ONE slot
      for(int _iIdx = a_iSlotIndex; _iIdx < m_iActiveObjectCount - 1; ++_iIdx)
      {
        m_aObjectPool[_iIdx] = m_aObjectPool[_iIdx + 1];
      }

      // decrement active object count
      m_iActiveObjectCount--;

      // put the destroyed object in the end:
      // NOTE: pool index : [0, active object count -1]
      m_aObjectPool[m_iActiveObjectCount] = _pDestroyObject;

      return true;
    }

    /**
     * destroy the given object
     */
    public bool destroyObject(T a_pDestroyObject)
    {
      for(int _iIdx = 0; _iIdx < m_iActiveObjectCount; ++_iIdx)
      {
        if(System.Object.ReferenceEquals(a_pDestroyObject, m_aObjectPool[_iIdx]))
        {
          // found the given object(same reference as the given one)
          return destroyObject(_iIdx);
        }
      }

      return false;
    }

    /**
     * destroy all objects
     */
    public void destroyAll()
    {
      // reset active object count
      m_iActiveObjectCount = 0;
    }
    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS
    // resize the current object pool to the given size
    protected void resizeObjectPool(int a_iNewSize)
    {
      // create a new array
      T[] _aNewObjectPool = new T[a_iNewSize];

      // create new objects
      for(int _iIdx = m_iPoolSize; _iIdx < a_iNewSize; ++_iIdx)
      {
        _aNewObjectPool[_iIdx] = new T();
      }

      // copy over all existing objects from old object pool
      for(int _iIdx = 0; _iIdx < m_iPoolSize; ++_iIdx)
      {
        _aNewObjectPool[_iIdx] = m_aObjectPool[_iIdx];
      }

      // replace old object pool
      m_aObjectPool = _aNewObjectPool;
    }
    #endregion // PROTECTED METHODS

    #region PROTECTED DATA MEMBERS
    // the T[]
    protected T[] m_aObjectPool;
    // pool size
    protected int m_iPoolSize;
    // current active object count
    protected int m_iActiveObjectCount;
    // initialized flag
    protected bool m_bInitialized;
    #endregion // PROTECTED DATA MEMBERS
  }
} //namespace Framework.Utils

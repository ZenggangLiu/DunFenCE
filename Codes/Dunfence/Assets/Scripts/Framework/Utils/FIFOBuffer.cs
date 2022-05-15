/**
 * FIFO buffer with pre-defined slot count
 * - NOTE: trying to push more elements than the pre-defined slot count will erase oldest elements
 */

namespace Framework.Utils
{
  public class FIFOBuffer < T > where T : class, Clonable< T >, new() // T must be a class and implements Clonable interface and has a default constructor: T()
  {
    #region PUBLIC METHODS
    /**
     * return true, if this FIFO buffer is empty
     */
    public bool isEmpty()
    {
      return 0 == m_pFIFOBuffer.ActiveObjectCount;
    }

    /**
     * clear this FIFO buffer
     */
    public void clear()
    {
      m_pFIFOBuffer.destroyAll();
    }

    /**
     * push the given element onto the head of this FIFO
     */
    public void push(T a_pObject)
    {
      // NO space to push new element: 
      // - destroy oldest one
      // - push the given one
      if(m_pFIFOBuffer.ActiveObjectCount >= m_pFIFOBuffer.PoolSize)
      {
        // destroy the oldest one
        m_pFIFOBuffer.destroyObject(0);
      }

      // get the head free slot
      T _pHeadSlot = m_pFIFOBuffer.createObject();
      if(null == _pHeadSlot)
      {
        return;
      }

      // clone the given object
      _pHeadSlot.clone(a_pObject);
    }

    /**
     * pop the oldest element out of this FIFO
     */
    public T pop()
    {
      // try to get the tail slot via touch
      T _pTailSlot = touch();

      // got the tail slot: pop it up
      if(_pTailSlot != null)
      {
        // destroy this slot
        m_pFIFOBuffer.destroyObject(0);
      }

      // return the popped up object
      return _pTailSlot;
    }

    /**
     * touch the tail slot of this FIFO without popping it out of this FIFO
     */
    public T touch()
    {
      // if the FIFO buffer is empty: return null
      if(0 == m_pFIFOBuffer.ActiveObjectCount)
      {
        return null;
      }

      // get the oldest slot: tail slot at index 0
      T _pTailSlot = m_pFIFOBuffer[0];

      // return the popped up object
      return _pTailSlot;
    }

    /**
     * constructor
     */
    public FIFOBuffer(int a_iSlotCount)
    {
      // create the object pool
      m_pFIFOBuffer = new ObjectPool< T >(a_iSlotCount);
    }
    #endregion // PUBLIC METHODS

    #region PROTECTED DATA MEMBERS
    // the object pool used to simulate FIFO buffer
    ObjectPool < T > m_pFIFOBuffer;
    #endregion // PROTECTED DATA MEMBERS
  }
} // namespace Framework.Utils

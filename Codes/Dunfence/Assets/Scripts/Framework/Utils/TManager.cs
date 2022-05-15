namespace Framework.Utils
{
  public class TManager < T > : TSingleTon < T > where T : class, new() // T must be a class and has a default constructor: T()
  {
    #region PUBLIC METHODS
    /**
     * return true, if this manager has been initialized
     */
    public bool isInitialized
    {
      get
      {
        return m_bIsInitialized;
      }
    }

    #region PUBLIC OVERRIDDENS
    /**
     * the function called before using this manager
     * set m_bIsInitialized to true, on successful initialization
     */
    public virtual void doInitialize()
    {
      
    }

    /**
     * reset this manager
     * clear all internal data members: roll back to the SAME state as after calling doInitialize()
     * STILL marked as initialized
     */
    public virtual void doReset()
    {
      
    }

    // pre-update function
    public virtual void doPreUpdate()
    {

    }

    // update function
    public virtual void doUpdate()
    {
      
    }

    // post-update function called after all update() functions had been called
    public virtual void doPostUpdate()
    {
      
    }

    /**
     * the function called before destroying this manager
     * clear m_bIsInitialized
     */
    public virtual void doDestroy()
    {
      
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    // initialization flag
    protected bool m_bIsInitialized;
  }
} // namespace Framework.Utils

/**
 *  A Thread Safe SingleTon Implementation based on Double Checked Locking
 *  https://en.wikipedia.org/wiki/Double-checked_locking
 *  Usage:
 *    class SomeMgr : Framework.Utils.TSingleTon < SomeMgr >
 *    {
 * 
 *    }
 * 
 *    SomeMgr mgr = SomeMgr.Instance;
 */

namespace Framework.Utils
{
  public class TSingleTon < T > where T : class, new() // T must be a class and has a default constructor: T()
  {
    // public methods
    // public static property: SomeSingleTon.Instance
    public static T Instance
    {
      // only provide Getter NOT Setter
      get
      {
        // 1st check
        if (null == ms_pInstance)
        {
          // lock the initialization lock
#if NEED_THREAD_SAFE
          lock(ms_pInitializedLock)
#endif
          {
            // 2nd (double) check
            if (null == ms_pInstance)
            {
              // create the instance
              ms_pInstance = new T();
              /**
               *  In .NET 1.1, write-release semantics are implicitly handled by marking ms_pInstance with
               *  'volatile', which inserts the necessary memory barriers between the constructor call
               *  and the write to ms_pInstance. 
               *  The barriers created by the lock are not sufficient because the object is made visible before the lock is released. 
               *  In .NET 2.0 and later, the lock is sufficient and 'volatile' is not needed.
               */
            }
          }
        }
        /**
         *  In .NET 1.1, the barriers created by the lock are not sufficient because not all threads will
         *  acquire the lock. 
         *  A fence for read-acquire semantics is needed between the test of ms_pInstance (above) and the use of its contents.
         *  This fence is automatically inserted because ms_pInstance is marked as 'volatile'.
         *  In .NET 2.0 and later, 'volatile' is not required.
         */
        return ms_pInstance;
      }
    }

    // protected methods
    protected TSingleTon()
    {

    }

    // protected variables

    // private methods
    // private variables
    // the lock used during initialization 
#if NEED_THREAD_SAFE
    private static object ms_pInitializedLock = new object();

    // single instance
    private static volatile T ms_pInstance = null; // 'volatile' is unnecessary in .NET 2.0 and later
#else
    // single instance
    private static T ms_pInstance = null;
#endif
  };

} // namespace Framework.Utils

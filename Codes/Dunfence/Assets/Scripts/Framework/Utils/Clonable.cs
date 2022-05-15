/**
 * Interface for all Cloneable Classes
 */

namespace Framework.Utils
{
  public interface Clonable < T > where T : class, new()
  {
    // clone the given object
    void clone(T a_pSourceObj);
  }

} // namespace Framework.Utils

/**
 * Decrete Random Integer Number Generator: [N1, N2]
 * For continuous random float number using unity's version: Random.Value
 */

namespace Framework.Mathe
{
  public class RandomNumberMgr : Utils.TManager< RandomNumberMgr >
  {
    #region PUBLIC METHODS
    public RandomNumberMgr()
    {
      // create random number generator using current timer as seed
      m_pSingleRandomInstance = new System.Random();
    }

    /**
     * return integer value in the range [L, H]
     */
    public int getIntegerVal(int a_iL, int a_iH)
    {
      // Random.Next(minVal, maxVal) returns integer in [minVal, maxVal)
      return m_pSingleRandomInstance.Next(a_iL, a_iH + 1);
    }

    #endregion // PUBLIC METHODS

    #region PRIVATE DATA MEMBERS
    // single random number generator
    System.Random m_pSingleRandomInstance;
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Framework.Mathe

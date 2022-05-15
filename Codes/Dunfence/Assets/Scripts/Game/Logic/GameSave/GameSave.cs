/**
 * Game Save Data
 */

using System.Collections.Generic; // List< T >
using Framework.Utils;

namespace Game.Logic
{
  public class GameSave
  {
    #region PUBLIC DATA MEMBERS
    // ECONOMY
    // reward points
    public int RP;
    // coin collected and purchased
    public int Coins;

    // BOUGHT WEAPON LIST: weapon UID list
    public List< int > BoughtWeaponList;

    // PAU flag
    public bool IsPAU; // paid user
    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    // constructor
    public GameSave()
    {
      // create the weapon list
      BoughtWeaponList = new List< int >(8);

      // reset the game save
      reset();
    }

    /**
     * reset the game save
     */
    public void reset()
    {
      RP = 0;
      Coins = 0;
      BoughtWeaponList.Clear();
      IsPAU = false;
    }

    /**
     * return a json string of the current game save
     */
    public string getData()
    {
      // create a new json for game save data
      JSONObject _pGameSaveJson = JSONObject.Create();
      // economy block
      JSONObject _pEconomyJson = JSONObject.Create();
      // BoughtWeaponList block
      JSONObject _pBoughtWeaponListJson = JSONObject.Create(JSONObject.Type.ARRAY);

      // add economy
      _pEconomyJson.AddField("RP", RP);
      _pEconomyJson.AddField("Coins", Coins);

      // create BoughtWeaponList
      for(int _iIdx = 0; _iIdx < BoughtWeaponList.Count; _iIdx++)
      {
        // insert all bought weapon UIDs
        _pBoughtWeaponListJson.list.Add(new JSONObject(BoughtWeaponList[_iIdx]));
      }

      // insert economy block
      _pGameSaveJson.AddField("Economy", _pEconomyJson);

      // insert BoughtWeaponList block
      _pGameSaveJson.AddField("BoughtWeaponList", _pBoughtWeaponListJson);

      // add PAU flag
      _pGameSaveJson.AddField("IsPAU", IsPAU);

      return _pGameSaveJson.Print();
    }
    #endregion // PUBLIC METHODS
  }

} // namespace Game.Logic

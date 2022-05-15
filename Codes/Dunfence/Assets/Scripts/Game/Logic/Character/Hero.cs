namespace Game.Logic
{
  /**
   * Note: Unity JUST support loading asset using string
   * - Use ResourceMgr::loadResource<T>(string a_szName) to load a given resource(PreFab or Normal File)
   * 
   * Character Asset Naming Convention: Sex_Level_AssetName_Ethnic_MaterialName
   * - Sex: F: female, M: Mail, U: UniSex
   * - Level: minimal level requirement
   * - Ethnic: Heaven(Gold), Asian, Western, Arabian, Hell(for Dungeon)
   * - MaterialName: defines which Material to be used
   * - Example: U_3_TigerHemelt_Asian_GreenDecro
   */
  // Hero: user controlled character
  public class Hero : Character
  {
    //m_pEventQueue = new Framework.Utils.ObjectPool< Game.Event.GameEvent >();
    //m_pEventQueue.initialize(20);  // 20 different events for Hero
  }

}

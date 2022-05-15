/**
 * class used to handle all economy related data: HP, MP, RP, ProP, XP and Coin
 */

using Framework.Utils;

namespace Game.Logic
{
  public class EconomyMgr : TManager < EconomyMgr >
  {
    #region PUBLIC METHODS
    /**
     * return current HP
     */
    public int CurrentHP
    {
      get
      {
        // update HP GUI
        if(GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel)
        {
          return (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).CurrentHP;
        }
        else
        {
          return 0;
        }
      }
    }

    /**
     * return current MP
     */
    public int CurrentMP
    {
      get
      {
        return 100;
      }
    }

    /**
     * return current RP
     */
    public int CurrentRP
    {
      get
      {
        return GameSaveMgr.Instance.CurrentGameSave.RP;
      }
    }

    /**
     * gain HP
     */
    public void gainHP(int a_iHPGained)
    {
      // update HP GUI
      if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.DefenseLevelGUI)
      {
        (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.DefenseLevelGUI).gainHPBar((float)a_iHPGained);
      }

      // let the game level handle this gained HP
      if(GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel)
      {
        (GameLevelMgr.Instance.CurrentGameLevel as FarmFenceLevel).gainHP(a_iHPGained);
      }
    }

    /**
     * gain MP
     */
    public void gainMP(int a_iMPGained)
    {

    }

    /**
     * gain RP
     */
    public void gainRP(int a_iRPGained)
    {
      // if we are in inventory scene: return
      if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.InventoryLevelGUI)
      {
        return;
      }

      // increase RP
      GameSaveMgr.Instance.CurrentGameSave.RP += a_iRPGained;

      // dump game save
      GameSaveMgr.Instance.dumpGameSave();

      // update RP GUI
      if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.DefenseLevelGUI)
      {
        (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.DefenseLevelGUI).updateRPBar(GameSaveMgr.Instance.CurrentGameSave.RP);
      }
    }

    /**
     * deduct RP
     * return true: on success
     */
    public bool deductRP(int a_iRPRequired)
    {
      // within inventory scene: always return true
      if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.InventoryLevelGUI)
      {
        return true;
      }

      // check if the current player has the required RP
      if(GameSaveMgr.Instance.CurrentGameSave.RP - a_iRPRequired < 0)
      {
        return false;
      }

      // deduct RP
      GameSaveMgr.Instance.CurrentGameSave.RP -= a_iRPRequired;

      // dump game save
      GameSaveMgr.Instance.dumpGameSave();

      // update RP GUI
      if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.InventoryLevelGUI)
      {
        (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.InventoryLevelGUI).updateRPBar(GameSaveMgr.Instance.CurrentGameSave.RP);
      }
      else if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.DefenseLevelGUI)
      {
        (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.DefenseLevelGUI).updateRPBar(GameSaveMgr.Instance.CurrentGameSave.RP);
      }

      return true;
    }

    /**
     * gain Coins
     */
    public void gainCoins(int a_iCoinsGained)
    {
      // increase Coins
      GameSaveMgr.Instance.CurrentGameSave.Coins += a_iCoinsGained;

      // dump game save
      GameSaveMgr.Instance.dumpGameSave();

      // update Coins GUI
      if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.InventoryLevelGUI)
      {
        (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.InventoryLevelGUI).updateCoinsBar(GameSaveMgr.Instance.CurrentGameSave.Coins);
      }
      else if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.DefenseLevelGUI)
      {
        (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.DefenseLevelGUI).updateCoinsBar(GameSaveMgr.Instance.CurrentGameSave.Coins);
      }
    }

    /**
     * purchase weapon with the given coins
     * return true: on success
     */
    public bool purchaseWeaponWithCoins(int a_iCoinsRequired, int a_iExpectedWeaponUID)
    {
      // check if the current player has the required Coins
      // NO: return
      if(GameSaveMgr.Instance.CurrentGameSave.Coins - a_iCoinsRequired < 0)
      {
        return false;
      }

      // deduct Coins
      GameSaveMgr.Instance.CurrentGameSave.Coins -= a_iCoinsRequired;

      // insert this weapon UID into BoughtWeaponList
      GameSaveMgr.Instance.CurrentGameSave.BoughtWeaponList.Add(a_iExpectedWeaponUID);

      // dump game save
      GameSaveMgr.Instance.dumpGameSave();

      // update Coins GUI
      if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.InventoryLevelGUI)
      {
        (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.InventoryLevelGUI).updateCoinsBar(GameSaveMgr.Instance.CurrentGameSave.Coins);
      }
      else if(GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.DefenseLevelGUI)
      {
        (GameLevelMgr.Instance.CurrentGameLevel.GameLevelGUI as Game.GUI.DefenseLevelGUI).updateCoinsBar(GameSaveMgr.Instance.CurrentGameSave.Coins);
      }

      return true;
    }

    /**
     * callback when a loot has been hit: loot handling
     */
    public void onGotLoot(LootTypes a_eType,  // loot type
                          int a_iLootData     // loot related data
                         )
    {
      switch(a_eType)
      {
        case LootTypes.LOOT_TYPES_COINPACK:
        {
          gainCoins(a_iLootData);

          break;
        }

        case LootTypes.LOOT_TYPES_HEALTHPACK:
        {
          gainHP(a_iLootData);

          break;
        }

        case LootTypes.LOOT_TYPES_REWARDPACK:
        {
          gainRP(a_iLootData);

          break;
        }

        default:
        {
          break;
        }
      }
    }

    #region PUBLIC OVERRIDDENS
    // the function called before using this manager
    // set m_bIsInitialized to true, on successful initialization
    public override void doInitialize()
    {
      m_bIsInitialized = true;
    }

    // pre-update function
    public override void doPreUpdate()
    {

    }

    // update function
    public override void doUpdate()
    {

    }

    // post-update function called after all update() functions had been called
    public override void doPostUpdate()
    {

    }

    // the function called before destroying this manager
    public override void doDestroy()
    {
      m_bIsInitialized = false;
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS
  }
} // namespace Game.Logic

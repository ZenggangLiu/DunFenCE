/**
 * IAP Item Purchase Manager
 * - used to buy Apple Store, Google Play Store Items
 */

using UnityEngine;
using Framework.Utils;
using Framework.Logger;
using UnityEngine.Purchasing;  // IAP
using System.Collections.Generic; // Dictionary< TKey, TVal >

namespace Game.Logic
{
  // item info
  public struct ItemInfo
  {
    // store generic unity item id
    public string ID;
    // localized item name/title
    public string Name;
    // localized item description
    public string Desc;
    // localized price
    public decimal Price;
    // localized price string
    public string PriceString;
    // ISO currency code
    public string CurrencyCode;
    // GUI sprite name
    public string SpriteName;
    // gained coins amount with a successful purchase
    public int GainedCoinAmount;
    // consumable flag
    public bool IsConsumable;
  }

  // Deriving the Purchaser class from IStoreListener enables it to receive messages from Unity Purchasing.
  public class IAPItemPurchaseMgr : TManager < IAPItemPurchaseMgr >, IStoreListener
  {
    #region PUBLIC METHODS
    /**
     * return item info cache
     */
    public Dictionary< int, ItemInfo > CurrentItemInfoCache
    {
      get
      {
        return m_mItemInfoCache;
      }
    }

    /**
     * try to purchase the given item id
     * return true on success
     */
    public bool buyItem(int a_iItemID)
    {
      // NOT yet initialized: return
      if(false == m_bIsInitialized)
      {
        DefaultGameLogger.Instance.dumpLogError("[IAPItemPurchaseMgr][buyItem]: IAPItemPurchaseMgr has NOT been initialized!. Please intialize it at first");

        return false;
      }

      // NOT found
      if(m_mItemInfoCache.ContainsKey(a_iItemID) == false)
      {
        DefaultGameLogger.Instance.dumpLogError("[IAPItemPurchaseMgr][buyItem]: try to buy a NON-Existing Item: " + a_iItemID);

        return false;
      }

      // try to get the item from unity store
      Product _pStoreItem = m_pUnityStoreController.products.WithID(m_mItemInfoCache[a_iItemID].ID);

      // ready to buy
      if (_pStoreItem != null
          &&
          true == _pStoreItem.availableToPurchase
         )
      {
#if !RETAIL
        DefaultGameLogger.Instance.dumpLogInfo("[IAPItemPurchaseMgr][buyItem]: try to buy " + _pStoreItem.metadata.localizedTitle + "[" + a_iItemID + "]");
#endif

        // asynchronously buy this item
        // expect a response either through ProcessPurchase or OnPurchaseFailed 
        m_pUnityStoreController.InitiatePurchase(_pStoreItem);

        return true;
      }

      // NOT ready
      else
      {
        DefaultGameLogger.Instance.dumpLogError("[IAPItemPurchaseMgr][buyItem]: " + _pStoreItem.metadata.localizedTitle + "[" + a_iItemID + "] is NOT ready to buy");

        return false;
      }
    }

    /**
     * restore previously made purchases
     * - Google restores the purchase automatically
     * - Apple  requires explicit purchase restoration for IAP, conditionally displaying a password prompt
     * return true on success
     */
    public bool restorePurchase()
    {
      // NOT yet initialized: return
      if(false == m_bIsInitialized)
      {
        DefaultGameLogger.Instance.dumpLogError("[IAPItemPurchaseMgr][restorePurchase]: IAPItemPurchaseMgr has NOT been initialized!. Please intialize it at first");

        return false;
      }

      // NOT on iOS and Mac
      if (RuntimePlatform.IPhonePlayer != Application.platform
          &&
          RuntimePlatform.OSXPlayer    != Application.platform
        )
      {
        DefaultGameLogger.Instance.dumpLogError("[IAPItemPurchaseMgr][restorePurchase]: try to restore purchase on NON-Apple platform: " + Application.platform.ToString());

        return false;
      }

      // fetch the Apple store-specific subsystem.
      IAppleExtensions _pAppleSubSystem = m_pSpecificStoreProvider.GetExtension<IAppleExtensions>();

      // start asynchronous process of restoring purchases
      // expect a confirmation response in the Action<bool> parameter of RestoreTransactions() below, and ProcessPurchase if there are previously purchased products to restore.
      _pAppleSubSystem.RestoreTransactions((result) => 
                                           {
        
                                           }
                                          );

      return true;
    }

    #region PUBLIC OVERRIDDENS
    /**
     * the function called before using this manager
     * set m_bIsInitialized to true, on successful initialization
     */
    public override void doInitialize()
    {
      // already initialized: return
      if(true == m_bIsInitialized)
      {
        return;
      }

      // create item ID --> game ID map
      m_mItemIDMap = new Dictionary< string, int >(5); // < ItemID, GameID >

      // create item info cache
      m_mItemInfoCache = new Dictionary < int, ItemInfo >(5); // < GameID, ItemInf >

      // load in configure data and build map and cache
      loadConfigFile();

      // create the store builder, first passing in a suite of Unity provided stores.
      ConfigurationBuilder _pStoreBuilder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

      // add all items
      foreach(int _iGameID in m_mItemInfoCache.Keys)
      {
        if(true == m_mItemInfoCache[_iGameID].IsConsumable)
        {
          _pStoreBuilder.AddProduct(m_mItemInfoCache[_iGameID].ID, ProductType.Consumable);
        }
        else
        {
          _pStoreBuilder.AddProduct(m_mItemInfoCache[_iGameID].ID, ProductType.NonConsumable);
        }
      }

      // asynchrounously initialize unity iap wit the given configuration and listener
      UnityPurchasing.Initialize(this, _pStoreBuilder);
    }

    /**
     * the function called before destroying this manager
     * clear m_bIsInitialized
     */
    public override void doDestroy()
    {
      // clear initialization flag
      m_bIsInitialized = false;
    }

    // --------- INTERFACE OF IStoreListener ------------//
    // callback of successful initialization
    public void OnInitialized (IStoreController a_pUnityStoreController, IExtensionProvider a_pSpecificStoreProvider)
    {
      // store the given unity purchase system
      m_pUnityStoreController = a_pUnityStoreController;

      // store the application store specific purchase sub-system
      m_pSpecificStoreProvider = a_pSpecificStoreProvider;

      // read in item details
      readItemDetails(a_pUnityStoreController.products.all);

      // set initialization flag
      m_bIsInitialized = true;
    }

    // callback of failed initialization
    public void OnInitializeFailed (InitializationFailureReason a_pError)
    {
      DefaultGameLogger.Instance.dumpLogError("[IAPItemPurchaseMgr][OnInitializeFailed]: Can NOT initialize Unity Item Store: " + a_pError);

      // reset initialization flag
      m_bIsInitialized = false;
    }

    // callback of successful purchase
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs a_pBoughtItem) 
    {
#if !RETAIL
      DefaultGameLogger.Instance.dumpLogInfo("[IAPItemPurchaseMgr][ProcessPurchase]: bought Item: " +a_pBoughtItem.purchasedProduct.metadata.localizedTitle);
#endif

      // find the game specific id of the bought item
      int _iItemID = m_mItemIDMap[a_pBoughtItem.purchasedProduct.definition.id];

      // set PAU flag
      GameSaveMgr.Instance.CurrentGameSave.IsPAU = true;

      // fire event
      Event.GameEventMgr.Instance.fireGlobalScopeEvent(Game.Event.EventIDs.EVENT_IDS_LOGIC_IAP_PURCHASE_SUCCEEDED,
                                                       _iItemID,
                                                       Game.Event.EventTriggerType.EVENT_TRIGGER_TYPE_LOCAL,
                                                       this
                                                      );

      // return a flag indicating whether this product has completely been received,
      // or if the application needs to be reminded of this purchase at next app launch
      // use PurchaseProcessingResult.Pending when still saving purchased products to the cloud, and when that save is delayed. 
      return PurchaseProcessingResult.Complete;
    }

    // callback of failed purchase
    public void OnPurchaseFailed(Product a_pFailedItem, PurchaseFailureReason a_eFailedReason)
    {
      DefaultGameLogger.Instance.dumpLogError("[IAPItemPurchaseMgr][OnPurchaseFailed]: can NOT buy Item: " +a_pFailedItem.metadata.localizedTitle);

      // find the game specific id of the bought item
      int _iItemID = m_mItemIDMap[a_pFailedItem.definition.id];

      // fire event
      Event.GameEventMgr.Instance.fireGlobalScopeEvent(Game.Event.EventIDs.EVENT_IDS_LOGIC_IAP_PURCHASE_FAILED,
                                                       _iItemID,
                                                       Game.Event.EventTriggerType.EVENT_TRIGGER_TYPE_LOCAL,
                                                       this
                                                      );
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PRIVATE METHODS
    // load in store configure data file and build map and cache
    void loadConfigFile()
    {
      // try to load in StoreItemData.json file
      TextAsset _pStoreItemDataJsonFile = Resources.Load("StoreItemData") as TextAsset; // an TextAsset: the representation of a Textural or Binary File

      // can NOT load StoreItemData.json
      if(null == _pStoreItemDataJsonFile)
      {
        DefaultGameLogger.Instance.dumpLogError("[IAPItemPurchaseMgr][loadConfigFile]: Can NOT load StoreItemData.json file");

        return;
      }

      // create and parse the json data at the same time
      JSONObject _pStoreItemDataJson = new JSONObject(_pStoreItemDataJsonFile.text);

      if(true == _pStoreItemDataJson.HasField("ItemList")   // StoreItemData.json does contain ItemList array
         &&                                                 // and
         true == _pStoreItemDataJson["ItemList"].IsArray    // ItemList element is an array
         &&                                                 // and
         0 != _pStoreItemDataJson["ItemList"].Count         // ItemList is NOT empty
        )
      {
        // clear the id map and item info cache
        m_mItemIDMap.Clear();
        m_mItemInfoCache.Clear();

        // insert all item defines
        for(int _iIdx = 0; _iIdx < _pStoreItemDataJson["ItemList"].Count; _iIdx++)
        {
          // add this item
          if(_pStoreItemDataJson["ItemList"][_iIdx].HasField("ID")
             &&
             _pStoreItemDataJson["ItemList"][_iIdx]["ID"].IsNumber
             &&
             _pStoreItemDataJson["ItemList"][_iIdx].HasField("StoreID")
             &&
             _pStoreItemDataJson["ItemList"][_iIdx].HasField("Consumable")
             &&
             _pStoreItemDataJson["ItemList"][_iIdx]["Consumable"].IsBool
             &&
             _pStoreItemDataJson["ItemList"][_iIdx].HasField("SpriteName")
             &&
             _pStoreItemDataJson["ItemList"][_iIdx]["SpriteName"].IsString
             &&
             _pStoreItemDataJson["ItemList"][_iIdx].HasField("GainedCoinAmount")
             &&
             _pStoreItemDataJson["ItemList"][_iIdx]["GainedCoinAmount"].IsNumber
            )
          {
#if !RETAIL
            DefaultGameLogger.Instance.dumpLogInfo("[IAPItemPurchaseMgr][loadConfigFile] Added store item[" + _pStoreItemDataJson["ItemList"][_iIdx]["ID"].i + "]: " + _pStoreItemDataJson["ItemList"][_iIdx]["StoreID"].str);
#endif

            // add into id map
            m_mItemIDMap[_pStoreItemDataJson["ItemList"][_iIdx]["StoreID"].str] = (int)_pStoreItemDataJson["ItemList"][_iIdx]["ID"].i;

            // create the item info
            ItemInfo _pNewItemInfo = new ItemInfo();
            _pNewItemInfo.ID = _pStoreItemDataJson["ItemList"][_iIdx]["StoreID"].str;
            _pNewItemInfo.IsConsumable = _pStoreItemDataJson["ItemList"][_iIdx]["Consumable"].b;
            _pNewItemInfo.SpriteName = _pStoreItemDataJson["ItemList"][_iIdx]["SpriteName"].str;
            _pNewItemInfo.GainedCoinAmount = (int)_pStoreItemDataJson["ItemList"][_iIdx]["GainedCoinAmount"].i;
            m_mItemInfoCache[(int)_pStoreItemDataJson["ItemList"][_iIdx]["ID"].i] = _pNewItemInfo;
          }
        }
      }
    }

    // read in all details of the given items
    void readItemDetails(Product[] a_aStoreItemList)
    {
      // NO store item at all: return
      if(null == a_aStoreItemList
         ||
         0 == a_aStoreItemList.Length
        )
      {
        return;
      }

      // store details of all items
      for(int _iIdx = 0; _iIdx < a_aStoreItemList.Length; _iIdx++)
      {
        // unknown item
        if(m_mItemIDMap.ContainsKey(a_aStoreItemList[_iIdx].definition.id) == false)
        {
          DefaultGameLogger.Instance.dumpLogError("[IAPItemPurchaseMgr][readItemDetails]: Unknown Item[" + a_aStoreItemList[_iIdx].definition.id + "]: " + a_aStoreItemList[_iIdx].metadata.localizedTitle + "  found in Application Store");

          continue;
        }

        // store all details
        // copy the stored item info
        ItemInfo _pCurItemInfo = m_mItemInfoCache[m_mItemIDMap[a_aStoreItemList[_iIdx].definition.id]];

        // update IAP item details
        _pCurItemInfo.Name         = a_aStoreItemList[_iIdx].metadata.localizedTitle;
        _pCurItemInfo.Desc         = a_aStoreItemList[_iIdx].metadata.localizedDescription;
        _pCurItemInfo.Price        = a_aStoreItemList[_iIdx].metadata.localizedPrice;
        _pCurItemInfo.PriceString  = a_aStoreItemList[_iIdx].metadata.localizedPriceString;
        _pCurItemInfo.CurrencyCode = a_aStoreItemList[_iIdx].metadata.isoCurrencyCode;

        // store the updated item info
        m_mItemInfoCache[m_mItemIDMap[a_aStoreItemList[_iIdx].definition.id]] = _pCurItemInfo;
      }
    }
    #endregion // PRIVATE METHODS

    #region PRIVATE DATA MEMBERS

    // unity purchase system
    IStoreController m_pUnityStoreController;
    // store specific purchase sub-system
    IExtensionProvider m_pSpecificStoreProvider;
    // item ID --> GameID map
    Dictionary< string, int > m_mItemIDMap;  // < ItemID, GameID >
    // item info cache
    Dictionary< int, ItemInfo > m_mItemInfoCache; // < GameID, ItemInf >
    #endregion // PRIVATE DATA MEMBERS
  }

} // namespace Game.Logic

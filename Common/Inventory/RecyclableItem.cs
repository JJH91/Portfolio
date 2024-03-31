using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab.ClientModels;
using PolyAndCode.UI;
using Sirenix.OdinInspector;

public abstract class RecyclableItem<ItemDataClass> : AddressableUsingAssetSeriallizedMonoBehavior, ICell where ItemDataClass : RecyclableItemData
{
    [TitleGroup("Recyclable ScrollRect Item"), BoxGroup("Recyclable ScrollRect Item/RSI", showLabel: false)]
    [BoxGroup("Recyclable ScrollRect Item/RSI/Item Data"), ShowInInspector] public ItemDataClass ItemData { get; set; }

    [TitleGroup("UI"), BoxGroup("UI/U", showLabel: false)]
    [BoxGroup("UI/U/Rect Transform"), ShowInInspector, ReadOnly] public RectTransform RectTransform { get; private set; }

    public event Action<string, Action> ReloadViewerWithDataRemoveAction;
    public event Action<string, Action> RefreshViewerWithDataRemoveAction;

    // ? This action conneted viewer action. If want to add any method on this, override like 'OnItemSelet' method.
    public event Action<ItemDataClass> OnItemSelectAction;
    public event Action<ItemDataClass> OnItemClickAction;
    public event Action<ItemDataClass> OnItemLongSelectAction;

    protected override void Awake()
    {
        isUI = true;

        RectTransform = GetComponent<RectTransform>();

        base.Awake();
    }

    /// <summary>
    /// Update item data and UI. Override it inherited class.
    /// </summary>
    /// <param name="data"> Cell Data. </param>
    /// <param name="cellIndex"> Cell Index. </param>
    public virtual void ConfigureCell(object data, int cellIndex)
    {
        ReloadViewerWithDataRemoveAction = null;
        RefreshViewerWithDataRemoveAction = null;
        OnItemSelectAction = null;
        OnItemClickAction = null;
        OnItemLongSelectAction = null;

        if (data is KeyValuePair<string, ItemDataClass> kvp)
        {
            ItemData = kvp.Value;
            ItemData.Index = cellIndex;
        }
        else
            gameObject.LogError($"data can't cast to KeyValuePair<string, ItemDataClass>");
        // var kvp = data as KeyValuePair<string, ItemDataClass>?;
        // ItemData = kvp.Value.Value;
        // ItemData.Index = cellIndex;

        UpdateUI();

        // When modal window using this item, localScale changed cause window on/off animation.(maybe...) So, init localScale.
        RectTransform.localScale = Vector3.one;
    }

    [BoxGroup("Recyclable ScrollRect Item/RSI/Update UI"), Button("Update UI", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
    public abstract void UpdateUI();

    public virtual void InvokeOnItemSelectAction()
    {
        OnItemSelectAction?.Invoke(ItemData);
    }

    public virtual void InvokeOnItemClickAction()
    {
        OnItemClickAction?.Invoke(ItemData);
    }

    public virtual void InvokeOnItemLongSelectAction()
    {
        OnItemLongSelectAction?.Invoke(ItemData);
    }

    protected void InvokeReloadViewerWithDataRemoveAction(string key, Action completeAction = null)
    {
        ReloadViewerWithDataRemoveAction?.Invoke(key, completeAction);
    }

    protected void InvokeRefreshViewerWithDataRemoveAction(string key, Action completeAction = null)
    {
        RefreshViewerWithDataRemoveAction?.Invoke(key, completeAction);
    }
}

[Serializable]
public class RecyclableItemData
{
    [TitleGroup("Recyclable ScrollRect Item Data"), BoxGroup("Recyclable ScrollRect Item Data/RSID", showLabel: false)]
    [BoxGroup("Recyclable ScrollRect Item Data/RSID/Data"), ShowInInspector] public string Key { get; set; }
    [BoxGroup("Recyclable ScrollRect Item Data/RSID/Data"), ShowInInspector] public int Index { get; set; }
    [BoxGroup("Recyclable ScrollRect Item Data/RSID/Data"), ShowInInspector] public bool WasRetriedPlayFabAPIRequest { get; set; }
    [BoxGroup("Recyclable ScrollRect Item Data/RSID/Data"), ShowInInspector] public CatalogItem CatalogItem { get; set; }
    [BoxGroup("Recyclable ScrollRect Item Data/RSID/Data"), ShowInInspector] public PlayFabManager.CatalogItemClass CatalogItemClass { get; set; }
    [BoxGroup("Recyclable ScrollRect Item Data/RSID/Data"), ShowInInspector] public PlayFabManager.CatalogItemTag CatalogItemMainTag { get; set; }
    [BoxGroup("Recyclable ScrollRect Item Data/RSID/Data"), ShowInInspector] public ItemInstance ItemInstance { get; set; }
    [BoxGroup("Recyclable ScrollRect Item Data/RSID/Data"), ShowInInspector] public int ItemAmount { get; set; }
    [BoxGroup("Recyclable ScrollRect Item Data/RSID/Data"), ShowInInspector] public object ExtraItemData { get; set; }

    public RecyclableItemData AssembleRecyclableItemData(CatalogItem catalogItem)
    {
        Key = catalogItem.ItemId;
        WasRetriedPlayFabAPIRequest = false;
        CatalogItem = catalogItem;
        CatalogItemClass = CatalogItem.ItemClass.ToCachedEnum<PlayFabManager.CatalogItemClass>();
        CatalogItemMainTag = CatalogItem.GetMainTag();

        return this;
    }

    public RecyclableItemData AssembleRecyclableItemData(ItemInstance itemInstance)
    {
        AssembleRecyclableItemData(PlayFabManager.Instance.MainCatalogItemDictionary[itemInstance.ItemId]);
        Key = itemInstance.ItemInstanceId;
        WasRetriedPlayFabAPIRequest = false;
        ItemInstance = itemInstance;

        return this;
    }
}

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PolyAndCode.UI;
using Newtonsoft.Json.Linq;
using PlayFab.ClientModels;
using Sirenix.OdinInspector;

public class InventoryItemViewer : RecyclableItemViewer<InventoryItem, InventoryItemData>
{
    [TitleGroup("Inventory Item Viewer"), BoxGroup("Inventory Item Viewer/IIV", showLabel: false)]
    [BoxGroup("Inventory Item Viewer/IIV/Is Display Equipped"), ShowInInspector, ReadOnly] public bool isDisplayBlackOverlay { get; set; }
    [BoxGroup("Inventory Item Viewer/IIV/Inventory Item List"), ShowInInspector, ReadOnly]
    public List<InventoryItem> InventoryItemList
    {
        get => RecyclableItemList.Select(cell => cell.GetComponent<InventoryItem>()).ToList();
    }

    // ? 인벤토리 혹은 카탈로그 상의 아이템을 일단 데이터로 받고, 인벤토리 아이템 데이터로 변환하여 아이템 데이터 딕셔너리에 저장

    public override void SetCell(ICell cell, int index)
    {
        base.SetCellWithOutConfigure(cell, index);

        var item = cell as InventoryItem;
        item.ConfigureCell(InventoryItem.InventoryItemType.SimpleType, InventoryItem.AmountDisplayType.RemainUses, ItemDataDictionary.ElementAt(index), index, isDisplayBlackOverlay);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Reset Item Data *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    protected override void ResetItemDataDictionary(object itemDataDictionary)
    {
        base.ResetItemDataDictionaryFromPlayfabUserInventory(itemDataDictionary);

        // Ordering ItemDataDictionary.
        var sortedDictionary = ItemDataDictionary
            .OrderBy(kvp => kvp.Value.CatalogItemMainTag)
            .OrderByDescending(kvp => kvp.Value.Grade)
            .ThenByDescending(kvp => kvp.Value.PrmGrade)
            .ThenByDescending(kvp => kvp.Value.AwakeningLv)
            .ThenByDescending(kvp => kvp.Value.Lv)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        ItemDataDictionary = sortedDictionary;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                    * Ordering *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void OderingItemDataDictionary(Dictionary<string, InventoryItemData> orderedItemDataDictionary, bool isRefresh, Action completeAction = null)
    {
        ResetViewerInfoData();
        ItemDataDictionary = orderedItemDataDictionary;
        gameObject.Log($"RecyclableItemList Cleared: OderingItemDataDictionary({orderedItemDataDictionary})");

        if (isRefresh)
        {
            PreScrollPosition = recyclableScrollRect.normalizedPosition;

            recyclableScrollRect.ReloadData(completeAction);

            SetPreScrollPosition();
        }
        else
            recyclableScrollRect.ReloadData(completeAction);
    }

    public void OderingItemDataDictionary(List<OrderingData> orderDataList, bool isReload, bool isRefresh = false, Action completeAction = null)
    {
        foreach (var orderingData in orderDataList)
            Ordering(orderingData);

        if (isReload)
            ReloadViewerWithData(ItemDataDictionary, completeAction);
        if (isRefresh)
            RefreshViewerWithData(ItemDataDictionary, completeAction);
    }

    void Ordering(OrderingData orderingData)
    {
        Dictionary<string, InventoryItemData> orderedItemDataDictionary;
        if (orderingData.orderBy == Inventory.OrderBy.Ascending)
            orderedItemDataDictionary = ItemDataDictionary.OrderBy(kvp =>
            {
                return orderingData.orderAbout switch
                {
                    Inventory.OrderAbout.AwakeningLv => (kvp.Value.ExtraItemData as EquipmentStatModifierData).AwakeningLv,
                    Inventory.OrderAbout.Lv => kvp.Value.Lv,
                    Inventory.OrderAbout.PrmGrade => (int)(kvp.Value.ExtraItemData as EquipmentStatModifierData).PrmGrade,
                    Inventory.OrderAbout.Grade => (int)(kvp.Value.ExtraItemData as EquipmentStatModifierData).Grade,
                    Inventory.OrderAbout.RemainingUses => kvp.Value.ItemInstance.RemainingUses.HasValue ? 1 : 0,
                    Inventory.OrderAbout.Equipped => !(kvp.Value.ExtraItemData as EquipmentStatModifierData).EquippedCharacterInstId.IsNullOrEmpty() ? 1 : 0,
                    _ => 1
                };
            }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        else
            orderedItemDataDictionary = ItemDataDictionary.OrderByDescending(kvp =>
            {
                return orderingData.orderAbout switch
                {
                    Inventory.OrderAbout.AwakeningLv => (kvp.Value.ExtraItemData as EquipmentStatModifierData).AwakeningLv,
                    Inventory.OrderAbout.Lv => kvp.Value.Lv,
                    Inventory.OrderAbout.PrmGrade => (int)(kvp.Value.ExtraItemData as EquipmentStatModifierData).PrmGrade,
                    Inventory.OrderAbout.Grade => (int)(kvp.Value.ExtraItemData as EquipmentStatModifierData).Grade,
                    Inventory.OrderAbout.RemainingUses => kvp.Value.ItemInstance.RemainingUses.HasValue ? 1 : 0,
                    Inventory.OrderAbout.Equipped => !(kvp.Value.ExtraItemData as EquipmentStatModifierData).EquippedCharacterInstId.IsNullOrEmpty() ? 1 : 0,
                    _ => 1
                };
            }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        ItemDataDictionary = orderedItemDataDictionary;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Filtering *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    /// <summary>
    /// Filtering Method, filtering data variables prefix means: u => union('or' condition), i => intersection('and' condition), e => exception 
    /// </summary>
    public void Filtering(FilteringData filteringData, bool isDisplayBlackOverlay, Action completeAction = null)
    {
        this.isDisplayBlackOverlay = isDisplayBlackOverlay;
        base.Filtering(filteringData, completeAction);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *           * Inventory Item UI Refresh *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void RefreshInventoryItem(string itemInstanceId = null)
    {
        var inventoryItemArr = transform.GetComponentsInChildren<InventoryItem>();

        if (itemInstanceId == null)
            for (int i = 0; i < inventoryItemArr.Length; i++)
                inventoryItemArr[i].RefreshItemInstance();
        else
            inventoryItemArr.Where(item => item.ItemData.ItemInstance.ItemInstanceId == itemInstanceId).FirstOrDefault().RefreshItemInstance();
    }

    public void RefreshInventoryItem(List<string> itemInstanceIdList)
    {
        var inventoryItemArr = transform.GetComponentsInChildren<InventoryItem>();

        if (itemInstanceIdList == null)
            for (int i = 0; i < inventoryItemArr.Length; i++)
                inventoryItemArr[i].RefreshItemInstance();
        else
        {
            var count = 0;

            for (int i = 0; i < inventoryItemArr.Length; i++)
                if (itemInstanceIdList.Contains(inventoryItemArr[i].ItemData.ItemInstance.ItemInstanceId))
                {
                    inventoryItemArr[i].RefreshItemInstance();

                    if (count >= itemInstanceIdList.Count)
                        break;
                }
        }
    }
}

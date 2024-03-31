using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PolyAndCode.UI;
using PlayFab;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;

public class RecyclableItemViewer<RecyclableItemClass, ItemDataClass> : SerializedMonoBehaviour, IRecyclableScrollRectDataSource where RecyclableItemClass : RecyclableItem<ItemDataClass> where ItemDataClass : RecyclableItemData
{
    [TitleGroup("Recyclable Item Viewer", order: 0), BoxGroup("Recyclable Item Viewer/RIV", ShowLabel = false)]
    [BoxGroup("Recyclable Item Viewer/RIV/Is Reload Completed"), ShowInInspector] public bool IsReloadCompleted { get => recyclableScrollRect.IsReloadCompleted; }
    [BoxGroup("Recyclable Item Viewer/RIV/Item Data Dictionary"), ShowInInspector] public Dictionary<string, ItemDataClass> ItemDataDictionary { get; protected set; } = new Dictionary<string, ItemDataClass>();
    [BoxGroup("Recyclable Item Viewer/RIV/Last Select Item Data"), ShowInInspector] public ItemDataClass LastSelectItemData { get; private set; }

    [TitleGroup("Filter", order: 1), BoxGroup("Filter/F", ShowLabel = false)]
    [BoxGroup("Filter/F/Is Filtered"), ShowInInspector] public bool IsFilted { get; protected set; }
    [BoxGroup("Filter/F/Filter"), OnValueChanged(nameof(OnFilterItemClassEnumChanged)), SerializeField] PlayFabManager.CatalogItemClass filterItemClassEnum;
    [BoxGroup("Filter/F/Filter"), SerializeField, ReadOnly] string filterItemClassName;
    public string FilterItemClassName { get => filterItemClassName; }
    [BoxGroup("Filter/F/Filter"), OnValueChanged(nameof(OnFilterItemTagEnumChanged)), SerializeField] PlayFabManager.CatalogItemTag filterItemTagEnum;
    [BoxGroup("Filter/F/Filter"), SerializeField, ReadOnly] string filterItemTagName;
    public string FilterItemTagName { get => filterItemTagName; }

    [TitleGroup("PlayFab API Request Info", order: 2), BoxGroup("PlayFab API Request Info/PARI", showLabel: false)]
    [BoxGroup("PlayFab API Request Info/PARI/Completed Request Count"), SerializeField] protected int ConsecutiveApiRequestCount = 3;
    [BoxGroup("PlayFab API Request Info/PARI/Completed Request Count"), SerializeField] protected int CurCompletedApiRequestCount, PreCompletedApiRequestCount;
    [BoxGroup("PlayFab API Request Info/PARI/Item List Of PlayFab API Request Result"), ShowInInspector] public List<string> ApiRequestSuccessItemKeyList { get; private set; } = new List<string>();
    [BoxGroup("PlayFab API Request Info/PARI/Item List Of PlayFab API Request Result"), ShowInInspector] public List<string> ApiRequestFailedItemKeyList { get; private set; } = new List<string>();

    [TitleGroup("Recyclable Scroll Rect", order: 3), BoxGroup("Recyclable Scroll Rect/RSR", ShowLabel = false)]
    [BoxGroup("Recyclable Scroll Rect/RSR/Recyclable Scroll Rect"), SerializeField] protected RecyclableScrollRect recyclableScrollRect;
    public RecyclableScrollRect RecyclableScrollRect { get => recyclableScrollRect; }
    [BoxGroup("Recyclable Scroll Rect/RSR/Recyclable Scroll Rect"), OnValueChanged(nameof(OnCellNameEnumChanged)), SerializeField] NameManager.UIPrefabName cellNameEnum;
    [BoxGroup("Recyclable Scroll Rect/RSR/Recyclable Scroll Rect"), SerializeField, ReadOnly] string cellName;
    [BoxGroup("Recyclable Scroll Rect/RSR/Scroll Rect Pre Nomalize Position"), ShowInInspector] protected Vector2 PreScrollPosition { get; set; }

    [TitleGroup("Recyclable Item List", order: 2), BoxGroup("Recyclable Item List/RIL", ShowLabel = false)]
    [BoxGroup("Recyclable Item List/RIL/Recyclable Item List"), ShowInInspector] public List<RecyclableItemClass> RecyclableItemList { get; set; } = new List<RecyclableItemClass>();

    // ? This action conneted item action. If want to add any method on this, override like 'OnItemSelet_Viewer' method.
    [HideInInspector] public Action<ItemDataClass> OnItemSelectViewerAction;
    [HideInInspector] public Action<ItemDataClass> OnItemClickViewerAction;
    [HideInInspector] public Action<ItemDataClass> OnItemLongSelectViewerAction;

    protected Action<KeyValuePair<string, ItemDataClass>> ConsecutiveApiRequestAction;
    protected DateTime consecutiveApiRequestStartDateTimeUtcNow;

    Coroutine addContentOnTailCo;

    protected WaitUntil WaitUntil5CompletedRequestAdded;

    [HideInInspector] public WaitUntil WaitUntilReloadCompleted;

    protected virtual void Awake()
    {
        recyclableScrollRect.DataSource = this;

        WaitUntil5CompletedRequestAdded = new WaitUntil(() => CurCompletedApiRequestCount > PreCompletedApiRequestCount && CurCompletedApiRequestCount % ConsecutiveApiRequestCount == 0);
        WaitUntilReloadCompleted = new WaitUntil(() => IsReloadCompleted);

        if (FilterItemClassName == string.Empty)
            filterItemClassName = PlayFabManager.CatalogItemClass.None.ToCachedString();
    }

    protected virtual void Start()
    {
        OnItemSelectViewerAction += OnItemSelect_Viewer;
        OnItemClickViewerAction += OnItemClick_Viewer;
        OnItemLongSelectViewerAction += OnItemLongSelect_Viewer;

        ConsecutiveApiRequestAction += ConsecutiveApiRequestMethod;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *             * Recyclable Scroll Rect *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public int GetItemCount()
    {
        return ItemDataDictionary.Count;
    }

    /// <summary>
    /// Set cell prefab position and data(ConfigureCell(Update UI)) when viewer dragging or create item.
    /// </summary>
    /// <param name="cell"> Cell prefab. </param>
    /// <param name="index"> Cell index. </param>
    public virtual void SetCell(ICell cell, int index)
    {
        var item = cell as RecyclableItemClass;

        RecyclableItemList.Add(item);
        item.ConfigureCell(ItemDataDictionary.ElementAt(index), index);
        item.OnItemSelectAction += OnItemSelectViewerAction;
        item.OnItemClickAction += OnItemClickViewerAction;
        item.OnItemLongSelectAction += OnItemLongSelectViewerAction;
        item.ReloadViewerWithDataRemoveAction += ReloadViewerWithDataRemove;
        item.RefreshViewerWithDataRemoveAction += RefreshViewerWithDataRemove;
    }

    public virtual void SetCellWithOutConfigure(ICell cell, int index)
    {
        var item = cell as RecyclableItemClass;

        RecyclableItemList.Add(item);
        item.OnItemSelectAction += OnItemSelectViewerAction;
        item.OnItemClickAction += OnItemClickViewerAction;
        item.OnItemLongSelectAction += OnItemLongSelectViewerAction;
        item.ReloadViewerWithDataRemoveAction += ReloadViewerWithDataRemove;
        item.RefreshViewerWithDataRemoveAction += RefreshViewerWithDataRemove;
    }

    /// <summary>
    /// This method set scroll position to pre value.  But it call after reload viewer only in following conditions. The viewer has additional content on tail and viewer content 'sizedelta' value expected will be change on reload. If not satisfied conditions call refresh viewer method.
    /// </summary>
    public void SetPreScrollPosition()
    {
        // First, set zoro position for value changed event execute and then set pre scroll position.
        recyclableScrollRect.normalizedPosition = Vector2.zero;
        recyclableScrollRect.normalizedPosition = PreScrollPosition;
    }

    [BoxGroup("Recyclable Item Viewer/RIV/Viewer Controller"), Button("Clear Viewer", ButtonSizes.Large), GUIColor(1, 0.7f, 0.7f)]
    public virtual void ClearViewer(Action completeAction = null)
    {
        IsFilted = false;
        ResetItemDataDictionary(null);
        ItemDataDictionary.Clear();

        recyclableScrollRect.ReloadData(completeAction);
    }

    /// <summary>
    /// Reload viewer with data.
    /// </summary>
    /// <param name="itemDataDictionary"> Dictionary<string, ItemDataClass> must non null.</param>
    public void ForceReloadViewerWithData(object nonNullItemDataDictionary, Action completeAction = null)
    {
        ResetViewerInfoData();

        ItemDataDictionary = nonNullItemDataDictionary as Dictionary<string, ItemDataClass>;

        recyclableScrollRect.ReloadData(completeAction);
    }

    /// <summary>
    /// Refresh viewer with data.
    /// </summary>
    /// <param name="itemDataDictionary"> Dictionary<string, ItemDataClass> must non null.</param>
    public void ForceRefreshViewerWithData(object nonNullItemDataDictionary)
    {
        PreScrollPosition = recyclableScrollRect.normalizedPosition;

        ForceReloadViewerWithData(nonNullItemDataDictionary);

        SetPreScrollPosition();
    }

    /// <summary>
    /// Reload viewer with data.
    /// </summary>
    /// <param name="itemDataDictionary"> Dictionary<string, ItemDataClass> or null. If is it null reload viewer with local data. </param>
    public virtual void ReloadViewerWithData(object itemDataDictionary, Action completeAction = null)
    {
        ResetItemDataDictionary(itemDataDictionary);

        recyclableScrollRect.ReloadData(completeAction);
    }

    /// <summary>
    /// Refresh viewer with data.
    /// </summary>
    /// <param name="itemDataDictionary"> Dictionary<string, ItemDataClass> or null. If is it null reload viewer with local data. </param>
    public virtual void RefreshViewerWithData(object itemDataDictionary, Action completeAction = null)
    {
        PreScrollPosition = recyclableScrollRect.normalizedPosition;

        ReloadViewerWithData(itemDataDictionary, completeAction);

        SetPreScrollPosition();
    }

    public virtual void ReloadViewerWithDataRemove(string key, Action completeAction = null)
    {
        if (!key.IsNullOrEmpty() && ItemDataDictionary.ContainsKey(key))
            ItemDataDictionary.Remove(key);
        else if (key.IsNullOrEmpty())
        {
            ClearViewer();
            return;
        }

        ReloadViewerWithData(ItemDataDictionary, completeAction);
    }

    public virtual void RefreshViewerWithDataRemove(string key, Action completeAction = null)
    {
        if (!key.IsNullOrEmpty() && ItemDataDictionary.ContainsKey(key))
            ItemDataDictionary.Remove(key);
        else if (key.IsNullOrEmpty())
        {
            ClearViewer();
            return;
        }

        RefreshViewerWithData(ItemDataDictionary, completeAction);
    }

    public virtual void ReloadViewerWithDataRemove(List<string> keyList, Action completeAction = null)
    {
        if (keyList != null && keyList.Count > 0)
            foreach (var key in keyList)
                ItemDataDictionary.Remove(key);
        else
        {
            // ClearViewer();
            return;
        }

        ReloadViewerWithData(ItemDataDictionary, completeAction);
    }

    public virtual void RefreshViewerWithDataRemove(List<string> keyList)
    {
        if (keyList != null && keyList.Count > 0)
            foreach (var key in keyList)
                ItemDataDictionary.Remove(key);
        else
        {
            ClearViewer();
            return;
        }

        RefreshViewerWithData(ItemDataDictionary);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Reset Item Data *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    protected void ResetViewerInfoData()
    {
        RecyclableItemList.Clear();
        CurCompletedApiRequestCount = 0;
        PreCompletedApiRequestCount = 0;
        ApiRequestSuccessItemKeyList.Clear();
        ApiRequestFailedItemKeyList.Clear();
    }

    /// <summary>
    /// If is get data from inventory or catalog dictionary of PlayFabManager, input any non null data like 'true'.
    /// </summary>
    protected virtual void ResetItemDataDictionary(object itemDataDictionary)
    {
        ResetViewerInfoData();
        // gameObject.Log($"RecyclableItemList Cleared: ResetItemDataDictionary({itemDataDictionary})");

        // TODO: itemDataDictionary 가 이미 외부에서 필터링 되어 들어온 경우가 있을 수 있는데 이를 IsFiltering = true 로 해야하는가? 현재는 그건 필터링된 것으로 생각하지 않음
        if (itemDataDictionary == null || ItemDataDictionary == itemDataDictionary)
            return;

        IsFilted = false;
        ItemDataDictionary.Clear();

        // ? 아이템 클래스로 필터링하지 않으면 전달받은 값을 그대로 복사, 아이템 클래스로 필터링한다면, 카탈로그 및 인벤토리를 검색해서 해당 키값의 아이템 클래스를 찾아 필터링 후 복사
        foreach (var kvp in itemDataDictionary as Dictionary<string, ItemDataClass>)
        {
            if (FilterItemClassName == PlayFabManager.CatalogItemClass.None.ToCachedString() || FilterItemClassName.IsNullOrEmpty())
                ItemDataDictionary.Add(kvp.Key, kvp.Value);
            else
            {
                // Key is CatalogItemId.
                if (PlayFabManager.Instance.MainCatalogItemDictionary.ContainsKey(kvp.Key))
                {
                    if (PlayFabManager.Instance.MainCatalogItemDictionary[kvp.Key].ItemClass == FilterItemClassName)
                        if (FilterItemTagName.IsNullOrEmpty() || FilterItemTagName == PlayFabManager.CatalogItemTag.None.ToCachedString())
                            ItemDataDictionary.Add(kvp.Key, kvp.Value);
                        else if (PlayFabManager.Instance.MainCatalogItemDictionary[kvp.Key].GetMainTag().ToCachedString() == FilterItemTagName)
                            ItemDataDictionary.Add(kvp.Key, kvp.Value);
                }
                else // Key is ItemInstanceId.
                    foreach (var dictionaryKvp in PlayFabManager.Instance.InventoryItemDictionary)
                        if (dictionaryKvp.Value.ContainsKey(kvp.Key))
                            if (dictionaryKvp.Value[kvp.Key].ItemClass == FilterItemClassName)
                            {
                                if (FilterItemTagName.IsNullOrEmpty() || FilterItemTagName == PlayFabManager.CatalogItemTag.None.ToCachedString())
                                    ItemDataDictionary.Add(kvp.Key, kvp.Value);
                                else if (PlayFabManager.Instance.MainCatalogItemDictionary[dictionaryKvp.Value[kvp.Key].ItemId].GetMainTag().ToCachedString() == FilterItemTagName)
                                    ItemDataDictionary.Add(kvp.Key, kvp.Value);
                                break;
                            }
            }
        }
    }

    protected void ResetItemDataDictionaryFromPlayfabUserInventory(object itemDataDictionary)
    {
        ResetViewerInfoData();
        // gameObject.Log($"RecyclableItemList Cleared: ResetItemDataDictionaryFromPlayfabUserInventory({itemDataDictionary})");

        if (ItemDataDictionary == itemDataDictionary)
            return;

        IsFilted = false;
        ItemDataDictionary.Clear();

        var filterClass = FilterItemClassName.ToCachedEnum<PlayFabManager.CatalogItemClass>();
        var filterTag = filterItemTagName.ToCachedEnum<PlayFabManager.CatalogItemTag>();

        if (PlayFabManager.Instance.InventoryItemDictionary.ContainsKey(filterClass))
        {
            foreach (var kvp in PlayFabManager.Instance.InventoryItemDictionary[filterClass])
                if (filterTag == PlayFabManager.CatalogItemTag.None || kvp.Value.GetMainTag() == filterTag)
                    ItemDataDictionary.Add(kvp.Key, kvp.Value.GetAssembledRecyclableItemData<ItemDataClass>());
        }
        else if (filterClass == PlayFabManager.CatalogItemClass.None)
            foreach (var dictionaryKvp in PlayFabManager.Instance.InventoryItemDictionary)
            {
                // Unit item impossible displaying on normal inventory viewer.
                if (dictionaryKvp.Key == PlayFabManager.CatalogItemClass.Unit)
                    if (cellName != NameManager.UIPrefabName.LargeInventoryItem.ToCachedUncamelCaseString())
                        continue;

                // Mail item has MailViewer.
                if (dictionaryKvp.Key == PlayFabManager.CatalogItemClass.Mail)
                    continue;

                // Quest item has QuestViewer.
                if (dictionaryKvp.Key == PlayFabManager.CatalogItemClass.Quest)
                    continue;

                foreach (var kvp in dictionaryKvp.Value)
                    if (filterTag == PlayFabManager.CatalogItemTag.None || kvp.Value.GetMainTag() == filterTag)
                        ItemDataDictionary.Add(kvp.Key, kvp.Value.GetAssembledRecyclableItemData<ItemDataClass>());
            }
    }

    protected void ResetItemDataDictionaryFromPlayfabCatalog(object itemDataDictionary, bool isShowUnit = false)
    {
        ResetViewerInfoData();

        if (ItemDataDictionary == itemDataDictionary)
            return;

        IsFilted = false;
        ItemDataDictionary.Clear();

        var filterClass = FilterItemClassName.ToCachedEnum<PlayFabManager.CatalogItemClass>();
        var filterTag = filterItemTagName.ToCachedEnum<PlayFabManager.CatalogItemTag>();

        foreach (var kvp in PlayFabManager.Instance.MainCatalogItemDictionary)
            if (filterClass == PlayFabManager.CatalogItemClass.None || kvp.Value.ItemClass.ToCachedEnum<PlayFabManager.CatalogItemClass>() == filterClass)
                if (filterTag == PlayFabManager.CatalogItemTag.None || kvp.Value.GetMainTag() == filterTag)
                {
                    // Character item impossible displaying on normal inventory viewer.
                    if (!isShowUnit)
                        if (kvp.Value.ItemClass.ToCachedEnum<PlayFabManager.CatalogItemClass>() == PlayFabManager.CatalogItemClass.Unit)
                            continue;

                    ItemDataDictionary.Add(kvp.Key, kvp.Value.GetAssembledRecyclableItemData<ItemDataClass>());
                }
    }

    public void SetContentOffset(Vector2 offset, float spacing = 0)
    {
        recyclableScrollRect.ContentOffset = offset + spacing * Vector2.one;
        recyclableScrollRect._recyclingSystem.ContentOffset = offset + spacing * Vector2.one;
    }

    public void SetContentOffset(RectTransform headModuleRect, float spacing = 0)
    {
        recyclableScrollRect.ContentOffset = headModuleRect.sizeDelta + spacing * Vector2.one;
        recyclableScrollRect._recyclingSystem.ContentOffset = headModuleRect.sizeDelta + spacing * Vector2.one;
    }

    public void SetAdditionalContentOnHead(RectTransform additionalHeadModuleRect, float spacing = 0)
    {
        SetContentOffset(additionalHeadModuleRect, spacing);

        additionalHeadModuleRect.SetParent(recyclableScrollRect.content);
        // additionalHeadModuleRect.pivot = Vector2.zero;
        additionalHeadModuleRect.anchoredPosition = Vector2.zero;
    }

    public void SetAdditionalContentOnTail(RectTransform additionalTailModuleRect, float spacing = 0)
    {
        additionalTailModuleRect.SetParent(recyclableScrollRect.content);
        // additionalTailModuleRect.pivot = Vector2.zero;

        if (addContentOnTailCo != null)
            StopCoroutine(addContentOnTailCo);
        addContentOnTailCo = StartCoroutine(SetAdditionalContentOnTailCo(additionalTailModuleRect, spacing));
    }

    IEnumerator SetAdditionalContentOnTailCo(RectTransform additionalTailModuleRect, float spacing = 0)
    {
        yield return WaitUntilReloadCompleted;

        if (recyclableScrollRect.Direction == RecyclableScrollRect.DirectionType.Horizontal)
        {
            additionalTailModuleRect.anchoredPosition = new Vector2(recyclableScrollRect.content.sizeDelta.x + spacing, 0);
            recyclableScrollRect.content.sizeDelta += (additionalTailModuleRect.sizeDelta.x + spacing) * Vector2.right;
        }
        else
        {
            additionalTailModuleRect.anchoredPosition = new Vector2(0, -(recyclableScrollRect.content.sizeDelta.y + spacing));
            recyclableScrollRect.content.sizeDelta += (additionalTailModuleRect.sizeDelta.y + spacing) * Vector2.up;
        }

        addContentOnTailCo = null;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                    * Filtering *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    /// <summary>
    /// Filtering Method, filtering data variables prefix means: u => union('or' condition), i => intersection('and' condition), e => exception 
    /// </summary>
    public Dictionary<string, ItemDataClass> GetFilteredItemDataDictionary(FilteringData filteringData)
    {
        if (IsFilted || ItemDataDictionary.Count == 0)
            ResetItemDataDictionary(null);

        IsFilted = true;

        var removeKvpList = new List<KeyValuePair<string, ItemDataClass>>();

        // 필터링 옵션과 별개로 제거할 데이터.
        if (!filteringData.ExtraRemoveKey.IsNullOrEmpty())
        {
            ItemDataDictionary.Remove(filteringData.ExtraRemoveKey);
        }

        if (filteringData.ExtraRemoveKeyList != null && filteringData.ExtraRemoveKeyList.Count > 0)
        {
            foreach (var key in filteringData.ExtraRemoveKeyList)
                ItemDataDictionary.Remove(key);
        }

        // 카탈로그 아이디로 필터링. => 해당 키 값만 남김.
        if (!filteringData.FilteringCatalogId.IsNullOrEmpty())
        {
            removeKvpList.Clear();

            foreach (var kvp in ItemDataDictionary)
                if (kvp.Key == filteringData.FilteringCatalogId)
                {
                    removeKvpList.Clear();
                    removeKvpList.AddRange(ItemDataDictionary);
                    removeKvpList.Remove(kvp);

                    break;
                }
                else if (kvp.Value.CatalogItem != null && !kvp.Value.CatalogItem.ItemId.IsNullOrEmpty())
                {
                    if (kvp.Value.CatalogItem.ItemId != filteringData.FilteringCatalogId)
                        removeKvpList.Add(kvp);
                }

            foreach (var kvp in removeKvpList)
                ItemDataDictionary.Remove(kvp.Key);
        }

        if (filteringData.FilteringCatalogIdList != null && filteringData.FilteringCatalogIdList.Count > 0)
        {
            removeKvpList.Clear();

            foreach (var kvp in ItemDataDictionary)
                if (kvp.Value.CatalogItem != null && !kvp.Value.CatalogItem.ItemId.IsNullOrEmpty())
                    if (!filteringData.FilteringCatalogIdList.Contains(kvp.Value.CatalogItem.ItemId))
                        removeKvpList.Add(kvp);

            foreach (var kvp in removeKvpList)
                ItemDataDictionary.Remove(kvp.Key);
        }

        // 아이템 클래스 필터링
        if (filteringData.unionItemClassList != null || filteringData.exceptionItemClassList != null)
        {
            removeKvpList.Clear();

            foreach (var ivItemDataKvp in ItemDataDictionary)
            {
                if (filteringData.unionItemClassList != null)
                    if (!filteringData.unionItemClassList.Contains(ivItemDataKvp.Value.CatalogItemClass))
                        removeKvpList.Add(ivItemDataKvp);

                if (filteringData.exceptionItemClassList != null)
                    if (filteringData.exceptionItemClassList.Contains(ivItemDataKvp.Value.CatalogItemClass))
                        removeKvpList.Add(ivItemDataKvp);
            }

            foreach (var kvp in removeKvpList)
                ItemDataDictionary.Remove(kvp.Key);
        }

        // 아이템 태그 필터링
        if (filteringData.unionItemTagList != null || filteringData.intersectiomItemTagList != null || filteringData.exceptionItemTagList != null)
        {
            removeKvpList.Clear();

            List<string> itemTagList;
            int intersectionCount;

            foreach (var ivItemDataKvp in ItemDataDictionary)
            {
                itemTagList = ivItemDataKvp.Value.CatalogItem.Tags;

                if (filteringData.unionItemTagList != null)
                {
                    removeKvpList.Add(ivItemDataKvp);

                    foreach (var conditionTag in filteringData.unionItemTagList)
                        if (itemTagList.Contains(conditionTag.ToCachedString()) || IsMeanClassTag(ivItemDataKvp.Value.CatalogItemClass, conditionTag))
                        {
                            removeKvpList.Remove(ivItemDataKvp);
                            break;
                        }
                }

                if (filteringData.intersectiomItemTagList != null)
                {
                    removeKvpList.Add(ivItemDataKvp);
                    intersectionCount = 0;

                    foreach (var conditionTag in filteringData.intersectiomItemTagList)
                        if (itemTagList.Contains(conditionTag.ToCachedString()) || IsMeanClassTag(ivItemDataKvp.Value.CatalogItemClass, conditionTag))
                        {
                            intersectionCount++;

                            if (intersectionCount == filteringData.intersectiomItemTagList.Count)
                                removeKvpList.Remove(ivItemDataKvp);
                        }
                }

                if (filteringData.exceptionItemTagList != null)
                    foreach (var conditionTag in filteringData.exceptionItemTagList)
                        if (itemTagList.Contains(conditionTag.ToCachedString()) || IsMeanClassTag(ivItemDataKvp.Value.CatalogItemClass, conditionTag))
                        {
                            removeKvpList.Add(ivItemDataKvp);
                            break;
                        }
            }

            foreach (var kvp in removeKvpList)
                ItemDataDictionary.Remove(kvp.Key);
        }

        // 커스텀 데이터 프로퍼티 필터링
        if (filteringData.unionJPropertyList != null || filteringData.intersectionJPropertyList != null || filteringData.exceptionJPropertyList != null)
        {
            removeKvpList.Clear();

            IEnumerable<JProperty> itemDataProperties;
            int intersectionCount;

            foreach (var ivItemDataKvp in ItemDataDictionary)
            {
                if (ivItemDataKvp.Value.ExtraItemData == null)
                    itemDataProperties = ivItemDataKvp.Value.ToJObject().Properties();
                else
                    itemDataProperties = ivItemDataKvp.Value.ExtraItemData.ToJObject().Properties();

                intersectionCount = 0;

                if (filteringData.unionJPropertyList != null)
                {
                    removeKvpList.Add(ivItemDataKvp);

                    foreach (var conditionJp in filteringData.unionJPropertyList)
                        if (itemDataProperties.ContainsJp(conditionJp))
                        {
                            removeKvpList.Remove(ivItemDataKvp);
                            break;
                        }
                }

                if (filteringData.intersectionJPropertyList != null)
                {
                    removeKvpList.Add(ivItemDataKvp);

                    foreach (var conditionJp in filteringData.intersectionJPropertyList)
                        if (itemDataProperties.ContainsJp(conditionJp))
                        {
                            intersectionCount++;

                            if (intersectionCount == filteringData.intersectionJPropertyList.Count)
                                removeKvpList.Remove(ivItemDataKvp);
                        }
                }

                if (filteringData.exceptionJPropertyList != null)
                    foreach (var conditionJp in filteringData.exceptionJPropertyList)
                        if (itemDataProperties.ContainsJp(conditionJp))
                        {
                            removeKvpList.Add(ivItemDataKvp);
                            break;
                        }
            }

            foreach (var kvp in removeKvpList)
                ItemDataDictionary.Remove(kvp.Key);
        }

        return ItemDataDictionary;
    }

    /// <summary>
    /// Filtering Method, filtering data variables prefix means: u => union('or' condition), i => intersection('and' condition), e => exception 
    /// </summary>
    public virtual void Filtering(FilteringData filteringData, Action completeAction = null)
    {
        ReloadViewerWithData(GetFilteredItemDataDictionary(filteringData), completeAction);
    }

    protected bool IsMeanClassTag(PlayFabManager.CatalogItemClass itemClass, PlayFabManager.CatalogItemTag conditionTag)
    {
        if (conditionTag == PlayFabManager.CatalogItemTag.Weapons && itemClass == PlayFabManager.CatalogItemClass.Weapon)
            return true;
        else if (conditionTag == PlayFabManager.CatalogItemTag.Armors && itemClass == PlayFabManager.CatalogItemClass.Armor)
            return true;
        else
            return false;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Manage Item *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void UpdateAllItemUI()
    {
        RecyclableItemList.ForEach(item => item.UpdateUI());
    }

    /// <summary>
    /// This Method Must Execute With Follow Conditions.
    /// 1. The index of each item must be equal to or less than ItemDataDictionary.Count.
    /// </summary>
    public void UpdateAllItemUIWithReallocationItemData()
    {
        RecyclableItemList.ForEach(item =>
            {
                var newItemData = ItemDataDictionary.ElementAt(item.ItemData.Index);
                item.ItemData.Key = newItemData.Key;
                item.ItemData = newItemData.Value;
                item.UpdateUI();
            }
        );
    }

    public RecyclableItemClass FindItemByItemDataKey(string key)
    {
        return RecyclableItemList.Where(item => item.ItemData.Key == key).FirstOrDefault();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *          * On Item Select Viewer Event *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public virtual void OnItemSelect_Viewer(ItemDataClass itemData)
    {
        LastSelectItemData = itemData;
    }

    public virtual void OnItemClick_Viewer(ItemDataClass itemData)
    {
        LastSelectItemData = itemData;
    }

    public virtual void OnItemLongSelect_Viewer(ItemDataClass itemData)
    {
        LastSelectItemData = itemData;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *            * Consecutive API Request *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    protected void ConsecutiveApiRequest()
    {
        consecutiveApiRequestStartDateTimeUtcNow = DateTime.UtcNow;

        StartCoroutine(ConsecutiveApiRequestCo());
    }

    protected virtual IEnumerator ConsecutiveApiRequestCo()
    {
        CurCompletedApiRequestCount = 0;
        PreCompletedApiRequestCount = 0;

        int requestCount = 0;
        foreach (var kvp in ItemDataDictionary)
        {
            requestCount++;
            gameObject.Log($"{kvp.Key}, 연속 API 요청({requestCount})");
            ConsecutiveApiRequestAction?.Invoke(kvp);

            if (requestCount != ItemDataDictionary.Count && requestCount % ConsecutiveApiRequestCount == 0)
            {
                gameObject.Log($"기존 API 요청 완료까지 대기.");
                yield return WaitUntil5CompletedRequestAdded;
                PreCompletedApiRequestCount = CurCompletedApiRequestCount;
            }
        }
    }

    protected virtual void ConsecutiveApiRequestMethod(KeyValuePair<string, ItemDataClass> kvp) { }
    protected virtual void ConsecutiveApiRequestCallbackAction<T, D>(T result, PlayFabError error, D delgateData) { }

    protected double GetConsecutiveApiRequestCompletedSeconds(DateTime completedDateTimeUtcNow)
    {
        return (completedDateTimeUtcNow - consecutiveApiRequestStartDateTimeUtcNow).TotalSeconds;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                       * UI *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Recyclable Scroll Rect/RSR"), Button("Find Scroll Rect", ButtonSizes.Gigantic), GUIColor(0.35f, 0.7f, 1)]
    public void FindRecyclableScrollRect()
    {
        recyclableScrollRect = GetComponentInChildren<RecyclableScrollRect>();

        recyclableScrollRect.CellName = cellName;
    }

    void OnFilterItemClassEnumChanged()
    {
        filterItemClassName = filterItemClassEnum.ToCachedString();
    }

    void OnFilterItemTagEnumChanged()
    {
        filterItemTagName = filterItemTagEnum.ToCachedString();
    }

    void OnCellNameEnumChanged()
    {
        if (recyclableScrollRect == null)
            FindRecyclableScrollRect();

        cellName = cellNameEnum.ToCachedUncamelCaseString();
        recyclableScrollRect.CellName = cellName;
    }
}

/**
 * !--------------------------------------------------
 * !--------------------------------------------------
 *            * Ordering / Filtering Data *
 * ?--------------------------------------------------
 * ?--------------------------------------------------
 */

public class OrderingData
{
    public Inventory.OrderBy orderBy;
    public Inventory.OrderAbout orderAbout;
}

public class FilteringData
{
    // 카탈로그 아이디로 필터링(해당 아이디만 남김), 사실상 별개의 필터링 옵션
    public string FilteringCatalogId;
    public List<string> FilteringCatalogIdList;

    public string ExtraRemoveKey;
    public List<string> ExtraRemoveKeyList;

    public List<PlayFabManager.CatalogItemClass> unionItemClassList;
    public List<PlayFabManager.CatalogItemClass> exceptionItemClassList;

    public List<PlayFabManager.CatalogItemTag> unionItemTagList;
    public List<PlayFabManager.CatalogItemTag> intersectiomItemTagList;
    public List<PlayFabManager.CatalogItemTag> exceptionItemTagList;

    public List<JProperty> unionJPropertyList;
    public List<JProperty> intersectionJPropertyList;
    public List<JProperty> exceptionJPropertyList;
}
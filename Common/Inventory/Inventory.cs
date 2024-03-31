using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Michsky.MUIP;
using Sirenix.OdinInspector;
using PlayFab;

public class Inventory : MonoSingleton<Inventory>
{
    [TitleGroup("Inventory"), BoxGroup("Inventory/I", showLabel: false)]
    [BoxGroup("Inventory/I/Current Inventory Window"), SerializeField, ReadOnly] string curWindowName;
    [BoxGroup("Inventory/I/Inventory Item Viewer"), SerializeField] List<InventoryItemViewer> inventoryViewerList;
    public List<InventoryItemViewer> InventoryViewerList { get => inventoryViewerList; }
    [BoxGroup("Inventory/I/Inventory Item Viewer"), ShowInInspector]
    public InventoryItemViewer CurOpendInventoryViewer
    {
        get { try { return InventoryViewerList.Where(viewer => viewer.FilterItemClassName == curWindowName).FirstOrDefault(); } catch { return null; } }
    }
    [BoxGroup("Inventory/I/Fade Duration"), SerializeField] float fadeDuration = 0.35f;

    [TitleGroup("UI"), BoxGroup("UI/U", showLabel: false)]
    [BoxGroup("UI/U/Inventory Item Viewer"), SerializeField] WindowManager windowManager;
    [BoxGroup("UI/U/Canvas Group"), SerializeField] CanvasGroup canvasGroup;

    public enum InventoryViewerName { None, Unit, Equipment, SmallSize, LargeSize }
    public enum OrderBy { Ascending, Descending }
    public enum OrderAbout { AwakeningLv, Lv, PrmGrade, Grade, RemainingUses, Equipped }

    // ETC 전용 필터링 데이터
    readonly FilteringData etcFilteringData = new FilteringData
    {
        exceptionItemClassList = new List<PlayFabManager.CatalogItemClass> {
        PlayFabManager.CatalogItemClass.Unit,
        PlayFabManager.CatalogItemClass.Equipment, PlayFabManager.CatalogItemClass.Weapon, PlayFabManager.CatalogItemClass.Armor,
        PlayFabManager.CatalogItemClass.Enchantment, PlayFabManager.CatalogItemClass.Skill,
        PlayFabManager.CatalogItemClass.Package,PlayFabManager.CatalogItemClass.Mail, PlayFabManager.CatalogItemClass.Quest
        }
    };

    private void Awake()
    {
        base.MakeDestroyableInstance();

        // FindInventoryItemViewer();
    }

    private void Start()
    {
        GameManager.Instance.RequestPlayFabDataOnSceneLoaded(GameManager.PlayFabDataType.Catalogs);
        GameManager.Instance.RequestPlayFabDataOnSceneLoaded(GameManager.PlayFabDataType.UserInventory);

        curWindowName = windowManager.windows[0].windowName;

        LoadInventory();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Test *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [TitleGroup("Test"), BoxGroup("Test/T")]
    [BoxGroup("Test/T/Test"), Button("Filter Test 1", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    public void FilterTest1()
    {
        var filteringData = new FilteringData()
        {
            unionItemClassList = new List<PlayFabManager.CatalogItemClass> { PlayFabManager.CatalogItemClass.Weapon },
            unionItemTagList = new List<PlayFabManager.CatalogItemTag> { PlayFabManager.CatalogItemTag.Weapons }
        };

        OpenInventoryViewerWithFilting(PlayFabManager.CatalogItemClass.None, filteringData);
    }

    [BoxGroup("Test/T/Test"), Button("Filter Test 2", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    public void FilterTest2()
    {
        var filteringData = new FilteringData()
        {
            unionItemClassList = new List<PlayFabManager.CatalogItemClass> { PlayFabManager.CatalogItemClass.Weapon },
            unionItemTagList = new List<PlayFabManager.CatalogItemTag> { PlayFabManager.CatalogItemTag.Weapons },
            exceptionItemTagList = new List<PlayFabManager.CatalogItemTag> { PlayFabManager.CatalogItemTag.Rifle }
        };

        OpenInventoryViewerWithFilting(PlayFabManager.CatalogItemClass.None, filteringData);
    }

    [BoxGroup("Test/T/Test"), Button("Filter Test 3", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    public void FilterTest3()
    {
        var filteringData = new FilteringData()
        {
            unionItemClassList = new List<PlayFabManager.CatalogItemClass> { PlayFabManager.CatalogItemClass.Weapon },
            unionItemTagList = new List<PlayFabManager.CatalogItemTag> { PlayFabManager.CatalogItemTag.Weapons },
            exceptionItemTagList = new List<PlayFabManager.CatalogItemTag> { PlayFabManager.CatalogItemTag.Pistol }
        };

        OpenInventoryViewerWithFilting(PlayFabManager.CatalogItemClass.None, filteringData);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *             * Find Inventory Viewer *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Inventory/I/Inventory Item Viewer"), Button("Find Inventory Item Viewer", ButtonSizes.Gigantic), GUIColor(0.35f, 0.7f, 1)]
    void FindInventoryItemViewer()
    {
        inventoryViewerList = GetComponentsInChildren<InventoryItemViewer>().ToList();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *             * Open / Close Inventory *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Inventory/I/Inventory On Off"), Button("Inventory On", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    public void OpenInventory(InventoryViewerName inventoryViewerName = InventoryViewerName.None)
    {
        // canvasGroup.CanvasGroupControl(1, true, true);
        canvasGroup.CanvasGroupControl(1, true, true, fadeDuration);

        if (inventoryViewerName != InventoryViewerName.None)
            ChangeInventoryWindow(inventoryViewerName);
    }

    [BoxGroup("Inventory/I/Inventory On Off"), Button("Inventory Off", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    public void CloseInventory()
    {
        // canvasGroup.CanvasGroupControl(0);
        canvasGroup.CanvasGroupControl(0, false, false, fadeDuration * 0.5f);
    }

    // Lobby scene window changed default.
    public void OpenInventoryByLobbyWindow(NameManager.LobbySceneWindowName lobbyWindowName)
    {
        OpenInventory();

        switch (lobbyWindowName)
        {
            case NameManager.LobbySceneWindowName.Home:
                break;

            case NameManager.LobbySceneWindowName.Shop:
                break;

            case NameManager.LobbySceneWindowName.Mailbox:
                break;

            case NameManager.LobbySceneWindowName.Quest:
                break;

            case NameManager.LobbySceneWindowName.UnitSetting:
                break;

            case NameManager.LobbySceneWindowName.Forge:
                ChangeInventoryWindow(InventoryViewerName.Equipment);
                UpgradeManager.Instance.OnForgeItemRectClick(UpgradeManager.SelectedRectTransform.UpgradeTargetRect, 0, true);
                break;
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Load Inventory *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Inventory/I/Inventory Item Viewer"), Button("Load Inventory", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    void LoadInventory(bool isRefresh = false, int index = -1)
    {
        if (index == -1)
            for (int i = 0; i < InventoryViewerList.Count; i++)
            {
                InventoryViewerList[i].isDisplayBlackOverlay = true;
                InventoryViewerList[i].ClearViewer();
            }
        else
        {
            InventoryViewerList[index].isDisplayBlackOverlay = true;
            InventoryViewerList[index].ClearViewer();
        }

        if (isRefresh)
            PlayFabManager.Instance.GetUserInventory();

        gameObject.Log($"인벤토리 클리어 완료");

        StartCoroutine(LoadInventoryCo(index));
    }

    IEnumerator LoadInventoryCo(int index)
    {
        yield return GameManager.Instance.WaitUntilMainCatalogLoaded;
        yield return GameManager.Instance.WaitUntilUserInventoryLoaded;

        if (index == -1)
            foreach (var viewer in InventoryViewerList)
            {
                if (viewer.name == "ETC")
                    viewer.Filtering(etcFilteringData);
                else if (viewer.name != PlayFabManager.CatalogItemClass.None.ToCachedString() && viewer.name != "None - Large Size")
                    viewer.ReloadViewerWithData(null);
                // None viewer is used to filtering, so, None viewer doesn't load in this case.
            }
        else
        {
            if (InventoryViewerList[index].name == "ETC")
                InventoryViewerList[index].Filtering(etcFilteringData);
            else
                InventoryViewerList[index].ReloadViewerWithData(null);
        }

        gameObject.Log($"인벤토리 업데이트 완료");
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Change Window *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Inventory/I/Inventory Item Viewer"), Button("Change Inventory Window", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    public void ChangeInventoryWindow(string windowName)
    {
        if (curWindowName != windowName)
        {
            windowManager.OpenWindow(windowName);
            curWindowName = windowName;
        }
    }

    [BoxGroup("Inventory/I/Inventory Item Viewer"), Button("Change Inventory Window", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    public void ChangeInventoryWindow(InventoryViewerName inventoryName)
    {
        ChangeInventoryWindow(inventoryName.ToCachedUncamelCaseString());
    }

    // [BoxGroup("Inventory/I/Inventory Item Viewer"), Button("Change Inventory Window", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    // public void ChangeInventoryWindow(PlayFabManager.CatalogItemClass itemClass)
    // {
    //     ChangeInventoryWindow(itemClass.ToCachedString());
    // }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *              * Filtering Inventory *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    /// <summary>
    /// Change inventory window with Filtering.
    /// Filtering Method, variables prefix means: u => union('or' condition), i => intersection('and' condition), e => exception.
    /// </summary>
    [BoxGroup("Inventory/I/Inventory Item Viewer"), Button("Filter Inventory", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    public void OpenInventoryViewerWithFilting(InventoryViewerName inventoryViewerName, FilteringData filteringData, bool isDisplayBlackOverlay = true, Action completeAction = null)
    {
        OpenInventory(inventoryViewerName);

        InventoryViewerList[windowManager.currentWindowIndex].Filtering(filteringData, isDisplayBlackOverlay, completeAction);
    }

    public void OpenInventoryViewerWithFilting(string inventoryViewerName, FilteringData filteringData, bool isDisplayBlackOverlay = true, Action completeAction = null)
    {
        OpenInventoryViewerWithFilting(inventoryViewerName.ToCachedEnum<InventoryViewerName>(), filteringData, isDisplayBlackOverlay, completeAction);
    }

    public void OpenInventoryViewerWithFilting(PlayFabManager.CatalogItemClass catalogItemClass, FilteringData filteringData, bool isDisplayBlackOverlay = true, Action completeAction = null)
    {
        OpenInventoryViewerWithFilting(catalogItemClass.ToCachedString(), filteringData, isDisplayBlackOverlay, completeAction);
    }
}
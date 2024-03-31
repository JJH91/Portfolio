using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using PlayFab.ClientModels;
using PolyAndCode.UI;
using Sirenix.OdinInspector;

public class InventoryItem : RecyclableItem<InventoryItemData>
{
    public enum InventoryItemType { SimpleType }
    public enum AmountDisplayType { RemainUses, UsesIncremented, ItemAmount }

    [TitleGroup("Inventory Item"), BoxGroup("Inventory Item/II", showLabel: false)]
    [BoxGroup("Inventory Item/II/Setting"), SerializeField] InventoryItemType itemType;
    [BoxGroup("Inventory Item/II/Setting"), SerializeField] AmountDisplayType amountDisplayType;
    [BoxGroup("Inventory Item/II/Setting"), SerializeField] bool isLargeInventoryItem;
    [BoxGroup("Inventory Item/II/Display Setting"), SerializeField] bool isDisplayBlackOverlay, isDisplayStar, isDisplayLv;

    [TitleGroup("Grade Difference"), BoxGroup("Grade Difference/GD", showLabel: false)]
    [BoxGroup("Grade Difference/GD/Background Image Name List"), SerializeField] List<string> backgroundImageNameList;

    [TitleGroup("UI"), BoxGroup("UI/U", showLabel: false)]
    [BoxGroup("UI/U")]
    [BoxGroup("UI/U/Star"), SerializeField] List<GameObject> starImgList;
    [BoxGroup("UI/U/Unit UGUI Parent"), ShowIf("@isLargeInventoryItem"), SerializeField] RectTransform unitUguiRect;
    [BoxGroup("UI/U/Unit UGUI Parent"), ShowIf("@isLargeInventoryItem"), SerializeField] DragonBonesUGUI dragonBonesUGUI;
    public DragonBonesUGUI DragonBonesUGUI { get => dragonBonesUGUI; }
    [BoxGroup("UI/U/Now Equipped"), SerializeField] GameObject blackOverlay;
    [BoxGroup("UI/U/Image"), SerializeField] Image itemBackgroundImage, icon;
    [BoxGroup("UI/U/Slider"), SerializeField] Slider slider;
    [BoxGroup("UI/U/Text"), SerializeField] TextMeshProUGUI underText, blackOverlayText;
    [BoxGroup("UI/U/Outline"), SerializeField] GameObject selectEffect;
    [BoxGroup("UI/U/Button"), SerializeField] LongClickableButton lcButton;
    public LongClickableButton LcButton { get => lcButton; private set => lcButton = value; }

    ObjectManager.AssetHandle<SpriteAtlas> iconAssetHandle;
    ObjectManager.AssetHandle<SpriteAtlas> uiAssetHandle;

    protected override void Awake()
    {
        iconAssetHandle = ObjectManager.Instance.GetAtlasAssetHandle(NameManager.UiIconAtlasName, this);
        uiAssetHandle = ObjectManager.Instance.GetAtlasAssetHandle(NameManager.UiFrameAtlasName, this);

        base.Awake();
    }

    protected override void OnEnable()
    {
        // 버튼 활성화.
        LcButton.enabled = true;
        LcButton.interactable = true;

        // 드래곤본 UGUI 렉트 활성화.
        // ? ItemMainSpecModule 에서 더미 아이템으로 사용된 DragonBones UGUI 가 인벤토리 업데이트 콜백에 의해 재활성화 되는 것을 방지하기 위해 이곳에서 재활성화 및 클리어.
        if (unitUguiRect != null)
        {
            unitUguiRect.gameObject.SetActive(true);
            unitUguiRect.SetDeactiveAllChildObeject();
        }

        // Playfab 이벤트 구독.
        PlayFabManager.Instance.OnMainCatalogUpdatedAct += RefreshCatalogItem;
        PlayFabManager.Instance.OnUserInventoryUpdatedAct += RefreshItemInstance;

        base.OnEnable();
    }

    protected override void OnDisable()
    {
        // Playfab 이벤트 구독 해제.
        PlayFabManager.Instance.OnMainCatalogUpdatedAct -= RefreshCatalogItem;
        PlayFabManager.Instance.OnUserInventoryUpdatedAct -= RefreshItemInstance;

        // 동적으로 추가한 이벤트 구독 해지 및 클릭 설정 초기화.
        LcButton.onClick.RemoveAllListeners();
        LcButton.IsInvokeClickEventOnLongClick = false;

        // 모든 정보 초기화.
        for (int i = 0; i < starImgList.Count; i++)
            starImgList[i].SetActive(false);
        blackOverlay.SetActive(false);
        underText.text = string.Empty;

        base.OnDisable();

        // gameObject.Log($"인벤토리 아이템 비활성화.");
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                     * Test *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [Button("Test")]
    void Test()
    {
        icon.SetMaxSizeHoldRatio();
    }

    [Button("Test2")]
    void Test2()
    {
        LcButton.onClick.RemoveAllListeners();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Configure Cell *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void ConfigureCell(InventoryItemType itemType, AmountDisplayType amountDisplayType, KeyValuePair<string, InventoryItemData> inventoryItemDataKvp, int cellIndex, bool isDisplayBlackOverlay = false, bool isDisplayStar = true, bool isDisplayLv = true)
    {
        this.itemType = itemType;
        this.amountDisplayType = amountDisplayType;
        this.isDisplayBlackOverlay = isDisplayBlackOverlay;
        this.isDisplayStar = isDisplayStar;
        this.isDisplayLv = isDisplayLv;

        base.ConfigureCell(inventoryItemDataKvp, cellIndex);
    }

    public void ConfigureCell(InventoryItemType itemType, AmountDisplayType amountDisplayType, InventoryItemData inventoryItemData, int cellIndex, bool isDisplayBlackOverlay = false, bool isDisplayStar = true, bool isDisplayLv = true)
    {
        this.ConfigureCell(itemType, amountDisplayType, new KeyValuePair<string, InventoryItemData>(inventoryItemData.Key, inventoryItemData), cellIndex, isDisplayBlackOverlay, isDisplayStar, isDisplayLv);
    }

    /// <summary>
    /// Only virtual currency reward displaying item configere method. The 'data' is kvp of Virtual Currency Dictionary.
    /// </summary>
    public void ConfigureVcCell(InventoryItemType itemType, string virtualCurrencyKey, int amount, int cellIndex)
    {
        this.itemType = itemType;
        this.amountDisplayType = AmountDisplayType.UsesIncremented;

        // 재화는 아이템 인스턴스가 없음, 임시적으로 인스턴스를 생성하고, 재화량 표시를 위해 값 대입.
        var tempItemInstance = new ItemInstance()
        {
            ItemId = virtualCurrencyKey,
            UsesIncrementedBy = amount
        };

        var tempKvp = new KeyValuePair<string, InventoryItemData>(virtualCurrencyKey, tempItemInstance.GetAssembledInventoryItemData());
        base.ConfigureCell(tempKvp, cellIndex);
    }

    public void ConfigureSkillCell(InventoryItemType itemType, CatalogItem skillCatalogItem, int skillLv, DragonBonesUnit.DragonBonesUnitType skillUseUnitType)
    {
        this.itemType = itemType;
        this.isDisplayBlackOverlay = false;
        this.isDisplayStar = false;
        this.isDisplayLv = false;

        // 리싸이클러 뷰로 사용할게 아니기 때문에 cellIndex 값을 0으로 줌.
        var tempKvp = new KeyValuePair<string, InventoryItemData>(skillCatalogItem.ItemId, skillCatalogItem.GetAssembledInventoryItemDataForSkill(skillLv, skillUseUnitType));
        base.ConfigureCell(tempKvp, 0);

        UpdateItemUI_Skill();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Update UI *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Inventory Item/II/Update UI", order: 2), Button("Update UI", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    public override void UpdateUI()
    {
        // 유닛인지 아닌지에 따라 아이템 정보 표시. 변화.
        if (ItemData.CatalogItemClass != PlayFabManager.CatalogItemClass.Unit)
        {
            if (isLargeInventoryItem)
                unitUguiRect.SetDeactiveAllChildObeject();

            if (amountDisplayType == AmountDisplayType.RemainUses)
                underText.text = ItemData.RemainUses.HasValue ? $"{ItemData.RemainUses.Value:N0}" : string.Empty;
            else if (amountDisplayType == AmountDisplayType.UsesIncremented)
                underText.text = ItemData.UsesIncremented.HasValue ? $"{ItemData.UsesIncremented.Value:N0}" : string.Empty;
            else
                underText.text = ItemData.ItemAmount == 0 ? string.Empty : $"{ItemData.ItemAmount:N0}";

            // 아이템 활성화 및 이미지와 회전 설정.
            icon.gameObject.SetActive(true);
            var iconSpriteName = ItemData.CatalogItem.ItemId.ToCachedEnum<PlayFabManager.VirtualCurrencyCode>().GetVirtualCurrenySpriteName();
            icon.sprite = iconAssetHandle.GetAtlasedSprite(iconSpriteName.IsNullOrEmpty() ? ItemData.CatalogItem.ItemId : iconSpriteName);
            icon.SetMaxSizeHoldRatio();

            // if (PlayFabManager.CatalogItemTag.Weapons < ItemData.CatalogItemMainTag && ItemData.CatalogItemMainTag < PlayFabManager.CatalogItemTag.Weapons_End)
            // {
            //     icon.transform.localEulerAngles = new Vector3(0, 0, -60);

            //     // 권총은 사이즈를 절반으로 줄임.
            //     if (ItemData.CatalogItemMainTag == PlayFabManager.CatalogItemTag.Pistol)
            //         icon.transform.localScale = Vector3.one * 0.7f;
            //     else
            //         icon.transform.localScale = Vector3.one;
            // }
            // else
            // {
            //     icon.transform.localEulerAngles = Vector3.zero;
            //     icon.transform.localScale = Vector3.one;
            // }

            // 아이템 커스텀 데이터가 장착중인 캐릭터 아이디 값을 가지고 있으면 장착중이라고 표시.
            ItemData.IsEquipped = ItemData.ItemInstance.CustomData != null && ItemData.ItemInstance.IsCustomDataHasKeyAndNonNullValue(nameof(EquipmentStatModifierData.EquippedCharacterInstId));
        }
        else
        {
            // ? ItemMainSpecModule 에서 더미 아이템으로 사용된 DragonBones UGUI 가 인벤토리 업데이트 콜백에 의해 재활성화 되는 것을 방지하기 위해,
            // ? OnEnable 에서 기존 드래곤본 UGUI 비활성화 후 새로 생성.

            // 아이콘 비활성화.
            icon.gameObject.SetActive(false);

            // 기존 드래곤본 유닛 UGUI 비활성화.
            unitUguiRect.SetDeactiveAllChildObeject();

            dragonBonesUGUI = ObjectManager.Instance.GetDragonBonesUGUI(ItemData.ItemInstance.ItemId, unitUguiRect);
            // 캐릭터인 경우, 기본 자세를 재생시킴. 드래곤본의 기본 자세가 이상한 경우를 위함.
            if (ItemData.CatalogItemMainTag == PlayFabManager.CatalogItemTag.Character && unitUguiRect.gameObject.activeSelf)
                dragonBonesUGUI.PlayCharacterAnimationByWeaponStatModData(null, null);

            underText.text = string.Empty;
        }

        // 아이템 등급에 따라 백그라운드 이미지 수정.
        itemBackgroundImage.sprite = uiAssetHandle.GetAtlasedSprite(backgroundImageNameList[(int)ItemData.Grade - 1]);

        // 아이템 진급 등급 표시.
        for (int i = 0; i < starImgList.Count; i++)
            if (isDisplayStar)
                starImgList[i].SetActive(i < (int)ItemData.PrmGrade);
            else
                starImgList[i].SetActive(false);

        // 아이템 레벨 표시.
        if (underText.text == string.Empty && isDisplayLv)
            underText.text = $"Lv. {ItemData.Lv}";

        // 오버레이 표시.
        SetBlackOverlayText(isDisplayBlackOverlay && ItemData.IsEquipped, isDisplayBlackOverlay && ItemData.IsSelected);
    }

    [BoxGroup("Inventory Item/II/Update UI", order: 2), Button("Update UI_Skill", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    public void UpdateItemUI_Skill()
    {
        if (amountDisplayType == AmountDisplayType.RemainUses)
            underText.text = string.Empty;

        icon.sprite = iconAssetHandle.GetAtlasedSprite(ItemData.CatalogItem.ItemId);
        icon.SetMaxSizeHoldRatio();

        // 장착중 표시. 해제.
        blackOverlay.SetActive(false);

        // 아이템 등급에 따라 백그라운드 이미지 수정.
        itemBackgroundImage.sprite = uiAssetHandle.GetAtlasedSprite(backgroundImageNameList[(int)ItemData.Grade - 1]);

        // 아이템 레벨 표시.
        if (underText.text == string.Empty && isDisplayLv)
            underText.text = $"Lv. {ItemData.Lv}";
    }

    public void SetDeactiveUguiBox()
    {
        if (unitUguiRect != null)
            unitUguiRect.gameObject.SetActive(false);
    }

    public void SetBlackOverlayText(bool isEquipped, bool isSelected)
    {
        blackOverlay.SetActive(isEquipped || isSelected);

        if (isEquipped)
            blackOverlayText.text = "Equipped";
        else if (isSelected)
            blackOverlayText.text = "Selected";
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * On Click Event *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public override void InvokeOnItemSelectAction()
    {
        base.InvokeOnItemSelectAction();

        // TODO: 현재 활성화된 게임 매니저 윈도우에 따라 행동 1차 분류.
        // TODO: 아이템 클래스에 따라서 행동 2차분류.

        if (GameManager.Instance.CurSceneName == NameManager.SceneName.Lobby)
        {
            // 각 윈도우의 내부 내용 변경.
            // 스킬이 아니면 아이템 데이터 인스펙터의 변경과 인벤토리 변경.
            // 스킬이라면 유닛 데이터 인스펙터에 스킬정보 표시. => SkillInfoModule 에서 이벤트를 동적으로 등록함.
            if (ItemData.CatalogItemMainTag != PlayFabManager.CatalogItemTag.Unit_Skill && ItemData.CatalogItemMainTag != PlayFabManager.CatalogItemTag.Bullet_Skill)
                switch (SceneAssistManager.Instance.SceneMainWindowManager.GetCurrentWindowName<NameManager.LobbySceneWindowName>())
                {
                    case NameManager.LobbySceneWindowName.UnitPreset:
                        UnitPresetManager.Instance.OnItemSelectInUnitPresetWindow(this);
                        break;

                    case NameManager.LobbySceneWindowName.UnitSetting:
                        // 캐릭터 아이템을 클릭하면 캐릭터 데이터 인스펙터를 변경.
                        if (ItemData.CatalogItemClass == PlayFabManager.CatalogItemClass.Unit)
                            UnitSettingManager.Instance.SetUnitStatDataOnUnitViewer(ItemData);

                        UnitSettingManager.Instance.SelectedItemSpecInspector.UpdateInspectorUI(ItemData);
                        UnitSettingManager.Instance.ButtonCgControll();

                        // 아이템이 현재 USM에 선택된 유닛이 장착한 무기인 경우(유닛 스펙 인스펙터 상의 장비 아이템 클릭), 이 아이템의 장비 인덱스를 찾아줌.
                        // ? 'ItemData.IsSelected': 현재 아이템이 USM에서 선택된 유닛이 장착한 아이템이라는 표시로 사용됨.
                        if (ItemData.IsSelected)
                            UnitSettingManager.Instance.SetSelectedEquipmentIndexOnUnitEquipmentRectClick(UnitSettingManager.Instance.SelectedCharacterStatData.EqInstIds.FindIndex(id => id == ItemData.Key));
                        else if (ItemData.CatalogItemClass == PlayFabManager.CatalogItemClass.Unit)
                            UnitSettingManager.Instance.SetSelectedEquipmentIndexOnUnitEquipmentRectClick(-1);

                        // 인벤토리 윈도우 변경.
                        UnitSettingManager.Instance.OnUnitEquipmentRectClick(ItemData.CatalogItem.ItemClass);
                        break;

                    case NameManager.LobbySceneWindowName.Forge:
                        UpgradeManager.Instance.OnInventoryItemSelectEvent(this);
                        break;

                    case NameManager.LobbySceneWindowName.Summon:
                        SummonManager.Instance.DisplayPickupItemSpecInspector(ItemData);
                        break;
                }
        }
    }

    public override void InvokeOnItemLongSelectAction()
    {
        base.InvokeOnItemLongSelectAction();
        gameObject.Log($"아이템 롱클릭 이벤트: {ItemData.CatalogItem.ItemId}");

        // TODO: 현재 활성화된 게임 매니저 윈도우에 따라 행동 1차 분류.
        // TODO: 아이템 클래스에 따라서 행동 2차분류.

        if (GameManager.Instance.CurSceneName == NameManager.SceneName.Lobby)
        {
            // 각 윈도우의 내부 내용 변경.
            switch (SceneAssistManager.Instance.SceneMainWindowManager.windows[SceneAssistManager.Instance.SceneMainWindowManager.currentWindowIndex].windowName.ToCachedEnum<NameManager.LobbySceneWindowName>())
            {
                case NameManager.LobbySceneWindowName.UnitPreset:
                    UnitPresetManager.Instance.KeepCurrentState(ItemData);
                    SceneAssistManager.Instance.SceneMainWindowManager.OpenWindow(NameManager.LobbySceneWindowName.UnitSetting.ToCachedUncamelCaseString());
                    break;

                case NameManager.LobbySceneWindowName.UnitSetting:
                    // TODO: 롱 클릭이 필요하면 구현.
                    // // 스킬이 아니면 아이템 데이터 인스펙터의 변경과 인벤토리 변경
                    // if (ItemData.CatalogItemMainTag != PlayFabManager.CatalogItemTag.UnitSkill && ItemData.CatalogItemMainTag != PlayFabManager.CatalogItemTag.BulletSkill)
                    // {
                    //     // 캐릭터 아이템을 클릭하면 캐릭터 데이터 인스펙터를 변경
                    //     if (ItemData.CatalogItemClass == PlayFabManager.CatalogItemClass.Unit)
                    //         UnitSettingManager.Instance.SetCharacterDataOnViewer(this);

                    //     UnitSettingManager.Instance.ItemSpecInspector.SetItemDataOnInspector(this);

                    //     // 인벤토리 윈도우 변경
                    //     Inventory.Instance.ChangeInventoryWindow(ItemData.CatalogItemClass);
                    // }
                    // // 스킬이라면 유닛 데이터 인스펙터에 스킬정보 표시. => SkillInfoModule 에서 이벤트를 동적으로 등록함
                    break;

                case NameManager.LobbySceneWindowName.Forge:
                    UpgradeManager.Instance.OnInventoryItemLongClickEvent(ItemData);
                    break;
            }
        }
    }
    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Update Action *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void RefreshCatalogItem()
    {
        if (ItemData != null && ItemData.CatalogItem != null && !ItemData.CatalogItem.ItemId.IsNullOrEmpty())
        {
            ItemData.CatalogItem = PlayFabManager.Instance.MainCatalogItemDictionary[ItemData.CatalogItem.ItemId];
            ItemData = ItemData.UpdateData();

            UpdateUI();
        }
    }

    public void RefreshItemInstance()
    {
        if (ItemData != null && ItemData.ItemInstance != null && !ItemData.ItemInstance.ItemInstanceId.IsNullOrEmpty())
            if (PlayFabManager.Instance.InventoryItemDictionary.ContainsKey(ItemData.CatalogItemClass) && PlayFabManager.Instance.InventoryItemDictionary[ItemData.CatalogItemClass].ContainsKey(ItemData.ItemInstance.ItemInstanceId))
            {
                ItemData.ItemInstance = PlayFabManager.Instance.InventoryItemDictionary[ItemData.CatalogItemClass][ItemData.ItemInstance.ItemInstanceId];
                ItemData = ItemData.UpdateData();

                UpdateUI();
            }
    }
}

[System.Serializable]
public class InventoryItemData : RecyclableItemData
{
    [ShowInInspector] public int Lv { get; set; } = 1;
    [ShowInInspector] public int AwakeningLv { get; set; }
    [ShowInInspector] public int? RemainUses { get; set; }
    [ShowInInspector] public int? UsesIncremented { get; set; }
    [ShowInInspector] public string DisplayName { get; set; }
    [ShowInInspector] public string Description { get; set; }
    [ShowInInspector] public bool IsEquipped { get; set; }
    [ShowInInspector] public bool IsSelected { get; set; }
    [ShowInInspector] public NameManager.Grade Grade { get; set; }
    [ShowInInspector] public NameManager.Grade PrmGrade { get; set; }

    public InventoryItemData AssembleInventoryItemData(CatalogItem catalogItem, ItemInstance itemInstance)
    {
        // 기본 데이터 조립.
        if (itemInstance != null)
            AssembleRecyclableItemData(itemInstance);
        else
            AssembleRecyclableItemData(catalogItem.GetTempItemInstance());

        // InventoryItemData 조립.
        var catalogInventoryItemData = CatalogItem.CustomData.DeserializeObject<InventoryItemData>();
        var itemInstanceInventoryItemData = ItemInstance.CustomData.DeserializeObject<InventoryItemData>();

        DisplayName = CatalogItem.DisplayName;
        Description = CatalogItem.Description;
        Grade = catalogInventoryItemData.Grade;
        PrmGrade = itemInstanceInventoryItemData.PrmGrade != NameManager.Grade.None ? itemInstanceInventoryItemData.PrmGrade : Grade;

        Lv = itemInstanceInventoryItemData.Lv;
        RemainUses = itemInstance == null ? new int?() : ItemInstance.RemainingUses;
        UsesIncremented = itemInstance == null ? new int?() : ItemInstance.UsesIncrementedBy;

        if (CatalogItemClass == PlayFabManager.CatalogItemClass.Weapon || CatalogItemClass == PlayFabManager.CatalogItemClass.Armor)
            IsEquipped = ItemInstance.IsCustomDataHasKeyAndNonNullValue(nameof(EquipmentStatModifierData.EquippedCharacterInstId));

        // ItemClassData 조립.
        switch (CatalogItemClass)
        {
            case PlayFabManager.CatalogItemClass.Unit:
                // 캐릭터 데이터 조립.
                if (CatalogItemMainTag == PlayFabManager.CatalogItemTag.Character || CatalogItemMainTag == PlayFabManager.CatalogItemTag.Player)
                    ExtraItemData = ItemInstance.GetAssembledCharacterStatData();
                Lv = (ExtraItemData as CharacterStatData).Lv;
                AwakeningLv = (ExtraItemData as CharacterStatData).AwakeningLv;
                break;

            case PlayFabManager.CatalogItemClass.Weapon:
                // 웨폰 데이터 조립.
                ExtraItemData = ItemInstance.GetAssembledWeaponStatModifierData();
                DisplayName = (ExtraItemData as WeaponStatModifierData).Name;
                Lv = (ExtraItemData as WeaponStatModifierData).Lv;
                AwakeningLv = (ExtraItemData as WeaponStatModifierData).AwakeningLv;
                break;

            case PlayFabManager.CatalogItemClass.Armor:
            case PlayFabManager.CatalogItemClass.Enhancement:
            case PlayFabManager.CatalogItemClass.Awakening:
                // 장비 데이터 조립.
                ExtraItemData = ItemInstance.GetAssembledEquipmentStatModifierData();
                DisplayName = (ExtraItemData as EquipmentStatModifierData).Name;
                Lv = (ExtraItemData as EquipmentStatModifierData).Lv;
                AwakeningLv = (ExtraItemData as EquipmentStatModifierData).AwakeningLv;
                break;

            case PlayFabManager.CatalogItemClass.Enchantment:
                // 인챈트 데이터 조립.
                ExtraItemData = ItemInstance.GetAssembledEnchantStatModifierData(itemInstance.ItemInstanceId.IsNullOrEmpty() ? catalogItem.ItemId : itemInstance.ItemInstanceId);
                break;
        }

        return this;
    }

    public InventoryItemData AssembleInventoryItemDataForSkill(CatalogItem catalogItem, int skillLv, DragonBonesUnit.DragonBonesUnitType skillUseUnitType)
    {
        // 기본 데이터 조립.
        AssembleRecyclableItemData(catalogItem);

        // 플레이팹 관련 데이터 클래스 초기화.
        CatalogItem = catalogItem;
        ItemInstance = new ItemInstance { ItemId = catalogItem.ItemId };

        // InventoryItemData 조립.
        var catalogInventoryItemData = CatalogItem.CustomData.DeserializeObject<InventoryItemData>();

        Grade = catalogInventoryItemData.Grade;

        DisplayName = CatalogItem.DisplayName;
        Description = CatalogItem.Description;
        Lv = skillLv;

        CatalogItemClass = ExtensionClass.ToCachedEnum<PlayFabManager.CatalogItemClass>(CatalogItem.ItemClass);

        // ItemClassData 조립.
        ExtraItemData = CatalogItem.GetAssembledSkillData(Lv, skillUseUnitType);

        return this;
    }

    public InventoryItemData UpdateData()
    {
        return AssembleInventoryItemData(CatalogItem, ItemInstance);
    }
}
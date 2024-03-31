using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Newtonsoft.Json.Linq;
using PlayFab.ClientModels;
using Sirenix.OdinInspector;

public class UnitSettingManager : MonoSingleton<UnitSettingManager>
{
    [TitleGroup("Unit Setting Manager"), BoxGroup("Unit Setting Manager/USM", showLabel: false)]
    [BoxGroup("Unit Setting Manager/USM/Keep Current State")]
    [BoxGroup("Unit Setting Manager/USM/Keep Current State/Is Keep Current State"), ShowInInspector] public bool IsKeepCurrentState { get; private set; }
    [BoxGroup("Unit Setting Manager/USM/Keep Current State/Is Keep Current State"), SerializeField] string keepSelectedUnitInstId;
    [BoxGroup("Unit Setting Manager/USM/Keep Current State/Is Keep Current State"), SerializeField] Vector2 keepInventoryViewerPosition;

    [BoxGroup("Unit Setting Manager/USM/Info")]
    [BoxGroup("Unit Setting Manager/USM/Info/Pre Selected Item Class Or Tag"), SerializeField] string preSelectedItemClassOrTag;
    [BoxGroup("Unit Setting Manager/USM/Info/LastInventoryFilteringData"), SerializeField] FilteringData lastInventoryFilteringData;

    // Unit.
    [BoxGroup("Unit Setting Manager/USM/Selected Unit")]
    [BoxGroup("Unit Setting Manager/USM/Selected Unit/Unit Dragon Bones UGUI"), SerializeField] DragonBonesUGUI dragonBonesUGUI;
    [BoxGroup("Unit Setting Manager/USM/Selected Unit/Unit Skill Info Module"), SerializeField] SkillInfoModule unitSkillInfoModule;
    [BoxGroup("Unit Setting Manager/USM/Selected Unit/Selected Unit Data"), ShowInInspector] public InventoryItemData SelectedUnitIvItemData { get; private set; }
    [BoxGroup("Unit Setting Manager/USM/Selected Unit/Selected Unit Data"), ShowInInspector] public CharacterStatData SelectedCharacterStatData { get; private set; }

    // Item.
    [BoxGroup("Unit Setting Manager/USM/Selected Item")]
    [BoxGroup("Unit Setting Manager/USM/Selected Item/Item Spec Inspector"), SerializeField] ItemSpecInspector selectedItemSpecInspector;
    public ItemSpecInspector SelectedItemSpecInspector { get => selectedItemSpecInspector; }
    [BoxGroup("Unit Setting Manager/USM/Selected Unit/Equipped Equipment Inventory Item List"), SerializeField] List<InventoryItem> eqInventoryItemList = new List<InventoryItem>();
    [BoxGroup("Unit Setting Manager/USM/Selected Unit/Last Selected Equipment Index"), ShowInInspector] public int CurSelectedEquipmentIndex { get; set; }

    // Change Equipped Item.
    [BoxGroup("Unit Setting Manager/USM/Change Equipment")]
    [BoxGroup("Unit Setting Manager/USM/Change Equipment/Max Equipment Character Updatable Count"), SerializeField, ReadOnly] int maxUpdateCount = 6;

    [TitleGroup("UI", order: 1), BoxGroup("UI/U", showLabel: false)]
    [BoxGroup("UI/U/Selected Unit")]
    [BoxGroup("UI/U/Selected Unit/Text"), SerializeField] TextMeshProUGUI nameText, levelText, atkText, defText, dpsText, toughnessText;
    [BoxGroup("UI/U/Selected Unit/Scroll Rect"), SerializeField] ScrollRect unitSpecInspectorScrollRect;

    [BoxGroup("UI/U/Selected Item")]
    [BoxGroup("UI/U/Selected Item/Equipped Item Position Rect"), SerializeField] RectTransform unitUguiRect, weaponItemRect_1, weaponItemRect_2, armorItemRect1, armorItemRect2, accItemRect;
    RectTransform[] equippedItemPositionRectArr;
    [BoxGroup("UI/U/Selected Item/Equipment Rect Select Effect"), SerializeField] RectTransform equipmentRectSelectEffect;
    [BoxGroup("UI/U/Selected Item/Button Canvas Group"), SerializeField] CanvasGroup equipCg, unequipCg;

    enum ES3EquipmentChangeLog { BeforeEquippedInfoDictionary, AfterEquippedInfoDictionary }

    private void Awake()
    {
        base.MakeDestroyableInstance();

        equippedItemPositionRectArr = new RectTransform[] { weaponItemRect_1, weaponItemRect_2, armorItemRect1, armorItemRect2, accItemRect };
        equipmentRectSelectEffect.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        UpdateUnitEquipmentItem(true);

    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *          * Set Unit Data On Unit Viewer *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void SetUnitStatDataOnUnitViewer(InventoryItemData selectedUnitIvItemData, bool isUpdate = false)
    {
        if (!isUpdate && SelectedUnitIvItemData == selectedUnitIvItemData)
            return;

        SelectedUnitIvItemData = selectedUnitIvItemData.ItemInstance.GetAssembledInventoryItemData();
        SelectedCharacterStatData = SelectedUnitIvItemData.ExtraItemData as CharacterStatData;

        // Clear pre unit equipments.
        for (int i = 0; i < eqInventoryItemList.Count; i++)
            eqInventoryItemList[i].gameObject.SetActive(false);
        eqInventoryItemList.Clear();

        if (SelectedUnitIvItemData.CatalogItemMainTag == PlayFabManager.CatalogItemTag.Character)
        {
            // Assemble character's skill data.
            // TODO: 현재는 캐릭터 스킬 비활성화, 나중에 유닛 뽑기 구현하면 그때 스킬 구현

            // Clear skill info module.
            unitSkillInfoModule.ClearSKillInfoUI();

            // TODO: Edit scroll view contents sizes and posions about skill info module.
            // ? 아이템을 장착하지 않았을 경우에 스킬의 빈 렉트를 표시하지 않도록 수정하면 해당 기능 구현.

            // Assemble equipped item data.
            DisplayUnitEquipmentItemOnViewer(weaponItemRect_1, PlayFabManager.CatalogItemClass.Weapon, SelectedCharacterStatData.EqInstIds[0]);
            DisplayUnitEquipmentItemOnViewer(weaponItemRect_2, PlayFabManager.CatalogItemClass.Weapon, SelectedCharacterStatData.EqInstIds[1]);
            DisplayUnitEquipmentItemOnViewer(armorItemRect1, PlayFabManager.CatalogItemClass.Armor, SelectedCharacterStatData.EqInstIds[2]);
            DisplayUnitEquipmentItemOnViewer(armorItemRect2, PlayFabManager.CatalogItemClass.Armor, SelectedCharacterStatData.EqInstIds[3]);
            // DisplayUnitEquipmentItemOnViewer(accItemRect, PlayFabManager.CatalogItemClass.Accessory, SelectedCharacterData.EqInstIds[4])

            // Display unit UGUI.
            if (dragonBonesUGUI != null)
                dragonBonesUGUI.gameObject.SetActive(false);
            dragonBonesUGUI = ObjectManager.Instance.GetDragonBonesUGUI(SelectedUnitIvItemData.CatalogItem.ItemId, unitUguiRect);
            ChangeWeaponWithUpdateUnitSpecInspector(isUpdate ? CurSelectedEquipmentIndex : 0);
        }

        // Scrolling to top.
        unitSpecInspectorScrollRect.DOVerticalNormalizedPos(1, 0.2f);
    }

    void DisplayUnitEquipmentItemOnViewer(RectTransform itemDisplayRect, PlayFabManager.CatalogItemClass catalogItemClass, string equipmentInstId)
    {
        if (equipmentInstId.IsNullOrEmpty())
            return;

        // Get equipped item instance.
        var equipmentItemInstance = PlayFabManager.Instance.InventoryItemDictionary[catalogItemClass][equipmentInstId];

        // Display equipped item.
        var equipmentIvItem = ObjectManager.Instance.GetInventoryItem();
        equipmentIvItem.RectTransform.SetParent(itemDisplayRect);
        equipmentIvItem.RectTransform.anchoredPosition = Vector2.zero;
        equipmentIvItem.ConfigureCell(InventoryItem.InventoryItemType.ThickType, InventoryItem.AmountDisplayType.RemainUses, equipmentItemInstance.GetAssembledInventoryItemData(), 0);
        // ? 선택 이벤트 메소드에서 해당 아이템이 현재 선택된 유닛이 장착한 아이템이라는 표시로 사용됨.
        equipmentIvItem.ItemData.IsSelected = true;

        eqInventoryItemList.Add(equipmentIvItem);

        // Display equipment skill.
        unitSkillInfoModule.UpdateSkillInfoUI(equipmentItemInstance, false, SelectedCharacterStatData.EqInstIds.FindIndex(id => id == equipmentInstId) % 2, 0, unitSpecInspectorScrollRect);
    }

    public void ChangeWeaponWithUpdateUnitSpecInspector(int index = 0)
    {
        if (index > 1)
            return;

        var isEqWeaponInstId_1Exist = !SelectedCharacterStatData.EqWeaponInstId_0.IsNullOrEmpty();
        var isEqWeaponInstId_2Exist = !SelectedCharacterStatData.EqWeaponInstId_1.IsNullOrEmpty();

        WeaponStatModifierData weaponStatModData = null;
        if (isEqWeaponInstId_1Exist && isEqWeaponInstId_2Exist)
            weaponStatModData = eqInventoryItemList[index].ItemData.ExtraItemData as WeaponStatModifierData;
        else if (isEqWeaponInstId_1Exist || isEqWeaponInstId_2Exist)
        {
            var eqIvItem = eqInventoryItemList.Where(item => item.ItemData.Key == SelectedCharacterStatData.EqInstIds[index]).FirstOrDefault();

            if (eqIvItem != null)
                weaponStatModData = eqIvItem.ItemData.ExtraItemData as WeaponStatModifierData;
        }

        dragonBonesUGUI.PlayCharacterAnimationByWeaponStatModData(SelectedCharacterStatData, weaponStatModData);

        // Get unit data which all item spec applied.
        SelectedCharacterStatData.ApplyAllEquipmentStatModifierData(index <= 1 ? index : 0);

        // Display unit main spec.
        nameText.text = SelectedUnitIvItemData.DisplayName;
        levelText.text = $"{SelectedCharacterStatData.Lv:N0} / {100:N0}";
        atkText.text = $"{SelectedCharacterStatData.Atk:N0}";
        defText.text = $"{SelectedCharacterStatData.Def:N0}";
        dpsText.text = $"{SelectedCharacterStatData.Dps:N2}";
        toughnessText.text = $"{SelectedCharacterStatData.Tough:N2}";
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *      * On Item Equip / Unequip Button Click *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void OnItemEquipButtonClick()
    {
        ChangeUnitEquippedItem(SelectedItemSpecInspector.DisplayingInventoryItemData, true);
    }

    public void OnItemUnequipButtonClick()
    {
        var equipmentIndex = SelectedCharacterStatData.EqInstIds.FindIndex(id => id == SelectedItemSpecInspector.DisplayingInventoryItemData.Key);
        SetSelectedEquipmentIndexOnUnitEquipmentRectClick(equipmentIndex);

        ChangeUnitEquippedItem(SelectedItemSpecInspector.DisplayingInventoryItemData, false);
    }

    void ChangeUnitEquippedItem(InventoryItemData editEquippingInventoryItemData, bool isEquipItem, string editEquipInfoUnitInstId = null)
    {
        // 선택된 유닛이 없을 때.
        if (SelectedUnitIvItemData == null)
        {
            gameObject.LogError($"선택된 캐릭터가 없습니다.");
            return;
        }

        // 선택된 장비가 없을 떄.
        if (editEquippingInventoryItemData == null)
        {
            gameObject.LogError($"선택된 아이템이 없습니다.");
            return;
        }

        // 변경할 장비 리스트의 인덱스 지정 및 장비가 아닌 아이템을 장착 시도했는지 체크.
        if (!(editEquippingInventoryItemData.CatalogItemClass == PlayFabManager.CatalogItemClass.Weapon || editEquippingInventoryItemData.CatalogItemClass == PlayFabManager.CatalogItemClass.Armor))
        {
            gameObject.LogError($"장비가 아닌 아이템({editEquippingInventoryItemData.CatalogItemClass})을 장착할 수 없습니다.");
            return;
        }

        // 방어구인 경우, 같은 종류의 방어구를 장착하는지 체크.
        if (editEquippingInventoryItemData.CatalogItemClass == PlayFabManager.CatalogItemClass.Armor)
        {
            var theOtherEqInstId = GetTheOtherArmorItemEqInstId();
            if (!theOtherEqInstId.IsNullOrEmpty())
                if (editEquippingInventoryItemData.CatalogItemMainTag == eqInventoryItemList.Where(item => item.ItemData.Key == theOtherEqInstId).FirstOrDefault().ItemData.CatalogItemMainTag)
                {
                    gameObject.LogError($"같은 종류의 방어구({editEquippingInventoryItemData.CatalogItemMainTag})는 장착할 수 없습니다.");
                    return;
                }
        }

        // 장비를 변경할 유닛의 데이터.
        editEquipInfoUnitInstId ??= SelectedUnitIvItemData.ItemInstance.ItemInstanceId;
        var editEquipInfoUnitItemInstance = PlayFabManager.Instance.InventoryItemDictionary[PlayFabManager.CatalogItemClass.Unit][editEquipInfoUnitInstId];
        var editEquipInfoCharacterData = editEquipInfoUnitItemInstance.GetAssembledCharacterStatData();

        // 장비 데이터.
        var editEquippingEquipmentStatModData = editEquippingInventoryItemData.ExtraItemData as EquipmentStatModifierData;

        if (isEquipItem && editEquippingInventoryItemData.CatalogItemClass == PlayFabManager.CatalogItemClass.Weapon)
            if (!editEquipInfoCharacterData.WeaponType.Contains(editEquippingInventoryItemData.CatalogItemMainTag))
            {
                gameObject.LogError($"캐릭터가 장착할 수 있는 장비입니다.");
                return;
            }

        // * 새로 장착하려는 장비가 다른 유닛이 장착 중인 경우,
        // 먼저 장착 중이던 유닛의 장비를 해제시키는 절차를 진행.
        if (isEquipItem && editEquippingInventoryItemData.IsEquipped)
            if (!editEquippingEquipmentStatModData.EquippedCharacterInstId.IsNullOrEmpty() && editEquippingEquipmentStatModData.EquippedCharacterInstId != editEquipInfoUnitInstId)
            {
                // 기존에 장비를 착용 중이던 유닛의 장비를 해제 후 현재 유닛에게 장비 시킬 것인지 유저에게 물음.
                var preEquippingUnitName = PlayFabManager.Instance.InventoryItemDictionary[PlayFabManager.CatalogItemClass.Unit][editEquippingEquipmentStatModData.EquippedCharacterInstId].DisplayName;
                SceneModalWindowManager.Instance.OpenModalWindow(SceneModalWindowManager.ModalWindowName.ChangeCharacterEquippedItem, preEquippingUnitName, () => ChangeItemEquippedUnit(editEquippingInventoryItemData, editEquippingEquipmentStatModData.EquippedCharacterInstId, editEquipInfoUnitInstId), null);
                return;
            }


        // * 유닛의 장비 목록 변경 전 데이터 백업.
        SaveEquippedItemInfoChangeToEs3(ES3EquipmentChangeLog.BeforeEquippedInfoDictionary, editEquipInfoUnitInstId, editEquipInfoCharacterData.EqInstIds);

        // 장비의 인덱스.
        // TODO: 장착하는 장비의 갯수가 추가되는 경우 여기를 수정해야함.

        // * 유닛이 기존에 장비하고 있던 아이템의 장착 정보를 초기화.
        if (isEquipItem && editEquipInfoCharacterData.EqInstIds.Count > 0 && !editEquipInfoCharacterData.EqInstIds[CurSelectedEquipmentIndex].IsNullOrEmpty())
        {
            var equippedItemInstId = editEquipInfoCharacterData.EqInstIds[CurSelectedEquipmentIndex];
            PlayFabManager.Instance.InventoryItemDictionary[editEquippingInventoryItemData.CatalogItemClass][equippedItemInstId]
                .CustomDataAddOrValueChange(nameof(EquipmentStatModifierData.EquippedCharacterInstId), null);
        }

        // * 유닛의 아이템 장착 데이터 수정(오프라인).
        // TODO: 장착하는 장비의 갯수가 추가되는 경우 여기를 수정해야함.
        if (editEquipInfoCharacterData.EqInstIds.Count == 0)
            editEquipInfoCharacterData.EqInstIds = new List<string>(5);
        editEquipInfoCharacterData.EqInstIds[CurSelectedEquipmentIndex] = isEquipItem ? editEquippingInventoryItemData.ItemInstance.ItemInstanceId : null;
        editEquipInfoUnitItemInstance.CustomDataAddOrValueChange(nameof(CharacterStatData.EqInstIds), editEquipInfoCharacterData.EqInstIds.SerializeObject());
        if (editEquipInfoUnitInstId == SelectedUnitIvItemData.Key)
            SelectedCharacterStatData.EqInstIds[CurSelectedEquipmentIndex] = isEquipItem ? editEquippingInventoryItemData.ItemInstance.ItemInstanceId : null;

        // * 장비할 아이템에 장착 캐릭터 정보 변경.
        editEquippingInventoryItemData.ItemInstance.CustomDataAddOrValueChange(nameof(EquipmentStatModifierData.EquippedCharacterInstId), isEquipItem ? editEquipInfoUnitInstId : null);

        // * 유닛의 장비 목록 변경 후 데이터 백업.
        SaveEquippedItemInfoChangeToEs3(ES3EquipmentChangeLog.AfterEquippedInfoDictionary, editEquipInfoUnitInstId, editEquipInfoCharacterData.EqInstIds);

        // * 서버에 변경 사항 업데이트. 최대 누적 개수에 도달해야만 실행됨.
        UpdateUnitEquipmentItem();

        // ? 이 조건문은 선택한 유닛이 아닌 다른 유닛의 장비를 변경하는 경우 업데이트를 하지않기 위함.
        if (SelectedUnitIvItemData.ItemInstance.ItemInstanceId == editEquipInfoUnitInstId)
        {
            // 인벤토리 아이템 업데이트(장착 정보 변경됨, 장착 정보는 업데이트 호출만해도 적용됨).
            Inventory.Instance.CurOpendInventoryViewer.UpdateAllItemUI();

            // 선택된 캐릭터 뷰어 UI 업데이트.
            SetUnitStatDataOnUnitViewer(SelectedUnitIvItemData, true);

            // 버튼 CG 업데이트.
            ButtonCgControll();
        }
    }

    void ChangeItemEquippedUnit(InventoryItemData tryEquippingInventoryItemData, string preEquippedUnitInstId, string tryEquippingUnitInstId)
    {
        // 새로운 캐릭터의 장비 변경으로 업데이트할 수 있는 데이터의 숫자를 넘기면 기존의 업데이트 내용을 먼저 업데이트.
        // ? 현재 로직상 경우 2개의 캐릭터 업데이트 데이터 추가. 한 아이템을 바꿔끼는 경우 서버 업데이트시, 두 캐릭터의 데이터가 짝을 이뤄야함.
        var beforeEqInfoBackupDict = ES3.Load(ES3EquipmentChangeLog.BeforeEquippedInfoDictionary.ToCachedUncamelCaseString(), new Dictionary<string, List<string>>(), ES3Settings.defaultSettings);
        if (beforeEqInfoBackupDict.Count + 2 > maxUpdateCount)
            if (!beforeEqInfoBackupDict.ContainsKey((tryEquippingInventoryItemData.ExtraItemData as EquipmentStatModifierData).EquippedCharacterInstId) && !beforeEqInfoBackupDict.ContainsKey(tryEquippingUnitInstId))
                UpdateUnitEquipmentItem(true);

        // 기존 장착 유닛의 장비를 해제 후, 현재 유닛에게 장착.
        ChangeUnitEquippedItem(tryEquippingInventoryItemData, false, preEquippedUnitInstId);
        // ? 장비 해제의 경우, 장비 인벤토리 아이템 데이터를 수동으로 업데이트 해줘야함. 최소한의 CPU 사용을 위함.
        tryEquippingInventoryItemData.IsEquipped = false;
        (tryEquippingInventoryItemData.ExtraItemData as EquipmentStatModifierData).EquippedCharacterInstId = null;
        ChangeUnitEquippedItem(tryEquippingInventoryItemData, true, tryEquippingUnitInstId);
    }

    string GetTheOtherArmorItemEqInstId()
    {
        return SelectedCharacterStatData.EqInstIds[(CurSelectedEquipmentIndex + 1) % 2 == 0 ? 2 : 3];
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *          * Save Equipped Item Info To ES3 *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void SaveEquippedItemInfoChangeToEs3(ES3EquipmentChangeLog eS3EquipmentChangeLog, string unitInstanceId, List<string> equippedItemList)
    {
        // 유닛 장비 목록 데이터 백업을 불러옴.
        var equipInfoBackupDict = ES3.Load(eS3EquipmentChangeLog.ToCachedUncamelCaseString(), new Dictionary<string, List<string>>(), ES3Settings.defaultSettings);

        // 유닛 장비 목록 데이터 백업.
        if (eS3EquipmentChangeLog == ES3EquipmentChangeLog.BeforeEquippedInfoDictionary)
        {
            // ? 장비 목록 변경 전 데이터는 서버에 데이터를 보내기 이전 최초의 데이터만 필요함.
            if (!equipInfoBackupDict.ContainsKey(unitInstanceId))
                equipInfoBackupDict.Add(unitInstanceId, equippedItemList);
        }
        else
            equipInfoBackupDict[unitInstanceId] = equippedItemList;

        ES3.Save(eS3EquipmentChangeLog.ToCachedUncamelCaseString(), equipInfoBackupDict, ES3Settings.defaultSettings);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *       * Apply Unit Equipment Item Changed *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void ApplyNotYetUpdatedUnitEquipmentItemChangedData()
    {
        if (!ES3.KeyExists(ES3EquipmentChangeLog.AfterEquippedInfoDictionary.ToCachedUncamelCaseString()))
            return;

        // 기존에 저장된 캐릭터 장비 아이디 리스트 백업을 불러옴.
        var eqAfterChangeBackupDict = ES3.Load(ES3EquipmentChangeLog.AfterEquippedInfoDictionary.ToCachedUncamelCaseString(), new Dictionary<string, List<string>>(), ES3Settings.defaultSettings);

        // 변경된 장비 데이터 적용.
        foreach (var kvp in eqAfterChangeBackupDict)
        {
            var characterItemInstance = PlayFabManager.Instance.InventoryItemDictionary[PlayFabManager.CatalogItemClass.Unit][kvp.Key];
            characterItemInstance.CustomDataAddOrValueChange(nameof(CharacterStatData.EqInstIds), kvp.Value.SerializeObject());
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *      * Update Unit Equipment Item On Server *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Unit Setting Manager/USM/Update Unit Equipment Item"), Button("Update Unit Equipment Item", ButtonSizes.Gigantic, ButtonStyle.Box, Expanded = true), GUIColor(0.35f, 1, 0.7f)]
    public void UpdateUnitEquipmentItem(bool isForcedExcute = false)
    {
        // 업데이트할 목록이 없으면 리턴.
        if (!ES3.KeyExists(ES3EquipmentChangeLog.AfterEquippedInfoDictionary.ToCachedUncamelCaseString()))
        {
            // gameObject.Log($"업데이트할 장비 변경 목록이 없음");
            return;
        }

        // 비교용 변경전 데이터.
        var beforeEquipInfoBackupDict = ES3.Load(ES3EquipmentChangeLog.BeforeEquippedInfoDictionary.ToCachedUncamelCaseString(), new Dictionary<string, List<string>>(), ES3Settings.defaultSettings);
        var afterEquipInfoBackupDict = ES3.Load(ES3EquipmentChangeLog.AfterEquippedInfoDictionary.ToCachedUncamelCaseString(), new Dictionary<string, List<string>>(), ES3Settings.defaultSettings);

        // 강제로 서버에 업데이트 시키는게 아닌 경우, 최대 업데이트 갯수까지 대기.
        if (!isForcedExcute && afterEquipInfoBackupDict.Count < maxUpdateCount)
            return;

        // 변경사항 서버에 업로드.
        var requestData = new CloudScriptFunctionRequestData().SetUpdateUnitEquipmentRequestData(beforeEquipInfoBackupDict, afterEquipInfoBackupDict);
        if (requestData.FunctionParameter.Count > 0)
            PlayFabManager.Instance.ExecuteCloudScript(PlayFabManager.CloudScriptFunctionName.UpdateUnitEquipmentItem_C, requestData);

        // 서버 전송 후 백업 데이터 클리어.
        ES3.DeleteKey(ES3EquipmentChangeLog.BeforeEquippedInfoDictionary.ToCachedUncamelCaseString(), ES3Settings.defaultSettings);
        ES3.DeleteKey(ES3EquipmentChangeLog.AfterEquippedInfoDictionary.ToCachedUncamelCaseString(), ES3Settings.defaultSettings);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *        * Unit Inspector UGUI Click Event *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void OnUguiButtonClick()
    {
        // 아이템 정보 표시.
        if (SelectedUnitIvItemData != null)
            SelectedItemSpecInspector.UpdateInspectorUI(SelectedUnitIvItemData);

        // 장착 버튼 CG 초기화 및 장비 렉트 선택 이펙트 비활성화.
        ButtonCgControll();
        SetSelectedEquipmentIndexOnUnitEquipmentRectClick(-1);

        // 인벤토리 전환.
        OnUnitEquipmentRectClick(PlayFabManager.CatalogItemClass.Unit.ToCachedString());
    }

    /**
    * !--------------------------------------------------
    * !--------------------------------------------------
    *        * On Equipment Display Button Click *
    * ?--------------------------------------------------
    * ?--------------------------------------------------
*/

    public void OnUnitEquipmentRectClick(string itemClass)
    {
        if (preSelectedItemClassOrTag == itemClass && itemClass == PlayFabManager.CatalogItemClass.Weapon.ToCachedString())
            return;
        else
            preSelectedItemClassOrTag = itemClass;

        // 인벤토리 필터링
        lastInventoryFilteringData = new FilteringData
        {
            unionItemClassList = new List<PlayFabManager.CatalogItemClass> { itemClass.ToCachedEnum<PlayFabManager.CatalogItemClass>() },
        };

        // 무기 인벤토리를 띄우는 경우, 유닛이 장착 가능한 무기 타입 필터링.
        if (itemClass == PlayFabManager.CatalogItemClass.Weapon.ToCachedString())
            lastInventoryFilteringData.unionItemTagList = (SelectedUnitIvItemData.ExtraItemData as CharacterStatData).WeaponType;
        else if (itemClass == PlayFabManager.CatalogItemClass.Armor.ToCachedString())
        {
            // 현재 선택된 방어구 아이템 인덱스가 아닌 다른 인덱스에 장착한 방어구 제외.
            var theOtherEqInstId = GetTheOtherArmorItemEqInstId();
            if (!theOtherEqInstId.IsNullOrEmpty())
            {
                var theOtherEqItemInstance = PlayFabManager.Instance.InventoryItemDictionary[PlayFabManager.CatalogItemClass.Armor][theOtherEqInstId];
                lastInventoryFilteringData.exceptionItemTagList = new List<PlayFabManager.CatalogItemTag> { theOtherEqItemInstance.GetMainTag() };
            }
        }

        Inventory.Instance.OpenInventoryViewerWithFilting(Inventory.InventoryViewerName.LargeSize, lastInventoryFilteringData);
    }

    public void SetSelectedEquipmentIndexOnUnitEquipmentRectClick(int index)
    {
        equipmentRectSelectEffect.gameObject.SetActive(index >= 0);

        if (index >= 0)
        {
            CurSelectedEquipmentIndex = index;

            equipmentRectSelectEffect.SetParent(equippedItemPositionRectArr[index].transform);
            equipmentRectSelectEffect.anchoredPosition = Vector2.one * 0.5f;

            ChangeWeaponWithUpdateUnitSpecInspector(index);
        }
    }

    public void ButtonCgControll()
    {
        // equipCg.alpha = 0;
        equipCg.interactable = false;
        equipCg.blocksRaycasts = false;
        // unequipCg.alpha = 0;
        unequipCg.interactable = false;
        unequipCg.blocksRaycasts = false;

        // 세팅중인 유닛이 캐릭터이면서, 선택된 아이템이 캐릭터가 아닌 경우.
        if (SelectedUnitIvItemData != null && SelectedUnitIvItemData.CatalogItemClass == PlayFabManager.CatalogItemClass.Unit)
            if (SelectedItemSpecInspector.DisplayingInventoryItemData.CatalogItemMainTag != PlayFabManager.CatalogItemTag.Character)
            {
                {
                    // 장착 중인 아이템인 경우.
                    if (SelectedCharacterStatData.EqInstIds.Contains(SelectedItemSpecInspector.DisplayingInventoryItemData.ItemInstance.ItemInstanceId))
                    {
                        equipCg.alpha = 0;
                        unequipCg.alpha = 1;
                        unequipCg.interactable = true;
                        unequipCg.blocksRaycasts = true;
                    }
                    // 장착 중인 아이템이 아닌 경우.
                    else
                    {
                        unequipCg.alpha = 0;
                        equipCg.alpha = 1;
                        equipCg.interactable = true;
                        equipCg.blocksRaycasts = true;
                    }
                }
            }
            else
            {
                equipCg.alpha = 0;
                unequipCg.alpha = 0;
            }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *            * On Enhance Button Click *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void OnEnhanceButtonClick()
    {
        if (selectedItemSpecInspector.DisplayingInventoryItemData.CatalogItemClass == PlayFabManager.CatalogItemClass.Weapon || selectedItemSpecInspector.DisplayingInventoryItemData.CatalogItemClass == PlayFabManager.CatalogItemClass.Armor)
        {
            IsKeepCurrentState = true;
            keepInventoryViewerPosition = Inventory.Instance.CurOpendInventoryViewer.RecyclableScrollRect.normalizedPosition;
            keepSelectedUnitInstId = SelectedUnitIvItemData.Key;

            UpgradeManager.Instance.UpgradeItemDataFromUnitSetting = selectedItemSpecInspector.DisplayingInventoryItemData;
            GameManager.Instance.MainWindowManager.OpenWindow(NameManager.LobbyWindowName.Forge.ToCachedString());
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *       * On Unit Setting Manager Window Opend *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void OnUnitSettingWindowOpend()
    {
        if (IsKeepCurrentState)
        {
            IsKeepCurrentState = false;
            Inventory.Instance.OpenInventoryViewerWithFilting(Inventory.InventoryViewerName.LargeSize, lastInventoryFilteringData, false, () =>
            {
                Inventory.Instance.CurOpendInventoryViewer.RecyclableScrollRect.normalizedPosition = keepInventoryViewerPosition;
                var unitIvItem = Inventory.Instance.CurOpendInventoryViewer.RecyclableItemList.Where(item => item.ItemData.Key == keepSelectedUnitInstId).FirstOrDefault();
                unitIvItem.InvokeOnItemSelectAction();
            });
        }
        else
        {
            OnUnitEquipmentRectClick(PlayFabManager.CatalogItemClass.Unit.ToCachedString());

            Inventory.Instance.OpenInventoryViewerWithFilting(Inventory.InventoryViewerName.LargeSize, lastInventoryFilteringData, false, () =>
            {
                if (UnitPresetManager.Instance.IsKeepCurrentState)
                {
                    var unitIvItem = Inventory.Instance.CurOpendInventoryViewer.RecyclableItemList.Where(item => item.ItemData.Key == UnitPresetManager.Instance.KeepSelectedUnitInstId).FirstOrDefault();
                    if (unitIvItem != null)
                        unitIvItem.InvokeOnItemSelectAction();
                }
                else
                    Inventory.Instance.CurOpendInventoryViewer.RecyclableItemList[0].InvokeOnItemSelectAction();
            });
        }
    }
}

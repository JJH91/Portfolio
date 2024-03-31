using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kryz.CharacterStats;
using Sirenix.OdinInspector;

[Serializable]
public class UnitStatData : UnitBaseData
{
    public Dictionary<string, CharacterStat> StatDictionary { get; } = new Dictionary<string, CharacterStat>();

    public CharacterStat AtkStat { get; } = new CharacterStat(nameof(Atk));
    [ShowInInspector] public override float Atk { get => AtkStat.Value; set => AtkStat.BaseValue = value; }
    public CharacterStat DefStat { get; } = new CharacterStat(nameof(Def));
    [ShowInInspector] public override float Def { get => DefStat.Value; set => DefStat.BaseValue = value; }
    public CharacterStat CrtRateStat { get; } = new CharacterStat(0.05f, nameof(CrtRate_1));
    [ShowInInspector] public override float CrtRate_1 { get => CrtRateStat.Value; set => CrtRateStat.BaseValue = value; }
    public CharacterStat CrtDmgStat { get; } = new CharacterStat(1.5f, nameof(CrtDmg_0));
    [ShowInInspector] public override float CrtDmg_0 { get => CrtDmgStat.Value; set => CrtDmgStat.BaseValue = value; }

    public CharacterStat MaxHpStat { get; } = new CharacterStat(nameof(MaxHp));
    [ShowInInspector] public override float MaxHp { get => MaxHpStat.Value; set => MaxHpStat.BaseValue = value; }
    public CharacterStat MaxMpStat { get; } = new CharacterStat(nameof(MaxMp));
    [ShowInInspector] public override float MaxMp { get => MaxMpStat.Value; set => MaxMpStat.BaseValue = value; }
    public CharacterStat MpRecyStat { get; } = new CharacterStat(1f, nameof(MpRecy));
    [ShowInInspector] public override float MpRecy { get => MpRecyStat.Value; set => MpRecyStat.BaseValue = value; }
    public CharacterStat SpdStat { get; } = new CharacterStat(5f, nameof(Spd));
    [ShowInInspector] public override float Spd { get => SpdStat.Value; set => SpdStat.BaseValue = value; }

    public CharacterStat DetectRangeStat { get; } = new CharacterStat(nameof(DetectRange));
    [ShowInInspector] public override float DetectRange { get => DetectRangeStat.Value; set => DetectRangeStat.BaseValue = value; }

    public CharacterStat RpmStat { get; } = new CharacterStat(nameof(Rpm));
    [ShowInInspector] public override float Rpm { get => RpmStat.Value; set => RpmStat.BaseValue = value; }
    public CharacterStat Spread_180Stat { get; } = new CharacterStat(nameof(Spread_180));
    [ShowInInspector] public override float Spread_180 { get => Spread_180Stat.Value; set => Spread_180Stat.BaseValue = value; }
    public CharacterStat ProjSpdStat { get; } = new CharacterStat(nameof(ProjSpd_0));
    [ShowInInspector] public override float ProjSpd_0 { get => ProjSpdStat.Value; set => ProjSpdStat.BaseValue = value; }
    public CharacterStat ProjTimeStat { get; } = new CharacterStat(nameof(ProjTime_0));
    [ShowInInspector] public override float ProjTime_0 { get => ProjTimeStat.Value; set => ProjTimeStat.BaseValue = value; }

    public void AssembleUnitStatData(UnitBaseData customData, UnitBaseData catalogData, string name)
    {
        base.AssembleUnitBaseData(customData, catalogData, name);

        var properties = GetType().GetProperties();
        foreach (var pi in properties)
            if (pi.PropertyType == typeof(CharacterStat))
                StatDictionary.AddStatData(pi.GetValue(this, null) as CharacterStat);
    }

    public void AddStatModifierData(UnitStatModifierData unitStatModifierData)
    {
        foreach (var kvp in unitStatModifierData.StatModifierDictionary)
            foreach (var mod in kvp.Value)
                StatDictionary[kvp.Key].AddModifier(mod);
    }

    public void RemoveStatModifierData(UnitStatModifierData unitStatModifierData)
    {
        foreach (var kvp in unitStatModifierData.StatModifierDictionary)
            foreach (var mod in kvp.Value)
                StatDictionary[kvp.Key].RemoveModifier(mod);
    }

    public void RemoveAllStatModifierDataFromSource(object source)
    {
        foreach (var kvp in StatDictionary)
            kvp.Value.RemoveAllModifiersFromSource(source);
    }

    public void RemoveAllStatModifierData()
    {
        foreach (var kvp in StatDictionary)
            kvp.Value.RemoveAllModifiers();
    }

    [Button("Check Data"), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
    public void CheckData()
    {
        Debug.Log($"----- Check Stat Data -----");
        foreach (var kvp in StatDictionary)
        {
            Debug.Log($"Key: {kvp.Key}, Value: {kvp.Value.StatKey}, {kvp.Value.BaseValue}, {kvp.Value.Value}");
            foreach (var mod in kvp.Value.StatModifiers)
                Debug.Log($"Key: {kvp.Key}, Mod: {mod.StatKey}, {mod.Type}, {mod.Value}, {mod.Source}");
        }
    }
}

[Serializable]
public class CharacterStatData : UnitStatData
{
    [ShowInInspector] public List<CraftMaterialData> CraftMaterialDataList { get; set; } = new List<CraftMaterialData>();
    [ShowInInspector] public List<PlayFabManager.CatalogItemTag> WeaponType { get; set; } = new List<PlayFabManager.CatalogItemTag>();
    [ShowInInspector] public List<string> EqInstIds { get; set; } = new List<string>();
    [ShowInInspector] public string EqWeaponInstId_0 { get; set; }
    [ShowInInspector] public string EqWeaponInstId_1 { get; set; }
    [ShowInInspector] public string EqArmorInstId_0 { get; set; }
    [ShowInInspector] public string EqArmorInstId_1 { get; set; }
    // [ShowInInspector] public string EqAccInstId { get; set; }

    // 회피 쿨타임
    public CharacterStat DodgeCdStat { get; } = new CharacterStat(nameof(DodgeCd));
    [ShowInInspector] public float DodgeCd { get; set; } = 5;

    public CharacterStatData AssembleCharacterStatData(CharacterStatData customData, CharacterStatData catalogData, string name)
    {
        EqInstIds = customData.EqInstIds;
        // 장비의 인덱스를 맞춰줌.
        // ? 서버 업데이트시, 100바이트 제한으로 최대 5개의 아이디 값 적용 가능.
        var count = 5 - EqInstIds.Count;
        for (int i = 0; i < count; i++)
            EqInstIds.Add(null);

        EqWeaponInstId_0 = EqInstIds[0] ?? customData.EqWeaponInstId_0;
        EqWeaponInstId_0 ??= PlayFabManager.CatalogItemTag.Temp_Weapon.ToCachedString().ReplaceUnderScoreToSpace();
        EqWeaponInstId_1 = EqInstIds[1] ?? customData.EqWeaponInstId_1;
        EqWeaponInstId_1 ??= PlayFabManager.CatalogItemTag.Temp_Weapon.ToCachedString().ReplaceUnderScoreToSpace();
        EqArmorInstId_0 = EqInstIds[2] ?? customData.EqArmorInstId_0;
        EqArmorInstId_1 = EqInstIds[3] ?? customData.EqArmorInstId_1;
        // EqAccInstId = EqInstIdList[4] ?? customData.EqAccInstId;

        WeaponType = catalogData.WeaponType;
        DodgeCd = catalogData.DodgeCd;

        base.AssembleUnitStatData(customData, catalogData, name);

        return this;
    }

    public void ApplyAllEquipmentStatModifierData(int weaponIndex, Weapon weapon = null)
    {
        // 이전에 장착한 아이템 데이터 제거.
        RemoveAllStatModifierData();

        // 장비한 아이템 데이터 조립 및 적용.
        var weaponInstId = weaponIndex == 0 ? EqWeaponInstId_0 : EqWeaponInstId_1;
        var weaponStatModifierData = CombatManager.Instance != null ?
                                    CombatManager.Instance.WeaponStatModDataDictionary.GetOrAdd(weaponInstId, weaponInstId.GetAssembledWeaponStatModifierData())
                                    : weaponInstId.GetAssembledWeaponStatModifierData();

        var armorStatModifierData_0 = CombatManager.Instance != null ?
                                    CombatManager.Instance.EquipmentStatModDataDictionary.GetOrAdd(EqArmorInstId_0.IsNullOrEmpty() ? PlayFabManager.CatalogItemTag.None.ToCachedString() : EqArmorInstId_0, EqArmorInstId_0.GetAssembledEquipmentStatModifierData())
                                    : EqArmorInstId_0.GetAssembledEquipmentStatModifierData();
        var armorStatModifierData_1 = CombatManager.Instance != null ?
                                    CombatManager.Instance.EquipmentStatModDataDictionary.GetOrAdd(EqArmorInstId_1.IsNullOrEmpty() ? PlayFabManager.CatalogItemTag.None.ToCachedString() : EqArmorInstId_1, EqArmorInstId_1.GetAssembledEquipmentStatModifierData())
                                    : EqArmorInstId_1.GetAssembledEquipmentStatModifierData();


        AddStatModifierData(weaponStatModifierData);
        AddStatModifierData(armorStatModifierData_0);
        AddStatModifierData(armorStatModifierData_1);

        Dps = this.GetDPS();
        Tough = this.GetToughness();

        if (weapon != null)
            weapon.WeaponStatModData = weaponStatModifierData;
    }

    public void ChangeAppliedWeaponStatModifierData(int weaponIndex)
    {
        // 이전에 장착한 아이템 데이터 제거.
        RemoveAllStatModifierDataFromSource(EqWeaponInstId_0);
        RemoveAllStatModifierDataFromSource(EqWeaponInstId_1);

        var weaponInstId = weaponIndex == 0 ? EqWeaponInstId_0 : EqWeaponInstId_1;
        var weaponStatModifierData = CombatManager.Instance != null ?
                                    CombatManager.Instance.WeaponStatModDataDictionary.GetOrAdd(weaponInstId, weaponInstId.GetAssembledWeaponStatModifierData())
                                    : weaponInstId.GetAssembledWeaponStatModifierData();

        // 바뀐 아이템 데이터 적용.
        AddStatModifierData(weaponStatModifierData);

        Dps = this.GetDPS();
        Tough = this.GetToughness();
    }

    public List<SkillData> GetAllSkillDataList()
    {
        var resultList = new List<SkillData>();

        // 장비한 아이템 데이터 조립 및 스킬 추가.
        // TODO: 2개의 무기를 활용할 생각이면, 무기 인덱스에 따라 사용 가능 불가능을 판별하는 코드 추가가 필요함.
        var weaponStatModifierData_0 = CombatManager.Instance != null ?
                                    CombatManager.Instance.WeaponStatModDataDictionary.GetOrAdd(EqWeaponInstId_0, EqWeaponInstId_0.GetAssembledWeaponStatModifierData())
                                    : EqWeaponInstId_0.GetAssembledWeaponStatModifierData();
        var weaponStatModifierData_1 = CombatManager.Instance != null ?
                                    CombatManager.Instance.WeaponStatModDataDictionary.GetOrAdd(EqWeaponInstId_1, EqWeaponInstId_1.GetAssembledWeaponStatModifierData())
                                    : EqWeaponInstId_1.GetAssembledWeaponStatModifierData();

        if (weaponStatModifierData_0.EquipmentSkillData != null)
            resultList.Add(weaponStatModifierData_0.EquipmentSkillData);
        if (weaponStatModifierData_1.EquipmentSkillData != null)
            resultList.Add(weaponStatModifierData_1.EquipmentSkillData);

        var armorStatModifierData_0 = CombatManager.Instance != null ?
                                    CombatManager.Instance.EquipmentStatModDataDictionary.GetOrAdd(EqArmorInstId_0.IsNullOrEmpty() ? PlayFabManager.CatalogItemTag.None.ToCachedString() : EqArmorInstId_0, EqArmorInstId_0.GetAssembledEquipmentStatModifierData())
                                    : EqArmorInstId_0.GetAssembledEquipmentStatModifierData();
        var armorStatModifierData_1 = CombatManager.Instance != null ?
                                    CombatManager.Instance.EquipmentStatModDataDictionary.GetOrAdd(EqArmorInstId_1.IsNullOrEmpty() ? PlayFabManager.CatalogItemTag.None.ToCachedString() : EqArmorInstId_1, EqArmorInstId_1.GetAssembledEquipmentStatModifierData())
                                    : EqArmorInstId_1.GetAssembledEquipmentStatModifierData();

        if (armorStatModifierData_0.EquipmentSkillData != null)
            resultList.Add(armorStatModifierData_0.EquipmentSkillData);
        if (armorStatModifierData_1.EquipmentSkillData != null)
            resultList.Add(armorStatModifierData_1.EquipmentSkillData);

        // 캐릭터 전용 스킬 추가.
        if (SkillDataList != null)
            resultList.AddRange(SkillDataList);

        // 회피 스킬 추가.
        if (PlayFabManager.Instance.MainCatalogItemDictionary[CatalogItemId].GetMainTag() == PlayFabManager.CatalogItemTag.Player)
        {
            var dodgeSkillData = PlayFabManager.Instance.SkillCatalogItemDictionary[Character.CharacterUnitSkillKind.Dodge.ToCachedString()].CustomData.DeserializeObject<SkillData>();
            dodgeSkillData.IsExceptOnRandomPick = true;
            dodgeSkillData.StdCd = DodgeCd;

            resultList.Add(dodgeSkillData);
        }

        return resultList;
    }
}

[Serializable]
public class MonsterStatData : UnitStatData
{
    // ? 기존 랭크 별 유닛의 스킬 데이터 목록을 작성, 입력하는 것보다 랭크별 장비 차이를 통한 스킬 추가와 난이도 수정이 가능함.
    [ShowInInspector] public List<string> EqCatalogIds { get; set; } = new List<string>();
    [ShowInInspector] public string EqWeaponCatalogId { get; set; }
    [ShowInInspector] public string EqArmorCatalogId_0 { get; set; }
    [ShowInInspector] public string EqArmorCatalogId_1 { get; set; }
    // [ShowInInspector] public string EqAccCatalogId { get; set; }

    // 공통 몬스터 데이터 조립
    public MonsterStatData AssembleMonsterData(MonsterStatData customData, MonsterStatData catalogData, string name)
    {
        EqCatalogIds = catalogData.EqCatalogIds;
        // 장비의 인덱스를 맞춰줌.
        // ? 서버 업데이트시, 100바이트 제한으로 최대 5개의 아이디 값 적용 가능.
        var count = 4 - EqCatalogIds.Count;
        for (int i = 0; i < count; i++)
            EqCatalogIds.Add(null);

        EqWeaponCatalogId = EqCatalogIds[0] ?? catalogData.EqWeaponCatalogId;
        EqWeaponCatalogId ??= PlayFabManager.CatalogItemTag.Temp_Weapon.ToCachedString().ReplaceUnderScoreToSpace();
        EqArmorCatalogId_0 = EqCatalogIds[2] ?? catalogData.EqArmorCatalogId_0;
        EqArmorCatalogId_1 = EqCatalogIds[3] ?? catalogData.EqArmorCatalogId_1;
        // EqAccCatalogId = EqCatalogIds[4] ?? catalogData.EqAccCatalogId;

        base.AssembleUnitStatData(customData, catalogData, name);

        return this;
    }

    public MonsterStatData AssembleMonsterStatData(MonsterStatData catalogData, string name, int level)
    {
        catalogData.Lv = level;

        return AssembleMonsterData(catalogData, catalogData, name);
    }

    public void ApplyAllEquipmentStatModifierData(Weapon weapon = null)
    {
        // 이전에 장착한 아이템 데이터 제거.
        RemoveAllStatModifierData();

        // 장비한 아이템 데이터 조립 및 적용.
        var weaponStatModifierData = CombatManager.Instance != null ?
                                    CombatManager.Instance.WeaponStatModDataDictionary.GetOrAdd(EqWeaponCatalogId, EqWeaponCatalogId.GetAssembledWeaponStatModifierData())
                                    : EqWeaponCatalogId.GetAssembledWeaponStatModifierData();

        var armorStatModifierData_0 = CombatManager.Instance != null ?
                                            CombatManager.Instance.EquipmentStatModDataDictionary.GetOrAdd(EqArmorCatalogId_0.IsNullOrEmpty() ? PlayFabManager.CatalogItemTag.None.ToCachedString() : EqArmorCatalogId_0, EqArmorCatalogId_0.GetAssembledEquipmentStatModifierData())
                                            : EqArmorCatalogId_0.GetAssembledEquipmentStatModifierData();
        var armorStatModifierData_1 = CombatManager.Instance != null ?
                                    CombatManager.Instance.EquipmentStatModDataDictionary.GetOrAdd(EqArmorCatalogId_1.IsNullOrEmpty() ? PlayFabManager.CatalogItemTag.None.ToCachedString() : EqArmorCatalogId_1, EqArmorCatalogId_1.GetAssembledEquipmentStatModifierData())
                                    : EqArmorCatalogId_1.GetAssembledEquipmentStatModifierData();

        AddStatModifierData(weaponStatModifierData);
        AddStatModifierData(armorStatModifierData_0);
        AddStatModifierData(armorStatModifierData_1);

        Dps = this.GetDPS();
        Tough = this.GetToughness();

        if (weapon != null)
            weapon.WeaponStatModData = weaponStatModifierData;
    }

    public List<SkillData> GetAllSkillDataList()
    {
        // 캐릭터 전용 스킬 추가.
        var resultList = new List<SkillData>();
        if (SkillDataList != null)
            resultList.AddRange(SkillDataList);

        // 장비한 아이템 데이터 조립 및 스킬 추가.
        var weaponStatModifierData = CombatManager.Instance != null ?
                                     CombatManager.Instance.WeaponStatModDataDictionary.GetOrAdd(EqWeaponCatalogId, EqWeaponCatalogId.GetAssembledWeaponStatModifierData())
                                     : EqWeaponCatalogId.GetAssembledWeaponStatModifierData();

        if (weaponStatModifierData.EquipmentSkillData != null)
            resultList.Add(weaponStatModifierData.EquipmentSkillData);

        var armorStatModifierData_0 = CombatManager.Instance != null ?
                                    CombatManager.Instance.EquipmentStatModDataDictionary.GetOrAdd(EqArmorCatalogId_0.IsNullOrEmpty() ? PlayFabManager.CatalogItemTag.None.ToCachedString() : EqArmorCatalogId_0, EqArmorCatalogId_0.GetAssembledEquipmentStatModifierData())
                                    : EqArmorCatalogId_0.GetAssembledEquipmentStatModifierData();
        var armorStatModifierData_1 = CombatManager.Instance != null ?
                                    CombatManager.Instance.EquipmentStatModDataDictionary.GetOrAdd(EqArmorCatalogId_1.IsNullOrEmpty() ? PlayFabManager.CatalogItemTag.None.ToCachedString() : EqArmorCatalogId_1, EqArmorCatalogId_1.GetAssembledEquipmentStatModifierData())
                                    : EqArmorCatalogId_1.GetAssembledEquipmentStatModifierData();

        if (armorStatModifierData_0.EquipmentSkillData != null)
            resultList.Add(armorStatModifierData_0.EquipmentSkillData);
        if (armorStatModifierData_1.EquipmentSkillData != null)
            resultList.Add(armorStatModifierData_1.EquipmentSkillData);

        return resultList;
    }
}
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kryz.CharacterStats;
using Sirenix.OdinInspector;

[Serializable]
public class UnitStatModifierData : ExtendedUnitBaseData
{
    [ShowInInspector] public object StatModifierSource { get; set; }
    public Dictionary<string, List<StatModifier>> StatModifierDictionary { get; } = new Dictionary<string, List<StatModifier>>();

    public StatModifier AtkMod { get; } = new StatModifier(StatModType.Flat, nameof(Atk));
    [ShowInInspector] public override float Atk { get => AtkMod.Value; set => AtkMod.Value = value; }
    public StatModifier DefMod { get; } = new StatModifier(StatModType.Flat, nameof(Def));
    [ShowInInspector] public override float Def { get => DefMod.Value; set => DefMod.Value = value; }
    public StatModifier CrtRate_1Mod { get; } = new StatModifier(StatModType.Flat, nameof(CrtRate_1));
    [ShowInInspector] public override float CrtRate_1 { get => CrtRate_1Mod.Value; set => CrtRate_1Mod.Value = value; }
    public StatModifier CrtDmg_0Mod { get; } = new StatModifier(StatModType.Flat, nameof(CrtDmg_0));
    [ShowInInspector] public override float CrtDmg_0 { get => CrtDmg_0Mod.Value; set => CrtDmg_0Mod.Value = value; }

    public StatModifier MaxHpMod { get; } = new StatModifier(StatModType.Flat, nameof(MaxHp));
    [ShowInInspector] public override float MaxHp { get => MaxHpMod.Value; set => MaxHpMod.Value = value; }
    public StatModifier MaxMpMod { get; } = new StatModifier(StatModType.Flat, nameof(MaxMp));
    [ShowInInspector] public override float MaxMp { get => MaxMpMod.Value; set => MaxMpMod.Value = value; }
    public StatModifier MpRecyMod { get; } = new StatModifier(StatModType.Flat, nameof(MpRecy));
    [ShowInInspector] public override float MpRecy { get => MpRecyMod.Value; set => MpRecyMod.Value = value; }
    public StatModifier SpdMod { get; } = new StatModifier(StatModType.Flat, nameof(Spd));
    [ShowInInspector] public override float Spd { get => SpdMod.Value; set => SpdMod.Value = value; }

    public StatModifier DetectRangeMod { get; } = new StatModifier(StatModType.Flat, nameof(DetectRange));
    [ShowInInspector] public override float DetectRange { get => DetectRangeMod.Value; set => DetectRangeMod.Value = value; }

    public StatModifier RpmMod { get; } = new StatModifier(StatModType.Flat, nameof(Rpm));
    [ShowInInspector] public override float Rpm { get => RpmMod.Value; set => RpmMod.Value = value; }
    public StatModifier Spread_180Mod { get; } = new StatModifier(StatModType.Flat, nameof(Spread_180));
    [ShowInInspector] public override float Spread_180 { get => Spread_180Mod.Value; set => Spread_180Mod.Value = value; }
    public StatModifier ProjSpd_0Mod { get; } = new StatModifier(StatModType.Flat, nameof(ProjSpd_0));
    [ShowInInspector] public override float ProjSpd_0 { get => ProjSpd_0Mod.Value; set => ProjSpd_0Mod.Value = value; }
    public StatModifier ProjTime_0Mod { get; } = new StatModifier(StatModType.Flat, nameof(ProjTime_0));
    [ShowInInspector] public override float ProjTime_0 { get => ProjTime_0Mod.Value; set => ProjTime_0Mod.Value = value; }

    public StatModifier AtkXMod { get; } = new StatModifier(StatModType.PercentAdd, nameof(Atk));
    [ShowInInspector] public override float AtkX_0 { get => AtkXMod.Value; set => AtkXMod.Value = value; }
    public StatModifier DefXMod { get; } = new StatModifier(StatModType.PercentAdd, nameof(Def));
    [ShowInInspector] public override float DefX_0 { get => DefXMod.Value; set => DefXMod.Value = value; }
    public StatModifier CrtRateXMod { get; } = new StatModifier(StatModType.PercentAdd, nameof(CrtRate_1));
    [ShowInInspector] public override float CrtRateX_0 { get => CrtRateXMod.Value; set => CrtRateXMod.Value = value; }
    public StatModifier CrtDmgXMod { get; } = new StatModifier(StatModType.PercentAdd, nameof(CrtDmg_0));
    [ShowInInspector] public override float CrtDmgX_0 { get => CrtDmgXMod.Value; set => CrtDmgXMod.Value = value; }

    public StatModifier MaxHpXMod { get; } = new StatModifier(StatModType.PercentAdd, nameof(MaxHp));
    [ShowInInspector] public override float MaxHpX_0 { get => MaxHpXMod.Value; set => MaxHpXMod.Value = value; }
    public StatModifier MaxMpXMod { get; } = new StatModifier(StatModType.PercentAdd, nameof(MaxMp));
    [ShowInInspector] public override float MaxMpX_0 { get => MaxMpXMod.Value; set => MaxMpXMod.Value = value; }
    public StatModifier MpRecyXMod { get; } = new StatModifier(StatModType.PercentAdd, nameof(MpRecy));
    [ShowInInspector] public override float MpRecyX_0 { get => MpRecyXMod.Value; set => MpRecyXMod.Value = value; }
    public StatModifier SpdXMod { get; } = new StatModifier(StatModType.PercentAdd, nameof(Spd));
    [ShowInInspector] public override float SpdX_0 { get => SpdXMod.Value; set => SpdXMod.Value = value; }

    public StatModifier RpmXMod { get; } = new StatModifier(StatModType.PercentAdd, nameof(Rpm));
    [ShowInInspector] public override float RpmX_0 { get => RpmXMod.Value; set => RpmXMod.Value = value; }
    public StatModifier SpreadXMod { get; } = new StatModifier(StatModType.PercentAdd, nameof(Spread_180));
    [ShowInInspector] public override float SpreadX_0 { get => SpreadXMod.Value; set => SpreadXMod.Value = value; }
    public StatModifier ProjSpdXMod { get; } = new StatModifier(StatModType.PercentAdd, nameof(ProjSpd_0));
    [ShowInInspector] public override float ProjSpdX_0 { get => ProjSpdXMod.Value; set => ProjSpdXMod.Value = value; }
    public StatModifier ProjTimeXMod { get; } = new StatModifier(StatModType.PercentAdd, nameof(ProjTime_0));
    [ShowInInspector] public override float ProjTimeX_0 { get => ProjTimeXMod.Value; set => ProjTimeXMod.Value = value; }

    public void AssembleUnitStatModifierData(UnitBaseData customData, UnitBaseData catalogData, string name, object source)
    {
        StatModifierSource = source;

        base.AssembleUnitBaseData(customData, catalogData, name);

        var properties = GetType().GetProperties();
        foreach (var pi in properties)
            if (pi.PropertyType == typeof(StatModifier))
            {
                var mod = pi.GetValue(this, null) as StatModifier;
                mod.SetSource(source);
                StatModifierDictionary.AddStatModifierData(mod);
            }
    }

    public void AssembleUnitStatModifierData(UnitBaseData customData, UnitBaseData catalogData, string name)
    {
        AssembleUnitStatModifierData(customData, catalogData, name, name);
    }

    // public void SetSource(object source)
    // {
    //     StatModifierSource = source;

    //     foreach (var kvp in StatModifierDictionary)
    //         foreach (var mod in kvp.Value)
    //             mod.SetSource(source);
    // }

    [Button("Check Data"), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
    public void CheckData()
    {
        Debug.Log($"----- Check Stat Mod Data -----");
        foreach (var kvp in StatModifierDictionary)
            foreach (var mod in kvp.Value)
                Debug.Log($"Key: {kvp.Key}, Mod: {mod.StatKey}, {mod.Type}, {mod.Value}, {mod.Source}");
    }
}

[Serializable]
public class EquipmentStatModifierData : UnitStatModifierData
{
    // 장비 타입, 데아터 조립시에 카탈로그 아이템 또는 인스턴스 아이템 값에 직접 입력해줘야함. 입력방법은 서버상의 클래스 값을 enum화 해서 입력.
    /// <summary>
    /// Item type.
    /// This value must manually input server item class value in assembly method variable which catalog item or instance item.
    /// </summary>
    [ShowInInspector] public PlayFabManager.CatalogItemTag EquipmentType { get; set; }

    // 인챈트, prefix, suffix
    [ShowInInspector] public string[] Enchantments { get; set; } = new string[2];

    // 인팬트 값 배율, 최소~최대, 각 10개의 값
    [ShowInInspector] public float[][] EnchantFactors { get; set; } = new float[2][];

    // 아이템에 적용할 인챈트 데이터
    [ShowInInspector] public EnchantStatModifierData[] EnchantmentsStatModifierData = new EnchantStatModifierData[2];

    // 장비 스킬 데이터
    [ShowInInspector] public SkillData EquipmentSkillData { get; set; }

    // 아이템 잠금
    [ShowInInspector] public bool IsLock { get; set; }

    // 장착 중인 캐릭터 아이디
    // ? 오프라인에서 장비를 장착중인 캐릭터 아이디를 할당함.
    [ShowInInspector] public string EquippedCharacterInstId { get; set; }

    public EquipmentStatModifierData AssembleEquipmentStatModifierData(EquipmentStatModifierData customData, EquipmentStatModifierData catalogData, string name, object source)
    {
        EquipmentType = catalogData.EquipmentType;

        AtkX_0 = Math.Max(customData.AtkX_0, catalogData.AtkX_0);
        DefX_0 = Math.Max(customData.DefX_0, catalogData.DefX_0);
        CrtRateX_0 = Math.Max(customData.CrtRateX_0, catalogData.CrtRateX_0);
        CrtDmgX_0 = Math.Max(customData.CrtDmgX_0, catalogData.CrtDmgX_0);

        MaxHpX_0 = Math.Max(customData.MaxHpX_0, catalogData.MaxHpX_0);
        MaxMpX_0 = Math.Max(customData.MaxMpX_0, catalogData.MaxMpX_0);
        MpRecyX_0 = Math.Max(customData.MpRecyX_0, catalogData.MpRecyX_0);
        SpdX_0 = Math.Max(customData.SpdX_0, catalogData.SpdX_0);

        RpmX_0 = Math.Max(customData.RpmX_0, catalogData.RpmX_0);
        SpreadX_0 = Math.Max(customData.SpreadX_0, catalogData.SpreadX_0);
        ProjSpdX_0 = Math.Max(customData.ProjSpdX_0, catalogData.ProjSpdX_0);
        ProjTimeX_0 = Math.Max(customData.ProjTimeX_0, catalogData.ProjTimeX_0);

        Enchantments = customData.Enchantments;
        EnchantFactors = customData.EnchantFactors;

        base.AssembleUnitStatModifierData(customData, catalogData, name, source);

        // Assemble enchantments data.
        if (Enchantments != null && Enchantments.Length == 2)
        {
            for (int i = 0; i < Enchantments.Length; i++)
                EnchantmentsStatModifierData[i] = Enchantments[i].IsNullOrEmpty() ? null : PlayFabManager.Instance.MainCatalogItemDictionary[Enchantments[i]].GetAssembledEnchantStatModifierData(source, EnchantFactors[i]);

            // Aplly enchantments affix to item name.
            // ? 영어와 한글의 순서가 다름
            // Name = $"{(Enchantments[0].IsNullOrEmpty() ? string.Empty : $"{enchantmentsData[0].Name} ")}{name}{(Enchantments[1].IsNullOrEmpty() ? string.Empty : $" {enchantmentsData[1].Name}")}";
            Name = $"{(Enchantments[0].IsNullOrEmpty() ? string.Empty : EnchantmentsStatModifierData[0].Name)} {(Enchantments[1].IsNullOrEmpty() ? string.Empty : EnchantmentsStatModifierData[1].Name)} {name}";
        }

        // Apply enchantments data to this modifier data.
        foreach (var data in EnchantmentsStatModifierData)
            if (data != null)
                ApplyEnchantStatModifierData(data);


        // 스킬 데이터 조립
        // 장비의 스킬은 ItemInstance CustomData 가 아닌 Catalog CustomData 에 있다.
        // 레벨값은 장비의 커스텀 데이터로부터 가져온다.
        if (catalogData.EquipmentSkillData != null && !catalogData.EquipmentSkillData.Id.IsNullOrEmpty())
        {
            int skillLv = 1;
            if (customData.EquipmentSkillData != null && customData.EquipmentSkillData.Lv > 1)
                skillLv = customData.EquipmentSkillData.Lv;
            EquipmentSkillData = catalogData.EquipmentSkillData.GetAssembledSkillData(skillLv, DragonBonesUnit.DragonBonesUnitType.Character);
        }

        IsLock = customData.IsLock;
        EquippedCharacterInstId = customData.EquippedCharacterInstId;

        return this;
    }

    protected void ApplyEnchantStatModifierData(EnchantStatModifierData enchantStatModifierData)
    {
        // ? 프로퍼티 이름, 프로퍼티로 이루어진 사전을 만들고, 같은 이름의 프로퍼티 값을 합산.
        var equipmentStatModifierPropertyDict = GetType().GetProperties().Where(pi => pi.PropertyType == typeof(StatModifier)).Select(pi => new KeyValuePair<string, StatModifier>(pi.Name, pi.GetValue(this, null) as StatModifier)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var enchantStatModifierPropertyDict = enchantStatModifierData.GetType().GetProperties().Where(pi => pi.PropertyType == typeof(StatModifier)).Select(pi => new KeyValuePair<string, StatModifier>(pi.Name, pi.GetValue(enchantStatModifierData, null) as StatModifier)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        foreach (var kvp in equipmentStatModifierPropertyDict)
            kvp.Value.Value += enchantStatModifierPropertyDict[kvp.Key].Value;
    }
}

[Serializable]
public class WeaponStatModifierData : EquipmentStatModifierData
{
    // 'One Hand' or 'Two Hand'.
    [ShowInInspector] public string GripType { get; set; }

    // Shotgun's bullet count.
    [ShowInInspector] public int Shot { get; set; }

    // 무기 탄환 스킬 리스트
    [ShowInInspector] public List<SkillData> BulletSkillDataList { get; set; } = new List<SkillData>();

    // 탄환 업그레이드 소켓
    [ShowInInspector] public int CurSoket { get; set; }
    [ShowInInspector] public int MaxSoket { get; set; }

    public WeaponStatModifierData AssembleWeaponStatModifierData(WeaponStatModifierData customData, WeaponStatModifierData catalogData, string name, object source)
    {
        CurSoket = customData.CurSoket;

        Shot = catalogData.Shot;

        MaxSoket = catalogData.MaxSoket;
        GripType = catalogData.GripType;

        // 탄환 스킬 생성
        if (Shot > 0)
        {
            // 기본 탄환 스킬 생성
            var normalBulletSkillData = PlayFabManager.Instance.SkillCatalogItemDictionary["Normal Bullet"].GetAssembledSkillData(AwakeningLv, DragonBonesUnit.DragonBonesUnitType.Character);

            BulletSkillDataList = new List<SkillData>() { normalBulletSkillData };

            // 카탈로그 커스텀 데이터의 스킬 추가
            if (catalogData.BulletSkillDataList != null)
                foreach (var catalogSkillData in catalogData.BulletSkillDataList)
                    if (BulletSkillDataList.Exists((skillData) => skillData.Id == catalogSkillData.Id))
                        BulletSkillDataList.Add(catalogSkillData.GetAssembledSkillData(AwakeningLv, DragonBonesUnit.DragonBonesUnitType.Character));

            // 아이템 커스텀 데이터의 스킬 추가
            if (customData.BulletSkillDataList != null)
                foreach (var instanceSkillData in customData.BulletSkillDataList)
                    BulletSkillDataList.Add(instanceSkillData.GetAssembledSkillData(AwakeningLv, DragonBonesUnit.DragonBonesUnitType.Character));
        }

        base.AssembleEquipmentStatModifierData(customData, catalogData, name, source);

        // 무기 아이템의 카탈로그 데이터에 기본적인 무기의 탄환 속도 및 시간이 설정되어있지 않으면, 기본값 1을 할당함.
        if (ProjSpd_0 == 0)
            ProjSpd_0 = 1;
        if (ProjTime_0 == 0)
            ProjTime_0 = 1;
        if (Rpm == 0)
            Rpm = 60f;

        // 무기 아이템 정보 표시용 DPS 계산.
        Dps = this.GetDPS() * Shot;

        return this;
    }
}

[Serializable]
public class EnchantStatModifierData : UnitStatModifierData
{
    // 접두, 접미
    public enum EnchantAffixEnum { Prefix, Suffix }
    [ShowInInspector] public EnchantAffixEnum EnchantAffix { get; set; }
    [ShowInInspector] public List<PlayFabManager.CatalogItemTag> EnchantableTypes { get; set; } = new List<PlayFabManager.CatalogItemTag>();

    // 아이템에 적용된 인챈트 랜덤값
    [ShowInInspector] float[] enchantFactors = new float[10];
    [ShowInInspector] int factorIndex;

    // 능력치 추가 비율 범위
    [ShowInInspector] public float[] AtkRng { get; set; } = new float[2];
    [ShowInInspector] public float[] DefRng { get; set; } = new float[2];
    [ShowInInspector] public float[] CrtRateRng_1 { get; set; } = new float[2];
    [ShowInInspector] public float[] CrtDmgRng_0 { get; set; } = new float[2];

    [ShowInInspector] public float[] MaxHpRng { get; set; } = new float[2];
    [ShowInInspector] public float[] MaxMpRng { get; set; } = new float[2];
    [ShowInInspector] public float[] MpRecyRng { get; set; } = new float[2];
    [ShowInInspector] public float[] MaxSpdRng { get; set; } = new float[2];

    [ShowInInspector] public float[] RpmRng { get; set; } = new float[2];
    [ShowInInspector] public float[] SpreadRng_180 { get; set; } = new float[2];
    [ShowInInspector] public float[] ProjSpdRng_0 { get; set; } = new float[2];
    [ShowInInspector] public float[] ProjTimeRng_0 { get; set; } = new float[2];

    [ShowInInspector] public float[] AtkXRng_0 { get; set; } = new float[2];
    [ShowInInspector] public float[] DefXRng_0 { get; set; } = new float[2];
    [ShowInInspector] public float[] CrtRateXRng_0 { get; set; } = new float[2];
    [ShowInInspector] public float[] CrtDmgXRng_0 { get; set; } = new float[2];

    [ShowInInspector] public float[] MaxHpXRng_0 { get; set; } = new float[2];
    [ShowInInspector] public float[] MaxMpXRng_0 { get; set; } = new float[2];
    [ShowInInspector] public float[] MpRecyXRng_0 { get; set; } = new float[2];
    [ShowInInspector] public float[] MaxSpdXRng_0 { get; set; } = new float[2];

    [ShowInInspector] public float[] RpmXRng_0 { get; set; } = new float[2];
    [ShowInInspector] public float[] SpreadXRng_0 { get; set; } = new float[2];
    [ShowInInspector] public float[] ProjSpdXRng_0 { get; set; } = new float[2];
    [ShowInInspector] public float[] ProjTimeXRng_0 { get; set; } = new float[2];

    public EnchantStatModifierData AssembleEnchantStatModifierData(EnchantStatModifierData customData, EnchantStatModifierData catalogData, string name, object source)
    {
        EnchantAffix = catalogData.EnchantAffix;
        EnchantableTypes = catalogData.EnchantableTypes;

        AtkRng = catalogData.AtkRng;
        DefRng = catalogData.DefRng;
        CrtRateRng_1 = catalogData.CrtRateRng_1;
        CrtDmgRng_0 = catalogData.CrtDmgRng_0;

        MaxHpRng = catalogData.MaxHpRng;
        MaxMpRng = catalogData.MaxMpRng;
        MpRecyRng = catalogData.MpRecyRng;
        MaxSpdRng = catalogData.MaxSpdRng;

        AtkXRng_0 = catalogData.AtkXRng_0;
        DefXRng_0 = catalogData.DefXRng_0;
        CrtRateXRng_0 = catalogData.CrtDmgXRng_0;
        CrtDmgXRng_0 = catalogData.CrtDmgXRng_0;

        MaxHpXRng_0 = catalogData.MaxHpXRng_0;
        MaxMpXRng_0 = catalogData.MaxMpXRng_0;
        MpRecyXRng_0 = catalogData.MpRecyXRng_0;
        MaxSpdXRng_0 = catalogData.MaxSpdXRng_0;

        RpmXRng_0 = catalogData.RpmXRng_0;
        SpreadXRng_0 = catalogData.SpreadXRng_0;
        ProjSpdXRng_0 = catalogData.ProjSpdXRng_0;
        ProjTimeXRng_0 = catalogData.ProjTimeXRng_0;

        base.AssembleUnitStatModifierData(customData, catalogData, name, source);

        return this;
    }

    public void ApplyEnchantFactors(float[] enchantFactorsFromEquipment)
    {
        this.enchantFactors = enchantFactorsFromEquipment;
        factorIndex = 0;

        Atk = ApplyValuebyRangeWithEnchantFactor(AtkRng);
        Def = ApplyValuebyRangeWithEnchantFactor(DefRng);
        CrtRate_1 = ApplyValuebyRangeWithEnchantFactor(CrtRateRng_1);
        CrtDmg_0 = ApplyValuebyRangeWithEnchantFactor(CrtDmgRng_0);

        MaxHp = ApplyValuebyRangeWithEnchantFactor(MaxHpRng);
        MaxMp = ApplyValuebyRangeWithEnchantFactor(MaxMpRng);
        MpRecy = ApplyValuebyRangeWithEnchantFactor(MpRecyRng);
        Spd = ApplyValuebyRangeWithEnchantFactor(MaxSpdRng);

        Rpm = ApplyValuebyRangeWithEnchantFactor(RpmRng);
        Spread_180 = ApplyValuebyRangeWithEnchantFactor(SpreadRng_180);
        ProjSpd_0 = ApplyValuebyRangeWithEnchantFactor(ProjSpdRng_0);
        ProjTime_0 = ApplyValuebyRangeWithEnchantFactor(ProjTimeRng_0);

        AtkX_0 = ApplyValuebyRangeWithEnchantFactor(AtkXRng_0);
        DefX_0 = ApplyValuebyRangeWithEnchantFactor(DefXRng_0);
        CrtRateX_0 = ApplyValuebyRangeWithEnchantFactor(CrtRateXRng_0);
        CrtDmgX_0 = ApplyValuebyRangeWithEnchantFactor(CrtDmgXRng_0);

        MaxHpX_0 = ApplyValuebyRangeWithEnchantFactor(MaxHpXRng_0);
        MaxMpX_0 = ApplyValuebyRangeWithEnchantFactor(MaxMpXRng_0);
        MpRecyX_0 = ApplyValuebyRangeWithEnchantFactor(MpRecyXRng_0);
        SpdX_0 = ApplyValuebyRangeWithEnchantFactor(MaxSpdXRng_0);

        RpmX_0 = ApplyValuebyRangeWithEnchantFactor(RpmXRng_0);
        SpreadX_0 = ApplyValuebyRangeWithEnchantFactor(SpreadXRng_0);
        ProjSpdX_0 = ApplyValuebyRangeWithEnchantFactor(ProjSpdXRng_0);
        ProjTimeX_0 = ApplyValuebyRangeWithEnchantFactor(ProjTimeXRng_0);

        // ? 유연성은 좋지만... 성능상 및 계층구조의 코드 복잡도가 증가하는거 같아 이 방식의 메소드는 deprecate.
        // var jObject = this.ToJObject();
        // foreach (var valueJp in jObject.Properties())
        // {
        //     foreach (var rangeJp in jObject.Properties())
        //         if (rangeJp.Value.HasValues)
        //             if (valueJp.Name != rangeJp.Name)
        //                 if (valueJp.Name == rangeJp.Name.Replace("Rng", string.Empty))
        //                 {
        //                     jObject[valueJp.Name] = ApplyValuebyRangeAndEnchantFactor(rangeJp.Value.Value<object>().DeserializeObject<List<float>>());

        //                     if (factorIndex > enchantFactors.Length)
        //                         break;
        //                 }

        //     if (factorIndex > enchantFactors.Length)
        //         break;
        // }

        // var factorAppliedEnchantData = jObject.DeserializeObject<EnchantData>();
        // AssembleEnchantData(factorAppliedEnchantData, factorAppliedEnchantData, factorAppliedEnchantData.Name);
    }

    float ApplyValuebyRangeWithEnchantFactor(float[] rangeArr)
    {
        if (rangeArr != null && factorIndex < enchantFactors.Length)
            return rangeArr[0] + (rangeArr[1] - rangeArr[0]) * enchantFactors[factorIndex++];
        else return 0;
    }
}
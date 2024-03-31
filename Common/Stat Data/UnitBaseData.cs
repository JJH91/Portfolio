using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[Serializable]
public abstract class UnitBaseData
{
    [ShowInInspector] public string InstanceItemId { get; set; }
    [ShowInInspector] public string CatalogItemId { get; set; }
    [ShowInInspector] public string Name { get; set; }
    [ShowInInspector] public string Series { get; set; }
    [ShowInInspector] public NameManager.Grade Grade { get; set; }
    [ShowInInspector] public NameManager.Grade PrmGrade { get; set; }
    [ShowInInspector] public NameManager.Element Element { get; set; }
    [ShowInInspector] public DragonBonesUnit.UnitRole Role { get; set; }
    [ShowInInspector] public int Lv { get; set; } = 1;
    [ShowInInspector] public int AwakeningLv { get; set; }
    [ShowInInspector] public int Exp { get; set; }

    [ShowInInspector] public List<SkillData> SkillDataList { get; set; } = new List<SkillData>();

    [ShowInInspector] public Dictionary<NameManager.Rank, float[]> AtkFactors { get; set; } = new Dictionary<NameManager.Rank, float[]>();
    [ShowInInspector] public Dictionary<NameManager.Rank, float[]> DefFactors { get; set; } = new Dictionary<NameManager.Rank, float[]>();
    [ShowInInspector] public Dictionary<NameManager.Rank, float[]> MaxHpFactors { get; set; } = new Dictionary<NameManager.Rank, float[]>();
    [ShowInInspector] public Dictionary<NameManager.Rank, float[]> MaxMpFactors { get; set; } = new Dictionary<NameManager.Rank, float[]>();

    [ShowInInspector] public float Dps { get; set; }
    [ShowInInspector] public float Tough { get; set; }

    [ShowInInspector] public abstract float Atk { get; set; }
    [ShowInInspector] public abstract float Def { get; set; }
    [ShowInInspector] public abstract float CrtRate_1 { get; set; }        // 크리티컬 확률 최대 1 = 100%
    [ShowInInspector] public abstract float CrtDmg_0 { get; set; }         // 1 = 100% 기준 크리티컬 추가 데미지 (기존 공격력 1 + 현재 변수의 값 0.00)

    [ShowInInspector] public abstract float MaxHp { get; set; }
    float curHp;
    [ShowInInspector]
    public float CurHp
    {
        get => curHp;
        set
        {
            if (value >= 0)
            {
                if (value > MaxHp)
                    curHp = MaxHp;
                else curHp = value;
            }
            else curHp = 0;
        }
    }
    [ShowInInspector] public abstract float MaxMp { get; set; }
    float curMp;
    [ShowInInspector]
    public float CurMp
    {
        get => curMp;
        set
        {
            if (value >= 0)
            {
                if (value > MaxMp) curMp = MaxMp;
                else curMp = value;
            }
            else curMp = 0;
        }
    }
    [ShowInInspector] public abstract float MpRecy { get; set; }          // MP 회복 속도
    [ShowInInspector] public abstract float Spd { get; set; }

    [ShowInInspector] public abstract float DetectRange { get; set; }

    /// <summary>
    /// Fire per min.
    /// </summary>
    [ShowInInspector] public abstract float Rpm { get; set; }              // 무기 데이터에만 적용.
    /// <summary>
    /// Projectile spread angle -90 ~ +90.
    /// </summary>
    [ShowInInspector] public abstract float Spread_180 { get; set; }       // 무기 및 몬스터 데이터에만 적용.
    [ShowInInspector] public abstract float ProjSpd_0 { get; set; }
    [ShowInInspector] public abstract float ProjTime_0 { get; set; }

    public void AssembleUnitBaseData(UnitBaseData customData, UnitBaseData catalogData, string name)
    {
        InstanceItemId = customData.InstanceItemId;
        CatalogItemId = catalogData.CatalogItemId;
        Name = name;
        Series = catalogData.Series;

        Lv = Mathf.Max(customData.Lv, catalogData.Lv);
        AwakeningLv = customData.AwakeningLv;
        Exp = catalogData.Lv == 1 && catalogData.Exp != 0 ? catalogData.Exp : customData.Exp;
        Grade = catalogData.Grade;
        PrmGrade = customData.PrmGrade > Grade ? customData.PrmGrade : Grade;
        Role = catalogData.Role;

        // TODO: 유닛 타입별 스킬의 범위 값 적용이 방법이 달라서 유닛 타입을 알아내야함. (캐릭터는 스킬 카탈로그의 범위, 몬스터는 몬스터 카탈로그의 커스텀 데이터에 스킬 데이터 리스트에 작성하는 범위 값 사용).
        // TODO: (캐릭터는 스킬 카탈로그의 범위, 몬스터는 몬스터 카탈로그의 커스텀 데이터에 스킬 데이터 리스트에 작성하는 범위 값 사용).
        // TODO: 퍼포먼스와 통일성을 위해 해당 차이를 삭제하거나, 삭제하되 차이를 주는 방법을 버프 시스템을 활용하여 범위 값을 조절하는 등의 우회 방법을 고려해볼것.
        if (!catalogData.CatalogItemId.IsNullOrEmpty())
        {
            var catalogItem = PlayFabManager.Instance.MainCatalogItemDictionary.ContainsKey(catalogData.CatalogItemId)
                ? PlayFabManager.Instance.MainCatalogItemDictionary[catalogData.CatalogItemId] : PlayFabManager.Instance.MonsterCatalogItemDictionary[catalogData.CatalogItemId];
            var unitType = catalogItem.ItemClass.ToCachedEnum<DragonBonesUnit.DragonBonesUnitType>();
            SkillDataList = catalogData.SkillDataList;
            foreach (var skillData in SkillDataList)
                skillData.GetAssembledSkillData(1, unitType);
        }

        AtkFactors = catalogData.AtkFactors;
        DefFactors = catalogData.DefFactors;
        MaxHpFactors = catalogData.MaxHpFactors;
        MaxMpFactors = catalogData.MaxMpFactors;

        Atk = Mathf.Max(catalogData.Atk, AtkFactors.GetQuadraticEquationValueByLevel(Lv));
        Atk = ApplyPrmGradeToValue(Atk);
        Def = Mathf.Max(catalogData.Def, DefFactors.GetQuadraticEquationValueByLevel(Lv));
        Def = ApplyPrmGradeToValue(Def);
        CrtRate_1 = catalogData.CrtRate_1;
        CrtDmg_0 = catalogData.CrtDmg_0;

        MaxHp = Mathf.Max(catalogData.MaxHp, MaxHpFactors.GetQuadraticEquationValueByLevel(Lv));
        MaxHp = ApplyPrmGradeToValue(MaxHp);
        CurHp = MaxHp;
        MaxMp = Mathf.Max(catalogData.MaxMp, MaxMpFactors.GetQuadraticEquationValueByLevel(Lv));
        MaxMp = ApplyPrmGradeToValue(MaxMp);
        CurMp = MaxMp;
        MpRecy = catalogData.MpRecy;
        Spd = catalogData.Spd;

        DetectRange = catalogData.DetectRange;

        Rpm = catalogData.Rpm;
        Spread_180 = catalogData.Spread_180 * 0.5f;
        ProjSpd_0 = catalogData.ProjSpd_0;
        ProjTime_0 = catalogData.ProjTime_0;

        Tough = this.GetToughness();
    }

    float ApplyPrmGradeToValue(float value)
    {
        return Mathf.Ceil(value * (5 + (int)PrmGrade) / 10);
    }

    public void LevelUp(int addLevel)
    {
        Lv += addLevel;

        Atk = ApplyPrmGradeToValue(AtkFactors.GetQuadraticEquationValueByLevel(Lv));
        Def = ApplyPrmGradeToValue(DefFactors.GetQuadraticEquationValueByLevel(Lv));

        var preMaxHp = MaxHp;
        var preMaxMp = MaxMp;

        MaxHp = ApplyPrmGradeToValue(MaxHpFactors.GetQuadraticEquationValueByLevel(Lv));
        MaxMp = ApplyPrmGradeToValue(MaxMpFactors.GetQuadraticEquationValueByLevel(Lv));

        CurHp += MaxHp - preMaxHp;
        CurMp += MaxMp - preMaxMp;

        Dps = this.GetDPS();
        Tough = this.GetToughness();
    }
}

[Serializable]
public abstract class ExtendedUnitBaseData : UnitBaseData
{
    // 능력치 추가 비율
    [ShowInInspector] public abstract float AtkX_0 { get; set; }
    [ShowInInspector] public abstract float DefX_0 { get; set; }
    [ShowInInspector] public abstract float CrtRateX_0 { get; set; }        // 크리티컬 확률 최대 1 = 100%
    [ShowInInspector] public abstract float CrtDmgX_0 { get; set; }        // 1 = 100% 기준 크리티컬 추가 데미지 (기존 공격력 1 + 현재 변수의 값 0.00)

    [ShowInInspector] public abstract float MaxHpX_0 { get; set; }
    [ShowInInspector] public abstract float MaxMpX_0 { get; set; }
    [ShowInInspector] public abstract float MpRecyX_0 { get; set; }
    [ShowInInspector] public abstract float SpdX_0 { get; set; }

    // 이하 무기 데이터에만 적용하는 비율
    [ShowInInspector] public abstract float RpmX_0 { get; set; }
    [ShowInInspector] public abstract float SpreadX_0 { get; set; }
    [ShowInInspector] public abstract float ProjSpdX_0 { get; set; }
    [ShowInInspector] public abstract float ProjTimeX_0 { get; set; }
}
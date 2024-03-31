using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;

public class SpawnLocator : MonoBehaviour, IMapPointerObject
{
    // 조합에 따라 분류
    public enum SpawnTypeSet { A, B, C, D }
    public enum SpawnTargetType { Character, Monster, Boss }

    [TitleGroup("Spawn Locator"), BoxGroup("Spawn Locator/SL", showLabel: false)]
    [BoxGroup("Spawn Locator/SL/Spawn Data"), OnValueChanged("OnTargetChanged"), SerializeField] SpawnTargetType spawnTarget;
    public SpawnTargetType SpawnTarget { get => spawnTarget; private set => spawnTarget = value; }
    [BoxGroup("Spawn Locator/SL/Spawn Data"), OnValueChanged("OnSpawnSetOrAmountChanged"), SerializeField] SpawnTypeSet spawnSet;
    public SpawnTypeSet SpawnSet { get => spawnSet; private set => spawnSet = value; }
    [BoxGroup("Spawn Locator/SL/Spawn Data/Specific"), TableList(AlwaysExpanded = true), ShowIf("@spawnTarget == SpawnTargetType.Monster"), SerializeField] List<RoleWeightData> roleWeightDataList = new List<RoleWeightData>();
    public List<RoleWeightData> RoleWeightDataList { get => roleWeightDataList; private set => roleWeightDataList = value; }
    [BoxGroup("Spawn Locator/SL/Spawn Data"), OnValueChanged("OnSpawnSetOrAmountChanged"), SerializeField] int totalWeight;
    public int TotalWeight { get => totalWeight; private set => totalWeight = value; }
    [BoxGroup("Spawn Locator/SL/Spawn Data"), OnValueChanged("OnRangeChanged"), SerializeField] float spawnRange;
    public float Range { get => spawnRange; }

    [TitleGroup("UI"), BoxGroup("UI/U", showLabel: false)]
    [BoxGroup("UI/U/Information ON OFF"), SerializeField, ReadOnly] bool isShowInformation = true;
    public bool IsShowInformation
    {
        get => isShowInformation;
        set
        {
            isShowInformation = value;
            InformationOnOff(value);
        }
    }
    [BoxGroup("UI/U/Content UI"), SerializeField] SpriteRenderer spriteRenderer;
    [BoxGroup("UI/U/Content UI"), SerializeField] TextMeshPro spawnAmountText;
    [BoxGroup("UI/U/Content UI"), SerializeField] List<Color> colorList;

    [BoxGroup("Spawn Locator/SL/Spawn Data"), Button("Init Spawn Amount", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Blue")]
    public void InitSpawnAmount()
    {
        if (SpawnTarget != SpawnTargetType.Monster)
            TotalWeight = 1;
        else
            TotalWeight = 0;

        for (int i = 0; i < RoleWeightDataList.Count; i++)
            TotalWeight += RoleWeightDataList[i].Weight;

        OnSpawnSetOrAmountChanged();
    }

    [BoxGroup("Spawn Locator/SL/Information ON OFF"), Button("Information ON OFF", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
    void InformationOnOFf()
    {
        IsShowInformation = !IsShowInformation;

        InformationOnOff(IsShowInformation);
    }

    public void InformationOnOff(bool onoff)
    {
        spriteRenderer.enabled = onoff;
        spawnAmountText.enabled = onoff;
    }

    public Vector3 GetCenterPosition()
    {
        return transform.position;
    }

    public Vector3 GetRandomPositionInRange(float range = 0)
    {
        if (range == 0)
            range = Range;

        return transform.position.GetRandomInsidePositionAsQuarterView(range * 5);
    }

    void OnTargetChanged()
    {
        spriteRenderer.color = colorList[(int)SpawnTarget];

        if (SpawnTarget != SpawnTargetType.Monster)
        {
            TotalWeight = 1;
            RoleWeightDataList.Clear();
        }
        else
        {
            TotalWeight = 0;

            var unitRoleNames = Enum.GetNames(typeof(DragonBonesUnit.UnitRole));
            for (int i = 0; i < unitRoleNames.Length; i++)
                RoleWeightDataList.Add(new RoleWeightData(unitRoleNames[i]));
        }
    }

    void OnSpawnSetOrAmountChanged()
    {
        spawnAmountText.text = $"{SpawnSet}{TotalWeight}";
    }

    void OnRangeChanged()
    {
        transform.localScale = new Vector2(Range, Range * 0.5f);
    }
}

[System.Serializable]
public class RoleWeightData
{
    [SerializeField, ReadOnly] DragonBonesUnit.UnitRole role;
    public DragonBonesUnit.UnitRole Role { get => role; set => role = value; }
    [SerializeField] int weight;
    public int Weight { get => weight; set => weight = value; }

    public RoleWeightData(string roleStr)
    {
        Role = roleStr.ToCachedEnum<DragonBonesUnit.UnitRole>();
    }
}
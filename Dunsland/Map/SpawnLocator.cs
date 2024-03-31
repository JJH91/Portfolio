using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;

public class SpawnLocator : MonoBehaviour
{
    // 조합에 따라 분류
    public enum SpawnSet { A, B, C, D }
    public enum SpawnTarget { Character, Monster, Boss }

    [TitleGroup("Spawn Locator"), BoxGroup("Spawn Locator/SL", showLabel: false)]
    [BoxGroup("Spawn Locator/SL/Spawn Data"), OnValueChanged("OnTargetChanged"), SerializeField] SpawnTarget mySpawnTarget;
    public SpawnTarget MySpawnTarget { get => mySpawnTarget; private set => mySpawnTarget = value; }
    [BoxGroup("Spawn Locator/SL/Spawn Data"), OnValueChanged("OnSpawnSetOrAmountChanged"), SerializeField] SpawnSet mySpawnSet;
    public SpawnSet MySpawnSet { get => mySpawnSet; private set => mySpawnSet = value; }
    [BoxGroup("Spawn Locator/SL/Spawn Data/Specific"), TableList(AlwaysExpanded = true), ShowIf("@MySpawnTarget == SpawnTarget.Monster"), SerializeField] List<RoleWeightData> roleWeightDataList = new List<RoleWeightData>();
    public List<RoleWeightData> RoleWeightDataList { get => roleWeightDataList; private set => roleWeightDataList = value; }
    [BoxGroup("Spawn Locator/SL/Spawn Data"), OnValueChanged("OnSpawnSetOrAmountChanged"), SerializeField] int totalWeight;
    public int TotalWeight { get => totalWeight; private set => totalWeight = value; }
    [BoxGroup("Spawn Locator/SL/Spawn Data"), OnValueChanged("OnRangeChanged"), SerializeField] float spawnRange;
    public float SpawnRange { get => spawnRange; private set => spawnRange = value; }

    [TitleGroup("UI"), BoxGroup("UI/U", showLabel: false)]
    [BoxGroup("UI/U/Information ON OFF"), ShowInInspector, ReadOnly] public bool IsShowInformation { get; set; } = true;
    [BoxGroup("UI/U/Content UI"), SerializeField, ReadOnly] SpriteRenderer spriteRenderer;
    [BoxGroup("UI/U/Content UI"), SerializeField, ReadOnly] TextMeshPro spawnAmountTMP;
    [BoxGroup("UI/U/Content UI"), SerializeField, ReadOnly] List<Color> colorList;

    [BoxGroup("Spawn Locator/SL/Spawn Data"), Button("Init Spawn Amount", ButtonSizes.Gigantic), GUIColor(0.35f, 0.7f, 1)]
    public void InitSpawnAmount()
    {
        if (MySpawnTarget != SpawnTarget.Monster)
            TotalWeight = 1;
        else
            TotalWeight = 0;

        for (int i = 0; i < RoleWeightDataList.Count; i++)
            TotalWeight += RoleWeightDataList[i].Weight;

        OnSpawnSetOrAmountChanged();
    }

    [BoxGroup("Spawn Locator/SL/Information ON OFF"), Button("Information ON OFF", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    public void InformationOnOFf()
    {
        IsShowInformation = !IsShowInformation;

        spriteRenderer.enabled = IsShowInformation;
        spawnAmountTMP.enabled = IsShowInformation;
    }

    public void InformationOnOFf(bool onoff)
    {
        spriteRenderer.enabled = onoff;
        spawnAmountTMP.enabled = onoff;
    }

    void OnTargetChanged()
    {
        spriteRenderer.color = colorList[(int)MySpawnTarget];

        if (MySpawnTarget != SpawnTarget.Monster)
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
        spawnAmountTMP.text = $"{MySpawnSet}{TotalWeight}";
    }

    void OnRangeChanged()
    {
        transform.localScale = new Vector2(SpawnRange, SpawnRange * 0.5f);
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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DG.Tweening;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;

public class Map : AddressableSerializedMonoBehavior
{
    [TitleGroup("Map"), BoxGroup("Map/M", showLabel: false)]
    [BoxGroup("Map/M/Checker"), HorizontalGroup("Map/M/Checker/H"), SerializeField, ReadOnly] bool isOptimized;
    [BoxGroup("Map/M/Checker"), HorizontalGroup("Map/M/Checker/H"), ShowInInspector, ReadOnly] public bool IsCompleteMapLoad { get; private set; }

    [BoxGroup("Map/M/Tilemap Renderer"), SerializeField] List<TilemapRenderer> tilemapRendererList = new List<TilemapRenderer>();

    [BoxGroup("Map/M/Spawn Location"), SerializeField] List<SpawnLocator> spawnLocatorList = new List<SpawnLocator>();
    [BoxGroup("Map/M/Spawn Location"), SerializeField, ReadOnly] int totalCharacterSpawnCount, totalMonsterSpawnCount, totalBossSpawnCount;
    [BoxGroup("Map/M/Spawn Location"), SerializeField, ReadOnly] bool isShowInformation;

    [BoxGroup("Map/M/Spawn Unit"), SerializeField] List<SpawnUnitData> spawnUnitDataListFromQuestData;
    [BoxGroup("Map/M/Spawn Unit"), SerializeField] Dictionary<DragonBonesUnit.UnitRole, List<SpawnUnitData>> roleClassifiedSpawnUnitDataDictFromQuestData = new Dictionary<DragonBonesUnit.UnitRole, List<SpawnUnitData>>();
    [BoxGroup("Map/M/Spawn Unit"), SerializeField, ReadOnly] int diffRoleTotalWeight;
    // [BoxGroup("Map/M/Spawn Unit"), SerializeField, ReadOnly] int spawnAmountFactorByQuestGrade;
    [BoxGroup("Map/M/Spawn Unit"), SerializeField] string testJson;

    DragonBonesUnit.UnitRole tempDiffRoleKey1, tempDiffRoleKey2;
    bool isTempDiffListCleared = false;

    protected override void Awake()
    {
        InitTilemapRendererList();

        base.Awake();
    }

    protected override void Start()
    {
        IsCompleteMapLoad = false;

        base.Start();

        SpecificTilemapToChunkMode();
        InitSpawnLocator();
        SpawnLocationInformationOff();
        if (CombatTestManager.Instance != null)
            SetSpawnDataList(CombatTestManager.Instance.TestQuestData);
        else
            SetSpawnDataList(GameManager.Instance.CurQuestData);
        SpawnUnits();

        IsCompleteMapLoad = true;
    }

    /**
     * --------------------------------------------------
     * Deactive On Map Load
     * --------------------------------------------------
     */

    protected override void SetDeactiveOnMapLoad()
    {
        gameObject.SetActive(false);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                     * Test *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public bool isTest;
    int test = 0;

    [BoxGroup("Test"), ButtonGroup("Test/Spawn Test")]
    void SpawnTest()
    {
        InitSpawnLocator();
        SetSpawnDataList(GameManager.Instance.CurQuestData);
        SpawnUnits();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                * Map Edit Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Map/M/Tilemap Renderer"), Button("Init Tilemap Renderer List", ButtonSizes.Gigantic), GUIColor(0.35f, 0.7f, 1)]
    void InitTilemapRendererList()
    {
        tilemapRendererList = transform.GetComponentsInChildren<TilemapRenderer>().ToList();
    }

    [BoxGroup("Map/M/Tilemap Renderer"), Button("Specific Tilemap to Chunk Mode (Play Mode)", ButtonSizes.Gigantic), GUIColor(0.35f, 0.7f, 1)]
    void SpecificTilemapToChunkMode()
    {
        if (isOptimized)
            return;

        InitTilemapRendererList();

        for (int i = 0; i < tilemapRendererList.Count; i++)
        {
            if (tilemapRendererList[i].sortingLayerName == NameManager.SortingLayerName.Unit.ToCachedString())
                tilemapRendererList[i].mode = TilemapRenderer.Mode.Individual;
            else
                tilemapRendererList[i].mode = TilemapRenderer.Mode.Chunk;

            if (tilemapRendererList[i].name.Split(' ').Contains("Colider"))
                tilemapRendererList[i].gameObject.GetComponent<Tilemap>().color = Color.clear;
        }

        isOptimized = true;

        gameObject.Log($"일부 Tilemap Renderer Chunk Mode 변경 완료");
    }

    [BoxGroup("Map/M/Tilemap Renderer"), Button("All Tilemap to Individual Mode (Editor Mode)", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    void AllTilemapToIndividualMode()
    {
        InitTilemapRendererList();

        for (int i = 0; i < tilemapRendererList.Count; i++)
        {
            tilemapRendererList[i].mode = TilemapRenderer.Mode.Individual;

            if (tilemapRendererList[i].name.Split(' ').Contains("Colider"))
                tilemapRendererList[i].gameObject.GetComponent<Tilemap>().color = Color.white;
        }

        isOptimized = false;

        gameObject.Log($"모든 Tilemap Renderer Individual Mode 변경 완료");
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *             * Spawn Location Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Map/M/Spawn Location"), Button("Init Spawn Location", ButtonSizes.Gigantic), GUIColor(0.35f, 0.7f, 1)]
    void InitSpawnLocator()
    {
        isShowInformation = true;
        spawnLocatorList = transform.GetComponentsInChildren<SpawnLocator>().ToList();

        totalCharacterSpawnCount = 0;
        totalMonsterSpawnCount = 0;
        totalBossSpawnCount = 0;

        for (int i = 0; i < spawnLocatorList.Count; i++)
        {
            spawnLocatorList[i].IsShowInformation = true;
            spawnLocatorList[i].InitSpawnAmount();

            switch (spawnLocatorList[i].MySpawnTarget)
            {
                case SpawnLocator.SpawnTarget.Character:
                    totalCharacterSpawnCount += spawnLocatorList[i].TotalWeight;
                    break;
                case SpawnLocator.SpawnTarget.Monster:
                    totalMonsterSpawnCount += spawnLocatorList[i].TotalWeight;
                    break;
                case SpawnLocator.SpawnTarget.Boss:
                    totalBossSpawnCount += spawnLocatorList[i].TotalWeight;
                    break;
            }

            // ! deprecated
            // totalMonsterSpawnCount *= spawnAmountFactorByQuestGrade;
        }

        gameObject.Log($"Spawn Location 초기화 완료.");
    }

    [BoxGroup("Map/M/Spawn Location"), Button("Spawn Location Information ON, OFF", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    void SpawnLocationInformationOnOff()
    {
        isShowInformation = !isShowInformation;

        for (int i = 0; i < spawnLocatorList.Count; i++)
        {
            spawnLocatorList[i].IsShowInformation = isShowInformation;
            spawnLocatorList[i].InformationOnOFf(isShowInformation);
        }

        gameObject.Log($"Spawn Location Information ON / OFF 완료.");
    }

    void SpawnLocationInformationOff()
    {
        isShowInformation = false;

        for (int i = 0; i < spawnLocatorList.Count; i++)
        {
            spawnLocatorList[i].IsShowInformation = isShowInformation;
            spawnLocatorList[i].InformationOnOFf(isShowInformation);
        }

        gameObject.Log($"Spawn Location Information OFF 완료.");
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Spawn Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Map/M/Spawn Unit"), Button("Clear Spawn Unit Data", ButtonSizes.Large), GUIColor(1, 0.7f, 0.7f)]
    void ClearSpawnUnitData()
    {
        spawnUnitDataListFromQuestData.Clear();
        roleClassifiedSpawnUnitDataDictFromQuestData.Clear();
        diffRoleTotalWeight = 0;
        testJson = string.Empty;
    }


    [BoxGroup("Map/M/Spawn Unit"), Button("Set Spawn Data List", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    void SetSpawnDataList(QuestData questData)
    {
        if (CombatManager.Instance == null)
            return;

        spawnUnitDataListFromQuestData = questData.SpawnUnitDataList;

        // ! deprecated
        // 소환량 조절
        // spawnAmountFactorByQuestGrade = (int)GameManager.Instance.CurQuestData.Grade / 2;

        // 서버의 퀘스트 데이터에는 role 값이 등록되어 있지 않으므로 여기서 role 초기화
        for (int i = 0; i < spawnUnitDataListFromQuestData.Count; i++)
            spawnUnitDataListFromQuestData[i].InitRole();

        // 이전 맵에서 합산 시킨 무게 초기화
        spawnUnitDataListFromQuestData[0].TotalWeight = 0;

        for (int i = 0; i < spawnUnitDataListFromQuestData.Count; i++)
        {
            if (!roleClassifiedSpawnUnitDataDictFromQuestData.ContainsKey(spawnUnitDataListFromQuestData[i].Role))
                roleClassifiedSpawnUnitDataDictFromQuestData.Add(spawnUnitDataListFromQuestData[i].Role, new List<SpawnUnitData>());
            roleClassifiedSpawnUnitDataDictFromQuestData[spawnUnitDataListFromQuestData[i].Role].Add(spawnUnitDataListFromQuestData[i]);

            // 0번 인덱스에 Weight를 합산
            roleClassifiedSpawnUnitDataDictFromQuestData[spawnUnitDataListFromQuestData[i].Role][0].TotalWeight += spawnUnitDataListFromQuestData[i].Wgt;
        }
    }

    [BoxGroup("Map/M/Spawn Unit"), Button("Set Spawn Data List Test", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    void SetSpawnDataListTest()
    {
        spawnUnitDataListFromQuestData = JsonConvert.DeserializeObject<QuestData>(testJson).SpawnUnitDataList;

        // 서버의 퀘스트 데이터에는 role 값이 등록되어 있지 않으므로 여기서 role 초기화, 이건 테스트라 따로 이렇게 해놓음
        for (int i = 0; i < spawnUnitDataListFromQuestData.Count; i++)
        {
            if (spawnUnitDataListFromQuestData[i].Id == "Slime Basic_W")
                spawnUnitDataListFromQuestData[i].Role = DragonBonesUnit.UnitRole.Warrior;
            else if (spawnUnitDataListFromQuestData[i].Id == "Slime Basic_A")
                spawnUnitDataListFromQuestData[i].Role = DragonBonesUnit.UnitRole.Archer;
            else if (spawnUnitDataListFromQuestData[i].Id == "Slime Basic_M")
                spawnUnitDataListFromQuestData[i].Role = DragonBonesUnit.UnitRole.Mage;
            else if (spawnUnitDataListFromQuestData[i].Id == "Slime Basic_C")
                spawnUnitDataListFromQuestData[i].Role = DragonBonesUnit.UnitRole.Cleric;
        }

        if (roleClassifiedSpawnUnitDataDictFromQuestData == null)
            roleClassifiedSpawnUnitDataDictFromQuestData = new Dictionary<DragonBonesUnit.UnitRole, List<SpawnUnitData>>();
        roleClassifiedSpawnUnitDataDictFromQuestData.Clear();

        for (int i = 0; i < spawnUnitDataListFromQuestData.Count; i++)
        {
            // Role Dictionary 에 해당 Role Key 유무를 확인하고 할당함
            if (!roleClassifiedSpawnUnitDataDictFromQuestData.ContainsKey(spawnUnitDataListFromQuestData[i].Role))
                roleClassifiedSpawnUnitDataDictFromQuestData.Add(spawnUnitDataListFromQuestData[i].Role, new List<SpawnUnitData>());
            roleClassifiedSpawnUnitDataDictFromQuestData[spawnUnitDataListFromQuestData[i].Role].Add(spawnUnitDataListFromQuestData[i]);

            // 해당 Role Dictionary 의 0번 인덱스에 Weight를 합산
            roleClassifiedSpawnUnitDataDictFromQuestData[spawnUnitDataListFromQuestData[i].Role][0].TotalWeight += spawnUnitDataListFromQuestData[i].Wgt;
        }
    }

    [BoxGroup("Map/M/Spawn Unit"), Button("Spawn Units", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    void SpawnUnits()
    {
        if (CombatManager.Instance == null)
            return;

        for (int i = 0; i < spawnLocatorList.Count; i++)
        {
            switch (spawnLocatorList[i].MySpawnTarget)
            {
                case SpawnLocator.SpawnTarget.Character:
                    string presetMainCharacterInstanceId = PlayFabManager.Instance.UserData.UnitPresets[GameManager.Instance.CurUnitPresetIndex].MainUnitInstId;

                    if (CombatManager.Instance.PlayingCharacter == null)
                        ObjectManager.Instance.GetCharacter(presetMainCharacterInstanceId, GetSpawnPosion(spawnLocatorList[i], true), true);

                    CombatManager.Instance.PlayingCharacter.MoveTransformPosition(GetSpawnPosion(spawnLocatorList[i], true));
                    break;

                case SpawnLocator.SpawnTarget.Monster:
                    // 스폰 로케이션의 직군 스폰 데이터 리스트
                    List<RoleWeightData> spawnLocatorRoleWeightDataList = spawnLocatorList[i].RoleWeightDataList;
                    // 대체 소환 직군 임시 리스트
                    List<SpawnUnitData> tmepDiffRoleSpawnUnitDataList = new List<SpawnUnitData>();
                    isTempDiffListCleared = false;

                    for (int j = 0; j < spawnLocatorRoleWeightDataList.Count; j++)
                        SpawnReplaceableRoleUnit(spawnLocatorRoleWeightDataList[j].Role, i, spawnLocatorRoleWeightDataList[j].Weight, tmepDiffRoleSpawnUnitDataList);

                    break;

                case SpawnLocator.SpawnTarget.Boss:

                    break;
            }
        }
    }

    void SpawnReplaceableRoleUnit(DragonBonesUnit.UnitRole roleKey, int spawnLocatorIndex, int repeat, List<SpawnUnitData> diffRoleSpawnUnitDataList)
    {
        // ! deprecated
        // // 퀘스트 등급에 따라 소환량 조절
        // repeat *= spawnAmountFactorByQuestGrade;

        // 직군에 맞거나 비슷한 직군에서 소환('SpawnSimilerRoleUnit')
        if (!SpawnSimilerRoleUnit(roleKey, spawnLocatorIndex, repeat))
        {
            // 대체 직군은 물리 계열 / 마법 계열 중 1개만 생성하면 된다. 단, Assassin 과 Terminator는 기존의 대체 직군을 초기화하고 다시 목록을 생성한다.
            if (roleKey >= DragonBonesUnit.UnitRole.Assassin && !isTempDiffListCleared)
            {
                diffRoleSpawnUnitDataList.Clear();
                isTempDiffListCleared = true;
            }

            // 비슷한 직군이 없는 경우, 반대 계열 직군에서 소환
            if (diffRoleSpawnUnitDataList.Count == 0)
                ConfigureDiffRoleSpawnUnitDataList(roleKey, diffRoleSpawnUnitDataList);

            // 대체 직군 생성에 실패한 경우, Assassin(물리 계열) 또는 Terminator(마법 계열)로 대체 소환한다.(각 계열에 맞는 소환을 먼저 진행)
            if (diffRoleSpawnUnitDataList.Count == 0)
            {
                // 0 ~ 3 에 해당하는 직군이 없다면 4, 5 직군은 무조건 존재함.
                if (roleKey <= DragonBonesUnit.UnitRole.Archer) // 물리 계열 (0, 1)
                {
                    if (roleClassifiedSpawnUnitDataDictFromQuestData.ContainsKey(DragonBonesUnit.UnitRole.Assassin))
                        for (int i = 0; i < repeat; i++)
                            SpawnWeightedRandomMonster(roleClassifiedSpawnUnitDataDictFromQuestData[DragonBonesUnit.UnitRole.Assassin], diffRoleTotalWeight, GetSpawnPosion(spawnLocatorList[spawnLocatorIndex]));
                    else
                        for (int i = 0; i < repeat; i++)
                            SpawnWeightedRandomMonster(roleClassifiedSpawnUnitDataDictFromQuestData[DragonBonesUnit.UnitRole.Terminator], diffRoleTotalWeight, GetSpawnPosion(spawnLocatorList[spawnLocatorIndex]));
                }
                else if (roleKey <= DragonBonesUnit.UnitRole.Cleric) // 마법 계열 (2, 3)
                {
                    if (roleClassifiedSpawnUnitDataDictFromQuestData.ContainsKey(DragonBonesUnit.UnitRole.Terminator))
                        for (int i = 0; i < repeat; i++)
                            SpawnWeightedRandomMonster(roleClassifiedSpawnUnitDataDictFromQuestData[DragonBonesUnit.UnitRole.Terminator], diffRoleTotalWeight, GetSpawnPosion(spawnLocatorList[spawnLocatorIndex]));
                    else
                        for (int i = 0; i < repeat; i++)
                            SpawnWeightedRandomMonster(roleClassifiedSpawnUnitDataDictFromQuestData[DragonBonesUnit.UnitRole.Assassin], diffRoleTotalWeight, GetSpawnPosion(spawnLocatorList[spawnLocatorIndex]));
                }
                else
                    ConfigureDiffRoleSpawnUnitDataList(roleKey, diffRoleSpawnUnitDataList, true);
            }

            // 대체 직군 소환
            if (diffRoleSpawnUnitDataList.Count != 0)
                for (int i = 0; i < repeat; i++)
                    SpawnWeightedRandomMonster(diffRoleSpawnUnitDataList, diffRoleTotalWeight, GetSpawnPosion(spawnLocatorList[spawnLocatorIndex]));
            else
            {
                gameObject.LogError($"대체 직군 소환 완전 실패");
                gameObject.LogError($"roleKey: {roleKey}");
                gameObject.LogError($"spawnLocatorIndex: {spawnLocatorIndex}");
                gameObject.LogError($"repeat: {repeat / ((int)GameManager.Instance.CurQuestData.Grade / 2)}, {repeat}");
                foreach (var dictKvp in roleClassifiedSpawnUnitDataDictFromQuestData)
                    foreach (var data in dictKvp.Value)
                        gameObject.LogError($"data(Id, Role, Wgt, ttlWgt): {data.Id} / {data.Role} / {data.Wgt} / {data.TotalWeight}");
            }
        }
    }

    bool SpawnSimilerRoleUnit(DragonBonesUnit.UnitRole roleKey, int spawnLocatorIndex, int repeat)
    {
        if (roleClassifiedSpawnUnitDataDictFromQuestData.ContainsKey(roleKey)) // 해당 직군이 소환 가능하면 소환
        {
            for (int i = 0; i < repeat; i++)
                SpawnWeightedRandomMonster(roleClassifiedSpawnUnitDataDictFromQuestData[roleKey], roleClassifiedSpawnUnitDataDictFromQuestData[roleKey][0].TotalWeight, GetSpawnPosion(spawnLocatorList[spawnLocatorIndex]));
            return true;
        }
        else // 비슷한 직군으로 전환
        {
            // 0 <=> 1 (Warrior <=> Archer), 2 <=> 3 (Mage <=> Cleric), 4 <=> 5 (Assassin <=> Terminator)
            if ((int)roleKey % 2 == 0)
                roleKey++;
            else
                roleKey--;

            // 비슷한 직군 내에 소환 가능한 몬스터가 있으면 소환
            if (roleClassifiedSpawnUnitDataDictFromQuestData.ContainsKey(roleKey))
            {
                for (int i = 0; i < repeat; i++)
                    SpawnWeightedRandomMonster(roleClassifiedSpawnUnitDataDictFromQuestData[roleKey], roleClassifiedSpawnUnitDataDictFromQuestData[roleKey][0].TotalWeight, GetSpawnPosion(spawnLocatorList[spawnLocatorIndex]));
                return true;
            }
        }

        return false;
    }

    void ConfigureDiffRoleSpawnUnitDataList(DragonBonesUnit.UnitRole roleKey, List<SpawnUnitData> diffRoleSpawnUnitDataList, bool wasFailed = false)
    {
        switch (roleKey)
        {
            case DragonBonesUnit.UnitRole.Warrior:
            case DragonBonesUnit.UnitRole.Archer:
                tempDiffRoleKey1 = DragonBonesUnit.UnitRole.Mage;
                tempDiffRoleKey2 = DragonBonesUnit.UnitRole.Cleric;
                break;

            case DragonBonesUnit.UnitRole.Mage:
            case DragonBonesUnit.UnitRole.Cleric:
                tempDiffRoleKey1 = DragonBonesUnit.UnitRole.Warrior;
                tempDiffRoleKey2 = DragonBonesUnit.UnitRole.Archer;
                break;

            case DragonBonesUnit.UnitRole.Assassin:
                tempDiffRoleKey1 = !wasFailed ? DragonBonesUnit.UnitRole.Warrior : DragonBonesUnit.UnitRole.Mage;
                tempDiffRoleKey2 = !wasFailed ? DragonBonesUnit.UnitRole.Archer : DragonBonesUnit.UnitRole.Cleric;
                break;

            case DragonBonesUnit.UnitRole.Terminator:
                tempDiffRoleKey1 = !wasFailed ? DragonBonesUnit.UnitRole.Mage : DragonBonesUnit.UnitRole.Warrior;
                tempDiffRoleKey2 = !wasFailed ? DragonBonesUnit.UnitRole.Cleric : DragonBonesUnit.UnitRole.Archer;
                break;

            default:
                tempDiffRoleKey1 = DragonBonesUnit.UnitRole.Warrior;
                tempDiffRoleKey2 = DragonBonesUnit.UnitRole.Archer;
                break;
        }

        // 존재하는 다른 직군의 목록을 형성 (계열이 반대쪽인 직군)
        diffRoleTotalWeight = 0;
        if (roleClassifiedSpawnUnitDataDictFromQuestData.ContainsKey(tempDiffRoleKey1))
        {
            diffRoleTotalWeight += roleClassifiedSpawnUnitDataDictFromQuestData[tempDiffRoleKey1][0].TotalWeight;
            diffRoleSpawnUnitDataList.AddRange(roleClassifiedSpawnUnitDataDictFromQuestData[tempDiffRoleKey1]);
        }
        if (roleClassifiedSpawnUnitDataDictFromQuestData.ContainsKey(tempDiffRoleKey2))
        {
            diffRoleTotalWeight += roleClassifiedSpawnUnitDataDictFromQuestData[tempDiffRoleKey2][0].TotalWeight;
            diffRoleSpawnUnitDataList.AddRange(roleClassifiedSpawnUnitDataDictFromQuestData[tempDiffRoleKey2]);
        }
    }

    void SpawnWeightedRandomMonster(List<SpawnUnitData> spawnUnitDataList, int totalWeight, Vector2 spawnPosition)
    {
        if (spawnUnitDataList.Count > 1)
        {
            float pivot = Random.Range(0, 1f) * totalWeight;

            for (int i = 0; i < spawnUnitDataList.Count; i++)
                if (pivot <= spawnUnitDataList[i].Wgt)
                {
                    ObjectManager.Instance.GetMonster(spawnUnitDataList[i].Id, spawnPosition, GameManager.Instance.CurQuestData);
                    break;
                }
                else
                    pivot -= spawnUnitDataList[i].Wgt;
        }
        else
            ObjectManager.Instance.GetMonster(spawnUnitDataList[0].Id, spawnPosition, GameManager.Instance.CurQuestData);
    }

    Vector2 GetSpawnPosion(SpawnLocator spawnLocator, bool isCenterPosition = false)
    {
        if (isCenterPosition)
            return spawnLocator.transform.position;
        else
            return (Vector2)spawnLocator.transform.position + new Vector2(Random.Range(-spawnLocator.SpawnRange, spawnLocator.SpawnRange), 0.5f * Random.Range(-spawnLocator.SpawnRange, spawnLocator.SpawnRange));
    }
}

[System.Serializable]
public class SpawnUnitData
{
    [ShowInInspector] public DragonBonesUnit.UnitRole Role { get; set; }
    [ShowInInspector] public string Id { get; set; }
    [ShowInInspector] public int Wgt { get; set; }
    [ShowInInspector] public int TotalWeight { get; set; } // Role Dictionary 의 0번 인덱스의 값만 사용함. (거기에만 합산 및 사용)

    public void InitRole()
    {
        Role = PlayFabManager.Instance.MonsterCatalogItemDictionary[Id].CustomData.ToJObject()[nameof(MonsterStatData.Role)].ToCachedEnum<DragonBonesUnit.UnitRole>();
    }
}
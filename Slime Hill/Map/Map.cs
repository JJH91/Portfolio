using System;
using System.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.Tilemaps;
using CodeStage.AntiCheat.ObscuredTypes;
using Unity.VisualScripting;
using Sirenix.OdinInspector;

public class Map : AddressableSerializedMonoBehavior
{
    [TitleGroup("Map"), BoxGroup("Map/M", showLabel: false)]
    [BoxGroup("Map/M/Status"), HorizontalGroup("Map/M/Status/H"), SerializeField, ReadOnly] bool isOptimized;
    [BoxGroup("Map/M/Quest Data"), ShowInInspector] QuestData CurQuestData { get => CombatManager.Instance != null ? CombatManager.Instance.CurQuestData : null; }
    [BoxGroup("Map/M/Spawn Unit Data"), SerializeField] List<UnitSpawnData> spawnUnitDataList;

    [TitleGroup("Map Configure"), BoxGroup("Map Configure/MC", showLabel: false)]
    [BoxGroup("Map Configure/MC/Tilemap Renderer"), SerializeField] List<TilemapRenderer> tilemapRendererList = new List<TilemapRenderer>();

    [BoxGroup("Map Configure/MC/Spawn Locator"), SerializeField, ReadOnly] bool isShowSpawnLocator;
    [BoxGroup("Map Configure/MC/Spawn Locator"), SerializeField, HideInInspector] List<SpawnLocator> allSpawnLocatorList = new List<SpawnLocator>();
    [BoxGroup("Map Configure/MC/Spawn Locator"), SerializeField] List<SpawnLocator> characterSpawnLocatorList = new List<SpawnLocator>();
    [BoxGroup("Map Configure/MC/Spawn Locator"), SerializeField] List<SpawnLocator> monsterSpawnLocatorList = new List<SpawnLocator>();
    [BoxGroup("Map Configure/MC/Spawn Locator"), SerializeField] List<SpawnLocator> bossSpawnLocatorList = new List<SpawnLocator>();

    [BoxGroup("Map Configure/MC/Way Pointer"), SerializeField, ReadOnly] bool isShowWayPointer;
    [BoxGroup("Map Configure/MC/Way Pointer"), SerializeField, HideInInspector] List<WayPointer> allWayPointerList = new List<WayPointer>();
    [BoxGroup("Map Configure/MC/Way Pointer"), SerializeField] List<WayPointer> characterWayPointerList = new List<WayPointer>();
    public ReadOnlyCollection<WayPointer> CharacterWayPointerList { get => characterWayPointerList.AsReadOnly(); }
    [BoxGroup("Map Configure/MC/Way Pointer"), SerializeField] List<WayPointer> monsterWayPointerList = new List<WayPointer>();
    public ReadOnlyCollection<WayPointer> MonsterWayPointerList { get => monsterWayPointerList.AsReadOnly(); }
    [BoxGroup("Map Configure/MC/Way Pointer"), SerializeField] List<WayPointer> bossWayPointerList = new List<WayPointer>();
    public ReadOnlyCollection<WayPointer> BossWayPointerList { get => bossWayPointerList.AsReadOnly(); }

    WaitForSeconds waitSummonDelay;

    protected override void Awake()
    {
        base.Awake();

        waitSummonDelay = new WaitForSeconds(1f);
    }

    protected override void Start()
    {
        base.Start();

        SpecificTilemapToChunkMode();
        SpawnLocatorInformationOnOff(false);
        WayPointerInformationOnOff(false);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *              * Deactive On Map Load *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    protected override void SetDeactiveOnMapLoad()
    {
        gameObject.SetActive(false);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Test *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Test"), ButtonGroup("Test/Weighted Random Test")]
    void SpawnTest()
    {
        var testList = new List<UnitSpawnData>();
        testList.Add(new UnitSpawnData { Id = "SS", Weight = 3 });
        testList.Add(new UnitSpawnData { Id = "S", Weight = 5 });
        testList.Add(new UnitSpawnData { Id = "A", Weight = 12 });
        testList.Add(new UnitSpawnData { Id = "B", Weight = 30 });
        testList.Add(new UnitSpawnData { Id = "C", Weight = 50 });

        testList.GetWeightedRandomPickResult(10000000);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Map Edit Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Map Configure/MC/Tilemap Renderer"), Button("Init Tilemap Renderer List", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Blue")]
    void InitTilemapRendererList()
    {
        tilemapRendererList = transform.GetComponentsInChildren<TilemapRenderer>().ToList();
    }

    [BoxGroup("Map Configure/MC/Tilemap Renderer"), Button("Specific Tilemap to Chunk Mode (Play Mode)", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Blue")]
    void SpecificTilemapToChunkMode()
    {
        if (isOptimized)
            return;

        InitTilemapRendererList();

        for (int i = 0; i < tilemapRendererList.Count; i++)
        {
            if (tilemapRendererList[i].sortingLayerName == NameManager.LayerName.Wall.ToCachedString()
                || tilemapRendererList[i].sortingLayerName == NameManager.LayerName.Object.ToCachedString()
                || tilemapRendererList[i].sortingLayerName == NameManager.LayerName.Unit.ToCachedString())
                tilemapRendererList[i].mode = TilemapRenderer.Mode.Individual;
            else
                tilemapRendererList[i].mode = TilemapRenderer.Mode.Chunk;

            if (tilemapRendererList[i].name.Split(' ').Contains("Colider") || tilemapRendererList[i].sortingLayerName == NameManager.LayerName.NotWalkable.ToCachedUncamelCaseString())
                tilemapRendererList[i].gameObject.GetComponent<Tilemap>().color = Color.clear;
        }

        isOptimized = true;

        gameObject.Log($"일부 Tilemap Renderer Chunk Mode 변경 완료");
    }

    [BoxGroup("Map Configure/MC/Tilemap Renderer"), Button("All Tilemap to Individual Mode (Editor Mode)", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
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
     *              * Spawn Locator Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Map Configure/MC/Spawn Locator"), Button("Init Spawn Locator", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Blue")]
    void InitSpawnLocator()
    {
        isShowSpawnLocator = true;

        allSpawnLocatorList = transform.GetComponentsInChildren<SpawnLocator>().ToList();
        characterSpawnLocatorList = allSpawnLocatorList.Where(item => item.SpawnTarget == SpawnLocator.SpawnTargetType.Character).ToList();
        monsterSpawnLocatorList = allSpawnLocatorList.Where(item => item.SpawnTarget == SpawnLocator.SpawnTargetType.Monster).ToList();
        bossSpawnLocatorList = allSpawnLocatorList.Where(item => item.SpawnTarget == SpawnLocator.SpawnTargetType.Boss).ToList();

        for (int i = 0; i < allSpawnLocatorList.Count; i++)
        {
            allSpawnLocatorList[i].IsShowInformation = true;
            allSpawnLocatorList[i].InitSpawnAmount();
        }

        gameObject.Log($"Spawn Locator 초기화 완료.");
    }

    [BoxGroup("Map Configure/MC/Spawn Locator"), Button("Spawn Locator Information ON, OFF", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
    void SpawnLocatorInformationOnOff()
    {
        isShowSpawnLocator = !isShowSpawnLocator;

        SpawnLocatorInformationOnOff(isShowSpawnLocator);
    }

    void SpawnLocatorInformationOnOff(bool value)
    {
        isShowSpawnLocator = value;

        for (int i = 0; i < allSpawnLocatorList.Count; i++)
            allSpawnLocatorList[i].IsShowInformation = value;

        gameObject.Log($"Spawn Locator Information {value} 완료.");
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *              * Way Pointer Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Map Configure/MC/Way Pointer"), Button("Init Way Pointer", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Blue")]
    void InitWayPointer()
    {
        isShowWayPointer = true;

        allWayPointerList = transform.GetComponentsInChildren<WayPointer>().OrderBy(wp => wp.WayPointerNumber).ToList();
        characterWayPointerList = allWayPointerList.Where(item => item.WayPointerTarget == WayPointer.WayPointerTargetType.Character).OrderBy(wp => wp.WayPointerNumber).ToList();
        monsterWayPointerList = allWayPointerList.Where(item => item.WayPointerTarget == WayPointer.WayPointerTargetType.Monster).OrderBy(wp => wp.WayPointerNumber).ToList();
        bossWayPointerList = allWayPointerList.Where(item => item.WayPointerTarget == WayPointer.WayPointerTargetType.Boss).OrderBy(wp => wp.WayPointerNumber).ToList();

        for (int i = 0; i < allWayPointerList.Count; i++)
        {
            allWayPointerList[i].IsShowInformation = true;
            allWayPointerList[i].Init();
        }

        characterWayPointerList.CheckHasSameWayPointerNumberWithSetNextIndex();
        monsterWayPointerList.CheckHasSameWayPointerNumberWithSetNextIndex();
        bossWayPointerList.CheckHasSameWayPointerNumberWithSetNextIndex();

        gameObject.Log($"Way Pointer 초기화 완료.");
    }

    [BoxGroup("Map Configure/MC/Way Pointer"), Button("Way Pointer Information ON, OFF", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
    void WayPointerInformationOnOff()
    {
        isShowWayPointer = !isShowWayPointer;

        WayPointerInformationOnOff(isShowWayPointer);
    }

    void WayPointerInformationOnOff(bool value)
    {
        isShowWayPointer = value;

        for (int i = 0; i < allWayPointerList.Count; i++)
            allWayPointerList[i].IsShowInformation = value;

        gameObject.Log($"Way Pointer Information {value} 완료.");
    }

    public WayPointer GetNextWayPointer(WayPointer wayPointer)
    {
        var wayPointerList = wayPointer.WayPointerTarget switch
        {
            WayPointer.WayPointerTargetType.Character => characterWayPointerList,
            WayPointer.WayPointerTargetType.Monster => monsterWayPointerList,
            WayPointer.WayPointerTargetType.Boss => bossWayPointerList,
            _ => allWayPointerList
        };

        return wayPointerList[wayPointer.NextPointerIndex];
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Spawn Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Map/M/Spawn Unit Data"), Button("Clear Spawn Unit Data", ButtonSizes.Large), GUIColor("@ExtensionClass.GuiCOLOR_Red")]
    void ClearSpawnUnitData()
    {
        spawnUnitDataList.Clear();
    }

    [BoxGroup("Map/M/Spawn Unit"), Button("Spawn Units", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
    public void SpawnUnits()
    {
        if (CurQuestData.Lv == 1)
        {
            // ! 테스트
            gameObject.Log($"{GameManager.Instance.CurUnitPresetData.MainUnitInstId}");
            gameObject.Log($"{characterSpawnLocatorList[0].transform.position}");
        }
        if (CurQuestData.Lv == 1)
        {
            var playingCharacter = ObjectManager.Instance.GetCharacter(GameManager.Instance.CurUnitPresetData.MainUnitInstId, characterSpawnLocatorList[0].transform.position, true);
            // playingCharacter.UnitStatData.Atk = 1;
            // playingCharacter.SkillDataList[0].ProjSpd = 10;
            // playingCharacter.SkillDataList[0].ProjTime = 5;
            // playingCharacter.SkillDataList[0].Range = 50;
            // playingCharacter.SkillDataList[0].IsHoming = true;
            // playingCharacter.SkillDataList[0].HomingTorque = 100;
        }

        StartCoroutine(SpawnUnitCo());
    }

    IEnumerator SpawnUnitCo()
    {
        // 퀘스트 진행 데이터 업데이트 => 유닛 소환 진행.
        CombatManager.Instance.CurQuestProcessData.IsCompletedUnitSummon = false;

        // 현재 레벨(웨이브)에 소환 가능한 유닛의 리스트를 작성.
        var summonableBossSpawnDataList = new List<UnitSpawnData>();
        foreach (var unitSpawnData in CurQuestData.BossSpawnDataList)
            if (unitSpawnData.IsSpawnWave(CurQuestData.Lv))
                summonableBossSpawnDataList.Add(unitSpawnData);
        summonableBossSpawnDataList = summonableBossSpawnDataList.GetWeightedRandomPickResult(CurQuestData.GetBossSpawnAmount());

        var summonableMonsterSpawnDataList = new List<UnitSpawnData>();
        foreach (var unitSpawnData in CurQuestData.UnitSpawnDataList)
            if (unitSpawnData.IsSpawnWave(CurQuestData.Lv))
                summonableMonsterSpawnDataList.Add(unitSpawnData);

        // 유닛 소환.
        int summonTotalCount = 0;
        var summonRepeatCount = CurQuestData.GetUnitSpawnAmount() / monsterSpawnLocatorList.Count;
        for (int i = 0; i < summonRepeatCount; i++)
        {
            // 각 웨이브 첫 소환시에 소환가능한 보스부터 소환.
            if (i == 0)
            {
                int randomIndex;
                foreach (var bossSpawnData in summonableBossSpawnDataList)
                {
                    randomIndex = Random.Range(0, bossSpawnLocatorList.Count);
                    var newBoss = ObjectManager.Instance.GetMonster(bossSpawnData.Id,
                          bossSpawnLocatorList[randomIndex].transform.position.GetRandomInsidePositionAsQuarterView(bossSpawnLocatorList[randomIndex].Range),
                          CurQuestData);
                }
            }

            foreach (var spawnLocator in monsterSpawnLocatorList)
            {
                // ? 로케이터를 번갈아가며 소환 가능한 목록에 있는 몬스터를 순서대로 소환한다.
                var newMonster = ObjectManager.Instance.GetMonster(summonableMonsterSpawnDataList[summonTotalCount % summonableMonsterSpawnDataList.Count].Id,
                    spawnLocator.GetRandomPositionInRange(), CurQuestData);

                // if (CurQuestData.Lv % 10 != 0)
                newMonster.UnitStatData.DetectRange = 0;
                // newMonster.UnitStatData.Def = 100000;
                // newMonster.UnitStatData.MaxHp = 1000000;
                // newMonster.UnitStatData.CurHp = 1000000;

                summonTotalCount++;
            }

            yield return waitSummonDelay;
        }

        // 퀘스트 진행 데이터 업데이트 => 유닛 소환 완료.
        CombatManager.Instance.CurQuestProcessData.IsCompletedUnitSummon = true;
    }
}

[Serializable]
public class UnitSpawnData : IWeightedRandomPickable
{
    [ShowInInspector] public DragonBonesUnit.UnitRole Role { get; set; }
    [ShowInInspector] public ObscuredString Id { get; set; }
    [ShowInInspector] public ObscuredFloat Weight { get; set; }
    [ShowInInspector] public ObscuredBool IsExceptOnRandomPick { get; set; } = false;
    [ShowInInspector] public ObscuredInt[] SpawnWaves { get; set; }

    public void InitRole()
    {
        Role = PlayFabManager.Instance.MonsterCatalogItemDictionary[Id].CustomData.ToJObject()[nameof(MonsterStatData.Role)].ToCachedEnum<DragonBonesUnit.UnitRole>();
    }

    public bool IsSpawnWave(int waveNum, int binaryMaxIndex = 25)
    {
        waveNum--;
        var powNum = (int)Math.Pow(2, waveNum % binaryMaxIndex);
        return (SpawnWaves[waveNum / binaryMaxIndex] & powNum) == powNum;
    }
}

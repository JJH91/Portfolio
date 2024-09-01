using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CombatManager : MonoSingleton<CombatManager>
{
    [Header("Map And Castle")]
    [SerializeField] GameObject map;
    [SerializeField] GameObject castle;

    [Header("Character List")]
    [SerializeField] Dictionary<GameObject, Character> characterGameObjectDictionary = new Dictionary<GameObject, Character>();
    public Dictionary<GameObject, Character> CharacterGameObjectDictionary { get => characterGameObjectDictionary; }
    [SerializeField] Dictionary<int, Character> characterViewIdDictionary = new Dictionary<int, Character>();
    public Dictionary<int, Character> CharacterViewIdDictionary { get => characterViewIdDictionary; }

    [Header("Monster List")]
    [SerializeField] List<Monster> monsterList = new List<Monster>();
    public List<Monster> MonsterList { get => monsterList; }
    [SerializeField] Dictionary<GameObject, Monster> monsterGameObjectDictionary = new Dictionary<GameObject, Monster>();
    public Dictionary<GameObject, Monster> MonsterGameObjectDictionary { get => monsterGameObjectDictionary; }
    [SerializeField] Dictionary<int, Monster> monsterViewIdDictionary = new Dictionary<int, Monster>();
    public Dictionary<int, Monster> MonsterViewIdDictionary { get => monsterViewIdDictionary; }

    WaitForSeconds wait1s;

    [Header("Wave Data")]
    [SerializeField] List<MonsterWaveData_SO> monsterWaveData;
    public List<MonsterWaveData_SO> MonsterWaveData { get => monsterWaveData; }
    [SerializeField] int currentWaveLevel = 0;
    public int CurrentWaveLevel { get => currentWaveLevel; set => currentWaveLevel = value; }
    private int currentWaveMonsterCount;

    public InGameStageInfo inGameStageInfo;
    public int currentCastleHP;
    public int maxCastleHP;

    public float currentTime;

    [Header("Goods Data")]
    public VirtualCurrencyPanel virtualCurrencyPanel;
    private int currentGold;
    public int CurrentGold { get => currentGold; set => currentGold = value; }

    [SerializeField] int startGold = 12;

    [Header("Summon Rate Data")]
    [SerializeField] List<SummonRateData_SO> summonRateData;
    public List<SummonRateData_SO> SummonRateData { get => summonRateData; }


    private void Awake()
    {
        MakeDestroyableInstance();

        wait1s = new WaitForSeconds(1f);
    }

    private void Start()
    {
        currentCastleHP = maxCastleHP;
        currentTime = MonsterWaveData[CurrentWaveLevel].waveTime;

        inGameStageInfo.ShowTime(currentTime);
        inGameStageInfo.ShowStageInfo(CurrentWaveLevel + 1);
        currentGold = startGold;
        virtualCurrencyPanel.UpdateGold(currentGold);

        RotateMapAndCameraForPlayer2();
        StartCoroutine(Test_SummonMonsterCo());
    }

    private void Update()
    {
        currentTime -= Time.deltaTime;
        inGameStageInfo.ShowTime(currentTime);

        if (currentTime <= 0f && CurrentWaveLevel == MonsterWaveData.Count - 1)
        {
            // �¸�
            inGameStageInfo.WinGame();
            return;
        }

        if (currentTime <= 0f)
        {
            CurrentWaveLevel++;
            currentTime = MonsterWaveData[CurrentWaveLevel].waveTime;

            inGameStageInfo.ShowStageInfo(CurrentWaveLevel + 1);
        }

    }

    public void HitCastleByMonster(int curCastleHp)
    {
        if (curCastleHp <= currentCastleHP)
        {
            currentCastleHP = curCastleHp;

            currentCastleHP -= 1;
            currentCastleHP = Mathf.Clamp(currentCastleHP, 0, maxCastleHP);
            inGameStageInfo.UpdateCastleHp(currentCastleHP, maxCastleHP);
            if (currentCastleHP <= 0)
            {
                // ���ӿ���
                inGameStageInfo.LoseGame();
            }
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Test *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void Test_AddGold1000()
    {
        CurrentGold += 1000;

        virtualCurrencyPanel.UpdateGold(currentGold);
    }

    IEnumerator Test_SummonMonsterCo()
    {
        yield return new WaitUntil(() => NetworkManager.Instance.IsScenLoadCompleted);

        if (!NetworkManager.Instance.IsMasterClient)
            yield break;

        while (NetworkManager.Instance.IsInRoom)
        {
            int secSpawnCount = Instance.MonsterWaveData[Instance.CurrentWaveLevel].secSpawnCount;
            for (int i = 0; i < secSpawnCount; i++)
            {
                if (NetworkManager.Instance.IsInRoom)
                {
                    if (currentWaveLevel == 4 || currentWaveLevel == 9)
                        ObjectManager.Instance.GetMonster("Monster_10003", 15f.GetRancomOutCircleVector2());
                    else
                        ObjectManager.Instance.GetMonster("Monster_10001", 15f.GetRancomOutCircleVector2());
                }
            }
            yield return wait1s;
        }
    }

    public void Test_Summon(string characterName)
    {
        var indexList = new List<int>();
        for (int i = 0; i < SummonManager.Instance.summonBlocks.Count; i++)
            if (SummonManager.Instance.summonBlocks[i].summonCount == 0)
                indexList.Add(i);

        if (indexList.Count == 0)
        {
            SceneAssistManager.Instance.ShowGameMessage("빈자리가 없어 소환이 불가능 합니다.");
            return;
        }

        var randIndex = Random.Range(0, indexList.Count);

        SummonManager.Instance.SummonRandomPlace(characterName + $"_{(int)NameManager.UnitRank.Mythical}", characterName,
                                                    SummonManager.Instance.RankColors[(int)NameManager.UnitRank.Mythical], (int)NameManager.UnitRank.Mythical);
        // SummonManager.Instance.SummonJoinSquard(SummonManager.Instance.summonBlocks[randIndex],
        //                                             characterName + $"_{(int)NameManager.UnitRank.Mythical}", characterName, 1,
        //                                             SummonManager.Instance.RankColors[(int)NameManager.UnitRank.Mythical], (int)NameManager.UnitRank.Mythical);

        // var character = ObjectManager.Instance.GetCharacter(characterName, Vector2.zero);
        // character.SetUnit(characterName, SummonManager.Instance.RankColors[(int)NameManager.UnitRank.Mythical], (int)NameManager.UnitRank.Mythical);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Rotate For P2 *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void RotateMapAndCameraForPlayer2()
    {
        if (NetworkManager.Instance.IsMasterClient)
            return;

        Camera.main.transform.rotation = Quaternion.Euler(0, 0, -180);
        map.transform.rotation = Quaternion.Euler(0, 0, -180);
        castle.transform.rotation = Quaternion.Euler(0, 0, -180);
    }
}

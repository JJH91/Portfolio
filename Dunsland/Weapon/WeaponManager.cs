using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using DragonBones;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;

public class WeaponManager : AddressableSerializedMonoBehavior
{
    [TitleGroup("Weapon Manager"), BoxGroup("Weapon Manager/WM", showLabel: false)]
    [BoxGroup("Weapon Manager/WM/Master Character"), ShowInInspector, ReadOnly] public Character MasterCharacter { get; set; }

    [BoxGroup("Weapon Manager/WM/State Machince"), ReadOnly] public StateMachine StateMachine { get; private set; }

    // Attack Delay Controll.
    [BoxGroup("Weapon Manager/WM/Attack Dealy Controll"), SerializeField, ReadOnly] float attackAnimationTotalTime;
    [BoxGroup("Weapon Manager/WM/Attack Dealy Controll"), SerializeField, ReadOnly] float attackAnimationPlayedTime;

    // Weapon Armature.
    [BoxGroup("Weapon Manager/WM/Weapon"), ShowInInspector, ReadOnly] public Weapon Weapon_L { get; private set; }
    [BoxGroup("Weapon Manager/WM/Weapon"), ShowInInspector, ReadOnly] public Weapon Weapon_R { get; private set; }

    // Weapon Skill.
    [TitleGroup("Weapon Skill"), BoxGroup("Weapon Skill/WS", showLabel: false)]
    [BoxGroup("Weapon Skill/WS/Weapon Skill Data"), SerializeField] List<SkillData> equipmentSkillDataList;
    SkillData EquipmentSkillData { get => MasterCharacter == null ? null : equipmentSkillDataList[MasterCharacter.WeaponIndex]; }
    [BoxGroup("Weapon Skill/WS/Bullet Skill Data"), SerializeField] List<List<SkillData>> bulletSkillDataNestedList;
    List<SkillData> BulletSkillDataList { get => MasterCharacter == null ? null : bulletSkillDataNestedList[MasterCharacter.WeaponIndex]; }
    [BoxGroup("Weapon Skill/WS/Bullet Skill Data"), ShowInInspector] public SkillData CurBulletSkillData { get; private set; }
    [BoxGroup("Weapon Skill/WS/Weapon Skill Chain Data"), SerializeField] List<WeaponSkillChainData> weaponSkillChainDataList;
    WeaponSkillChainData WeaponSkillChainData { get => MasterCharacter == null ? null : weaponSkillChainDataList[MasterCharacter.WeaponIndex]; }

    Coroutine initAnimationTimeCo;
    Coroutine weaponSkillChainCo;
    Coroutine delayWeaponSkillChainCo;
    Coroutine restartWeaponSkillChainCo;

    protected override void Awake()
    {
        StateMachine = GetComponent<StateMachine>();

        base.Awake();
    }

    protected override void Start()
    {
        CheckWeaponExist();

        base.Start();
    }

    void Update()
    {
        CoolDownSkill();
    }

    protected override void OnDisable()
    {
        Weapon_L = null;
        Weapon_R = null;

        base.OnDisable();
    }

    /**
     * --------------------------------------------------
     * Deactive On Map Load
     * --------------------------------------------------
     */

    protected override void SetDeactiveOnMapLoad() { }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *              * Init Weapon Manager *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void InitWeaponManager(Character character, WeaponStatModifierData[] weaponDataArr)
    {
        MasterCharacter = character;

        if (weaponDataArr != null)
        {
            equipmentSkillDataList = new List<SkillData>();
            bulletSkillDataNestedList = new List<List<SkillData>>();
            weaponSkillChainDataList = new List<WeaponSkillChainData>();

            foreach (var weaponData in weaponDataArr)
            {
                if (weaponData == null)
                    continue;

                equipmentSkillDataList.Add(weaponData.EquipmentSkillData);
                bulletSkillDataNestedList.Add(weaponData.BulletSkillDataList);
                // var weaponSkillChainData = PlayFabManager.Instance.SkillCatalogItemDictionary[$"{weaponData.Series} Weapon Skill Chain"]
                //                             .CustomData.DeserializeObject<WeaponSkillChainData>();
                // ! 포트폴리오 무기 변경시 시리즈가 바뀌면 스킬도 바뀌는 것 시연 전용 코드.
                var skillChainName = weaponData.CatalogItemId == "Steeled Rage Sniper Rifle" ? $"{weaponData.Series}2 Weapon Skill Chain" : $"{weaponData.Series} Weapon Skill Chain";
                var weaponSkillChainData = PlayFabManager.Instance.SkillCatalogItemDictionary[skillChainName]
                                            .CustomData.DeserializeObject<WeaponSkillChainData>();
                weaponSkillChainData.AssembleWeaponSkillData();
                weaponSkillChainDataList.Add(weaponSkillChainData);
            }
        }

        ChangeWeapon();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Change Weapon *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void ChangeWeapon()
    {
        // 이전 무기 비활성화.
        if (Weapon_L != null)
            Weapon_L.gameObject.SetActive(false);
        if (Weapon_R != null)
            Weapon_R.gameObject.SetActive(false);

        // 무기 탄환 지정.
        CurBulletSkillData = BulletSkillDataList[0];
        // .Where(data => data.SkillName == NameManager.SkillProjectilePrefabName.NormalBullet.ToCachedUncamelCaseString()).FirstOrDefault();

        // 무기 생성 및 장착.
        // 권총은 양손에 장착.
        var leftHandSlotTr = (MasterCharacter.ArmatureComponent.armature.GetSlot(NameManager.DragonBonesLayerName.LeftHandSlot.ToCachedUncamelCaseString()).display as GameObject).transform;
        var leftWeaponOrdterLayerTr = (MasterCharacter.ArmatureComponent.armature.GetSlot(NameManager.DragonBonesLayerName.LeftWeaponOrderLayer.ToCachedUncamelCaseString()).display as GameObject).transform;
        if (MasterCharacter.WeaponStatModData.EquipmentType == PlayFabManager.CatalogItemTag.Pistol)
        {
            // 무기 생성.
            Weapon_L = ObjectManager.Instance.GetWeapon(MasterCharacter.WeaponStatModData.EquipmentType);
            Weapon_R = ObjectManager.Instance.GetWeapon(MasterCharacter.WeaponStatModData.EquipmentType);

            // 무기 초기화.
            Weapon_L.InitWeapon(this, Weapon.WeaponSide.Left);
            Weapon_R.InitWeapon(this, Weapon.WeaponSide.Right);

            // 우측 무기 장착 및 정렬 슬롯.
            var rightHandSlotTr = (MasterCharacter.ArmatureComponent.armature.GetSlot(NameManager.DragonBonesLayerName.RightHandSlot.ToCachedUncamelCaseString()).display as GameObject).transform;
            var rightWeaponOrdterLayerTr = (MasterCharacter.ArmatureComponent.armature.GetSlot(NameManager.DragonBonesLayerName.RightWeaponOrderLayer.ToCachedUncamelCaseString()).display as GameObject).transform;

            EquipWeapon(Weapon_L, leftHandSlotTr, (int)(leftWeaponOrdterLayerTr.localPosition.z * -10000));
            EquipWeapon(Weapon_R, rightHandSlotTr, (int)(rightWeaponOrdterLayerTr.localPosition.z * -10000));
        }
        // 권총이 아닌 총은 왼손에 장착.
        else
        {
            // 무기 생성.
            Weapon_L = ObjectManager.Instance.GetWeapon(MasterCharacter.WeaponStatModData.EquipmentType);

            // 무기 초기화.
            Weapon_L.InitWeapon(this, Weapon.WeaponSide.Left);

            EquipWeapon(Weapon_L, leftHandSlotTr, (int)(leftWeaponOrdterLayerTr.localPosition.z * -10000));
        }
    }

    void EquipWeapon(Weapon weapon, UnityEngine.Transform parentTr, int sortingOrder)
    {
        weapon.transform.SetParent(parentTr);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localEulerAngles = Vector3.zero;
        weapon.ArmatureComponent.sortingOrder = sortingOrder;

        weapon.ArmatureComponent.armature.InvalidUpdate();
    }

    public void ChangeBulletSkill()
    {
        // TODO: 구현 필요.
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Skill Cool Down *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void CoolDownSkill()
    {
        foreach (var equipmentSkillData in equipmentSkillDataList)
            equipmentSkillData.DecreaseCooldownByDeltaTime(Time.deltaTime);

        foreach (var bulletSkillList in bulletSkillDataNestedList)
            foreach (var skillData in bulletSkillList)
                skillData.DecreaseCooldownByDeltaTime(Time.deltaTime);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                     * Attack *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // Used Character State Machine.
    public void Attack_SM()
    {
        if (MasterCharacter.CurUnitState != DragonBonesUnit.DragonBonesUnitState.Attack)
            return;

        CheckWeaponExist();

        Weapon_L.PlayAttackAnimationByTime(attackAnimationPlayedTime);
        if (Weapon_R)
            Weapon_R.PlayAttackAnimationByTime(attackAnimationPlayedTime);

        StartWeaponSkillChain();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Cease Attack *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // Used Character State Machine.
    public void CeaseAttack_SM()
    {
        SaveAttackAnimationPlayedTimeAndTotalTime();

        Weapon_L.StopAnimation();
        if (Weapon_R)
            Weapon_R.StopAnimation();

        SetIntervalLastAnimationPlayedTime();

        DelayWeaponSkillChain();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                * Check Weapon Exist *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // 무기가 사라지는 경우가 있음. null 체크하고 없으면 생성.
    void CheckWeaponExist()
    {
        if (MasterCharacter.WeaponStatModData.EquipmentType == PlayFabManager.CatalogItemTag.Pistol)
        {
            // 양 쪽의 무기 존재 확인.
            if (Weapon_L == null || Weapon_R == null)
            {
                // 남아 있는 무기 비활성화.
                if (Weapon_L != null)
                    Weapon_L.gameObject.SetActive(false);
                else if (Weapon_R != null)
                    Weapon_R.gameObject.SetActive(false);

                // 웨폰 매니저 초기화.
                InitWeaponManager(MasterCharacter, null);
            }
        }
        else
        {
            // 한 쪽의 무기 존재 확인.
            if (!Weapon_L)
                // 웨폰 매니저 초기화.
                InitWeaponManager(MasterCharacter, null);
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *          * Make Character Look At Target *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // 플레이어가 타겟을 가지고 있으면 타겟의 방향을 바라봄.
    // Used WeaponManager State Machine.
    public void MakeCharacterLookAtTarget()
    {
        if (MasterCharacter.TargetUnit != null)
        {
            if (MasterCharacter.Position.x < MasterCharacter.TargetUnit.Position.x)
            {
                if (MasterCharacter.CurLookingAt != DragonBonesUnit.UnitLookingAt.Right)
                    MasterCharacter.ArmatureFlipX();
            }
            else if (MasterCharacter.Position.x > MasterCharacter.TargetUnit.Position.x)
            {
                if (MasterCharacter.CurLookingAt != DragonBonesUnit.UnitLookingAt.Left)
                    MasterCharacter.ArmatureFlipX();
            }
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *             * Attack Control System *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // 공격이 끝나면, 기존 애니메이션의 이름을 초기화 및 진행값을 저장, 그리고 간격 값 초기화를 위한 전체 애니메이션 시간 값을 저장.
    void SaveAttackAnimationPlayedTimeAndTotalTime()
    {
        attackAnimationTotalTime = Weapon_L.GetAttackAnimationTotalTime();
        attackAnimationPlayedTime = Weapon_L.GetAttackAnimationPlayedTime();

        // 권총은 양 손으로 쏘므로, 전체 시간값의 절반.
        if (MasterCharacter.WeaponStatModData.EquipmentType == PlayFabManager.CatalogItemTag.Pistol)
            attackAnimationTotalTime *= 0.5f;
    }

    void SetIntervalLastAnimationPlayedTime()
    {
        if (initAnimationTimeCo != null)
        {
            StopCoroutine(initAnimationTimeCo);
            initAnimationTimeCo = null;
        }
        else
            initAnimationTimeCo = StartCoroutine(SetIntervalLastAnimationPlayedTimeCo());
    }

    IEnumerator SetIntervalLastAnimationPlayedTimeCo()
    {
        if (!gameObject.activeSelf) // 웨폰 매니저가 비활성화 될 때마다 오류가 떠서 액티브 체크함.
            yield break;

        while (attackAnimationPlayedTime != 0)
        {
            attackAnimationPlayedTime += Time.deltaTime;

            if (attackAnimationPlayedTime >= attackAnimationTotalTime)
                attackAnimationPlayedTime = 0;

            yield return null;
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *              * Weapon Skill System *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void StartWeaponSkillChain()
    {
        if (delayWeaponSkillChainCo != null)
            StopCoroutine(delayWeaponSkillChainCo);

        // Add the delayed time to maximum process time.
        if (WeaponSkillChainData.CurWeaponSkillIndex < WeaponSkillChainData.WeaponSkillDataList.Count)
        {
            WeaponSkillChainData.CurProgressTime += WeaponSkillChainData.CurContinuableTime;
            if (WeaponSkillChainData.CurProgressTime > WeaponSkillChainData.WeaponSkillDataList[WeaponSkillChainData.CurWeaponSkillIndex].InvkTime)
                WeaponSkillChainData.CurProgressTime = WeaponSkillChainData.WeaponSkillDataList[WeaponSkillChainData.CurWeaponSkillIndex].InvkTime;
        }

        weaponSkillChainCo = StartCoroutine(WeaponSkillChainCo());
    }

    IEnumerator WeaponSkillChainCo()
    {
        WeaponSkillData curWeaponSkillData;

        while (true)
        {
            curWeaponSkillData = WeaponSkillChainData.WeaponSkillDataList[WeaponSkillChainData.CurWeaponSkillIndex];

            // Wait until weapon skill chain restartable.
            if (WeaponSkillChainData.CurWeaponSkillIndex < WeaponSkillChainData.WeaponSkillDataList.Count)
            {
                WeaponSkillChainData.CurProgressTime += Time.deltaTime;

                if (WeaponSkillChainData.CurProgressTime >= curWeaponSkillData.InvkTime)
                {
                    // Set target unit.
                    MasterCharacter.SetTargetUnit(curWeaponSkillData.Target, curWeaponSkillData.TgtCond, curWeaponSkillData.TgtSort);

                    // Cast weapon skill.
                    ObjectManager.Instance.GetWeaponSkill(WeaponSkillChainData.WeaponSkillDataList[WeaponSkillChainData.CurWeaponSkillIndex])
                        .CastSkill_SM(MasterCharacter, GetWeaponSkillCastPosition(curWeaponSkillData, WeaponSkillChainData.CurWeaponSkillIndex));

                    // Update cur weapon skill index.
                    WeaponSkillChainData.CurWeaponSkillIndex++;

                    // When last weapon skill casted, start weapon skill chain restart coroutine.
                    if (WeaponSkillChainData.CurWeaponSkillIndex >= WeaponSkillChainData.WeaponSkillDataList.Count)
                        StartCoroutine(RestartWeaponSkillChainCo());
                }
            }

            yield return null;
        }
    }

    Vector2 GetWeaponSkillCastPosition(WeaponSkillData weaponSkillData, int curSkillIndex)
    {
        if (weaponSkillData.CastLoc == Skill.CastLocation.Unit)
            return MasterCharacter.Position;

        if (Weapon_R == null)
            return Weapon_L.MuzzleSlotTransform.position;
        else
            return curSkillIndex % 2 == 0 ? Weapon_L.MuzzleSlotTransform.position : Weapon_R.MuzzleSlotTransform.position;
    }

    void DelayWeaponSkillChain()
    {
        if (weaponSkillChainCo != null)
            StopCoroutine(weaponSkillChainCo);

        // Start delay coroutine when last weapon skill not casted.
        if (WeaponSkillChainData.CurWeaponSkillIndex < WeaponSkillChainData.WeaponSkillDataList.Count)
            delayWeaponSkillChainCo = StartCoroutine(DelayWeaponSkillChainCo());
    }

    IEnumerator DelayWeaponSkillChainCo()
    {
        WeaponSkillChainData.CurContinuableTime = 0;

        while (true)
        {
            WeaponSkillChainData.CurContinuableTime += Time.deltaTime;

            if (WeaponSkillChainData.CurContinuableTime >= WeaponSkillChainData.ContinuableDelay)
            {
                WeaponSkillChainData.CurProgressTime = 0;
                WeaponSkillChainData.CurContinuableTime = 0;
                WeaponSkillChainData.CurWeaponSkillIndex = 0;

                break;
            }

            yield return null;
        }
    }

    IEnumerator RestartWeaponSkillChainCo()
    {
        while (true)
        {
            WeaponSkillChainData.CurReStartTime += Time.deltaTime;

            if (WeaponSkillChainData.CurReStartTime >= WeaponSkillChainData.RestartDelay)
            {
                WeaponSkillChainData.CurReStartTime = 0;
                WeaponSkillChainData.CurProgressTime = 0;
                WeaponSkillChainData.CurWeaponSkillIndex = 0;

                break;
            }

            yield return null;
        }
    }
}

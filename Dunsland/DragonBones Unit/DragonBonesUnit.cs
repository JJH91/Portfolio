using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.VisualScripting;
using DragonBones;
using Sirenix.OdinInspector;

public abstract class DragonBonesUnit : AddressableSerializedMonoBehavior, IDragonBonesUnit
{
    public enum DragonBonesUnitType { Character, Buddy, Monster, Npc, Weapon }
    public enum DragonBonesUnitState { Idle, Move, Attack, Skill, Damaged, Defeat, Revive, Action, Cast }
    public enum DragonBonesBoneName { root, Left_Hand_Ikt, Right_Hand_Ikt, Gun_Body }
    public enum DragonBonesSlotName { Left_Hand_Slot, Right_Hand_Slot, Muzzle_Slot }
    public enum UnitLookingAt { Left, Right }
    public enum UnitRole { Warrior, Archer, Mage, Cleric, Assassin, Terminator }

    // 테스트 메소드 변수
    [BoxGroup("Test")] public bool isTest;

    [TitleGroup("DragonBones Unit"), BoxGroup("DragonBones Unit/DU", showLabel: false)]
    [BoxGroup("DragonBones Unit/DU/Unit Type"), ShowInInspector] public DragonBonesUnitType UnitType { get; protected set; }
    [BoxGroup("DragonBones Unit/DU/Unit Stat Data"), ShowInInspector] public virtual UnitStatData UnitStatData { get; private set; }
    [BoxGroup("DragonBones Unit/DU/Is State Machine Inited"), ShowInInspector] public bool IsStateMachineInited { get; set; }
    [BoxGroup("DragonBones Unit/DU/Unity Armature Component"), ShowInInspector] public UnityArmatureComponent ArmatureComponent { get; private set; }
    [BoxGroup("DragonBones Unit/DU/Unit State"), ShowInInspector] public DragonBonesUnitState CurUnitState { get; protected set; }
    [BoxGroup("DragonBones Unit/DU/Unit Status Indicator"), SerializeField] protected UnitStatusIndicator unitStatusIndicator;
    [BoxGroup("DragonBones Unit/DU/Move"), SerializeField] protected float originStoppingDistance;
    [BoxGroup("DragonBones Unit/DU/Move"), SerializeField] protected float steeringMagnitude;
    [BoxGroup("DragonBones Unit/DU/Move"), SerializeField] protected NavMeshPath curNavMeshPath;
    [BoxGroup("DragonBones Unit/DU/Move"), SerializeField] protected Vector2 curDestinationVec2;

    // [BoxGroup("DragonBones Unit/DU/DragonBones Offset Data"), SerializeField] protected DragonBonesOffsetData dragonBonesOffsetData;
    // [BoxGroup("DragonBones Unit/DU/DragonBones Offset Data"), SerializeField] protected bool isOffsetInited;
    UnitLookingAt preLookingAt; // 이전에 바라보던 방향
    [BoxGroup("DragonBones Unit/DU/DragonBones Transform Flip Setting", order: 1), SerializeField] protected bool hasFlipAnimation; // _Flip 이 붙는 애니메이션이 있는지 여부
    [SerializeField, HideInInspector] UnitLookingAt standardLookingAt; // 기준값
    [BoxGroup("DragonBones Unit/DU/DragonBones Transform Flip Setting"), ShowInInspector] protected UnitLookingAt StandardLookingAt { get => standardLookingAt; set { standardLookingAt = value; CurLookingAt = value; } }
    [BoxGroup("DragonBones Unit/DU/DragonBones Transform Flip Setting", order: 1), SerializeField] UnitLookingAt curLookingAt; // 현재 바라보는 방향
    public UnitLookingAt CurLookingAt { get => curLookingAt; protected set => curLookingAt = value; }

    DragonBones.Transform dragonBonesRootTr; // 드래곤본 root 위치좌표 (SetDragonBonesTrCenterPos 에서 초기화)
    [BoxGroup("DragonBones Unit/DU/DragonBones Transform Position Setting", order: 1), SerializeField] Vector2 rootOffset; // 유니티 엔진의 트랜스폼과 드래곤본의 트랜스폼의 중심을 맞추는 좌표 변수
    public Vector2 RootOffset { get => rootOffset; }
    [BoxGroup("DragonBones Unit/DU/DragonBones Transform Position Setting"), ShowInInspector, ReadOnly] public Vector2 Position { get { return (Vector2)transform.position + RootOffset; } } // 유닛 중앙 좌표 보정(드래곤본 유닛의 좌표값이 필요할 때, 유닛의 중앙값을 주기위함)


    [TitleGroup("Target"), BoxGroup("Target/T", showLabel: false)]
    [BoxGroup("Target/T/Target Unit"), ShowInInspector] public DragonBonesUnit TargetUnit { get; protected set; }
    [BoxGroup("Target/T/Target Unit"), SerializeField] protected DragonBonesUnit tempTarget;

    // 스킬
    [TitleGroup("Skill"), BoxGroup("Skill/S", showLabel: false)]
    [BoxGroup("Skill/S/Skill"), ShowInInspector, ReadOnly] protected float MinNeedMP { get; set; } = float.MaxValue;
    [BoxGroup("Skill/S/Skill"), ShowInInspector, ReadOnly] public int CurSkillIndex { get; protected set; }
    [BoxGroup("Skill/S/Skill"), ShowInInspector, ReadOnly] public SkillData CurSkillData { get; protected set; }
    [BoxGroup("Skill/S/Activate Skill"), ShowInInspector, ReadOnly] public List<Skill> ActivateSkillList { get; protected set; } = new List<Skill>();
    [BoxGroup("Skill/S/Skill List"), TableList, SerializeField] List<SkillData> unitSkillList = new List<SkillData>();
    public List<SkillData> UnitSkillList { get => unitSkillList; set => unitSkillList = value; }

    // 컴포넌트
    [ReadOnly] protected StateMachine stateMachine;
    protected BoxCollider2D boxCollider2D;
    protected CapsuleCollider2D capsuleCollider2D;
    protected Rigidbody2D rigidbody2D;
    protected NavMeshAgent navMeshAgent;

    // 장애물 조사 레이저 변수
    protected Ray2D ray2D;
    protected RaycastHit2D raycastHit2D;
    protected int layerMask = 0;

    public event Action OnDamagedAct;
    public event Action OnUnitActivatedAct;
    public event Action OnUnitDefeatedAct;

    protected WaitUntil waitUntilActivateSkillZero;

    protected override void Awake()
    {
        ArmatureComponent = GetComponent<UnityArmatureComponent>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        capsuleCollider2D = GetComponent<CapsuleCollider2D>();
        rigidbody2D = GetComponent<Rigidbody2D>();
        stateMachine = GetComponent<StateMachine>();

        // 생성되자 마자 공격 받은 경우 스테이트 머신이 활성화 되기전에 유닛의 Hp가 0이 될 수 있음.
        // 해당 경우에는 유닛이 고장남. 그것을 방지 하지위해 Awake에서 콜라이더를 비활성화 시키고, 스테이트 머신이 활성화되면 콜라이더도 활성화함.
        ColliderOnOff(false);

        // 3D => 2D 로 변환한거라 XY 축으로 렌더러를 유지하기 위해서는 false를 해줘야함
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.updateRotation = false;
        navMeshAgent.updateUpAxis = false;
        originStoppingDistance = navMeshAgent.stoppingDistance;
        curNavMeshPath = new NavMeshPath();

        base.Awake();
    }

    protected override void Start()
    {
        InitDragonBonesTrCenterPos();

        preLookingAt = CurLookingAt;

        // Init armature sorting mode. If isn't init this, it cause sorting problem between unit and map.
        ArmatureComponent.sortingMode = SortingMode.SortByZ;
        ArmatureComponent.sortingMode = SortingMode.SortByOrder;

        // Init waitUntil.
        waitUntilActivateSkillZero = new WaitUntil(() => ActivateSkillList.Count == 0);

        OnUnitActivatedAct += UnitRevive;
        OnUnitActivatedAct += InitAllSkillsCooldown;

        OnUnitDefeatedAct += OnHpZero;

        base.Start();
    }

    protected override void OnEnable()
    {
        StartCoroutine(WaitforLoadingCo());

        navMeshAgent.enabled = true;
        navMeshAgent.ResetPath();

        OnUnitActivatedAct?.Invoke();

        base.OnEnable();
    }

    protected virtual void Update()
    {
        UpdateMpHealing();
        UpdateSkillCoolDown();
    }

    protected override void OnDisable()
    {
        StopAllCoroutines();

        navMeshAgent.enabled = false;
        IsStateMachineInited = false;
        TargetUnit = null;

        // OnEnable 되면 UnitStatusIndicator 는 강제 활성화된 것으로 간주되어 바로 비활성화 됨, 그러므로 InitUnit 에서 받아올 수 있도록 초기화
        unitStatusIndicator = null;

        base.OnDisable();
    }

    IEnumerator WaitforLoadingCo()
    {
        // 전체 로딩 대기 및 스테이트 머신 초기화 대기.
        yield return new WaitUntil(() => !(GameManager.Instance.IsNowSceneLoading || GameManager.Instance.IsNowLoading || ArmatureComponent == null));
        yield return new WaitUntil(() => IsStateMachineInited);

        ColliderOnOff(true);

        stateMachine.TriggerUnityEvent(DragonBonesUnitState.Idle.ToCachedString());
    }

    public abstract string GetAnimationNameWithFlipChecking_SM(string animationName);
    public abstract Vector2 GetSkillInitialPosition(Skill skill, bool isProjectileSkill, bool isAimToField);

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Test *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Test"), Button("정렬 값 변경")]
    public void Test1()
    {
        ArmatureComponent.sortingMode = SortingMode.SortByOrder;
        ArmatureComponent.armature.InvalidUpdate();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *      * DragonBones Transform Center Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // 드래곤본 루트 좌표 리셋
    [BoxGroup("DragonBones Unit/DU/DragonBones Transform Position Setting"), Button("Init DragonBones Transform Center Position", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Blue")]
    void InitDragonBonesTrCenterPos()
    {
        if (ArmatureComponent == null)
            ArmatureComponent = GetComponent<UnityArmatureComponent>();

        dragonBonesRootTr = ArmatureComponent.armature.GetBone(DragonBonesBoneName.root.ToCachedString()).boneData.transform;

        dragonBonesRootTr.x = 0;
        dragonBonesRootTr.y = 0;

        ArmatureComponent.armature.InvalidUpdate();
    }

    // 유니티 엔진의 트랜스폼과 드래곤본의 트랜스폼의 중심을 맞추는 함수
    [BoxGroup("DragonBones Unit/DU/DragonBones Transform Position Setting"), Button("Find DragonBones Transform Center Position", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
    void SetDragonBonesTrCenterPos()
    {
        if (isTest)
            Awake();

        dragonBonesRootTr = ArmatureComponent.armature.GetBone(DragonBonesBoneName.root.ToCachedString()).boneData.transform;

        dragonBonesRootTr.x = -RootOffset.x;
        dragonBonesRootTr.y = RootOffset.y;

        ArmatureComponent.armature.InvalidUpdate();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Init Unit *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public virtual void InitUnit()
    {
        ArmatureComponent.sortingLayerName = NameManager.SortingLayerName.Unit.ToCachedString();

        InitAllSkillsCooldown();
    }
    // public abstract void InitUnit(DragonBonesOffsetData dragonBonesOffsetData);

    // ! Deprecated. 드래곤본 팩토리를 사용하여 로드하는 방식을 새로 구현해봤지만, 퍼포먼스 이득은 없고 로직 변경사항만 많아짐.
    // protected void InitOffSet(DragonBonesOffsetData dragonBonesOffsetData)
    // {
    //     if (isOffsetInited)
    //         return;

    //     this.dragonBonesOffsetData = dragonBonesOffsetData;

    //     CurLookingAt = dragonBonesOffsetData.standardLookingAt;
    //     SetDragonBonesTrCenterPos();
    //     transform.localScale = dragonBonesOffsetData.scaleVector;

    //     isOffsetInited = true;
    // }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *            * DragonBones Unit Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // Used Character, Monster
    public void ChangeUnitState_SM(DragonBonesUnitState unitState)
    {
        CurUnitState = unitState;
    }

    [TitleGroup("Pause"), BoxGroup("Pause/P", showLabel: false)]
    [Button("Pause", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    public void Puase()
    {
        if (CombatManager.Instance.IsPaused)
        {
            ArmatureComponent.animation.timeScale = 0;
            navMeshAgent.speed = 0;
        }
        else
        {
            ArmatureComponent.animation.timeScale = 1;
            navMeshAgent.speed = UnitStatData.Spd;
        }
    }

    public void GetDamage(Skill skill)
    {
        if (!gameObject.activeSelf || CurUnitState == DragonBonesUnitState.Defeat)
            return;

        var attackerUnitStatData = skill.SkillCastUnit.UnitStatData;

        // TODO: 유닛 감속

        // 크리티컬 확률 초기화
        var isCritical = 0;
        var criticalChecker = UnityEngine.Random.Range(0, 1f);
        if (criticalChecker <= attackerUnitStatData.CrtRate_1)
            isCritical = 1;

        // 데미지 계수 초기화
        int skillFixedDamage = 0;
        float skillDamageX = 1;
        switch (skill.CurSkillState)
        {
            case Skill.State.Projectile:
                skillDamageX = skill.SkillData.ProjDmgX_0;
                skillFixedDamage = skill.SkillData.ProjFixDmg;
                break;

            case Skill.State.Impact:
                skillDamageX = skill.SkillData.ImptDmgX_0;
                skillFixedDamage = skill.SkillData.ImptFixDmg;
                break;

            case Skill.State.Stream:
                skillDamageX = skill.SkillData.StrmDmgX_0;
                skillFixedDamage = skill.SkillData.StrmFixDmg;
                break;
        }

        // 데미지 계산
        var damageBaseValue = skill.SkillData.DamageBase == Skill.DamageBase.Atk ? attackerUnitStatData.Atk : attackerUnitStatData.Dps;
        var damage = damageBaseValue * skillDamageX
                    * Math.Min(1, (float)Math.Log10(Math.Pow(damageBaseValue / (UnitStatData.Def + 1), 0.9) + 1)) * (1 + isCritical * attackerUnitStatData.CrtDmg_0);
        damage *= 1 + isCritical * attackerUnitStatData.CrtDmg_0;
        damage += skillFixedDamage;
        damage = Mathf.RoundToInt(damage);

        // 데미지 적용
        if (damage != 0)
        {
            UnitStatData.CurHp -= damage;

            OnDamagedAct?.Invoke();
            ObjectManager.Instance.GetDamageIndicator().ShowDamage(Position, damage, isCritical);

            if (UnitStatData.CurHp == 0)
            {
                OnUnitDefeatedAct?.Invoke();
                // StartCoroutine(HpZeroCo());
            }
        }
    }

    public void GetDamage(IEnumerator DamageIEnumerator)
    {
        // TODO: HpZeroCo 실행을 할 수 있도록 해야함... // ??? 안 건든지 오래되어서 까먹음, 아마 데미지 계산식을 코루틴으로 넘겨주는 방식인데...
        StartCoroutine(DamageIEnumerator);
    }

    // TODO: 이거 ㅅ1발 자꾸 버그나서 빡쳐서 코루틴화 해버렸는데... 안 죽는 버그 고쳐야해;;;
    IEnumerator HpZeroCo()
    {
        while (true)
        {
            if (CurUnitState != DragonBonesUnitState.Defeat)
                OnUnitDefeatedAct?.Invoke();
            yield return null;
        }
    }

    public virtual void UnitRevive()
    {
        UnitStatData.CurHp = UnitStatData.MaxHp;
        UnitStatData.CurMp = UnitStatData.MaxMp;

        stateMachine.TriggerUnityEvent(DragonBonesUnitState.Revive.ToCachedString());

        ColliderOnOff(true);
    }

    protected abstract void OnHpZero();

    public void UpdateMpHealing()
    {
        UnitStatData.CurMp += Time.deltaTime * UnitStatData.MpRecy;

        if (UnitStatData.CurMp >= UnitStatData.MaxMp)
            UnitStatData.CurMp = UnitStatData.MaxMp;
    }

    protected void ColliderOnOff(bool onOff)
    {
        boxCollider2D.enabled = onOff;
        capsuleCollider2D.enabled = onOff;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Move Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    protected void SetUnitDestination(Vector2 position, bool isLookTargetUnit = false, bool isStoppingDistanceToOne = false, bool isRush = false)
    {
        if (navMeshAgent.pathPending)
            return;

        // Set destination.
        navMeshAgent.CalculatePath(position, curNavMeshPath);
        if (curNavMeshPath.status != NavMeshPathStatus.PathComplete)
            navMeshAgent.SetDestination(position);
        else
            navMeshAgent.SetPath(curNavMeshPath);

        // Change animation for flip.
        if (IsChangedUnitLookSide_SM(isLookTargetUnit ? TargetUnit : null) && hasFlipAnimation)
            if (CurUnitState == DragonBonesUnitState.Idle || CurUnitState == DragonBonesUnitState.Move)
                ArmatureComponent.animation.Play(GetAnimationNameWithFlipChecking_SM(ArmatureComponent.animation.lastAnimationName));

        // Control speed and stopping distance as quarter view.
        steeringMagnitude = navMeshAgent.steeringTarget.GetQuarterViewDirectionMagnitude(transform.position);
        navMeshAgent.speed = isRush && CurSkillData != null ? steeringMagnitude * CurSkillData.ProjSpd : steeringMagnitude * UnitStatData.Spd;
        navMeshAgent.stoppingDistance = isStoppingDistanceToOne ? steeringMagnitude : steeringMagnitude * originStoppingDistance;
    }

    protected void SetUnitDestination(Vector3 position, bool isLookTargetUnit = false, bool isStoppingDistanceToOne = false, bool isRush = false)
    {
        SetUnitDestination((Vector2)position, isLookTargetUnit, isStoppingDistanceToOne, isRush);
    }

    /// <summary>
    /// Update KdTree conditions of skill.
    /// </summary>
    public DragonBonesUnit GetTargetUnit(Skill.Target skillTarget, Skill.TargetingValueCondition targetingCondition, Skill.TargetingSortCondition ordering)
    {
        TargetUnit = null;

        // 타게팅 목록 생성, 타겟이 Self 인 경우 GetSkillPosition 에서 바로 자신의 위치 리턴함

        // KdTree 할당.
        KdTree<DragonBonesUnit> targetKdTree = null;
        switch (skillTarget)
        {
            case Skill.Target.Ally:
                targetKdTree = UnitType == DragonBonesUnitType.Character ? CombatManager.Instance.CharacterKdTree : CombatManager.Instance.MonsterKdTree;
                break;

            case Skill.Target.Enemy:
                targetKdTree = UnitType == DragonBonesUnitType.Character ? CombatManager.Instance.MonsterKdTree : CombatManager.Instance.CharacterKdTree;
                break;

            case Skill.Target.Both:
                targetKdTree = new KdTree<DragonBonesUnit>();
                targetKdTree.AddAll(CombatManager.Instance.CharacterList);
                targetKdTree.AddAll(CombatManager.Instance.MonsterList);
                targetKdTree.UpdatePositions();
                break;
        }

        // 조건에 따라 값 리턴
        if (targetingCondition == Skill.TargetingValueCondition.Distance)
        {
            if (ordering == Skill.TargetingSortCondition.Lowest)
                TargetUnit = targetKdTree.FindClosest(transform.position);
            else
                TargetUnit = targetKdTree.FindFarthest(transform.position);
        }
        else
        {
            if (ordering == Skill.TargetingSortCondition.Lowest)
                TargetUnit = targetKdTree.FindLowestValue(transform.position, targetingCondition);
            else
                TargetUnit = targetKdTree.FindHighestValue(transform.position, targetingCondition);
        }

        return TargetUnit;
    }

    public void SetTargetUnit(Skill.Target skillTarget, Skill.TargetingValueCondition targetingCondition, Skill.TargetingSortCondition ordering)
    {
        TargetUnit = GetTargetUnit(skillTarget, targetingCondition, ordering);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Skill Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Skill/S/Skill"), Button("Init Skill List", ButtonSizes.Gigantic), GUIColor(0.35f, 0.7f, 1)]
    void InitSkillList()
    {
        // ! 스킬의 이름과 유닛의 애니메이션을 일치시키던 때의 메소드, 현재에는 맞지 않는 방식이지만, 일단 수정 보류.

        Awake();

        List<SkillData> preSkillList;

        if (UnitSkillList.Count > 0)
            preSkillList = new List<SkillData>(UnitSkillList);
        else
            preSkillList = new List<SkillData>();

        List<string> skillNameList = ArmatureComponent.animation.animationNames;
        List<string> exceptSkillNameList = new List<string>() { "Idle", "Idle_Flip", "Move", "Move_Flip", "Damaged", "Damaged_Flip", "Defeat", "Defeat_Flip" };

        for (int i = skillNameList.Count - 1; i >= 0; i--) // 스킬 목록에서 제외
            if (exceptSkillNameList.Contains(skillNameList[i]))
                skillNameList.RemoveAt(i);
            else if (hasFlipAnimation && skillNameList[i].Split('_')[skillNameList[i].Split('_').Length - 1] == "Flip") // _Flip 제거
                skillNameList.RemoveAt(i);


        UnitSkillList.Clear();
        MinNeedMP = 10000; // 최소 요구 포인트 찾기위해 일시적으로 값 지정

        // 기존 스킬 값 복사
        for (int i = 0; i < skillNameList.Count; i++)
        {
            SkillData skillData = new SkillData { AniName = skillNameList[i] };

            for (int j = 0; j < preSkillList.Count; j++)
            {
                if (skillNameList[i] == preSkillList[j].AniName)
                {
                    skillData = preSkillList[j];
                    break;
                }
            }

            if (skillData.Mp < MinNeedMP) // 최소 요구 MP 업데이트
                MinNeedMP = skillData.Mp;

            UnitSkillList.Add(skillData);
        }

        UnitSkillList.Sort((act1, act2) => act1.AniName.CompareTo(act2.AniName));

        gameObject.Log($"{gameObject.name} 스킬 리스트 초기화 완료 / 필요없는 스킬 제거");
    }

    [BoxGroup("Skill/S/Extract Skill List To JSON"), Button("Extract Skill List To JSON", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    void ExtractSkillListToJSON()
    {
        foreach (var data in UnitSkillList)
            data.ExtractSkillDataToJSON();
    }

    protected void InitAllSkillsCooldown()
    {
        if (UnitSkillList != null)
            foreach (var skillData in UnitSkillList)
                skillData?.SetCooldownToZero();

    }

    protected void UpdateSkillCoolDown()
    {
        if (UnitSkillList != null)
            foreach (var skillData in UnitSkillList)
                skillData?.DecreaseCooldownByDeltaTime(Time.deltaTime);
    }

    // 스킬 사용을 시작할때(애니메이션) 프레임 이벤트로 실행
    public void ReadyForUsingSkill()
    {
        // gameObject.Log($"스킬 사용 준비 - 유닛 정지");
        navMeshAgent.speed = 0;

        UnitStatData.CurMp -= CurSkillData.Mp;
        CurSkillData.SetCooldownToMax();
    }

    // 스킬 스테이트를 빠져나오면 현재 스킬을 초기화
    // Used Character, Monster
    public void OnExitSkillState_SM()
    {
        CurSkillData = null;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *            * DragonBones Flip Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // Used Character, Monster
    public bool IsChangedUnitLookSide_SM(DragonBonesUnit standardTargetUnit)
    {
        // if (standardTargetUnit != null && (CurUnitState == DragonBonesUnitState.Attack || CurUnitState == DragonBonesUnitState.Skill))
        if (standardTargetUnit != null)
        {
            if (standardTargetUnit.Position.x < Position.x) // 봐야할 방향이 왼쪽
            {
                if (CurLookingAt != UnitLookingAt.Left) // 현재 보는 방향이 왼쪽이 아니라면
                {
                    // gameObject.Log($"타겟 방향으로 플립: 좌측({standardTargetUnit.Position.x} < {Position.x} / {standardTargetUnit.Position.x < Position.x} / {CurLookingAt})");
                    ArmatureFlipX();
                }
                // else
                //     gameObject.Log($"타겟 방향 플립 유지: 현재 좌측({standardTargetUnit.Position.x} < {Position.x} / {standardTargetUnit.Position.x < Position.x} / {CurLookingAt})");
            }
            else if (standardTargetUnit.Position.x > Position.x) // 봐야할 방향이 오른쪽
            {
                if (CurLookingAt != UnitLookingAt.Right) // 현재 보는 방향이 오른쪽이 아니라면
                {
                    // gameObject.Log($"타겟 방향으로 플립: 우측({standardTargetUnit.Position.x} > {Position.x} / {standardTargetUnit.Position.x > Position.x} / {CurLookingAt})");
                    ArmatureFlipX();
                }
                // else
                //     gameObject.Log($"타겟 방향 플립 유지: 현재 우측({standardTargetUnit.Position.x} > {Position.x} / {standardTargetUnit.Position.x > Position.x} / {CurLookingAt})");
            }

            // ! 레이저
            // ray2D = new Ray2D(Position, (Vector2)standardTargetUnit.Position - Position);
            // ExtensionClass.DrawRay(ray2D.origin, ray2D.direction * Vector2.Distance((Vector2)standardTargetUnit.Position, Position), Color.red, 0.5f);
            // ray2D = new Ray2D(Position, (Vector2)navMeshAgent.destination - Position);
            // ExtensionClass.DrawRay(ray2D.origin, ray2D.direction * Vector2.Distance((Vector2)navMeshAgent.destination, Position), Color.blue, 0.5f);
        }
        else
        {
            if (navMeshAgent.destination.x < Position.x) // 봐야할 방향이 왼쪽
            // if (navMeshAgent.steeringTarget.x < Position.x) // 봐야할 방향이 왼쪽
            {
                if (CurLookingAt != UnitLookingAt.Left) // 현재 보는 방향이 왼쪽이 아니라면
                {
                    // gameObject.Log($"진행 방향으로 플립: 좌측({navMeshAgent.steeringTarget} / {navMeshAgent.destination} / {navMeshAgent.steeringTarget.x} < {Position.x} / {navMeshAgent.steeringTarget.x < Position.x} / {CurLookingAt})");
                    ArmatureFlipX();
                }
                // else
                //     gameObject.Log($"진행 방향 플립 유지: 현재 좌측({navMeshAgent.steeringTarget} / {navMeshAgent.destination} / {navMeshAgent.steeringTarget.x} < {Position.x} / {navMeshAgent.steeringTarget.x < Position.x} / {CurLookingAt})");
            }
            else if (navMeshAgent.destination.x > Position.x) // 봐야할 방향이 오른쪽
            // else if (navMeshAgent.steeringTarget.x > Position.x) // 봐야할 방향이 오른쪽
            {
                if (CurLookingAt != UnitLookingAt.Right) // 현재 보는 방향이 오른쪽이 아니라면
                {
                    // gameObject.Log($"진행 방향으로 플립: 우측({navMeshAgent.steeringTarget} / {navMeshAgent.destination} / {navMeshAgent.steeringTarget.x} > {Position.x} / {navMeshAgent.steeringTarget.x > Position.x} / {CurLookingAt})");
                    ArmatureFlipX();
                }
                // else
                //     gameObject.Log($"진행 방향 플립 유지: 현재 우측({navMeshAgent.steeringTarget} / {navMeshAgent.destination} / {navMeshAgent.steeringTarget.x} > {Position.x} / {navMeshAgent.steeringTarget.x > Position.x} / {CurLookingAt})");
            }

            // ! 레이저
            // ray2D = new Ray2D(Position, (Vector2)navMeshAgent.steeringTarget - Position);
            // ExtensionClass.DrawRay(ray2D.origin, ray2D.direction * Vector2.Distance((Vector2)navMeshAgent.steeringTarget, Position), Color.red, 0.5f);
            // ray2D = new Ray2D(Position, (Vector2)navMeshAgent.destination - Position);
            // ExtensionClass.DrawRay(ray2D.origin, ray2D.direction * Vector2.Distance((Vector2)navMeshAgent.destination, Position), Color.blue, 0.5f);
        }

        if (preLookingAt != CurLookingAt)
        {
            preLookingAt = CurLookingAt;

            return true;
        }

        return false;
    }

    public void ArmatureFlipX()
    {
        // 아마추어 플립 변경
        ArmatureComponent.armature.flipX = !ArmatureComponent.armature.flipX;

        // 드래곤본이 현재 바라보는 방향표시 변경
        if (CurLookingAt != UnitLookingAt.Left)
            CurLookingAt = UnitLookingAt.Left;
        else
            CurLookingAt = UnitLookingAt.Right;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *     * Compare Animation & Event Name Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    protected string ChangeSkillAnimationNameToCommonAnimationName(string animationName, bool isAfterCastPlayed)
    {
        // ? 마법 사용 유닛의 경우, 공통된 스킬 애니메이션을 사용하는 경우가 있음. 아래는 '스킬' 스테이트일 경우, 스킬에 해당하는 애니메이션이 없다면 공통 스킬 애니메이션을 재생하게 함.
        if (CurUnitState == DragonBonesUnitState.Skill)
            if (!ArmatureComponent.animation.animationNames.Contains(animationName))
            {
                if (!isAfterCastPlayed)
                {
                    if (ArmatureComponent.animation.animationNames.Contains(DragonBonesUnitState.Cast.ToCachedString()))
                        return DragonBonesUnitState.Cast.ToCachedString();
                    else
                        return DragonBonesUnitState.Skill.ToCachedString();
                }
                else
                {
                    if (!ArmatureComponent.animation.animationNames.Contains(animationName))
                        return DragonBonesUnitState.Skill.ToCachedString();
                }
            }

        return animationName;
    }

    protected bool IsSameAnimationName(string curAnimationStateName, System.Enum enumValue)
    {
        if (curAnimationStateName == enumValue.ToCachedString() || curAnimationStateName == enumValue.ToCachedString_Flip())
            return true;
        else
            return false;
    }

    protected bool IsSameAnimationOrEventName(string curAnimationOrEventName, DragonBonesUnit unit) // _Flip 애니메이션도 같은 이름으로 판단
    {
        if (unit.CurSkillData != null)
        {
            var skillAniName = unit.CurSkillData.AniName;
            if (unit.CurUnitState == DragonBonesUnitState.Skill)
                skillAniName = ChangeSkillAnimationNameToCommonAnimationName(skillAniName, false);

            if (curAnimationOrEventName == skillAniName || curAnimationOrEventName == skillAniName.ToCachedString_Flip())
                return true;
            else if (curAnimationOrEventName == DragonBonesUnitState.Skill.ToCachedString() && skillAniName == DragonBonesUnitState.Cast.ToCachedString())
                return true;
        }

        return false;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *     * Check State Machine Unit State Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // Used Character, Monster
    public bool IsValidUnitState_SM(DragonBonesUnitState unitState)
    {
        if (CurUnitState == unitState) return true;
        else return false;
    }
}

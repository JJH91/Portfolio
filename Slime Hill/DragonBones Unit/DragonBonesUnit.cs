using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.VisualScripting;
using BehaviorDesigner.Runtime;
using DragonBones;
using Sirenix.OdinInspector;
using System.Linq;

public abstract class DragonBonesUnit : AddressableSerializedMonoBehavior, IDragonBonesUnit
{
    public enum DragonBonesUnitType { Character, Buddy, Monster, Npc, Weapon }
    public enum DragonBonesUnitMode { Idle, Patrol, Combat }
    public enum DragonBonesUnitState { Idle, Move, Attack, Skill, Damaged, Defeat, Revive, Action, Cast }
    public enum DragonBonesBoneName { root, Left_Hand_Ikt, Right_Hand_Ikt, Gun_Body }
    public enum DragonBonesSlotName { Left_Hand_Slot, Right_Hand_Slot, Muzzle_Slot }
    public enum UnitLookingAt { Left, Right }
    public enum UnitRole { Warrior, Archer, Mage, Cleric, Assassin, Terminator }

    [TitleGroup("DragonBones Unit"), BoxGroup("DragonBones Unit/DU", showLabel: false)]
    [BoxGroup("DragonBones Unit/DU/Unit Type"), ShowInInspector] public DragonBonesUnitType UnitType { get; protected set; }
    [BoxGroup("DragonBones Unit/DU/Unit Stat Data"), ShowInInspector] public virtual UnitStatData UnitStatData { get; private set; }
    [BoxGroup("DragonBones Unit/DU/Is State Machine Inited"), ShowInInspector] public bool IsStateMachineInited { get; set; }
    [BoxGroup("DragonBones Unit/DU/Unit Status"), ShowInInspector] public DragonBonesUnitMode CurUnitMode { get; set; }
    [BoxGroup("DragonBones Unit/DU/Unit Status"), ShowInInspector] public DragonBonesUnitState CurUnitState { get; protected set; }
    [BoxGroup("DragonBones Unit/DU/Unit Status"), SerializeField] protected UnitStatusIndicator unitStatusIndicator;
    [BoxGroup("DragonBones Unit/DU/Move"), SerializeField] protected float originStoppingDistance;
    [BoxGroup("DragonBones Unit/DU/Move"), SerializeField] protected float steeringMagnitude;
    [BoxGroup("DragonBones Unit/DU/Move"), SerializeField] protected NavMeshPath curNavMeshPath;
    [BoxGroup("DragonBones Unit/DU/Move"), SerializeField] protected Vector2 curDestinationVec2;

    [TitleGroup("Weapon"), BoxGroup("Weapon/W", showLabel: false)]
    [BoxGroup("Weapon/W/Weapon"), ShowInInspector] public Weapon Weapon { get; protected set; }

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

    [TitleGroup("Way Pointer"), BoxGroup("Way Pointer/WP", showLabel: false)]
    [BoxGroup("Way Pointer/WP/Way Pointer"), ShowInInspector] public WayPointer PreWayPointer { get; set; }
    [BoxGroup("Way Pointer/WP/Way Pointer"), ShowInInspector] public WayPointer CurWayPointer { get; set; }

    [TitleGroup("Target"), BoxGroup("Target/T", showLabel: false)]
    [BoxGroup("Target/T/Target Unit"), ShowInInspector] public DragonBonesUnit TargetUnit { get; set; }

    // 스킬
    [TitleGroup("Skill"), BoxGroup("Skill/S", showLabel: false)]
    [BoxGroup("Skill/S/Skill"), ShowInInspector, ReadOnly] protected float MinNeedMP { get; set; } = float.MaxValue;
    [BoxGroup("Skill/S/Skill"), ShowInInspector, ReadOnly] public int CurSkillIndex { get; protected set; }
    [BoxGroup("Skill/S/Skill"), ShowInInspector, ReadOnly] public SkillData CurSkillData { get; protected set; }
    [BoxGroup("Skill/S/Activate Skill"), ShowInInspector, ReadOnly] public List<Skill> ActivateRefSkillList { get; protected set; } = new List<Skill>();
    [BoxGroup("Skill/S/Skill List"), ShowInInspector] public List<SkillData> SkillDataList { get; set; }
    [BoxGroup("Skill/S/Skill List"), ShowInInspector] public List<SkillData> AvailableSkillDataList { get; protected set; } = new List<SkillData>();

    // 컴포넌트
    [TitleGroup("Component"), BoxGroup("Component/C", showLabel: false)]
    [BoxGroup("Component/C/Component"), SerializeField] public UnityArmatureComponent armatureComponent;
    public UnityArmatureComponent ArmatureComponent { get => armatureComponent; }
    [BoxGroup("Component/C/Component"), SerializeField] protected StateMachine stateMachine;
    public StateMachine StateMachine { get => stateMachine; }
    [BoxGroup("Component/C/Component"), SerializeField] protected BehaviorTree behaviorTree;
    public BehaviorTree BehaviorTree { get => behaviorTree; }
    [BoxGroup("Component/C/Component"), SerializeField] protected BoxCollider2D boxCollider2D;
    [BoxGroup("Component/C/Component"), SerializeField] protected CapsuleCollider2D capsuleCollider2D;
    [BoxGroup("Component/C/Component"), SerializeField] protected Rigidbody2D rigidbody2D;
    [BoxGroup("Component/C/Component"), SerializeField] protected NavMeshAgent navMeshAgent;
    public NavMeshAgent NavMeshAgent { get => navMeshAgent; }

    int skillWeightRebalancingDivisor = -1;

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
        // 생성되자 마자 공격 받은 경우 스테이트 머신이 활성화 되기전에 유닛의 Hp가 0이 될 수 있음.
        // 해당 경우에는 유닛이 고장남. 그것을 방지 하지위해 Awake에서 콜라이더를 비활성화 시키고, 스테이트 머신이 활성화되면 콜라이더도 활성화함.
        ColliderOnOff(false);

        // 3D => 2D 로 변환한거라 XY 축으로 렌더러를 유지하기 위해서는 false를 해줘야함
        navMeshAgent.updateRotation = false;
        navMeshAgent.updateUpAxis = false;
        originStoppingDistance = navMeshAgent.stoppingDistance;
        curNavMeshPath = new NavMeshPath();

        BehaviorTree.StartWhenEnabled = false;
        BehaviorTree.RestartWhenComplete = true;

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
        waitUntilActivateSkillZero = new WaitUntil(() => ActivateRefSkillList.Count == 0);

        OnUnitActivatedAct += UnitRevive;
        OnUnitActivatedAct += InitAllSkillCooldown;

        OnUnitDefeatedAct += OnHpZero;
        if (CombatManager.Instance != null)
            OnUnitDefeatedAct += CombatManager.Instance.CheckQuestStepConditionCleared;

        AddDragonBonesAnimationEventListener();

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

    IEnumerator WaitforLoadingCo()
    {
        // 전체 로딩 대기 및 스테이트 머신 초기화 대기.
        yield return new WaitUntil(() => !(GameManager.Instance.IsNowSceneLoading || GameManager.Instance.IsNowLoading || ArmatureComponent == null));
        yield return new WaitUntil(() => IsStateMachineInited);

        ColliderOnOff(true);

        stateMachine.TriggerUnityEvent(DragonBonesUnitState.Idle.ToCachedString());

        yield return new WaitUntil(() => CurUnitState == DragonBonesUnitState.Idle);

        BehaviorTree.EnableBehavior();
    }

    protected virtual void Update()
    {
        UpdateSkillCoolDown();
    }

    protected override void OnDisable()
    {
        StopAllCoroutines();

        navMeshAgent.enabled = false;
        IsStateMachineInited = false;
        TargetUnit = null;

        // 유닛이 OnEnable 되면 아래의 오브젝트들은 강제 활성화된 것으로 간주되어 바로 비활성화 됨, 그러므로 InitUnit 에서 받아올 수 있도록 초기화.
        unitStatusIndicator = null;
        Weapon = null;

        if (CombatManager.Instance != null)
        {
            PreWayPointer = null;
            CurWayPointer = null;
        }

        base.OnDisable();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                     * Test *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [TitleGroup("Test"), BoxGroup("Test/T", showLabel: false)]
    [BoxGroup("Test/T/Test"), Button("비헤비어 트리 시작 1", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
    public void Test1()
    {
        BehaviorTree.Start();
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
            armatureComponent = GetComponent<UnityArmatureComponent>();

        dragonBonesRootTr = ArmatureComponent.armature.GetBone(DragonBonesBoneName.root.ToCachedString()).boneData.transform;

        dragonBonesRootTr.x = 0;
        dragonBonesRootTr.y = 0;

        ArmatureComponent.armature.InvalidUpdate();
    }

    // 유니티 엔진의 트랜스폼과 드래곤본의 트랜스폼의 중심을 맞추는 함수
    [BoxGroup("DragonBones Unit/DU/DragonBones Transform Position Setting"), Button("Find DragonBones Transform Center Position", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
    void SetDragonBonesTrCenterPos()
    {
        dragonBonesRootTr = ArmatureComponent.armature.GetBone(DragonBonesBoneName.root.ToCachedString()).boneData.transform;

        dragonBonesRootTr.x = -RootOffset.x;
        dragonBonesRootTr.y = RootOffset.y;

        ArmatureComponent.armature.InvalidUpdate();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Init Unit *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Component/C/Init"), Button("Init Components", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Blue")]
    public virtual void InitComponents()
    {
        armatureComponent = GetComponent<UnityArmatureComponent>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        capsuleCollider2D = GetComponent<CapsuleCollider2D>();
        rigidbody2D = GetComponent<Rigidbody2D>();
        stateMachine = GetComponent<StateMachine>();
        behaviorTree = GetComponent<BehaviorTree>();
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    public virtual void InitUnit()
    {
        ArmatureComponent.sortingLayerName = NameManager.SortingLayerName.Unit.ToCachedString();

        skillWeightRebalancingDivisor = SkillDataList.Count - SkillDataList.Where(data => data.IsExceptOnRandomPick).Count() - 1;
        InitAllSkillCooldown();

        // Set weapon and muzzle.
        Weapon ??= ObjectManager.Instance.GetWeapon(PlayFabManager.CatalogItemTag.Temp_Weapon, null);
        Weapon.transform.SetParent(transform);
        Weapon.SetWeaponMuzzleTransform(
            (ArmatureComponent.armature.GetSlot(NameManager.DragonBonesLayerName.MuzzleSlot.ToCachedUncamelCaseString()).display as GameObject).transform);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                       * AI *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void GoToCurrentWayPoint()
    {
        SetUnitDestination(CurWayPointer.GetRandomPositionInRange());
    }

    public void OnWayPointArrived(WayPointer wayPointer)
    {
        PreWayPointer = wayPointer;
        CurWayPointer ??= CombatManager.Instance.CurMap.GetNextWayPointer(wayPointer);

        // if (CurUnitMode == DragonBonesUnitMode.Patrol)
        //     GoToCurrentWayPoint();
    }

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
    [Button("Pause", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
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
        var roundToIntDamage = Mathf.RoundToInt(damage);

        // 데미지 적용
        if (roundToIntDamage != 0)
        {
            UnitStatData.CurHp -= roundToIntDamage;

            OnDamagedAct?.Invoke();
            ObjectManager.Instance.GetDamageIndicator().ShowDamage(Position + Vector2.up * rootOffset.y, roundToIntDamage, isCritical);

            if (UnitStatData.CurHp <= 0)
                OnUnitDefeatedAct?.Invoke();
        }
    }

    public virtual void UnitRevive()
    {
        UnitStatData.CurHp = UnitStatData.MaxHp;
        UnitStatData.CurMp = UnitStatData.MaxMp;

        stateMachine.TriggerUnityEvent(DragonBonesUnitState.Revive.ToCachedString());

        ColliderOnOff(true);
    }

    protected virtual void OnHpZero()
    {
        ColliderOnOff(false);
        BehaviorTree.DisableBehavior();

        if (CurUnitState != DragonBonesUnitState.Defeat)
        {
            StopAllCoroutines();
            navMeshAgent.ResetPath();

            if (UnitType == DragonBonesUnitType.Character)
                CombatManager.Instance.RemoveCharacter(this);
            else
                CombatManager.Instance.RemoveMonster(this);

            stateMachine.TriggerUnityEvent(DragonBonesUnitState.Defeat.ToCachedString());
        }

        // 사용한 스킬에 모두 활성화, 이미 활성화된 스킬은 영향이 없지만, 미처 활성화 되지못한 스킬들은 다음 로직으로 넘어가면서 유닛의 활성화 여부를 체크하게됨.
        foreach (var skill in ActivateRefSkillList)
            skill.ActivateSkill();
    }

    public void MpRecovery()
    {
        if (UnitStatData.CurMp < UnitStatData.MaxMp)
        {
            UnitStatData.CurMp += UnitStatData.MpRecy;

            if (UnitStatData.CurMp > UnitStatData.MaxMp)
                UnitStatData.CurMp = UnitStatData.MaxMp;
        }
    }

    protected void ColliderOnOff(bool onOff)
    {
        boxCollider2D.enabled = onOff;
        capsuleCollider2D.enabled = onOff;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                    * Animation *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // Used Character, Monster.
    public string GetAnimationNameWithFlipChecking_SM(string animationName)
    {
        return GetAnimationNameWithFlipChecking(animationName);
    }

    public virtual string GetAnimationNameWithFlipChecking(string animationName)
    {
        if (CurUnitState == DragonBonesUnitState.Skill)
            animationName = ChangeSkillAnimationNameToCommonAnimationName(animationName, false);

        // Flip 애니메이션이 있으면서 보는 방향이 기준값과 현재 값이 다르면 Flip Animation 이름을 리턴함
        if (hasFlipAnimation && StandardLookingAt != CurLookingAt)
            return animationName.ToCachedString_Flip();
        else
            return animationName;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Move *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    /// <summary>
    /// Using this method for teleport unit to target position.
    /// </summary>
    public void TeleportByTransform(Vector2 position)
    {
        navMeshAgent.enabled = false;
        transform.position = position;
        navMeshAgent.enabled = true;


        // TODO: 만약 캐릭터에 카메라를 붙여둘거면 조건 체크 후 활성화.
        // CombatManager.Instance.CmVcamCharacterFollower.transform.position = newPosition;
    }

    public void SetUnitDestination(Vector2 position, bool isLookTargetUnit = false, bool isStoppingDistanceToOne = false, bool isRush = false)
    {
        if (navMeshAgent.pathPending)
            return;

        // Set destination.
        navMeshAgent.CalculatePath(position, curNavMeshPath);
        if (navMeshAgent.CalculatePath(position, curNavMeshPath))
            navMeshAgent.SetPath(curNavMeshPath);
        else
            return;

        // Change animation for flip.
        if (IsChangedUnitLookSide_SM(isLookTargetUnit ? TargetUnit : null) && hasFlipAnimation)
            if (CurUnitState == DragonBonesUnitState.Idle || CurUnitState == DragonBonesUnitState.Move)
                ArmatureComponent.animation.Play(GetAnimationNameWithFlipChecking(ArmatureComponent.animation.lastAnimationName));

        // Control speed and stopping distance as quarter view.
        UpdateNavMeshAgentSpeedAndStoppingDistance(isStoppingDistanceToOne, isRush);

        stateMachine.TriggerUnityEvent(DragonBonesUnitState.Move.ToCachedString());
    }

    public void SetUnitDestination(Vector3 position, bool isLookTargetUnit = false, bool isStoppingDistanceToOne = false, bool isRush = false)
    {
        SetUnitDestination((Vector2)position, isLookTargetUnit, isStoppingDistanceToOne, isRush);
    }

    public void UpdateNavMeshAgentSpeedAndStoppingDistance(bool isStoppingDistanceToOne = false, bool isRush = false)
    {
        steeringMagnitude = navMeshAgent.steeringTarget.GetQuarterViewDirectionMagnitude(transform.position);
        navMeshAgent.speed = isRush && CurSkillData != null ? steeringMagnitude * CurSkillData.ProjSpd : steeringMagnitude * UnitStatData.Spd;
        navMeshAgent.stoppingDistance = isStoppingDistanceToOne ? steeringMagnitude : steeringMagnitude * originStoppingDistance;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Targeting *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public DragonBonesUnit GetTargetUnit(Skill.Target skillTarget, Skill.TargetingValueCondition targetingCondition, Skill.TargetingSortCondition ordering)
    {
        if (skillTarget == Skill.Target.Self)
            return this;

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

        // 조건에 따라 값 리턴.
        if (targetingCondition == Skill.TargetingValueCondition.Distance)
        {
            if (ordering == Skill.TargetingSortCondition.Lowest)
                return targetKdTree.FindClosest(transform.position);
            else
                return targetKdTree.FindFarthest(transform.position);
        }
        else
        {
            if (ordering == Skill.TargetingSortCondition.Lowest)
                return targetKdTree.FindLowestValue(transform.position, targetingCondition);
            else
                return targetKdTree.FindHighestValue(transform.position, targetingCondition);
        }
    }

    public DragonBonesUnit SetTargetUnit(Skill.Target skillTarget, Skill.TargetingValueCondition targetingCondition, Skill.TargetingSortCondition ordering)
    {
        TargetUnit = GetTargetUnit(skillTarget, targetingCondition, ordering);

        return TargetUnit;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                     * Skill *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Skill/S/Skill"), Button("Init Skill List", ButtonSizes.Gigantic), GUIColor(0.35f, 0.7f, 1)]
    void InitSkillList()
    {
        // ! 스킬의 이름과 유닛의 애니메이션을 일치시키던 때의 메소드, 현재에는 맞지 않는 방식이지만, 일단 수정 보류.

        Awake();

        List<SkillData> preSkillList;

        if (SkillDataList.Count > 0)
            preSkillList = new List<SkillData>(SkillDataList);
        else
            preSkillList = new List<SkillData>();

        List<string> skillNameList = ArmatureComponent.animation.animationNames;
        List<string> exceptSkillNameList = new List<string>() { "Idle", "Idle_Flip", "Move", "Move_Flip", "Damaged", "Damaged_Flip", "Defeat", "Defeat_Flip" };

        for (int i = skillNameList.Count - 1; i >= 0; i--) // 스킬 목록에서 제외
            if (exceptSkillNameList.Contains(skillNameList[i]))
                skillNameList.RemoveAt(i);
            else if (hasFlipAnimation && skillNameList[i].Split('_')[skillNameList[i].Split('_').Length - 1] == "Flip") // _Flip 제거
                skillNameList.RemoveAt(i);


        SkillDataList.Clear();
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

            SkillDataList.Add(skillData);
        }

        SkillDataList.Sort((act1, act2) => act1.AniName.CompareTo(act2.AniName));

        gameObject.Log($"{gameObject.name} 스킬 리스트 초기화 완료 / 필요없는 스킬 제거");
    }

    [BoxGroup("Skill/S/Extract Skill List To JSON"), Button("Extract Skill List To JSON", ButtonSizes.Gigantic), GUIColor(0.35f, 1, 0.7f)]
    void ExtractSkillListToJSON()
    {
        foreach (var data in SkillDataList)
            data.ExtractSkillDataToJSON();
    }

    protected void InitAllSkillCooldown()
    {
        if (SkillDataList != null)
            foreach (var skillData in SkillDataList)
                skillData?.SetCooldownToZero();
    }

    protected void UpdateSkillCoolDown()
    {
        if (SkillDataList != null)
            foreach (var skillData in SkillDataList)
                skillData?.DecreaseCooldownByDeltaTime(Time.deltaTime);
    }

    // 스킬 사용을 시작할때(애니메이션) 프레임 이벤트로 실행.
    public void ReadyForUsingSkill()
    {
        navMeshAgent.ResetPath();

        UnitStatData.CurMp -= CurSkillData.Mp;
        MpRecovery();
        CurSkillData.SetCooldownToMax();
    }

    // 스킬 스테이트를 빠져나오면 현재 스킬을 초기화.
    // Used Character, Monster.
    public void OnExitSkillState_SM()
    {
        CurSkillData = null;
    }

    public bool IsSkillAvailable(Weapon weapon, SkillData skillData)
    {
        // 스킬 사용할 대상의 active 확인
        if (TargetUnit == null || !TargetUnit.gameObject.activeSelf || skillData == null)
            return false;

        var distanceFromTarget = TargetUnit.Position.GetQuarterViewDistanceFrom(Position);

        if (distanceFromTarget > CurSkillData.Range)
            return false;
        else
        {
            // 직사 스킬 사용 가능 검사, 곡사는 거리만 되면 바로 스킬 사용 가능.
            if (skillData.Trajectory == Skill.Trajectory.Direct)
            {
                var quarterViewSkillRange = TargetUnit.Position.GetQuarterViewScalar(Position, CurSkillData.Range);

                // 직사 스킬사용 판단용 레이저
                layerMask = 1 << LayerMask.NameToLayer(NameManager.LayerName.Wall.ToCachedString());
                if (CurSkillData.Type == Skill.Type.Ranged && weapon.MuzzleSlotTransform != null)
                    ray2D = new Ray2D((Vector2)weapon.MuzzleSlotTransform.position, TargetUnit.Position - (Vector2)weapon.MuzzleSlotTransform.position);
                else
                    ray2D = new Ray2D(Position, TargetUnit.Position - Position);
                raycastHit2D = Physics2D.Raycast(ray2D.origin, ray2D.direction, quarterViewSkillRange, layerMask);

                // ! 레이저
                // ExtensionClass.DrawRay(ray2D.origin, ray2D.direction * raycastHit2D.distance, Color.red, 0.5f);
                // ExtensionClass.DrawRay(ray2D.origin, ray2D.direction * quarterViewSkillRange, Color.red, 0.5f);

                // 스킬의 타겟까지 레이저를 발사하여 중간에 장애물이 있다면 스킬사용 불가능 판정.
                // 중간에 벽이 있더라도 근접 공격 사거리의 절반에 해당하면 공격 가능하다고 판정.
                if (raycastHit2D && distanceFromTarget > CurSkillData.Range * 0.5)
                    return false;
            }

            return true;
        }
    }

    public void SelectWeightedRandomSkill()
    {
        // Get weighted random pick skill.
        CurSkillData = AvailableSkillDataList.GetWeightedRandomPickResult();

        // Weight rebalancing.
        if (skillWeightRebalancingDivisor > 0)
        {
            CurSkillData.Weight *= 0.5f;
            var rebalancingWeight = CurSkillData.Weight / skillWeightRebalancingDivisor;
            foreach (var skillData in SkillDataList)
            {
                if (skillData.IsExceptOnRandomPick || skillData == CurSkillData)
                    continue;

                skillData.Weight += rebalancingWeight;
            }
        }
    }

    public Vector2 GetSkillCastingPosition(Skill skill, bool isProjectileSkill, bool isAimToField)
    {
        // 자기자신의 위치에 사용하는 스킬, 혹은 타겟 유닛이 비활성화 상태이면 자신의 포지션 리턴.
        if (skill.SkillData.Target == Skill.Target.Self || !TargetUnit.IsUnitAlive())
            return isAimToField ? (Vector2)transform.position : Position;

        // 스킬 타입에 따른 포지션 리턴.
        var quarterViewSkillRange = TargetUnit.Position.GetQuarterViewScalar(Position, skill.SkillData.Range);
        switch (skill.SkillData.Type)
        {
            case Skill.Type.Melee:
                if (skill.SkillData.Target == Skill.Target.Ally)
                    layerMask = 1 << LayerMask.NameToLayer(UnitType.ToCachedString());
                else
                {
                    if (UnitType == DragonBonesUnitType.Character)
                        layerMask = 1 << LayerMask.NameToLayer(NameManager.LayerName.Monster.ToCachedString());
                    else // if (UnitType == DragonBonesUnitType.Monster)
                        layerMask = 1 << LayerMask.NameToLayer(NameManager.LayerName.Character.ToCachedString());
                }

                ray2D = new Ray2D(Position, TargetUnit.Position - Position);
                raycastHit2D = Physics2D.Raycast(ray2D.origin, ray2D.direction, quarterViewSkillRange, layerMask);

                // ! 레이저.
                // Debug.DrawRay(ray2D.origin, ray2D.direction * raycastHit2D.distance, Color.green, 0.5f);

                if (raycastHit2D) // range 내에 타겟이 있음.
                    return isAimToField ? TargetUnit.transform.position : raycastHit2D.point;
                else // 대상의 방향으로 range 만큼 떨어진 곳.
                    return Position + quarterViewSkillRange * (isAimToField ? TargetUnit.transform.position.GetNomalizedVector2From(Position) : TargetUnit.Position.GetNomalizedVector2From(Position));

            case Skill.Type.Ranged:
                if (isProjectileSkill)
                    return Weapon.MuzzleSlotTransform != null ? (Vector2)Weapon.MuzzleSlotTransform.position : Position;
                else
                    return isAimToField ? TargetUnit.transform.position : TargetUnit.Position;

            // 해당 경우는 없음, 오류 방지용.
            default:
                return Position;
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *            * DragonBones Flip Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // Used Character, Monster. => Deprecated in State Machine.
    public bool IsChangedUnitLookSide_SM(DragonBonesUnit standardTargetUnit)
    {
        if (!standardTargetUnit.IsUnitAlive())
            standardTargetUnit = null;

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
        // ? 또한, 'Cast'라는 애니메이션이 별도로 존재하면 Cast 애니메이션을 재생하도록 함. 이후 애니메이션 컴플리트 이벤트에서 Cast가 끝나면 스킬 애니메이션 재생을 시킴.
        if (CurUnitState == DragonBonesUnitState.Skill)
            if (!ArmatureComponent.animation.animationNames.Contains(animationName))
            {
                if (isAfterCastPlayed)
                {
                    if (!ArmatureComponent.animation.animationNames.Contains(animationName))
                        return DragonBonesUnitState.Skill.ToCachedString();
                }
                else
                {
                    if (ArmatureComponent.animation.animationNames.Contains(DragonBonesUnitState.Cast.ToCachedString()))
                        return DragonBonesUnitState.Cast.ToCachedString();
                    else
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
            var skillAniName = ChangeSkillAnimationNameToCommonAnimationName(unit.CurSkillData.AniName, false);

            if (curAnimationOrEventName == skillAniName || curAnimationOrEventName == skillAniName.ToCachedString_Flip())
                return true;
            else if (curAnimationOrEventName == DragonBonesUnitState.Skill.ToCachedString())
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

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *            * Animation Event Listener *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    protected virtual void AddDragonBonesAnimationEventListener()
    {
        ArmatureComponent.AddDBEventListener(EventObject.START, OnDragonBonesAnimationStartEventHandler);
        ArmatureComponent.AddDBEventListener(EventObject.COMPLETE, OnDragonBonesAnimationCompleteEventHandler);
        ArmatureComponent.AddDBEventListener(EventObject.FRAME_EVENT, OnDragonBonesAnimationFrameEventHandler);
    }

    protected virtual void OnDragonBonesAnimationStartEventHandler(string type, EventObject eventObject)
    {
        if (IsSameAnimationOrEventName(eventObject.animationState.name, this))
            ReadyForUsingSkill();
    }

    protected virtual void OnDragonBonesAnimationCompleteEventHandler(string type, EventObject eventObject)
    {
        if (IsSameAnimationName(eventObject.animationState.name, DragonBonesUnitState.Defeat))
        {
            if (ActivateRefSkillList.Count == 0)
                gameObject.SetActive(false);
            else
            {
                StopAllCoroutines();
                StartCoroutine(WaitUntilActivateSkillZeroCo());
            }
        }
    }

    IEnumerator WaitUntilActivateSkillZeroCo()
    {
        ArmatureComponent.sortingLayerName = NameManager.SortingLayerName.Default.ToCachedString();
        transform.position = Vector3.zero;

        yield return waitUntilActivateSkillZero;

        gameObject.SetActive(false);
    }

    protected virtual void OnDragonBonesAnimationFrameEventHandler(string type, EventObject eventObject)
    {

    }
}

[Serializable]
public class SharedDragonBonesUnit : SharedVariable<DragonBonesUnit>
{
    public static implicit operator SharedDragonBonesUnit(DragonBonesUnit value) { return new SharedDragonBonesUnit { Value = value }; }
}

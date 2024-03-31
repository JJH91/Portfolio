using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DragonBones;
using Sirenix.OdinInspector;

public class Monster : DragonBonesUnit
{
    public override UnitStatData UnitStatData { get => MonsterStatData; }

    [TitleGroup("Monster"), BoxGroup("Monster/M", showLabel: false)]
    [BoxGroup("Monster/M/Monster Stat Data"), ShowInInspector] public MonsterStatData MonsterStatData { get; set; }
    [BoxGroup("Monster/M/Muzzle"), SerializeField] int originNavPriority;
    [BoxGroup("Monster/M/Muzzle"), ShowInInspector] public UnityEngine.Transform MuzzleSlotTransform { get; set; }
    [BoxGroup("Monster/M/Distance"), SerializeField] float distanceFromTarget;
    [BoxGroup("Monster/M/Distance"), SerializeField] float quarterViewSkillRange;
    [BoxGroup("Monster/M/Distance"), SerializeField] Vector2 diffPositionVec2;
    [BoxGroup("Monster/M/AI Condition"), SerializeField] int skillUseFailureStack;
    [BoxGroup("Monster/M/AI Condition"), SerializeField] float attackInterval;
    [BoxGroup("Monster/M/AI Condition"), SerializeField] bool isConsiderNextBehavior;
    [BoxGroup("Monster/M/AI Condition"), SerializeField] bool isAccessToTargetCooldownDone;
    [BoxGroup("Monster/M/AI Condition"), SerializeField] bool isKeepProperDistanceCooldownDone;

    WaitForSeconds wait100ms = new WaitForSeconds(0.1f);
    WaitForSeconds wait1s = new WaitForSeconds(1);

    protected override void Awake()
    {
        UnitType = DragonBonesUnitType.Monster;

        base.Awake();
    }

    protected override void Start()
    {
        AddEventListenerForMonster();

        // Save nav data.
        originNavPriority = navMeshAgent.avoidancePriority;

        // Get muzzle slot tranform.
        var muzzleSlot = ArmatureComponent.armature.GetSlot(NameManager.DragonBonesLayerName.MuzzleSlot.ToCachedUncamelCaseString());
        if (muzzleSlot != null)
            MuzzleSlotTransform = (muzzleSlot.display as GameObject).transform;

        for (int i = 0; i < UnitSkillList.Count; i++)
            if (UnitSkillList[i].Mp < MinNeedMP)
                MinNeedMP = UnitSkillList[i].Mp;

        base.Start();
    }

    protected override void OnEnable()
    {
        if (CombatManager.Instance != null)
            CombatManager.Instance.AddMonster(this);

        isConsiderNextBehavior = true;
        isAccessToTargetCooldownDone = true;
        isKeepProperDistanceCooldownDone = true;

        base.OnEnable();
    }

    protected override void Update()
    {
        base.Update();

        if (attackInterval > 0)
            attackInterval -= Time.deltaTime;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Test *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [Button("새로운 방식의 스킬 레인지")]
    void Test()
    {
        StartCoroutine(TestCo());
    }

    IEnumerator TestCo()
    {
        while (isTest)
        {
            var layerMask = 1 << LayerMask.NameToLayer(NameManager.LayerName.Character.ToCachedString());
            var ray2D = new Ray2D(Position, TargetUnit.Position - Position);
            var raycastHit2D = Physics2D.Raycast(ray2D.origin, ray2D.direction, TargetUnit.Position.GetQuarterViewScalar(Position, 5), layerMask);

            Debug.DrawRay(ray2D.origin, ray2D.direction * raycastHit2D.distance, Color.blue, 0.1f);

            yield return wait100ms;
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *          * DragonBones Method Override *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    protected override void OnHpZero()
    {
        ColliderOnOff(false);

        if (CurUnitState != DragonBonesUnitState.Defeat)
        {
            StopAllCoroutines();
            navMeshAgent.speed = 0;
            CombatManager.Instance.RemoveMonster(this);
            stateMachine.TriggerUnityEvent(DragonBonesUnitState.Defeat.ToCachedString());
        }

        // 사용한 스킬에 모두 활성화, 이미 활성화된 스킬은 영향이 없지만, 미처 활성화 되지못한 스킬들은 다음 로직으로 넘어가면서 유닛의 활성화 여부를 체크하게됨.
        foreach (var skill in ActivateSkillList)
            skill.ActivateSkill();
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
        StopAllCoroutines();
        CombatManager.Instance.RemoveMonster(this);
        gameObject.SetActive(false);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Apply Unit Data *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public override void InitUnit()
    {
        base.InitUnit();

        // 스테이터스 바 업데이트 이벤트 구독은 오브젝트 매니저에서, 구독 해지는 스테이스바에서 함
        // if (unitStatusIndicator == null)
        //     ObjectManager.Instance.GetUnitStatusIndicator(this);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                    * Animation *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // Used Monster
    public override string GetAnimationNameWithFlipChecking_SM(string animationName)
    {
        animationName = animationName.ToCachedDragonBonesAniName();
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
     *                       * AI *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // TODO: BT로 마이그레이션 할까 생각중. 아래는 스탯 데이터로 바꾼뒤 데이터 적용이 안되서 생기는 버그 해결용 변수.
    WaitUntil waitUnitStatDataInit;

    public void ConsiderNextBehavior_SM()
    {
        waitUnitStatDataInit ??= new WaitUntil(() => UnitStatData != null);

        // 스테이트 머신에 서로 연결상태에 있지 않은 2개 이상의 플로우 머신을 가동할 수 있음.
        // 스테이트 머신에 AI Satate를 추가할 수 있지만, 퍼포먼스에 악영향이 있을 것같아서 코드로만 구현함.
        StartCoroutine(ConsiderNextBehaviorCo());
    }

    IEnumerator ConsiderNextBehaviorCo()
    {
        yield return waitUnitStatDataInit;

        StartCoroutine(ReachDestinationCo());

        while (true)
        {
            if (isConsiderNextBehavior && CurUnitState != DragonBonesUnitState.Damaged)
            {
                if (CurSkillData == null)
                {
                    navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

                    // 타겟이 없거나, 같은 편에게 스킬을 사용했을 경우, AI의 정상 작동을 위해 적을 타게팅함.
                    if (TargetUnit == null || TargetUnit is Monster)
                        GetTargetUnit(Skill.Target.Enemy, Skill.TargetingValueCondition.Distance, Skill.TargetingSortCondition.Lowest);

                    if (isKeepProperDistanceCooldownDone)
                    {
                        KeepProperDistance_SM();
                    }

                    if (attackInterval <= 0 && UnitStatData.CurMp >= MinNeedMP)
                    {
                        SelectRandomSkill_SM();
                    }
                }
                else
                {
                    if (CurUnitState != DragonBonesUnitState.Skill)
                    {
                        if (IsSkillAvailable(CurSkillData))
                        {
                            skillUseFailureStack = 0;
                            navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

                            stateMachine.TriggerUnityEvent(DragonBonesUnitState.Skill.ToCachedString());
                        }
                        else
                        {
                            if (isAccessToTargetCooldownDone)
                            {
                                skillUseFailureStack++;
                                navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

                                if (skillUseFailureStack <= 10)
                                {
                                    AccessToTarget_SM();
                                }
                                else
                                {
                                    skillUseFailureStack = 0;
                                    CurSkillData = null;
                                }
                            }
                        }
                    }
                }
            }

            yield return wait100ms;
        }
    }

    public void SelectRandomSkill_SM()
    {
        isConsiderNextBehavior = false;

        CurSkillIndex = UnityEngine.Random.Range(0, UnitSkillList.Count);

        // 사용할 스킬 설정
        if (UnitStatData.CurMp >= UnitSkillList[CurSkillIndex].Mp && UnitSkillList[CurSkillIndex].CurCd <= 0)
        {
            CurSkillData = UnitSkillList[CurSkillIndex];
            TargetUnit = GetTargetUnit(CurSkillData.Target, CurSkillData.TgtCond, CurSkillData.TgtSort);

            // ! 레이저
            // ray2D = new Ray2D(Position, (Vector2)navMeshAgent.destination - Position);
            // ExtensionClass.DrawRay(ray2D.origin, ray2D.direction * Vector2.Distance((Vector2)navMeshAgent.destination, Position), Color.yellow, 0.5f);
        }

        isConsiderNextBehavior = true;
    }

    public void KeepProperDistance_SM()
    {
        isConsiderNextBehavior = false;

        if (TargetUnit != null)
        {
            distanceFromTarget = TargetUnit.Position.GetQuarterViewDistanceFrom(Position);

            if (MonsterStatData != null)
                switch (MonsterStatData.Role)
                {
                    case UnitRole.Warrior:
                    case UnitRole.Assassin:
                    // if (distanceFromTarget > originStoppingDistance * 4 || distanceFromTarget - originStoppingDistance > UnitData.MaxSpd * 3)
                    // {
                    //     isKeepProperDistanceCooldownDone = false;
                    //     Invoke(nameof(CooldownKeepProperDistance), 1.5f);

                    //     var randomProperPosition = TargetUnit.Position.GetRandomInsidePositionAsQuarterView(Math.Min(originStoppingDistance * 2.5f, UnitData.MaxSpd * 2));
                    //     SetUnitDestination(randomProperPosition, distanceFromTarget <= originStoppingDistance * 0.5f, true);
                    //     if (CurUnitState != DragonBonesUnitState.Move)
                    //         stateMachine.TriggerUnityEvent(DragonBonesUnitState.Move.ToCachedString());

                    //     // ! 레이저
                    //     // ray2D = new Ray2D(TargetUnit.Position, randomProperPosition - TargetUnit.Position);
                    //     // ExtensionClass.DrawRay(ray2D.origin, ray2D.direction * Vector2.Distance(randomProperPosition, TargetUnit.Position), Color.blue, 0.5f);
                    // }
                    // break;

                    case UnitRole.Archer:
                    case UnitRole.Mage:
                    case UnitRole.Cleric:
                    case UnitRole.Terminator:
                        if (distanceFromTarget < originStoppingDistance || distanceFromTarget > originStoppingDistance * 1.5f || distanceFromTarget - originStoppingDistance > UnitStatData.Spd * 3)
                        {
                            isKeepProperDistanceCooldownDone = false;
                            Invoke(nameof(CooldownKeepProperDistance), 2f);

                            var randomProperPosition = Position.GetRandomOutlinePositionAsQuarterViewWithSameQueadrantFrom(TargetUnit.Position, Math.Max(originStoppingDistance * 1.2f, UnitStatData.Spd * 5));
                            SetUnitDestination(randomProperPosition, distanceFromTarget <= originStoppingDistance * 0.5f, true);
                            if (CurUnitState != DragonBonesUnitState.Move)
                                stateMachine.TriggerUnityEvent(DragonBonesUnitState.Move.ToCachedString());

                            // ! 레이저
                            // ray2D = new Ray2D(TargetUnit.Position, randomProperPosition - TargetUnit.Position);
                            // ExtensionClass.DrawRay(ray2D.origin, ray2D.direction * Vector2.Distance(randomProperPosition, TargetUnit.Position), Color.blue, 0.5f);
                        }
                        break;
                }
        }

        isConsiderNextBehavior = true;
    }

    void CooldownKeepProperDistance()
    {
        isKeepProperDistanceCooldownDone = true;
    }

    public void AccessToTarget_SM()
    {
        isConsiderNextBehavior = false;

        // 스킬 사용할 대상의 active 확인 // TODO: 임시로 아이들로 변경했는데.. 다른 타게팅을 조사하고 없으면 아이들로 가야함
        if (TargetUnit != null && TargetUnit.gameObject.activeSelf)
        {
            isAccessToTargetCooldownDone = false;

            curDestinationVec2 = TargetUnit.transform.position;
            SetUnitDestination(curDestinationVec2.GetRandomInsidePositionAsQuarterView(UnityEngine.Random.Range(0, CurSkillData.Range * 0.8f)), true, true);
            navMeshAgent.stoppingDistance *= (10 - skillUseFailureStack) * 0.1f;

            if (CurUnitState != DragonBonesUnitState.Move)
                stateMachine.TriggerUnityEvent(DragonBonesUnitState.Move.ToCachedString());

            // ! 레이저
            // ray2D = new Ray2D(Position, (Vector2)navMeshAgent.destination - Position);
            // ExtensionClass.DrawRay(ray2D.origin, ray2D.direction * Vector2.Distance((Vector2)navMeshAgent.destination, Position), Color.yellow, 0.5f);

            Invoke(nameof(CooldownAccessToTarget), 0.5f);
        }

        isConsiderNextBehavior = true;
    }

    void CooldownAccessToTarget()
    {
        isAccessToTargetCooldownDone = true;
    }

    void CheckReachDestination()
    {
        StartCoroutine(ReachDestinationCo());
    }

    IEnumerator ReachDestinationCo()
    {
        while (true)
        {
            if (CurUnitState == DragonBonesUnitState.Move)
                if (!navMeshAgent.pathPending)
                    if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                        if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
                        {
                            // gameObject.Log($"목적지 도착, 아이들로 전환");
                            stateMachine.TriggerUnityEvent(DragonBonesUnitState.Idle.ToCachedString());
                        }

            yield return wait100ms;
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Idle State *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Move State *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Skill State *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    bool IsSkillAvailable(SkillData skillData)
    {
        // 스킬 사용할 대상의 active 확인
        if (TargetUnit == null || !TargetUnit.gameObject.activeSelf || skillData == null)
            return false;

        distanceFromTarget = TargetUnit.Position.GetQuarterViewDistanceFrom(Position);

        if (distanceFromTarget > CurSkillData.Range)
            return false;
        else
        {
            // 직사 스킬, 곡사는 거리만 되면 바로 스킬 사용 가능
            if (skillData.Trajectory == Skill.Trajectory.Direct)
            {
                quarterViewSkillRange = TargetUnit.Position.GetQuarterViewScalar(Position, CurSkillData.Range);

                // 직사 스킬사용 판단용 레이저
                layerMask = 1 << LayerMask.NameToLayer(NameManager.LayerName.Wall.ToCachedString());
                if (CurSkillData.Type == Skill.Type.Ranged && MuzzleSlotTransform != null)
                    ray2D = new Ray2D((Vector2)MuzzleSlotTransform.position, TargetUnit.Position - (Vector2)MuzzleSlotTransform.position);
                else
                    ray2D = new Ray2D(Position, TargetUnit.Position - Position);
                raycastHit2D = Physics2D.Raycast(ray2D.origin, ray2D.direction, quarterViewSkillRange, layerMask);

                // ! 레이저
                // ExtensionClass.DrawRay(ray2D.origin, ray2D.direction * raycastHit2D.distance, Color.red, 0.5f);
                // ExtensionClass.DrawRay(ray2D.origin, ray2D.direction * quarterViewSkillRange, Color.red, 0.5f);

                // 스킬의 타겟까지 레이저를 발사하여 중간에 장애물이 있다면 스킬사용 불가능 판정
                // 중간에 벽이 있더라도 근접 공격 사거리의 절반에 해당하면 공격 가능하다고 판정
                if (raycastHit2D && distanceFromTarget > CurSkillData.Range * 0.5)
                    return false;
            }

            return true;
        }
    }

    // 스킬이 이 메소드를 호출함.
    // ? 기존에 스킬데이터에 어디를 노려야하는지 데이터를 넣어줬는데, 이 데이터를 제거하여 서버상의 데이터 작성을 줄임.
    public override Vector2 GetSkillInitialPosition(Skill skill, bool isProjectileSkill, bool isAimToField)
    {
        // 자기 자신이 대상인 경우 자신의 포지션 리턴; 해당 경우는 즉발스킬(impact)의 경우에 한정되지만 발사체 스킬을 자기자신에게 사용하는 경우가 없음.
        // 또는 스킬을 사용하는 시점에 대상 유닛이 활성화 상태가 아닌 경우에도 아래의 포지션을 리턴.
        if (skill.SkillData.Target == Skill.Target.Self || !TargetUnit.IsUnitAlive())
            return isAimToField ? (Vector2)transform.position : Position;

        var quarterViewSkillRange = TargetUnit.Position.GetQuarterViewScalar(Position, skill.SkillData.Range);

        switch (skill.SkillData.Type)
        {
            case Skill.Type.Melee:
                // 자기자신의 경우는 위에서 처리했으므로 2가지 경우만 고려하면 됨
                if (skill.SkillData.Target == Skill.Target.Ally)
                    layerMask = 1 << LayerMask.NameToLayer(NameManager.LayerName.Monster.ToCachedString());
                else
                    layerMask = 1 << LayerMask.NameToLayer(NameManager.LayerName.Character.ToCachedString());
                ray2D = new Ray2D(Position, TargetUnit.Position - Position);
                raycastHit2D = Physics2D.Raycast(ray2D.origin, ray2D.direction, quarterViewSkillRange, layerMask);

                // ! 레이저
                // Debug.DrawRay(ray2D.origin, ray2D.direction * raycastHit2D.distance, Color.green, 0.5f);

                if (raycastHit2D) // range 내에 타겟이 있음
                {
                    return raycastHit2D.point;
                }
                else // 대상의 방향으로 range 만큼 떨어진 곳
                {
                    return Position + quarterViewSkillRange * TargetUnit.Position.GetNomalizedVector2From(Position);
                }

            case Skill.Type.Ranged:
                if (isProjectileSkill)
                    return MuzzleSlotTransform != null ? (Vector2)MuzzleSlotTransform.position : Position;
                else
                {
                    if (isAimToField)
                        return TargetUnit.transform.position;
                    else
                        return TargetUnit.Position;
                }

            // 해당 경우는 없음, 오류 방지용
            default:
                return Position;
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Damaged State *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Defeat State *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *             * Animation Event Listener *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // 기본적이고 공통되는 애니메이션의 이벤트 리스너 등록
    void AddEventListenerForMonster()
    {
        ArmatureComponent.AddDBEventListener(EventObject.START, OnStartEventHandler);
        ArmatureComponent.AddDBEventListener(EventObject.COMPLETE, OnCompleteEventHandler);
        ArmatureComponent.AddDBEventListener(EventObject.FRAME_EVENT, OnFrameEventHandler);
    }

    void OnStartEventHandler(string type, EventObject eventObject)
    {
        // gameObject.Log($"{name}: {eventObject.animationState.name} 애니메이션 시작");
        // 스킬 애니메이션이 시작되면 GetSkillPosition 메소드 실행
        if (IsSameAnimationOrEventName(eventObject.animationState.name, this))
        {
            ReadyForUsingSkill();
        }
    }

    void OnCompleteEventHandler(string type, EventObject eventObject)
    {
        // gameObject.Log($"{name}: {eventObject.animationState.name} 애니메이션 종료");
        // 스킬 애니메이션이 종료되면 Idle 상태로 전환
        if (IsSameAnimationOrEventName(eventObject.animationState.name, this))
        {
            // ? 'Skill' 스테이트에서 공통 애니메이션인 'Cast'가 실행되었다면, 드래곤본 이벤트로 스킬 애니메이션이 재생되므로, 'Idle'로 변경시키지 않는다.
            // ? 드래곤본즈 이벤트로 애니메이션 제어를 하면 에러가 발생하므로 코드로 제어한다. => 더더욱 큰 에러 발생;;;
            if (CurUnitState == DragonBonesUnitState.Skill && eventObject.animationState.name == DragonBonesUnitState.Cast.ToCachedString())
            {
                ArmatureComponent.animation.Stop();
                ArmatureComponent.animation.Play(ChangeSkillAnimationNameToCommonAnimationName(CurSkillData.AniName, true), 1);
                return;
            }

            // gameObject.Log($"{name}: {eventObject.animationState.name} == {CurUnitSkillData.AniName}, 아이들로 상태 전환");
            stateMachine.TriggerUnityEvent(DragonBonesUnitState.Idle.ToCachedString());
        }

        if (IsSameAnimationName(eventObject.animationState.name, DragonBonesUnitState.Defeat))
        {
            if (ActivateSkillList.Count == 0)
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

    void OnFrameEventHandler(string type, EventObject eventObject)
    {
        if (IsSameAnimationOrEventName(eventObject.name, this))
        {
            stateMachine.TriggerUnityEvent(NameManager.TriggerEventName.UseSkill.ToCachedUncamelCaseString());

            // 공격 인터벌 설정.
            attackInterval = CurSkillData.StdCd / 2;
            if (attackInterval > 5)
                attackInterval = 5;
        }

        if (eventObject.name == NameManager.FrameEventName.Rush.ToCachedString())
        {
            if (TargetUnit != null)
            {
                // ? 레이저 판정 보류
                // layerMask = 1 << LayerMask.NameToLayer(NameManager.LayerName.Wall.ToCachedString());
                // ray2D = new Ray2D(Position, TargetUnit.Position - Position);
                // raycastHit2D = Physics2D.Raycast(ray2D.origin, ray2D.direction, quarterViewSkillRange, layerMask);

                // // ! 레이저
                // ExtensionClass.DrawRay(ray2D.origin, ray2D.direction * quarterViewSkillRange, Color.red, 0.5f);

                // if (raycastHit2D)
                //     SetUnitDestination(raycastHit2D.point, true, true);
                // else
                SetUnitDestination(TargetUnit.Position, true, true, true);
            }
        }
        else if (eventObject.name == NameManager.FrameEventName.Stop.ToCachedString())
        {
            navMeshAgent.speed = 0;
        }

        // 스킬 활성화 프레임 이벤트.
        if (eventObject.name == NameManager.FrameEventName.ActivateSkill.ToCachedUncamelCaseString())
        {
            foreach (var skill in ActivateSkillList)
                skill.ActivateSkill();
        }
    }
}
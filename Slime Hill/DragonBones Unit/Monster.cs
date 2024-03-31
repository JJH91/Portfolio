using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using DragonBones;
using Sirenix.OdinInspector;

public class Monster : DragonBonesUnit
{
    public override UnitStatData UnitStatData { get => MonsterStatData; }

    [TitleGroup("Monster"), BoxGroup("Monster/M", showLabel: false)]
    [BoxGroup("Monster/M/Monster Stat Data"), ShowInInspector] public MonsterStatData MonsterStatData { get; set; }

    protected override void Awake()
    {
        UnitType = DragonBonesUnitType.Monster;

        base.Awake();
    }

    protected override void Start()
    {
        for (int i = 0; i < SkillDataList.Count; i++)
            if (SkillDataList[i].Mp < MinNeedMP)
                MinNeedMP = SkillDataList[i].Mp;

        base.Start();
    }

    protected override void OnEnable()
    {
        CombatManager.Instance.AddMonster(this);

        base.OnEnable();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Test *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Test/T/Test"), Button("Test 1", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
    void Test_1()
    {

    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *          * DragonBones Method Override *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

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

        OnHpZero();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Unit Data *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public override void InitUnit()
    {
        base.InitUnit();

        MonsterStatData.ApplyAllEquipmentStatModifierData(Weapon);

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

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                       * AI *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Skill *
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

    protected override void OnDragonBonesAnimationCompleteEventHandler(string type, EventObject eventObject)
    {
        // 스킬 애니메이션이 종료되면 Idle 상태로 전환.
        if (IsSameAnimationOrEventName(eventObject.animationState.name, this))
        {
            if (CurUnitState == DragonBonesUnitState.Skill && eventObject.animationState.name == DragonBonesUnitState.Cast.ToCachedString())
            {
                ArmatureComponent.animation.Stop();
                ArmatureComponent.animation.Play(ChangeSkillAnimationNameToCommonAnimationName(CurSkillData.AniName, true), 1);
                return;
            }

            stateMachine.TriggerUnityEvent(DragonBonesUnitState.Idle.ToCachedString());

            // 사용 가능한 스킬 리스트 초기화.
            AvailableSkillDataList.Clear();
        }

        base.OnDragonBonesAnimationCompleteEventHandler(type, eventObject);
    }

    protected override void OnDragonBonesAnimationFrameEventHandler(string type, EventObject eventObject)
    {
        if (IsSameAnimationOrEventName(eventObject.name, this))
        {
            // ? 스킬 사용중에 타겟이 바뀔 경우, 방향 전환을 위해 추가했지만 부자연스러워서 주석처리.
            // IsChangedUnitLookSide_SM(TargetUnit);

            stateMachine.TriggerUnityEvent(NameManager.TriggerEventName.UseSkill.ToCachedUncamelCaseString());
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
            foreach (var skill in ActivateRefSkillList)
                skill.ActivateSkill();
        }

        base.OnDragonBonesAnimationFrameEventHandler(type, eventObject);
    }
}

[Serializable]
public class SharedMonster : SharedVariable<Monster>
{
    public static implicit operator SharedMonster(Monster value) { return new SharedMonster { Value = value }; }
}
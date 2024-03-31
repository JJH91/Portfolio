using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using DragonBones;
using Kryz.CharacterStats;
using Sirenix.OdinInspector;

public class Character : DragonBonesUnit
{
    public enum CharacterPosture { None, Peace, Combat, Walk, Run, Stand, Move }
    public enum CharacterUnitSkillKind { CharacterSkill, Dodge, WeaponSkill, HeadArmorSkill, BodyArmorSkill, HandArmorSkill, FootArmorSkill }

    public override UnitStatData UnitStatData { get => CharacterStatData; }

    [TitleGroup("Character"), BoxGroup("Character/C", showLabel: false)]
    /// <summary>
    /// The second weapon data applied unit data.
    /// </summary>
    [BoxGroup("Character/C/Animation Name Dictionary"), SerializeField] bool hasSimpleAnimationName;
    public bool HasSimpleAnimationName { get => hasSimpleAnimationName; }
    [BoxGroup("Character/C/Character Stat Data"), ShowInInspector] public CharacterStatData CharacterStatData { get; set; }

    [TitleGroup("Weapon"), BoxGroup("Weapon/W", showLabel: false)]
    [BoxGroup("Weapon/W/Weapon Index"), ShowInInspector] public int WeaponIndex { get; private set; }
    [BoxGroup("Weapon/W/Weapon Manager"), ShowInInspector] public WeaponManager WeaponManager { get; private set; }

    [BoxGroup("Character/C"), SerializeField, ReadOnly] bool isMove = false;
    [BoxGroup("Character/C"), SerializeField, ReadOnly] bool isAttack = false;

    StatModifier movingAttackStatModifier = new StatModifier(-0.5f, StatModType.PercentMult, nameof(UnitBaseData.Spd), "Moving Attack");

    WaitForSeconds wait100ms = new WaitForSeconds(0.1f);

    protected override void Awake()
    {
        dontDeactiveOnMapLoad = true;

        UnitType = DragonBonesUnitType.Character;

        base.Awake();
    }

    protected override void OnEnable()
    {
        if (CombatManager.Instance != null)
            CombatManager.Instance.AddCharacter(this);

        base.OnEnable();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                     * Test *
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
     *         * DragonBones Unit Method Overrid *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public override void UnitRevive()
    {
        base.UnitRevive();

        stateMachine.TriggerUnityEvent(DragonBonesUnitState.Idle.ToCachedString());
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *             * Deactive On Map Load *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    protected override void SetDeactiveOnMapLoad() { }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Unit Data *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public override void InitUnit()
    {
        base.InitUnit();

        CharacterStatData.ApplyAllEquipmentStatModifierData(0, Weapon);

        // 스테이터스 바 업데이트 이벤트 구독은 오브젝트 매니저에서, 구독 해지는 스테이스바에서 함
        // if (unitStatusIndicator == null)
        //      ObjectManager.Instance.GetUnitStatusIndicator(this);

        // WeaponManager = ObjectManager.Instance.GetWeaponManager(this, new WeaponStatModifierData[] { weaponStatModData0, weaponStatModData1 });
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Animation *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public override string GetAnimationNameWithFlipChecking(string animationName)
    {
        if (!HasSimpleAnimationName)
            animationName = animationName.ToCachedCharacterAniName(this);

        return base.GetAnimationNameWithFlipChecking(animationName);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *          * Character Move Control Method *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void MoveCharacter()
    {
        if (!isMove)
        {
            isMove = true;
            stateMachine.TriggerUnityEvent(DragonBonesUnitState.Move.ToCachedString());
            StartCoroutine(MoveCharacterCo());
        }
    }

    IEnumerator MoveCharacterCo()
    {
        while (isMove)
        {
            curDestinationVec2 = (Vector2)transform.position + CombatManager.Instance.Joystick.Direction.normalized * 4f;
            SetUnitDestination(curDestinationVec2, isAttack);

            yield return wait100ms;
        }
    }

    public void StopCharacter()
    {
        if (isMove)
        {
            isMove = false;
            stateMachine.TriggerUnityEvent(NameManager.TriggerEventName.Stop.ToCachedString());
        }

        navMeshAgent.speed = 0;
    }

    public void AttackEnemy(bool value)
    {
        isAttack = value;

        if (isAttack)
            stateMachine.TriggerUnityEvent(NameManager.TriggerEventName.Attack.ToCachedString());
        else
            stateMachine.TriggerUnityEvent(NameManager.TriggerEventName.Cease.ToCachedString());
    }

    // Used Character.
    public void MovingAttakSpeedReduce_SM(bool isReducing)
    {
        if (isReducing)
            UnitStatData.SpdStat.AddModifier(movingAttackStatModifier);
        else
            UnitStatData.SpdStat.RemoveModifier(movingAttackStatModifier);

        navMeshAgent.speed = UnitStatData.Spd;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Weapon Change *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void ChangeWeapon()
    {
        // Change weapon index and sync CurHp and CurMp.
        if (WeaponIndex == 0)
            WeaponIndex++;
        else
            WeaponIndex--;
        CharacterStatData.ChangeAppliedWeaponStatModifierData(WeaponIndex);

        // WeaponManager.ChangeWeapon();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Skill *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void OnSkillButtonClick(CharacterUnitSkillKind characterUnitSkillKind)
    {
        // 사용할 스킬 지정
        CurSkillIndex = (int)characterUnitSkillKind;
        CurSkillData = SkillDataList[CurSkillIndex];

        // 스킬 스테이트로 이동
        if (CurSkillData != null)
            if (CurSkillData.CurCd <= 0)
                if (UnitStatData.CurMp >= CurSkillData.Mp)
                {
                    SetTargetUnit(CurSkillData.Target, CurSkillData.TgtCond, CurSkillData.TgtSort);

                    stateMachine.TriggerUnityEvent(DragonBonesUnitState.Skill.ToCachedString());
                    return;
                }

        // 스킬 사용 조건이 안되면 현재 스킬을 초기화
        CurSkillData = null;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *            * Animation Event Listener *
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

            // 회피 후처리.
            if (eventObject.animationState.name == CharacterUnitSkillKind.Dodge.ToCachedString())
            {
                ColliderOnOff(true);
                navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                rigidbody2D.velocity = Vector2.zero;

                if (CombatManager.Instance.Joystick.Direction != Vector2.zero)
                    MoveCharacter();
            }

            if (CombatManager.Instance.PlayingCharacter != this)
                stateMachine.TriggerUnityEvent(DragonBonesUnitState.Idle.ToCachedString());
            else
            {
                // 이동 조작 여부에 따른 스테이트 전환.
                if (CombatManager.Instance.Joystick.Direction == Vector2.zero)
                    stateMachine.TriggerUnityEvent(DragonBonesUnitState.Idle.ToCachedString());
                else
                {
                    MoveCharacter();
                    // stateMachine.TriggerUnityEvent(DragonBonesUnitState.Move.ToCachedString());
                }

                // 공격 조작 여부에 따른 스테이트 전환.
                if (isAttack)
                    stateMachine.TriggerUnityEvent(DragonBonesUnitState.Attack.ToCachedString());
            }

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

            // 회피 전처리
            if (eventObject.name == CharacterUnitSkillKind.Dodge.ToCachedString())
            {
                ColliderOnOff(false);
                navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                rigidbody2D.AddForce(rigidbody2D.mass * UnitStatData.Spd * 3 * CombatManager.Instance.Joystick.Direction.ToQuarterViewDirectionVector2(), ForceMode2D.Impulse);
            }
        }

        base.OnDragonBonesAnimationFrameEventHandler(type, eventObject);
    }
}

[Serializable]
public class SharedCharacter : SharedVariable<Character>
{
    public static implicit operator SharedCharacter(Character value) { return new SharedCharacter { Value = value }; }
}
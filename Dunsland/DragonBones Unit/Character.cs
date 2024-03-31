using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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
    [BoxGroup("Character/C/Character Stat Data"), ShowInInspector] public CharacterStatData CharacterStatData { get; set; }
    [BoxGroup("Character/C/Animation Name Dictionary"), SerializeField] Dictionary<Enum, string> animationNameCacheDict = new Dictionary<Enum, string>();
    [BoxGroup("Character/C/Animation Name Dictionary"), SerializeField] Dictionary<Enum, string> animationNameCacheDict_Flip = new Dictionary<Enum, string>();
    [BoxGroup("Character/C/Animation Name Dictionary"), SerializeField] Dictionary<string, string> skillAnimationNameCacheDict = new Dictionary<string, string>();
    [BoxGroup("Character/C/Animation Name Dictionary"), SerializeField] Dictionary<string, string> skillAnimationNameCacheDict_Flip = new Dictionary<string, string>();

    [TitleGroup("Weapon"), BoxGroup("Weapon/W", showLabel: false)]
    [BoxGroup("Weapon/W/Weapon Index"), ShowInInspector] public int WeaponIndex { get; private set; }
    [BoxGroup("Weapon/W/Weapon Manager"), ShowInInspector] public WeaponManager WeaponManager { get; private set; }
    [BoxGroup("Weapon/W/Weapon Stat Mod Data"), SerializeField] WeaponStatModifierData weaponStatModData0, weaponStatModData1;
    public WeaponStatModifierData WeaponStatModData { get => WeaponIndex == 0 ? weaponStatModData0 : weaponStatModData1; }
    public void SetWeaponStatModData(WeaponStatModifierData weaponStatModData0, WeaponStatModifierData weaponStatModData1)
    {
        this.weaponStatModData0 = weaponStatModData0;
        this.weaponStatModData1 = weaponStatModData1;
    }

    [BoxGroup("Test"), SerializeField] float floatTest, floatTest2 = 1f;

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

    protected override void Start()
    {
        AddEventListenerForCharacter();

        base.Start();
    }

    protected override void OnEnable()
    {
        if (CombatManager.Instance != null)
            CombatManager.Instance.AddCharacter(this);

        base.OnEnable();
    }

    protected override void OnDisable()
    {
        if (CombatManager.Instance != null)
            CombatManager.Instance.RemoveCharacter(this);

        base.OnDisable();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                     * Test *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // ? 무기가 유닛의 손 위치를 따라다니게 테스트하던 메소드.
    // ? 결국 손 위치의 자식 오브젝트로 무기를 집어넣는 방법을 사용하여 테스트의 내용은 더 이상 사용되진 않음
    // ? 하지만 양손 무기인 경우, 무기의 위치에 손을 위치시키는 방법을 사용해야할 수도 있음. 그때를 위해 테스트 메소드를 남겨놓음.
    [BoxGroup("Test"), SerializeField] UnityArmatureComponent weaponUAC_L, weaponUAC_R;
    [BoxGroup("Test"), SerializeField] int index;
    [BoxGroup("Test"), SerializeField] bool runRoop = true;
    [BoxGroup("Test"), SerializeField] float xFactor = 1, yFactor = 1;

    [BoxGroup("Test"), Button("Pistol Tr Positioning", ButtonSizes.Gigantic)]
    void PTPReady()
    {
        weaponUAC_L = GameObject.Find($"Pistol ({index++})").GetComponent<UnityArmatureComponent>();
        weaponUAC_R = GameObject.Find($"Pistol ({index++})").GetComponent<UnityArmatureComponent>();
    }

    [BoxGroup("Test"), Button("Get Weapon")]
    void TestGetWeapon(NameManager.PistolName weaponAniName, bool isAnimationPlay)
    {
        var LHS = (ArmatureComponent.armature.GetSlot("Left_Hand_Slot").display as GameObject).transform;
        var RHS = (ArmatureComponent.armature.GetSlot("Right_Hand_Slot").display as GameObject).transform;

        weaponUAC_L.sortingOrder = 160;
        weaponUAC_R.sortingOrder = 20;

        weaponUAC_L.armature.animation.Play(weaponAniName.ToCachedString());
        weaponUAC_R.armature.animation.Play(weaponAniName.ToCachedString());

        weaponUAC_L.transform.SetParent(LHS);
        weaponUAC_R.transform.SetParent(RHS);

        weaponUAC_L.transform.localPosition = Vector3.zero;
        weaponUAC_R.transform.localPosition = Vector3.zero;

        var rotation = new Quaternion();
        rotation.eulerAngles = new Vector3(0, 0, 180);
        weaponUAC_L.transform.localRotation = rotation;
        weaponUAC_R.transform.localRotation = rotation;

        weaponUAC_L.armature.InvalidUpdate();
        weaponUAC_R.armature.InvalidUpdate();

        if (isAnimationPlay)
        {
            ArmatureComponent.animation.Play("Attack-One_Hand_Gun-Stand", 10);
            weaponUAC_L.armature.animation.Play($"{weaponAniName.ToCachedString()}-Fire_L", 10);
            weaponUAC_R.armature.animation.Play($"{weaponAniName.ToCachedString()}-Fire_R", 10);
        }
    }

    [BoxGroup("Test"), Button("Weapon Follow", ButtonSizes.Gigantic)]
    void PTP()
    {
        var RHIkt = ArmatureComponent.armature.GetBone(DragonBonesBoneName.Right_Hand_Ikt.ToCachedString());

        StartCoroutine(PTPCo(RHIkt));
    }

    IEnumerator PTPCo(Bone bone)
    {
        var wait = new WaitForEndOfFrame();
        var bTr = bone.boneData.transform.ToUnityTransform();

        gameObject.Log($"타겟 좌표: {bTr}");
        gameObject.Log($"총기 좌표: {weaponUAC_R.transform.position}");

        bone.offsetMode = OffsetMode.Override;
        bone.offset.x = weaponUAC_R.transform.position.x * xFactor;
        bone.offset.y = -weaponUAC_R.transform.position.y * yFactor;
        bone.offset.rotation = -weaponUAC_R.transform.rotation.eulerAngles.z * yFactor;

        while (runRoop)
        {
            yield return wait;

            bone.offset.x = weaponUAC_R.transform.position.x * xFactor;
            bone.offset.y = -weaponUAC_R.transform.position.y * yFactor;
            bone.offset.rotation = -weaponUAC_R.transform.rotation.eulerAngles.z * yFactor;

            ArmatureComponent.armature.InvalidUpdate();
        }

        ArmatureComponent.armature.InvalidUpdate();
    }

    [BoxGroup("Test"), Button("Weapon Follow2", ButtonSizes.Gigantic)]
    void PTP221()
    {
        var RHIkt = ArmatureComponent.armature.GetBone(DragonBonesBoneName.Right_Hand_Ikt.ToCachedString());
        gameObject.Log($"타겟 좌표: {RHIkt.boneData.transform.ToUnityTransform()}");
        gameObject.Log($"총기 좌표: {weaponUAC_R.transform.position}");

        StartCoroutine(PTP221Co(RHIkt));
    }

    IEnumerator PTP221Co(DragonBones.Bone bone)
    {
        var wait = new WaitForEndOfFrame();
        var bTr = bone.boneData.transform.ToUnityTransform();

        gameObject.Log($"타겟 좌표: {bTr}");
        gameObject.Log($"총기 좌표: {weaponUAC_R.transform.position}");

        bone.offsetMode = OffsetMode.Override;
        bone.offset.x = weaponUAC_R.armature.GetBone("Handle").boneData.transform.ToUnityTransform().x * xFactor;
        bone.offset.y = -weaponUAC_R.armature.GetBone("Handle").boneData.transform.ToUnityTransform().y * yFactor;
        bone.offset.rotation = -weaponUAC_R.transform.rotation.eulerAngles.z * yFactor;

        while (runRoop)
        {
            yield return wait;

            bone.offset.x = weaponUAC_R.armature.GetBone("Handle").boneData.transform.ToUnityTransform().x;
            bone.offset.y = -weaponUAC_R.armature.GetBone("Handle").boneData.transform.ToUnityTransform().y;
            bone.offset.rotation = -weaponUAC_R.transform.rotation.eulerAngles.z;

            ArmatureComponent.armature.InvalidUpdate();
        }

        ArmatureComponent.armature.InvalidUpdate();
    }

    [BoxGroup("Test"), Button("Test", ButtonSizes.Gigantic)]
    public void Test()
    {
        gameObject.Log($"Test Start");

        CharacterStatData = new CharacterStatData();

        SkillData normalBullet = new SkillData
        {
            SkillName = NameManager.SkillProjectilePrefabName.NormalBullet.ToCachedUncamelCaseString(),
            Kind = Skill.Kind.Active,
            Type = Skill.Type.Ranged,
            Trajectory = Skill.Trajectory.Direct,
            Target = Skill.Target.Enemy,
            ProjDmgX_0 = 1,
            ProjSpd = 20,
            ProjTime = 1
        };

        weaponStatModData0 = new WeaponStatModifierData
        {
            EquipmentType = PlayFabManager.CatalogItemTag.Pistol,
            Name = "Beretta M9",
            BulletSkillDataList = new List<SkillData>() { normalBullet }
        };

        ObjectManager.Instance.GetUnitStatusIndicator(this);
        WeaponManager = ObjectManager.Instance.GetWeaponManager(this, new WeaponStatModifierData[] { weaponStatModData0, weaponStatModData1 });

        CombatManager.Instance.PlayingCharacter = this;

        gameObject.Log($"Test Done");
    }

    [BoxGroup("Test"), Button("Stop All Coroutines")]
    void StopAllTestCoroutines()
    {
        StopAllCoroutines();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *         * DragonBones Unit Method Overrid
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    protected override void OnHpZero()
    {
        ColliderOnOff(false);

        if (CurUnitState != DragonBonesUnitState.Defeat)
        {
            stateMachine.TriggerUnityEvent(DragonBonesUnitState.Defeat.ToCachedString());

            CombatManager.Instance.ChangeCombatWindow(CombatManager.CombatWindowName.Defeat);
        }
    }

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
     *               * Unit Data Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public override void InitUnit()
    {
        base.InitUnit();

        CharacterStatData.ApplyAllEquipmentStatModifierData(0);

        // 스테이터스 바 업데이트 이벤트 구독은 오브젝트 매니저에서, 구독 해지는 스테이스바에서 함
        if (unitStatusIndicator == null)
            ObjectManager.Instance.GetUnitStatusIndicator(this);

        WeaponManager = ObjectManager.Instance.GetWeaponManager(this, new WeaponStatModifierData[] { weaponStatModData0, weaponStatModData1 });
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Move Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void MoveTransformPosition(Vector2 newPosition)
    {
        navMeshAgent.enabled = false;
        transform.position = newPosition;
        CombatManager.Instance.CmVcamCharacterFollower.transform.position = newPosition;
        navMeshAgent.enabled = true;
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

    public void MoveCharacterForPC()
    {
        curDestinationVec2 = (Vector2)transform.position + new Vector2(GameManager.Instance.moveX, GameManager.Instance.moveY);

        SetUnitDestination(curDestinationVec2);

        // 바라보는 방향 설정
        if (!isAttack)
        {
            if (GameManager.Instance.moveX < 0)
                if (CurLookingAt != UnitLookingAt.Left)
                    ArmatureFlipX();

            if (GameManager.Instance.moveX > 0)
                if (CurLookingAt != UnitLookingAt.Right)
                    ArmatureFlipX();
        }
    }

    public void AttackEnemy(bool value)
    {
        isAttack = value;

        if (value)
        {
            StartCoroutine(MonsterTargetingCo());
            stateMachine.TriggerUnityEvent(NameManager.TriggerEventName.Attack.ToCachedString());
        }
        else
        {
            stateMachine.TriggerUnityEvent(NameManager.TriggerEventName.Cease.ToCachedString());
        }
    }

    // Used Character
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
        if (weaponStatModData0 == null || weaponStatModData1 == null)
        {
            gameObject.LogError($"무기 변경을 할 수 없습니다. 장착한 무기가 1개 입니다.");
            return;
        }

        // Change weapon index and sync CurHp and CurMp.
        if (WeaponIndex == 0)
            WeaponIndex++;
        else
            WeaponIndex--;
        CharacterStatData.ChangeAppliedWeaponStatModifierData(WeaponIndex == 0 ? weaponStatModData0 : weaponStatModData1);

        WeaponManager.ChangeWeapon();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *               * Animation Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // 기준 타겟이 없으면 플레이어가 현재 바라보는 방향을 기준으로,
    // 기준 타겟이 있다면 플레이어가 바라보는 방향을 체크하고 바라보게 만듦.
    // Used Player, Weapon Manager
    public string GetAnimationNameWithFlipChecking_SM(CharacterPosture characterPosture)
    {
        return GetAnimationNameWithFlipChecking_SM(characterPosture.ToCachedString());
    }

    public override string GetAnimationNameWithFlipChecking_SM(string animationName)
    {
        if (CurUnitState == DragonBonesUnitState.Damaged || CurUnitState == DragonBonesUnitState.Defeat)
        {
            if (!animationNameCacheDict.ContainsKey(CurUnitState))
                animationNameCacheDict[CurUnitState] = CurUnitState.ToCachedString();

            return animationNameCacheDict[CurUnitState];
        }
        else
        {
            if (hasFlipAnimation && StandardLookingAt != CurLookingAt) // Flip 애니메이션이 있으면서 보는 방향이 기준값과 현재 값이 다르면 Flip Animation 이름을 리턴함.
            {
                if (!animationNameCacheDict_Flip.ContainsKey(animationName.ToCachedEnum<CharacterPosture>()))
                    animationNameCacheDict_Flip[animationName.ToCachedEnum<CharacterPosture>()] = $"{CurUnitState.ToCachedString()}-{WeaponStatModData.GripType.Replace(" ", "_")}-{animationName}";

                return animationNameCacheDict_Flip[animationName.ToCachedEnum<CharacterPosture>()];
            }
            else
            {
                if (!animationNameCacheDict.ContainsKey(animationName.ToCachedEnum<CharacterPosture>()))
                    animationNameCacheDict[animationName.ToCachedEnum<CharacterPosture>()] = $"{CurUnitState.ToCachedString()}-{WeaponStatModData.GripType.Replace(" ", "_")}-{animationName}";

                return animationNameCacheDict[animationName.ToCachedEnum<CharacterPosture>()];
            }
        }
    }

    // Used Player, Weapon Manager
    public string GetSkillAnimationNameWithFlipChecking_SM()
    {
        string result;
        if (hasFlipAnimation && StandardLookingAt != CurLookingAt) // Flip 애니메이션이 있으면서 보는 방향이 기준값과 현재 값이 다르면 Flip Animation 이름을 리턴함.
        {
            if (!skillAnimationNameCacheDict_Flip.ContainsKey(CurSkillData.AniName))
                skillAnimationNameCacheDict_Flip[CurSkillData.AniName] = $"{CurSkillData.AniName.Replace("Weapon_Grip_Type", WeaponStatModData.GripType.Replace(" ", "_"))}";

            result = skillAnimationNameCacheDict_Flip[CurSkillData.AniName];
        }
        else
        {
            if (!skillAnimationNameCacheDict.ContainsKey(CurSkillData.AniName))
                skillAnimationNameCacheDict[CurSkillData.AniName] = $"{CurSkillData.AniName.Replace("Weapon_Grip_Type", WeaponStatModData.GripType.Replace(" ", "_"))}";

            result = skillAnimationNameCacheDict[CurSkillData.AniName];
        }

        // 스킬 애니메이션 종료시에 애니메이션 이름을 비교하므로, 변경된 애니메이션 이름으로 바꿔줌.
        CurSkillData.AniName = result;

        return result;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *              * Targeting Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // TODO: Range 값을 캐릭터 값 말고도 무기 값도 영향을 받게 할 것
    public IEnumerator MonsterTargetingCo()
    {
        do
        {
            tempTarget = CombatManager.Instance.MonsterKdTree.FindClosest(Position);

            if (tempTarget.IsUnitAlive())
                if (tempTarget.Position.GetQuarterViewDistanceFrom(Position) <= CharacterStatData.Range)
                    TargetUnit = tempTarget;

            yield return wait100ms;
        } while (isAttack);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Skill Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // Used Character
    // 스킬이 이 메소드를 호출함.
    // ? 기존에 스킬데이터에 어디를 노려야하는지 데이터를 넣어줬는데, 이 데이터를 제거하여 서버상의 데이터 작성을 줄임.
    public override Vector2 GetSkillInitialPosition(Skill skill, bool isProjectileSkill, bool isAimToField)
    {
        // 발사체인 경우
        // 한손 무기의 경우 왼손의 무기 머즐, 양손 무기의 경우 오른손의 무기 머즐의 포지션을 리턴
        if (isProjectileSkill)
            return WeaponManager.Weapon_R != null ? WeaponManager.Weapon_R.MuzzleSlotTransform.position : WeaponManager.Weapon_L.MuzzleSlotTransform.position;

        // 발사체가 아닌 경우 
        if (TargetUnit.IsUnitAlive())
        {
            if (isAimToField)
            {
                if (TargetUnit.transform.position.GetQuarterViewDistanceFrom(transform.position) <= skill.SkillData.Range)
                    return TargetUnit.transform.position;
                else
                    return (Vector2)transform.position + TargetUnit.transform.position.GetNomalizedVector2From(transform.position) * skill.SkillData.Range;
            }
            else
            {
                if (TargetUnit.transform.position.GetQuarterViewDistanceFrom(transform.position) <= skill.SkillData.Range)
                    return TargetUnit.Position;
                else
                    return Position + TargetUnit.Position.GetNomalizedVector2From(Position) * skill.SkillData.Range;
            }
        }
        else
        {
            var range = skill.SkillData.Range * (CurLookingAt == UnitLookingAt.Left ? -1 : 1);

            if (isAimToField)
                return (Vector2)transform.position + new Vector2(range, 0);
            else
                return Position + new Vector2(range, 0);
        }
    }

    public void OnSkillButtonClick(CharacterUnitSkillKind characterUnitSkillKind)
    {
        // 사용할 스킬 지정
        CurSkillIndex = (int)characterUnitSkillKind;
        CurSkillData = UnitSkillList[CurSkillIndex];

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

    // 기본적이고 공통되는 애니메이션의 이벤트 리스너 등록
    void AddEventListenerForCharacter()
    {
        ArmatureComponent.AddDBEventListener(EventObject.START, OnStartEventHandler);
        ArmatureComponent.AddDBEventListener(EventObject.COMPLETE, OnCompleteEventHandler);
        ArmatureComponent.AddDBEventListener(EventObject.FRAME_EVENT, OnFrameEventHandler);
    }

    void OnStartEventHandler(string type, EventObject eventObject)
    {
        if (IsSameAnimationOrEventName(eventObject.animationState.name, this))
        {
            ReadyForUsingSkill();
        }
    }

    void OnCompleteEventHandler(string type, EventObject eventObject)
    {
        // 스킬 애니메이션이 종료되면 Idle 상태로 전환
        if (IsSameAnimationOrEventName(eventObject.animationState.name, this))
        {
            // 회피 후처리
            if (eventObject.animationState.name == CharacterUnitSkillKind.Dodge.ToCachedString())
            {
                ColliderOnOff(true);
                navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                rigidbody2D.velocity = Vector2.zero;

                if (CombatManager.Instance.Joystick.Direction != Vector2.zero)
                    MoveCharacter();
            }

            // 이동 조작 여부에 따른 스테이트 전환
            if (CombatManager.Instance.Joystick.Direction == Vector2.zero)
                stateMachine.TriggerUnityEvent(DragonBonesUnitState.Idle.ToCachedString());
            else
                stateMachine.TriggerUnityEvent(DragonBonesUnitState.Move.ToCachedString());

            // 공격 조작 여부에 따른 스테이트 전환
            if (isAttack)
                stateMachine.TriggerUnityEvent(DragonBonesUnitState.Attack.ToCachedString());
        }
    }

    void OnFrameEventHandler(string type, EventObject eventObject)
    {
        if (IsSameAnimationOrEventName(eventObject.name, this))
        {
            stateMachine.TriggerUnityEvent(NameManager.TriggerEventName.UseSkill.ToCachedUncamelCaseString());

            // 회피 전처리
            if (eventObject.name == CharacterUnitSkillKind.Dodge.ToCachedString())
            {
                ColliderOnOff(false);
                navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                rigidbody2D.AddForce(rigidbody2D.mass * UnitStatData.Spd * 3 * CombatManager.Instance.Joystick.Direction.ToQuarterViewDirectionVector2(), ForceMode2D.Impulse);
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;

public class Skill : AddressableSerializedMonoBehavior
{
    public enum TargetingValueCondition { Distance, Hp, Mp, Atk, Def }
    public enum TargetingSortCondition { Lowest, Highest }
    public enum DamageBase { Atk, Dps }
    public enum State { Projectile, Impact, Stream }
    public enum Target { Ally, Enemy, Self, Both }
    public enum Kind { Active, Passive }
    public enum Type { Melee, Ranged }
    public enum Trajectory { Direct, Howitzer }
    public enum CastLocation { None, Unit, Muzzle }

    [TitleGroup("Skill Option"), BoxGroup("Skill Option/SO", showLabel: false)]
    [BoxGroup("Skill Option/SO/Wait Activating"), SerializeField] bool isWaitActivating;
    [BoxGroup("Skill Option/SO/Wait Activating"), SerializeField, ReadOnly] bool isSkillActivated;
    [BoxGroup("Skill Option/SO/Display Skill Warning"), SerializeField] bool isDisplaySkillWarning;
    [ShowIfGroup("Skill Option/SO/Display Skill Warning/Option", Condition = "isDisplaySkillWarning")]
    [BoxGroup("Skill Option/SO/Display Skill Warning/Option/Waring Part"), EnumToggleButtons, SerializeField] SkillWarningEffect.SkillWarningPart skillWarningPart;
    [BoxGroup("Skill Option/SO/Display Skill Warning/Option/Skill Warning Effect"), SerializeField] SkillWarningEffect skillWarningEffect;
    [BoxGroup("Skill Option/SO/Display Skill Warning/Option/Start Position"), SerializeField] SkillWarningEffect.SkillWarningStartPosition startPositionType;
    [BoxGroup("Skill Option/SO/Display Skill Warning/Option/Tracking"), SerializeField] SkillWarningEffect.SkillWarningTrackingType trackingType;
    [BoxGroup("Skill Option/SO/Display Skill Warning/Option/Tracking"), ShowIf("@trackingType == SkillWarningEffect.SkillWarningTrackingType.Tracking"), SerializeField] float trackingSpeed;
    public float TrackingSpeed
    {
        get
        {
            if (trackingType == SkillWarningEffect.SkillWarningTrackingType.Tracking)
                return trackingSpeed;
            else return 0;
        }
    }
    [BoxGroup("Skill Option/SO/Rotatable"), SerializeField, ReadOnly] Vector2 rotationDirectionVec2;
    [BoxGroup("Skill Option/SO/Rotatable"), SerializeField, ReadOnly] protected float rotationAngle;

    [TitleGroup("Skill"), BoxGroup("Skill/S", showLabel: false)]
    [BoxGroup("Skill/S/Skill Homing Target"), SerializeField] DragonBonesUnit homingTargetUnit;
    [BoxGroup("Skill/S/Skill On Field"), SerializeField] bool isSkillOnField;
    public bool IsSkillOnField { get => isSkillOnField; }
    [BoxGroup("Skill/S/Skill Part"), SerializeField] Skill_ProjectilePart projectilePart;
    [BoxGroup("Skill/S/Skill Part"), SerializeField] Skill_ImpactPart impactPart;
    [BoxGroup("Skill/S/Skill Part"), SerializeField] Skill_StreamPart streamPart;
    [BoxGroup("Skill/S/Skill State"), SerializeField] State baseSkillState;
    public State BaseSkillState { get => baseSkillState; private set { baseSkillState = value; CurSkillState = value; } }
    [BoxGroup("Skill/S/Skill State"), SerializeField] State curSkillState;
    public State CurSkillState { get => curSkillState; set => curSkillState = value; }
    [BoxGroup("Skill/S/Skill Effect"), SerializeField] string launchEffectPrefabName;
    [BoxGroup("Skill/S/Skill Effect"), ShowInInspector]
    NameManager.EffectPrefabName LaunchEffectPrefabName
    {
        get => launchEffectPrefabName.ToCachedEnum<NameManager.EffectPrefabName>();
        set => launchEffectPrefabName = value.ToCachedUncamelCaseString();
    }
    [BoxGroup("Skill/S/Skill Effect"), SerializeField] string hitEffectPrefabName;
    [BoxGroup("Skill/S/Skill Effect"), ShowInInspector]
    NameManager.EffectPrefabName HitEffectPrefabName
    {
        get => hitEffectPrefabName.ToCachedEnum<NameManager.EffectPrefabName>();
        set => hitEffectPrefabName = value.ToCachedUncamelCaseString();
    }

    [TitleGroup("Skill Data"), BoxGroup("Skill Data/SD", showLabel: false)]
    // 스킬 데이터의 SkillName 에 값을 입력하기위한 프로퍼티.
    [BoxGroup("Skill Data/SD/Set Skill Name"), ShowInInspector]
    NameManager.SkillProjectilePrefabName ProjectileSkillPrefabName
    {
        get { return NameManager.SkillProjectilePrefabName.None; }
        set { SkillData.Id = value.ToCachedUncamelCaseString(); }
    }

    [BoxGroup("Skill Data/SD/Set Skill Name"), ShowInInspector]
    NameManager.SkillImpactPrefabName ImpactSkillPrefabName
    {
        get { return NameManager.SkillImpactPrefabName.None; }
        set { SkillData.Id = value.ToCachedUncamelCaseString(); }
    }

    [BoxGroup("Skill Data/SD/Set Skill Name"), ShowInInspector]
    NameManager.SkillAreaPrefabName AreaSkillPrefabName
    {
        get { return NameManager.SkillAreaPrefabName.None; }
        set { SkillData.Id = value.ToCachedUncamelCaseString(); }
    }

    [BoxGroup("Skill Data/SD/Skill Data", order: 1), SerializeField] SkillData skillData;
    public SkillData SkillData { get => skillData; }
    [BoxGroup("Skill Data/SD/Skill Data", order: 1), HideDuplicateReferenceBox, SerializeField] WeaponSkillData weaponSkillData;
    public WeaponSkillData WeaponSkillData
    {
        get => weaponSkillData;
        set
        {
            skillData = value;
            weaponSkillData = value;
        }
    }

    [TitleGroup("Skill Infomation"), BoxGroup("Skill Infomation/SI", showLabel: false)]
    [BoxGroup("Skill Infomation/SI/Skill Damage Calculate Refence Count"), SerializeField] int skillRefCount;
    public int SkillRefCount { get => skillRefCount; set { skillRefCount = value; if (skillRefCount < 0) skillRefCount = 0; } }
    [BoxGroup("Skill Infomation/SI/Target Unit Infomation"), ShowInInspector] public DragonBonesUnit SkillTargetUnit { get; private set; }
    [BoxGroup("Skill Infomation/SI/Cast Unit Infomation"), ShowInInspector]
    public DragonBonesUnit SkillCastUnit
    {
        get
        {
            if (SkillCastCharacter != null)
                return SkillCastCharacter;
            else if (SkillCastMonster != null)
                return SkillCastMonster;
            else
                return null;
        }
    }
    [BoxGroup("Skill Infomation/SI/Cast Unit Infomation"), ShowInInspector] public Character SkillCastCharacter { get; set; }
    [BoxGroup("Skill Infomation/SI/Cast Unit Infomation"), ShowInInspector] public Monster SkillCastMonster { get; set; }
    [BoxGroup("Skill Infomation/SI/Collision Collider2D Infomation"), ShowInInspector] public List<GameObject> CollisionGameObjectList { get; private set; } = new List<GameObject>();
    [BoxGroup("Skill Infomation/SI/Launch"), SerializeField] float angle;
    [BoxGroup("Skill Infomation/SI/Launch"), SerializeField] Vector2 directionVec2;
    [BoxGroup("Skill Infomation/SI/Launch"), SerializeField] Vector2 forceVector2;

    [TitleGroup("Skill Component"), BoxGroup("Skill Component/SC", showLabel: false)]
    [BoxGroup("Skill Component/SC/Sorting Group")] public SortingGroup SortingGroup;

    Rigidbody2D rigidbody2D;

    WaitUntil waitSkillActivating;
    WaitUntil waitProjectilePartDeactice;
    WaitUntil waitImapctPartDeactice;
    WaitUntil waitStreamPartDeactice;
    WaitForSeconds waitHomingDelay = new WaitForSeconds(0.2f);

    Coroutine homingToTargetCo;

    protected override void Awake()
    {
        if (projectilePart != null)
            rigidbody2D = GetComponent<Rigidbody2D>();

        waitSkillActivating = new WaitUntil(() => isSkillActivated);

        waitProjectilePartDeactice = new WaitUntil(() => !projectilePart.gameObject.activeSelf);
        waitImapctPartDeactice = new WaitUntil(() => !impactPart.gameObject.activeSelf);
        waitStreamPartDeactice = new WaitUntil(() => !streamPart.gameObject.activeSelf);

        base.Awake();
    }

    protected override void OnEnable()
    {
        curSkillState = baseSkillState;

        if (SortingGroup != null)
            SortingGroup.enabled = true;

        if (!isWaitActivating)
            isSkillActivated = true;

        base.OnEnable();
    }

    protected override void OnDisable()
    {
        SkillCastUnit.ActivateRefSkillList.Remove(this);

        if (skillWarningEffect != null)
        {
            skillWarningEffect.gameObject.SetActive(false);
            skillWarningEffect = null;
        }

        SkillCastCharacter = null;
        SkillCastMonster = null;

        isSkillActivated = false;

        directionVec2 = Vector2.zero;
        rotationDirectionVec2 = Vector2.zero;

        base.OnDisable();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Test *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Skill/S/Skill Launch Test"), Button("Skill Launch Test", ButtonSizes.Gigantic, ButtonStyle.Box, Expanded = true), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
    void LaunchTest(float speed, bool isStop)
    {
        if (rigidbody2D == null)
            rigidbody2D = GetComponent<Rigidbody2D>();

        if (isStop)
            rigidbody2D.velocity = Vector2.zero;
        else
        {
            rigidbody2D.AddForce(Vector2.right * speed);

#if UNITY_EDITOR
            UnityEditor.Selection.activeObject = this;
            gameObject.Log($"Shift + F 로 오브젝트 추적 가능.");
#endif
        }
    }

    [Button("TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT")]
    void TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT()
    {
        switch (baseSkillState)
        {
            case State.Projectile:
                projectilePart.TTTTTTTTTTTTTTTTTTTTT();
                break;
            case State.Impact:
                impactPart.TTTTTTTTTTTTTTTTTTTTT();
                break;
            case State.Stream:
                streamPart.TTTTTTTTTTTTTTTTTTTTT();
                break;
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Init *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Skill/S"), Button("Init Skill Components", ButtonSizes.Gigantic, ButtonStyle.Box, Expanded = true), GUIColor(0.35f, 0.7f, 1)]
    public void InitSkillComponents(bool isContainSkillWarningSystem)
    {
        projectilePart = transform.GetComponentInChildren<Skill_ProjectilePart>();
        impactPart = transform.GetComponentInChildren<Skill_ImpactPart>();
        streamPart = transform.GetComponentInChildren<Skill_StreamPart>();

        if (streamPart != null)
        {
            baseSkillState = State.Stream;
            streamPart.InitSkillComponent();
        }
        if (impactPart != null)
        {
            baseSkillState = State.Impact;
            impactPart.InitSkillComponent();
        }
        if (projectilePart != null)
        {
            baseSkillState = State.Projectile;
            projectilePart.InitSkillComponent();
        }
        curSkillState = baseSkillState;

        SortingGroup = GetComponent<SortingGroup>();
        if (SortingGroup == null)
            SortingGroup = gameObject.AddComponent<SortingGroup>();
        SortingGroup.sortingLayerName = nameof(Skill);
        SortingGroup.sortingOrder = 100;
    }

    public void InitSkillData(SkillData skillData, WeaponSkillData weaponSkillData)
    {
        this.skillData = skillData;
        this.weaponSkillData = weaponSkillData;

        if (streamPart != null)
            streamPart.InitStreamSkillData();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *             * Deactive On Map Load *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    protected override void SetDeactiveOnMapLoad()
    {
        SkillRefCount = 0;

        if (projectilePart != null)
            projectilePart.SetDeactiveOnMapLoad();
        if (impactPart != null)
            impactPart.SetDeactiveOnMapLoad();
        if (streamPart != null)
            streamPart.SetDeactiveOnMapLoad();

        gameObject.SetActive(false);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *               * Skill Activation *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void ActivateSkill()
    {
        if (isSkillActivated)
            return;

        isSkillActivated = true;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Skill Casting *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // Used Character.
    public void CastSkill_SM(Character skillCastCharacter, Vector2? castPosition)
    {
        SkillCastCharacter = skillCastCharacter;
        SkillTargetUnit = SkillCastUnit.TargetUnit;
        SkillCastUnit.ActivateRefSkillList.Add(this);

        StartCoroutine(CastSkillCo(castPosition, true));
    }

    // Used Monster.
    public void CastSkill_SM(Monster skillCastMonster, Vector2? castPosition)
    {
        SkillCastMonster = skillCastMonster;
        SkillTargetUnit = SkillCastUnit.TargetUnit;
        SkillCastUnit.ActivateRefSkillList.Add(this);

        StartCoroutine(CastSkillCo(castPosition, true));
    }

    IEnumerator CastSkillCo(Vector2? castPosition, bool isSkillCastedByCharacter)
    {
        if (!castPosition.HasValue)
        {
            castPosition = SkillCastUnit.GetSkillCastingPosition(this, projectilePart != null, isSkillOnField);
            transform.position = castPosition.Value;
        }

        if (isDisplaySkillWarning)
        {
            // TODO: 임팩트, 혹은 스트림 스킬 파트를 경고 표시할 건데, 프로젝틸이 날라가야한다 => 신규 바닥 지정형 스킬 아직 구현하지 않음
            // TODO: 해당 스킬은 날라가는데 시간 혹은 속도가 정해져 있어야 함, 그리고 무엇보다 중요한건, 스킬의 목적지가 정해져 있어야한다는 것

            Skill_PartBase skill_PartBase = null;
            if (skillWarningPart == SkillWarningEffect.SkillWarningPart.ProjectilePart)
                skill_PartBase = projectilePart;
            else if (skillWarningPart == SkillWarningEffect.SkillWarningPart.ImpactPart)
                skill_PartBase = impactPart;
            else if (skillWarningPart == SkillWarningEffect.SkillWarningPart.StreamPart)
                skill_PartBase = streamPart;

            skillWarningEffect = ObjectManager.Instance.GetSkillWarningEffect(new SkillWarningEffect.SkillWarningEffectData(this, skill_PartBase, startPositionType, trackingType));
        }

        yield return waitSkillActivating;

        if (skillWarningEffect != null)
            skillWarningEffect.StopRotateAndTracking();

        if (!SkillCastUnit.IsUnitAlive())
        {
            gameObject.SetActive(false);
            yield break;
        }

        if (isSkillCastedByCharacter)
        {
            // 회피 스킬 경우만의 회전각도 계산과 이펙트 소환.
            if (skillData.Id == Character.CharacterUnitSkillKind.Dodge.ToCachedString())
            {
                if (CombatManager.Instance.Joystick.Direction != Vector2.zero)
                    rotationDirectionVec2 = CombatManager.Instance.Joystick.Direction.normalized;
                else
                    rotationDirectionVec2 = SkillCastUnit.CurLookingAt == DragonBonesUnit.UnitLookingAt.Left ? Vector2.left : Vector2.right;

                rotationAngle = Mathf.Atan2(rotationDirectionVec2.y, rotationDirectionVec2.x) * Mathf.Rad2Deg;

                SummonLaunchEffect(SkillCastUnit.transform.position);
            }
        }

        if (baseSkillState != State.Projectile && skillWarningEffect != null)
            castPosition = skillWarningEffect.transform.position;

        CastSkill(baseSkillState, castPosition.Value, true);
    }

    void CastSkill(State skillState, Vector2 castPosition, bool isApplyOffset = false)
    {
        // Apply offset.
        if (weaponSkillData != null && isApplyOffset)
        {
            if (weaponSkillData.Offset.HasValue && weaponSkillData.Offset.Value != Vector2.zero)
            {
                castPosition.x += (SkillCastUnit.CurLookingAt == DragonBonesUnit.UnitLookingAt.Right ? 1 : -1) * weaponSkillData.Offset.Value.x;
                castPosition.y += weaponSkillData.Offset.Value.y;
            }

            // Apply random position.
            if (weaponSkillData.RndRng.HasValue && weaponSkillData.RndRng.Value != 0)
                castPosition += Random.insideUnitCircle * weaponSkillData.RndRng.Value;
        }

        // Calc rotate angle. Projectile rotate on 'LaunchProjectile' method.
        if (skillState != State.Projectile && rotationDirectionVec2 == Vector2.zero)
        {
            if (skillData.Target == Target.Self)
            {
                if (SkillTargetUnit.IsUnitAlive())
                    rotationDirectionVec2 = SkillTargetUnit.Position.GetNomalizedVector2From(SkillCastUnit.Position);
                else
                    rotationDirectionVec2 = SkillCastUnit.CurLookingAt == DragonBonesUnit.UnitLookingAt.Left ? Vector2.left : Vector2.right;
            }
            else
                rotationDirectionVec2 = castPosition.GetNomalizedVector2From(SkillCastUnit.Position);

            rotationAngle = Mathf.Atan2(rotationDirectionVec2.y, rotationDirectionVec2.x) * Mathf.Rad2Deg;
        }

        // Rotate skill.
        RotateSkill(skillState);

        // Change skill state.
        switch (skillState)
        {
            case State.Projectile:
                LaunchProjectile(SkillTargetUnit, SkillCastUnit.CurLookingAt, castPosition,
                    SkillCastUnit.UnitStatData.ProjSpd_0, SkillCastUnit.UnitStatData.ProjTime_0, SkillCastUnit.UnitStatData.Spread_180);
                break;

            case State.Impact:
                SummonImapct(castPosition);
                break;

            case State.Stream:
                SummonArea(castPosition);
                break;
        }

        // Relese skill warning effect.
        if (skillState != State.Projectile)
            if (skillWarningEffect != null)
            {
                skillWarningEffect.gameObject.SetActive(false);
                skillWarningEffect = null;
            }
    }

    void RotateSkill(State skillState)
    {
        // Projectile rotate on 'LaunchProjectile' method.
        switch (skillState)
        {
            case State.Impact:
                if (impactPart.IsRotatable)
                    transform.eulerAngles = rotationAngle * Vector3.forward;
                else
                    transform.eulerAngles = Vector3.zero;
                break;

            case State.Stream:
                if (streamPart.IsRotatable)
                    transform.eulerAngles = rotationAngle * Vector3.forward;
                else
                    transform.eulerAngles = Vector3.zero;
                break;
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *               * Projectile Method *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void LaunchProjectile(DragonBonesUnit targetUnit, DragonBonesUnit.UnitLookingAt curLookingAt, Vector2 launchPostion, float projSpd_0, float projTime_0, float spread_90)
    {
        SummonLaunchEffect(launchPostion);

        transform.position = launchPostion;
        projectilePart.ProjectileLifeTimeX_0 = projTime_0;
        projectilePart.gameObject.SetActive(true);

        // Set direction.
        if (targetUnit.IsUnitAlive())
            directionVec2 = (targetUnit.Position - (Vector2)transform.position).GetProjectileImpactPointDirVec2ByAngle(spread_90);
        else
            directionVec2 = (curLookingAt == DragonBonesUnit.UnitLookingAt.Left ? Vector2.left : Vector2.right).GetProjectileImpactPointDirVec2ByAngle(spread_90);

        var calcedProjSpd = SkillData.ProjSpd * projSpd_0;
        forceVector2 = directionVec2.GetQuarterViewScalar(Vector2.zero, calcedProjSpd) * directionVec2;

        // Rotate skill.
        if (projectilePart.IsRotatable)
            transform.eulerAngles = Mathf.Atan2(directionVec2.y, directionVec2.x) * Mathf.Rad2Deg * Vector3.forward;
        else
            transform.eulerAngles = Vector3.zero;

        // Launch to target.
        rigidbody2D.AddForce(forceVector2, ForceMode2D.Impulse);

        // Homing to target.
        if (SkillData.HomingRate_1 > 0)
        {
            if (skillData.Target == Target.Enemy)
                homingToTargetCo = StartCoroutine(HomingToTargetCo(targetUnit, calcedProjSpd, SkillCastUnit.UnitType == DragonBonesUnit.DragonBonesUnitType.Character ? CombatManager.Instance.MonsterKdTree : CombatManager.Instance.CharacterKdTree));
            else
                homingToTargetCo = StartCoroutine(HomingToTargetCo(targetUnit, calcedProjSpd, targetUnit.UnitType == DragonBonesUnit.DragonBonesUnitType.Character ? CombatManager.Instance.CharacterKdTree : CombatManager.Instance.MonsterKdTree));
        }
    }

    IEnumerator HomingToTargetCo(DragonBonesUnit targetUnit, float calcedProjSpd, KdTree<DragonBonesUnit> kdTree)
    {
        homingTargetUnit = targetUnit;

        while (true)
        {
            yield return waitHomingDelay;

            if (!homingTargetUnit.IsUnitAlive())
                homingTargetUnit = kdTree.FindClosest(transform.position);

            if (homingTargetUnit.IsUnitAlive())
            {
                var directionToTarget = (homingTargetUnit.Position - (Vector2)transform.position).normalized;

                // Rotate skill.
                if (projectilePart.IsRotatable)
                {
                    var rotateAmount = Vector3.Cross(directionVec2, directionToTarget).z;
                    rigidbody2D.angularVelocity = rotateAmount * 180;
                }

                // Homing to target.
                // ? 시존 방향으로 벡터와 새로운 목적지의 방향 벡터를 합해 새로운 방향 벡터를 구한다. 이때 유도 비율이 사용된다.
                var releativeDirection = directionToTarget * skillData.HomingRate_1 + rigidbody2D.velocity.normalized * (1 - skillData.HomingRate_1);
                // ? 기존 방향과 목적지의 방향이 일치하면 일치율 100%(속력 감소 0%).
                var homingDirectionMathRate = (directionToTarget + rigidbody2D.velocity.normalized).magnitude * 0.5f;
                rigidbody2D.velocity = directionToTarget.GetQuarterViewScalar(Vector2.zero, calcedProjSpd * homingDirectionMathRate) * releativeDirection;
            }
        }
    }

    public void StopSkillMovement()
    {
        if (homingToTargetCo != null)
            StopCoroutine(homingToTargetCo);
        homingToTargetCo = null;

        rigidbody2D.angularVelocity = 0;
        rigidbody2D.velocity = Vector2.zero;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Impact Method *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void SummonImapct(Vector2 castPosition)
    {
        transform.position = castPosition;
        impactPart.ColliderLifeTime = skillData.ImptTime;
        impactPart.gameObject.SetActive(true);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Area Method *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void SummonArea(Vector2 castPosition)
    {
        transform.position = castPosition;
        streamPart.ColliderLifeTime = skillData.StrmTime;
        streamPart.gameObject.SetActive(true);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Summon Effect *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void SummonLaunchEffect(Vector2 position)
    {
        if (!launchEffectPrefabName.IsNullOrEmpty())
            ObjectManager.Instance.GetEffect(launchEffectPrefabName, position);
    }

    public void SummonHitEffect(Vector2 position)
    {
        if (!hitEffectPrefabName.IsNullOrEmpty())
            ObjectManager.Instance.GetEffect(hitEffectPrefabName, position);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *               * State Changed Event *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void ChangeToNextSkillState()
    {
        if (gameObject.activeSelf)
            switch (CurSkillState)
            {
                case State.Projectile:
                    if (impactPart != null)
                        CastSkill(State.Impact, transform.position);
                    else if (streamPart != null)
                        CastSkill(State.Stream, transform.position);
                    else
                        StartCoroutine(SkillDeactiveWhenZeroRefCo());
                    break;

                case State.Impact:
                    if (streamPart != null)
                        CastSkill(State.Stream, transform.position);
                    else
                        StartCoroutine(SkillDeactiveWhenZeroRefCo());
                    break;

                case State.Stream:
                    StartCoroutine(SkillDeactiveWhenZeroRefCo());
                    break;
            }
    }

    IEnumerator SkillDeactiveWhenZeroRefCo()
    {
        yield return new WaitUntil(() => SkillRefCount == 0);

        if (projectilePart != null)
            yield return waitProjectilePartDeactice;
        if (impactPart != null)
            yield return waitImapctPartDeactice;
        if (streamPart != null)
            yield return waitStreamPartDeactice;

        gameObject.SetActive(false);
    }
}

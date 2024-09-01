using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using DG.Tweening;
using Sequence = DG.Tweening.Sequence;

public class Skill : NetworkAddressableMonoBehavior
{
    enum SkillType { Projectile, Impact }
    enum TargetingType { Targeting, NonTargeting }
    enum PiercingType { NonPiercing, Piercing }
    enum TrajectoryType { Direct, Howitzer }

    [Header("Skill Setting")]
    [SerializeField] SkillType skillType;
    [SerializeField] TargetingType targetingType;
    [SerializeField] PiercingType piercingType;
    [SerializeField] TrajectoryType trajectoryType;
    [SerializeField] bool isZeroDamage;
    [SerializeField] bool isLookAtTarget;
    [SerializeField] SkillData_SO summonOtherSkillData_SO;
    [SerializeField] Effect impactEffect;

    [Header("Skill Data")]
    [SerializeField] SkillData_SO skillData_SO;
    SkillData skillData;
    public SkillData SkillData { get => skillData; }
    bool isCritical;
    public bool IsCritical { get => isCritical; }
    bool isCasted;

    [Header("Unit")]
    [SerializeField] Character castCharacter;
    public Character CastCharacter { get => castCharacter; }
    [SerializeField] Monster targetMonster;
    public Monster TargetMonster { get => targetMonster; }

    [Header("Componnent")]
    [SerializeField] Rigidbody2D rigidbody2D;
    [SerializeField] CircleCollider2D circleCollider2D;
    [SerializeField] TrailRenderer trailRenderer;

    Tweener moveTweener;
    Sequence moveSequence;

    WaitForSeconds wait100ms;

    private void Awake()
    {
        skillData = new SkillData(skillData_SO);

        wait100ms = new WaitForSeconds(0.1f);
    }

    public override void OnEnable()
    {
        base.OnEnable();

        if (trailRenderer != null)
            trailRenderer.enabled = true;

        ColliderOn();
    }

    public override void OnDisable()
    {
        base.OnDisable();

        StopAllCoroutines();

        if (moveTweener != null)
        {
            moveTweener.Kill();
            moveTweener = null;
        }

        if (trailRenderer != null)
            trailRenderer.Clear();

        if (isCasted && photonView.IsMine && summonOtherSkillData_SO != null)
        {
            var otherSkill = ObjectManager.Instance.GetSkill(summonOtherSkillData_SO, transform.position);
            otherSkill.castCharacter = CastCharacter;
            otherSkill.targetMonster = TargetMonster;
            otherSkill.CastSkill_Impact(transform.position);
        }

        isCasted = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ? 충돌 처리를 스킬을 만든 유저가 진행함, 만약 반대의 경우에서 계산 시키려면 크리티컬 여부를 RPC를 통해 동기화 시켜야함.
        if (!photonView.IsMine)
            return;

        if (other.CompareTag(NameManager.TagNameList[(int)NameManager.TagName.Monster]))
        {
            if (targetMonster != null && targetingType == TargetingType.Targeting)
                if (other.gameObject != targetMonster.gameObject)
                    return;

            if (!isZeroDamage)
                if (CombatManager.Instance.MonsterGameObjectDictionary.ContainsKey(other.gameObject))
                    CombatManager.Instance.MonsterGameObjectDictionary[other.gameObject].GetDamage(this);

            if (piercingType == PiercingType.NonPiercing)
                SetNetworkDeactive();
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Cast Skill *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void CastSkill(Character castUnit, Monster targetUnit)
    {
        if (photonView.IsMine)
        {
            isCasted = true;

            isCritical = Random.Range(0f, 1f) < castUnit.CurUnitStat_SO.criticalRate ? true : false;

            photonView.RPC(nameof(CastSkill_RPC), RpcTarget.AllBuffered, castUnit.PhotonViewId, targetUnit != null ? targetUnit.PhotonViewId : castUnit.LastTargetUnitViewId);
        }
    }

    [PunRPC]
    void CastSkill_RPC(int castUnitViewId, int targetUnitViewId)
    {
        castCharacter = CombatManager.Instance.CharacterViewIdDictionary[castUnitViewId];

        if (CombatManager.Instance.MonsterViewIdDictionary.ContainsKey(targetUnitViewId))
            targetMonster = CombatManager.Instance.MonsterViewIdDictionary[targetUnitViewId];

        var targetPosition = targetMonster != null ? targetMonster.Position : castCharacter.LastTargetUnitPosition;

        if (isLookAtTarget)
        {
            var directionVec2 = targetPosition - (Vector2)transform.position;
            float degree = Mathf.Atan2(directionVec2.y, directionVec2.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, degree);
        }

        if (skillType == SkillType.Projectile)
            CastSkill_Projectile(targetPosition);
        else
        {
            CastSkill_Impact(targetPosition);

            if (impactEffect != null)
                ObjectManager.Instance.GetEffect(impactEffect.name, targetPosition);
        }
    }

    void CastSkill_Projectile(Vector2 targetPosition)
    {
        transform.position = castCharacter.MuzzleTransform.position;

        if (trajectoryType == TrajectoryType.Howitzer)
            ColliderOnOff(false);

        var jumpPower = 3.5f;

        if (targetingType == TargetingType.Targeting)
        {
            // moveTweener = transform.DOMove(targetPosition, skillData.GetArrivedTime(transform.position, targetPosition)).SetAutoKill(false);
            if (trajectoryType == TrajectoryType.Direct)
                moveTweener = transform.DOMove(targetPosition, SkillData.GetArrivedTime(transform.position, targetPosition));
            else
            {
                var arrivedTime = SkillData.GetArrivedTime(transform.position, targetPosition);
                moveSequence = transform.DOJump(targetPosition, NetworkManager.Instance.IsMasterClient ? jumpPower : -jumpPower, 1, arrivedTime);
                Invoke(nameof(ColliderOn), arrivedTime - 0.5f);
            }

            if (targetMonster != null)
            {
                targetMonster.OnAssetDisabledAct_Init += SetNetworkDeactive;
                // CheckTargetUnitExist();
            }
        }
        else
        {
            var dirEndPosition = targetPosition.normalized * 25f;

            if (trajectoryType == TrajectoryType.Direct)
                moveTweener = transform.DOMove(dirEndPosition, SkillData.GetArrivedTime(transform.position, dirEndPosition));
            else
            {
                var arrivedTime = SkillData.GetArrivedTime(transform.position, targetPosition);
                moveSequence = transform.DOJump(targetPosition, NetworkManager.Instance.IsMasterClient ? jumpPower : -jumpPower, 1, SkillData.GetArrivedTime(transform.position, targetPosition));
                Invoke(nameof(ColliderOn), arrivedTime - 0.5f);
            }
        }

        if (moveTweener != null)
        {
            moveTweener.SetEase(Ease.Linear);
            moveTweener.onComplete += OnTweenCompleted;
        }
        if (moveSequence != null)
            moveSequence.onComplete += OnTweenCompleted;
    }

    void OnTweenCompleted()
    {
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.enabled = false;
        }

        if (targetingType == TargetingType.NonTargeting)
            SetNetworkDeactive();
    }

    // IEnumerator ReCalcArrivedTime()
    // {
    //     while (gameObject.activeSelf || targetMonster.gameObject.activeSelf)
    //     {
    //         moveTweener.ChangeEndValue(targetMonster.transform.position, skillData.GetArrivedTime(transform.position, targetMonster.transform.position));

    //         yield return wait100ms;
    //     }
    // }

    void CastSkill_Impact(Vector2 targetPosition)
    {
        transform.position = targetPosition;

        // ? 임시로 임팩트 스킬은 0.5초로 통일.
        Invoke(nameof(ColliderOff), 0.5f);
        Invoke(nameof(SetNetworkDeactive), 2f);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                * Check Target Unit *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void CheckTargetUnitExist()
    {
        if (photonView.IsMine)
            StartCoroutine(CheckTargetUnitExistCo());
    }

    IEnumerator CheckTargetUnitExistCo()
    {
        while (targetMonster.gameObject.activeSelf)
        {
            yield return null;
        }

        SetNetworkDeactive();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Collider On/Off *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void ColliderOnOff(bool value)
    {
        circleCollider2D.enabled = value;
    }

    void ColliderOn()
    {
        ColliderOnOff(true);
    }

    void ColliderOff()
    {
        ColliderOnOff(false);
    }
}

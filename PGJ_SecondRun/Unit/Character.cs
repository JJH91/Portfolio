using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using DG.Tweening;

public class Character : NetworkAddressableMonoBehavior, IPunInstantiateMagicCallback
{
    public enum UnitState { Idle, Attack, Move }

    [Header("FSM")]
    [SerializeField] UnitState curUnitState;
    public UnitState CurUnitState { get => curUnitState; }

    [Header("Unit Stat")]
    [SerializeField] int atk;
    public int Atk { get => atk; }
    [SerializeField] float curMp;
    public float CurMp { get => curMp; set => curMp = value; }
    [SerializeField] float raange;
    public float Range { get => raange; }

    [Header("Unit Stat_SO")]
    [SerializeField] SummonUnitData_SO curUnitStat_SO;
    public SummonUnitData_SO CurUnitStat_SO { get => curUnitStat_SO; }
    [SerializeField] List<SummonUnitData_SO> unitData_SoList;

    [Header("Skill Data")]
    [SerializeField] SkillData_SO nomalSkillData_SO;
    SkillData nomalSkillData;
    [SerializeField] SkillData_SO enhancedNomalSkillData_SO;
    SkillData enhancedNomalSkillData;
    [SerializeField] SkillData_SO specialSkillData_SO;
    SkillData specialSkillData;

    [Header("Detect Enemy")]
    [SerializeField] UnitSkillRange unitSkillRange;
    public List<Monster> EnemyList { get => unitSkillRange.MonsterList; }
    public Monster TargetMonster { get => unitSkillRange.GetClosestEnemy(); }

    [Header("Muzzle")]
    [SerializeField] Transform muzzleTransform;
    public Transform MuzzleTransform { get => muzzleTransform; }

    [Header("Temp Data")]
    [SerializeField] int lastTargetUnitViewId;
    public int LastTargetUnitViewId { get => lastTargetUnitViewId; }
    [SerializeField] Vector2 lastTargetUnitPosition;
    public Vector2 LastTargetUnitPosition { get => lastTargetUnitPosition; }
    [SerializeField] float tempAttackDelay;

    Tweener moveTweener;

    string unitKey;
    public string UnitKey { get => unitKey; }

    [SerializeField] SpriteRenderer shadowRnderer;

    private void Awake()
    {
        nomalSkillData = new SkillData(nomalSkillData_SO);
        enhancedNomalSkillData = new SkillData(enhancedNomalSkillData_SO);
        specialSkillData = new SkillData(specialSkillData_SO);

        tempAttackDelay = nomalSkillData.SkillData_SO.CoolDown_std;
    }

    public override void OnEnable()
    {
        base.OnEnable();

        CombatManager.Instance.CharacterGameObjectDictionary.Add(gameObject, this);
        CombatManager.Instance.CharacterViewIdDictionary.Add(photonView.ViewID, this);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        CombatManager.Instance.CharacterGameObjectDictionary.Remove(gameObject);
        CombatManager.Instance.CharacterViewIdDictionary.Remove(photonView.ViewID);
    }

    private void Update()
    {
        if (!photonView.IsMine)
            return;

        CurMp = Mathf.Clamp(CurMp + Time.deltaTime, 0, curUnitStat_SO.MaxMp);
        tempAttackDelay -= Time.deltaTime;

        switch (curUnitState)
        {
            case UnitState.Idle:
                if (TargetMonster != null)
                    ChangeState_Attack();
                break;

            case UnitState.Attack:
                if (tempAttackDelay > 0)
                    return;

                if (TargetMonster == null)
                    ChangeState_Idle();
                else
                {
                    lastTargetUnitViewId = TargetMonster.PhotonViewId;
                    lastTargetUnitPosition = TargetMonster.Position;
                }

                if (specialSkillData.UseSkill(this) || enhancedNomalSkillData.UseSkill(this) || nomalSkillData.UseSkill(this))
                    tempAttackDelay = nomalSkillData.SkillData_SO.CoolDown_std;
                break;

            case UnitState.Move:
                break;
        }

        nomalSkillData.UpdateSkillCoolDown(Time.deltaTime);
        enhancedNomalSkillData.UpdateSkillCoolDown(Time.deltaTime);
        specialSkillData.UpdateSkillCoolDown(Time.deltaTime);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Init *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void InitUnit()
    {
        if (photonView.IsMine)
            photonView.RPC(nameof(InitUnit_RPC), RpcTarget.AllBuffered);

        CurMp = 0;
    }

    [PunRPC]
    public void InitUnit_RPC()
    {
        unitSkillRange.SetSkillRange(raange);
        unitSkillRange.ShowSkillRange(false);

        nomalSkillData.CurCoolDown = 0;
        // specialSkillData.CurCoolDown = 0;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * FSM *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Idle *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void ChangeState_Idle()
    {
        curUnitState = UnitState.Idle;

        moveTweener = null;
        // TODO: Idle 애니메이션 재생.
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Attack *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void ChangeState_Attack()
    {
        curUnitState = UnitState.Attack;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                       * Move *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void ChangeState_Move()
    {
        curUnitState = UnitState.Move;

        // TODO: Move 애니메이션 재생.        
    }

    public void SetDest(Vector2 targetPosition)
    {
        ChangeState_Move();

        if (photonView.IsMine)
            photonView.RPC(nameof(Move_RPC), RpcTarget.AllBuffered, targetPosition);
    }

    [PunRPC]
    void Move_RPC(Vector2 targetPosition)
    {
        moveTweener = transform.DOMove(targetPosition, 1f);
        moveTweener.onComplete += ChangeState_Idle;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *          * IPunInstantiateMagicCallback *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        info.Sender.TagObject = gameObject;

        // ? P2 카메라 방향 적용.
        if (!NetworkManager.Instance.IsMasterClient)
            gameObject.transform.rotation = Quaternion.Euler(0, 0, -180);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                * Set Unit Data *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void SetUnit(string unitKey, Color shadowColor, int summonRank)
    {
        if (photonView.IsMine)
            photonView.RPC(nameof(SetUnit_RPC), RpcTarget.AllBuffered, unitKey, summonRank);
    }

    [PunRPC]
    void SetUnit_RPC(string unitKey, int summonRank)
    {
        this.unitKey = unitKey;
        shadowRnderer.color = SummonManager.Instance.RankColors[summonRank];

        Debug.Log($"{name} 유닛 등급: {summonRank}");

        curUnitStat_SO = unitData_SoList[summonRank];
        atk = curUnitStat_SO.attackPower;
        raange = curUnitStat_SO.range;
    }
}

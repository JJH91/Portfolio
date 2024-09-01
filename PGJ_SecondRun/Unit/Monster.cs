using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Monster : NetworkAddressableMonoBehavior, IPunInstantiateMagicCallback
{
    [Header("Monster Status_SO")]
    [SerializeField] MonsterWaveData_SO curMonsterStat_SO;
    [SerializeField] List<MonsterWaveData_SO> MonsterStat_SoList;

    [Header("Monster Status")]
    [SerializeField] int hp;
    public int Hp { get => hp; set { if (value < 0) value = 0; hp = value; } }
    [SerializeField] float speed;
    public float Speed { get => speed; set { if (value < 0) value = 0; speed = value; } }

    [Header("Debuff")]
    [SerializeField] List<int> tempList;

    public Vector2 Position
    {
        get
        {
            if (NetworkManager.Instance.IsMasterClient)
                return (Vector2)transform.position + capsuleCollider2D.offset;
            else
                return (Vector2)transform.position - capsuleCollider2D.offset;
        }
    }

    [SerializeField] bool isArrivedOnCenter;
    public bool IsArrivedOnCenter { get => isArrivedOnCenter; }

    [Header("Component")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Rigidbody2D rigidbody2D;
    [SerializeField] CapsuleCollider2D capsuleCollider2D;

    public override void OnEnable()
    {
        base.OnEnable();

        CombatManager.Instance.MonsterList.Add(this);
        CombatManager.Instance.MonsterGameObjectDictionary.Add(gameObject, this);
        CombatManager.Instance.MonsterViewIdDictionary.Add(photonView.ViewID, this);

        curMonsterStat_SO = MonsterStat_SoList[CombatManager.Instance.CurrentWaveLevel];
        Hp = curMonsterStat_SO.monsterHP;
        Speed = curMonsterStat_SO.monsterSpeed;
    }

    public override void OnDisable()
    {
        base.OnDisable();

        CombatManager.Instance.MonsterList.Remove(this);
        CombatManager.Instance.MonsterGameObjectDictionary.Remove(gameObject);
        CombatManager.Instance.MonsterViewIdDictionary.Remove(photonView.ViewID);

        CombatManager.Instance.CurrentGold += curMonsterStat_SO.monsterGold;
        CombatManager.Instance.virtualCurrencyPanel.UpdateGold(CombatManager.Instance.CurrentGold);
    }

    private void Update()
    {
        MoveToCenter();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(NameManager.TagName.Character.ToString()))
        {
            isArrivedOnCenter = true;

            if (NetworkManager.Instance.IsMasterClient)
            {
                photonView.RPC(nameof(HitCastleByMonster_RPC), RpcTarget.AllBuffered, CombatManager.Instance.currentCastleHP);
                SetNetworkDeactive();
            }
            else
                gameObject.SetActive(false);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(NameManager.TagName.Character.ToString()))
        {
            isArrivedOnCenter = false;
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Init Unit *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void InitUnit()
    {
        isArrivedOnCenter = false;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Move *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void MoveToCenter()
    {
        if (isArrivedOnCenter)
            return;

        spriteRenderer.flipX = NetworkManager.Instance.IsMasterClient ? transform.position.x < 0 : transform.position.x > 0;

        var dirVec = -transform.position.normalized;
        transform.position += speed * Time.deltaTime * dirVec;
        // var dirVec2 = -(Vector2)transform.position.normalized;
        // rigidbody2D.position += speed * Time.deltaTime * dirVec2;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Get Damage *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void GetDamage(Skill skill)
    {
        var damage = Mathf.RoundToInt(skill.IsCritical ? skill.CastCharacter.Atk * skill.CastCharacter.CurUnitStat_SO.criticalDamage : skill.CastCharacter.Atk);
        damage = Mathf.Clamp(damage - curMonsterStat_SO.monsterDefense, 0, damage);

        photonView.RPC(nameof(GetDamage_RPC), RpcTarget.AllBuffered, damage, skill.IsCritical, transform.position);
    }

    [PunRPC]
    void GetDamage_RPC(int damage, bool isCritical, Vector3 dmgIndicatorPosition)
    {
        var damageIndicator = ObjectManager.Instance.GetDamageIndicator();
        damageIndicator.ShowDamage(damage, dmgIndicatorPosition, isCritical ? Color.yellow : Color.red);

        if (NetworkManager.Instance.IsMasterClient)
        {
            Hp -= damage;

            if (Hp <= 0)
                SetNetworkDeactive();
        }
    }

    [PunRPC]
    void HitCastleByMonster_RPC(int curCastleHp)
    {
        CombatManager.Instance.HitCastleByMonster(curCastleHp);
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
}

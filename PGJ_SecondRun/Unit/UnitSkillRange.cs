using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class UnitSkillRange : MonoBehaviour
{
    [Header("Range")]
    [SerializeField] float range;

    [Header("Monster List")]
    [SerializeField] List<Monster> monsterList = new List<Monster>();
    public List<Monster> MonsterList { get => monsterList; }

    [Header("Content UI")]
    [SerializeField] SpriteRenderer rangeSpriteRenderer;

    [Header("Component")]
    [SerializeField] CircleCollider2D circleCollider2D;

    private void OnDisable()
    {
        MonsterList.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(NameManager.TagNameList[(int)NameManager.TagName.Monster]))
        {
            if (CombatManager.Instance.MonsterGameObjectDictionary.ContainsKey(other.gameObject))
                monsterList.Add(CombatManager.Instance.MonsterGameObjectDictionary[other.gameObject]);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(NameManager.TagNameList[(int)NameManager.TagName.Monster]))
        {
            if (CombatManager.Instance.MonsterGameObjectDictionary.ContainsKey(other.gameObject))
                monsterList.Remove(CombatManager.Instance.MonsterGameObjectDictionary[other.gameObject]);
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                 * Set Skill Range *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void SetSkillRange(float range)
    {
        this.range = range;

        transform.localScale = Vector3.one * range;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                * Show Skill Range *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void ShowSkillRange(bool value)
    {
        rangeSpriteRenderer.enabled = value;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                * Get Closest Enemy *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public Monster GetClosestEnemy()
    {
        Monster closestMonster = null;
        float minsDistance = float.MaxValue;
        for (int i = MonsterList.Count - 1; i >= 0; i--)
        {
            if (MonsterList[i] == null || !MonsterList[i].gameObject.activeSelf)
            {
                MonsterList.RemoveAt(i);
                continue;
            }

            var distance = Vector2.Distance(transform.position, MonsterList[i].transform.position);
            if (distance < minsDistance)
            {
                closestMonster = MonsterList[i];
                minsDistance = distance;
            }
        }

        return closestMonster;
    }
}

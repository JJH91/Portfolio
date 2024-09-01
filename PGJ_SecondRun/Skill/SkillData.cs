using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SkillData : MonoBehaviour
{
    [SerializeField] SkillData_SO skillData_SO;
    public SkillData_SO SkillData_SO { get => skillData_SO; }
    public float CurCoolDown { get; set; }

    public SkillData(SkillData_SO skillData_SO)
    {
        if (skillData_SO == null)
            return;

        this.skillData_SO = skillData_SO;

        SkillData_SO.InversedSpeed = 0;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                    * Use Skill *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public bool UseSkill(Character character)
    {
        if (skillData_SO == null || character.TargetMonster == null)
            return false;

        if (CurCoolDown <= 0 && character.CurMp >= skillData_SO.NeedMp)
        {
            CurCoolDown = skillData_SO.CoolDown_std;
            character.CurMp -= skillData_SO.NeedMp;

            var skill = ObjectManager.Instance.GetSkill(skillData_SO, character.MuzzleTransform.transform.position);
            skill.CastSkill(character, character.TargetMonster);

            return true;
        }

        return false;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                * Update Cool Down *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void UpdateSkillCoolDown(float deltaTime)
    {
        if (skillData_SO == null)
            return;

        if (CurCoolDown > 0)
            CurCoolDown -= deltaTime;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                * Get Arrived Time *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public float GetArrivedTime(Vector2 skillPosition, Vector2 targetPosition)
    {
        var distance = Vector2.Distance(skillPosition, targetPosition);

        return distance * skillData_SO.InversedSpeed;
    }
}
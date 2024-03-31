using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DragonBones;
using Sirenix.OdinInspector;
using System.Linq;
using System;
using System.Security.Policy;

public class Weapon : AddressableSerializedMonoBehavior
{
    public enum WeaponGripType { OneHand, TwoHand }
    public enum WeaponSide { Left, Right }

    [TitleGroup("Weapon"), BoxGroup("Weapon/W", showLabel: false)]
    [BoxGroup("Weapon/W/Weapon Manager"), ShowInInspector] public WeaponManager WeaponManager { get; private set; }
    [BoxGroup("Weapon/W/Unity Armature Component"), ShowInInspector] public UnityArmatureComponent ArmatureComponent { get; private set; }
    [BoxGroup("Weapon/W/Armature Slot"), ShowInInspector, ReadOnly] public UnityEngine.Transform MuzzleSlotTransform { get; private set; }
    [BoxGroup("Weapon/W/Fire Animation Name"), SerializeField] string weaponAnimationName, attackAnimationName;
    public string WeaponAnimationName { get => weaponAnimationName; }
    public string AttackAnimationName { get => attackAnimationName; }
    [BoxGroup("Weapon/W/Attack Animation State"), SerializeField] DragonBones.AnimationState attackAnimationState;
    [BoxGroup("Weapon/W/RPM Time Scale"), SerializeField] float rpmTimeScale;
    public float RpmTimeScale { get => rpmTimeScale; }

    protected override void Awake()
    {
        if (!ArmatureComponent)
            ArmatureComponent = GetComponent<UnityArmatureComponent>();

        base.Awake();
    }

    protected override void Start()
    {
        MuzzleSlotTransform = (ArmatureComponent.armature.GetSlot(NameManager.DragonBonesLayerName.MuzzleSlot.ToCachedUncamelCaseString()).display as GameObject).transform;

        ArmatureComponent.armature.eventDispatcher.AddDBEventListener(EventObject.FRAME_EVENT, OnFrameEventHandler);

        base.Start();
    }

    protected override void OnDisable()
    {
        weaponAnimationName = string.Empty;
        attackAnimationName = string.Empty;
        rpmTimeScale = 0;

        base.OnDisable();
    }

    /**
     * --------------------------------------------------
     * Deactive On Map Load
     * --------------------------------------------------
     */

    protected override void SetDeactiveOnMapLoad() { }

    /**
     * --------------------------------------------------
     * Init Weapon Manager
     * --------------------------------------------------
     */

    public void InitWeapon(WeaponManager weaponManager, WeaponSide weaponSide)
    {
        WeaponManager = weaponManager;

        weaponAnimationName = WeaponManager.MasterCharacter.WeaponStatModData.Series.Replace(" ", "_");

        // 무기 외형 및 본 위치 조절용 애니메이션 재생
        ArmatureComponent.armature.animation.Play(weaponAnimationName, 1);

        attackAnimationName = weaponSide switch
        {
            WeaponSide.Right => $"{weaponAnimationName}-Attack_R",
            _ => $"{weaponAnimationName}-Attack_L"
        };
        rpmTimeScale = RpmToTimeScale();
    }

    /**
     * --------------------------------------------------
     * Animation
     * --------------------------------------------------
     */

    public float GetAttackAnimationPlayedTime()
    {
        return ArmatureComponent.animation.GetState(AttackAnimationName).currentTime;
    }

    public float GetAttackAnimationTotalTime()
    {
        // return ArmatureComponent.animation.GetState(AttackAnimationName).totalTime;
        return ArmatureComponent.animation.animations[AttackAnimationName].duration;
    }

    public void PlayAttackAnimationByTime(float playedTime)
    {
        ArmatureComponent.armature.animation.timeScale = rpmTimeScale;
        ArmatureComponent.armature.animation.GotoAndPlayByTime(attackAnimationName, playedTime, 0);
    }

    public void StopAnimation()
    {
        ArmatureComponent.armature.animation.timeScale = 1;
        ArmatureComponent.armature.animation.Play(weaponAnimationName, 1);
    }

    /**
     * --------------------------------------------------
     * RPM To Time Scale
     * --------------------------------------------------
     */


    // 사격 애니메이션의 타임 스케일을 결정 (1 배속시에 사격 수로 나눔)
    // Used Weapon Manager
    public float RpmToTimeScale()
    {
        // Based weapon animation time
        return WeaponManager.MasterCharacter.WeaponStatModData.EquipmentType switch
        {
            PlayFabManager.CatalogItemTag.Pistol => WeaponManager.MasterCharacter.WeaponStatModData.Rpm / 120f, // Dual Weapon x2 ( 1 shot : 60 sec, x2 )
            _ => WeaponManager.MasterCharacter.WeaponStatModData.Rpm / 60f,
        };
    }

    /**
     * --------------------------------------------------
     * Fire
     * --------------------------------------------------
     */
    int test = 0;
    DateTime firstDt, lastDt;
    void Fire()
    {
        if (WeaponManager.MasterCharacter.WeaponStatModData.EquipmentType == PlayFabManager.CatalogItemTag.Shotgun)
            for (int i = 0; i < WeaponManager.MasterCharacter.WeaponStatModData.Shot; i++)
            {
                var bullet = ObjectManager.Instance.GetBulletSkill(WeaponManager.CurBulletSkillData);
                bullet.CastSkill_SM(WeaponManager.MasterCharacter, MuzzleSlotTransform.position);
            }
        else
        {
            var bullet = ObjectManager.Instance.GetBulletSkill(WeaponManager.CurBulletSkillData);
            bullet.CastSkill_SM(WeaponManager.MasterCharacter, MuzzleSlotTransform.position);
        }

        test++;
        if (test % WeaponManager.MasterCharacter.WeaponStatModData.Rpm == 0)
        {
            lastDt = DateTime.UtcNow;
            gameObject.Log($"{test}번째 발사됨, 1 RPM 도달 시간차: {(lastDt - firstDt).TotalSeconds}");
            test = 0;
        }
        else if (test == 1)
            firstDt = DateTime.UtcNow;
    }

    /**
     * --------------------------------------------------
     * Animation Event Listener
     * --------------------------------------------------
     */

    void OnFrameEventHandler(string type, EventObject eventObject)
    {
        if (eventObject.name.Equals(NameManager.FrameEventName.Attack.ToCachedString()))
        {
            WeaponManager.MakeCharacterLookAtTarget();
            Fire();
        }
    }
}

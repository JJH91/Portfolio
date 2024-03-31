using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DragonBones;
using Sirenix.OdinInspector;
// TODO: NPC 애니메이션 재생법을 만들어야함
public class DragonBonesUGUI : AddressableSerializedMonoBehavior
{
    [TitleGroup("DragonBones UGUI"), BoxGroup("DragonBones UGUI/DBUGUI", showLabel: false)]
    [BoxGroup("DragonBones UGUI/DBUGUI/Is Inited DragonBones UGUI"), SerializeField] bool isInited;
    [BoxGroup("DragonBones UGUI/DBUGUI/Unity Armature Component"), SerializeField] UnityArmatureComponent armatureComponent;
    public UnityArmatureComponent ArmatureComponent { get => armatureComponent; }
    [BoxGroup("DragonBones UGUI/DBUGUI/RectTransform"), SerializeField] RectTransform rectTransform;
    public RectTransform RectTransform { get => rectTransform; }
    [BoxGroup("DragonBones UGUI/DBUGUI/Standard Flip"), SerializeField] bool standardFlipX;
    public bool StandardFlipX { get => standardFlipX; }
    [BoxGroup("DragonBones UGUI/DBUGUI/Has Flip Animation"), SerializeField] bool hasFlipAnimation;
    [BoxGroup("DragonBones UGUI/DBUGUI/Unit Type"), SerializeField] DragonBonesUnit.DragonBonesUnitType unitType;
    public DragonBonesUnit.DragonBonesUnitType UnitType { get => unitType; set => unitType = value; }
    [BoxGroup("DragonBones UGUI/DBUGUI/Weapon DragonBones UGUI"), ShowIf("@UnitType == DragonBonesUnit.DragonBonesUnitType.Character"), SerializeField] DragonBonesUGUI weapon_L, weapon_R;
    [BoxGroup("DragonBones UGUI/DBUGUI/Character Armature Slot, Layer"), ShowIf("@UnitType == DragonBonesUnit.DragonBonesUnitType.Character"), SerializeField] UnityEngine.Transform leftHandSlotTr, rightHandSlotTr;
    // [BoxGroup("DragonBones UGUI/DBUGUI/Character Armature Slot, Layer"), ShowIf("@UnitType == DragonBonesUnit.DragonBonesUnitType.Character"), SerializeField] UnityEngine.Transform leftHandLayerTr, rightHandLayerTr;
    // [BoxGroup("DragonBones UGUI/DBUGUI/Character Armature Slot, Layer"), ShowIf("@UnitType == DragonBonesUnit.DragonBonesUnitType.Character"), SerializeField] UnityEngine.Transform copiedLeftHandLayerTr, copiedRightHandLayerTr;
    // [BoxGroup("DragonBones UGUI/DBUGUI/Canvas Group"), SerializeField] CanvasGroup canvasGroup;
    WaitUntil waitUntilAnimationComplete;
    protected override void Awake()
    {
        if (!isInited)
        {
            gameObject.Log($"프리펩이 미리 초기화되어 있지 않습니다. 프리펩을 미리 초기화하지 않으면 문제가 있을 수 있습니다.");
            Init();
        }

        base.Awake();
    }

    protected override void Start()
    {
        waitUntilAnimationComplete = new WaitUntil(() => armatureComponent.armature.animation.isCompleted);

        armatureComponent.armature.flipX = standardFlipX;

        // Character can't play animation immediatly because it need weapon data.
        if (unitType != DragonBonesUnit.DragonBonesUnitType.Character && unitType != DragonBonesUnit.DragonBonesUnitType.Weapon)
            PlayUnitAnimation(DragonBonesUnit.DragonBonesUnitState.Idle.ToCachedString(), armatureComponent.armature.flipX);

        base.Start();
    }

    protected override void OnDisable()
    {
        armatureComponent.armature.flipX = standardFlipX;

        base.OnDisable();
    }

    /**
     * --------------------------------------------------
     * Deactive On Map Load
     * --------------------------------------------------
     */

    protected override void SetDeactiveOnMapLoad() { }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Init *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("DragonBones UGUI/DBUGUI/Init"), Button("Init", ButtonSizes.Gigantic), GUIColor(0.35f, 0.7f, 1)]
    void Init()
    {
        if (armatureComponent == null)
            armatureComponent = transform.GetComponentInChildren<UnityArmatureComponent>();

        rectTransform = GetComponent<RectTransform>();

        StartCoroutine(InitUGUICo());

        gameObject.Log($"초기화 완료");
        isInited = true;
    }

    IEnumerator InitUGUICo()
    {
        yield return new WaitUntil(() => armatureComponent.armature != null);

        // 캐릭터 무기 장착 슬롯
        if (unitType == DragonBonesUnit.DragonBonesUnitType.Character)
        {
            leftHandSlotTr = (armatureComponent.armature.GetSlot(NameManager.DragonBonesLayerName.LeftHandSlot.ToCachedUncamelCaseString()).display as GameObject).transform;
            rightHandSlotTr = (armatureComponent.armature.GetSlot(NameManager.DragonBonesLayerName.RightHandSlot.ToCachedUncamelCaseString()).display as GameObject).transform;
        }

        standardFlipX = armatureComponent.armature.flipX;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *           * DragonBones UGUI Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void PlayUnitAnimation(string animationName, bool isFlipX, int repeat = 0, float timeScale = 1, bool isPlayIdleOnComplete = false)
    {
        armatureComponent.armature.flipX = isFlipX;
        armatureComponent.armature.animation.timeScale = timeScale;
        armatureComponent.armature.animation.Play(hasFlipAnimation && armatureComponent.armature.flipX ? $"{animationName}_Flip" : animationName, repeat);

        if (repeat != 0 && isPlayIdleOnComplete)
            StartCoroutine(PlayPreAnimationCo());
    }
    IEnumerator PlayPreAnimationCo()
    {
        yield return waitUntilAnimationComplete;

        PlayUnitAnimation(DragonBonesUnit.DragonBonesUnitState.Idle.ToCachedString(), ArmatureComponent.armature.flipX);
    }

    /// <summary>
    /// Play animation on UI.
    /// </summary>
    public void PlayCharacterAnimationByWeaponStatModData(CharacterStatData characterStatData, WeaponStatModifierData weaponStatModData, Character.CharacterPosture unitPosture = Character.CharacterPosture.Peace, int repeat = 0, float timeScale = 1)
    {
        string animationName;

        if (characterStatData != null && weaponStatModData != null)
        {
            EquipCharacterWeapon(weaponStatModData.EquipmentType, weaponStatModData.Series);

            if (weaponStatModData.EquipmentType == PlayFabManager.CatalogItemTag.Pistol)
                animationName = $"{DragonBonesUnit.DragonBonesUnitState.Idle.ToCachedString()}-One_Hand-{unitPosture}";
            else
                animationName = $"{DragonBonesUnit.DragonBonesUnitState.Idle.ToCachedString()}-Two_Hand-{unitPosture}";
        }
        else
        {
            // 장착한 무기가 없는 상태의 애니메이션 재생을 위해 무기 비활성화.
            if (weapon_L != null)
                weapon_L.gameObject.SetActive(false);
            if (weapon_R != null)
                weapon_R.gameObject.SetActive(false);
            animationName = $"{DragonBonesUnit.DragonBonesUnitState.Idle.ToCachedString()}-One_Hand-{unitPosture}";
            if (!armatureComponent.animation.animationNames.Contains(animationName))
                animationName = $"{DragonBonesUnit.DragonBonesUnitState.Idle.ToCachedString()}-Two_Hand-{unitPosture}";
        }

        if (!armatureComponent.animation.animationNames.Contains(animationName))
            animationName = DragonBonesUnit.DragonBonesUnitState.Idle.ToCachedString();

        PlayUnitAnimation(animationName, armatureComponent.armature.flipX, repeat, timeScale);
    }

    /// <summary>
    /// Play same animation of character parameter in game.
    /// </summary>
    public void PlayCharacterAnimation(Character character, string animationName = "", int repeat = 0, float timeScale = 1)
    {
        EquipCharacterWeapon(character.Weapon.WeaponStatModData.EquipmentType, character.Weapon.WeaponStatModData.Name);

        if (animationName == string.Empty)
            animationName = character.ArmatureComponent.animation.lastAnimationName;

        PlayUnitAnimation(animationName, character.ArmatureComponent.armature.flipX, repeat, timeScale);
    }

    public void PlayCharacterAnimation(PlayFabManager.CatalogItemTag weaponType, string weaponName, string animationName, bool isFlipX = false, int repeat = 0, float timeScale = 1)
    {
        EquipCharacterWeapon(weaponType, weaponName);

        PlayUnitAnimation(animationName, isFlipX, repeat, timeScale);
    }

    public void StopAnimation()
    {
        armatureComponent.animation.Stop();
    }

    void EquipCharacterWeapon(PlayFabManager.CatalogItemTag weaponType, string weaponSeries)
    {
        var weaponAnimationName = weaponSeries.Replace(" ", "_");

        // ! 아래의 버그가 고쳐졌는지 확인이 안됨 후에 고쳐진걸로 판단되면 주석 지울 것
        // 기존 활성화된 무기가 있으면 비활성화 => 무기가 이중으로 비활성화 되면서 파괴되는 버그가 있음. 아마도 상위 캐릭터가 비활성화되면서 무기가 큐에 들어가고, 다시 캐릭터가 활성화되면 총들이 큐에있는 상태로 활성화되는게 문제인 것 같음
        // 다른 타입의 무기로 바꿀때, 무기를 비활성화 시키지 않으면 버그가 날텐데...
        if (weapon_L != null)
            weapon_L.gameObject.SetActive(false);
        if (weapon_R != null)
            weapon_R.gameObject.SetActive(false);

        if (weaponType == PlayFabManager.CatalogItemTag.Pistol)
        {
            // 무기 UGUI 생성
            weapon_L = ObjectManager.Instance.GetDragonBonesWeaponUGUI(weaponType);
            weapon_R = ObjectManager.Instance.GetDragonBonesWeaponUGUI(weaponType);

            EquipWeapon(weapon_L.armatureComponent, leftHandSlotTr, weaponAnimationName, NameManager.DragonBonesLayerName.LeftWeaponOrderLayer);
            EquipWeapon(weapon_R.armatureComponent, rightHandSlotTr, weaponAnimationName, NameManager.DragonBonesLayerName.RightWeaponOrderLayer);
        }
        else
        {
            // 무기 UGUI 생성
            weapon_L = ObjectManager.Instance.GetDragonBonesWeaponUGUI(weaponType);

            EquipWeapon(weapon_L.armatureComponent, leftHandSlotTr, weaponAnimationName, NameManager.DragonBonesLayerName.LeftWeaponOrderLayer);
        }
    }

    void EquipWeapon(UnityArmatureComponent weapon, UnityEngine.Transform followingTr, string weaponAnimationName, NameManager.DragonBonesLayerName sibilingLayerName)
    {
        weapon.transform.SetParent(armatureComponent.transform, false);

        // 레이아웃 정렬.
        var siblingIndex = (armatureComponent.armature.GetSlot(sibilingLayerName.ToCachedUncamelCaseString()).display as GameObject).transform.GetSiblingIndex();
        weapon.transform.SetSiblingIndex(siblingIndex);

        // 위치 업데이트.
        StartCoroutine(EquipWeaponCo(weapon.transform, followingTr));

        // ? 업데이트가 유의미한지 모르겠음
        // weapon.armature.InvalidUpdate();
        weapon.animation.Play(weaponAnimationName, 1);
    }

    IEnumerator EquipWeaponCo(UnityEngine.Transform weaponTr, UnityEngine.Transform followingTr)
    {
        while (weaponTr != null && weaponTr.gameObject.activeSelf && followingTr != null && followingTr.gameObject.activeSelf)
        {
            weaponTr.localPosition = followingTr.localPosition;
            weaponTr.localEulerAngles = followingTr.localEulerAngles;

            yield return null;
        }
    }

    // ! Deprecated, 무기의 배치를 손 레이어의 자식에 위치시켜서 코루틴 등으로 위치를 업데이트하는 방법을 사용하지 않는 장점이 있지만, 몸 위로 무기가 올라오는 등의 order에 문제가 있어서 사용하지 않음
    // void EquipCharacterWeapon(string weaponType, string weaponName)
    // {
    //     var weaponAnimationName = weaponName.Replace(" ", "_");

    //     if (weaponType == Weapon.WeaponType.Pistol.ToCachedString())
    //     {
    //         // 무기 UGUI 생성
    //         weapon_L = ObjectManager.Instance.GetDragonBonesWeaponUGUI(weaponType);
    //         weapon_R = ObjectManager.Instance.GetDragonBonesWeaponUGUI(weaponType);

    //         // 덮어쓸 레이어 복사
    //         if (copiedLeftHandLayerTr == null)
    //             copiedLeftHandLayerTr = Instantiate(leftHandLayerTr);
    //         if (copiedRightHandLayerTr == null)
    //             copiedRightHandLayerTr = Instantiate(rightHandLayerTr);

    //         EquipWeapon(weapon_L.armatureComponent, leftHandSlotTr, leftHandLayerTr, copiedLeftHandLayerTr, weaponAnimationName);
    //         EquipWeapon(weapon_R.armatureComponent, rightHandSlotTr, rightHandLayerTr, copiedRightHandLayerTr, weaponAnimationName);
    //     }
    //     else
    //     {
    //         // ? 한 손 무기는 무기의 위치를 손에 맞추는데 이때 자식오브젝트로 삽입하면 무기가 손 위에 표시된다.
    //         // ? 그래서 무기를 손 아래로 위치시키기 위해서 손 레이어를 복사한 다음, 자식오브젝트로 넣어서 무기위에 복사된 손 레이어를 표시시키는 방법을 사용했다.
    //         // ? 하지만 양 손 무기는 가슴 부분에 무기를 자식 오브젝트로 주고, 손이 따라가는 방식을 사용하려고 한다.
    //         // ? 계속 따라다니게 (코루틴 등으로) 할 생각은 없고, 손의 포커스에 오프셋을 먹여서 애니메이션을 재생시키려고 함
    //         // ? 현재는 권총만 구현했기 때문에 그게 실제 가능한지는 테스트와 실험이 필요함
    //         // ? 현재 코드는 변경하지 않은 상태

    //         // 무기 UGUI 생성
    //         weapon_L = ObjectManager.Instance.GetDragonBonesWeaponUGUI(weaponType);

    //         // 무기 장착 슬롯
    //         var chestSlotTr = (armature.GetSlot(NameManager.DragonBonesSlotName.Chest_Slot.ToCachedString()).display as GameObject).transform;

    //         // EquipWeapon(weapon_L.armatureComponent, chestSlotTr, weaponAnimationName, NameManager.DragonBonesSlotName.Chest_Layer, 1);
    //     }

    //     // 무기 자리 배치후 투명해제
    //     // weapon_L.canvasGroup.alpha = 1;
    //     // weapon_R.canvasGroup.alpha = 1;
    // }

    // void EquipWeapon(UnityArmatureComponent weapon, UnityEngine.Transform handSlot, UnityEngine.Transform handLayer, UnityEngine.Transform copiedHandLayer, string weaponAnimationName)
    // {
    //     copiedHandLayer.SetParent(handSlot);
    //     copiedHandLayer.position = handLayer.position;
    //     copiedHandLayer.localScale = new Vector3(1, 1, 1);

    //     weapon.transform.SetParent(handSlot);
    //     weapon.transform.SetZeroLocalPositionAndAngles();
    //     weapon.transform.SetSiblingIndex(0);

    //     weapon.armature.InvalidUpdate();
    //     weapon.animation.Play(weaponAnimationName, 1);
    // }
}

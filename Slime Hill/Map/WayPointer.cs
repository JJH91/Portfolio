using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;

public class WayPointer : MonoBehaviour, IMapPointerObject
{
    public enum WayPointerTargetType { Character, Monster, Boss }

    [TitleGroup("Way Pointer"), BoxGroup("Way Pointer/WP", showLabel: false)]
    [BoxGroup("Way Pointer/WP/Way Pointer Data"), OnValueChanged("OnWayPointerUnitTypeChanged"), SerializeField] WayPointerTargetType wayPointerTarget;
    public WayPointerTargetType WayPointerTarget { get => wayPointerTarget; }
    [BoxGroup("Way Pointer/WP/Way Pointer Data"), SerializeField] public int nextPointerIndex;
    public int NextPointerIndex { get => nextPointerIndex; set => nextPointerIndex = value; }
    [BoxGroup("Way Pointer/WP/Way Pointer Data"), OnValueChanged("OnWayPointerNumberChanged"), SerializeField] int wayPointerNumber;
    public float WayPointerNumber { get => wayPointerNumber; }
    [BoxGroup("Way Pointer/WP/Way Pointer Data"), OnValueChanged("OnRangeChanged"), SerializeField] float range;
    public float Range { get => range; }

    [TitleGroup("UI"), BoxGroup("UI/U", showLabel: false)]
    [BoxGroup("UI/U/Information ON OFF"), SerializeField, ReadOnly] bool isShowInformation = true;
    public bool IsShowInformation
    {
        get => isShowInformation;
        set
        {
            isShowInformation = value;
            InformationOnOff(value);
        }
    }
    [BoxGroup("UI/U/Content UI"), SerializeField] List<SpriteRenderer> spriteRendererList;
    [BoxGroup("UI/U/Content UI"), SerializeField] TextMeshPro wayPointNumberText;
    [BoxGroup("UI/U/Content UI"), SerializeField] List<Color> colorList;

    [TitleGroup("Component"), BoxGroup("Component/C", showLabel: false)]
    [BoxGroup("Component/C/Colider 2D"), SerializeField] PolygonCollider2D polygonCollider2D;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(wayPointerTarget.ToCachedString()) && other is BoxCollider2D)
            CombatManager.Instance.UnitDictionary.GetOrAdd(other, other.GetComponent<DragonBonesUnit>()).OnWayPointArrived(this);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Init *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Way Pointer/WP/Init"), Button("Init", ButtonSizes.Gigantic, ButtonStyle.Box, Expanded = true), GUIColor("@ExtensionClass.GuiCOLOR_Blue")]
    public void Init()
    {
        OnWayPointerUnitTypeChanged();
        OnWayPointerNumberChanged();
        OnRangeChanged();
    }

    [BoxGroup("Component/C/Draw Ellipse Colider 2D"), Button("Draw Ellipse Colider 2D", ButtonSizes.Gigantic, ButtonStyle.Box, Expanded = true), GUIColor("@ExtensionClass.GuiCOLOR_Blue")]
    void DrawEllipseColider2D(float radius = 0.5f)
    {
        var newPoints = new Vector2[24];

        var angle = 0f;
        var angleStep = 2f * Mathf.PI / 24;
        for (int i = 0; i < 24; i++)
        {
            newPoints[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            angle += angleStep;
        }

        polygonCollider2D.points = newPoints;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                     * Methods *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [BoxGroup("Way Pointer/WP/Information ON OFF"), Button("Information ON OFF", ButtonSizes.Gigantic), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
    void InformationOnOFf()
    {
        IsShowInformation = !IsShowInformation;

        InformationOnOff(IsShowInformation);
    }

    public void InformationOnOff(bool onoff)
    {
        foreach (var spriteRenderer in spriteRendererList)
            spriteRenderer.enabled = onoff;
        wayPointNumberText.enabled = onoff;
    }

    public Vector3 GetCenterPosition()
    {
        return transform.position;
    }

    public Vector3 GetRandomPositionInRange(float range = 0)
    {
        if (range == 0)
            range = Range;

        return transform.position.GetRandomInsidePositionAsQuarterView(range * 2);
    }

    void OnWayPointerUnitTypeChanged()
    {
        foreach (var spriteRenderer in spriteRendererList)
            spriteRenderer.color = colorList[(int)WayPointerTarget];
    }

    void OnWayPointerNumberChanged()
    {
        wayPointNumberText.text = $"{WayPointerNumber}";
    }

    void OnRangeChanged()
    {
        transform.localScale = new Vector2(Range, Range * 0.5f);
    }
}

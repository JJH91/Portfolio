using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;
using Sirenix.OdinInspector;

[RequireComponent(typeof(LineRenderer)), RequireComponent(typeof(Mesh)), RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(SortingGroup))]
public class SkillWarningEffect : Effect
{
    public enum SkillWarningPart { ProjectilePart, ImpactPart, StreamPart };
    public enum SkillWarningTrackingType { LookingAtTarget, LockOn, Tracking };
    public enum SkillWarningStartPosition { CastPosition, TargetUnit, CastUnit };

    [TitleGroup("Skill Warning Effect"), BoxGroup("Skill Warning Effect/SWE", showLabel: false)]
    [BoxGroup("Skill Warning Effect/SWE/Max Vertext Count"), SerializeField] int vertexCount = 24;
    [BoxGroup("Skill Warning Effect/SWE/Skill Warning Effect Data"), SerializeField] SkillWarningEffectData skillWarningEffectData;
    [BoxGroup("Skill Warning Effect/SWE/Warning Color"), SerializeField] List<Gradient> warningLineGradientList;
    [BoxGroup("Skill Warning Effect/SWE/Warning Color"), SerializeField] List<Color> warningMeshColorList;
    [BoxGroup("Skill Warning Effect/SWE/Rotate"), SerializeField] Vector2 rotationDirectionVec2;
    [BoxGroup("Skill Warning Effect/SWE/Rotate"), SerializeField] float rotationAngle;

    [TitleGroup("Renderer"), BoxGroup("Renderer/R", showLabel: false)]
    [BoxGroup("Renderer/R/Line"), SerializeField] LineRenderer lineRenderer;
    [BoxGroup("Renderer/R/Mesh"), SerializeField] int textureRepeatCount = 1;
    [BoxGroup("Renderer/R/Mesh"), SerializeField] Mesh mesh;
    [BoxGroup("Renderer/R/Mesh"), SerializeField] MeshFilter meshFilter;
    [BoxGroup("Renderer/R/Mesh"), SerializeField] MeshRenderer meshRenderer;

    Tweener moveTweener;

    protected override void Awake()
    {
        if (mesh == null)
            mesh = new Mesh { name = nameof(mesh) };

        base.Awake();
    }

    protected override void OnEnable()
    {
        Clear();

        base.OnEnable();
    }

    protected override void OnDisable()
    {
        moveTweener.Kill();

        base.OnDisable();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Test *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [TitleGroup("Test"), BoxGroup("Test/T", showLabel: false)]
    [BoxGroup("Test/T/Test"), Button("Test", ButtonSizes.Gigantic, ButtonStyle.Box, Expanded = true), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
    public void Test(int index)
    {
        if (index == 0)
        {
            mesh.colors = new Color[mesh.vertexCount];
            for (int i = 0; i < mesh.vertexCount; i++)
                mesh.colors[i] = skillWarningEffectData.Skill.IsSkillOnField ? warningMeshColorList[0] : warningMeshColorList[1];
        }
        else if (index == 1)
        {
            meshRenderer.material.color = skillWarningEffectData.Skill.IsSkillOnField ? warningMeshColorList[0] : warningMeshColorList[1];
        }
        else
        {
            meshRenderer.material.SetColor("_Color", skillWarningEffectData.Skill.IsSkillOnField ? warningMeshColorList[0] : warningMeshColorList[1]);
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                      * Init *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    protected override void InitEffect()
    {
        base.InitEffect();

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = false;
#if UNITY_EDITOR
        lineRenderer.material = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
#endif
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.sortingLayerName = nameof(Effect);
        lineRenderer.sortingOrder = 100;

        if (mesh == null)
            mesh = new Mesh { name = nameof(mesh) };
        mesh.Clear();

        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        meshRenderer = GetComponent<MeshRenderer>();
#if UNITY_EDITOR
        meshRenderer.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Skill Warning Material.mat");
#endif
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                  * Draw Method *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [TitleGroup("Draw Skill Warning"), BoxGroup("Draw Skill Warning/DSW", showLabel: false)]
    [BoxGroup("Draw Skill Warning/DSW/Clear"), Button("Clear", ButtonSizes.Large), GUIColor("@ExtensionClass.GuiCOLOR_Red")]
    public void Clear()
    {
        lineRenderer.positionCount = 0;
        mesh.Clear();

# if UNITY_EDITOR
        transform.position = Vector3.zero;
        transform.eulerAngles = Vector3.zero;
        transform.localScale = Vector3.one;
#endif
    }

    public void DrawSkillWarning(Vector3[] vertices, bool isDrawLine, bool isDrawMesh)
    {
        if (isDrawLine)
        {
            lineRenderer.positionCount = vertices.Length;
            lineRenderer.SetPositions(vertices);
        }

        if (isDrawMesh)
            DrawSkillWarningMesh(vertices);

        transform.position = skillWarningEffectData.Skill.transform.position;
        if (!(skillWarningEffectData.Skill_PartBase is Skill_ProjectilePart)) // ? 발사체의 경우, 크기는 커지지만 비행 거리는 그대로 이므로, 스케일을 적용하지 않는다. 대신, 라인 및 메시를 그릴때 발사체의 스케일을 적용한다.
            transform.localScale = skillWarningEffectData.Skill.transform.localScale;
        transform.localEulerAngles = skillWarningEffectData.Skill_PartBase.transform.eulerAngles;

        StartCoroutine(RotateAndTrackingCo());
        if (skillWarningEffectData.Skill_PartBase is Skill_ProjectilePart)
            StartCoroutine(ApplyProjectileWarningQuaterViewRangeCo(transform.localScale.x));
    }

    public void DrawSkillWarning(SkillWarningEffectData skillWarningEffectData, bool isDrawLine, bool isDrawMesh)
    {
        this.skillWarningEffectData = skillWarningEffectData;

        DrawSkillWarningLine(isDrawLine, isDrawMesh);

        transform.position = skillWarningEffectData.Skill.transform.position;
        if (!(skillWarningEffectData.Skill_PartBase is Skill_ProjectilePart)) // ? 발사체의 경우, 크기는 커지지만 비행 거리는 그대로 이므로, 스케일을 적용하지 않는다. 대신, 라인 및 메시를 그릴때 발사체의 스케일을 적용한다.
            transform.localScale = skillWarningEffectData.Skill.transform.localScale;
        transform.localEulerAngles = skillWarningEffectData.Skill_PartBase.transform.eulerAngles;

        StartCoroutine(RotateAndTrackingCo());
        if (skillWarningEffectData.Skill_PartBase is Skill_ProjectilePart)
            StartCoroutine(ApplyProjectileWarningQuaterViewRangeCo(transform.localScale.x));
    }

    IEnumerator RotateAndTrackingCo()
    {
        var isSkillOnField = skillWarningEffectData.Skill.IsSkillOnField;
        var isRotatable = skillWarningEffectData.Skill_PartBase.IsRotatable;
        var castUnit = skillWarningEffectData.Skill.SkillCastUnit;
        var targetUnit = skillWarningEffectData.Skill.SkillTargetUnit;
        var skillRange = skillWarningEffectData.Skill.SkillData.Range;
        var trackingSpeed = skillWarningEffectData.Skill.TrackingSpeed;

        Vector3 castUnitPosition;
        Vector3 targetUnitPosition;
        float distanceFromCastUnit;
        float distanceFromEffect;

        // Set Start Position. Default is skill position
        if (skillWarningEffectData.StartPositionType == SkillWarningStartPosition.TargetUnit)
            transform.position = isSkillOnField ? targetUnit.transform.position : (Vector3)targetUnit.Position;
        else if (skillWarningEffectData.StartPositionType == SkillWarningStartPosition.CastUnit)
            transform.position = isSkillOnField ? castUnit.transform.position : (Vector3)castUnit.Position;

        // Tracking target Unit with rotating.
        if (skillWarningEffectData.TrackingType == SkillWarningTrackingType.LookingAtTarget)
        {
            if (isRotatable || skillWarningEffectData.Skill_PartBase is Skill_ProjectilePart)
                while (gameObject.activeSelf)
                {
                    RotateSkillWarningEffect();

                    yield return null;
                }
        }
        else if (skillWarningEffectData.TrackingType == SkillWarningTrackingType.LockOn)
        {
            while (gameObject.activeSelf)
            {
                if (isSkillOnField)
                {
                    targetUnitPosition = targetUnit.transform.position;
                    castUnitPosition = castUnit.transform.position;
                }
                else
                {
                    targetUnitPosition = targetUnit.Position;
                    castUnitPosition = castUnit.Position;
                }
                distanceFromCastUnit = targetUnitPosition.GetQuarterViewDistanceFrom(castUnitPosition);

                if (distanceFromCastUnit <= skillRange)
                    transform.position = targetUnitPosition;
                else
                    transform.position = castUnitPosition + (targetUnitPosition - castUnitPosition).normalized * targetUnitPosition.GetQuarterViewScalar(castUnitPosition, skillRange);

                if (isRotatable)
                    RotateSkillWarningEffect();

                yield return null;
            }
        }
        else if (skillWarningEffectData.TrackingType == SkillWarningTrackingType.Tracking)
        {
            // Init tweener.
            moveTweener = transform.DOMove(transform.position, 0).SetAutoKill(false);

            while (gameObject.activeSelf)
            {
                if (isSkillOnField)
                {
                    targetUnitPosition = targetUnit.transform.position;
                    castUnitPosition = castUnit.transform.position;
                }
                else
                {
                    targetUnitPosition = targetUnit.Position;
                    castUnitPosition = castUnit.Position;
                }
                distanceFromCastUnit = targetUnitPosition.GetQuarterViewDistanceFrom(castUnitPosition);
                distanceFromEffect = targetUnitPosition.GetQuarterViewDistanceFrom(transform.position);

                if (distanceFromCastUnit <= skillRange)
                    moveTweener.ChangeEndValue(targetUnitPosition, distanceFromEffect / trackingSpeed, true).Restart();
                else
                    moveTweener.ChangeEndValue(castUnitPosition + (targetUnitPosition - castUnitPosition).normalized * targetUnitPosition.GetQuarterViewScalar(castUnitPosition, skillRange), distanceFromEffect / trackingSpeed, true).Restart();

                if (isRotatable)
                    RotateSkillWarningEffect();

                yield return null;
            }
        }
    }

    void RotateSkillWarningEffect()
    {
        if (skillWarningEffectData.Skill.SkillData.Target == Skill.Target.Self)
        {
            if (skillWarningEffectData.Skill.SkillTargetUnit.IsUnitAlive())
                rotationDirectionVec2 = skillWarningEffectData.Skill.SkillTargetUnit.Position.GetNomalizedVector2From(skillWarningEffectData.Skill.SkillCastUnit.Position);
            else
                rotationDirectionVec2 = skillWarningEffectData.Skill.SkillCastUnit.CurLookingAt == DragonBonesUnit.UnitLookingAt.Left ? Vector2.left : Vector2.right;
        }
        else
        {
            if (skillWarningEffectData.Skill.SkillTargetUnit.IsUnitAlive())
                rotationDirectionVec2 = (skillWarningEffectData.Skill.IsSkillOnField ? (Vector2)skillWarningEffectData.Skill.SkillTargetUnit.transform.position : skillWarningEffectData.Skill.SkillTargetUnit.Position)
                                            .GetNomalizedVector2From(transform.position);
            // else
            //     rotationDirectionVec2 = (skillWarningEffectData.skill.IsSkillOnField ? (Vector2)skillWarningEffectData.skill.SkillCastUnit.transform.position : skillWarningEffectData.skill.SkillCastUnit.Position).GetNomalizedVector2From(transform.position);
        }

        rotationAngle = Mathf.Atan2(rotationDirectionVec2.y, rotationDirectionVec2.x) * Mathf.Rad2Deg;

        transform.localEulerAngles = skillWarningEffectData.Skill_PartBase.transform.eulerAngles + rotationAngle * Vector3.forward;
    }

    IEnumerator ApplyProjectileWarningQuaterViewRangeCo(float originScaleX)
    {
        originScaleX *= 1.2f;

        while (gameObject.activeSelf)
        {
            transform.localScale = new Vector3((1 - Mathf.Abs(Mathf.Sin(transform.eulerAngles.z * Mathf.Deg2Rad) * 0.5f)) * originScaleX, transform.localScale.y, transform.localScale.z);

            yield return null;
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *              * Draw Skill Warning *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    [TitleGroup("Draw Skill Warning"), BoxGroup("Draw Skill Warning/DSW", showLabel: false)]
    [BoxGroup("Draw Skill Warning/DSW/Draw Skill Warning"), Button("Draw Skill Warning", ButtonSizes.Gigantic, ButtonStyle.Box, Expanded = true), GUIColor("@ExtensionClass.GuiCOLOR_Green")]
    void DrawSkillWarningLine(bool isDrawLine, bool isDrawMesh)
    {
#if UNITY_EDITOR
        if (!isDrawLine && !isDrawMesh)
        {
            isDrawLine = true;
            isDrawMesh = true;
        }

        Clear();
#endif

        if (skillWarningEffectData.Skill.BaseSkillState != skillWarningEffectData.Skill.CurSkillState)
            return;

        Vector3[] vertices = null;
        var angle = 0f;
        var angleStep = 2f * Mathf.PI / vertexCount;

        if (skillWarningEffectData.Skill_PartBase is Skill_ProjectilePart)
        {
            // ? 발사체의 경우, 크기는 커지지만 비행 거리는 그대로 이므로, 발사체에 대해서만 스케일을 적용한다. 또한, 여기서 이미 스케일을 적용했으므로, 이후에 경고 이펙트의 스케일은 1로 놔둔다.
            var scaledHalfOfSizeX = skillWarningEffectData.Size.x * 0.5f * skillWarningEffectData.Skill.transform.localScale.x;
            var scaledHalfOfSizeY = skillWarningEffectData.Size.y * 0.5f * skillWarningEffectData.Skill.transform.localScale.y;
            if (scaledHalfOfSizeY < 0.5f) // 최소 폭 적용.
                scaledHalfOfSizeY = 0.5f;

            var skillRange = skillWarningEffectData.Skill.SkillData.ProjSpd * skillWarningEffectData.Skill.SkillData.ProjTime;
            skillRange *= skillWarningEffectData.Skill.SkillCastCharacter != null ? skillWarningEffectData.Skill.SkillCastCharacter.WeaponStatModData.ProjSpdX_0 : skillWarningEffectData.Skill.SkillCastMonster.MonsterStatData.ProjSpd_0;
            skillRange *= skillWarningEffectData.Skill.SkillCastCharacter != null ? skillWarningEffectData.Skill.SkillCastCharacter.WeaponStatModData.ProjTimeX_0 : skillWarningEffectData.Skill.SkillCastMonster.MonsterStatData.ProjTime_0;

            vertices = new Vector3[] {
                        new Vector2(-scaledHalfOfSizeX, -scaledHalfOfSizeY) + skillWarningEffectData.ScaledOffset,
                        new Vector2(scaledHalfOfSizeX + skillRange, -scaledHalfOfSizeY) + skillWarningEffectData.ScaledOffset,
                        new Vector2(scaledHalfOfSizeX + skillRange, scaledHalfOfSizeY) + skillWarningEffectData.ScaledOffset,
                        new Vector2(-scaledHalfOfSizeX, scaledHalfOfSizeY) + skillWarningEffectData.ScaledOffset};

            if (skillWarningEffectData.Skill_PartBase.transform.localRotation.eulerAngles.z != 0)
                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] = vertices[i].GetRotatePointFromZeroPoint(skillWarningEffectData.Skill_PartBase.transform.localRotation.eulerAngles.z);
        }
        else
            switch (skillWarningEffectData.ColliderType)
            {
                case Skill_PartBase.ColliderType.None:
                    return;

                case Skill_PartBase.ColliderType.Box:
                    var halfOfSizeX = skillWarningEffectData.Size.x * 0.5f;
                    var halfOfSizeY = skillWarningEffectData.Size.y * 0.5f;

                    vertices = new Vector3[] {
                        new Vector2(-halfOfSizeX, -halfOfSizeY) + skillWarningEffectData.ScaledOffset,
                        new Vector2(halfOfSizeX, -halfOfSizeY) + skillWarningEffectData.ScaledOffset,
                        new Vector2(halfOfSizeX, halfOfSizeY) + skillWarningEffectData.ScaledOffset,
                        new Vector2(-halfOfSizeX, halfOfSizeY) + skillWarningEffectData.ScaledOffset};

                    if (skillWarningEffectData.Skill_PartBase.transform.localRotation.eulerAngles.z != 0)
                        for (int i = 0; i < vertices.Length; i++)
                            vertices[i] = vertices[i].GetRotatePointFromZeroPoint(skillWarningEffectData.Skill_PartBase.transform.localRotation.eulerAngles.z);
                    break;

                case Skill_PartBase.ColliderType.Circle:
                    vertices = new Vector3[vertexCount];

                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] = skillWarningEffectData.Radius * Mathf.Max(skillWarningEffectData.Skill_PartBase.transform.localScale.x, skillWarningEffectData.Skill_PartBase.transform.localScale.y) * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) + skillWarningEffectData.ScaledOffset;
                        angle += angleStep;
                    }

                    if (skillWarningEffectData.Skill_PartBase.transform.localRotation.eulerAngles.z != 0)
                        for (int i = 0; i < vertices.Length; i++)
                            vertices[i] = vertices[i].GetRotatePointFromZeroPoint(skillWarningEffectData.Skill_PartBase.transform.localRotation.eulerAngles.z);
                    break;

                case Skill_PartBase.ColliderType.Capsule:
                    var halfOfDist = Mathf.Abs(skillWarningEffectData.Size.x * skillWarningEffectData.Skill_PartBase.transform.localScale.x - skillWarningEffectData.Size.y * skillWarningEffectData.Skill_PartBase.transform.localScale.y) * 0.5f;
                    var radius = Mathf.Min(skillWarningEffectData.Size.x * skillWarningEffectData.Skill_PartBase.transform.localScale.x, skillWarningEffectData.Size.y * skillWarningEffectData.Skill_PartBase.transform.localScale.y) * 0.5f;
                    vertices = new Vector3[halfOfDist == 0 ? vertexCount : vertexCount + 2];

                    bool isSkipAssignment;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        isSkipAssignment = false;

                        var x = Mathf.Cos(angle) * radius;
                        var y = Mathf.Sin(angle) * radius;

                        if (halfOfDist != 0)
                        {
                            if (skillWarningEffectData.Direction == CapsuleDirection2D.Horizontal)
                            {
                                // ? 길이가 확장되는 부분을 x 혹은 y가 0인 것으로 구분하지 않는 이유는 소수점 오차로 인해 값이 정확히 0이 안나오기 떄문에 인덱스로 판단.
                                // ? 여기서 인덱스는 각도가 90도가 되는 부분을 계산해줘야함. 그것도 360의 공약수로 정점 수를 할당하고, 추가되는 점이 2개 중에 1개가 앞서 계산되었음을 상기.
                                if (i != 6 && i != 19)
                                {
                                    if (x >= 0)
                                        x += halfOfDist;
                                    else
                                        x -= halfOfDist;
                                }
                                else
                                {
                                    vertices[i] = (x >= 0 ? new Vector2(x + halfOfDist, y) : new Vector2(x - halfOfDist, y)) + skillWarningEffectData.ScaledOffset;
                                    vertices[++i] = (x >= 0 ? new Vector2(x - halfOfDist, y) : new Vector2(x + halfOfDist, y)) + skillWarningEffectData.ScaledOffset;

                                    isSkipAssignment = true;
                                }
                            }
                            else
                            {
                                // ? 세로 방향의 경우에는 인덱스는 첫 정점이 0이라 추가되는 점이 2개 있지만, 앞서 계산된 점은 제일 마지막 인덱스에 할당되어 각도가 90도가 되는 인덱스를 그대로 쓴다.
                                if (i != 0 && i != 12)
                                {
                                    if (y >= 0)
                                        y += halfOfDist;
                                    else
                                        y -= halfOfDist;
                                }
                                else
                                {
                                    vertices[i] = (y >= 0 ? new Vector2(x, y + halfOfDist) : new Vector2(x, y - halfOfDist)) + skillWarningEffectData.ScaledOffset;
                                    if (i == 0) // ? 정점을 찍는 순서는 반시계방향이어야 하는데, 방향이 세로인 경우 첫 정점은 마지막 인덱스의 정점과 쌍을 이룸.
                                        vertices[vertices.Length - 1] = new Vector2(x, y - halfOfDist) + skillWarningEffectData.ScaledOffset;
                                    else
                                        vertices[++i] = new Vector2(x, y - halfOfDist) + skillWarningEffectData.ScaledOffset;

                                    isSkipAssignment = true;
                                }
                            }
                        }

                        if (!isSkipAssignment)
                            vertices[i] = new Vector2(x, y) + skillWarningEffectData.ScaledOffset;
                        angle += angleStep;
                    }

                    if (skillWarningEffectData.Skill_PartBase.transform.localRotation.eulerAngles.z != 0)
                        for (int i = 0; i < vertices.Length; i++)
                            vertices[i] = vertices[i].GetRotatePointFromZeroPoint(skillWarningEffectData.Skill_PartBase.transform.localRotation.eulerAngles.z);
                    break;

                case Skill_PartBase.ColliderType.Polygon:
                    vertices = skillWarningEffectData.Points
                        .Select(v2 =>
                            {
                                v2.x *= skillWarningEffectData.Skill_PartBase.transform.localScale.x;
                                v2.y *= skillWarningEffectData.Skill_PartBase.transform.localScale.y;
                                return (Vector3)(v2 + skillWarningEffectData.ScaledOffset);
                            }).ToArray();

                    if (skillWarningEffectData.Skill_PartBase.transform.localRotation.eulerAngles.z != 0)
                        for (int i = 0; i < vertices.Length; i++)
                            vertices[i] = vertices[i].GetRotatePointFromZeroPoint(skillWarningEffectData.Skill_PartBase.transform.localRotation.eulerAngles.z);
                    break;
            }

        if (isDrawLine)
        {
            lineRenderer.positionCount = vertices.Length;
            lineRenderer.SetPositions(vertices);
            lineRenderer.colorGradient = skillWarningEffectData.Skill.IsSkillOnField ? warningLineGradientList[0] : warningLineGradientList[1];
        }

        if (isDrawMesh)
            DrawSkillWarningMesh(vertices);
    }

    void DrawSkillWarningMesh(Vector3[] vertices)
    {
        // ? 콜라이더를 사용하여 메시를 그리는 방법.
        // mesh = collider2D.CreateMesh(false, true);
        // var newVertices = new Vector3[mesh.vertexCount];
        // for (int i = 0; i < mesh.vertexCount; i++)
        // {
        //     newVertices[i] = (mesh.vertices[i] - (Vector3)((Vector2)skill.transform.position)).RotatePointFromZeroPoint(-skill.transform.localRotation.eulerAngles.z);
        //     newVertices[i].x /= skill.transform.localScale.x;
        //     newVertices[i].y /= skill.transform.localScale.y;
        // }
        // mesh.vertices = newVertices;

        var triangleCount = vertices.Length - 2;
        var triangles = new int[triangleCount * 3];
        for (int i = 0; i < triangleCount; i++)
        {
            // ? 시계방향으로 삼각형을 그려줘야 메시가 보임.
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 2;
            triangles[i * 3 + 2] = i + 1;
        }

        // var uv = new Vector2[vertices.Length];
        // if (skillWarningEffectData.Skill_PartBase is Skill_ProjectilePart)
        // {
        //     var size = vertices[2];
        //     for (int i = 0; i < uv.Length; i++)
        //         uv[i] = 0.5f * textureRepeatCount * (new Vector2(vertices[i].x / size.x, vertices[i].y / size.y) + Vector2.one);
        // }
        // else
        //     for (int i = 0; i < uv.Length; i++)
        //         uv[i] = 0.5f * textureRepeatCount * (new Vector2(vertices[i].x / skillWarningEffectData.Size.x, vertices[i].y / skillWarningEffectData.Size.y) + Vector2.one);

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        // mesh.uv = uv;


        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshRenderer.material.color = skillWarningEffectData.Skill.IsSkillOnField ? warningMeshColorList[0] : warningMeshColorList[1];
    }

    public class SkillWarningEffectData
    {
        public Skill Skill { get; set; }
        public Skill_PartBase Skill_PartBase { get; set; }
        public Skill_PartBase.ColliderType ColliderType { get; set; }
        public SkillWarningStartPosition StartPositionType { get; set; }
        public SkillWarningTrackingType TrackingType { get; set; }
        public Vector2 Size { get; set; }
        public CapsuleDirection2D Direction { get; set; }
        public float Radius { get; set; }
        public Vector2[] Points { get; set; }
        public Vector2 ScaledOffset { get; set; }

        public SkillWarningEffectData(Skill skill, Skill_PartBase skill_PartBase, SkillWarningStartPosition startPositionType = SkillWarningStartPosition.TargetUnit, SkillWarningTrackingType trackingType = SkillWarningTrackingType.LockOn)
        {
            this.Skill = skill;
            this.Skill_PartBase = skill_PartBase;
            this.StartPositionType = startPositionType;
            this.TrackingType = trackingType;
            ScaledOffset = (skill_PartBase.transform.localPosition +
                new Vector3(skill_PartBase.Collider2D.offset.x * skill_PartBase.transform.localScale.x, skill_PartBase.Collider2D.offset.y * skill_PartBase.transform.localScale.y)
            .GetRotatePointFromZeroPoint(skill_PartBase.transform.localEulerAngles.z))
            .GetRotatePointFromZeroPoint(-skill_PartBase.transform.localEulerAngles.z);
            Points = null;

            if (skill_PartBase.Collider2D is BoxCollider2D)
            {
                ColliderType = Skill_PartBase.ColliderType.Box;
                Size = (skill_PartBase.Collider2D as BoxCollider2D).size;
                Direction = Size.x >= Size.y ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical;
                Radius = 0;
            }
            else if (skill_PartBase.Collider2D is CircleCollider2D)
            {
                ColliderType = Skill_PartBase.ColliderType.Circle;
                Direction = CapsuleDirection2D.Horizontal;
                Radius = (skill_PartBase.Collider2D as CircleCollider2D).radius;
                Size = Vector2.one * Radius;
                Points = null;
            }
            else if (skill_PartBase.Collider2D is CapsuleCollider2D)
            {
                ColliderType = Skill_PartBase.ColliderType.Capsule;
                Size = (skill_PartBase.Collider2D as CapsuleCollider2D).size;
                Direction = (skill_PartBase.Collider2D as CapsuleCollider2D).direction;
                Radius = (Direction == CapsuleDirection2D.Horizontal ? Size.y : Size.x) * 0.5f;
                Points = null;
            }
            else if (skill_PartBase.Collider2D is PolygonCollider2D)
            {
                ColliderType = Skill_PartBase.ColliderType.Polygon;
                Size = skill_PartBase.Collider2D.bounds.size;
                Direction = Size.x >= Size.y ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical;
                Radius = 0;
                Points = (skill_PartBase.Collider2D as PolygonCollider2D).points;
            }
            else
                Debug.LogError($"콜라이더 타입이 명확하지 않음.");
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *               * Stop All Coroutines *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void StopRotateAndTracking()
    {
        StopAllCoroutines();
    }
}

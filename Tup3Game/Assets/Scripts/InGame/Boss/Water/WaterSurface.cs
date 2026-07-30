using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterSurface : MonoBehaviour
{
    [Header("표면 형태")]
    [SerializeField] private float width = 10f;          // 물 표면 전체 가로 폭 (고정값)
    [SerializeField] private int columnCount = 40;        // 컬럼 개수 (해상도, 많을수록 부드러움)
    [SerializeField] private float waterBottomY = -3f;    // 물 몸체의 바닥 y (로컬 좌표, 화면 아래로 안 보이는 지점)

    [Header("스프링 파라미터")]
    [SerializeField] private float springConstant = 20f;  // 클수록 빨리 원위치로 복원 (너무 크면 젤리처럼 튐)
    [SerializeField] private float damping = 0.98f;        // 클수록(1에 가까울수록) 안 죽고 오래 출렁임
    [SerializeField] private float spreadSpeed = 1.5f;     // 옆 컬럼으로 퍼지는 속도
    [SerializeField] private int spreadIterations = 8;     // 한 프레임에 전파를 몇 번 반복할지 (많을수록 매끄럽게 퍼짐)

    private float[] heights;       // 각 컬럼의 baseLevels[i] 기준 상대 오프셋 (스프링으로 출렁이는 값)
    private float[] velocities;
    private float[] leftDeltas;
    private float[] rightDeltas;

    private float[] baseLevels;    // 컬럼별 기준 수위 (절대 로컬 y). 균일하게 쓰려면 BaseLevel 프로퍼티 사용.
    private float uniformBaseLevel; // BaseLevel 프로퍼티로 마지막에 균일 설정한 값 (getter/콜라이더 계산용)

    private Mesh mesh;
    private Vector3[] vertices;
    private float spacing;

    // ── 벽(장애물)에 의한 막힘 처리 ──
    private class WallBoundary
    {
        public int boundaryIndex; // 컬럼 i와 i+1 사이 경계
        public float topY;        // 벽의 윗면 높이 (로컬 y, 절대값)
    }
    private readonly List<WallBoundary> wallBoundaries = new();

    /// <summary>
    /// 전체 컬럼에 동일한 기준 수위를 적용합니다 (기존 아래→위 차오름 방식과 호환).
    /// 컬럼별로 다른 값을 주려면 SetColumnBaseLevel을 사용하세요.
    /// </summary>
    public float BaseLevel
    {
        get => uniformBaseLevel;
        set
        {
            uniformBaseLevel = value;
            for (int i = 0; i < columnCount; i++)
                baseLevels[i] = value;
        }
    }

    private void Awake()
    {
        spacing = width / (columnCount - 1);

        heights = new float[columnCount];
        velocities = new float[columnCount];
        leftDeltas = new float[columnCount];
        rightDeltas = new float[columnCount];
        baseLevels = new float[columnCount];

        BaseLevel = waterBottomY; // 처음엔 안 보이게 바닥에 깔아둠

        BuildMesh();
    }

    private void BuildMesh()
    {
        mesh = new Mesh { name = "WaterSurfaceMesh" };
        GetComponent<MeshFilter>().mesh = mesh;

        vertices = new Vector3[columnCount * 2]; // 각 컬럼마다 위(표면) + 아래(바닥) 정점
        var uv = new Vector2[vertices.Length];
        var triangles = new int[(columnCount - 1) * 6];

        for (int i = 0; i < columnCount; i++)
        {
            float x = -width / 2f + i * spacing;
            vertices[i * 2] = new Vector3(x, baseLevels[i] + heights[i], 0f);  // 표면
            vertices[i * 2 + 1] = new Vector3(x, waterBottomY, 0f);           // 바닥

            uv[i * 2] = new Vector2((float)i / (columnCount - 1), 1f);
            uv[i * 2 + 1] = new Vector2((float)i / (columnCount - 1), 0f);

            if (i < columnCount - 1)
            {
                int vi = i * 2;
                int ti = i * 6;
                triangles[ti + 0] = vi;
                triangles[ti + 1] = vi + 2;
                triangles[ti + 2] = vi + 1;

                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + 2;
                triangles[ti + 5] = vi + 3;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.MarkDynamic();
        mesh.RecalculateBounds();
    }

    private void Update()
    {
        SimulateSpring(Time.deltaTime);
        SpreadWave(Time.deltaTime);
        UpdateMeshVertices();
    }

    private void SimulateSpring(float dt)
    {
        for (int i = 0; i < columnCount; i++)
        {
            velocities[i] += -springConstant * heights[i] * dt;
            velocities[i] *= damping;
            heights[i] += velocities[i] * dt;
        }
    }

    private void SpreadWave(float dt)
    {
        for (int iter = 0; iter < spreadIterations; iter++)
        {
            for (int i = 0; i < columnCount; i++)
            {
                if (i > 0 && !IsBoundaryBlocked(i - 1))
                {
                    leftDeltas[i] = spreadSpeed * (heights[i] - heights[i - 1]);
                    velocities[i - 1] += leftDeltas[i];
                }
                else
                {
                    leftDeltas[i] = 0f;
                }

                if (i < columnCount - 1 && !IsBoundaryBlocked(i))
                {
                    rightDeltas[i] = spreadSpeed * (heights[i] - heights[i + 1]);
                    velocities[i + 1] += rightDeltas[i];
                }
                else
                {
                    rightDeltas[i] = 0f;
                }
            }

            for (int i = 0; i < columnCount; i++)
            {
                if (i > 0) heights[i - 1] += leftDeltas[i] * dt;
                if (i < columnCount - 1) heights[i + 1] += rightDeltas[i] * dt;
            }
        }
    }

    private void UpdateMeshVertices()
    {
        for (int i = 0; i < columnCount; i++)
        {
            vertices[i * 2].y = baseLevels[i] + heights[i];
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }

    /// <summary>월드 x 좌표 위치에 충격을 줍니다. force는 velocity에 직접 더해지는 값 (양수=위로 솟음, 음수=아래로 파임)</summary>
    public void AddImpulse(float worldX, float force)
    {
        int index = WorldXToColumnIndex(worldX);
        velocities[index] += force;
    }

    /// <summary>컬럼 인덱스를 직접 알고 있을 때 사용하는 impulse (파도 선두 트리거 등에 사용)</summary>
    public void AddImpulseAtColumn(int index, float force)
    {
        if (index < 0 || index >= columnCount) return;
        velocities[index] += force;
    }

    /// <summary>
    /// 컬럼의 높이 오프셋(heights)을 즉시 특정 값으로 세팅합니다.
    /// 파도가 "순간적으로 치솟았다가 스프링에 의해 무너져 내리는" 연출에 사용합니다.
    /// resetVelocity를 true로 두면 이전 속도를 지우고 깔끔하게 시작합니다.
    /// </summary>
    public void SetColumnHeightOffset(int index, float offset, bool resetVelocity = true)
    {
        if (index < 0 || index >= columnCount) return;
        heights[index] = offset;
        if (resetVelocity) velocities[index] = 0f;
    }

    public float GetColumnHeightOffset(int index)
    {
        if (index < 0 || index >= columnCount) return 0f;
        return heights[index];
    }

    /// <summary>특정 컬럼의 기준 수위를 개별적으로 설정합니다. (파도가 지나간 자리만 수위를 올리는 등에 사용)</summary>
    public void SetColumnBaseLevel(int index, float level)
    {
        if (index < 0 || index >= columnCount) return;
        baseLevels[index] = level;
    }

    public float GetColumnBaseLevel(int index)
    {
        if (index < 0 || index >= columnCount) return 0f;
        return baseLevels[index];
    }

    /// <summary>컬럼 인덱스에 대응하는 월드 x 좌표.</summary>
    public float GetColumnWorldX(int index)
    {
        float localX = -width / 2f + index * spacing;
        return transform.TransformPoint(new Vector3(localX, 0f, 0f)).x;
    }

    private int WorldXToColumnIndex(float worldX)
    {
        float localX = transform.InverseTransformPoint(new Vector3(worldX, 0f, 0f)).x;
        int index = Mathf.RoundToInt((localX + width / 2f) / spacing);
        return Mathf.Clamp(index, 0, columnCount - 1);
    }

    /// <summary>표면의 왼쪽/오른쪽 끝 월드 x 좌표.</summary>
    public float GetLeftEdgeWorldX() => transform.TransformPoint(new Vector3(-width / 2f, 0f, 0f)).x;
    public float GetRightEdgeWorldX() => transform.TransformPoint(new Vector3(width / 2f, 0f, 0f)).x;
    public int ColumnCount => columnCount;
    public float Width => width;
    public float Spacing => spacing;

    // ── 벽(장애물) 등록 ──

    /// <summary>
    /// 지정한 월드 x 위치에 벽을 등록합니다. 벽 높이(wallTopWorldY) 밑으로는 물이 못 넘어가고,
    /// 그 높이를 넘어서면 자동으로 흘러넘치듯 다시 이어집니다.
    /// 반환된 boundaryIndex를 저장해뒀다가 벽이 사라질 때 RemoveWall로 제거하세요.
    /// </summary>
    public int AddWall(float worldX, float wallTopWorldY)
    {
        float localX = transform.InverseTransformPoint(new Vector3(worldX, 0f, 0f)).x;
        int boundaryIndex = Mathf.Clamp(
            Mathf.RoundToInt((localX + width / 2f) / spacing),
            0, columnCount - 2);

        float localTopY = transform.InverseTransformPoint(new Vector3(0f, wallTopWorldY, 0f)).y;

        wallBoundaries.Add(new WallBoundary { boundaryIndex = boundaryIndex, topY = localTopY });
        return boundaryIndex;
    }

    public void RemoveWall(int boundaryIndex)
    {
        wallBoundaries.RemoveAll(w => w.boundaryIndex == boundaryIndex);
    }

    public void ClearAllWalls() => wallBoundaries.Clear();

    /// <summary>경계(컬럼 i와 i+1 사이)가 벽으로 막혀있는지 확인. 파도 선두가 벽 앞에서 멈추는 연출 등에 사용 가능.</summary>
    public bool IsBoundaryBlocked(int boundaryIndex)
    {
        foreach (var wall in wallBoundaries)
        {
            if (wall.boundaryIndex != boundaryIndex) continue;

            float leftSurface = baseLevels[boundaryIndex] + heights[boundaryIndex];
            float rightSurface = baseLevels[boundaryIndex + 1] + heights[boundaryIndex + 1];

            // 양쪽 다 벽 높이보다 낮으면 아직 막힌 상태. 한쪽이라도 넘으면 흘러넘침(개방).
            if (leftSurface < wall.topY && rightSurface < wall.topY)
                return true;
        }
        return false;
    }
}

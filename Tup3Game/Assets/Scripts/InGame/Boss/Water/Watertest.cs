using UnityEngine;

[RequireComponent(typeof(WaterSurface))]
public class Watertest : MonoBehaviour
{
    [Header("참조 (같은 오브젝트에 있으면 자동으로 찾음)")]
    [SerializeField] private WaterSurface surface;
    [SerializeField] private TsunamiWaveSequence tsunami;

    [Header("테스트용 벽 설정")]
    [SerializeField] private float wallTopHeight = 1.5f; // 클릭 지점 기준 벽 윗면 높이(로컬 y 절대값)
    [SerializeField] private GameObject wallVisualPrefab; // 선택: 벽 위치를 눈으로 보여줄 프리팹 (없어도 동작함)

    private Camera cam;
    private int lastWallBoundaryIndex = -1;
    private GameObject lastWallVisual;

    private void Awake()
    {
        if (surface == null)
            surface = GetComponent<WaterSurface>();

        if (tsunami == null)
            tsunami = GetComponent<TsunamiWaveSequence>();

        cam = Camera.main;

        if (cam == null)
            Debug.LogWarning("[TsunamiTestHarness] Camera.main을 찾지 못했습니다. 마우스 클릭 테스트가 동작하지 않습니다.");

        if (tsunami == null)
            Debug.LogWarning("[TsunamiTestHarness] TsunamiWaveSequence가 연결되어 있지 않습니다.");
    }

    private void Update()
    {
        HandleTsunamiTest();
        HandleWallTest();
        HandleClearWalls();
    }

    private void HandleTsunamiTest()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (tsunami != null)
            {
                Debug.Log("[TsunamiTestHarness] StartTsunami() 호출");
                tsunami.StartTsunami();
            }
        }
    }

    private void HandleWallTest()
    {
        if (cam == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            float wallTopWorldY = mouseWorld.y + wallTopHeight;

            lastWallBoundaryIndex = surface.AddWall(mouseWorld.x, wallTopWorldY);
            Debug.Log($"[TsunamiTestHarness] AddWall(x={mouseWorld.x:F2}, topY={wallTopWorldY:F2}) boundaryIndex={lastWallBoundaryIndex}");

            if (lastWallVisual != null)
                Destroy(lastWallVisual);

            if (wallVisualPrefab != null)
            {
                lastWallVisual = Instantiate(wallVisualPrefab,
                    new Vector3(mouseWorld.x, mouseWorld.y + wallTopHeight / 2f, 0f),
                    Quaternion.identity);
                lastWallVisual.transform.localScale = new Vector3(0.3f, wallTopHeight, 1f);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (lastWallBoundaryIndex >= 0)
            {
                surface.RemoveWall(lastWallBoundaryIndex);
                Debug.Log($"[TsunamiTestHarness] RemoveWall(boundaryIndex={lastWallBoundaryIndex})");
                lastWallBoundaryIndex = -1;
            }

            if (lastWallVisual != null)
            {
                Destroy(lastWallVisual);
                lastWallVisual = null;
            }
        }
    }

    private void HandleClearWalls()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            surface.ClearAllWalls();
            Debug.Log("[TsunamiTestHarness] ClearAllWalls()");

            if (lastWallVisual != null)
            {
                Destroy(lastWallVisual);
                lastWallVisual = null;
            }
            lastWallBoundaryIndex = -1;
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 420, 120),
            "Tsunami Test Harness\n" +
            "T: 쓰나미 시작\n" +
            "좌클릭: 클릭 위치에 벽 생성 (파도가 막히는지 테스트)\n" +
            "우클릭: 마지막 벽 제거\n" +
            "C: 모든 벽 제거");
    }
}

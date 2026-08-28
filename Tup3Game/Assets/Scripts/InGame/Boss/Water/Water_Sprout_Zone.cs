using UnityEngine;

public class Water_Sprout_Zone : MonoBehaviour
{
    [Header("범위 설정")]
    [SerializeField] private float interval = 1;
    [SerializeField] private float height = 5;
    [SerializeField] private float width = 1;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform[] spawnPoints = new Transform[5];
    [SerializeField] private GameObject Water_Sprout;

    private void Start()
    {
        EnsureSpawnPoints();
    }

    private bool EnsureSpawnPoints()
    {
        if (startPoint == null)
        {
            Debug.LogError("Water_Sprout_Zone: Start Point가 연결되지 않았습니다.", this);
            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
            spawnPoints = new Transform[5];

        for (int k = 0; k < spawnPoints.Length; k++)
        {
            if (spawnPoints[k] == null)
            {
                GameObject point = new GameObject($"SpawnPoint_{k}");
                point.transform.SetParent(transform);
                spawnPoints[k] = point.transform;
            }

            spawnPoints[k].position = startPoint.position + Vector3.right * interval * k;
        }

        return true;
    }


    int[] Get_random_index(int spawnNum)
    {
        spawnNum = Mathf.Clamp(spawnNum, 0, spawnPoints.Length);

        int[] indices = new int[spawnPoints.Length];
        for (int i = 0; i < indices.Length; i++)
            indices[i] = i;

        // Fisher-Yates shuffle
        for (int i = indices.Length - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            (indices[i], indices[rand]) = (indices[rand], indices[i]);
        }

        int[] result = new int[spawnNum];
        System.Array.Copy(indices, result, spawnNum);
        return result;
    }

    public void SpawnWaterBullets(int spawnNum)
    {
        if (!EnsureSpawnPoints())
            return;

        if (Water_Sprout == null)
        {
            Debug.LogError("Water_Sprout_Zone: Water Sprout 프리팹이 연결되지 않았습니다.", this);
            return;
        }

        int[] selected = Get_random_index(spawnNum);
        foreach (int idx in selected)
        {
            GameObject obj = Instantiate(Water_Sprout, spawnPoints[idx].position, Quaternion.identity);
            Water_Sprout sprout = obj.GetComponent<Water_Sprout>();
            if (sprout == null)
            {
                Debug.LogError("Water_Sprout_Zone: 생성된 프리팹에 Water_Sprout 컴포넌트가 없습니다.", obj);
                Destroy(obj);
                continue;
            }

            RisingWaterPhase.LiftAboveWater(obj);
            sprout.SetTargetWidth(width);
            sprout.SetTargetLength(height);
            sprout.Launch(Vector2.up);
        }
    }

    private void OnValidate()
    {
        interval = Mathf.Max(0.01f, interval);
        height = Mathf.Max(0.01f, height);
        width = Mathf.Max(0.01f, width);
    }
}

using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class Water_Sprout_Zone : MonoBehaviour
{
    [Header("범위 설정")]
    [SerializeField] private Transform startPoint; // 왼쪽 끝
    [SerializeField] private Transform endPoint;
    [SerializeField] private float height = 5;
    [SerializeField] private Transform[] spawnPoints = new Transform[5];
    [SerializeField] private GameObject Water_Sprout;
    [SerializeField] private int SpawnNum = 3;
    
    private float segmentWidth;

    void Start()
    {
        Set_spawnpoint();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            SpawnWaterBullets(SpawnNum);
        }
    }

    void Set_spawnpoint()
    {
        float leftEdge = startPoint.position.x;
        float rightEdge = endPoint.position.x;
        float totalWidth = rightEdge - leftEdge;

        segmentWidth = totalWidth / spawnPoints.Length;
        float fixedY = startPoint.position.y;
        float fixedZ = startPoint.position.z;

        for (int k = 0; k < spawnPoints.Length; k++)
        {
            GameObject point = new GameObject($"SpawnPoint_{k}");
            point.transform.parent = transform;
            float x = leftEdge + segmentWidth * (k + 0.5f);
            point.transform.position = new Vector3(x, fixedY, fixedZ);

            spawnPoints[k] = point.transform;
        }
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
        int[] selected = Get_random_index(spawnNum);
        foreach (int idx in selected)
        {
            GameObject obj = Instantiate(Water_Sprout, spawnPoints[idx].position, Quaternion.identity);
            Water_Sprout sprout = obj.GetComponent<Water_Sprout>();
            sprout.SetTargetWidth(segmentWidth);
            sprout.SetTargetLength(height);
            sprout.Launch(Vector2.up);
        }
    }
}

using UnityEngine;

public class IceBulletSpawnZone : MonoBehaviour
{
    private enum FireDirection { Right, Left }

    [SerializeField] private Transform startPoint;
    [SerializeField] private float interval = 2f;
    [SerializeField] private Transform[] spawnPoints = new Transform[5];
    [SerializeField] private GameObject IceBullet;
    [SerializeField] private FireDirection fireDirection = FireDirection.Right;

    private void Start()
    {
        EnsureSpawnPoints();
    }

    private bool EnsureSpawnPoints()
    {
        if (startPoint == null)
        {
            Debug.LogError("IceBulletSpawnZone: Start Point가 연결되지 않았습니다.", this);
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

            spawnPoints[k].position = startPoint.position + Vector3.down * interval * k;
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

    public void SpawnIceBullets(int spawnNum)
    {
        float prefabTelegraphTime = 0f;
        if (IceBullet != null && IceBullet.TryGetComponent(out Ice_Bullet bullet))
            prefabTelegraphTime = bullet.TelegraphDuration;

        SpawnIceBullets(spawnNum, prefabTelegraphTime);
    }

    public float GetPatternDuration(float telegraphDuration)
    {
        if (IceBullet != null && IceBullet.TryGetComponent(out Ice_Bullet bullet))
            return bullet.GetTotalLifetime(telegraphDuration);

        return Mathf.Max(0.1f, telegraphDuration);
    }

    public void SpawnIceBullets(int spawnNum, float telegraphDuration)
    {
        if (!EnsureSpawnPoints())
            return;

        if (IceBullet == null)
        {
            Debug.LogError("IceBulletSpawnZone: Ice Bullet 프리팹이 연결되지 않았습니다.", this);
            return;
        }

        Vector2 dir = fireDirection == FireDirection.Right ? Vector2.right : Vector2.left;

        int[] selected = Get_random_index(spawnNum);
        foreach (int idx in selected)
        {
            GameObject obj = Instantiate(IceBullet, spawnPoints[idx].position, Quaternion.identity);
            Ice_Bullet bullet = obj.GetComponent<Ice_Bullet>();
            if (bullet == null)
            {
                Debug.LogError("IceBulletSpawnZone: 생성된 프리팹에 Ice_Bullet 컴포넌트가 없습니다.", obj);
                Destroy(obj);
                continue;
            }

            bullet.Launch(dir, telegraphDuration);
        }
    }

    private void OnValidate()
    {
        interval = Mathf.Max(0.01f, interval);
    }
}

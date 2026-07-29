using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterSpawnZone : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints = new Transform[5];
    [SerializeField] private GameObject waterAttackPrefab;
    [SerializeField] private float spawnInterval = 0.3f;

    [Header("테스트용")]
    [SerializeField] private int testSpawnCount = 2;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            SpawnAllAtOnce(testSpawnCount);
        }
    }

    public IEnumerator SpawnSequence(int spawnCount)
    {
        var indices = GetShuffledIndices();
        int actualCount = Mathf.Min(spawnCount, indices.Count);

        for (int i = 0; i < actualCount; i++)
        {
            Transform point = spawnPoints[indices[i]];
            SpawnAtPoint(point.position); // 이미 유효한 것만 담겨있으니 null 체크 불필요
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void SpawnAllAtOnce(int spawnCount)
    {
        var indices = GetShuffledIndices();
        int actualCount = Mathf.Min(spawnCount, indices.Count);

        for (int i = 0; i < actualCount; i++)
        {
            Transform point = spawnPoints[indices[i]];
            SpawnAtPoint(point.position);
        }
    }

    private void SpawnAtPoint(Vector3 position)
    {
        var spawned = Instantiate(waterAttackPrefab, position, Quaternion.identity);

        if (spawned.TryGetComponent(out Ice_Bullet iceBullet))
        {
            iceBullet.Launch(Vector2.right);
        }
    }

    private List<int> GetShuffledIndices()
    {
        var indices = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null) // null이 아닌 것만 후보에 넣음
                indices.Add(i);
        }

        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        return indices;
    }
}

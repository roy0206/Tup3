using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterSpawnZone : MonoBehaviour
{
    [SerializeField] private GameObject waterAttackPrefab;
    [SerializeField] private float spawnInterval = 0.3f;

    [Header("테스트용")]
    [SerializeField] private int testSpawnCount = 2;
    private enum SpawnAxis { Vertical, Horizontal }
    [SerializeField] private SpawnAxis spawnAxis = SpawnAxis.Vertical;
    private enum SpawnDirection { Positive, Negative }
    [SerializeField] private SpawnDirection direction = SpawnDirection.Positive;

    [SerializeField] private Transform spawnParent; 
    [SerializeField] private float spacing = 1f;
    [SerializeField] private int pointCount = 5;


    private float spawnTimer = 0f;
    private float spawnPeriod = 1f;

    private void Update()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnPeriod)
        {
            spawnTimer -= spawnPeriod; // 0으로 초기화 대신 나머지를 남겨서 오차 누적 방지
            StartCoroutine(SpawnSequence(testSpawnCount));
        }
    }

    public IEnumerator SpawnSequence(int spawnCount)
    {
        var indices = GetShuffledIndices();
        int actualCount = Mathf.Min(spawnCount, indices.Count);

        for (int i = 0; i < actualCount; i++)
        {
            Vector3 point = GetSpawnPosition(indices[i]);
            SpawnAtPoint(point);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void SpawnAllAtOnce(int spawnCount)
    {
        var indices = GetShuffledIndices();
        int actualCount = Mathf.Min(spawnCount, indices.Count);

        for (int i = 0; i < actualCount; i++)
        {
            Vector3 point = GetSpawnPosition(indices[i]);
            SpawnAtPoint(point);
        }
    }

    private void SpawnAtPoint(Vector3 position)
    {
        var spawned = Instantiate(waterAttackPrefab, position, Quaternion.identity);

        if (spawned.TryGetComponent(out Ice_Bullet iceBullet))
        {
            Vector2 launchDir = GetLaunchDirection();
            iceBullet.Launch(launchDir);
        }
    }

    private List<int> GetShuffledIndices()
    {
        var indices = new List<int>();
        for (int i = 0; i < pointCount; i++)
            indices.Add(i);

        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        return indices;
    }

    private Vector3 GetSpawnPosition(int index)
    {
        float sign = direction == SpawnDirection.Positive ? 1f : -1f;
        float offset = (index - (pointCount - 1) / 2f) * spacing * sign;
        return spawnAxis == SpawnAxis.Vertical
            ? spawnParent.position + new Vector3(offset, 0, 0)
            : spawnParent.position + new Vector3(0, offset, 0);
    }

    private Vector2 GetLaunchDirection()
    {
        switch (spawnAxis)
        {
            case SpawnAxis.Vertical:
                return direction == SpawnDirection.Positive ? Vector2.up : Vector2.down;
            case SpawnAxis.Horizontal:
                return direction == SpawnDirection.Positive ? Vector2.right : Vector2.left;
            default:
                return Vector2.right;
        }
    }
}

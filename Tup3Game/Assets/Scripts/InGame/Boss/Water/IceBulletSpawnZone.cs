using System.IO.Pipes;
using UnityEngine;

public class IceBulletSpawnZone : MonoBehaviour
{
    private enum FireDirection { Right, Left }

    [SerializeField] private Transform startPoint;
    [SerializeField] private float interval = 2f;
    [SerializeField] private Transform[] spawnPoints = new Transform[5];
    [SerializeField] private GameObject IceBullet;
    [SerializeField] private int SpawnNum = 3;
    [SerializeField] private FireDirection fireDirection = FireDirection.Right;

    void Start()
    {
        Set_spawnpoint();
    }


    // Update is called once per frame
    void Update()
    {
    }

    void Set_spawnpoint()
    {
        for (int k = 0; k < spawnPoints.Length; k++)
        {
            GameObject point = new GameObject($"SpawnPoint_{k}");
            point.transform.parent = transform;
            point.transform.position = startPoint.position + Vector3.down * interval * k;
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

    public void SpawnIceBullets(int spawnNum)
    {
        Vector2 dir = fireDirection == FireDirection.Right ? Vector2.right : Vector2.left;

        int[] selected = Get_random_index(spawnNum);
        foreach (int idx in selected)
        {
            GameObject obj = Instantiate(IceBullet, spawnPoints[idx].position, Quaternion.identity);
            Ice_Bullet bullet = obj.GetComponent<Ice_Bullet>();
            bullet.Launch(dir);
        }
    }
}

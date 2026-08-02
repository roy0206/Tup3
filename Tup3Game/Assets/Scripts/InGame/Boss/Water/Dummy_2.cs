using UnityEngine;

public class Dummy_2 : MonoBehaviour
{
    public GameObject icebulletPrefab;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            var bullet = Instantiate(icebulletPrefab, transform.position, Quaternion.identity);
            bullet.GetComponent<WaterPump>().Launch(Vector2.up);
        }
    }
}

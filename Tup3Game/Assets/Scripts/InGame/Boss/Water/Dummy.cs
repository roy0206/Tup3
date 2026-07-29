using UnityEngine;

public class Dummy : MonoBehaviour
{
    public GameObject icebulletPrefab;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            var bullet = Instantiate(icebulletPrefab, transform.position, Quaternion.identity);
            bullet.GetComponent<Ice_Bullet>().Launch(Vector2.right);
        }
    }
}

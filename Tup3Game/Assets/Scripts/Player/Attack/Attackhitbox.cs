using UnityEngine;

public class Attackhitbox : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("적 맞음 : " + other.name);
        }
    }
}

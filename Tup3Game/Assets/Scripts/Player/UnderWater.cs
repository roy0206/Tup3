using UnityEngine;

public class UnderWater : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var pm = other.GetComponent<Playermovement>();
            if (pm != null) pm.SetInWater(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var pm = other.GetComponent<Playermovement>();
            if (pm != null) pm.SetInWater(false);
        }
    }
}

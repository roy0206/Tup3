using UnityEngine;

public class Dummy_Boss : MonoBehaviour
{
    public float knockbackForce = 10f;
    public int damage = 15;
    public float hitCooldown = 1f;   // 같은 플레이어에게 연속으로 안 맞도록

    private float lastHitTime = -999f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time - lastHitTime < hitCooldown) return;

        PlayerKnockBack playerKnockback = other.GetComponent<PlayerKnockBack>();
        if (playerKnockback != null)
        {
            playerKnockback.TakeHit(transform.position, knockbackForce, damage);
            lastHitTime = Time.time;
        }
    }
}

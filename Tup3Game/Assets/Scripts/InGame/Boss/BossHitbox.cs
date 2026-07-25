using System;
using Unity.VisualScripting;
using UnityEngine;
public class BossHitbox : MonoBehaviour
{
    [SerializeField] private float damage;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(damage);
        }
    }

    // 활성화된 동안에만 그려진다. Game 뷰 우상단 Gizmos 토글을 켜면 인게임에서도 보인다
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
        Gizmos.matrix = transform.localToWorldMatrix;

        foreach (var col in GetComponents<Collider2D>())
        {
            if (!col.enabled) continue;

            switch (col)
            {
                case BoxCollider2D box:
                    Gizmos.DrawWireCube(box.offset, box.size);
                    break;
                case CircleCollider2D circle:
                    Gizmos.DrawWireSphere(circle.offset, circle.radius);
                    break;
                case CapsuleCollider2D capsule:
                    Gizmos.DrawWireCube(capsule.offset, capsule.size);
                    break;
            }
        }
    }
}

using System;
using Unity.VisualScripting;
using UnityEngine;
public class Hitbox : MonoBehaviour
{
    [SerializeField] private float damage;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(damage);
        }
    }
    
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

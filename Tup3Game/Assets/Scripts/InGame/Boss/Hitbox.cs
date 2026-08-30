using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [SerializeField] private int damage;
    [SerializeField] private float knockbackForce;

    private void OnTriggerStay2D(Collider2D other)
    {
        // 공격/이펙트용 자식 콜라이더는 Player 레이어를 공유할 수 있다.
        // 부모를 탐색하지 않고 실제 플레이어 본체 콜라이더만 피해 대상으로 인정한다.
        if (!other.CompareTag("Player") ||
            !other.TryGetComponent(out PlayerKnockBack playerKnockBack))
            return;

        playerKnockBack.TakeHit(transform.position, knockbackForce, damage);
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

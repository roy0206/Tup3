using UnityEngine;

public class Attackhitbox : MonoBehaviour
{
    public ComboAttack combo;

    private void Awake()
    {
        if (combo == null)
            combo = GetComponentInParent<ComboAttack>();
        Debug.Log($"[Attackhitbox Awake] 이 오브젝트: {gameObject.name}, 부모: {(transform.parent != null ? transform.parent.name : "없음")}, combo 찾음: {combo != null}");
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            float damage = combo.currentDamage;
            Debug.Log($"{other.name}에게 {damage} 데미지");

            /*var enemyHealth = other.GetComponent<EnemyHealth>();  // 나중에 만들 적 체력 스크립트
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }*/
        }
    }
}

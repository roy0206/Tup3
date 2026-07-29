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
        if (other.TryGetComponent(out BossBase bossBase))
        {
            float damage = combo.currentDamage;

            bossBase.DoDamage(damage);
        }
        else if (other.TryGetComponent(out Water_eye eye))
        {
            float damage = combo.currentDamage;
                
            eye.DoDamage(damage);
        }
    }
}

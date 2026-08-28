using UnityEngine;

public class Attackhitbox : MonoBehaviour
{
    public ComboAttack combo;

    private Skills skills;
    private Collider2D ownCollider;

    private void Awake()
    {
        if (combo == null)
            combo = GetComponentInParent<ComboAttack>();

        skills = combo != null ? combo.GetComponent<Skills>() : GetComponentInParent<Skills>();
        ownCollider = GetComponent<Collider2D>();

        Debug.Log($"[Attackhitbox Awake] 이 오브젝트: {gameObject.name}, 부모: {(transform.parent != null ? transform.parent.name : "없음")}, combo 찾음: {combo != null}");
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out BossBase bossBase))
        {
            float damage = combo.currentDamage;

            bossBase.DoDamage(damage);
            NotifyHit(other);
        }
        else if (other.TryGetComponent(out Water_eye eye))
        {
            float damage = combo.currentDamage;

            eye.DoDamage(damage);
            NotifyHit(other);
        }
    }

    private void NotifyHit(Collider2D target)
    {
        if (skills == null) return;

        skills.PlayAttackHitEffect(target, ownCollider);
    }
}

/* [파일 노트]
 * 플레이어 근접 공격의 실제 명중 지점이다. OnTriggerEnter2D 에서 데미지를 준 직후
 * Skills.PlayAttackHitEffect(맞은 콜라이더, 이 히트박스 콜라이더) 를 부른다.
 * 이펙트를 낼지 말지(F 공격력 버프가 켜져 있는지)와 좌표/정렬 계산은 전부 Skills 쪽에서 판단하므로
 * 여기서는 "맞았다"는 사실과 대상만 넘긴다. 버프가 꺼져 있으면 Skills 가 조용히 무시한다.
 * skills 참조는 ComboAttack 과 같은 오브젝트(플레이어 루트)에서 찾는다 —
 * Skills 가 [RequireComponent(typeof(ComboAttack))] 라 둘은 항상 같은 오브젝트에 붙어 있다.
 */

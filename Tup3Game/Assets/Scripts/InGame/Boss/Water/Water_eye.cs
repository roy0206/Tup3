using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Water_eye : MonoBehaviour
{
    private Animator Eye_animation;
    [Header("피격 (플레이어로부터)")]
    public float maxHp = 200f;
    private float hp;
    public bool IsDead { get; private set; }

    [Header("생존 시간")]
    public float lifeTime = 5f;

    [Header("크기")]
    public float scale = 1f;

    [Header("피격 연출")]
    public SpriteRenderer spriteRenderer;
    public CircleCollider2D Collider;
    public float hitFlashDuration = 0.1f;
    public Color hitFlashColor = Color.red;

    [Header("보스에게 입히는 피해량")]
    public float Damge_to_boss = 10f;
    private BossBase bossRef;
    private bool canReceiveDamage = true;
    private Color originalColor = Color.white;

    void Awake()
    {
        hp = maxHp;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        if (Collider == null)
            Collider = GetComponent<CircleCollider2D>();
        Scale(scale);
        Eye_animation = GetComponent<Animator>();
        if (Eye_animation != null)
            Eye_animation.SetBool("Can_attack_eye", true);
    }

    public void Init(BossBase boss, float time, float newscale, bool damageable = true)
    {
        bossRef = boss;
        lifeTime = time;
        hp = maxHp;
        IsDead = false;
        canReceiveDamage = damageable;
        CancelInvoke();
        CancelInvoke(nameof(DestroySelf));
        Scale(newscale);
        if (Collider != null)
            Collider.enabled = damageable;

        if (Eye_animation != null)
        {
            Eye_animation.enabled = true;
            Eye_animation.SetBool("Can_attack_eye", true);
        }

        Invoke(nameof(ExpireByTime), lifeTime);
    }
    
    public void DoDamage(float amount)
    {
        if (IsDead || !canReceiveDamage || bossRef == null || amount <= 0f) return;

        float realDamage = Mathf.Min(hp, amount);
        
        hp -= realDamage;
        
        bossRef.DoDamage(realDamage);

        FlashOnHit();

        if (hp <= 0f)
        {
            IsDead = true;
            Die();
        }
    }


    private void FlashOnHit()
    {
        if (spriteRenderer == null) return;
        StopAllCoroutines(); // 연타 시 겹쳐 실행 방지
        StartCoroutine(FlashRoutine());
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        if (Eye_animation != null)
            Eye_animation.enabled = false;

        spriteRenderer.color = hitFlashColor;

        yield return new WaitForSeconds(hitFlashDuration);

        spriteRenderer.color = originalColor;

        if (Eye_animation != null)
            Eye_animation.enabled = true;
    }

    public void Scale(float scale)
    {
        transform.localScale = Vector3.one * scale;
    }

    public void ExpireByTime()
    {
        if (IsDead) return;
        IsDead = true;
        Die();
    }

    public void Die()
    {
        CancelInvoke(nameof(ExpireByTime)); // 체력으로 먼저 죽었으면 타임아웃 예약 취소
        canReceiveDamage = false;

        if (Collider != null)
            Collider.enabled = false;

        StopAllCoroutines();
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        if (Eye_animation != null)
        {
            Eye_animation.enabled = true;
            Eye_animation.SetBool("Can_attack_eye", false);
        }

        // Eye_die 애니메이션의 DestroySelf 이벤트가 정상적으로 재생되도록 즉시 삭제하지 않는다.
        // 컨트롤러 연결이 끊긴 경우에도 오브젝트가 남지 않도록 안전 삭제를 예약한다.
        Destroy(gameObject, 1f);
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    private void OnValidate()
    {
        maxHp = Mathf.Max(1f, maxHp);
        lifeTime = Mathf.Max(0f, lifeTime);
        scale = Mathf.Max(0.01f, scale);
        hitFlashDuration = Mathf.Max(0f, hitFlashDuration);
    }
}

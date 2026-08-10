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

    void Awake()
    {
        hp = maxHp;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (Collider == null)
            Collider = GetComponent<CircleCollider2D>();
        Scale(scale);
        Eye_animation = GetComponent<Animator>();
        Eye_animation.SetBool("Can_attack_eye", true);
    }

    public void Init(BossBase boss, float time, float newscale)
    {
        bossRef = boss;
        lifeTime = time;
        CancelInvoke();
        Scale(newscale);
        Invoke(nameof(ExpireByTime), lifeTime);
    }
    
    public void DoDamage(float amount)
    {
        if (IsDead) return;

        float realDamage = Mathf.Min(hp, amount);
        
        hp -= realDamage;
        
        Debug.Log($"<color=orange> Water Eye Hit! {hp}/{maxHp} Left</color>");

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
        Debug.Log("FlashOnHit 호출");
        if (spriteRenderer == null) return;
        StopAllCoroutines(); // 연타 시 겹쳐 실행 방지
        StartCoroutine(FlashRoutine());
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        Eye_animation.enabled = false;

        spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(0.5f);

        spriteRenderer.color = Color.white;

        Eye_animation.enabled = true;
    }

    public void Scale(float scale)
    {
        spriteRenderer.transform.localScale = new Vector3(scale, scale, scale);
        Collider.transform.localScale = new Vector3(scale, scale, scale);
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

        Eye_animation.SetBool("Can_attack_eye", false);

        Debug.Log("Water Eye Destroyed");

        DestroySelf();
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}

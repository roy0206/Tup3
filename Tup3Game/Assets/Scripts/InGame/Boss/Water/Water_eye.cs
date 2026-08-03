using UnityEngine;

public class Water_eye : MonoBehaviour
{
    [Header("피격 (플레이어로부터)")]
    public float maxHp = 200f;
    private float hp;
    public bool IsDead { get; private set; }


    [Header("생존 시간")]
    public float lifeTime = 5f;


    [Header("피격 연출")]
    public SpriteRenderer spriteRenderer;
    public float hitFlashDuration = 0.1f;
    public Color hitFlashColor = Color.white;
    [Header("보스에게 입히는 피해량")]
    public float Damge_to_boss = 10f;

    private BossBase Water;

    void Awake()
    {
        hp = maxHp;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        Water = GetComponent<BossBase>();
    }

    private void Start()
    {
        Invoke(nameof(ExpireByTime), lifeTime);
    }
    public void DoDamage(float amount)
    {
        if (IsDead) return;

        hp -= amount;
        Debug.Log($"<color=orange>Dummy Boss Hit! {hp}/{maxHp} Left</color>");

        Water.DoDamage(amount);

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
        Color original = spriteRenderer.color;
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = original;
    }
    private void ExpireByTime()
    {
        if (IsDead) return;
        IsDead = true;
        Die();
    }

    private void Die()
    {
        CancelInvoke(nameof(ExpireByTime)); // 체력으로 먼저 죽었으면 타임아웃 예약 취소
        Debug.Log("Water Eye Destroyed");
        Destroy(gameObject);
    }
}

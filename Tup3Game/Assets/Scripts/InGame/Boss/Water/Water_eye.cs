using UnityEngine;

public class Water_eye : MonoBehaviour
{
    [Header("피격 (플레이어로부터)")]
    public float maxHp = 200f;
    private float hp;
    public bool IsDead { get; private set; }

    [Header("피격 연출")]
    public SpriteRenderer spriteRenderer;
    public float hitFlashDuration = 0.1f;
    public Color hitFlashColor = Color.white;
    [Header("보스에게 입히는 피해량")]
    public float Damge_to_boss = 10f;

    private BossBase Water_BOSS;

    void Awake()
    {
        hp = maxHp;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        Water_BOSS = GetComponent<BossBase>();
    }

    public void DoDamage(float amount)
    {
        if (IsDead) return;

        float hpBeforeHit = hp;
        hp -= amount;
        
        float actualDamage = Mathf.Min(amount, hpBeforeHit); // 오버킬 방지된 실제 대미지
        Debug.Log($"<color=orange>Dummy Boss Hit! {hp}/{hpBeforeHit} Left</color>");
        Water_BOSS.DoDamage(actualDamage);

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

    private void Die()
    {
        Debug.Log("Dummy Boss Died");
    }
}

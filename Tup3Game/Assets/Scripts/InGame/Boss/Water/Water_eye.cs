using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[RequireComponent(typeof(Animator))]
public class Water_eye : MonoBehaviour
{
    private static readonly int CanAttackEyeHash = Animator.StringToHash("Can_attack_eye");
    private static readonly int EyeEntryStateHash = Animator.StringToHash("Eye_3_entry");

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

    [Header("렌더링 순서")]
    [SerializeField] private int minimumSortingOrder = 5;

    [Header("눈 파괴 시 보스에게 입히는 피해량")]
    [SerializeField, FormerlySerializedAs("Damge_to_boss")]
    private float damageToBoss = 10f;
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
        ApplySortingOrder();
        if (Collider == null)
            Collider = GetComponent<CircleCollider2D>();
        Scale(scale);
        Eye_animation = GetComponent<Animator>();
        if (Eye_animation != null)
        {
            RemoveUnnamedAnimationEvents(Eye_animation);
            Eye_animation.SetBool(CanAttackEyeHash, true);
        }
    }

    private static void RemoveUnnamedAnimationEvents(Animator animator)
    {
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller == null)
            return;

        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip == null)
                continue;

            AnimationEvent[] events = clip.events;
            AnimationEvent[] validEvents = System.Array.FindAll(
                events,
                animationEvent => !string.IsNullOrWhiteSpace(animationEvent.functionName)
            );

            if (validEvents.Length != events.Length)
                clip.events = validEvents;
        }
    }

    public void Init(
        BossBase boss,
        float time,
        float newscale,
        bool damageable = true,
        bool startClosed = false
    )
    {
        bossRef = boss;
        lifeTime = time;
        hp = maxHp;
        IsDead = false;
        canReceiveDamage = damageable;
        CancelInvoke();
        CancelInvoke(nameof(DestroySelf));
        ApplySortingOrder();
        Scale(newscale);
        if (Collider != null)
            Collider.enabled = damageable;

        if (Eye_animation != null)
        {
            Eye_animation.enabled = true;
            if (startClosed)
                HoldClosed();
            else
                OpenEye();
        }

        Invoke(nameof(ExpireByTime), lifeTime);
    }
    
    public void DoDamage(float amount)
    {
        if (IsDead || !canReceiveDamage || bossRef == null || amount <= 0f) return;

        hp -= Mathf.Min(hp, amount);

        FlashOnHit();

        if (hp <= 0f)
        {
            IsDead = true;
            bossRef.DoDamage(damageToBoss);
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

    public void HoldClosed()
    {
        if (Eye_animation == null)
            return;

        Eye_animation.enabled = true;
        Eye_animation.SetBool(CanAttackEyeHash, false);
        Eye_animation.Play(EyeEntryStateHash, 0, 0f);
        Eye_animation.Update(0f);
        Eye_animation.speed = 0f;
    }

    public void OpenEye()
    {
        if (Eye_animation == null)
            return;

        Eye_animation.enabled = true;
        Eye_animation.speed = 1f;
        Eye_animation.SetBool(CanAttackEyeHash, true);
    }

    private void ApplySortingOrder()
    {
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
            renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, minimumSortingOrder);

        foreach (SortingGroup sortingGroup in GetComponentsInChildren<SortingGroup>(true))
            sortingGroup.sortingOrder = Mathf.Max(sortingGroup.sortingOrder, minimumSortingOrder);
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
            Eye_animation.speed = 1f;
            Eye_animation.SetBool(CanAttackEyeHash, false);
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
        damageToBoss = Mathf.Max(0f, damageToBoss);
        minimumSortingOrder = Mathf.Max(1, minimumSortingOrder);
    }
}

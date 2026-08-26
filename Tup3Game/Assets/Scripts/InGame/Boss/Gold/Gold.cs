using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class Gold : BossBase
{
    private readonly List<float> attackRange = new() { 0f, 3f, 100f, 3f, float.MaxValue, 3f };
    private List<float> curTimes;
    private GameObject player;

    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float gravity = -40f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 0.1f;

    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("그로기 지속시간")]
    [SerializeField] private float pattern1GroggyTime = 5f;
    [SerializeField] private float pattern2GroggyTime = 5f;
    [SerializeField] private float pattern3GroggyTime = 5f;
    [SerializeField] private float pattern4GroggyTime = 5f;

    [Header("패턴1 (검기)")]
    [SerializeField] private GameObject pattern1SlashEffect;
    [SerializeField] private float pattern1SlashStart = 0.35f;
    [SerializeField] private float pattern1SlashDuration = 0.35f;
    [SerializeField] private float pattern1Damage = 20f;
    [SerializeField] private float pattern1KnockBackForce = 1f;
    [SerializeField] private float pattern1HitRange = 3f;

    [Header("쳐내기 판정 - 패턴1 (근접)")]
    [SerializeField] private float pattern1ParryStart = 0.1f;
    [SerializeField] private float pattern1ParryEnd = 0.35f;
    [SerializeField] private float pattern1ParryRange = 4f;

    [Header("쳐내기 판정 - 패턴2 (검 함정)")]
    [SerializeField] private float pattern2ParryStart = 0.8f;
    [SerializeField] private float pattern2ParryEnd = 1.4f;
    [SerializeField] private float pattern2ParryRange = 5f;

    [Header("쳐내기 판정 - 패턴3 (날아드는 검)")]
    [SerializeField] private int pattern3ReflectHitCount = 5;

    [Header("패턴4 (발도 참격) - 수치")]
    [Range(40f, 50f)]
    [SerializeField] private float pattern4Damage = 45f;
    [SerializeField] private float pattern4Cooldown = 30f;
    [SerializeField] private float pattern4KnockBackForce = 1.5f;
    [SerializeField] private float pattern4ParryRange = 999f;

    [Header("패턴4 (발도 참격) - 타이밍")]
    [SerializeField] private float pattern4PrepareTime = 1.3f;
    [SerializeField] private float pattern4SlashDelay = 0.2f;
    [SerializeField] private float pattern4ParryGrace = 0.15f;
    [SerializeField] private float pattern4RecoverTime = 1.5f;
    [SerializeField] private float pattern4DarkenTime = 0.5f;

    [Header("패턴4 (발도 참격) - 연출 오브젝트")]
    [SerializeField] private SpriteRenderer pattern4DarkOverlay;
    [SerializeField] private GameObject pattern4FlashEffect;
    [Range(0f, 1f)]
    [SerializeField] private float pattern4OverlayAlpha = 0.85f;

    [Header("패턴4 (발도 참격) - 참격 풀 이펙트")]
    [SerializeField] private string pattern4SlashPoolKey = "Pattern4Slash";
    [SerializeField] private float pattern4SlashLifeTime = 0.8f;

    private BoxCollider2D bodyCollider;
    private ComboAttack playerCombo;
    private Playermovement playerMovement;
    private float verticalVelocity;
    private bool isPatternSetup;
    private bool isCounterAttackReady;
    private bool isCounterAttacking;
    private bool isPattern4Casting;
    private bool isPattern4ParryOpen;
    private bool isPattern4Parried;
    private bool wasPlayerAttacking;
    private bool playerAttackStarted;
    private bool isPattern1EffectShown;
    private bool isFacingRight;
    private float patternElapsed;
    private int reflectedSwordHits;
    private Sequence pattern4Sequence;
    private float groggyTime;

    public float GroggyTime => groggyTime;
    public bool IsGroggy => !IsDead && groggyTime > 0f;
    public bool IsPattern4Casting => isPattern4Casting;
    public bool IsPattern4ParryOpen => isPattern4ParryOpen;

    new void Awake()
    {
        base.Awake();
        behaviorTree = new BehaviorTreeBuilder(gameObject)
            .Selector("Root")
                .Sequence("DeadSequence")
                    .Do("Dead", Dead)
                .End()
                .Do("Groggy", Groggy)

                .Selector("PatternSelector")
            .Do("Counter", CounterAttack)

                    .Sequence("Pattern4")
                        .Do("CanUsePattern4", () => PatternStarter(4))
                        .Do("UsePattern4", Pattern4)
                    .End()
                    .Sequence("Pattern1")
                        .Do("CanUsePattern1", () => PatternStarter(1))
                        .Do("UsePattern1", Pattern1)
                    .End()
                    .Sequence("Pattern2")
                        .Do("CanUsePattern2", () => PatternStarter(2))
                        .Do("UsePattern2", Pattern2)
                    .End()
                    .Sequence("Pattern3")
                        .Do("CanUsePattern3", () => PatternStarter(3))
                        .Do("UsePattern3", Pattern3)
                    .End()
                .End()
                .Do("Move", Move)
                .Do("Idle", Idle)
            .End()
            .Build();

        curTimes = new List<float> { 0f, 0f, 0f, 0f, pattern4Cooldown };
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        transform.localRotation = Quaternion.identity;
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerCombo = player.GetComponent<ComboAttack>();
            if (playerCombo == null) playerCombo = player.GetComponentInChildren<ComboAttack>(true);
            playerMovement = player.GetComponent<Playermovement>();
            if (playerMovement == null) playerMovement = player.GetComponentInChildren<Playermovement>(true);
        }
        bodyCollider = boxColliders.Count > 0 ? boxColliders[0] : GetComponent<BoxCollider2D>();
        ClearPattern4Visual();
        HidePattern1Effect();
    }

    private void Update()
    {
        if (PauseManager.IsPaused || DialogueManager.IsDialogueActive) return;

        for (int i = 0; i < curTimes.Count; i++)
        {
            curTimes[i] -= Time.deltaTime;
        }
        isCounterAttackReady = false;
        groggyTime -= Time.deltaTime;
        UpdatePlayerAttackDetection();
        patternElapsed = isPatternSetup ? patternElapsed + Time.deltaTime : 0f;
        animator.SetBool("IsDead", IsDead);
        animator.SetBool("IsGroggy", !IsDead && GroggyTime >= 0f);
        behaviorTree.Tick();
        ApplyGravity();
    }

    private void OnDisable()
    {
        CancelPattern4();
        HidePattern1Effect();
    }

    private void ShowPattern1Effect()
    {
        isPattern1EffectShown = true;
        if (pattern1SlashEffect == null) return;
        pattern1SlashEffect.SetActive(false);
        pattern1SlashEffect.SetActive(true);
    }

    private void HidePattern1Effect()
    {
        isPattern1EffectShown = false;
        if (pattern1SlashEffect == null) return;
        pattern1SlashEffect.SetActive(false);
    }

    private void UpdatePlayerAttackDetection()
    {
        bool attacking = playerCombo != null && playerCombo.IsLunging;
        playerAttackStarted = attacking && !wasPlayerAttacking;
        wasPlayerAttacking = attacking;
    }

    private bool CheckPlayerParry(float range)
    {
        if (IsDead) return false;
        if (!playerAttackStarted) return false;
        if (player == null || playerMovement == null) return false;

        float distance = HorizontalDistance;
        if (distance > range) return false;
        if (distance <= 0.3f) return true;

        float towardBoss = Mathf.Sign(transform.position.x - player.transform.position.x);
        float facing = playerMovement.GetFacingDirection();
        if (Mathf.Abs(facing) < 0.01f) return false;
        return Mathf.Approximately(Mathf.Sign(facing), towardBoss);
    }

    private bool CheckPatternParry(float range, float windowStart, float windowEnd)
    {
        if (patternElapsed < windowStart || patternElapsed > windowEnd) return false;
        return CheckPlayerParry(range);
    }

    private void Parried(int patternIndex, float groggyDuration)
    {
        if (patternIndex == 4) isPattern4Parried = true;
        CancelPattern4();
        HidePattern1Effect();

        isPatternSetup = false;
        patternElapsed = 0f;
        isCounterAttacking = false;
        if (curTimes != null) curTimes[0] = 0f;
        ResetPatternTriggers();
        groggyTime = groggyDuration;
        Debug.Log($"<color=#00FF88>[금 보스] 패턴{patternIndex} 쳐내기 성공! {groggyDuration}초간 그로기</color>");
    }

    private void ResetPatternTriggers()
    {
        if (animator == null) return;
        animator.ResetTrigger("Pattern1");
        animator.ResetTrigger("Pattern2");
        animator.ResetTrigger("Pattern3");
        animator.ResetTrigger("Pattern4");
        animator.ResetTrigger("CounterAttack");
    }

    public void NotifyReflectedSwordHit()
    {
        if (IsDead) return;

        reflectedSwordHits++;
        Debug.Log($"<color=#FFD700>[금 보스] 되돌아온 검 명중 {reflectedSwordHits}/{pattern3ReflectHitCount}</color>");
        if (reflectedSwordHits < pattern3ReflectHitCount) return;

        reflectedSwordHits = 0;
        Parried(3, pattern3GroggyTime);
    }

    private TaskStatus Dead()
    {
        if (!IsDead) return TaskStatus.Failure;

        CancelPattern4();
        HidePattern1Effect();
        animator.SetBool("IsMoving", false);
        animator.SetBool("IsIdle", false);
        gameObject.layer = LayerMask.GetMask("Default");
        return TaskStatus.Success;
    }


    private TaskStatus Groggy()
    {
        if (IsDead || GroggyTime < 0) return TaskStatus.Failure;
        animator.SetBool("IsMoving", false);
        animator.SetBool("IsIdle", false);
        return TaskStatus.Success;
    }

    private TaskStatus CounterAttack()
    {
        if (IsDead || GroggyTime > 0)
        {
            isPatternSetup = false;
            return TaskStatus.Failure;
        }
        if(!isCounterAttacking) return  TaskStatus.Failure;
        if (!isPatternSetup)
        {
            curTimes[0] = 1f;
            patternElapsed = 0f;
            isPatternSetup = true;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsIdle", false);
            animator.SetTrigger("CounterAttack");
            DOVirtual.DelayedCall(0.2f,
                () => player.GetComponent<PlayerKnockBack>().TakeHit(transform.position, 0.5f, 20));
        }

        isCounterAttackReady = true;
        if (curTimes[0] > 0f) return TaskStatus.Continue;
        isCounterAttacking = false;
        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private TaskStatus PatternStarter(int patternIndex)
    {
        if (IsDead || GroggyTime > 0) return TaskStatus.Failure;
        if (curTimes[patternIndex] > 0f) return TaskStatus.Failure;
        if (HorizontalDistance > attackRange[patternIndex]) return TaskStatus.Failure;
        return TaskStatus.Success;
    }

    private TaskStatus Pattern1()
    {
        if (IsDead || GroggyTime > 0)
        {
            isPatternSetup = false;
            HidePattern1Effect();
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            curTimes[0] = 1f;
            curTimes[1] = 10f;
            patternElapsed = 0f;
            isPatternSetup = true;
            isPattern1EffectShown = false;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsIdle", false);
            animator.SetTrigger("Pattern1");
        }

        if (CheckPatternParry(pattern1ParryRange, pattern1ParryStart, pattern1ParryEnd))
        {
            Parried(1, pattern1GroggyTime);
            return TaskStatus.Failure;
        }

        UpdatePattern1Effect();

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        HidePattern1Effect();
        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private void UpdatePattern1Effect()
    {
        float slashStart = Mathf.Max(0f, pattern1SlashStart);
        float slashEnd = slashStart + Mathf.Max(0f, pattern1SlashDuration);

        if (!isPattern1EffectShown)
        {
            if (patternElapsed >= slashStart && patternElapsed < slashEnd)
            {
                ShowPattern1Effect();
                ApplyPattern1Damage();
            }
            return;
        }

        if (patternElapsed >= slashEnd) HidePattern1Effect();
    }

    private void ApplyPattern1Damage()
    {
        if (player == null) return;
        if (HorizontalDistance > pattern1HitRange) return;

        if (player.TryGetComponent(out PlayerKnockBack knockBack))
        {
            knockBack.TakeHit(transform.position, pattern1KnockBackForce, Mathf.RoundToInt(pattern1Damage));
            return;
        }

        if (player.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(pattern1Damage);
        }
    }

    private TaskStatus Pattern2()
    {
        if (IsDead || GroggyTime > 0)
        {
            isPatternSetup = false;
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            curTimes[0] = 3f;
            curTimes[2] = 10f;
            patternElapsed = 0f;
            isPatternSetup = true;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsIdle", false);
            animator.SetTrigger("Pattern2");

            DOVirtual.DelayedCall(1f, () =>
            {
                if (IsDead || GroggyTime > 0) return;

                PoolManager.Instance.Get(
                    "SwordTrap",
                    new Vector3(transform.position.x + 1, -3f, 0f),
                    Quaternion.identity).transform.rotation = Quaternion.Euler(0f, 180, 0f);


                PoolManager.Instance.Get(
                    "SwordTrap",
                    new Vector3(transform.position.x + -1, -3f, 0f),
                    Quaternion.identity).transform.rotation = Quaternion.Euler(0f, 0, 0f);
            });
        }

        if (CheckPatternParry(pattern2ParryRange, pattern2ParryStart, pattern2ParryEnd))
        {
            Parried(2, pattern2GroggyTime);
            return TaskStatus.Failure;
        }

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private TaskStatus Pattern3()
    {
        if (IsDead || GroggyTime > 0)
        {
            isPatternSetup = false;
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            curTimes[0] = 8f;
            curTimes[3] = 60f;
            patternElapsed = 0f;
            reflectedSwordHits = 0;
            isPatternSetup = true;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsIdle", false);
            animator.SetTrigger("Pattern3");

            DOVirtual.DelayedCall(1f, () =>
            {
                if (IsDead || GroggyTime > 0) return;

                for (int i = 0; i < 5; i++)
                {
                    GameObject flyingSword = PoolManager.Instance.Get(
                        "FlyingSword",
                        transform.position,
                        Quaternion.identity);
                    PoolManager.Instance.Release(flyingSword, 10f);
                }
            });
        }

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private TaskStatus Pattern4()
    {
        if (IsDead || GroggyTime > 0)
        {
            CancelPattern4();
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            float flashTime = Mathf.Max(0f, pattern4PrepareTime);
            float slashTime = flashTime + Mathf.Max(0f, pattern4SlashDelay);
            float endTime = slashTime + Mathf.Max(0f, pattern4RecoverTime);

            curTimes[0] = endTime + 0.1f;
            curTimes[4] = pattern4Cooldown;
            patternElapsed = 0f;
            isPatternSetup = true;
            isPattern4Casting = true;
            isPattern4Parried = false;
            isPattern4ParryOpen = false;

            animator.SetBool("IsMoving", false);
            animator.SetBool("IsIdle", false);
            animator.SetTrigger("Pattern4");
            if (player != null) Face(Mathf.Sign(player.transform.position.x - transform.position.x));

            BuildPattern4Sequence(flashTime, slashTime, endTime);
            Debug.Log("<color=#FFD700>[금 보스] 발도 참격 시전 시작</color>");
        }

        if (isPattern4ParryOpen && CheckPlayerParry(pattern4ParryRange))
        {
            Parried(4, pattern4GroggyTime);
            return TaskStatus.Failure;
        }

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        FinishPattern4();
        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private void BuildPattern4Sequence(float flashTime, float slashTime, float endTime)
    {
        if (pattern4Sequence != null && pattern4Sequence.IsActive()) pattern4Sequence.Kill();
        PreparePattern4Visual();

        float darkenStart = Mathf.Max(0f, flashTime - pattern4DarkenTime);
        float darkenDuration = Mathf.Max(0.01f, flashTime - darkenStart);
        float brightenDuration = Mathf.Max(0.01f, Mathf.Min(pattern4DarkenTime, endTime - slashTime));
        float parryOpenTime = Mathf.Max(0f, flashTime - Mathf.Max(0f, pattern4ParryGrace));

        pattern4Sequence = DOTween.Sequence();
        pattern4Sequence.AppendInterval(endTime);

        if (pattern4DarkOverlay != null)
        {
            pattern4Sequence.Insert(darkenStart, CreateOverlayFade(pattern4OverlayAlpha, darkenDuration));
            pattern4Sequence.Insert(slashTime, CreateOverlayFade(0f, brightenDuration));
        }

        pattern4Sequence.InsertCallback(parryOpenTime, OpenPattern4ParryWindow);
        pattern4Sequence.InsertCallback(flashTime, ShowPattern4Flash);
        pattern4Sequence.InsertCallback(slashTime, ResolvePattern4Slash);
        pattern4Sequence.OnComplete(FinishPattern4);
    }

    private Tween CreateOverlayFade(float targetAlpha, float duration)
    {
        SpriteRenderer overlay = pattern4DarkOverlay;
        return DOTween.To(
            () => overlay.color.a,
            value =>
            {
                Color color = overlay.color;
                color.a = value;
                overlay.color = color;
            },
            targetAlpha,
            duration).SetEase(Ease.Linear);
    }

    private void OpenPattern4ParryWindow()
    {
        if (!isPattern4Casting) return;
        isPattern4ParryOpen = true;
        Debug.Log("<color=#00FFFF>[금 보스] 쳐내기 가능 구간 시작</color>");
    }

    private void ShowPattern4Flash()
    {
        if (!isPattern4Casting) return;
        if (pattern4FlashEffect == null) return;
        if (!pattern4FlashEffect.transform.IsChildOf(transform))
        {
            pattern4FlashEffect.transform.position = transform.position;
        }
        pattern4FlashEffect.SetActive(true);
    }

    private void ResolvePattern4Slash()
    {
        if (!isPattern4Casting) return;
        isPattern4ParryOpen = false;
        if (pattern4FlashEffect != null) pattern4FlashEffect.SetActive(false);
        if (isPattern4Parried) return;

        SpawnPattern4Slash();
        Debug.Log("<color=red>[금 보스] 발도 참격 명중 판정</color>");
        ApplyPattern4Damage();
    }

    private void SpawnPattern4Slash()
    {
        if (PoolManager.Instance == null) return;
        if (string.IsNullOrEmpty(pattern4SlashPoolKey)) return;

        GameObject slash = PoolManager.Instance.Get(pattern4SlashPoolKey, Vector3.zero, Quaternion.identity);
        if (slash == null) return;

        PoolManager.Instance.Release(slash, pattern4SlashLifeTime);
    }

    private void ApplyPattern4Damage()
    {
        if (player == null) return;

        if (player.TryGetComponent(out PlayerKnockBack knockBack))
        {
            knockBack.TakeHit(transform.position, pattern4KnockBackForce, Mathf.RoundToInt(pattern4Damage));
            return;
        }

        if (player.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(pattern4Damage);
        }
    }

    private void CancelPattern4()
    {
        isPatternSetup = false;
        if (!isPattern4Casting) return;

        isPattern4Casting = false;
        isPattern4ParryOpen = false;
        if (curTimes != null) curTimes[0] = 0f;
        if (pattern4Sequence != null && pattern4Sequence.IsActive()) pattern4Sequence.Kill();
        pattern4Sequence = null;
        ClearPattern4Visual();
    }

    private void FinishPattern4()
    {
        if (!isPattern4Casting) return;

        isPattern4Casting = false;
        isPattern4ParryOpen = false;
        if (pattern4Sequence != null && pattern4Sequence.IsActive() && !pattern4Sequence.IsComplete())
        {
            pattern4Sequence.Kill();
        }
        pattern4Sequence = null;
        ClearPattern4Visual();
    }

    private void PreparePattern4Visual()
    {
        if (pattern4DarkOverlay != null)
        {
            Color color = pattern4DarkOverlay.color;
            color.a = 0f;
            pattern4DarkOverlay.color = color;
            pattern4DarkOverlay.gameObject.SetActive(true);
        }
        if (pattern4FlashEffect != null) pattern4FlashEffect.SetActive(false);
    }

    private void ClearPattern4Visual()
    {
        if (pattern4DarkOverlay != null)
        {
            Color color = pattern4DarkOverlay.color;
            color.a = 0f;
            pattern4DarkOverlay.color = color;
            pattern4DarkOverlay.gameObject.SetActive(false);
        }
        if (pattern4FlashEffect != null) pattern4FlashEffect.SetActive(false);
    }

    private float HorizontalDistance => Mathf.Abs(player.transform.position.x - transform.position.x);

    private TaskStatus Move()
    {
        if (IsDead || GroggyTime > 0 || isCounterAttacking) return TaskStatus.Failure;
        if (HorizontalDistance <= attackRange[5]) return TaskStatus.Failure;

        isCounterAttackReady = true;
        animator.SetBool("IsMoving", true);
        animator.SetBool("IsIdle", false);
        float direction = Mathf.Sign(player.transform.position.x - transform.position.x);
        Face(direction);
        transform.Translate(Vector3.right * (direction * moveSpeed * Time.deltaTime), Space.World);
        return TaskStatus.Success;
    }

    private TaskStatus Idle()
    {
        if (IsDead || GroggyTime > 0 || isCounterAttacking) return TaskStatus.Failure;
        isCounterAttackReady = true;
        animator.SetBool("IsMoving", false);
        animator.SetBool("IsIdle", true);
        Face(Mathf.Sign(player.transform.position.x - transform.position.x));
        return TaskStatus.Success;
    }

    private void Face(float direction)
    {
        if (Mathf.Approximately(direction, 0f)) return;

        bool facingRight = direction > 0f;
        if (spriteRenderer != null) spriteRenderer.flipX = facingRight;
        if (facingRight == isFacingRight) return;

        isFacingRight = facingRight;
        MirrorChild(pattern1SlashEffect);
        MirrorChild(pattern4FlashEffect);
    }

    private static void MirrorChild(GameObject child)
    {
        if (child == null) return;

        Transform t = child.transform;
        Vector3 pos = t.localPosition;
        pos.x = -pos.x;
        t.localPosition = pos;

        Vector3 scale = t.localScale;
        scale.x = -scale.x;
        t.localScale = scale;
    }

    private void ApplyGravity()
    {
        Bounds bounds = bodyCollider.bounds;
        bool grounded = Physics2D.BoxCast(
            bounds.center,
            bounds.size,
            0f,
            Vector2.down,
            groundCheckDistance,
            groundMask).collider != null;

        if (grounded && verticalVelocity <= 0f)
        {
            verticalVelocity = 0f;
            return;
        }

        verticalVelocity += gravity * Time.deltaTime;
        transform.Translate(Vector3.up * (verticalVelocity * Time.deltaTime), Space.World);
    }

    public override bool DoDamage(float damage)
    {
        if (IsDead) return false;

        if (GroggyTime > 0f)
        {
            return base.DoDamage(damage);
        }

        if (isCounterAttackReady || !isCounterAttacking)
        {
            isCounterAttacking = true;
        }
        return false;
    }
}

/* [파일 노트]
 *
 * ─────────────────────────────────────────────────────────────
 * 그로기 = 유일한 피해 구간
 * ─────────────────────────────────────────────────────────────
 * DoDamage 규칙은 단 하나다. "그로기가 아니면 보스 체력은 절대 깎이지 않는다."
 *   1) 사망            → 아무것도 안 함
 *   2) GroggyTime > 0  → base.DoDamage(damage) 로 체력 감소 (유일한 피해 경로)
 *   3) 그 외           → 체력 변화 없음. 기존 카운터 로직대로 isCounterAttacking 만 세운다
 * 보스에게 DoDamage 를 호출하는 외부 코드는 Attackhitbox 하나뿐이라 이 게이트만으로 전부 막힌다.
 * 되돌아온 검(FlyingSword)의 보스 명중도 DoDamage 가 아니라 NotifyReflectedSwordHit() 로만
 * 통지되므로 체력에는 영향이 없다.
 *
 * ─────────────────────────────────────────────────────────────
 * 그로기 진입 = 쳐내기 성공
 * ─────────────────────────────────────────────────────────────
 * 쳐내기 성립 조건(패턴1/2/4 공통, CheckPlayerParry)
 *   - 그 패턴의 판정 창(시간) 안일 것
 *   - 플레이어가 사정거리(수평 거리) 안일 것
 *   - 그 순간 플레이어가 "공격을 새로 시작"했을 것
 *   - 플레이어가 보스 쪽을 바라보고 있을 것 (거리 0.3 이내면 방향 무시)
 * 보스 콜라이더에 공격이 닿을 필요는 없다. 즉 판정은 접촉이 아니라 "사거리 + 방향 + 타이밍"이다.
 *
 * 플레이어 공격 감지는 전부 읽기 전용이다. ComboAttack.IsLunging(공격 히트박스가 켜져 있는
 * 돌진 구간) 의 상승 엣지를 Update 에서 잡아 "이번 프레임에 공격이 시작됐다"로 쓰고,
 * 방향은 Playermovement.GetFacingDirection() 을 읽는다. 플레이어 코드는 한 줄도 고치지 않았다.
 * 콤보 2타·3타도 각각 IsLunging 이 새로 켜지므로 매 타격이 쳐내기 시도로 인정된다.
 *
 * 패턴별 판정 창 (전부 SerializeField, 패턴 시작 후 경과초 patternElapsed 기준)
 *   패턴1 (지속 1.0s, 근접)  : 0.10 ~ 0.35초, 사거리 4  → 검기(히트박스)가 켜지기 직전까지가 창이다
 *   패턴2 (지속 3.0s, 함정)  : 0.80 ~ 1.40초, 사거리 5  → 검 함정이 솟는 1.0초 시점 전후
 *   패턴4 (발도 참격)        : 섬광 0.15초 전 ~ 참격 순간, 사거리 999(사실상 무제한)
 *                              창 개폐는 연출 시퀀스 콜백(OpenPattern4ParryWindow / ResolvePattern4Slash)이
 *                              담당하므로 연출과 판정이 항상 같은 타이밍이다.
 *   패턴3                    : 시간 창이 아니라 "되돌아온 검 pattern3ReflectHitCount(5)회 명중"
 *
 * 그로기 지속시간은 패턴별로 분리돼 있다(pattern1~4GroggyTime, 전부 기본 5초).
 *
 * 쳐내기 성공 시 Parried() 가 패턴 진행 타이머·연출 시퀀스·애니 트리거·카운터 플래그를 모두
 * 정리하고 groggyTime 을 세팅한 뒤 해당 패턴 태스크는 Failure 를 반환한다. 그 프레임에는 이미
 * GroggyTime > 0 이라 아래 패턴/이동/대기가 전부 Failure 로 빠지고, 다음 프레임부터 루트의
 * Groggy 태스크가 잡는다.
 *
 * ─────────────────────────────────────────────────────────────
 * 패턴1 "검기" (sword_aura)
 * ─────────────────────────────────────────────────────────────
 * pattern1SlashEffect 는 보스의 자식으로 붙이는 검기 오브젝트로, 순수 연출용이다
 * (SpriteRenderer + SpriteSequencePlayer 만 있으면 되고 콜라이더/Hitbox 불필요).
 * 피해 판정은 코드가 처리한다: 검기 ON 순간(= 쳐내기 창이 닫힌 직후) ApplyPattern1Damage() 가
 * 수평거리 pattern1HitRange 이내의 플레이어에게 pattern1Damage 를 1회 넣는다
 * (PlayerKnockBack.TakeHit 경유 → 대시 회피/무적 프레임 존중, 패턴4와 동일 방식).
 *   t = pattern1SlashStart (0.35s)                        : 검기 ON + 데미지 판정 (1회)
 *   t = pattern1SlashStart + pattern1SlashDuration (0.7s) : 검기 OFF
 * 쳐내기 창(0.10~0.35)이 검기 ON 직전에 닫히도록 잡아 두었다. 쳐내기에 성공하면 검기가 켜지기
 * 전에 패턴이 캔슬되므로 "제때 쳐내면 안 맞는다"가 성립한다. 두 값을 조절할 때 이 관계를 깨지 말 것.
 * 쳐내기 성공/그로기/사망/오브젝트 비활성 시 HidePattern1Effect() 로 즉시 끈다.
 * ShowPattern1Effect() 는 SetActive(false) → true 로 토글해 SpriteSequencePlayer 가 항상
 * 1프레임부터 다시 재생되게 한다.
 * 좌우반전은 Fire 보스와 같은 spriteRenderer.flipX 방식이다(Y축 회전이면 자식 체력바 캔버스가
 * 카메라 반대편으로 뒤집혀 안 보인다). flipX 는 자식을 안 뒤집으므로, 방향이 바뀔 때
 * MirrorChild() 가 pattern1SlashEffect / pattern4FlashEffect 의 localPosition.x / localScale.x
 * 부호를 뒤집어 이펙트 방향을 맞춘다.
 * pattern1SlashEffect 가 비어 있어도 타이밍/데미지 로직은 그대로 돌아간다(연출만 생략).
 *
 * ─────────────────────────────────────────────────────────────
 * 패턴4 "발도 참격" 타임라인 (t=0 은 패턴 진입 프레임)
 * ─────────────────────────────────────────────────────────────
 *   t = 0                                 : Pattern4 트리거, 기마자세+발도 준비 모션
 *   t = flashTime - pattern4DarkenTime    : 화면 어두워짐 (알파 0 → pattern4OverlayAlpha)
 *   t = flashTime - pattern4ParryGrace    : 쳐내기 창 열림
 *   t = flashTime (= pattern4PrepareTime) : 보스 위치에서 섬광 ON
 *   t = flashTime + pattern4SlashDelay    : 참격 스폰 + 창 닫힘 + 섬광 OFF + 데미지 판정
 *   t = 참격 + pattern4RecoverTime        : 오버레이 복귀, 패턴 종료
 * 데미지는 PlayerKnockBack.TakeHit 로 넣어 대시 회피/무적 프레임을 존중한다.
 * BT 지속시간 curTimes[0] 은 시퀀스 총 길이 + 0.1초. 시퀀스가 항상 먼저 끝나게 해서
 * BT 타이머가 시퀀스를 먼저 죽여 데미지 콜백이 스킵되는 것을 막는다.
 *
 * 참격 이펙트는 씬 참조가 아니라 풀에서 꺼낸다.
 *   PoolManager.Instance.Get(pattern4SlashPoolKey, Vector3.zero, Quaternion.identity)
 *   → PoolManager.Instance.Release(go, pattern4SlashLifeTime) 로 반납
 * 프리팹(AddressableAssets/Gold/Pattern4Slash.prefab, 주소 "Pattern4Slash", 라벨 Pool)이
 * Screen Space Overlay 캔버스라서 위치·회전·스케일이 아무 의미가 없다. 화면 전체를 자기가 알아서
 * 덮으므로 카메라 위치 계산이나 스케일 보정이 필요 없고, 카메라가 움직여도 흔들리지 않는다.
 * 풀이 준비되지 않았거나 주소가 없으면 Get 이 null 을 돌려주고 연출만 생략된다.
 * 암전 오버레이(pattern4DarkOverlay)와 섬광(pattern4FlashEffect)은 기존대로 씬 참조이며 null 허용.
 * 섬광은 보스 자식이 아닐 때만 보스 위치로 옮긴다.
 *
 * ─────────────────────────────────────────────────────────────
 * 기타
 * ─────────────────────────────────────────────────────────────
 * attackRange : [0]=미사용 [1]=패턴1 [2]=패턴2 [3]=패턴3 [4]=패턴4(무한) [5]=Move 정지 거리
 * curTimes    : [0]=현재 패턴 진행 타이머 [1~4]=각 패턴 쿨타임.
 *               curTimes[4] 는 Awake 에서 pattern4Cooldown 으로 초기화 → 전투 시작 30초 뒤 첫 발동.
 * 패턴2/3 의 소환 DelayedCall 은 실행 시점에 사망·그로기면 소환을 건너뛴다(쳐내기로 캔슬된 뒤
 * 뒤늦게 함정/검이 튀어나오는 것을 막기 위함).
 * 애니메이터 요구 파라미터는 기존과 동일(IsDead/IsGroggy/IsMoving/IsIdle, 트리거 Pattern1~4,
 * CounterAttack). 쳐내기 캔슬 시 모든 패턴 트리거를 ResetTrigger 하고 IsGroggy 로 넘어간다.
 *
 * ─────────────────────────────────────────────────────────────
 * 일시정지 대응
 * ─────────────────────────────────────────────────────────────
 * Update 첫 줄의 PauseManager.IsPaused 게이트로 BT/쿨타임/그로기 타이머/중력이 전부 멈춘다.
 * 패턴4 연출 시퀀스와 패턴2/3 의 소환 DelayedCall 은 PauseManager 의 DOTween.PauseAll 로 함께 멈추고,
 * 카운터·패턴 데미지는 PlayerKnockBack.TakeHit 쪽 게이트가 차단한다.
 */

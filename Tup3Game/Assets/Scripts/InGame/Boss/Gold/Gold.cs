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

    [Header("패턴 지속시간 / 이벤트 시각")]
    [SerializeField] private float pattern1Duration = 1f;
    [SerializeField] private float pattern2Duration = 3f;
    [SerializeField] private float pattern2TrapDelay = 1f;
    [SerializeField] private float pattern3Duration = 8f;
    [SerializeField] private float pattern3SummonDelay = 1f;
    [SerializeField] private float counterDuration = 1f;
    [SerializeField] private float counterHitDelay = 0.2f;
    [SerializeField] private float counterDamage = 20f;
    [SerializeField] private float counterKnockBackForce = 0.5f;
    [SerializeField] private float counterHitRange = 3f;

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

    [Header("근접 공격 - 위협 히트박스")]
    [SerializeField] private bool showMeleeThreatHitbox = true;
    [SerializeField] private Color meleeThreatColor = new Color(1f, 0.08f, 0.02f, 0.4f);
    [Range(0f, 1f)]
    [SerializeField] private float meleeThreatFillAlpha = 0.7f;
    [SerializeField] private float meleeThreatHeight;
    [SerializeField] private Vector2 meleeThreatOffset;
    [SerializeField] private float meleeThreatInset = 0.12f;
    [SerializeField] private float meleeThreatPulseSpeed = 20f;
    [SerializeField] private int meleeThreatSortingOrderOffset = -1;

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

    [Header("사운드")]
    [SerializeField] private string groggyHitSoundName = GroggyHitSound;
    [SerializeField] private string blockedHitSoundName = BlockedHitSound;
    [SerializeField] private float roarSoundVolume = 1f;
    [SerializeField] private float swingSoundVolume = 1f;
    [SerializeField] private float parrySoundVolume = 1f;
    [SerializeField] private float blockedHitVolume = 0.8f;

    private const string RoarSound = "Gold_Roar";
    private const string SwingMeleeSound = "Gold_SwingMelee";
    private const string SwingSwordSound = "Gold_SwingSword";
    private const string ScreenSlashSound = "Gold_ScreenSlash";
    private const string DrawSound = "Gold_Draw";
    private const string LightHitSound = "Gold_HitLight";
    private const string GroggyHitSound = "Gold_HitHeavy";
    private const string BlockedHitSound = "Block_Blunt";
    private const string ParrySuccessSound = "Parry_Success";

    private const string AnimSpeedParameter = "AnimSpeed";
    private const float Cut1ClipLength = 1.6666667f;
    private const float Cut1StrikeKeyTime = 0.16666667f;
    private const float Pattern2ClipLength = 3f;
    private const float Pattern2PlantKeyTime = 0.5f;
    private const float Pattern3ClipLength = 5.0833335f;

    private bool hasPlayedIntroRoar;

    protected override string DefaultHitSoundName => LightHitSound;

    protected override string CurrentHitSoundName =>
        IsGroggy ? groggyHitSoundName : base.CurrentHitSoundName;

    [Header("반격 가능 표시 (aura)")]
    [SerializeField] private Transform counterAura;
    [SerializeField] private string counterAuraObjectName = "aura";
    [SerializeField] private Vector2 counterAuraScaleRange = new Vector2(1f, 1.1f);
    [SerializeField] private Vector2 counterAuraPulseDuration = new Vector2(0.25f, 0.6f);

    private Tween counterAuraTween;
    private bool counterAuraShown;
    private SpriteRenderer counterAuraRenderer;
    private float counterAuraBaseOffsetX;

    private BoxCollider2D bodyCollider;
    private ComboAttack playerCombo;
    private Playermovement playerMovement;
    private float verticalVelocity;
    private bool isPatternSetup;
    private bool isCounterAttackReady;
    private bool isCounterAttacking;
    private bool isCounterSetup;
    private bool isPattern4Casting;
    private bool isPattern4ParryOpen;
    private bool isPattern4Parried;
    private bool wasPlayerAttacking;
    private bool playerAttackStarted;
    private bool warnedMissingPlayerRefs;
    private bool isPattern1EffectShown;
    private bool isFacingRight;
    private float patternElapsed;
    private int reflectedSwordHits;
    private Sequence pattern4Sequence;
    private float groggyTime;
    private string pendingAnimTrigger;
    private float pendingAnimTriggerTime;
    private float pendingAnimSpeed = 1f;
    private bool hasAnimSpeedParameter;
    private ThreatHitboxVisual meleeThreatVisual;
    private float meleeThreatDuration;
    private SwordTrap pendingSwordTrap;

    public float GroggyTime => groggyTime;
    public bool IsGroggy => !IsDead && groggyTime > 0f;
    public bool IsPattern4Casting => isPattern4Casting;
    public bool IsPattern4ParryOpen => isPattern4ParryOpen;

    [Header("사망 페이드")]
    [SerializeField] private bool fadeOnDeath = true;
    [SerializeField] private float deathFadeDelay = 0.6f;
    [SerializeField] private float deathFadeDuration = 1.2f;
    [SerializeField] private Ease deathFadeEase = Ease.InQuad;

    private Sequence deathFadeSequence;
    private bool deathFadeStarted;

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

        curTimes = new List<float> { 0f, 0f, 10f, 15f, pattern4Cooldown }; //금속성 3스킬 첫 발동시점을 10초로 했습니다!
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        hasAnimSpeedParameter = HasAnimatorParameter(AnimSpeedParameter);
        InitCounterAura();
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
        InitMeleeThreatVisual();
        ClearPattern4Visual();
        HidePattern1Effect();
        HideMeleeThreatHitbox();

        OnDeath += PlayDeathFade;
    }

    private void OnDestroy()
    {
        OnDeath -= PlayDeathFade;

        if (deathFadeSequence == null) return;
        Sequence seq = deathFadeSequence;
        deathFadeSequence = null;
        seq.Kill();
    }

    // 사망 페이드 — Soil.PlayDeathFade 와 동일한 방식 (구동은 BT Dead() 가 아니라 OnDeath 구독:
    // 승리 대사가 시작되면 Update 의 Tick 이 멈추므로 체력 0 시점에 정확히 1회 오는 OnDeath 를 쓴다).
    // deathFadeDelay 동안 사망 애니메이션(IsDead)이 보인 뒤 자식 SpriteRenderer 전부의 알파를 0 으로 내린다.
    private void PlayDeathFade()
    {
        if (!fadeOnDeath || deathFadeStarted) return;
        deathFadeStarted = true;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0) return;

        float delay = Mathf.Max(0f, deathFadeDelay);
        float duration = Mathf.Max(0.01f, deathFadeDuration);

        deathFadeSequence = DOTween.Sequence().SetTarget(this);
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;
            deathFadeSequence.Insert(delay, sr.DOFade(0f, duration).SetEase(deathFadeEase));
        }

        deathFadeSequence.OnComplete(() => deathFadeSequence = null);
    }

    private void Update()
    {
        if (PauseManager.IsPaused || DialogueManager.IsDialogueActive) return;

        if (!hasPlayedIntroRoar && !IsDead)
        {
            hasPlayedIntroRoar = true;
            BossSound.Play(RoarSound, roarSoundVolume);
        }

        for (int i = 0; i < curTimes.Count; i++)
        {
            curTimes[i] -= Time.deltaTime;
        }
        isCounterAttackReady = false;
        groggyTime -= Time.deltaTime;
        UpdateCounterAura();
        UpdatePlayerAttackDetection();
        patternElapsed = isPatternSetup ? patternElapsed + Time.deltaTime : 0f;
        animator.SetBool("IsDead", IsDead);
        animator.SetBool("IsGroggy", !IsDead && GroggyTime >= 0f);
        behaviorTree.Tick();
        FlushPendingAnimation();
        ApplyGravity();
    }

    private void OnDisable()
    {
        CancelPattern4();
        CancelPattern2Telegraph();
        isCounterSetup = false;
        HidePattern1Effect();
        HideMeleeThreatHitbox();
        SetCounterAuraShown(false);
        pendingAnimTrigger = null;
    }

    private bool HasAnimatorParameter(string parameterName)
    {
        if (animator == null) return false;
        if (animator.runtimeAnimatorController == null) return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName) return true;
        }

        Debug.LogWarning(
            $"[금 보스] 애니메이터에 '{parameterName}' 파라미터가 없어 배속 보정을 건너뜁니다. " +
            "메뉴 Tools/Tup3/금 보스 애니메이터 동기화 설정 을 한 번 실행하세요.", this);
        return false;
    }

    private void QueueAttackAnimation(
        string trigger,
        float clipLength,
        float clipKeyTime,
        float eventTime,
        float patternDuration)
    {
        float duration = Mathf.Max(0.01f, patternDuration);
        float keyTime = Mathf.Clamp(clipKeyTime, 0f, clipLength);
        float eventAt = Mathf.Clamp(eventTime, 0f, duration);
        float tail = duration - eventAt;

        float speed;
        float triggerTime;

        if (tail > 0.01f && clipLength - keyTime > 0.01f)
        {
            speed = Mathf.Clamp((clipLength - keyTime) / tail, 0.05f, 20f);
            triggerTime = eventAt - keyTime / speed;
        }
        else
        {
            speed = Mathf.Clamp(clipLength / duration, 0.05f, 20f);
            triggerTime = 0f;
        }

        pendingAnimTrigger = trigger;
        pendingAnimSpeed = speed;
        pendingAnimTriggerTime = Mathf.Clamp(triggerTime, 0f, duration);
    }

    private void FlushPendingAnimation()
    {
        if (pendingAnimTrigger == null) return;

        if (!isPatternSetup || IsDead || GroggyTime > 0f)
        {
            pendingAnimTrigger = null;
            return;
        }

        if (patternElapsed < pendingAnimTriggerTime) return;

        if (hasAnimSpeedParameter) animator.SetFloat(AnimSpeedParameter, pendingAnimSpeed);
        animator.SetTrigger(pendingAnimTrigger);
        pendingAnimTrigger = null;
    }

    private void InitCounterAura()
    {
        ResolveCounterAura();
        if (counterAura == null) return;

        counterAuraRenderer = counterAura.GetComponent<SpriteRenderer>();
        counterAuraBaseOffsetX = Mathf.Abs(counterAura.localPosition.x);
        MirrorCounterAura();

        counterAuraShown = false;
        counterAuraTween?.Kill();
        counterAuraTween = null;
        counterAura.localScale = Vector3.one * counterAuraScaleRange.x;
        counterAura.gameObject.SetActive(false);
    }

    private void ResolveCounterAura()
    {
        if (counterAura != null) return;
        if (string.IsNullOrWhiteSpace(counterAuraObjectName)) return;

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child == transform) continue;
            if (child.name != counterAuraObjectName) continue;

            counterAura = child;
            return;
        }
    }

    private void UpdateCounterAura()
    {
        SetCounterAuraShown(!IsDead && !IsGroggy);
    }

    private void SetCounterAuraShown(bool show)
    {
        if (counterAura == null) return;
        if (counterAuraShown == show) return;

        counterAuraShown = show;

        counterAuraTween?.Kill();
        counterAuraTween = null;
        counterAura.localScale = Vector3.one * counterAuraScaleRange.x;
        counterAura.gameObject.SetActive(show);

        if (show) PlayCounterAuraPulse();
    }

    private void PlayCounterAuraPulse()
    {
        if (counterAura == null) return;

        float target = UnityEngine.Random.Range(counterAuraScaleRange.x, counterAuraScaleRange.y);
        float duration = Mathf.Max(0.01f, UnityEngine.Random.Range(counterAuraPulseDuration.x, counterAuraPulseDuration.y));

        counterAuraTween = counterAura
            .DOScale(Vector3.one * target, duration)
            .SetEase(Ease.InOutSine)
            .SetTarget(this)
            .OnComplete(PlayCounterAuraPulse);
    }

    private void ShowPattern1Effect()
    {
        isPattern1EffectShown = true;
        BossSound.Play(SwingMeleeSound, swingSoundVolume);
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

    private void InitMeleeThreatVisual()
    {
        if (!showMeleeThreatHitbox) return;

        meleeThreatVisual = GetComponent<ThreatHitboxVisual>();
        if (meleeThreatVisual == null) meleeThreatVisual = gameObject.AddComponent<ThreatHitboxVisual>();
        meleeThreatVisual.Configure(
            spriteRenderer,
            meleeThreatColor,
            meleeThreatFillAlpha,
            meleeThreatInset,
            meleeThreatPulseSpeed,
            meleeThreatSortingOrderOffset);
    }

    private void ShowMeleeThreatHitbox(float range, float duration)
    {
        if (!showMeleeThreatHitbox) return;

        InitMeleeThreatVisual();
        if (meleeThreatVisual == null) return;

        float scaleX = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        float scaleY = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
        float height = meleeThreatHeight > 0f
            ? meleeThreatHeight
            : bodyCollider != null ? bodyCollider.bounds.size.y : 1f;
        float centerY = bodyCollider != null ? bodyCollider.bounds.center.y : transform.position.y;
        Vector3 worldCenter = new Vector3(
            transform.position.x + meleeThreatOffset.x,
            centerY + meleeThreatOffset.y,
            transform.position.z);

        meleeThreatDuration = Mathf.Max(0f, duration);
        meleeThreatVisual.ShowLocalBox(
            new Vector2(
                Mathf.Max(0.01f, range * 2f) / scaleX,
                Mathf.Max(0.01f, height) / scaleY),
            transform.InverseTransformPoint(worldCenter),
            ThreatFillDirection.CenterOutHorizontal);
        UpdateMeleeThreatVisual();
    }

    private void UpdateMeleeThreatVisual()
    {
        if (meleeThreatVisual == null || !meleeThreatVisual.IsVisible) return;
        if (meleeThreatDuration <= 0f || patternElapsed >= meleeThreatDuration)
        {
            HideMeleeThreatHitbox();
            return;
        }

        meleeThreatVisual.SetProgress(
            Mathf.Clamp01(patternElapsed / meleeThreatDuration),
            patternElapsed);
    }

    private void HideMeleeThreatHitbox()
    {
        if (meleeThreatVisual != null) meleeThreatVisual.Hide();
    }

    private void UpdatePlayerAttackDetection()
    {
        EnsurePlayerRefs();

        bool attacking = playerCombo != null && playerCombo.IsLunging;
        playerAttackStarted = attacking && !wasPlayerAttacking;
        wasPlayerAttacking = attacking;
    }

    private void EnsurePlayerRefs()
    {
        if (playerCombo != null && playerMovement != null) return;

        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (playerCombo == null)
        {
            playerCombo = player.GetComponent<ComboAttack>();
            if (playerCombo == null) playerCombo = player.GetComponentInChildren<ComboAttack>(true);
        }

        if (playerMovement == null)
        {
            playerMovement = player.GetComponent<Playermovement>();
            if (playerMovement == null) playerMovement = player.GetComponentInChildren<Playermovement>(true);
        }

        if (warnedMissingPlayerRefs) return;
        if (playerCombo != null && playerMovement != null) return;

        warnedMissingPlayerRefs = true;
        Debug.LogError(
            $"[금 보스] 플레이어에서 ComboAttack/Playermovement 를 찾지 못했습니다 " +
            $"(ComboAttack={playerCombo != null}, Playermovement={playerMovement != null}). 쳐내기 판정이 동작하지 않습니다.", this);
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
        CancelPattern2Telegraph();
        HidePattern1Effect();
        HideMeleeThreatHitbox();

        isPatternSetup = false;
        patternElapsed = 0f;
        isCounterAttacking = false;
        isCounterSetup = false;
        if (curTimes != null) curTimes[0] = 0f;
        ResetPatternTriggers();
        groggyTime = groggyDuration;
        BossSound.Play(ParrySuccessSound, parrySoundVolume);
        Debug.Log($"<color=#00FF88>[금 보스] 패턴{patternIndex} 쳐내기 성공! {groggyDuration}초간 그로기</color>");
    }

    private void ResetPatternTriggers()
    {
        pendingAnimTrigger = null;
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
        CancelPattern2Telegraph();
        HidePattern1Effect();
        HideMeleeThreatHitbox();
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
            isCounterSetup = false;
            HideMeleeThreatHitbox();
            return TaskStatus.Failure;
        }
        if(!isCounterAttacking) return  TaskStatus.Failure;
        if (!isCounterSetup)
        {
            CancelPattern4();
            CancelPattern2Telegraph();
            HidePattern1Effect();
            HideMeleeThreatHitbox();
            curTimes[0] = counterDuration;
            patternElapsed = 0f;
            isPatternSetup = true;
            isCounterSetup = true;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsIdle", false);
            ShowMeleeThreatHitbox(counterHitRange, counterHitDelay);
            QueueAttackAnimation(
                "CounterAttack", Cut1ClipLength, Cut1StrikeKeyTime, counterHitDelay, counterDuration);

            DOVirtual.DelayedCall(counterHitDelay, () =>
            {
                if (IsDead || GroggyTime > 0f) return;

                BossSound.Play(SwingMeleeSound, swingSoundVolume);
                if (player == null || HorizontalDistance > counterHitRange) return;
                DamagePlayer(counterDamage, counterKnockBackForce);
            });
        }

        UpdateMeleeThreatVisual();
        isCounterAttackReady = true;
        if (curTimes[0] > 0f) return TaskStatus.Continue;
        HideMeleeThreatHitbox();
        isCounterAttacking = false;
        isCounterSetup = false;
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
            HideMeleeThreatHitbox();
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            curTimes[0] = pattern1Duration;
            curTimes[1] = 2f;
            patternElapsed = 0f;
            isPatternSetup = true;
            isPattern1EffectShown = false;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsIdle", false);
            ShowMeleeThreatHitbox(pattern1HitRange, pattern1SlashStart);
            QueueAttackAnimation(
                "Pattern1", Cut1ClipLength, Cut1StrikeKeyTime,
                Mathf.Max(0f, pattern1SlashStart), pattern1Duration);
        }

        if (CheckPatternParry(pattern1ParryRange, pattern1ParryStart, pattern1ParryEnd))
        {
            Parried(1, pattern1GroggyTime);
            return TaskStatus.Failure;
        }

        UpdatePattern1Effect();

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        HidePattern1Effect();
        HideMeleeThreatHitbox();
        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private void UpdatePattern1Effect()
    {
        UpdateMeleeThreatVisual();

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

        DamagePlayer(pattern1Damage, pattern1KnockBackForce);
    }

    private void DamagePlayer(float damage, float knockBackForce)
    {
        if (player == null) return;

        PlayerKnockBack knockBack = player.GetComponent<PlayerKnockBack>();
        if (knockBack == null) knockBack = player.GetComponentInChildren<PlayerKnockBack>(true);
        if (knockBack == null) knockBack = player.GetComponentInParent<PlayerKnockBack>();

        if (knockBack == null)
        {
            Debug.LogError($"[금 보스] '{player.name}' 에서 PlayerKnockBack 을 찾지 못했습니다. 넉백·무적 점멸이 적용되지 않아 피해를 건너뜁니다.", this);
            return;
        }

        knockBack.TakeHit(transform.position, knockBackForce, Mathf.RoundToInt(damage));
    }

    private TaskStatus Pattern2()
    {
        if (IsDead || GroggyTime > 0)
        {
            isPatternSetup = false;
            CancelPattern2Telegraph();
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            HideMeleeThreatHitbox();
            curTimes[0] = pattern2Duration;
            curTimes[2] = 10f;
            patternElapsed = 0f;
            isPatternSetup = true;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsIdle", false);
            QueueAttackAnimation(
                "Pattern2", Pattern2ClipLength, Pattern2PlantKeyTime, pattern2TrapDelay, pattern2Duration);

            GameObject trapObject = PoolManager.Instance.Get(
                "SwordTrap",
                new Vector3(transform.position.x, -1.8f, 0f),
                Quaternion.Euler(0f, 180f, 0f));
            SwordTrap spawnedTrap = trapObject != null ? trapObject.GetComponent<SwordTrap>() : null;
            if (spawnedTrap == null)
            {
                Debug.LogError("[금 보스] SwordTrap 풀 오브젝트에 SwordTrap 컴포넌트가 없습니다.", trapObject);
            }
            else
            {
                pendingSwordTrap = spawnedTrap;
                spawnedTrap.Arm(
                    pattern2TrapDelay,
                    () =>
                    {
                        if (pendingSwordTrap == spawnedTrap) pendingSwordTrap = null;
                        BossSound.Play(SwingMeleeSound, swingSoundVolume);
                    },
                    () => !IsDead && GroggyTime <= 0f);
            }
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

    private void CancelPattern2Telegraph()
    {
        if (pendingSwordTrap == null) return;

        pendingSwordTrap.CancelTelegraph();
        pendingSwordTrap = null;
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
            HideMeleeThreatHitbox();
            curTimes[0] = pattern3Duration;
            curTimes[3] = 60f; 
            patternElapsed = 0f;
            reflectedSwordHits = 0;
            isPatternSetup = true;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsIdle", false);
            QueueAttackAnimation("Pattern3", Pattern3ClipLength, 0f, 0f, pattern3Duration);

            DOVirtual.DelayedCall(pattern3SummonDelay, () =>
            {
                if (IsDead || GroggyTime > 0) return;

                BossSound.Play(SwingSwordSound, swingSoundVolume);

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
            HideMeleeThreatHitbox();
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
            QueueAttackAnimation("Pattern4", Cut1ClipLength, Cut1StrikeKeyTime, slashTime, endTime);
            BossSound.Play(DrawSound, swingSoundVolume);
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

        BossSound.Play(ScreenSlashSound, swingSoundVolume);
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

        DamagePlayer(pattern4Damage, pattern4KnockBackForce);
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
        MirrorCounterAura();
    }

    private void MirrorCounterAura()
    {
        if (counterAura == null) return;

        Vector3 p = counterAura.localPosition;
        p.x = counterAuraBaseOffsetX * (isFacingRight ? 1f : -1f);
        counterAura.localPosition = p;

        if (counterAuraRenderer != null) counterAuraRenderer.flipX = isFacingRight;
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

        BossSound.PlayThrottled(blockedHitSoundName, blockedHitVolume, HitSoundMinInterval);

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
 * 플레이어 피해는 전부 DamagePlayer(damage, knockBackForce) 한 곳을 거친다 —
 * PlayerKnockBack.TakeHit 이 데미지·콤보 취소·넉백·0.5초 무적 점멸을 모두 처리하는 정식 경로다.
 * 예전에는 PlayerKnockBack 을 못 찾으면 PlayerHealth.TakeDamage 로 조용히 떨어졌는데,
 * 그 경로는 넉백과 무적 점멸을 통째로 건너뛰어 버그를 숨겼다. 지금은 루트→자식→부모 순으로
 * 참조를 찾고 그래도 없으면 에러 로그를 남기고 피해를 주지 않는다(조용한 실패 금지).
 * 카운터 반격도 이제 이 경로를 쓴다. 예전에는 player.GetComponent<PlayerKnockBack>().TakeHit 을
 * 직접 불러서 컴포넌트가 없으면 NRE 였고, 쳐내기로 캔슬된 뒤에도 0.2초 뒤 피해가 그대로 들어갔다.
 * 반격 피해는 counterHitRange 안에서만 성립하며, 같은 범위가 counterHitDelay 동안 채워져 원거리의
 * 플레이어에게 보이지 않는 전역 피해가 들어가지 않는다.
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
 * 근접 위협 히트박스는 패턴1 진입(t=0)부터 실제 타격(pattern1SlashStart) 직전까지, 반격 시작부터
 * counterHitDelay 직전까지 표시된다.
 * 별도 Collider2D/Hitbox 없이 런타임 SpriteRenderer 로 만든 순수 연출이라 피해 판정에는 관여하지
 * 않는다. 가로 폭은 해당 공격의 실제 판정 범위 * 2, 세로 높이는 기본적으로 보스의
 * bodyCollider 높이를 그대로 사용한다. 붉은 외곽선 안쪽이 준비 시간의 진행률에 맞춰 중앙에서
 * 양옆으로 차오르고, 가득 찬 순간 실제 공격이 나가므로 남은 시간을 공간적으로 읽을 수 있다.
 * 실제 타격·쳐내기 성공·반격 전환·사망·오브젝트 비활성 시 즉시 숨긴다.
 * 공통 생성/채움 로직은 ThreatHitboxVisual 이 맡는다.
 *
 * 패턴2는 예전처럼 1초 뒤 함정을 갑자기 꺼내지 않는다. 패턴 시작 즉시 SwordTrap 을 풀에서 꺼내
 * 실제 PolygonCollider2D bounds 크기의 위협 박스만 보여 주고, pattern2TrapDelay 동안 중앙에서
 * 채운다. 가득 찬 뒤에만 함정 스프라이트·Hitbox·Collider2D 를 켠다. 활성화 전에 쳐내기/사망하면
 * pendingSwordTrap 을 즉시 풀에 반납하므로 늦게 솟는 함정이 없다.
 * 패턴3의 각 FlyingSword 는 자체 대기 시간 동안 발사 방향 통로를 채워 표시한다.
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
 * 패턴2 함정은 즉시 전조 상태로 소환되고 activationGuard 로 사망·그로기 시 활성화를 막는다.
 * 패턴3 소환 DelayedCall 은 실행 시점에 사망·그로기면 검 소환을 건너뛴다.
 * 애니메이터 요구 파라미터는 기존 + Float AnimSpeed (아래 "애니메이션 동기화" 참고).
 * 쳐내기 캔슬 시 모든 패턴 트리거를 ResetTrigger 하고 IsGroggy 로 넘어간다.
 *
 * ─────────────────────────────────────────────────────────────
 * 애니메이션 동기화 (QueueAttackAnimation / FlushPendingAnimation)
 * ─────────────────────────────────────────────────────────────
 * 금 보스 클립은 코드 타이밍과 길이도, 타격 프레임 위치도 맞지 않는다(실측).
 *   Cut1     1.6667초, 스프라이트 키 0 / 0.1667 / 1.5833
 *            cut_1(검을 뒤로 당긴 준비) → cut_2(앞으로 뻗은 타격) → cut_3(마무리)
 *            즉 "베는 순간" = 0.1667초. 이 클립을 Cut1(패턴1) / Pattern4 / CounterAttack 세 상태가 공유한다.
 *   Pattern2 3.0000초, 키 0 / 0.5 / 2.9167
 *            counter_1(서서 검 세움) → counter_2(무릎 꿇고 검을 땅에 꽂음)
 *            즉 "검을 꽂는 순간" = 0.5초.
 *   Pattern3 5.0833초, 단일 스프라이트(hold_up) — 이벤트 프레임 없음.
 * 클립은 아트 자산이라 고치지 않는다. 대신 코드가 두 가지를 계산해서 맞춘다.
 *   1) 트리거 시각   : 클립의 타격 프레임이 코드의 이벤트 시각에 정확히 오도록 트리거를 늦춘다.
 *   2) 재생 배속     : 클립이 패턴 종료와 정확히 같이 끝나도록 AnimSpeed 파라미터에 배속을 넣는다.
 * 클립 길이 L, 클립 내 타격 키 K, 코드의 이벤트 시각 E, 패턴 지속 D 일 때
 *   배속 s   = (L - K) / (D - E)        ← 타격 이후 잔여 클립이 패턴 잔여 시간을 정확히 채운다
 *   트리거 T = E - K / s                ← 그래야 타격 프레임이 E 에 온다
 * 계산 결과(기본값 기준)
 *   패턴1 : L1.667 K0.167 E0.35 D1.0  → s 2.31, T 0.278  (0~0.278 대기, 0.35 타격, 1.0 종료)
 *   패턴2 : L3.000 K0.500 E1.00 D3.0  → s 1.25, T 0.600  (1.0 에 검 꽂기+함정, 3.0 종료)
 *   패턴3 : L5.083 K0     E0    D8.0  → s 0.635, T 0     (8.0 까지 검을 든 채 유지)
 *   패턴4 : L1.667 K0.167 E1.50 D3.0  → s 1.00, T 1.333  (섬광 1.3 직후 발도, 1.5 참격, 3.0 종료)
 *   반격  : L1.667 K0.167 E0.20 D1.0  → s 1.88, T 0.111  (0.2 에 타격+피해, 1.0 종료)
 * 고치기 전에는 패턴4가 진입 0.167초에 이미 베는 자세를 취하고 실제 참격은 1.5초에 나갔고(1.33초 어긋남),
 * 패턴3은 애니가 5.08초에 끝나 남은 2.9초를 서 있었고, 패턴1/반격은 패턴이 끝난 뒤에도 클립이
 * 0.667초 더 재생돼 벤 자세로 걸어다녔다.
 *
 * 트리거 대기는 코루틴/트윈이 아니라 patternElapsed 로 센다. 그래서 일시정지·쳐내기 캔슬·그로기·사망이
 * 전부 공짜로 처리된다 — FlushPendingAnimation 은 isPatternSetup 이 풀렸거나 그로기/사망이면
 * 예약을 버리고 아무것도 재생하지 않는다. 대기 구간 동안 보스는 GoldIdle(기본 자세)로 서 있다.
 *
 * AnimSpeed 는 Float 파라미터이고 Cut1/Cut2/Pattern3/Pattern4/CounterAttack 상태의 Multiplier 에
 * 연결돼 있어야 한다. 연결은 에디터 메뉴 Tools/Tup3/금 보스 애니메이터 동기화 설정
 * (Assets/Scripts/Editor/GoldAnimatorSyncSetup.cs) 이 담당한다. 파라미터가 없으면 Awake 가 경고를
 * 한 번 남기고 배속 보정만 건너뛴다(트리거 타이밍 보정은 그대로 동작).
 *
 * 남은 한계 — 클립 자체 문제라 코드로는 못 고친다(아트 작업)
 *   - Cut1 의 준비 동작이 전체의 10%(0.167/1.667)뿐이라 배속 보정 후 백스윙이 0.07~0.09초로 짧다.
 *   - Cut1 의 cut_2 가 1.42초를 그대로 유지해 뻗은 자세로 굳어 보인다. 중간 키가 없다.
 *   - Pattern3 는 스프라이트 1장이라 8초 내내 정지 화면이다.
 *   - 패턴4 전용 발도 클립이 없어 Cut1 을 재활용한다. 준비 구간(1.33초)은 GoldIdle 로 때운다.
 *   - Dead 상태에 클립이 없다(m_Motion 비어 있음).
 *
 * ─────────────────────────────────────────────────────────────
 * 일시정지 대응
 * ─────────────────────────────────────────────────────────────
 * Update 첫 줄의 PauseManager.IsPaused 게이트로 BT/쿨타임/그로기 타이머/중력이 전부 멈춘다.
 * 패턴4 연출, 패턴2 함정의 전조/수명 시퀀스, 패턴3 소환 DelayedCall 은
 * PauseManager 의 DOTween.PauseAll 로 함께 멈추고,
 * 카운터·패턴 데미지는 PlayerKnockBack.TakeHit 쪽 게이트가 차단한다.
 *
 * ─────────────────────────────────────────────────────────────
 * 사운드
 * ─────────────────────────────────────────────────────────────
 * 피격음이 세 갈래인 이유는 금보스의 피해 규칙이 세 갈래이기 때문이다(DoDamage 참조).
 *   1) 그로기 중 실제 피해 → Gold_HitHeavy
 *      base.DoDamage 로 넘어가는 유일한 경로다. BossBase 가 CurrentHitSoundName 으로 이름을 묻고,
 *      금보스는 IsGroggy 일 때 groggyHitSoundName 을 돌려준다. 즉 BossBase 의 재생 코드는 그대로 쓰고
 *      "어떤 이름을 쓸지"만 갈아끼운다(재생 지점을 늘리지 않아 이중 재생이 없다).
 *   2) 비그로기 피격(데미지 0, 카운터만 예약) → Block_Blunt
 *      이 경로는 base.DoDamage 를 부르지 않으므로 BossBase 의 피격음이 울리지 않는다.
 *      그래서 그 return false 직전에 직접 재생한다. 간격 제한은 BossBase 의 HitSoundMinInterval 을
 *      공유해 플레이어 3단 콤보로 연타할 때 막힘음이 뭉치지 않게 한다.
 *   3) hitSoundName(기본 Gold_HitLight) : 위 두 갈래에 걸리지 않는 일반 피격용 값이다.
 *      현재 규칙상 금보스에서는 실제로 도달하지 않지만(그로기가 유일한 피해 구간),
 *      최종보스와 같은 기본값을 유지해 두면 규칙이 완화될 때 그대로 동작한다.
 * 공격음
 *   Gold_Roar        : 전투가 실제로 시작되는 첫 프레임(Update 의 일시정지·대사 게이트를 처음 통과할 때) 1회.
 *                      금보스 애니메이터에 포효 상태가 따로 없어 등장 시점에 붙였다.
 *   Gold_SwingMelee  : 패턴1 검기가 켜지는 순간(ShowPattern1Effect = 실제 베는 프레임),
 *                      패턴2 검 함정을 뽑아내는 순간(pattern2TrapDelay),
 *                      카운터 반격의 타격 순간(counterHitDelay). 카운터 사운드는 예전에 패턴 진입
 *                      시점이었는데, 배속 보정 후 실제 베는 프레임이 0.2초라 거기로 옮겼다.
 *   Gold_SwingSword  : 패턴3 어검 5자루를 뿌리는 순간(1초 DelayedCall) 1회 — 검마다가 아니라 투척 1회 기준.
 *   Gold_Draw        : 패턴4 진입(기마자세 발도 준비) — 발도 전조.
 *   Gold_ScreenSlash : 패턴4 참격이 확정되는 순간(ResolvePattern4Slash, 쳐내기에 실패했을 때만).
 *                      쳐내기에 성공하면 isPattern4Parried 로 여기 도달하기 전에 return 하므로 울리지 않는다.
 *   Parry_Success    : Parried() — 패턴1/2/4 쳐내기와 패턴3 어검 5회 반사 모두 이 한 곳을 지난다.
 *   Sword_Clash      : 금보스가 아니라 FlyingSword.Reflect() 에 있다(검과 검이 부딪히는 순간).
 *   Gold_SwingClub   : 유저 보류 — 어디에서도 호출하지 않는다.
 */

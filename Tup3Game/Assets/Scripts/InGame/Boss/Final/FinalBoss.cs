using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class FinalBoss : BossBase
{
    private enum ElementKind
    {
        None,
        Soil,
        Water,
        Fire
    }

    [Header("공통")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float gravity = -40f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private float approachStopRange = 3f;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool spriteFacesRight = false;

    [Header("그로기 (추후 재추가용 골격 — 기본 비활성)")]
    [SerializeField] private bool enableGroggy = false;
    [SerializeField] private float parryGroggyTime = 5f;

    [Header("상시 반격")]
    [SerializeField] private float counterDelay = 0.5f;
    [SerializeField] private float counterDuration = 1f;
    [SerializeField] private float counterHitDelay = 0.2f;
    [SerializeField] private float counterDamage = 20f;
    [SerializeField] private float counterKnockBackForce = 0.5f;
    [SerializeField] private float counterHitRange = 999f;

    [Header("쳐내기 판정 - 반격")]
    [SerializeField] private float counterParryStart = 0f;
    [SerializeField] private float counterParryEnd = 0.18f;
    [SerializeField] private float counterParryRange = 4f;

    [Header("토 파동")]
    [SerializeField] private float soilWaveCooldown = 8f;
    [SerializeField] private float soilWaveRange = 100f;
    [SerializeField] private float soilWaveCastTime = 2f;
    [SerializeField] private float soilWaveSpawnDelay = 0.7f;
    [SerializeField] private string soilWavePoolKey = "SoilWave";
    [SerializeField] private Vector2 soilWaveSpawnOffset = new Vector2(1.5f, 0.5f);

    [Header("수 잠식 (상세 기획 미정 — 기본 비활성 슬롯)")]
    [SerializeField] private bool enableAbsorption = false;
    [SerializeField] private float absorptionCooldown = 20f;
    [SerializeField] private float absorptionRange = 100f;
    [SerializeField] private float absorptionDuration = 3f;

    [Header("어검 (금 패턴3)")]
    [SerializeField] private float flyingSwordCooldown = 60f;
    [SerializeField] private float flyingSwordRange = 100f;
    [SerializeField] private float flyingSwordCastTime = 8f;
    [SerializeField] private float flyingSwordSpawnDelay = 1f;
    [SerializeField] private int flyingSwordCount = 5;
    [SerializeField] private string flyingSwordPoolKey = "FlyingSword";
    [SerializeField] private float flyingSwordLifeTime = 10f;
    [SerializeField] private int swordReflectHitCount = 5;

    [Header("화 돌진")]
    [SerializeField] private float fireRushCooldown = 15f;
    [SerializeField] private float fireRushRange = 100f;
    [SerializeField] private float fireRushWindup = 0.5f;
    [SerializeField] private float fireRushDuration = 0.5f;
    [SerializeField] private float fireRushOvershoot = 3f;
    [SerializeField] private float fireRushDamage = 20f;
    [SerializeField] private float fireRushKnockBackForce = 1f;
    [SerializeField] private float fireRushHitWidth = 1.5f;
    [SerializeField] private float fireRushHitHeight = 2f;
    [SerializeField] private float fireRushRecoverTime = 1f;

    [Header("거합 (금 패턴4) - 수치")]
    [Range(40f, 50f)]
    [SerializeField] private float iaiDamage = 45f;
    [SerializeField] private float iaiCooldown = 30f;
    [SerializeField] private float iaiRange = 999f;
    [SerializeField] private float iaiKnockBackForce = 1.5f;
    [SerializeField] private float iaiParryRange = 999f;

    [Header("거합 (금 패턴4) - 타이밍")]
    [SerializeField] private float iaiPrepareTime = 1.3f;
    [SerializeField] private float iaiSlashDelay = 0.2f;
    [SerializeField] private float iaiParryGrace = 0.15f;
    [SerializeField] private float iaiRecoverTime = 1.5f;
    [SerializeField] private float iaiDarkenTime = 0.5f;

    [Header("거합 (금 패턴4) - 연출")]
    [SerializeField] private SpriteRenderer iaiDarkOverlay;
    [SerializeField] private GameObject iaiFlashEffect;
    [Range(0f, 1f)]
    [SerializeField] private float iaiOverlayAlpha = 0.85f;
    [SerializeField] private string iaiSlashPoolKey = "Pattern4Slash";
    [SerializeField] private float iaiSlashLifeTime = 0.8f;

    [Header("초기 쿨타임 (전투 시작 후 첫 사용까지)")]
    [SerializeField] private float soilWaveStartDelay = 3f;
    [SerializeField] private float absorptionStartDelay = 10f;
    [SerializeField] private float flyingSwordStartDelay = 12f;
    [SerializeField] private float fireRushStartDelay = 6f;
    [SerializeField] private float iaiStartDelay = 30f;

    [Header("환영 연출")]
    [SerializeField] private float phantomLeadTime = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float phantomAlpha = 0.5f;
    [SerializeField] private Vector2 phantomOffset = Vector2.zero;
    [SerializeField] private bool alignSoilPhantomFeet = true;
    [SerializeField] private bool alignWaterPhantomFeet = false;
    [SerializeField] private bool alignFirePhantomFeet = false;
    [SerializeField] private GameObject soilPhantom;
    [SerializeField] private GameObject waterPhantom;
    [SerializeField] private GameObject firePhantom;

    [Header("히트박스 교체 (전부 보스 루트 오브젝트의 Collider2D)")]
    [SerializeField] private Collider2D normalHurtbox;
    [SerializeField] private Collider2D soilHurtbox;
    [SerializeField] private Collider2D waterHurtbox;
    [SerializeField] private Collider2D fireHurtbox;

    private List<float> curTimes;
    private GameObject player;
    private ComboAttack playerCombo;
    private Playermovement playerMovement;
    private BoxCollider2D bodyCollider;
    private HashSet<string> animatorParams;

    private float verticalVelocity;
    private bool isFacingRight;
    private bool isPatternSetup;
    private float patternElapsed;
    private float groggyTime;

    private bool counterPending;
    private float counterPendingTimer;
    private bool isCounterAttacking;
    private bool counterHitDone;

    private bool wasPlayerAttacking;
    private bool playerAttackStarted;

    private ElementKind activeElement = ElementKind.None;
    private SpriteRenderer activePhantomRenderer;
    private SpriteRenderer[] activePhantomRenderers;
    private bool activePhantomIsRig;

    private bool soilWaveBodyStarted;
    private bool soilWaveFired;
    private bool absorptionBodyStarted;
    private bool fireRushStarted;
    private bool fireRushHitDone;
    private Tween fireRushTween;

    private int reflectedSwordHits;

    private bool isIaiCasting;
    private bool isIaiParryOpen;
    private bool isIaiParried;
    private Sequence iaiSequence;

    public bool GroggyActive => enableGroggy && !IsDead && groggyTime > 0f;

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
                    .Sequence("SoilWave")
                        .Do("CanSoilWave", () => PatternStarter(1, soilWaveRange))
                        .Do("UseSoilWave", SoilWavePattern)
                    .End()
                    .Sequence("Absorption")
                        .Do("CanAbsorption", CanUseAbsorption)
                        .Do("UseAbsorption", AbsorptionPattern)
                    .End()
                    .Sequence("FlyingSword")
                        .Do("CanFlyingSword", () => PatternStarter(3, flyingSwordRange))
                        .Do("UseFlyingSword", FlyingSwordPattern)
                    .End()
                    .Sequence("FireRush")
                        .Do("CanFireRush", () => PatternStarter(4, fireRushRange))
                        .Do("UseFireRush", FireRushPattern)
                    .End()
                    .Sequence("Iai")
                        .Do("CanIai", () => PatternStarter(5, iaiRange))
                        .Do("UseIai", IaiPattern)
                    .End()
                    .Do("Counter", CounterAttack)
                .End()
                .Do("Move", Move)
                .Do("Idle", Idle)
            .End()
            .Build();

        curTimes = new List<float>
        {
            0f,
            soilWaveStartDelay,
            absorptionStartDelay,
            flyingSwordStartDelay,
            fireRushStartDelay,
            iaiStartDelay
        };

        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        CacheAnimatorParams();
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
        if (normalHurtbox == null) normalHurtbox = bodyCollider;

        SnapToGround();

        ClearIaiVisual();
        HidePhantom();
    }

    private void Update()
    {
        if (PauseManager.IsPaused) return;

        bool dialogueActive = DialogueManager.IsDialogueActive;
        SetAnimatorPlayback(!dialogueActive);
        if (dialogueActive) return;

        for (int i = 0; i < curTimes.Count; i++)
        {
            curTimes[i] -= Time.deltaTime;
        }

        if (enableGroggy) groggyTime -= Time.deltaTime;
        UpdateCounterPending();
        UpdatePlayerAttackDetection();
        UpdatePhantomFollow();
        patternElapsed = isPatternSetup ? patternElapsed + Time.deltaTime : 0f;
        SetAnimBool("IsDead", IsDead);
        SetAnimBool("IsGroggy", GroggyActive);
        movedThisFrame = false;
        behaviorTree.Tick();
        ApplyGravity();
        UpdateAnimatorMotion();
    }

    private void UpdateAnimatorMotion()
    {
        SetAnimBool("IsGround", isGrounded);
        SetAnimFloat("VerticalVelocity", isGrounded ? 0f : verticalVelocity);
        SetAnimFloat("Speed", movedThisFrame ? moveSpeed : 0f);
    }

    private void SetAnimatorPlayback(bool playing)
    {
        if (animator != null) animator.speed = playing ? 1f : 0f;

        GameObject phantom = PhantomFor(activeElement);
        if (phantom == null) return;

        var phantomAnimator = phantom.GetComponentInChildren<Animator>(true);
        if (phantomAnimator != null) phantomAnimator.speed = playing ? 1f : 0f;

        var phantomAnimation = phantom.GetComponentInChildren<Animation>(true);
        if (phantomAnimation != null) phantomAnimation.enabled = playing;
    }

    private void OnEnable()
    {
        SetAnimatorPlayback(!PauseManager.IsPaused && !DialogueManager.IsDialogueActive);
    }

    private void PlayPhantomState(string stateName)
    {
        GameObject phantom = PhantomFor(activeElement);
        if (phantom == null || string.IsNullOrEmpty(stateName)) return;

        var legacyAnim = phantom.GetComponentInChildren<Animation>(true);
        if (legacyAnim != null)
        {
            if (legacyAnim.GetClip(stateName) != null) legacyAnim.CrossFade(stateName, 0.1f);
            return;
        }

        var phantomAnimator = phantom.GetComponentInChildren<Animator>(true);
        if (phantomAnimator != null && phantomAnimator.HasState(0, Animator.StringToHash(stateName)))
            phantomAnimator.Play(stateName, 0, 0f);
    }

    private void OnDisable()
    {
        SetAnimatorPlayback(false);
        CancelIai();
        CancelCounter();
        KillFireRushTween();
        HidePhantom();
    }

    public override bool DoDamage(float damage)
    {
        if (IsDead) return false;

        bool applied = base.DoDamage(damage);
        if (!applied || IsDead) return applied;
        if (enableGroggy && groggyTime > 0f) return applied;

        ScheduleCounter();
        return applied;
    }

    public void NotifyReflectedSwordHit()
    {
        if (IsDead) return;

        reflectedSwordHits++;
        if (!enableGroggy) return;
        if (reflectedSwordHits < swordReflectHitCount) return;

        reflectedSwordHits = 0;
        OnParrySuccess("어검 반사");
    }

    private void ScheduleCounter()
    {
        if (IsDead || isCounterAttacking || counterPending) return;
        counterPending = true;
        counterPendingTimer = counterDelay;
    }

    private void UpdateCounterPending()
    {
        if (!counterPending) return;

        counterPendingTimer -= Time.deltaTime;
        if (counterPendingTimer > 0f) return;

        counterPending = false;
        if (IsDead || GroggyActive) return;
        isCounterAttacking = true;
        PlayAttackAnim(1);
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

    private void OnParrySuccess(string label)
    {
        isPatternSetup = false;
        patternElapsed = 0f;
        if (curTimes != null) curTimes[0] = 0f;
        if (enableGroggy) groggyTime = parryGroggyTime;
        Debug.Log($"<color=#00FF88>[최종보스] {label} 쳐내기 성공! (그로기 {(enableGroggy ? parryGroggyTime : 0f)}초)</color>");
    }

    private TaskStatus Dead()
    {
        if (!IsDead) return TaskStatus.Failure;

        CancelIai();
        CancelCounter();
        KillFireRushTween();
        HidePhantom();
        SetAnimBool("IsMoving", false);
        SetAnimBool("IsIdle", false);
        gameObject.layer = LayerMask.NameToLayer("Default");
        return TaskStatus.Success;
    }

    private TaskStatus Groggy()
    {
        if (!GroggyActive) return TaskStatus.Failure;

        SetAnimBool("IsMoving", false);
        SetAnimBool("IsIdle", false);
        return TaskStatus.Success;
    }

    private TaskStatus PatternStarter(int patternIndex, float range)
    {
        if (IsDead || GroggyActive) return TaskStatus.Failure;
        if (curTimes[patternIndex] > 0f) return TaskStatus.Failure;
        if (HorizontalDistance > range) return TaskStatus.Failure;
        return TaskStatus.Success;
    }

    private TaskStatus CanUseAbsorption()
    {
        if (!enableAbsorption) return TaskStatus.Failure;
        return PatternStarter(2, absorptionRange);
    }

    private void BeginElementalPattern(ElementKind kind, float bodyTime, int cooldownIndex, float cooldown)
    {
        curTimes[0] = phantomLeadTime + bodyTime;
        curTimes[cooldownIndex] = cooldown;
        patternElapsed = 0f;
        isPatternSetup = true;
        SetAnimBool("IsMoving", false);
        SetAnimBool("IsIdle", false);
        FacePlayer();
        ShowPhantom(kind);
    }

    private void EndElementalPattern()
    {
        HidePhantom();
        isPatternSetup = false;
    }

    private TaskStatus SoilWavePattern()
    {
        if (IsDead || GroggyActive)
        {
            HidePhantom();
            isPatternSetup = false;
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            BeginElementalPattern(ElementKind.Soil, soilWaveCastTime, 1, soilWaveCooldown);
            soilWaveBodyStarted = false;
            soilWaveFired = false;
        }

        if (!soilWaveBodyStarted && patternElapsed >= phantomLeadTime)
        {
            soilWaveBodyStarted = true;
            SetAnimTrigger("SoilWave");
        }

        if (!soilWaveFired && patternElapsed >= phantomLeadTime + soilWaveSpawnDelay)
        {
            soilWaveFired = true;
            PlayPhantomState("SoilPattern1");
            LaunchSoilWave();
        }

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        EndElementalPattern();
        return TaskStatus.Success;
    }

    private void LaunchSoilWave()
    {
        if (PoolManager.Instance == null) return;
        if (string.IsNullOrEmpty(soilWavePoolKey)) return;

        float dir = isFacingRight ? 1f : -1f;
        Vector3 spawnPos = transform.position + new Vector3(soilWaveSpawnOffset.x * dir, soilWaveSpawnOffset.y, 0f);
        GameObject wave = PoolManager.Instance.Get(soilWavePoolKey, spawnPos, Quaternion.identity);
        if (wave == null) return;

        if (wave.TryGetComponent(out SoilWave soilWave)) soilWave.Launch(dir);
    }

    private TaskStatus AbsorptionPattern()
    {
        if (IsDead || GroggyActive || !enableAbsorption)
        {
            HidePhantom();
            isPatternSetup = false;
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            BeginElementalPattern(ElementKind.Water, absorptionDuration, 2, absorptionCooldown);
            absorptionBodyStarted = false;
        }

        if (!absorptionBodyStarted && patternElapsed >= phantomLeadTime)
        {
            absorptionBodyStarted = true;
            SetAnimTrigger("Absorption");
        }

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        EndElementalPattern();
        return TaskStatus.Success;
    }

    private TaskStatus FlyingSwordPattern()
    {
        if (IsDead || GroggyActive)
        {
            isPatternSetup = false;
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            curTimes[0] = flyingSwordCastTime;
            curTimes[3] = flyingSwordCooldown;
            patternElapsed = 0f;
            reflectedSwordHits = 0;
            isPatternSetup = true;
            SetAnimBool("IsMoving", false);
            SetAnimBool("IsIdle", false);
            SetAnimTrigger("FlyingSword");
            PlayAttackAnim(1);
            FacePlayer();

            DOVirtual.DelayedCall(flyingSwordSpawnDelay, () =>
            {
                if (IsDead || GroggyActive) return;
                if (PoolManager.Instance == null) return;

                for (int i = 0; i < flyingSwordCount; i++)
                {
                    GameObject sword = PoolManager.Instance.Get(
                        flyingSwordPoolKey,
                        transform.position,
                        Quaternion.identity);
                    if (sword == null) continue;
                    PoolManager.Instance.Release(sword, flyingSwordLifeTime);
                }
            });
        }

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private TaskStatus FireRushPattern()
    {
        if (IsDead || GroggyActive)
        {
            KillFireRushTween();
            HidePhantom();
            isPatternSetup = false;
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            BeginElementalPattern(ElementKind.Fire, fireRushWindup + fireRushDuration + fireRushRecoverTime, 4, fireRushCooldown);
            fireRushStarted = false;
            fireRushHitDone = false;
            PlayPhantomState("Warn");
        }

        float rushStart = phantomLeadTime + fireRushWindup;

        if (!fireRushStarted && patternElapsed >= rushStart)
        {
            fireRushStarted = true;
            SetAnimTrigger("FireRush");
            SetAnimTrigger("DashTrigger");
            PlayPhantomState("Rush");
            StartFireRush();
        }

        if (fireRushStarted && !fireRushHitDone && patternElapsed <= rushStart + fireRushDuration)
        {
            TryFireRushHit();
        }

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        KillFireRushTween();
        EndElementalPattern();
        return TaskStatus.Success;
    }

    private void StartFireRush()
    {
        if (player == null) return;

        float dir = Mathf.Sign(player.transform.position.x - transform.position.x);
        Face(dir);
        float targetX = player.transform.position.x + fireRushOvershoot * dir;
        KillFireRushTween();
        fireRushTween = transform.DOMoveX(targetX, fireRushDuration).SetEase(Ease.InCubic);
    }

    private void TryFireRushHit()
    {
        if (player == null) return;
        if (Mathf.Abs(player.transform.position.x - transform.position.x) > fireRushHitWidth) return;
        if (Mathf.Abs(player.transform.position.y - transform.position.y) > fireRushHitHeight) return;

        fireRushHitDone = true;
        if (player.TryGetComponent(out PlayerKnockBack knockBack))
        {
            knockBack.TakeHit(transform.position, fireRushKnockBackForce, Mathf.RoundToInt(fireRushDamage));
            return;
        }

        if (player.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(fireRushDamage);
        }
    }

    private void KillFireRushTween()
    {
        if (fireRushTween != null && fireRushTween.IsActive()) fireRushTween.Kill();
        fireRushTween = null;
    }

    private TaskStatus IaiPattern()
    {
        if (IsDead || GroggyActive)
        {
            CancelIai();
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            float flashTime = Mathf.Max(0f, iaiPrepareTime);
            float slashTime = flashTime + Mathf.Max(0f, iaiSlashDelay);
            float endTime = slashTime + Mathf.Max(0f, iaiRecoverTime);

            curTimes[0] = endTime + 0.1f;
            curTimes[5] = iaiCooldown;
            patternElapsed = 0f;
            isPatternSetup = true;
            isIaiCasting = true;
            isIaiParried = false;
            isIaiParryOpen = false;

            SetAnimBool("IsMoving", false);
            SetAnimBool("IsIdle", false);
            SetAnimTrigger("Iai");
            PlayAttackAnim(3);
            FacePlayer();

            BuildIaiSequence(flashTime, slashTime, endTime);
            Debug.Log("<color=#FFD700>[최종보스] 거합(발도 참격) 시전 시작</color>");
        }

        if (isIaiParryOpen && CheckPlayerParry(iaiParryRange))
        {
            isIaiParried = true;
            CancelIai();
            OnParrySuccess("거합");
            return TaskStatus.Failure;
        }

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        FinishIai();
        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private void BuildIaiSequence(float flashTime, float slashTime, float endTime)
    {
        if (iaiSequence != null && iaiSequence.IsActive()) iaiSequence.Kill();
        PrepareIaiVisual();

        float darkenStart = Mathf.Max(0f, flashTime - iaiDarkenTime);
        float darkenDuration = Mathf.Max(0.01f, flashTime - darkenStart);
        float brightenDuration = Mathf.Max(0.01f, Mathf.Min(iaiDarkenTime, endTime - slashTime));
        float parryOpenTime = Mathf.Max(0f, flashTime - Mathf.Max(0f, iaiParryGrace));

        iaiSequence = DOTween.Sequence();
        iaiSequence.AppendInterval(endTime);

        if (iaiDarkOverlay != null)
        {
            iaiSequence.Insert(darkenStart, CreateOverlayFade(iaiOverlayAlpha, darkenDuration));
            iaiSequence.Insert(slashTime, CreateOverlayFade(0f, brightenDuration));
        }

        iaiSequence.InsertCallback(parryOpenTime, OpenIaiParryWindow);
        iaiSequence.InsertCallback(flashTime, ShowIaiFlash);
        iaiSequence.InsertCallback(slashTime, ResolveIaiSlash);
        iaiSequence.OnComplete(FinishIai);
    }

    private Tween CreateOverlayFade(float targetAlpha, float duration)
    {
        SpriteRenderer overlay = iaiDarkOverlay;
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

    private void OpenIaiParryWindow()
    {
        if (!isIaiCasting) return;
        isIaiParryOpen = true;
        Debug.Log("<color=#00FFFF>[최종보스] 거합 쳐내기 가능 구간 시작</color>");
    }

    private void ShowIaiFlash()
    {
        if (!isIaiCasting) return;
        if (iaiFlashEffect == null) return;
        if (!iaiFlashEffect.transform.IsChildOf(transform))
        {
            iaiFlashEffect.transform.position = transform.position;
        }
        iaiFlashEffect.SetActive(true);
    }

    private void ResolveIaiSlash()
    {
        if (!isIaiCasting) return;
        isIaiParryOpen = false;
        if (iaiFlashEffect != null) iaiFlashEffect.SetActive(false);
        if (isIaiParried) return;

        SpawnIaiSlash();
        ApplyIaiDamage();
    }

    private void SpawnIaiSlash()
    {
        if (PoolManager.Instance == null) return;
        if (string.IsNullOrEmpty(iaiSlashPoolKey)) return;

        GameObject slash = PoolManager.Instance.Get(iaiSlashPoolKey, Vector3.zero, Quaternion.identity);
        if (slash == null) return;

        PoolManager.Instance.Release(slash, iaiSlashLifeTime);
    }

    private void ApplyIaiDamage()
    {
        if (player == null) return;

        if (player.TryGetComponent(out PlayerKnockBack knockBack))
        {
            knockBack.TakeHit(transform.position, iaiKnockBackForce, Mathf.RoundToInt(iaiDamage));
            return;
        }

        if (player.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(iaiDamage);
        }
    }

    private void CancelIai()
    {
        isPatternSetup = false;
        if (!isIaiCasting) return;

        isIaiCasting = false;
        isIaiParryOpen = false;
        if (curTimes != null) curTimes[0] = 0f;
        if (iaiSequence != null && iaiSequence.IsActive()) iaiSequence.Kill();
        iaiSequence = null;
        ClearIaiVisual();
    }

    private void FinishIai()
    {
        if (!isIaiCasting) return;

        isIaiCasting = false;
        isIaiParryOpen = false;
        if (iaiSequence != null && iaiSequence.IsActive() && !iaiSequence.IsComplete())
        {
            iaiSequence.Kill();
        }
        iaiSequence = null;
        ClearIaiVisual();
    }

    private void PrepareIaiVisual()
    {
        if (iaiDarkOverlay != null)
        {
            Color color = iaiDarkOverlay.color;
            color.a = 0f;
            iaiDarkOverlay.color = color;
            iaiDarkOverlay.gameObject.SetActive(true);
        }
        if (iaiFlashEffect != null) iaiFlashEffect.SetActive(false);
    }

    private void ClearIaiVisual()
    {
        if (iaiDarkOverlay != null)
        {
            Color color = iaiDarkOverlay.color;
            color.a = 0f;
            iaiDarkOverlay.color = color;
            iaiDarkOverlay.gameObject.SetActive(false);
        }
        if (iaiFlashEffect != null) iaiFlashEffect.SetActive(false);
    }

    private TaskStatus CounterAttack()
    {
        if (IsDead || GroggyActive)
        {
            CancelCounter();
            return TaskStatus.Failure;
        }
        if (!isCounterAttacking) return TaskStatus.Failure;

        if (!isPatternSetup)
        {
            curTimes[0] = counterDuration;
            patternElapsed = 0f;
            isPatternSetup = true;
            counterHitDone = false;
            SetAnimBool("IsMoving", false);
            SetAnimBool("IsIdle", false);
            SetAnimTrigger("CounterAttack");
            FacePlayer();
        }

        if (CheckPatternParry(counterParryRange, counterParryStart, counterParryEnd))
        {
            CancelCounter();
            OnParrySuccess("반격");
            return TaskStatus.Failure;
        }

        if (!counterHitDone && patternElapsed >= counterHitDelay)
        {
            counterHitDone = true;
            ApplyCounterHit();
        }

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        isCounterAttacking = false;
        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private void ApplyCounterHit()
    {
        if (player == null) return;
        if (HorizontalDistance > counterHitRange) return;

        if (player.TryGetComponent(out PlayerKnockBack knockBack))
        {
            knockBack.TakeHit(transform.position, counterKnockBackForce, Mathf.RoundToInt(counterDamage));
            return;
        }

        if (player.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(counterDamage);
        }
    }

    private void CancelCounter()
    {
        counterPending = false;
        counterPendingTimer = 0f;
        if (!isCounterAttacking) return;

        isCounterAttacking = false;
        isPatternSetup = false;
        if (curTimes != null) curTimes[0] = 0f;
        ResetAnimTrigger("CounterAttack");
    }

    private float HorizontalDistance =>
        player == null ? float.MaxValue : Mathf.Abs(player.transform.position.x - transform.position.x);

    private TaskStatus Move()
    {
        if (IsDead || GroggyActive || isCounterAttacking) return TaskStatus.Failure;
        if (player == null) return TaskStatus.Failure;
        if (HorizontalDistance <= approachStopRange) return TaskStatus.Failure;

        movedThisFrame = true;
        SetAnimBool("IsMoving", true);
        SetAnimBool("IsIdle", false);
        float direction = Mathf.Sign(player.transform.position.x - transform.position.x);
        Face(direction);
        transform.Translate(Vector3.right * (direction * moveSpeed * Time.deltaTime), Space.World);
        return TaskStatus.Success;
    }

    private TaskStatus Idle()
    {
        if (IsDead || GroggyActive || isCounterAttacking) return TaskStatus.Failure;

        SetAnimBool("IsMoving", false);
        SetAnimBool("IsIdle", true);
        if (player != null) Face(Mathf.Sign(player.transform.position.x - transform.position.x));
        return TaskStatus.Success;
    }

    private void FacePlayer()
    {
        if (player == null) return;
        Face(Mathf.Sign(player.transform.position.x - transform.position.x));
    }

    private void Face(float direction)
    {
        if (Mathf.Approximately(direction, 0f)) return;

        bool facingRight = direction > 0f;
        if (spriteRenderer != null) spriteRenderer.flipX = spriteFacesRight ? !facingRight : facingRight;
        if (facingRight == isFacingRight)
        {
            SyncPhantomFacing();
            return;
        }

        isFacingRight = facingRight;
        MirrorChild(iaiFlashEffect);
        SyncPhantomFacing();
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

    private GameObject PhantomFor(ElementKind kind)
    {
        switch (kind)
        {
            case ElementKind.Soil: return soilPhantom;
            case ElementKind.Water: return waterPhantom;
            case ElementKind.Fire: return firePhantom;
            default: return null;
        }
    }

    private Collider2D HurtboxFor(ElementKind kind)
    {
        switch (kind)
        {
            case ElementKind.Soil: return soilHurtbox;
            case ElementKind.Water: return waterHurtbox;
            case ElementKind.Fire: return fireHurtbox;
            default: return null;
        }
    }

    private void ShowPhantom(ElementKind kind)
    {
        HidePhantom();
        activeElement = kind;

        GameObject phantom = PhantomFor(kind);
        if (phantom != null)
        {
            phantom.SetActive(true);
            activePhantomRenderers = phantom.GetComponentsInChildren<SpriteRenderer>(true);
            activePhantomRenderer = activePhantomRenderers.Length > 0 ? activePhantomRenderers[0] : null;
            activePhantomIsRig = phantom.GetComponent<SpriteRenderer>() == null && activePhantomRenderers.Length > 0;
            ApplyPhantomAlpha();
            phantomAlignY = ComputePhantomAlignY(phantom);
        }

        ApplyHurtbox(kind);
        UpdatePhantomFollow();
    }

    private void HidePhantom()
    {
        if (soilPhantom != null) soilPhantom.SetActive(false);
        if (waterPhantom != null) waterPhantom.SetActive(false);
        if (firePhantom != null) firePhantom.SetActive(false);
        activePhantomRenderer = null;
        activePhantomRenderers = null;
        activePhantomIsRig = false;
        activeElement = ElementKind.None;
        ApplyHurtbox(ElementKind.None);
    }

    private void ApplyPhantomAlpha()
    {
        if (activePhantomRenderers == null) return;

        for (int i = 0; i < activePhantomRenderers.Length; i++)
        {
            SpriteRenderer sr = activePhantomRenderers[i];
            if (sr == null) continue;
            Color color = sr.color;
            color.a = phantomAlpha;
            sr.color = color;
        }
    }

    private void UpdatePhantomFollow()
    {
        if (activeElement == ElementKind.None) return;

        GameObject phantom = PhantomFor(activeElement);
        if (phantom == null) return;

        if (phantom.transform.parent == transform)
            phantom.transform.localPosition = new Vector3(phantomOffset.x, phantomOffset.y + phantomAlignY, 0f);
        else
            phantom.transform.position = transform.position + new Vector3(phantomOffset.x, phantomOffset.y + phantomAlignY, 0f);

        SyncPhantomFacing();
    }

    private float phantomAlignY;

    private bool ShouldAlignPhantomFeet(ElementKind kind)
    {
        switch (kind)
        {
            case ElementKind.Soil: return alignSoilPhantomFeet;
            case ElementKind.Water: return alignWaterPhantomFeet;
            case ElementKind.Fire: return alignFirePhantomFeet;
            default: return false;
        }
    }

    private float ComputePhantomAlignY(GameObject phantom)
    {
        if (!ShouldAlignPhantomFeet(activeElement) || phantom == null) return 0f;

        var box = normalHurtbox as BoxCollider2D;
        if (box == null) box = bodyCollider;
        if (box == null) return 0f;

        float colliderBottomLocal = box.offset.y - box.size.y * 0.5f;

        if (activePhantomIsRig)
        {
            if (phantom.transform.parent == transform) return colliderBottomLocal;
            return colliderBottomLocal * Mathf.Abs(transform.lossyScale.y);
        }

        if (activePhantomRenderer == null || activePhantomRenderer.sprite == null) return 0f;
        float spriteBottom = activePhantomRenderer.sprite.bounds.min.y;

        if (phantom.transform.parent == transform)
            return colliderBottomLocal - spriteBottom * phantom.transform.localScale.y;

        return colliderBottomLocal * Mathf.Abs(transform.lossyScale.y)
            - spriteBottom * Mathf.Abs(phantom.transform.lossyScale.y);
    }

    private void SyncPhantomFacing()
    {
        if (activePhantomIsRig)
        {
            GameObject phantom = PhantomFor(activeElement);
            if (phantom == null) return;

            Vector3 scale = phantom.transform.localScale;
            float sign = isFacingRight ? -1f : 1f;
            scale.x = Mathf.Abs(scale.x) * sign;
            phantom.transform.localScale = scale;
            return;
        }

        if (activePhantomRenderer == null) return;
        activePhantomRenderer.flipX = isFacingRight;
    }

    private void ApplyHurtbox(ElementKind kind)
    {
        Collider2D target = HurtboxFor(kind);
        bool useElement = kind != ElementKind.None && target != null;

        if (soilHurtbox != null) soilHurtbox.enabled = useElement && target == soilHurtbox;
        if (waterHurtbox != null) waterHurtbox.enabled = useElement && target == waterHurtbox;
        if (fireHurtbox != null) fireHurtbox.enabled = useElement && target == fireHurtbox;
        if (normalHurtbox != null) normalHurtbox.enabled = !useElement;
    }

    private Collider2D CurrentBodyCollider()
    {
        Collider2D element = HurtboxFor(activeElement);
        if (element != null && element.enabled) return element;
        if (normalHurtbox != null && normalHurtbox.enabled) return normalHurtbox;
        return bodyCollider;
    }

    [SerializeField] private float fallRescueDepth = 12f;
    private Vector3 lastGroundedPosition;
    private bool hasGroundedPosition;
    private bool snappedToGround;
    private bool isGrounded;
    private bool movedThisFrame;

    private Bounds GroundProbeBounds()
    {
        var box = normalHurtbox as BoxCollider2D;
        if (box == null) box = bodyCollider;
        if (box != null)
        {
            Vector3 s = transform.lossyScale;
            Vector2 center = (Vector2)transform.position
                + new Vector2(box.offset.x * s.x, box.offset.y * s.y);
            Vector2 size = new Vector2(
                Mathf.Abs(box.size.x * s.x),
                Mathf.Abs(box.size.y * s.y));
            return new Bounds(center, size);
        }

        Collider2D col = CurrentBodyCollider();
        return col != null ? col.bounds : new Bounds(transform.position, Vector3.one);
    }

    private void SnapToGround()
    {
        Bounds probe = GroundProbeBounds();
        Vector2 castOrigin = new Vector2(probe.center.x, probe.center.y + 30f);
        RaycastHit2D hit = Physics2D.BoxCast(castOrigin, probe.size, 0f, Vector2.down, 120f, groundMask);
        if (hit.collider == null) return;

        float delta = (castOrigin.y - hit.distance) - probe.center.y;
        if (Mathf.Abs(delta) > 0.001f)
            transform.Translate(Vector3.up * delta, Space.World);

        verticalVelocity = 0f;
        snappedToGround = true;
        RememberGrounded();
        UpdateAnimatorMotion();
    }

    private void RememberGrounded()
    {
        isGrounded = true;
        lastGroundedPosition = transform.position;
        hasGroundedPosition = true;
    }

    private void ApplyGravity()
    {
        if (!snappedToGround)
        {
            SnapToGround();
            if (snappedToGround) return;
        }

        Bounds bounds = GroundProbeBounds();

        Collider2D overlapped = Physics2D.OverlapBox(bounds.center, bounds.size * 0.98f, 0f, groundMask);
        if (overlapped != null)
        {
            float push = overlapped.bounds.max.y - bounds.min.y;
            if (push > 0f) transform.Translate(Vector3.up * push, Space.World);
            verticalVelocity = 0f;
            RememberGrounded();
            return;
        }

        float fallThisFrame = verticalVelocity < 0f ? -verticalVelocity * Time.deltaTime : 0f;
        float castDistance = Mathf.Max(groundCheckDistance, fallThisFrame);
        RaycastHit2D hit = Physics2D.BoxCast(
            bounds.center,
            bounds.size,
            0f,
            Vector2.down,
            castDistance,
            groundMask);

        if (hit.collider != null && verticalVelocity <= 0f)
        {
            if (hit.distance > groundCheckDistance)
                transform.Translate(Vector3.down * (hit.distance - groundCheckDistance * 0.5f), Space.World);
            verticalVelocity = 0f;
            RememberGrounded();
            return;
        }

        isGrounded = false;
        verticalVelocity += gravity * Time.deltaTime;
        transform.Translate(Vector3.up * (verticalVelocity * Time.deltaTime), Space.World);

        if (hasGroundedPosition && transform.position.y < lastGroundedPosition.y - fallRescueDepth)
        {
            transform.position = lastGroundedPosition;
            verticalVelocity = 0f;
        }
    }

    private void CacheAnimatorParams()
    {
        animatorParams = new HashSet<string>();
        if (animator == null || animator.runtimeAnimatorController == null) return;
        foreach (var param in animator.parameters) animatorParams.Add(param.name);
    }

    private void SetAnimBool(string paramName, bool value)
    {
        if (animator == null || animatorParams == null || !animatorParams.Contains(paramName)) return;
        animator.SetBool(paramName, value);
    }

    private void SetAnimTrigger(string paramName)
    {
        if (animator == null || animatorParams == null || !animatorParams.Contains(paramName)) return;
        animator.SetTrigger(paramName);
    }

    private void SetAnimFloat(string paramName, float value)
    {
        if (animator == null || animatorParams == null || !animatorParams.Contains(paramName)) return;
        animator.SetFloat(paramName, value);
    }

    private void SetAnimInt(string paramName, int value)
    {
        if (animator == null || animatorParams == null || !animatorParams.Contains(paramName)) return;
        animator.SetInteger(paramName, value);
    }

    private void PlayAttackAnim(int attackIndex)
    {
        SetAnimInt("AttackIndex", attackIndex);
        SetAnimTrigger("AttackTrigger");
    }

    private void ResetAnimTrigger(string paramName)
    {
        if (animator == null || animatorParams == null || !animatorParams.Contains(paramName)) return;
        animator.ResetTrigger(paramName);
    }
}

/* [파일 노트]
 *
 * ─────────────────────────────────────────────────────────────
 * 최종보스 개요 (금보스 Gold.cs 기반 킷)
 * ─────────────────────────────────────────────────────────────
 * 사용 패턴 : 어검(금 패턴3 FlyingSword) / 거합(금 패턴4 발도 참격) / 상시 반격 / 속성 3종(토·수·화).
 * 금 패턴1(검기)·패턴2(검 함정)는 없다.
 *
 * BT 셀렉터 우선순위(위→아래) : 토 파동 > 수 잠식 > 어검 > 화 돌진 > 거합 > 반격.
 * 그 아래 Move/Idle. 반격은 대기 상태가 아니라 "피격 후 지연 발동" 플래그(isCounterAttacking)로 켜진다.
 *
 * curTimes 인덱스 : [0]=현재 패턴 진행 타이머(공유) [1]=토 파동 [2]=수 잠식 [3]=어검 [4]=화 돌진 [5]=거합.
 * 초기값은 "초기 쿨타임" 헤더의 StartDelay 필드들로 시딩된다(거합은 금보스처럼 기본 30초 뒤 첫 발동).
 *
 * ─────────────────────────────────────────────────────────────
 * 피격 = 상시 데미지 + 지연 반격
 * ─────────────────────────────────────────────────────────────
 * 금보스와 달리 그로기 없이 항상 base.DoDamage 로 체력이 깎인다.
 * 데미지가 실제로 들어간 피격마다 ScheduleCounter() 가 counterDelay(기본 0.5초) 타이머를 건다.
 * 타이머 만료 시 isCounterAttacking = true → BT 가 반격 노드에 도달하면 반격 동작 수행.
 *   - 이미 반격 중이거나 예약 중이면 추가 피격은 무시(중첩 금지).
 *   - 다른 패턴이 진행 중이면 그 패턴이 끝난 뒤 반격이 나간다(셀렉터 순서상 최하위).
 *   - 반격 타격은 patternElapsed 기반(counterHitDelay 시점 1회, counterHitRange 이내)이라
 *     쳐내기로 캔슬하면 타격 자체가 발생하지 않는다(금보스의 DelayedCall 방식 대신 채택).
 *
 * 그로기 골격 : enableGroggy(기본 false)를 켜면 금보스식 그로기가 부활한다 —
 * 쳐내기 성공/어검 5회 반사 시 parryGroggyTime 그로기, 그로기 중 피격은 반격을 걸지 않는다.
 * 꺼져 있으면 groggyTime 은 어디서도 세팅되지 않고 Groggy BT 태스크는 항상 Failure.
 *
 * ─────────────────────────────────────────────────────────────
 * 쳐내기(패링) — 금 유래 스킬에만 적용
 * ─────────────────────────────────────────────────────────────
 * 판정 로직은 금보스와 동일(CheckPlayerParry: ComboAttack.IsLunging 상승 엣지 + 사거리 + 방향).
 *   - 거합  : 섬광 iaiParryGrace 전 ~ 참격 순간(연출 시퀀스 콜백이 창 개폐). 성공 시 참격 무효화.
 *   - 반격  : 반격 시작 후 counterParryStart~End 창. 성공 시 반격 타격 무효화.
 *   - 어검  : FlyingSword 자체의 쳐내기(공격 히트박스 접촉)로 검이 반사된다. 반사된 검이 보스에
 *             닿으면 NotifyReflectedSwordHit() — enableGroggy false 면 카운트만 하고 아무 일 없음.
 * 속성 패턴(토 파동/수 잠식/화 돌진)은 패링 판정이 아예 없다.
 * 패링 성공 보상은 "해당 공격 무효화"뿐이며 그로기는 enableGroggy 를 켠 경우에만 붙는다.
 *
 * ─────────────────────────────────────────────────────────────
 * 속성 패턴 3종 + 환영 연출
 * ─────────────────────────────────────────────────────────────
 * 공통 흐름 : 패턴 진입 즉시 해당 속성 보스의 반투명 환영을 띄우고(히트박스도 교체),
 * phantomLeadTime(기본 1초) 후 본체 동작 시작, 패턴이 끝나면 환영을 끄고 히트박스 원복.
 *
 *   토 파동  : phantomLeadTime + soilWaveSpawnDelay 시점에 바라보는 방향으로 SoilWave 투사체
 *              (풀 키 soilWavePoolKey) 1발 발사. 투사체는 플레이어 스킬2 지형(SkillGroundMarker)에
 *              닿으면 소멸한다. 수치는 SoilWave 프리팹 쪽 SerializeField 가 담당.
 *   수 잠식  : 상세 기획 미정. enableAbsorption(기본 false)이 꺼져 있으면 CanUseAbsorption 이
 *              Failure 를 반환해 노드 자체가 즉시 스킵된다. 켜면 환영+absorptionDuration 대기만
 *              수행하는 빈 슬롯으로 돌고, 쿨타임(curTimes[2])만 소모한다.
 *   화 돌진  : 화보스 패턴2 돌진의 지상판. windup 동안 조준 → DOMoveX(InCubic)로 지상 돌진
 *              (y 는 ApplyGravity 가 유지) → 돌진 구간 동안 사각 판정(fireRushHitWidth/Height)
 *              1회 접촉 피해 → recover. 트윈은 사망/비활성/그로기 시 Kill.
 *
 * 환영 오브젝트(soilPhantom/waterPhantom/firePhantom)는 빌더/유저가 배치해 참조만 꽂는다.
 * 보스의 자식이면 localPosition = phantomOffset 으로, 아니면 매 프레임 보스 위치를 따라간다.
 *
 * 환영 두 가지 형태를 지원한다 (ShowPhantom 시점에 자동 판별, activePhantomIsRig):
 *   - 단일 SR형(수/화): 루트에 SpriteRenderer. 좌우반전 = flipX(원본이 왼쪽 보기라 flipX = 오른쪽 보기),
 *     발 정렬 = 스프라이트 bounds.min.y 기준(기존 로직).
 *   - 리그형(토): 루트에 SR 없음 + 자식 SR 다수(Soil.prefab 비주얼 복제, 본 리깅 레거시 Animation).
 *     좌우반전 = 루트 localScale.x 부호 반전(원본 아트 왼쪽 보기 → 오른쪽 볼 때 음수).
 *     발 정렬 = 원본 피봇이 발이므로 스프라이트 bounds 없이 루트 피봇을 콜라이더 바닥에 정렬
 *     (자식이면 colliderBottomLocal, 월드 배치면 ×|lossyScale.y|).
 * 알파는 표시 시점에 캐시한 "모든" 자식 SpriteRenderer 에 phantomAlpha 로 강제된다.
 * 환영 애니메이션 탐색(GetComponentInChildren)이라 Animation/Animator 가 자식에 있어도 동작한다.
 *
 * 히트박스 교체 : normalHurtbox ↔ soil/water/fireHurtbox 의 enabled 토글.
 * 접지/중력    : 중력 판정은 히트박스 교체와 무관하게 항상 기본 몸통(normalHurtbox 의
 *               size/offset 을 스케일 반영해 직접 계산 — 비활성이어도 유효)을 기준으로 한다.
 *               속성 콜라이더는 피격 판정 전용이라 이것으로 접지를 재면 패턴마다 침하/부상한다.
 *               Awake 에서 SnapToGround 로 지면 표면에 1회 스냅(실패 시 전투 첫 프레임 재시도),
 *               착지 스냅·파묻힘 밀어올림·낙하속도 비례 캐스트로 터널링을 막고, 마지막 접지
 *               위치를 기억해 fallRescueDepth 이상 낙하(콜라이더 없는 구멍)하면 그 위치로 복귀한다.
 * ★ 모든 피격 콜라이더는 반드시 FinalBoss 컴포넌트와 "같은 루트 오브젝트"의 Collider2D 여야 한다.
 *   Attackhitbox 가 other.TryGetComponent<BossBase> 로 같은 오브젝트만 검사하기 때문에
 *   자식 오브젝트의 콜라이더에는 플레이어 공격이 들어가지 않는다.
 * 해당 속성 콜라이더 참조가 비어 있으면 교체를 생략하고 normalHurtbox 를 유지한다(안전).
 * 중력 지면 판정(ApplyGravity)은 현재 켜져 있는 피격 콜라이더의 bounds 를 쓴다
 * (꺼진 콜라이더의 bounds 는 신뢰할 수 없으므로).
 *
 * ─────────────────────────────────────────────────────────────
 * 거합(발도 참격)
 * ─────────────────────────────────────────────────────────────
 * 금보스 패턴4 타임라인을 그대로 이식(iai* 필드로 개명). 암전 오버레이(iaiDarkOverlay),
 * 섬광(iaiFlashEffect)은 씬 참조·null 허용, 참격 화면 이펙트는 풀 키 iaiSlashPoolKey
 * (기본 "Pattern4Slash" — 금보스와 같은 Screen Space Overlay 프리팹 재사용).
 * BT 타이머(curTimes[0])는 시퀀스 총 길이 + 0.1초로 시퀀스가 항상 먼저 끝난다.
 *
 * ─────────────────────────────────────────────────────────────
 * 좌우반전 / 애니메이션 / 일시정지
 * ─────────────────────────────────────────────────────────────
 * 좌우반전은 spriteRenderer.flipX 방식(Y회전 금지 — 자식 체력바 캔버스가 뒤집힌다).
 * 스프라이트가 플레이어 리컬러(원본이 오른쪽 보기)라면 spriteFacesRight 를 켜면 flipX 가 반전된다.
 * 방향 전환 시 MirrorChild 로 iaiFlashEffect 의 localPosition/scale.x 를 미러링한다.
 *
 * 애니메이터는 전부 선택 사항이다. Awake 에서 파라미터 이름을 캐시해 두고
 * 존재하는 파라미터만 Set/Reset 하므로(빈 컨트롤러/파라미터 없음 모두 무해)
 * 컨트롤러가 없어도 로직은 완전히 동작한다.
 * 인식하는 파라미터: bool IsDead/IsGroggy/IsMoving/IsIdle,
 * trigger SoilWave/Absorption/FlyingSword/FireRush/Iai/CounterAttack.
 *
 * Update 첫 줄 게이트(PauseManager.IsPaused || DialogueManager.IsDialogueActive)로
 * BT/쿨타임/반격 예약/중력이 전부 멈춘다. 거합 시퀀스·어검 소환 DelayedCall·돌진 트윈은
 * PauseManager 의 DOTween.PauseAll 로 함께 멈추고, 피해는 PlayerKnockBack.TakeHit 게이트가 차단.
 * FinalBossRoom 이 대사/전투 제어를 위해 이 컴포넌트의 enabled 를 끄면 OnDisable 이
 * 거합 연출·반격 예약·돌진 트윈·환영/히트박스를 전부 원상 복구한다.
 */

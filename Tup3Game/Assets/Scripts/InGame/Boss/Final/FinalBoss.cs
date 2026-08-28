using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class FinalBoss : BossBase
{
    private const string StandPlatformName = "StandPlatform";
    private const string SwordEffectName = "SwordEffect";

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
    [SerializeField] private float groundProbeDistance = 40f;
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

    [Header("공격 컷 동기화 (플레이어 공격 클립 기준)")]
    [SerializeField] private bool hitFollowsAttackAnimation = true;
    [Range(0f, 1f)]
    [SerializeField] private float counterHitNormalizedTime = 0.4f;

    [Header("쳐내기 판정 - 반격")]
    [SerializeField] private float counterParryStart = 0f;
    [SerializeField] private float counterParryRange = 4f;

    [Header("토 파동")]
    [SerializeField] private float soilWaveCooldown = 8f;
    [SerializeField] private float soilWaveRange = 100f;
    [SerializeField] private float soilWaveCastTime = 2f;
    [SerializeField] private float soilWaveSpawnDelay = 0.7f;
    [SerializeField] private string soilWavePoolKey = "SoilWave";
    [SerializeField] private Vector2 soilWaveSpawnOffset = new Vector2(1.5f, 0.5f);

    [Header("토 내리침 근접 판정")]
    [SerializeField] private float soilMeleeStart = 0.6f;
    [SerializeField] private float soilMeleeEnd = 0.9f;
    [SerializeField] private float soilMeleeDamage = 50f;
    [SerializeField] private float soilMeleeKnockBackForce = 0f;
    [SerializeField] private Vector2 soilMeleeOffset = new Vector2(4.39f, 2.2f);
    [SerializeField] private Vector2 soilMeleeSize = new Vector2(2f, 4f);

    [Header("수 물기둥")]
    [SerializeField] private bool enableWaterSprout = true;
    [SerializeField] private float waterSproutCooldown = 12f;
    [SerializeField] private float waterSproutRange = 100f;
    [SerializeField] private float waterSproutDuration = 3f;
    [SerializeField] private int waterSproutCount = 5;
    [SerializeField] private float waterSproutSpawnInterval = 0.25f;
    [SerializeField] private int waterSproutSortingOrder = 1;
    [SerializeField] private bool waterSproutShowPathPreview = false;
    [SerializeField] private bool waterSproutShowHitboxGizmo = false;
    [SerializeField] private float waterSproutSpacing = 2f;
    [SerializeField] private float waterSproutWarnTime = 1f;
    [SerializeField] private float waterSproutDamage = 10f;
    [SerializeField] private float waterSproutHeight = 5f;
    [SerializeField] private float waterSproutWidth = 1f;
    [SerializeField] private string waterSproutPoolKey = "Water_Pump";

    [Header("어검 (금 패턴3)")]
    [SerializeField] private float flyingSwordCooldown = 60f;
    [SerializeField] private float flyingSwordRange = 100f;
    [SerializeField] private float flyingSwordCastTime = 8f;
    [SerializeField] private float flyingSwordSpawnDelay = 1f;
    [SerializeField] private int flyingSwordCount = 5;
    [SerializeField] private string flyingSwordPoolKey = "FlyingSword";
    [SerializeField] private float flyingSwordLifeTime = 10f;
    [SerializeField] private int swordReflectHitCount = 5;
    [SerializeField] private float reflectedSwordDamage = 10f;

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

    [Header("화 돌진 - 화염구 발사")]
    [SerializeField] private int fireRushLavaCount = 8;
    [SerializeField] private float fireRushLavaSpreadX = 7f;
    [SerializeField] private Vector2 fireRushLavaFlightTime = new Vector2(2f, 3f);
    [SerializeField] private int fireRushLavaGroundRetries = 6;
    [SerializeField] private LayerMask fireRushLavaWallMask = 1 << 10;
    [SerializeField] private float fireRushLavaWallMargin = 0.5f;
    [SerializeField] private string fireRushLavaPoolKey = "Lava";
    [SerializeField] private float fireRushLavaLifeTime = 5f;

    [Header("화 돌진 - 화염구 굳음")]
    [SerializeField] private float lavaHardenTime = 3f;
    [SerializeField] private Color lavaHardenedColor = Color.black;
    [SerializeField] private int lavaHardenedMaxCount = 60;

    [Header("돌진 콤보 (플레이어 3단 콤보)")]
    [SerializeField] private bool enablePlayerCombo = true;
    [SerializeField] private float playerComboCooldown = 20f;
    [SerializeField] private float playerComboRange = 100f;
    [SerializeField] private float playerComboApproachRange = 2f;
    [SerializeField] private float playerComboApproachSpeed = 8f;
    [SerializeField] private float playerComboApproachTimeout = 3f;
    [SerializeField] private float playerComboDamage1 = 10f;
    [SerializeField] private float playerComboDamage2 = 15f;
    [SerializeField] private float playerComboDamage3 = 20f;
    [SerializeField] private float playerComboKnockBackForce = 0.5f;
    [SerializeField] private float playerComboRecoverTime = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float playerComboHitNormalizedTime = 0.4f;

    [Header("돌진 콤보 - 쳐내기")]
    [SerializeField] private bool playerComboParryable = true;
    [SerializeField] private float playerComboParryRange = 999f;

    [Header("돌진 콤보 - 플레이어 기준값 (자동 계산, 아래는 폴백)")]
    [SerializeField] private bool playerComboCopyPlayerProfile = true;
    [SerializeField] private float playerComboLungeScale = 1f;
    [SerializeField] private float playerComboFallbackAttackSpeed = 1.5f;
    [SerializeField] private Vector3 playerComboFallbackDurations = new Vector3(0.278f, 0.278f, 0.278f);
    [SerializeField] private float playerComboFallbackGap = 0.2f;
    [SerializeField] private float playerComboFallbackCharge = 0.2f;
    [SerializeField] private Vector2 playerComboFallbackHitboxSize = new Vector2(5f, 2f);
    [SerializeField] private Vector2 playerComboFallbackHitboxOffset = new Vector2(0.18f, -0.14f);

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
    [SerializeField] private float waterSproutStartDelay = 10f;
    [SerializeField] private float flyingSwordStartDelay = 12f;
    [SerializeField] private float fireRushStartDelay = 6f;
    [SerializeField] private float iaiStartDelay = 30f;
    [SerializeField] private float playerComboStartDelay = 20f;

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

    [Header("검기 이펙트 (플레이어 Attack_animation 복제)")]
    [SerializeField] private bool enableSwordEffect = true;
    [SerializeField] private float swordEffectDuration = 0.28f;
    [SerializeField] private float swordEffectScaleMultiplier = 1f;

    [Header("이동 애니메이션 (플레이어 컨트롤러 상태)")]
    [SerializeField] private bool forceLocomotionState = true;
    [SerializeField] private string runStateName = "Move_2";
    [SerializeField] private string idleStateName = "Idle";

    [Header("발판 콜라이더 (플레이어가 보스를 밟는 용도)")]
    [SerializeField] private bool enableStandPlatform = true;
    [SerializeField] private bool standPlatformFollowsHurtbox = true;
    [SerializeField] private Vector2 standPlatformSize = new Vector2(0.875f, 0.953125f);
    [SerializeField] private Vector2 standPlatformOffset = Vector2.zero;
    [SerializeField] private int standPlatformLayer = -1;
    [SerializeField] private int hurtboxLayer = -1;

    [Header("사운드")]
    [SerializeField] private float swingSoundVolume = 1f;
    [SerializeField] private float dashSoundVolume = 0.8f;
    [SerializeField] private float parrySoundVolume = 1f;
    [SerializeField] private float soilWaveSoundVolume = 1f;
    [SerializeField] private float fireRushSoundVolume = 1f;

    private const string SwingMeleeSound = "Gold_SwingMelee";
    private const string SwingSwordSound = "Gold_SwingSword";
    private const string ScreenSlashSound = "Gold_ScreenSlash";
    private const string DrawSound = "Gold_Draw";
    private const string DashSound = "Gold_Dash";
    private const string LightHitSound = "Gold_HitLight";
    private const string ParrySuccessSound = "Parry_Success";
    private const string SoilSmashSound = "Soil_Smash";
    private const string FireRushHitSound = "Fire_RushHit";
    private const string FireRushHitSound2 = "Fire_RushHit2";

    protected override string DefaultHitSoundName => LightHitSound;

    [Header("히트박스 교체 (전부 보스 루트 오브젝트의 Collider2D)")]
    [SerializeField] private Collider2D normalHurtbox;
    [SerializeField] private Collider2D soilHurtbox;
    [SerializeField] private Collider2D waterHurtbox;
    [SerializeField] private Collider2D fireHurtbox;

    private List<float> curTimes;
    private GameObject player;
    private ComboAttack playerCombo;
    private Playermovement playerMovement;
    private Skills playerSkills;
    private PlayerKnockBack playerKnockBack;
    private BoxCollider2D bodyCollider;
    private BoxCollider2D standPlatform;
    private Attack_animation swordEffect;
    private Vector3 swordEffectBaseLocalPosition;
    private bool swordEffectReady;
    private Tween swordEffectHideTween;
    private HashSet<string> animatorParams;
    private int[] attackStateHashes;

    private float verticalVelocity;
    private bool isFacingRight;
    private bool isPatternSetup;
    private float patternElapsed;
    private float groggyTime;

    private bool counterPending;
    private float counterPendingTimer;
    private bool isCounterAttacking;
    private bool counterHitDone;
    private bool counterAnimSeen;

    private bool comboApproaching;
    private int comboStep;
    private float comboStepElapsed;
    private bool comboStepHitDone;
    private bool comboAnimSeen;
    private readonly float[] comboStepDurations = new float[3];
    private Vector3 comboLungeDistances;
    private float comboGapTime;
    private float comboChargeTime;
    private Vector2 comboHitboxSize;
    private Vector2 comboHitboxOffset;

    private bool wasPlayerAttacking;
    private bool playerAttackStarted;

    private ElementKind activeElement = ElementKind.None;
    private SpriteRenderer activePhantomRenderer;
    private SpriteRenderer[] activePhantomRenderers;
    private bool activePhantomIsRig;

    private bool soilWaveBodyStarted;
    private bool soilWaveFired;
    private bool soilMeleeHitDone;
    private int waterSproutSpawnedCount;
    private float waterSproutCenterX;
    private bool waterSproutAscending;
    private bool fireRushStarted;
    private bool fireRushHitDone;
    private bool fireRushLavaFired;
    private Tween fireRushTween;

    private bool phantomAnimatorHeld;
    private Coroutine phantomHoldRoutine;

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
                    .Sequence("WaterSprout")
                        .Do("CanWaterSprout", CanUseWaterSprout)
                        .Do("UseWaterSprout", WaterSproutPattern)
                    .End()
                    .Sequence("FlyingSword")
                        .Do("CanFlyingSword", () => PatternStarter(3, flyingSwordRange))
                        .Do("UseFlyingSword", FlyingSwordPattern)
                    .End()
                    .Sequence("FireRush")
                        .Do("CanFireRush", () => PatternStarter(4, fireRushRange))
                        .Do("UseFireRush", FireRushPattern)
                    .End()
                    .Sequence("PlayerCombo")
                        .Do("CanPlayerCombo", CanUsePlayerCombo)
                        .Do("UsePlayerCombo", PlayerComboPattern)
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
            waterSproutStartDelay,
            flyingSwordStartDelay,
            fireRushStartDelay,
            iaiStartDelay,
            playerComboStartDelay
        };

        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        CacheAnimatorParams();
        CacheAttackStateHashes();
        transform.localRotation = Quaternion.identity;

        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerCombo = player.GetComponent<ComboAttack>();
            if (playerCombo == null) playerCombo = player.GetComponentInChildren<ComboAttack>(true);
            playerMovement = player.GetComponent<Playermovement>();
            if (playerMovement == null) playerMovement = player.GetComponentInChildren<Playermovement>(true);
            playerSkills = player.GetComponent<Skills>();
            if (playerSkills == null) playerSkills = player.GetComponentInChildren<Skills>(true);
            playerKnockBack = ResolvePlayerKnockBack();
        }

        bodyCollider = boxColliders.Count > 0 ? boxColliders[0] : GetComponent<BoxCollider2D>();
        if (normalHurtbox == null) normalHurtbox = bodyCollider;
        BuildStandPlatform();

        SnapToGround();
        if (!snappedToGround) StartCoroutine(SnapToGroundWhenReady());

        ClearIaiVisual();
        HidePhantom();
    }

    private System.Collections.IEnumerator SnapToGroundWhenReady()
    {
        for (int i = 0; i < 30 && !snappedToGround; i++)
        {
            yield return null;
            SnapToGround();
        }
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
        SetAnimFloat("Speed", movedThisFrame ? CurrentMoveSpeed() : 0f);
        EnsureLocomotionState();
    }

    private float CurrentMoveSpeed()
    {
        return comboApproaching ? playerComboApproachSpeed : moveSpeed;
    }

    private void EnsureLocomotionState()
    {
        if (!forceLocomotionState) return;
        if (IsDead || GroggyActive) return;
        if (isPatternSetup || isCounterAttacking) return;
        if (animator == null || animator.runtimeAnimatorController == null) return;
        if (animator.IsInTransition(0)) return;

        string target = movedThisFrame ? runStateName : idleStateName;
        if (string.IsNullOrEmpty(target)) return;

        int stateHash = Animator.StringToHash(target);
        if (!animator.HasState(0, stateHash)) return;
        if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash == stateHash) return;

        animator.Play(stateHash, 0, 0f);
    }

    private void SetAnimatorPlayback(bool playing)
    {
        if (animator != null) animator.speed = playing ? 1f : 0f;

        GameObject phantom = PhantomFor(activeElement);
        if (phantom == null) return;

        var phantomAnimator = phantom.GetComponentInChildren<Animator>(true);
        if (phantomAnimator != null) phantomAnimator.speed = playing && !phantomAnimatorHeld ? 1f : 0f;

        var phantomAnimation = phantom.GetComponentInChildren<Animation>(true);
        if (phantomAnimation != null) phantomAnimation.enabled = playing;
    }

    private void OnEnable()
    {
        SetAnimatorPlayback(!PauseManager.IsPaused && !DialogueManager.IsDialogueActive);
    }

    private void PlayPhantomState(string stateName, bool playOnce = false)
    {
        GameObject phantom = PhantomFor(activeElement);
        if (phantom == null || string.IsNullOrEmpty(stateName)) return;

        StopPhantomHoldRoutine();

        var legacyAnim = phantom.GetComponentInChildren<Animation>(true);
        if (legacyAnim != null)
        {
            if (legacyAnim.GetClip(stateName) == null) return;

            AnimationState state = legacyAnim[stateName];
            if (state != null)
            {
                state.wrapMode = playOnce ? WrapMode.ClampForever : WrapMode.Loop;
                state.time = 0f;
            }
            legacyAnim.CrossFade(stateName, 0.1f);
            return;
        }

        var phantomAnimator = phantom.GetComponentInChildren<Animator>(true);
        if (phantomAnimator == null) return;
        if (!phantomAnimator.HasState(0, Animator.StringToHash(stateName))) return;

        phantomAnimatorHeld = false;
        phantomAnimator.speed = PauseManager.IsPaused || DialogueManager.IsDialogueActive ? 0f : 1f;
        phantomAnimator.Play(stateName, 0, 0f);
        if (playOnce) phantomHoldRoutine = StartCoroutine(HoldPhantomAnimator(phantomAnimator, stateName));
    }

    private System.Collections.IEnumerator HoldPhantomAnimator(Animator target, string stateName)
    {
        int stateHash = Animator.StringToHash(stateName);
        yield return null;

        while (target != null && target.gameObject.activeInHierarchy)
        {
            if (PauseManager.IsPaused || DialogueManager.IsDialogueActive)
            {
                yield return null;
                continue;
            }

            AnimatorStateInfo info = target.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash != stateHash) yield break;
            if (info.normalizedTime >= 1f)
            {
                phantomAnimatorHeld = true;
                target.speed = 0f;
                yield break;
            }
            yield return null;
        }
    }

    private void StopPhantomHoldRoutine()
    {
        if (phantomHoldRoutine != null) StopCoroutine(phantomHoldRoutine);
        phantomHoldRoutine = null;
    }

    private void ReleasePhantomAnimatorHold()
    {
        StopPhantomHoldRoutine();
        phantomAnimatorHeld = false;

        ReleasePhantomAnimatorHold(soilPhantom);
        ReleasePhantomAnimatorHold(waterPhantom);
        ReleasePhantomAnimatorHold(firePhantom);
    }

    private static void ReleasePhantomAnimatorHold(GameObject phantom)
    {
        if (phantom == null) return;

        var phantomAnimator = phantom.GetComponentInChildren<Animator>(true);
        if (phantomAnimator != null) phantomAnimator.speed = 1f;
    }

    private void OnDisable()
    {
        SetAnimatorPlayback(false);
        CancelIai();
        CancelCounter();
        CancelPlayerCombo();
        KillFireRushTween();
        HideSwordEffect();
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
        if (reflectedSwordDamage > 0f)
        {
            base.DoDamage(reflectedSwordDamage);
            Debug.Log($"<color=#00FFFF>[최종보스] 쳐낸 어검 명중 — 체력 {reflectedSwordDamage} 감소 (남은 체력 {Hp})</color>");
        }

        if (IsDead) return;
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

    private PlayerKnockBack ResolvePlayerKnockBack()
    {
        if (player == null) return null;

        PlayerKnockBack found = player.GetComponent<PlayerKnockBack>();
        if (found == null) found = player.GetComponentInChildren<PlayerKnockBack>(true);
        if (found == null) found = player.GetComponentInParent<PlayerKnockBack>();
        return found;
    }

    private void DamagePlayer(float damage, float knockBackForce)
    {
        if (player == null) return;

        if (playerKnockBack == null) playerKnockBack = ResolvePlayerKnockBack();
        if (playerKnockBack == null)
        {
            Debug.LogError($"[최종보스] '{player.name}' 에서 PlayerKnockBack 을 찾지 못했습니다. 넉백·무적 점멸이 적용되지 않아 피해를 건너뜁니다.", this);
            return;
        }

        playerKnockBack.TakeHit(transform.position, knockBackForce, Mathf.RoundToInt(damage));
    }

    private void BuildStandPlatform()
    {
        int bossLayer = gameObject.layer;
        if (hurtboxLayer >= 0 && hurtboxLayer < 32) gameObject.layer = hurtboxLayer;
        if (!enableStandPlatform) return;

        Transform existing = transform.Find(StandPlatformName);
        GameObject host;
        if (existing != null)
        {
            host = existing.gameObject;
        }
        else
        {
            host = new GameObject(StandPlatformName);
            host.transform.SetParent(transform, false);
            host.transform.localPosition = Vector3.zero;
            host.transform.localRotation = Quaternion.identity;
            host.transform.localScale = Vector3.one;
        }

        host.layer = standPlatformLayer >= 0 && standPlatformLayer < 32 ? standPlatformLayer : bossLayer;

        standPlatform = host.GetComponent<BoxCollider2D>();
        if (standPlatform == null) standPlatform = host.AddComponent<BoxCollider2D>();
        standPlatform.isTrigger = false;
        standPlatform.enabled = true;

        var source = normalHurtbox as BoxCollider2D;
        if (standPlatformFollowsHurtbox && source != null)
        {
            standPlatform.size = source.size;
            standPlatform.offset = source.offset;
            return;
        }

        standPlatform.size = standPlatformSize;
        standPlatform.offset = standPlatformOffset;
    }

    private void EnsureSwordEffect()
    {
        if (swordEffectReady) return;
        swordEffectReady = true;

        if (!enableSwordEffect) return;
        if (player == null || playerCombo == null || playerCombo.attackEffect == null) return;

        GameObject source = playerCombo.attackEffect.gameObject;
        float playerScale = Mathf.Abs(player.transform.lossyScale.y);
        if (playerScale < 0.0001f) playerScale = 1f;

        GameObject clone = Instantiate(source, transform);
        clone.name = SwordEffectName;
        StripSwordEffectLogic(clone);

        swordEffectBaseLocalPosition = (source.transform.position - player.transform.position) / playerScale;
        clone.transform.localRotation = Quaternion.identity;
        clone.transform.localScale =
            source.transform.lossyScale / playerScale * Mathf.Max(0.01f, swordEffectScaleMultiplier);
        clone.transform.localPosition = swordEffectBaseLocalPosition;
        clone.SetActive(true);

        swordEffect = clone.GetComponent<Attack_animation>();
        if (swordEffect != null) swordEffect.HideEffect();
    }

    private static void StripSwordEffectLogic(GameObject clone)
    {
        foreach (var hitbox in clone.GetComponentsInChildren<Attackhitbox>(true)) Destroy(hitbox);
        foreach (var col in clone.GetComponentsInChildren<Collider2D>(true)) Destroy(col);
    }

    private void PlaySwordEffect(int attackIndex)
    {
        if (!enableSwordEffect) return;

        EnsureSwordEffect();
        if (swordEffect == null) return;

        float direction = isFacingRight ? 1f : -1f;
        Vector3 local = swordEffectBaseLocalPosition;
        local.x = Mathf.Abs(local.x) * direction;
        swordEffect.transform.localPosition = local;

        float duration = Mathf.Max(0.01f, swordEffectDuration);
        swordEffect.PlayEffect(Mathf.Clamp(attackIndex, 1, 3), direction, duration);

        if (swordEffectHideTween != null && swordEffectHideTween.IsActive()) swordEffectHideTween.Kill();
        swordEffectHideTween = DOVirtual.DelayedCall(duration, HideSwordEffect);
    }

    private void HideSwordEffect()
    {
        if (swordEffectHideTween != null && swordEffectHideTween.IsActive()) swordEffectHideTween.Kill();
        swordEffectHideTween = null;
        if (swordEffect != null) swordEffect.HideEffect();
    }

    protected override bool IsHitFlashRenderer(SpriteRenderer renderer)
    {
        if (renderer == null) return false;
        if (IsUnderPhantom(renderer.transform)) return false;
        if (iaiDarkOverlay != null && renderer == iaiDarkOverlay) return false;
        if (iaiFlashEffect != null && renderer.transform.IsChildOf(iaiFlashEffect.transform)) return false;
        if (swordEffect != null && renderer.transform.IsChildOf(swordEffect.transform)) return false;
        return true;
    }

    private bool IsUnderPhantom(Transform target)
    {
        if (soilPhantom != null && target.IsChildOf(soilPhantom.transform)) return true;
        if (waterPhantom != null && target.IsChildOf(waterPhantom.transform)) return true;
        if (firePhantom != null && target.IsChildOf(firePhantom.transform)) return true;
        return false;
    }

    private void CacheAttackStateHashes()
    {
        attackStateHashes = new int[4];
        for (int i = 1; i <= 3; i++) attackStateHashes[i] = Animator.StringToHash("Attack" + i);
    }

    private bool AttackAnimNormalizedTime(int attackIndex, out float normalized)
    {
        normalized = 0f;
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        if (attackStateHashes == null || attackIndex < 1 || attackIndex >= attackStateHashes.Length) return false;

        int stateHash = attackStateHashes[attackIndex];

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            if (next.shortNameHash != stateHash) return false;
            normalized = next.normalizedTime;
            return true;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (current.shortNameHash != stateHash) return false;

        normalized = current.normalizedTime;
        return true;
    }

    private bool CounterHitReady()
    {
        if (hitFollowsAttackAnimation && AttackAnimNormalizedTime(1, out float normalized))
        {
            counterAnimSeen = true;
            return normalized >= counterHitNormalizedTime;
        }

        if (counterAnimSeen) return true;
        return patternElapsed >= counterHitDelay;
    }

    private void OnParrySuccess(string label)
    {
        isPatternSetup = false;
        patternElapsed = 0f;
        if (curTimes != null) curTimes[0] = 0f;
        if (enableGroggy) groggyTime = parryGroggyTime;
        BossSound.Play(ParrySuccessSound, parrySoundVolume);
        Debug.Log($"<color=#00FF88>[최종보스] {label} 쳐내기 성공! (그로기 {(enableGroggy ? parryGroggyTime : 0f)}초)</color>");
    }

    private TaskStatus Dead()
    {
        if (!IsDead) return TaskStatus.Failure;

        CancelIai();
        CancelCounter();
        CancelPlayerCombo();
        KillFireRushTween();
        HideSwordEffect();
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

    private TaskStatus CanUseWaterSprout()
    {
        if (!enableWaterSprout) return TaskStatus.Failure;
        return PatternStarter(2, waterSproutRange);
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
            soilMeleeHitDone = false;
        }

        if (!soilWaveBodyStarted && patternElapsed >= phantomLeadTime)
        {
            soilWaveBodyStarted = true;
            SetAnimTrigger("SoilWave");
            PlayPhantomState("SoilPattern1", true);
        }

        if (soilWaveBodyStarted && !soilMeleeHitDone
            && patternElapsed >= phantomLeadTime + soilMeleeStart
            && patternElapsed <= phantomLeadTime + soilMeleeEnd)
        {
            TrySoilMeleeHit();
        }

        if (!soilWaveFired && patternElapsed >= phantomLeadTime + soilWaveSpawnDelay)
        {
            soilWaveFired = true;
            LaunchSoilWave();
        }

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        EndElementalPattern();
        return TaskStatus.Success;
    }

    private void TrySoilMeleeHit()
    {
        if (player == null) return;

        float dir = isFacingRight ? 1f : -1f;
        Vector2 center = (Vector2)transform.position + new Vector2(soilMeleeOffset.x * dir, soilMeleeOffset.y);
        Vector2 delta = (Vector2)player.transform.position - center;
        if (Mathf.Abs(delta.x) > soilMeleeSize.x * 0.5f) return;
        if (Mathf.Abs(delta.y) > soilMeleeSize.y * 0.5f) return;

        soilMeleeHitDone = true;
        DamagePlayer(soilMeleeDamage, soilMeleeKnockBackForce);
    }

    private void LaunchSoilWave()
    {
        if (PoolManager.Instance == null) return;
        if (string.IsNullOrEmpty(soilWavePoolKey)) return;

        float dir = isFacingRight ? 1f : -1f;
        Vector3 spawnPos = transform.position + new Vector3(soilWaveSpawnOffset.x * dir, soilWaveSpawnOffset.y, 0f);
        BossSound.Play(SoilSmashSound, soilWaveSoundVolume);

        GameObject wave = PoolManager.Instance.Get(soilWavePoolKey, spawnPos, Quaternion.identity);
        if (wave == null) return;

        if (wave.TryGetComponent(out SoilWave soilWave)) soilWave.Launch(dir);
    }

    private TaskStatus WaterSproutPattern()
    {
        if (IsDead || GroggyActive || !enableWaterSprout)
        {
            HidePhantom();
            isPatternSetup = false;
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            BeginElementalPattern(ElementKind.Water, WaterSproutBodyTime(), 2, waterSproutCooldown);
            waterSproutSpawnedCount = 0;
        }

        while (waterSproutSpawnedCount < waterSproutCount
            && patternElapsed >= phantomLeadTime + waterSproutSpawnedCount * Mathf.Max(0f, waterSproutSpawnInterval))
        {
            if (waterSproutSpawnedCount == 0)
            {
                SetAnimTrigger("WaterSprout");
                waterSproutCenterX = player != null ? player.transform.position.x : transform.position.x;
                waterSproutAscending = transform.position.x <= waterSproutCenterX;
            }

            SpawnWaterSprout(waterSproutSpawnedCount);
            waterSproutSpawnedCount++;
        }

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        EndElementalPattern();
        return TaskStatus.Success;
    }

    private float WaterSproutBodyTime()
    {
        float spawnWindow = Mathf.Max(0, waterSproutCount - 1) * Mathf.Max(0f, waterSproutSpawnInterval);
        return Mathf.Max(waterSproutDuration, spawnWindow + waterSproutWarnTime);
    }

    private void SpawnWaterSprout(int order)
    {
        if (PoolManager.Instance == null) return;
        if (string.IsNullOrEmpty(waterSproutPoolKey)) return;

        int slot = waterSproutAscending ? order : waterSproutCount - 1 - order;
        float half = (waterSproutCount - 1) * 0.5f;
        float x = waterSproutCenterX + (slot - half) * waterSproutSpacing;

        GameObject obj = PoolManager.Instance.Get(waterSproutPoolKey, WaterSproutSpawnPosition(x), Quaternion.identity);
        if (obj == null) return;

        if (!obj.TryGetComponent(out Water_Sprout sprout)) return;
        sprout.Configure(waterSproutWarnTime, waterSproutDamage);
        sprout.SetSortingOrder(waterSproutSortingOrder);
        sprout.SetPathPreviewVisible(waterSproutShowPathPreview);
        sprout.SetHitboxGizmoVisible(waterSproutShowHitboxGizmo);
        sprout.SetTargetWidth(waterSproutWidth);
        sprout.SetTargetLength(waterSproutHeight);
        sprout.Launch(Vector2.up);
    }

    private Vector3 WaterSproutSpawnPosition(float x)
    {
        float probeY = player != null ? player.transform.position.y : transform.position.y;
        if (GroundHeightAt(x, probeY, out float groundY)) return new Vector3(x, groundY, 0f);
        return new Vector3(x, GroundProbeBounds().min.y, 0f);
    }

    private bool GroundHeightAt(float x, float probeY, out float groundY)
    {
        RaycastHit2D hit = GroundRayCast(new Vector2(x, probeY + 1f), groundProbeDistance);
        if (hit.collider != null)
        {
            groundY = hit.point.y;
            return true;
        }

        groundY = 0f;
        return false;
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

                BossSound.Play(SwingSwordSound, swingSoundVolume);

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
            fireRushLavaFired = false;
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

        if (fireRushStarted && !fireRushLavaFired && patternElapsed >= rushStart + fireRushDuration)
        {
            fireRushLavaFired = true;
            KillFireRushTween();
            BossSound.Play(BossSound.PickVariant(FireRushHitSound, FireRushHitSound2), fireRushSoundVolume);
            SpawnFireRushLava();
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

    private Vector2 FireRushLavaLandPoint()
    {
        float bossX = transform.position.x;
        float probeY = transform.position.y;

        float minX = bossX - fireRushLavaSpreadX;
        float maxX = bossX + fireRushLavaSpreadX;

        if (fireRushLavaWallMask.value != 0)
        {
            Vector2 origin = GroundProbeBounds().center;
            RaycastHit2D leftWall = WallCast(origin, Vector2.left, fireRushLavaSpreadX);
            if (leftWall.collider != null) minX = Mathf.Max(minX, leftWall.point.x + fireRushLavaWallMargin);

            RaycastHit2D rightWall = WallCast(origin, Vector2.right, fireRushLavaSpreadX);
            if (rightWall.collider != null) maxX = Mathf.Min(maxX, rightWall.point.x - fireRushLavaWallMargin);
        }

        if (maxX < minX)
        {
            minX = bossX;
            maxX = bossX;
        }

        int retries = Mathf.Max(1, fireRushLavaGroundRetries);
        for (int i = 0; i < retries; i++)
        {
            float x = Random.Range(minX, maxX);
            if (GroundHeightAt(x, probeY, out float groundY)) return new Vector2(x, groundY);
        }

        if (GroundHeightAt(bossX, probeY, out float bossGroundY)) return new Vector2(bossX, bossGroundY);
        return new Vector2(bossX, GroundProbeBounds().min.y);
    }

    private void SpawnFireRushLava()
    {
        if (PoolManager.Instance == null) return;
        if (string.IsNullOrEmpty(fireRushLavaPoolKey)) return;

        for (int i = 0; i < fireRushLavaCount; i++)
        {
            GameObject obj = PoolManager.Instance.Get(fireRushLavaPoolKey, transform.position, Quaternion.identity);
            if (obj == null) return;

            if (obj.TryGetComponent(out Lava lava))
            {
                lava.SetHardenOnLand(lavaHardenTime, lavaHardenedMaxCount, lavaHardenedColor);
                lava.LaunchTo(FireRushLavaLandPoint(), fireRushLavaFlightTime);
            }

            PoolManager.Instance.Release(obj, fireRushLavaLifeTime);
        }
    }

    private void TryFireRushHit()
    {
        if (player == null) return;
        if (Mathf.Abs(player.transform.position.x - transform.position.x) > fireRushHitWidth) return;
        if (Mathf.Abs(player.transform.position.y - transform.position.y) > fireRushHitHeight) return;

        fireRushHitDone = true;
        DamagePlayer(fireRushDamage, fireRushKnockBackForce);
    }

    private void KillFireRushTween()
    {
        if (fireRushTween != null && fireRushTween.IsActive()) fireRushTween.Kill();
        fireRushTween = null;
    }

    private TaskStatus CanUsePlayerCombo()
    {
        if (!enablePlayerCombo) return TaskStatus.Failure;
        return PatternStarter(6, playerComboRange);
    }

    private TaskStatus PlayerComboPattern()
    {
        if (IsDead || GroggyActive || !enablePlayerCombo || player == null)
        {
            CancelPlayerCombo();
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            curTimes[0] = 0f;
            curTimes[6] = playerComboCooldown;
            patternElapsed = 0f;
            isPatternSetup = true;
            comboApproaching = true;
            comboStep = 0;
            comboStepElapsed = 0f;
            comboStepHitDone = false;
            comboAnimSeen = false;
            CachePlayerComboProfile();
            SetAnimBool("IsIdle", false);
            FacePlayer();
            BossSound.Play(DashSound, dashSoundVolume);
        }

        if (comboApproaching) return RunPlayerComboApproach();
        return RunPlayerComboStep();
    }

    private TaskStatus RunPlayerComboApproach()
    {
        float distance = HorizontalDistance;
        if (distance <= playerComboApproachRange || patternElapsed >= playerComboApproachTimeout)
        {
            comboApproaching = false;
            SetAnimBool("IsMoving", false);
            BeginPlayerComboStep(1);
            return TaskStatus.Continue;
        }

        movedThisFrame = true;
        SetAnimBool("IsMoving", true);
        float direction = Mathf.Sign(player.transform.position.x - transform.position.x);
        Face(direction);
        transform.Translate(Vector3.right * (direction * playerComboApproachSpeed * Time.deltaTime), Space.World);
        return TaskStatus.Continue;
    }

    private void BeginPlayerComboStep(int step)
    {
        comboStep = step;
        comboStepElapsed = 0f;
        comboStepHitDone = false;
        comboAnimSeen = false;
        FacePlayer();
        BossSound.Play(SwingMeleeSound, swingSoundVolume);
        PlayAttackAnim(step);
    }

    private TaskStatus RunPlayerComboStep()
    {
        comboStepElapsed += Time.deltaTime;
        float duration = PlayerComboStepDuration(comboStep);
        bool hitReady = !comboStepHitDone && PlayerComboHitReady(duration);

        if (playerComboParryable && !comboStepHitDone && !hitReady
            && CheckPlayerParry(playerComboParryRange))
        {
            CancelPlayerCombo();
            OnParrySuccess("돌진 콤보");
            return TaskStatus.Failure;
        }

        if (hitReady)
        {
            comboStepHitDone = true;
            ApplyPlayerComboHit(comboStep);
        }

        if (comboStepElapsed < duration)
        {
            ApplyPlayerComboLunge(duration);
            return TaskStatus.Continue;
        }

        if (!comboStepHitDone)
        {
            comboStepHitDone = true;
            ApplyPlayerComboHit(comboStep);
        }

        float tail = comboStep >= 3 ? comboChargeTime + playerComboRecoverTime : comboGapTime;
        if (comboStepElapsed < duration + tail) return TaskStatus.Continue;

        if (comboStep >= 3)
        {
            CancelPlayerCombo();
            return TaskStatus.Success;
        }

        BeginPlayerComboStep(comboStep + 1);
        return TaskStatus.Continue;
    }

    private bool PlayerComboHitReady(float duration)
    {
        if (hitFollowsAttackAnimation && AttackAnimNormalizedTime(comboStep, out float normalized))
        {
            comboAnimSeen = true;
            return normalized >= playerComboHitNormalizedTime;
        }

        if (comboAnimSeen) return true;
        return comboStepElapsed >= duration * playerComboHitNormalizedTime;
    }

    private float PlayerComboStepDuration(int step)
    {
        int index = Mathf.Clamp(step - 1, 0, comboStepDurations.Length - 1);
        return Mathf.Max(0.01f, comboStepDurations[index]);
    }

    private float PlayerComboDamage(int step)
    {
        switch (step)
        {
            case 1: return playerComboDamage1;
            case 2: return playerComboDamage2;
            default: return playerComboDamage3;
        }
    }

    private float PlayerComboLungeDistance(int step)
    {
        switch (step)
        {
            case 1: return comboLungeDistances.x;
            case 2: return comboLungeDistances.y;
            default: return comboLungeDistances.z;
        }
    }

    private void ApplyPlayerComboLunge(float duration)
    {
        float distance = PlayerComboLungeDistance(comboStep);
        if (distance <= 0f) return;

        float direction = isFacingRight ? 1f : -1f;
        transform.Translate(Vector3.right * (direction * (distance / duration) * Time.deltaTime), Space.World);
    }

    private void ApplyPlayerComboHit(int step)
    {
        if (player == null) return;

        float direction = isFacingRight ? 1f : -1f;
        Vector2 center = (Vector2)transform.position
            + new Vector2(comboHitboxOffset.x * direction, comboHitboxOffset.y);
        Vector2 delta = (Vector2)player.transform.position - center;
        if (Mathf.Abs(delta.x) > comboHitboxSize.x * 0.5f) return;
        if (Mathf.Abs(delta.y) > comboHitboxSize.y * 0.5f) return;

        float damage = PlayerComboDamage(step);
        DamagePlayer(damage, playerComboKnockBackForce);
    }

    private void CachePlayerComboProfile()
    {
        comboStepDurations[0] = playerComboFallbackDurations.x;
        comboStepDurations[1] = playerComboFallbackDurations.y;
        comboStepDurations[2] = playerComboFallbackDurations.z;
        comboGapTime = playerComboFallbackGap;
        comboChargeTime = playerComboFallbackCharge;
        comboHitboxSize = playerComboFallbackHitboxSize;
        comboHitboxOffset = playerComboFallbackHitboxOffset;
        comboLungeDistances = Vector3.zero;

        if (!playerComboCopyPlayerProfile || playerCombo == null) return;

        float attackSpeed = Mathf.Max(1f, playerSkills != null
            ? playerSkills.skill_3_attackSpeedMultiplier
            : playerComboFallbackAttackSpeed);

        comboStepDurations[0] = Mathf.Max(0.01f, playerCombo.attack1Duration / attackSpeed);
        comboStepDurations[1] = Mathf.Max(0.01f, playerCombo.attack2Duration / attackSpeed);
        comboStepDurations[2] = Mathf.Max(0.01f, playerCombo.attack3Duration / attackSpeed);
        comboGapTime = Mathf.Max(0f, playerCombo.comboDelay);
        comboChargeTime = Mathf.Max(0f, playerCombo.attack3ChargeTime);

        float ratio = PlayerScaleRatio();
        comboLungeDistances = new Vector3(
            playerCombo.attack1Distance,
            playerCombo.attack2Distance,
            playerCombo.attack3Distance) * (ratio * Mathf.Max(0f, playerComboLungeScale));

        BoxCollider2D box = playerCombo.attackCollider;
        if (box == null) return;

        Vector3 boxScale = box.transform.lossyScale;
        comboHitboxSize = new Vector2(
            Mathf.Abs(box.size.x * boxScale.x),
            Mathf.Abs(box.size.y * boxScale.y)) * ratio;

        Vector3 fromPlayer = box.transform.TransformPoint(box.offset) - player.transform.position;
        comboHitboxOffset = new Vector2(Mathf.Abs(fromPlayer.x) * ratio, fromPlayer.y * ratio);
    }

    private float PlayerScaleRatio()
    {
        if (player == null) return 1f;

        float playerScale = Mathf.Abs(player.transform.lossyScale.y);
        if (playerScale < 0.0001f) return 1f;
        return Mathf.Abs(transform.lossyScale.y) / playerScale;
    }

    private void CancelPlayerCombo()
    {
        if (comboApproaching || comboStep > 0)
        {
            if (curTimes != null) curTimes[0] = 0f;
            isPatternSetup = false;
        }

        comboApproaching = false;
        comboStep = 0;
        comboStepElapsed = 0f;
        comboStepHitDone = false;
        comboAnimSeen = false;
        SetAnimBool("IsMoving", false);
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
            BossSound.Play(DrawSound, swingSoundVolume);
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

        BossSound.Play(ScreenSlashSound, swingSoundVolume);
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

        DamagePlayer(iaiDamage, iaiKnockBackForce);
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
            counterAnimSeen = false;
            SetAnimBool("IsMoving", false);
            SetAnimBool("IsIdle", false);
            SetAnimTrigger("CounterAttack");
            BossSound.Play(SwingMeleeSound, swingSoundVolume);
            FacePlayer();
        }

        bool hitReady = !counterHitDone && CounterHitReady();

        if (!counterHitDone && !hitReady
            && patternElapsed >= counterParryStart
            && CheckPlayerParry(counterParryRange))
        {
            CancelCounter();
            OnParrySuccess("반격");
            return TaskStatus.Failure;
        }

        if (hitReady)
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

        DamagePlayer(counterDamage, counterKnockBackForce);
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
        ReleasePhantomAnimatorHold();
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

    private bool IsOwnCollider(Collider2D col)
    {
        return col != null && col.transform.IsChildOf(transform);
    }

    private RaycastHit2D WallCast(Vector2 origin, Vector2 direction, float distance)
    {
        foreach (var castHit in Physics2D.RaycastAll(origin, direction, distance, fireRushLavaWallMask))
        {
            if (!IsOwnCollider(castHit.collider)) return castHit;
        }
        return default;
    }

    private RaycastHit2D GroundRayCast(Vector2 origin, float distance)
    {
        foreach (var castHit in Physics2D.RaycastAll(origin, Vector2.down, distance, groundMask))
        {
            if (!IsOwnCollider(castHit.collider)) return castHit;
        }
        return default;
    }

    private RaycastHit2D GroundCast(Vector2 origin, Vector2 size, float distance)
    {
        foreach (var castHit in Physics2D.BoxCastAll(origin, size, 0f, Vector2.down, distance, groundMask))
        {
            if (!IsOwnCollider(castHit.collider)) return castHit;
        }
        return default;
    }

    private Collider2D GroundOverlap(Vector2 center, Vector2 size)
    {
        foreach (var col in Physics2D.OverlapBoxAll(center, size, 0f, groundMask))
        {
            if (!IsOwnCollider(col)) return col;
        }
        return null;
    }

    private void SnapToGround()
    {
        Bounds probe = GroundProbeBounds();
        Vector2 castOrigin = new Vector2(probe.center.x, probe.center.y + 30f);
        RaycastHit2D hit = GroundCast(castOrigin, probe.size, 120f);
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

        Collider2D overlapped = GroundOverlap(bounds.center, bounds.size * 0.98f);
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
        RaycastHit2D hit = GroundCast(bounds.center, bounds.size, castDistance);

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
        PlaySwordEffect(attackIndex);
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
 * BT 셀렉터 우선순위(위→아래) : 토 파동 > 수 물기둥 > 어검 > 화 돌진 > 돌진 콤보 > 거합 > 반격.
 * 그 아래 Move/Idle. 반격은 대기 상태가 아니라 "피격 후 지연 발동" 플래그(isCounterAttacking)로 켜진다.
 *
 * curTimes 인덱스 : [0]=현재 패턴 진행 타이머(공유) [1]=토 파동 [2]=수 물기둥 [3]=어검 [4]=화 돌진
 * [5]=거합 [6]=돌진 콤보. 인덱스는 추가 순서이고 BT 우선순위와 별개다(기존 인덱스를 밀지 않으려고
 * 신규 패턴을 6번에 붙이되 노드는 화 돌진과 거합 사이에 끼웠다).
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
 *   - 반격 타격은 1회(counterHitRange 이내)이며 쳐내기로 캔슬하면 타격 자체가 발생하지 않는다
 *     (금보스의 DelayedCall 방식 대신 채택).
 *
 * 타격 컷 동기화 (hitFollowsAttackAnimation, 기본 켬)
 *   최종보스는 플레이어 리컬러라 플레이어의 RuntimeAnimatorController 를 그대로 쓴다.
 *   반격/돌진 콤보의 데미지는 타이머가 아니라 "지금 재생 중인 Attack{n} 상태의 normalizedTime"이
 *   임계값(counterHitNormalizedTime / playerComboHitNormalizedTime, 기본 0.4)을 넘는 순간 들어간다.
 *   AttackAnimNormalizedTime 은 전환 중이면 GetNextAnimatorStateInfo 를, 아니면
 *   GetCurrentAnimatorStateInfo 를 보고 Attack1~3 의 shortNameHash 와 대조한다.
 *   기본값 0.4 근거 : Attack1~3.anim 은 12fps 5컷(길이 0.41667초)이고 검을 휘두르는 3번째 컷이
 *   0.16667초에 시작한다 → 0.16667 / 0.41667 = 0.4. 기존에 손으로 맞춰 둔 counterHitDelay 0.2초
 *   (≈0.48 정규화)와도 같은 컷 안이다. 실제 컷은 눈으로 보고 인스펙터에서 미세조정하면 된다.
 *   AnimationEvent 를 쓰지 않은 이유 : Attack1~3.anim 은 플레이어와 공유하는 에셋이고
 *   m_Events 가 비어 있다. 이벤트를 심으면 플레이어가 공격할 때마다 없는 핸들러를 찾아
 *   경고가 뜨고 플레이어 공격 경로에 부작용이 생긴다. 그래서 클립은 읽기만 한다.
 *   애니메이터/컨트롤러가 없거나 상태를 못 찾으면 기존 타이머(counterHitDelay / 스텝 길이 비율)로
 *   폴백하고, 상태를 한 번 본 뒤 지나쳐 버린 경우(counterAnimSeen)에는 즉시 타격한다.
 *
 * 반격 쳐내기 창 : counterParryStart ~ "타격 컷 직전". counterParryEnd 는 없앴다 —
 * 매 프레임 타격 준비 여부를 먼저 계산해서, 타격이 성립하는 프레임부터는 패링을 아예 검사하지 않는다.
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
 *             닿으면 NotifyReflectedSwordHit() — 이때 reflectedSwordDamage(기본 10)만큼
 *             base.DoDamage 로 체력이 깎인다. 금보스(Gold.cs)는 같은 통지를 받고도 5회 누적
 *             그로기만 하며 체력은 그대로다 — 통지 메서드가 보스별로 따로 있으므로 금보스 동작은
 *             전혀 바뀌지 않는다. FinalBoss 쪽만 데미지를 얹었다.
 *             base.DoDamage 를 쓰는 이유 : this.DoDamage 를 쓰면 ScheduleCounter 가 걸려
 *             "패링 보상"이 곧바로 반격이라는 역보상으로 되돌아온다. 반사 명중은 순수 보상으로 둔다.
 *             중복 타격 가드는 불필요하다 — FlyingSword.UpdateReflected 가 NotifyBossHit 직후
 *             ReleaseSelf() 로 isReflected 를 내리고 풀에 반납하므로 한 검당 통지는 1회뿐이다.
 *   - 돌진 콤보 : playerComboParryable(기본 true). 각 타의 "타격 컷 직전"까지만 가능하다.
 * 속성 패턴(토 파동/수 물기둥/화 돌진)은 패링 판정이 아예 없다.
 * 패링 성공 보상은 "해당 공격 무효화"뿐이며 그로기는 enableGroggy 를 켠 경우에만 붙는다.
 *
 * ─────────────────────────────────────────────────────────────
 * 속성 패턴 3종 + 환영 연출
 * ─────────────────────────────────────────────────────────────
 * 공통 흐름 : 패턴 진입 즉시 해당 속성 보스의 반투명 환영을 띄우고(히트박스도 교체),
 * phantomLeadTime(기본 1초) 후 본체 동작 시작, 패턴이 끝나면 환영을 끄고 히트박스 원복.
 *
 *   토 파동  : phantomLeadTime 시점에 본체 트리거와 환영 애니("SoilPattern1")를 함께 시작하고,
 *              애니 시작 기준 soilMeleeStart~End(원본 토보스 0.6~0.9) 창에서 전방 사각 근접 판정
 *              (soilMeleeOffset/Size — Boss_Soil 씬 HIt1 콜라이더 값 시딩, 데미지 50) 1회,
 *              soilWaveSpawnDelay(원본 0.7) 시점에 SoilWave 투사체(풀 키 soilWavePoolKey) 1발 발사
 *              — 내리치는 모션에 근접 타격과 장풍이 원본 타임라인대로 맞물린다.
 *              투사체는 플레이어 스킬2 지형(SkillGroundMarker)에 닿으면 소멸하고,
 *              투사체 수치는 SoilWave 프리팹 쪽 SerializeField 가 담당.
 *   수 물기둥(신) : waterSproutCount(기본 5)개를 waterSproutSpawnInterval(기본 0.25초) 간격으로
 *              "보스에서 먼 쪽이 아니라 가까운 쪽부터" 순차 생성한다 — 첫 기둥 시점에 플레이어 x 를
 *              중심(waterSproutCenterX)으로 고정하고, 보스가 그 중심의 왼쪽이면 왼→오른(오름차순),
 *              오른쪽이면 오른→왼(내림차순) 순서로 슬롯을 채운다(waterSproutAscending).
 *              보스 쪽에서 시작해 플레이어를 지나 반대편으로 훑는 파도라서 (a) 다가오는 방향이 눈에
 *              보이고 (b) 제자리에 서 있으면 맞으므로 이동을 강제하며 (c) 각 기둥이 waterSproutWarnTime
 *              전조를 따로 가져 개별 회피가 가능하다. 패턴 본체 시간은 WaterSproutBodyTime() 이
 *              생성창 + 전조를 덮도록 자동으로 늘린다.
 *              프리팹 전제를 바꾸지 않으려고 sortingOrder(waterSproutSortingOrder, 기본 1),
 *              경로 예고 스프라이트(waterSproutShowPathPreview), 히트박스 기즈모
 *              (waterSproutShowHitboxGizmo)는 전부 소환 직후 Water_Sprout 의 세터로 주입한다.
 *              수보스(Water_Sprout_Zone)는 이 세터를 호출하지 않으므로 원본 동작 그대로다.
 *   수 물기둥(구 설명): 수보스 물기둥(Water_Sprout, 풀 키 waterSproutPoolKey="Water_Pump" — 프리팹 이름)
 *              을 재사용한다. phantomLeadTime(수 환영 Eye_3 예고 1초) 후 플레이어 현재 x 를 중심으로
 *              waterSproutCount 개를 waterSproutSpacing 간격으로 지면(groundMask 레이캐스트) 위에
 *              소환 — 각 기둥은 원본과 동일하게 스프라이트 전조(warnTime=원본 startDelay 1초) 후
 *              분출하며 PlayerKnockBack.TakeHit 로 피해를 준다(Configure 로 전조/데미지 덮어씀).
 *              기둥은 수명이 끝나면 스스로 Destroy 되므로 풀 반납은 하지 않는다(매번 새 인스턴스).
 *              enableWaterSprout(기본 true)를 끄면 노드가 스킵되는 것은 구 잠식 슬롯과 동일.
 *   화 돌진  : 화보스 패턴2 돌진의 지상판. windup 동안 조준 → DOMoveX(InCubic)로 지상 돌진
 *              (y 는 ApplyGravity 가 유지) → 돌진 구간 동안 사각 판정(fireRushHitWidth/Height)
 *              1회 접촉 피해 → recover. 트윈은 사망/비활성/그로기 시 Kill.
 *              돌진이 끝나 멈추는 시점(rushStart + fireRushDuration)에 화염구를 fireRushLavaCount
 *              (기본 8)개 한 번에 쏜다 — 화보스 Fire.LavaJet(돌진 종료 직후 Lava 소환)과 같은
 *              타이밍·같은 궤적 공식이다. 착지점은 FireRushLavaLandPoint() 가 화염구마다 따로
 *              정한다 — 보스 x 중심 ±fireRushLavaSpreadX 로 착지 x 를 뽑고 그 x 에서 지면
 *              레이캐스트로 y 를 재서 (x, y) 를 Lava.LaunchTo 에 통째로 넘기며,
 *              fireRushLavaFlightTime(기본 2~3초) 무작위 체공시간에 맞춰 포물선 초기속도를
 *              역산한다(= Lava.OnEnable 의 화보스 공식과 동일).
 *              착지 후에는 원본대로 LavaPool(용암 장판, Hitbox 데미지)이 생성되지만,
 *              Lava.SetHardenOnLand 로 "굳음 모드"가 걸려 있다 — lavaHardenTime(기본 3초) 동안
 *              DOColor 로 lavaHardenedColor(기본 검정)까지 서서히 물들며 그 동안 데미지가 유지되고,
 *              다 굳으면 Hitbox/Collider 가 꺼져 무해해진다. 굳은 덩어리는 반납하지 않고 맵에 남으며
 *              lavaHardenedMaxCount(기본 60)를 넘으면 가장 오래된 것부터 풀에 반납된다.
 *
 * ─────────────────────────────────────────────────────────────
 * 지면 탐색 (GroundRayCast / GroundHeightAt) — "떠 있는 화염구" 버그 수정
 * ─────────────────────────────────────────────────────────────
 * Styx 씬에서 FinalBoss 루트 오브젝트는 layer 6(ground)로 올려져 있고 groundMask 도 layer 6 뿐이다.
 * 그래서 예전 FireRushLavaLandY() 의 raw Physics2D.Raycast(probe.center, down, groundMask) 는
 * 자기 자신의 히트박스 안에서 출발했고, Physics2D.queriesStartInColliders 기본값 true 때문에
 * 거리 0 에서 "자기 콜라이더"에 맞았다 → hit.point.y = 보스 몸통 중심 y 가 착지 높이로 쓰였다.
 * 결과적으로 화염구와 굳은 용암이 지면이 아니라 보스 가슴 높이에 떠서 생겼다.
 * 같은 파일의 GroundCast/GroundOverlap 이 IsOwnCollider 로 자기 콜라이더를 걸러내는 이유가 이것인데,
 * 지면 높이 조회 두 곳(FireRushLavaLandY, WaterSproutSpawnPosition)만 그 규약에서 빠져 있었다.
 * → GroundRayCast(origin, distance) 를 추가해 RaycastAll + IsOwnCollider 필터로 통일했고,
 *   GroundHeightAt(x, probeY, out y) 가 두 경로의 공통 진입점이 된다.
 * 트리거 오염 위험은 없다 : Styx 의 물 콜라이더(ShallowWaterZone, WaterSim 의 PolygonCollider2D)는
 * 둘 다 layer 0 이라 groundMask(layer 6)에 걸리지 않는다. 보스 자신의 트리거 히트박스만이
 * 유일한 오염원이었고 그건 IsOwnCollider 가 막는다. 전역 Physics2D.queriesHitTriggers 는 건드리지 않았다.
 * 벽 회피(WallCast) : Lava 프리팹에는 Rigidbody2D 도 Collider2D 도 없다(GameObject/Transform/
 * SpriteRenderer/Lava 뿐, layer 0). 즉 화염구는 물리로 벽에 "막히는" 것이 아니라 그냥 통과한 뒤
 * 벽 너머/안쪽에 착지해 굳은 용암이 벽에 박혀 보이는 것이다. 화보스가 이 문제를 안 겪는 이유는
 * 물리가 아니라 Lava 의 직렬화 필드 limitX(=7) 로 착지 x 범위를 경기장 안으로 묶어 두기 때문이다.
 * 그래서 같은 개념을 좌표 하드코딩 없이 재현한다 — 착지 x 를 뽑기 전에 보스 좌우로
 * fireRushLavaWallMask(기본 layer 10 = wall) 레이캐스트를 쏴서 벽 안쪽으로
 * fireRushLavaWallMargin 만큼 들어온 지점까지로 [minX, maxX] 를 좁힌 뒤 그 안에서 뽑는다.
 * Styx 의 벽은 'Square (1)'·'Square (2)'(layer 10, 비트리거)이고 보스/발판은 layer 6 이라
 * 이 마스크에 걸리지 않으며, IsOwnCollider 필터가 자기 콜라이더도 한 번 더 막는다.
 * Lava.cs 는 손대지 않았으므로 화보스 경로(OnEnable 의 limitX/groundY 계산)는 그대로다.
 * 지면이 없는 x 폴백 : Styx 바닥은 x ∈ [-10, 10] 짜리 콜라이더 하나뿐이라 보스 위치(x=5)에서
 * spreadX 7 이면 x > 10 구간이 허공이다. 그래서 fireRushLavaGroundRetries(기본 6)회까지 x 를 다시
 * 뽑고, 그래도 실패하면 지면이 있는 것이 확실한 "보스 발밑 x"로 떨어뜨린다 — 클램프 대신 재추첨을
 * 쓴 이유는 전장 경계값을 별도 필드로 들고 있지 않아도 되고, 지면이 끊긴 구간이 여러 개여도 통하기 때문.
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
 * PlayPhantomState(stateName, playOnce) 의 1회 재생 처리 (토 환영 SoilPattern1 이 여기 해당):
 *   - 레거시 Animation(리그형 토 환영) : AnimationState.wrapMode 를 ClampForever 로 덮고 time 을
 *     0 으로 되감은 뒤 CrossFade 한다. wrapMode 는 Animation 컴포넌트가 런타임에 들고 있는
 *     "상태" 값이라 AnimationClip 에셋(SoilPattern1.anim — 원본 토보스와 공유)을 건드리지 않는다.
 *     즉 Soil.cs 원본 보스 동작에는 아무 영향이 없다. 재생이 끝나면 마지막 프레임을 유지하고,
 *     환영이 다시 켜질 때 playAutomatically 의 SoilIdle(Loop)이 같은 레이어를 덮어 원복된다.
 *     playOnce 가 false 면 Loop 로 되돌려 놓으므로 상태가 눌어붙지 않는다.
 *   - Animator(단일 SR 환영 / 토 환영이 폴백 구성일 때) : 클립 에셋의 loop 플래그를 만지지 않고
 *     HoldPhantomAnimator 코루틴이 normalizedTime >= 1 을 감지해 animator.speed = 0 으로 멈춘다.
 *     이때 phantomAnimatorHeld 가 서고, SetAnimatorPlayback 이 이 플래그를 존중해 일시정지/대사
 *     복귀 시에도 다시 돌리지 않는다. HidePhantom(=패턴 종료·사망·비활성)에서 전부 해제된다.
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
 * 돌진 콤보 (플레이어 3단 콤보) — 유일한 "플레이어 유래" 패턴
 * ─────────────────────────────────────────────────────────────
 * 흐름 : 접근 → 1타 → comboGap → 2타 → comboGap → 3타 → charge + recover.
 * 접근은 playerComboApproachSpeed 로 플레이어 쪽으로 이동하다가 수평 거리가
 * playerComboApproachRange 이내가 되면(또는 playerComboApproachTimeout 초과 시) 콤보에 진입한다.
 * 각 타는 PlayAttackAnim(n) 으로 플레이어 공격 모션(AttackIndex n + AttackTrigger)을 그대로 재생하고,
 * 데미지는 playerComboDamage1/2/3(10/15/20)을 PlayerKnockBack.TakeHit 으로 1회 준다.
 * 금 킷이 아니라 플레이어 유래이므로 환영(phantom)을 띄우지 않고 히트박스 교체도 하지 않는다.
 *
 * BT 배치 근거 : 화 돌진과 거합 사이. 접근이 필요한 근접 압박기라서 원거리 견제기(토/수/어검)보다
 * 뒤에 두고, 최종 한 방인 거합(30초)보다는 앞에 둔다. 쿨타임은 화 돌진 15초와 거합 30초 사이인
 * 20초, 첫 발동은 playerComboStartDelay 20초.
 *
 * "플레이어와 같은 값"은 하드코딩하지 않고 CachePlayerComboProfile() 이 패턴 시작 시점에
 * 살아 있는 플레이어 컴포넌트에서 직접 읽는다(playerComboCopyPlayerProfile 로 끌 수 있고,
 * 끄거나 플레이어를 못 찾으면 playerComboFallback* 값이 쓰인다).
 *   - 공격 속도 : ComboAttack.GetAdjustedDuration 은 baseDuration / attackSpeedMultiplier 이고,
 *     이 멀티플라이어를 올리는 유일한 경로가 Skills.Do_skill_3 의
 *     SetAttackSpeedMultiplier(skill_3_attackSpeedMultiplier)(대입이지 누적이 아님)다.
 *     즉 플레이어의 최고 공속 = skill_3_attackSpeedMultiplier(프리팹 1.5).
 *     그래서 각 타 길이 = ComboAttack.attackNDuration / skill_3_attackSpeedMultiplier
 *     (프리팹 0.417 / 1.5 ≈ 0.278초). 타 사이 간격은 ComboAttack.comboDelay,
 *     3타 뒤 경직은 attack3ChargeTime 을 그대로 쓴다(플레이어도 이 둘은 공속 영향을 안 받는다).
 *   - 히트박스 : ComboAttack.attackCollider(플레이어 Attack 자식, BoxCollider2D)의 월드 크기와
 *     플레이어 루트 기준 오프셋을 읽어 PlayerScaleRatio(= 보스 lossyScale.y / 플레이어 lossyScale.y)
 *     를 곱한다. Styx 기준 플레이어 스케일 1 · 보스 스케일 2 → 비율 2,
 *     플레이어 2.5×1 (오프셋 0.09, -0.07) → 보스 5×2 (오프셋 0.18, -0.14).
 *     판정 방식은 TrySoilMeleeHit 과 같은 전방 사각 겹침 검사이고 x 오프셋은 바라보는 방향으로 미러링된다.
 *   - 전진 : 각 타마다 ComboAttack.attackNDistance × 비율 × playerComboLungeScale 만큼 앞으로 민다
 *     (플레이어 DashForward 의 지상판). 0 으로 두면 제자리 공격이 된다.
 * 플레이어 쪽 컴포넌트는 전부 "읽기"만 하므로 ComboAttack/Attackhitbox 의 기존 공격 판정에 영향이 없다.
 *
 * ─────────────────────────────────────────────────────────────
 * 플레이어 피해 경로 일원화 (DamagePlayer)
 * ─────────────────────────────────────────────────────────────
 * 토 근접 / 화 돌진 / 거합 / 반격 / 돌진 콤보 5곳이 전부 DamagePlayer(damage, knockBackForce) 를 쓴다.
 * PlayerKnockBack.TakeHit 이 데미지 + 콤보 취소 + 넉백 + 0.5초 무적 점멸을 모두 처리하는 정식 경로다.
 * 예전에는 PlayerKnockBack 을 못 찾으면 PlayerHealth.TakeDamage 로 조용히 떨어졌는데 그 경로는
 * 넉백·무적·점멸을 통째로 건너뛴다. 지금은 루트→자식→부모 순으로 찾고 없으면 에러 로그를 남기고
 * 피해를 주지 않는다(조용한 실패 금지). 참조는 Awake 에서 1회 캐시하고 null 이면 매번 재해석한다.
 * 부작용 : 모든 피해가 TakeHit 의 0.5초 무적과 대시 중 무적을 따른다.
 *
 * ─────────────────────────────────────────────────────────────
 * 검기 이펙트 (플레이어 Attack_animation 재사용)
 * ─────────────────────────────────────────────────────────────
 * PlayAttackAnim(n) 한 곳에 얹었으므로 검을 휘두르는 모든 공격(반격 1 / 어검 1 / 거합 3 /
 * 돌진 콤보 1·2·3)에서 같은 이펙트가 나온다.
 * 새 에셋을 만들지 않고 EnsureSwordEffect() 가 첫 사용 시 플레이어의 ComboAttack.attackEffect
 * (플레이어 "Attack" 자식의 Attack_animation + Animator + SpriteRenderer) 오브젝트를 통째로
 * Instantiate 해 보스 자식으로 붙인다. 같은 프리팹·같은 애니메이터 컨트롤러라 모양이 동일하다.
 * StripSwordEffectLogic 이 복제본의 Attackhitbox 와 Collider2D 를 제거한다 — 안 지우면 보스에
 * 플레이어용 공격 판정이 달려 다른 보스를 때리거나 combo 참조 null 로 터진다.
 * 비율 : 로컬 오프셋·스케일 모두 "플레이어 기준 월드값 / 플레이어 lossyScale" 로 넣는다.
 * 복제본이 보스(스케일 2)의 자식이므로 최종 월드값은 자동으로 ×2 가 되어
 * PlayerScaleRatio(= 보스 lossyScale.y / 플레이어 lossyScale.y) 배가 정확히 적용된다.
 * Styx 기준 플레이어 (0.09, -0.07)·스케일 1 → 보스 월드 (0.18, -0.14)·스케일 2.
 * x 오프셋은 바라보는 방향으로 미러링하고 facing 을 PlayEffect 에 넘겨 스프라이트도 뒤집는다.
 * 표시 시간은 swordEffectDuration 이며 DOVirtual.DelayedCall 로 HideEffect 를 예약한다
 * (DOTween 이라 일시정지에 함께 멈춘다). 사망/비활성 시 HideSwordEffect 로 정리.
 * 플레이어 원본 오브젝트는 읽기만 하고 복제본만 조작하므로 플레이어 이펙트 동작은 그대로다.
 *
 * ─────────────────────────────────────────────────────────────
 * 이동 애니메이션 (플레이어 컨트롤러 Move_2)
 * ─────────────────────────────────────────────────────────────
 * Playermovement 규약 : SetFloat("Speed", |velocity.x|), SetBool("IsGround", ...),
 * SetFloat("VerticalVelocity", velocity.y). 컨트롤러의 Idle↔Move_2 전이 임계값은 Speed 0.01 이다.
 * UpdateAnimatorMotion 은 이 셋을 같은 규약으로 채우되 Speed 에 실제 이동 속도
 * (일반 추격 = moveSpeed, 돌진 콤보 접근 = playerComboApproachSpeed)를 넣는다.
 * 달리기가 나와야 하는 경로는 "본체가 발로 걷는 이동"뿐이라 movedThisFrame 을 기준으로 삼았다 —
 * BT Move(일반 추격)와 돌진 콤보 접근만 이 플래그를 세우고, 화 돌진(DOMoveX 트윈)은 세우지 않아
 * DashTrigger 로 대시 모션이 유지된다.
 * forceLocomotionState : 파라미터만으로는 공격/피격 상태에서 Idle·Move_2 로 못 돌아오는 경우가
 * 있어(플레이어 컨트롤러의 Afterattack 계열 전이가 IsGround 조건에 묶여 있다) 패턴/반격이 전혀
 * 돌지 않는 자유 이동 구간에 한해 animator.Play(runStateName/idleStateName) 로 상태를 직접 보정한다.
 * 이미 그 상태면 재생하지 않으므로 애니메이션이 매 프레임 리셋되지 않는다.
 * 전이 중(IsInTransition)·사망·그로기·패턴 진행 중에는 아무것도 하지 않아 기존 연출과 충돌하지 않는다.
 *
 * ─────────────────────────────────────────────────────────────
 * 발판 콜라이더 (StandPlatform) — "스킬 중 보스가 뚫리는" 버그
 * ─────────────────────────────────────────────────────────────
 * 원인 : Playermovement 는 태그가 아니라 verticalCollisionMask 레이캐스트로 지면을 판정하고,
 * Styx 에서 보스 루트가 layer 6(ground)이라 보스를 밟을 수 있었다. 그런데 루트의 네 콜라이더 중
 * 실제로 발판 역할을 하던 것은 normalHurtbox 하나(비트리거)뿐이고, ApplyHurtbox 가 속성 패턴 동안
 * 이걸 꺼 버린다. 나머지 셋(soil/water/fireHurtbox)은 전부 isTrigger 이고 크기·오프셋도 제각각
 * (예: soilHurtbox 는 월드 기준 8.8 높이에 중심이 몸 위쪽)이라 발판이 사라지거나 엉뚱한 높이로 튄다.
 * 즉 "히트박스 교체가 발판까지 같이 갈아치운다"가 확정 원인이다.
 * 수정 : Awake 의 BuildStandPlatform() 이 보스 자식 "StandPlatform" 오브젝트를 런타임에 만들고
 * normalHurtbox 와 같은 size/offset 의 비트리거 BoxCollider2D 를 붙인다. 이 콜라이더는
 * ApplyHurtbox 가 절대 건드리지 않으므로 어떤 패턴 중에도 발판이 유지된다(씬/프리팹 편집 없음).
 *   - 자식에 둔 이유 : Attackhitbox 는 other.TryGetComponent<BossBase>() 로 "같은 오브젝트"만
 *     검사한다. 루트에 콜라이더를 하나 더 늘리면 플레이어 공격 1회에 트리거 이벤트가 2번 들어와
 *     데미지가 두 배가 된다. 자식이면 BossBase 가 없어 공격 판정에 전혀 잡히지 않는다.
 *   - 자기 오인 : IsOwnCollider 는 transform.IsChildOf(transform) 라 자식인 이 콜라이더도 걸러낸다.
 *     GroundCast / GroundOverlap / GroundRayCast(GroundHeightAt) / WallCast 전부 이 필터를 쓰므로
 *     보스가 자기 발판 위에 서려고 하는 일은 없다.
 *   - hurtboxLayer / standPlatformLayer(둘 다 기본 -1 = 변경 없음) : -1 이 아니면 Awake 에서
 *     루트(히트박스)와 발판의 레이어를 각각 지정한다. 속성 트리거 히트박스가 layer 6 에 남아 있는 한
 *     플레이어의 지면 레이캐스트에 "공중에 뜬 발판"으로 잡힐 수 있으므로, 루트를 8(Ememy)로 옮기고
 *     발판만 6 에 두는 것이 가장 깨끗하다. 충돌 매트릭스가 전 레이어 개방이라 공격 판정에는 영향이 없다.
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
 * trigger SoilWave/WaterSprout/FlyingSword/FireRush/Iai/CounterAttack.
 *
 * Update 첫 줄 게이트(PauseManager.IsPaused || DialogueManager.IsDialogueActive)로
 * BT/쿨타임/반격 예약/중력이 전부 멈춘다. 거합 시퀀스·어검 소환 DelayedCall·돌진 트윈은
 * PauseManager 의 DOTween.PauseAll 로 함께 멈추고, 피해는 PlayerKnockBack.TakeHit 게이트가 차단.
 * FinalBossRoom 이 대사/전투 제어를 위해 이 컴포넌트의 enabled 를 끄면 OnDisable 이
 * 거합 연출·반격 예약·돌진 트윈·환영/히트박스를 전부 원상 복구한다.
 *
 * ─────────────────────────────────────────────────────────────
 * 사운드 (금 킷 소리 + 속성 보스 소리 재사용)
 * ─────────────────────────────────────────────────────────────
 * 피격음 : BossBase 의 공통 경로를 그대로 쓴다. DefaultHitSoundName 이 "Gold_HitLight" 라
 * 인스펙터가 비어 있으면 그 값이 들어간다. 최종보스는 금보스와 달리 모든 피격에 데미지가 들어가므로
 * 막힘(Block_Blunt)·그로기(Gold_HitHeavy) 분기가 없다.
 *   금 킷
 *     Gold_Draw        : 거합 진입(발도 준비 모션 시작).
 *     Gold_ScreenSlash : 거합 참격 확정(ResolveIaiSlash, 쳐내기에 실패했을 때만 — isIaiParried 면
 *                        그 앞에서 return 하므로 울리지 않는다).
 *     Gold_SwingSword  : 어검 5자루를 뿌리는 순간(flyingSwordSpawnDelay 콜백) 1회.
 *     Gold_SwingMelee  : 반격 시작, 그리고 돌진 콤보 각 타 시작(BeginPlayerComboStep — 1·2·3타 모두).
 *     Gold_Dash        : 돌진 콤보 진입(접근 시작). 금보스에는 대시 모션이 없어(애니메이터에
 *                        Dash 상태 자체가 없다) 이 소리를 쓸 곳이 없었고, 최종보스의 "돌진 콤보"가
 *                        플레이어 쪽으로 달려드는 유일한 대시 동작이라 여기에 배정했다.
 *     Parry_Success    : OnParrySuccess() — 거합·반격·돌진 콤보 쳐내기가 전부 이 한 곳을 지난다.
 *                        어검 쳐내기는 FlyingSword.Reflect() 의 Sword_Clash 가 담당한다(중복 없음).
 *   3단 콤보에 검 소리를 고른 근거 : 이 패턴은 플레이어 유래지만 연출상으로는 보스가 검을 세 번
 *   휘두르는 근접 공격이고(플레이어 Attack1~3 모션 + 검기 이펙트), 표에서 "근접 베기"에 해당하는
 *   이름은 Gold_SwingMelee 하나뿐이다. 각 타마다 재생해 3연타가 소리로도 3번 들리게 했다.
 *   전용 소리(Gold_Draw/ScreenSlash)는 거합 전용이라 쓰지 않았고, Gold_SwingSword 는 어검 투척 전용이다.
 *   속성 패턴 (해당 보스의 소리를 그대로 재사용)
 *     Soil_Smash                   : 토 파동 발사(LaunchSoilWave) — 원본 토보스 패턴1의 내려치기와 같은 소리.
 *     Water_Sprout                 : 물기둥 본체(Water_Sprout.cs)가 분출 순간에 스스로 재생한다.
 *     Fire_RushHit/Fire_RushHit2   : 지상 돌진이 끝나 멈추고 화염구를 쏘는 순간에 둘 중 하나를 무작위로
 *                                    재생한다(화보스 Rush 종료 지점과 같은 타이밍·같은 방식).
 *     Fire_LavaLand / Fire_LavaSizzle : 화염구 착지(Lava.cs)와 용암 장판(LavaPool.cs)이 스스로 재생한다.
 * 연타 방지 : 여기서 직접 부르는 소리들은 전부 패턴당 1회(또는 콤보 타당 1회)라 스로틀이 필요 없다.
 * 여러 개가 동시에 생기는 소환물(물기둥 5개·화염구 8개)의 소리는 각 소환물 스크립트가 스로틀을 건다.
 */

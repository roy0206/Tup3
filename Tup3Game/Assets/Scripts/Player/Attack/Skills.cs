using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(Playermovement))]
[RequireComponent(typeof(ComboAttack))]
[RequireComponent(typeof(PlayerHealth))]
public class Skills : MonoBehaviour, ISceneEventListener
{
    private Playermovement movement;
    private ComboAttack attack;
    private PlayerHealth health;

    [Header("1번 스킬설정")]
    public float skill_1_increase = 1.5f;
    public float skill_1_duration = 10f;
    public float skill_1_cool = 10f;
    public bool IsSkill1Active { get; private set; }
    public float Skill1CooldownRemaining { get; private set; }
    public float Skill1CooldownTotal => skill_1_cool;

    [Header("2번 스킬설정 (이동속도/낙하 버프)")]
    public float skill_2_haste = 1.2f;
    public float skill_2_duration = 10f;
    public float skill_2_cool = 10f;
    public float Skill2CooldownRemaining { get; private set; }
    public float Skill2CooldownTotal => skill_2_cool;

    [Header("변환 2번 스킬 설정")]
    private bool isAiming = false;
    public float skill_2_aimRange = 10f;
    public float skill_2_aimMoveSpeed = 8f;
    public float skill_2_minSpawnDistance = 2f;
    public LayerMask skill_2_groundMask;
    public float skill_2_groundCheckDistance = 50f;
    public GameObject skill_2_groundPrefab;
    public float skill_2_spawnDelay = 0.5f;
    [Min(0f)] public float skill_2_overlapPushClearance = 0.05f;
    public Transform skill_2_aimMarker;

    [Header("3번 스킬설정 (공격속도)")]
    public float skill_3_attackSpeedMultiplier = 1.5f;
    public float skill_3_duration = 5f;
    public float skill_3_cool = 10f;
    public bool IsSkill3Active { get; private set; }
    public float Skill3CooldownRemaining { get; private set; }
    public float Skill3CooldownTotal => skill_3_cool;


    [Header("4번 스킬설정 (힐량)")]
    public float skill_4_healAmount = 5f;
    public float skill_4_cool = 10f;
    public float skill_4_duration = 5f;
    public float Skill4CooldownRemaining { get; private set; }
    public float Skill4CooldownTotal => skill_4_cool;


    private bool canUseSkill_1 = true;
    private bool canUseSkill_2 = true;
    private bool canUseSkill_3 = true;
    private bool canUseSkill_4 = true;

    [SerializeField] private List<bool> isSkillEquiped = new() {false, false, false, false};
    public List<bool> IsSkillEquiped => isSkillEquiped;

    public event Action<int> OnSkillEquipped;

    public event Action<int, bool> OnSkillEquipChanged;

    public List<Action<float, float>> OnSkillsActive = new() { null, null, null, null };
    

    private float skill_2_aimOffsetX = 0f;
    private Vector2 skill_2_currentAimPoint;
    private bool skill_2_hasValidAimPoint = false;

    [Header("스킬 이펙트 - 스프라이트 시퀀스")]
    [SerializeField] private Sprite[] barrierEffectSprites;
    [SerializeField] private Sprite[] healEffectSprites;
    [SerializeField] private Sprite[] attackSpeedEffectSprites;
    [SerializeField] private Sprite[] attackUpEffectSprites;

    [Header("스킬 이펙트 - 플레이어 뒤 배경 이펙트")]
    [SerializeField] private float backgroundEffectFrameRate = 12f;
    [SerializeField] private Vector2 backgroundEffectOffset = new Vector2(0f, 0.2f);
    [SerializeField] private float backgroundEffectScale = 1.4f;
    [SerializeField] private int backgroundEffectOrderOffset = 1;

    [Header("스킬 이펙트 - 장벽(소환 지형)")]
    [SerializeField] private float barrierEffectFrameRate = 10f;
    [SerializeField] private Vector2 barrierEffectOffset = Vector2.zero;
    [SerializeField] private float barrierEffectScale = 1f;
    [SerializeField] private int barrierEffectOrderOffset = 0;
    [SerializeField] private bool hideBarrierOriginalSprite = true;

    [Header("스킬 이펙트 - 공격력 버프 타격 이펙트")]
    [SerializeField] private float hitEffectFrameRate = 24f;
    [SerializeField] private float hitEffectScale = 1.2f;
    [SerializeField] private int hitEffectOrderOffset = 5;
    [SerializeField] private int hitEffectMaxCount = 8;

    private SpriteSequencePlayer healEffect;
    private SpriteSequencePlayer attackSpeedEffect;

    private Transform hitEffectRoot;
    private readonly List<SpriteSequencePlayer> hitEffectPool = new List<SpriteSequencePlayer>();
    private int hitEffectCursor;

    [Header("사운드")]
    [SerializeField, Range(0f, 1f)] private float skillSoundVolume = 0.9f;

    private const string SoundSkillAttackUp = "Player_SkillAttackUp";
    private const string SoundSkillBarrier = "Player_SkillBarrier";
    private const string SoundSkillAttackSpeed = "Player_SkillAttackSpeed";
    private const string SoundSkillHeal = "Player_SkillHeal";

    [Header("스킬 키 세팅")]
    public KeyCode skill_1_key = KeyCode.F;
    public KeyCode skill_2_key = KeyCode.A;
    public KeyCode skill_3_key = KeyCode.D;
    public KeyCode skill_4_key = KeyCode.S;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        movement = GetComponent<Playermovement>();
        attack = GetComponent<ComboAttack>();
        health = GetComponent<PlayerHealth>();
        PrepareSkill2AimMarker();

        healEffect = BuildBackgroundEffect("SkillEffect_Heal", healEffectSprites);
        attackSpeedEffect = BuildBackgroundEffect("SkillEffect_AttackSpeed", attackSpeedEffectSprites);

        SceneController.Instance.RegisterListener(this);
    }

    private SpriteSequencePlayer BuildBackgroundEffect(string objectName, Sprite[] frames)
    {
        if (frames == null || frames.Length == 0) return null;

        GameObject effect = new GameObject(objectName);
        effect.transform.SetParent(transform, false);
        effect.transform.localPosition = backgroundEffectOffset;
        effect.transform.localScale = Vector3.one * Mathf.Max(0.01f, backgroundEffectScale);

        SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();

        if (playerRenderer != null)
        {
            renderer.sortingLayerID = playerRenderer.sortingLayerID;
            renderer.sortingOrder = playerRenderer.sortingOrder - Mathf.Max(1, backgroundEffectOrderOffset);
        }
        else
        {
            renderer.sortingOrder = -Mathf.Max(1, backgroundEffectOrderOffset);
        }

        SpriteSequencePlayer player = effect.AddComponent<SpriteSequencePlayer>();
        player.SetSequence(frames, backgroundEffectFrameRate, true, false);

        effect.SetActive(false);
        return player;
    }

    private void PlayBackgroundEffect(SpriteSequencePlayer effect)
    {
        if (effect == null) return;

        effect.gameObject.SetActive(false);
        effect.gameObject.SetActive(true);
    }

    private void StopBackgroundEffect(SpriteSequencePlayer effect)
    {
        if (effect == null) return;

        effect.gameObject.SetActive(false);
    }

    public void PlayAttackHitEffect(Collider2D target, Collider2D source)
    {
        if (!IsSkill1Active) return;
        if (attackUpEffectSprites == null || attackUpEffectSprites.Length == 0) return;

        Vector2 reference = source != null ? (Vector2)source.bounds.center : (Vector2)transform.position;
        Vector2 point = target != null ? target.ClosestPoint(reference) : reference;

        SpriteSequencePlayer effect = GetHitEffectInstance();
        if (effect == null) return;

        effect.gameObject.SetActive(false);
        effect.transform.position = point;
        effect.transform.localScale = Vector3.one * Mathf.Max(0.01f, hitEffectScale);

        ApplyHitEffectSorting(effect.GetComponent<SpriteRenderer>(), target);

        effect.SetSequence(attackUpEffectSprites, hitEffectFrameRate, false, true);
        effect.gameObject.SetActive(true);
    }

    private SpriteSequencePlayer GetHitEffectInstance()
    {
        for (int i = 0; i < hitEffectPool.Count; i++)
        {
            SpriteSequencePlayer candidate = hitEffectPool[i];

            if (candidate == null)
            {
                candidate = CreateHitEffectInstance();
                hitEffectPool[i] = candidate;
                return candidate;
            }

            if (!candidate.gameObject.activeSelf) return candidate;
        }

        if (hitEffectPool.Count < Mathf.Max(1, hitEffectMaxCount))
        {
            SpriteSequencePlayer created = CreateHitEffectInstance();
            hitEffectPool.Add(created);
            return created;
        }

        hitEffectCursor = (hitEffectCursor + 1) % hitEffectPool.Count;
        return hitEffectPool[hitEffectCursor];
    }

    private SpriteSequencePlayer CreateHitEffectInstance()
    {
        if (hitEffectRoot == null)
        {
            hitEffectRoot = new GameObject("SkillEffect_AttackUpHitPool").transform;
        }

        GameObject effectObject = new GameObject("SkillEffect_AttackUpHit");
        effectObject.transform.SetParent(hitEffectRoot, false);
        effectObject.AddComponent<SpriteRenderer>();

        SpriteSequencePlayer player = effectObject.AddComponent<SpriteSequencePlayer>();
        effectObject.SetActive(false);
        return player;
    }

    private void ApplyHitEffectSorting(SpriteRenderer renderer, Collider2D target)
    {
        if (renderer == null) return;

        SpriteRenderer reference = null;

        if (target != null)
        {
            reference = target.GetComponent<SpriteRenderer>();
            if (reference == null) reference = target.GetComponentInParent<SpriteRenderer>();
            if (reference == null) reference = target.GetComponentInChildren<SpriteRenderer>();
        }

        if (reference == null) reference = GetComponent<SpriteRenderer>();

        int offset = Mathf.Max(1, hitEffectOrderOffset);

        if (reference != null)
        {
            renderer.sortingLayerID = reference.sortingLayerID;
            renderer.sortingOrder = reference.sortingOrder + offset;
        }
        else
        {
            renderer.sortingOrder = offset;
        }
    }

    private IEnumerator WaitSkillDuration(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (health != null && health.IsDead) yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void OnDisable()
    {
        StopBackgroundEffect(healEffect);
        StopBackgroundEffect(attackSpeedEffect);
    }

    private void OnDestroy()
    {
        if (hitEffectRoot != null) Destroy(hitEffectRoot.gameObject);
        hitEffectPool.Clear();
    }

    public void OptainSkill(int num)
    {
        SetSkillEquipped(num, true);
    }

    private void SetSkillEquipped(int num, bool equipped)
    {
        if (num < 0 || num >= isSkillEquiped.Count) return;
        if (isSkillEquiped[num] == equipped) return;

        isSkillEquiped[num] = equipped;

        if (equipped) OnSkillEquipped?.Invoke(num);
        OnSkillEquipChanged?.Invoke(num, equipped);
    }

    public void OnSceneLoadComplete(string sceneName)
    {
        SyncFromSaveData();
    }

    public void OnSceneExit(string sceneName)
    {
        SceneController.Instance.UnregisterListener(this);
    }

    private void SyncFromSaveData()
    {
        var data = UserDataManager.Instance != null ? UserDataManager.Instance.Data : null;
        if (data == null || data.Play == null || data.Play.skills == null) return;

        var saved = data.Play.skills;
        for (int i = 0; i < isSkillEquiped.Count; i++)
        {
            bool savedEquipped = i < saved.Count && saved[i];
            SetSkillEquipped(i, savedEquipped);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (PauseManager.IsPaused || DialogueManager.IsDialogueActive) return;

        if (Input.GetKeyDown(skill_1_key) && canUseSkill_1 && isSkillEquiped[0])
        {
            StartCoroutine(Do_skill_1());
        }

        if (Input.GetKeyDown(skill_2_key) && canUseSkill_2 && isSkillEquiped[1])
        {
            StartCoroutine(Do_changed_skill_2());
        }
        if (Input.GetKeyDown(skill_3_key) && canUseSkill_3 &&  isSkillEquiped[2])
        {
            StartCoroutine(Do_skill_3());
        }

        if (Input.GetKeyDown(skill_4_key) && canUseSkill_4 &&  isSkillEquiped[3])
        {
            StartCoroutine(Do_skill_4());
        }

        if (isAiming)
        {
            UpdateAimSkill2();

            if (Input.GetKeyUp(skill_2_key))
            {
                StopAimingAndSpawnSkill2();
            }
        }
    }


    private IEnumerator Do_skill_1()
    {
        canUseSkill_1 = false;
        OnSkillsActive[0]?.Invoke(skill_1_duration, skill_1_cool);

        AudioManager.Instance.PlaySound(SoundSkillAttackUp, skillSoundVolume);

        float originalDamage = attack.attackPower;

        try
        {
            IsSkill1Active = true;
            attack.attackPower *= skill_1_increase;
            yield return WaitSkillDuration(skill_1_duration);
        }
        finally
        {
            attack.attackPower = originalDamage;
            IsSkill1Active = false;
        }

        yield return RunCooldown(skill_1_cool, v => Skill1CooldownRemaining = v);

        canUseSkill_1 = true;
    }

   


    private IEnumerator Do_changed_skill_2()
    {
        canUseSkill_2 = false;
        OnSkillsActive[1]?.Invoke(skill_2_duration, skill_2_cool);

        AudioManager.Instance.PlaySound(SoundSkillBarrier, skillSoundVolume);

        float originalSpeed = movement.moveSpeed;
        float originalGravity = movement.fallGravityMultiplier;

        try
        {
            movement.moveSpeed *= skill_2_haste;
            movement.fallGravityMultiplier *= skill_2_haste;

            StartAimingSkill2();

            yield return new WaitForSeconds(skill_2_duration);
        }
        finally
        {
            movement.moveSpeed = originalSpeed;
            movement.fallGravityMultiplier = originalGravity;

            if (isAiming)
            {
                isAiming = false;
                if (skill_2_aimMarker != null)
                    skill_2_aimMarker.gameObject.SetActive(false);
            }
        }

        yield return RunCooldown(skill_2_cool, v => Skill2CooldownRemaining = v);

        canUseSkill_2 = true;
    }

    private void StartAimingSkill2()
    {
        isAiming = true;
        skill_2_aimOffsetX = 0f;

        movement.StopHorizontalMovement();
        UpdateAimSkill2();
    }

    private void UpdateAimSkill2()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        skill_2_aimOffsetX += horizontalInput * skill_2_aimMoveSpeed * Time.deltaTime;
        skill_2_aimOffsetX = Mathf.Clamp(skill_2_aimOffsetX, -skill_2_aimRange, skill_2_aimRange);

        if (Mathf.Abs(skill_2_aimOffsetX) < skill_2_minSpawnDistance)
        {
            float sign;
            if (horizontalInput != 0f)
                sign = Mathf.Sign(horizontalInput);   // 입력 방향으로 넘어감
            else
                sign = skill_2_aimOffsetX >= 0f ? 1f : -1f; // 입력 없으면 기존 쪽 유지

            skill_2_aimOffsetX = sign * skill_2_minSpawnDistance;
        }

        float aimX = transform.position.x + skill_2_aimOffsetX;

        Vector2 rayStart = new Vector2(aimX, transform.position.y);
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, skill_2_groundCheckDistance, skill_2_groundMask);

        Debug.DrawRay(rayStart, Vector2.down * skill_2_groundCheckDistance, Color.yellow);

        if (hit.collider != null)
        {
            skill_2_currentAimPoint = new Vector2(aimX, hit.point.y);
            skill_2_hasValidAimPoint = true;
        }
        else
        {
            skill_2_hasValidAimPoint = false;
        }

        if (skill_2_aimMarker != null)
        {
            if (skill_2_hasValidAimPoint)
                skill_2_aimMarker.position = skill_2_currentAimPoint;

            if (skill_2_aimMarker.gameObject.activeSelf != skill_2_hasValidAimPoint)
                skill_2_aimMarker.gameObject.SetActive(skill_2_hasValidAimPoint);
        }
    }

    private void PrepareSkill2AimMarker()
    {
        if (skill_2_aimMarker == null)
            return;

        GameObject markerObject = skill_2_aimMarker.gameObject;

        // 프리팹 에셋을 직접 연결한 경우 씬에 존재하지 않아 렌더링되지 않으므로 복제해서 사용한다.
        if (!markerObject.scene.IsValid())
        {
            markerObject = Instantiate(markerObject, transform);
            markerObject.name = skill_2_aimMarker.name;
            skill_2_aimMarker = markerObject.transform;
        }

        markerObject.SetActive(false);
    }

    private void StopAimingAndSpawnSkill2()
    {
        isAiming = false;

        if (skill_2_aimMarker != null)
            skill_2_aimMarker.gameObject.SetActive(false);

        if (skill_2_hasValidAimPoint && skill_2_groundPrefab != null)
        {
            StartCoroutine(SpawnGroundAfterDelay(skill_2_currentAimPoint));
        }

        skill_2_hasValidAimPoint = false;
    }

    private IEnumerator SpawnGroundAfterDelay(Vector2 spawnPoint)
    {
        yield return new WaitForSeconds(skill_2_spawnDelay);

        if (skill_2_groundPrefab != null)
        {
            GameObject spawnedGround = Instantiate(skill_2_groundPrefab, spawnPoint, Quaternion.identity);
            spawnedGround.SetActive(true);

            if (!spawnedGround.TryGetComponent(out SkillGroundMarker marker))
                marker = spawnedGround.AddComponent<SkillGroundMarker>();

            marker.ApplyBarrierVisual(
                barrierEffectSprites,
                barrierEffectFrameRate,
                barrierEffectOffset,
                barrierEffectScale,
                barrierEffectOrderOffset,
                hideBarrierOriginalSprite);

            ResolvePlayerOverlap(spawnedGround);

            yield return new WaitForSeconds(skill_2_duration);
            if (spawnedGround != null)
            {
                Destroy(spawnedGround);
            }
        }
    }

    private void ResolvePlayerOverlap(GameObject spawnedGround)
    {
        if (movement == null || spawnedGround == null)
            return;

        Collider2D playerCollider = movement.GetComponent<Collider2D>();
        if (playerCollider == null)
            return;

        Physics2D.SyncTransforms();

        Bounds playerBounds = playerCollider.bounds;
        float highestOverlappingSurface = float.NegativeInfinity;

        foreach (Collider2D groundCollider in spawnedGround.GetComponentsInChildren<Collider2D>(true))
        {
            if (groundCollider == null || !groundCollider.enabled)
                continue;

            Bounds groundBounds = groundCollider.bounds;
            if (!groundBounds.Intersects(playerBounds))
                continue;

            highestOverlappingSurface = Mathf.Max(
                highestOverlappingSurface,
                groundBounds.max.y
            );
        }

        if (float.IsNegativeInfinity(highestOverlappingSurface))
            return;

        float clearance = Mathf.Max(skill_2_overlapPushClearance, 0.01f);
        float pushDistance = highestOverlappingSurface + clearance - playerBounds.min.y;
        if (pushDistance <= 0f)
            return;

        movement.transform.position += Vector3.up * pushDistance;
        movement.ResetVerticalVelocity();
        Physics2D.SyncTransforms();
    }

    private IEnumerator Do_skill_3()
    {
        canUseSkill_3 = false;
        IsSkill3Active = true;
        OnSkillsActive[2]?.Invoke(skill_3_duration, skill_3_cool);

        AudioManager.Instance.PlaySound(SoundSkillAttackSpeed, skillSoundVolume);

        try
        {
            PlayBackgroundEffect(attackSpeedEffect);
            attack.SetAttackSpeedMultiplier(skill_3_attackSpeedMultiplier);

            yield return WaitSkillDuration(skill_3_duration);
        }
        finally
        {
            StopBackgroundEffect(attackSpeedEffect);
            attack.SetAttackSpeedMultiplier(1f);
            IsSkill3Active = false;
        }

        yield return RunCooldown(skill_3_cool, v => Skill3CooldownRemaining = v);

        canUseSkill_3 = true;
    }

   

    private IEnumerator Do_skill_4()
    {
        canUseSkill_4 = false;
        OnSkillsActive[3]?.Invoke(skill_4_duration, skill_4_cool);

        AudioManager.Instance.PlaySound(SoundSkillHeal, skillSoundVolume);

        float elapsed = 0f;
        float tickInterval = 1f;
        float nextTick = tickInterval;
        float healPerTick = skill_4_healAmount / (skill_4_duration / tickInterval);

        try
        {
            PlayBackgroundEffect(healEffect);

            while (elapsed < skill_4_duration)
            {
                elapsed += Time.deltaTime;

                while (elapsed >= nextTick)
                {
                    if (health.IsDead)
                    {
                        yield break;
                    }
                    health.Heal(healPerTick);
                    nextTick += tickInterval;
                }
                yield return null;
            }
        }
        finally
        {
            StopBackgroundEffect(healEffect);
        }

        yield return RunCooldown(skill_4_cool, v => Skill4CooldownRemaining = v);

        canUseSkill_4 = true;
    }
    
    public void TryActivateSkill4()
    {
        if (!canUseSkill_4) return;
        StartCoroutine(Do_skill_4());
    }


    private IEnumerator RunCooldown(float coolTime, System.Action<float> setRemaining)
    {
        float remaining = coolTime;
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            setRemaining(remaining);
            yield return null;
        }
        setRemaining(0f);
    }


    public bool IsAiming => isAiming;
}

/* [파일 노트]
 * 스킬은 isSkillEquiped[n] 이 true 인 것만 발동된다(Update 의 키 입력 게이트).
 * 획득 경로는 두 가지: 보스전 승리(BossRoom.GrantSkill → OptainSkill)와
 * 세이브 동기화(SyncFromSaveData — 씬 로드 완료 시 PlayData.skills 를 반영).
 * 씬마다 플레이어 인스턴스가 새로 생기므로 ISceneEventListener 로 등록해
 * 어느 씬에서든 저장된 보유 스킬이 살아나게 한다(기존에는 Lobby.cs 만 동기화해서
 * 보스 씬에서는 획득한 스킬도 잠겨 있었다).
 * OnSkillEquipped(int) 는 획득·동기화로 스킬이 새로 열릴 때마다 1회 발생한다. OptainSkill 은 멱등(이미 보유면 무시).
 *
 * ── 씬이 스킬을 강제 해금하던 문제 (2026-08-29) ───────────────────────────────
 * Boss_Fire / Boss_Gold / Boss_Soil 의 Player 프리팹 인스턴스에 isSkillEquiped[0~3] = 1 오버라이드가,
 * Boss_Water 에는 [1] = 1 오버라이드가 박혀 있었다(개발 중 테스트 잔재로 보인다).
 * 프리팹 기본값은 00000000 인데 씬 오버라이드가 이기고, 예전 SyncFromSaveData 는 false → true 한 방향만
 * 처리해서 세이브 데이터에 없는 스킬도 영영 열린 채로 남았다.
 * 이제 SyncFromSaveData 는 PlayData.skills 를 그대로 반영하는 양방향 동기화다 —
 * 세이브에 없으면 씬이 뭘 켜 놨든 잠긴다. 즉 세이브 데이터가 유일한 기준이고
 * 씬 오버라이드는 씬 로드 완료 시점에 무력화된다(오버라이드 자체를 씬에서 지우는 편이 여전히 깔끔하다).
 *
 * 잠금 방향 통지를 위해 OnSkillEquipChanged(int index, bool equipped) 를 추가했다.
 * OnSkillEquipped 는 해금 때만 울리므로 "열렸다 → 다시 잠김"을 SkillView 가 알 수 없었다.
 * 두 이벤트 모두 SetSkillEquipped 한 곳에서만 발생하고, 값이 실제로 바뀔 때만 울린다.
 * SkillView 는 OnSkillEquipChanged 를 구독한다(양방향).
 *
 * SpawnGroundAfterDelay 의 AddComponent<SkillGroundMarker>() 한 줄 : 스킬2로 소환한 지형에
 * 식별용 빈 마커를 붙인다. 최종보스의 토 파동 투사체(SoilWave)가 이 마커를 보고 소멸한다
 * (속성 상성). 지형 프리팹은 수정하지 않았고 지형 동작에도 영향이 없다.
 *
 * 효과음 배선 — 인덱스/메서드 번호가 아니라 "실제 기능" 기준으로 붙였다.
 *   isSkillEquiped[0] / skill_1_key(F) / Do_skill_1  = attack.attackPower *= skill_1_increase → 공격력 버프
 *                                                      → Player_SkillAttackUp
 *   isSkillEquiped[1] / skill_2_key(A) / Do_changed_skill_2 = 이동/낙하 가속 + 조준 후 지형(장벽) 소환
 *                                                      → Player_SkillBarrier
 *   isSkillEquiped[2] / skill_3_key(D) / Do_skill_3  = attack.SetAttackSpeedMultiplier → 공격속도 버프
 *                                                      → Player_SkillAttackSpeed
 *   isSkillEquiped[3] / skill_4_key(S) / Do_skill_4  = health.Heal 틱 회복 → Player_SkillHeal
 * 재생 지점은 네 코루틴 모두 발동 확정 직후(쿨다운 잠금 + OnSkillsActive 통지 다음)로 통일했다.
 * 장벽음은 지형이 실제로 생성되는 SpawnGroundAfterDelay 가 아니라 스킬 발동 시점에 낸다 —
 * 조준 실패로 지형이 안 나오는 경우에도 "스킬을 썼다"는 피드백이 필요하고 다른 세 스킬과 시점이 같다.
 * Player_SkillAttackSpeed 는 아직 오디오 파일이 없어 AudioManager 가 경고 로그만 남긴다(호출은 유효).
 *
 * ── 스킬 이펙트 (스프라이트 시퀀스 / 기존 파티클 대체) ──────────────────────────────
 * 리소스는 Assets/GameAssets/Player/Texutres/Skill/ 아래 키 이름 폴더에 있고 원소와 1:1이다.
 * 사운드와 마찬가지로 폴더(키)가 아니라 "실제 기능" 기준으로 연결했다.
 *   A / soil_1~4  (토) = 장벽 생성  → 인덱스1 / Do_changed_skill_2 → barrierEffectSprites
 *   S / water_1~4 (수) = 회복       → 인덱스3 / Do_skill_4         → healEffectSprites
 *   D / fire_1~5  (화) = 공격속도   → 인덱스2 / Do_skill_3         → attackSpeedEffectSprites
 *   F / Hit-Yellow(금) = 공격력     → 인덱스0 / Do_skill_1         → attackUpEffectSprites
 * Player.prefab 의 실제 키 바인딩도 이 표와 같다 — skill_1_key=F(102), skill_2_key=A(97),
 * skill_3_key=D(100), skill_4_key=S(115). 즉 "내부 번호"가 아니라 "원소/기능"이 키의 기준이다.
 * (예전 노트에 A=skill_1 로 적혀 있었으나 사실과 달라 바로잡았다 — 2026-08-29.)
 *
 * 제거한 파티클 : skill_1_auraEffect / skill_2_auraEffect / skill_3_auraEffect / skill_4_HealEffect
 * (public ParticleSystem 필드 4개와 Awake 의 초기 Stop, 각 코루틴의 Play/Stop 호출을 전부 삭제했다.
 *  Player.prefab 에 남아 있는 옛 참조는 필드가 사라져 그냥 무시된다 — 프리팹은 건드리지 않았다.)
 *
 * 배경 이펙트(회복/공속) : BuildBackgroundEffect 가 Awake 에서 플레이어의 자식으로
 * SpriteRenderer + SpriteSequencePlayer 오브젝트를 하나씩 만들어 두고 꺼 둔다(코드 생성, 프리팹 무수정).
 *  - "플레이어 뒤" 보장 : 플레이어 루트의 SpriteRenderer 에서 sortingLayerID 를 그대로 복사하고
 *    sortingOrder 를 (플레이어 order − backgroundEffectOrderOffset) 으로 잡는다. 같은 정렬 레이어
 *    안에서 order 가 작으면 반드시 뒤에 그려지므로, 레이어가 어떻게 설정돼 있든 항상 뒤에 깔린다.
 *    오프셋은 최소 1 로 클램프해 같은 order 로 겹쳐 z-파이팅이 나는 일이 없다.
 *  - 발동 시 PlayBackgroundEffect 가 SetActive(false)→(true) 로 토글한다. SpriteSequencePlayer 가
 *    OnEnable 에서 항상 1프레임부터 재생하는 관례(Gold 보스 이펙트와 동일)를 그대로 쓴 것이다.
 *  - 플레이어의 자식이므로 이동/점프를 그대로 따라다닌다.
 * 장벽 이펙트는 소환된 지형 쪽에 붙는다 — SkillGroundMarker.ApplyBarrierVisual 참고.
 * F(금) 는 스프라이트 시트 + F_Skill_Animation.anim 형태지만, 시트가 이미 Hit-Yellow_0~14 로
 * 슬라이스돼 있고 .anim 도 그 15장을 12fps 로 늘어놓은 PPtr 커브일 뿐이라 Animator 없이
 * 나머지 3종과 똑같이 SpriteSequencePlayer 로 재생한다(.anim 은 사용하지 않는다).
 *
 * ── 이펙트가 아예 안 나오던 원인 (2026-08-29 해결) ────────────────────────────
 * 두 가지가 겹쳐 있었고 둘 다 에러 없이 조용히 실패하는 종류였다.
 *
 * 1) Player.prefab 의 네 배열(barrier/heal/attackSpeed/attackUp EffectSprites)이 전부 비어 있었다.
 *    BuildBackgroundEffect 는 frames 가 비면 null 을 반환하고 PlayBackgroundEffect(null) 은
 *    조용히 return 하므로 로그 한 줄 없이 아무 일도 일어나지 않는다. 이제 채워 넣었다.
 *
 * 2) 프레임 PNG 13장(soil_1~4 / water_1~4 / fire_1~5)이 Sprite Mode = Multiple 로 임포트돼
 *    자동 슬라이스된 조각들만 서브 애셋으로 존재했다(예: soil_1 → 113x130 본체 + 6x11 티끌,
 *    water_4 → 7조각). 각 PNG 는 256x256 프레임 한 장이므로 Single 이 맞다. 전부 Single 로 바꿨고
 *    이제 fileID 21300000(단일 스프라이트 고정값)으로 참조한다.
 *    Hit-Yellow 만은 4096x4096 = 1024x1024 x 4x4 짜리 진짜 시트라 Multiple 을 유지했다.
 *    다만 PPU 가 100 이면 프레임 하나가 10.24 유닛(x backgroundEffectScale 1.4 = 14 유닛)이라
 *    화면을 덮어버려서, PPU 를 400 으로 올려 나머지 3종과 같은 2.56 유닛으로 맞췄다.
 *    (F_Skill_Animation.anim 은 어떤 컨트롤러/프리팹도 참조하지 않는 고아라 PPU 변경 영향이 없다.)
 *
 * 프레임 수 : 장벽 4 / 회복 4 / 공속 5 / 공격력 15. 순서는 파일명 순이고 공격력만
 * Hit-Yellow_0~14 이며 이는 .anim 의 키프레임 순서와 동일하다(sampleRate 12 = backgroundEffectFrameRate).
 *
 * ── 재생 방식 개편 (2026-08-29) ───────────────────────────────────────────────
 * 요구: "S/D 는 활성 시간 동안 상시 이펙트, F 는 발동 시 무이펙트 + 적을 때린 위치에 이펙트".
 *
 * 1) S(회복, Do_skill_4) / D(공속, Do_skill_3) — 지속 루프
 *    BuildBackgroundEffect 의 SetSequence 인자를 loop = true, deactivateOnComplete = false 로 바꿨다.
 *    (SpriteSequencePlayer 는 loop 가 켜져 있으면 Play 의 do-while 이 끝나지 않아 deactivateOnComplete
 *     가 영영 실행되지 않는다. 즉 이 조합에서 끄는 책임은 전적으로 호출부에 있다.)
 *    켜는 곳은 각 코루틴의 try 첫 줄, 끄는 곳은 같은 코루틴의 finally 첫 줄이다 —
 *    버프가 원복되는 지점(SetAttackSpeedMultiplier(1f) / 힐 틱 종료)과 정확히 같은 프레임에 꺼진다.
 *    Do_skill_4 는 원래 try 가 없었고 사망 시 while 안에서 yield break 로 빠져나가 이펙트가 남았을 자리라,
 *    힐 루프 전체를 try/finally 로 감쌌다(yield break 로 나가도 finally 는 실행된다).
 *    쿨다운(RunCooldown)은 finally 밖에 두어 "이펙트 지속 = 버프 지속"이 유지된다.
 *  - 코루틴이 통째로 중단되는 경우(사망 연출, 씬 전환) : Unity 는 StopCoroutine/오브젝트 파괴로 중단된
 *    코루틴의 finally 를 보장하지 않으므로 이중 안전장치를 뒀다.
 *      · WaitSkillDuration : WaitForSeconds 대신 쓰는 대기 루프. health.IsDead 가 되면 즉시 빠져나와
 *        정상 경로로 finally 를 태운다(사망 시 버프/이펙트가 함께 끊긴다).
 *      · OnDisable : 플레이어가 비활성화되면 두 루프 이펙트를 무조건 SetActive(false).
 *        이펙트가 플레이어의 자식이라 파괴 시에는 같이 사라지므로 이걸로 남는 경우가 없다.
 *
 * 2) F(공격력, Do_skill_1) — 발동 이펙트 없음 + 피격 위치 이펙트
 *    Do_skill_1 의 PlayBackgroundEffect 호출과 attackUpEffect 필드/생성을 제거했다.
 *    attackUpEffectSprites(Hit-Yellow_0~14) 는 이제 타격 이펙트 프레임으로만 쓴다.
 *  - 발생 조건 : IsSkill1Active(버프가 걸려 있는 동안)일 때만. 즉 평타에는 안 나오고 F 지속 시간에만 나온다.
 *    "적을 때릴 경우"를 F 스킬의 연출로 해석한 것 — 상시 타격 이펙트가 필요하면 이 가드만 빼면 된다.
 *  - 호출 경로 : Attackhitbox.OnTriggerEnter2D 가 BossBase.DoDamage / Water_eye.DoDamage 직후
 *    Skills.PlayAttackHitEffect(맞은 콜라이더, 히트박스 콜라이더) 를 부른다. 데미지가 실제로 들어간
 *    두 분기에만 붙였으므로 허공 스윙이나 무시된 충돌에는 나오지 않는다.
 *  - 위치 : target.ClosestPoint(히트박스 bounds 중심). 적 콜라이더 표면에서 히트박스 중심에 가장 가까운
 *    점이라 "맞은 면"에 붙고, 히트박스 중심이 적 안쪽이면 그 점을 그대로 돌려주므로 적 몸통에 뜬다.
 *    어느 경우든 플레이어가 아니라 적 쪽 좌표다.
 *  - 정렬 : 맞은 대상의 SpriteRenderer(자신 → 부모 → 자식 순으로 탐색)에서 sortingLayerID 를 복사하고
 *    sortingOrder = 대상 order + hitEffectOrderOffset(기본 5). 절대값을 박지 않고 대상 기준 상대값으로
 *    잡은 이유는 보스마다 order 가 제각각(FinalBossSceneBuilder 는 1/2/5/30/40/41 을 섞어 쓴다)이라
 *    고정값으로는 어떤 보스에서는 뒤로 숨기 때문이다. 배경 이펙트가 "플레이어 order − 1"로 뒤를 보장하는
 *    것과 정확히 대칭이다. +5 는 대상의 자식 파츠(무기/이펙트)가 몇 장 겹쳐 있어도 넘도록 잡은 여유값.
 *    대상 렌더러를 못 찾으면 플레이어 렌더러 기준으로 폴백한다.
 *  - 겹침 대응 : 3단 콤보로 짧은 간격에 연속 발생하므로 하나를 재사용하면 앞 재생이 끊긴다.
 *    hitEffectMaxCount(기본 8) 짜리 자체 풀을 쓴다 — 비활성(재생 완료) 인스턴스를 먼저 재사용하고,
 *    없으면 상한까지 새로 만들고, 상한에 닿으면 커서를 돌려 가장 오래된 것부터 덮어쓴다.
 *    각 인스턴스는 loop = false, deactivateOnComplete = true 라 재생이 끝나면 스스로 꺼져 풀로 돌아온다.
 *    PoolManager 는 쓰지 않았다 — Addressables 라벨 "Pool" 로 미리 로드한 *프리팹*만 Get 할 수 있는
 *    구조인데 이 이펙트는 코드로 생성하는 프리팹 없는 오브젝트라 등록 대상이 아니다.
 *  - 부모 : 플레이어가 아니라 씬 루트의 "SkillEffect_AttackUpHitPool" 아래에 둔다. 타격 이펙트는
 *    맞은 자리에 고정돼야 하므로 플레이어를 따라다니면 안 된다. Skills.OnDestroy 에서 풀 루트를 정리한다.
 *  - 크기 : Hit-Yellow 는 PPU 400 기준 프레임 하나가 2.56 유닛이라 타격 연출로는 크다.
 *    hitEffectScale 기본 0.7 → 약 1.8 유닛으로 잡았다(인스펙터에서 조절).
 *  - 프레임레이트 : hitEffectFrameRate 기본 12 — 원본 F_Skill_Animation.anim 의 sampleRate 와 같다.
 *    15프레임 / 12fps = 1.25초. 짧게 터뜨리고 싶으면 이 값을 올리면 된다.
 */

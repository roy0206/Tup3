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

    private SpriteSequencePlayer healEffect;
    private SpriteSequencePlayer attackSpeedEffect;
    private SpriteSequencePlayer attackUpEffect;

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

        attackUpEffect = BuildBackgroundEffect("SkillEffect_AttackUp", attackUpEffectSprites);
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
        player.SetSequence(frames, backgroundEffectFrameRate, false, true);

        effect.SetActive(false);
        return player;
    }

    private void PlayBackgroundEffect(SpriteSequencePlayer effect)
    {
        if (effect == null) return;

        effect.gameObject.SetActive(false);
        effect.gameObject.SetActive(true);
    }

    public void OptainSkill(int num)
    {
        if (num < 0 || num >= isSkillEquiped.Count) return;
        if (isSkillEquiped[num]) return;

        isSkillEquiped[num] = true;
        OnSkillEquipped?.Invoke(num);
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
        for (int i = 0; i < isSkillEquiped.Count && i < saved.Count; i++)
        {
            if (saved[i] && !isSkillEquiped[i])
            {
                isSkillEquiped[i] = true;
                OnSkillEquipped?.Invoke(i);
            }
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
        PlayBackgroundEffect(attackUpEffect);

        float originalDamage = attack.attackPower;
       
        try
        {
            attack.attackPower *= skill_1_increase;
            yield return new WaitForSeconds(skill_1_duration);
        }
        finally
        {
        attack.attackPower = originalDamage;
        }

        yield return RunCooldown(skill_1_cool, v => Skill1CooldownRemaining = v);

        canUseSkill_1 = true;
    }

   


    private IEnumerator Do_changed_skill_2()
    {
        canUseSkill_2 = false;
<<<<<<< HEAD
        OnSkillsActive[1].Invoke(skill_2_duration, skill_2_cool);

        AudioManager.Instance.PlaySound(SoundSkillBarrier, skillSoundVolume);

=======
        OnSkillsActive[1]?.Invoke(skill_2_duration, skill_2_cool);
>>>>>>> origin/main
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
<<<<<<< HEAD

            SkillGroundMarker marker = spawnedGround.AddComponent<SkillGroundMarker>();
            marker.ApplyBarrierVisual(
                barrierEffectSprites,
                barrierEffectFrameRate,
                barrierEffectOffset,
                barrierEffectScale,
                barrierEffectOrderOffset,
                hideBarrierOriginalSprite);
=======
            spawnedGround.SetActive(true);

            if (!spawnedGround.TryGetComponent<SkillGroundMarker>(out _))
                spawnedGround.AddComponent<SkillGroundMarker>();

            ResolvePlayerOverlap(spawnedGround);
>>>>>>> origin/main

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
        PlayBackgroundEffect(attackSpeedEffect);

        try
        {
            attack.SetAttackSpeedMultiplier(skill_3_attackSpeedMultiplier);

            yield return new WaitForSeconds(skill_3_duration);
        }
        finally
        {
            attack.SetAttackSpeedMultiplier(1f);
            IsSkill3Active = false;
        }

        yield return RunCooldown(skill_3_cool, v => Skill3CooldownRemaining = v);

        canUseSkill_3 = true;
    }

   

    private IEnumerator Do_skill_4()
    {
        canUseSkill_4 = false;
<<<<<<< HEAD
        OnSkillsActive[3].Invoke(skill_4_duration, skill_4_cool);

        AudioManager.Instance.PlaySound(SoundSkillHeal, skillSoundVolume);
        PlayBackgroundEffect(healEffect);

        float elapsed = 0f;
        float tickInterval = 1f;
        float nextTick = tickInterval;
        float healPerTick = skill_4_healAmount / (skill_4_duration / tickInterval);

        while (elapsed < skill_4_duration)
=======
        OnSkillsActive[3]?.Invoke(skill_4_duration, skill_4_cool);
        try
>>>>>>> origin/main
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
 * OnSkillEquipped(int) 는 획득·동기화로 스킬이 새로 열릴 때마다 1회 발생한다 —
 * SkillView 가 구독해 잠금 해제 표시에 사용. OptainSkill 은 멱등(이미 보유면 무시).
 *
 * SpawnGroundAfterDelay 의 AddComponent<SkillGroundMarker>() 한 줄 : 스킬2로 소환한 지형에
 * 식별용 빈 마커를 붙인다. 최종보스의 토 파동 투사체(SoilWave)가 이 마커를 보고 소멸한다
 * (속성 상성). 지형 프리팹은 수정하지 않았고 지형 동작에도 영향이 없다.
 *
 * 효과음 배선 — 인덱스/메서드 번호가 아니라 "실제 기능" 기준으로 붙였다.
 *   isSkillEquiped[0] / skill_1_key(A) / Do_skill_1  = attack.attackPower *= skill_1_increase → 공격력 버프
 *                                                      → Player_SkillAttackUp
 *   isSkillEquiped[1] / skill_2_key(S) / Do_changed_skill_2 = 이동/낙하 가속 + 조준 후 지형(장벽) 소환
 *                                                      → Player_SkillBarrier
 *   isSkillEquiped[2] / skill_3_key(D) / Do_skill_3  = attack.SetAttackSpeedMultiplier → 공격속도 버프
 *                                                      → Player_SkillAttackSpeed
 *   isSkillEquiped[3] / skill_4_key(F) / Do_skill_4  = health.Heal 틱 회복 → Player_SkillHeal
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
 * 키 바인딩(skill_N_key 기본값)은 A/S/D/F 순서라 "스킬1=A" 가정 자체는 맞다. 다만 내부 번호는
 * A=skill_1(공격력), S=skill_2(장벽), D=skill_3(공속), F=skill_4(회복) 로 유저가 말한 순서와 다르다.
 *
 * 제거한 파티클 : skill_1_auraEffect / skill_2_auraEffect / skill_3_auraEffect / skill_4_HealEffect
 * (public ParticleSystem 필드 4개와 Awake 의 초기 Stop, 각 코루틴의 Play/Stop 호출을 전부 삭제했다.
 *  Player.prefab 에 남아 있는 옛 참조는 필드가 사라져 그냥 무시된다 — 프리팹은 건드리지 않았다.)
 *
 * 배경 이펙트(회복/공속/공격력) : BuildBackgroundEffect 가 Awake 에서 플레이어의 자식으로
 * SpriteRenderer + SpriteSequencePlayer 오브젝트를 하나씩 만들어 두고 꺼 둔다(코드 생성, 프리팹 무수정).
 *  - "플레이어 뒤" 보장 : 플레이어 루트의 SpriteRenderer 에서 sortingLayerID 를 그대로 복사하고
 *    sortingOrder 를 (플레이어 order − backgroundEffectOrderOffset) 으로 잡는다. 같은 정렬 레이어
 *    안에서 order 가 작으면 반드시 뒤에 그려지므로, 레이어가 어떻게 설정돼 있든 항상 뒤에 깔린다.
 *    오프셋은 최소 1 로 클램프해 같은 order 로 겹쳐 z-파이팅이 나는 일이 없다.
 *  - 발동 시 PlayBackgroundEffect 가 SetActive(false)→(true) 로 토글한다. SpriteSequencePlayer 가
 *    OnEnable 에서 항상 1프레임부터 재생하는 관례(Gold 보스 이펙트와 동일)를 그대로 쓴 것이다.
 *    loop = false, deactivateOnComplete = true 이므로 1회 재생 후 스스로 꺼진다(루프 없음).
 *  - 스킬 지속시간과는 무관한 발동 연출이다. 지속시간 내내 유지되는 오라가 필요하면
 *    SetSequence 의 loop 인자를 켜고 종료 시점에 SetActive(false) 를 걸어야 한다(현재는 요구사항대로 1회).
 * 장벽 이펙트는 소환된 지형 쪽에 붙는다 — SkillGroundMarker.ApplyBarrierVisual 참고.
 * F(금) 는 스프라이트 시트 + F_Skill_Animation.anim 형태지만, 시트가 이미 Hit-Yellow_0~14 로
 * 슬라이스돼 있고 .anim 도 그 15장을 12fps 로 늘어놓은 PPtr 커브일 뿐이라 Animator 없이
 * 나머지 3종과 똑같이 SpriteSequencePlayer 로 재생한다(.anim 은 사용하지 않는다).
 */

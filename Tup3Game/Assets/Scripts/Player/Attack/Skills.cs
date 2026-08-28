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
    public ParticleSystem skill_1_auraEffect;
    public float Skill1CooldownRemaining { get; private set; }
    public float Skill1CooldownTotal => skill_1_cool;

    [Header("2번 스킬설정 (이동속도/낙하 버프)")]
    public float skill_2_haste = 1.2f;
    public float skill_2_duration = 10f;
    public float skill_2_cool = 10f;
    public ParticleSystem skill_2_auraEffect;
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
    public ParticleSystem skill_3_auraEffect;
    public bool IsSkill3Active { get; private set; }
    public float Skill3CooldownRemaining { get; private set; }
    public float Skill3CooldownTotal => skill_3_cool;


    [Header("4번 스킬설정 (힐량)")]
    public float skill_4_healAmount = 5f;
    public float skill_4_cool = 10f;
    public float skill_4_duration = 5f;
    public ParticleSystem skill_4_HealEffect;
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

    [Header("스킬 키 세팅")]
    public KeyCode skill_1_key = KeyCode.A;
    public KeyCode skill_2_key = KeyCode.S;
    public KeyCode skill_3_key = KeyCode.D;
    public KeyCode skill_4_key = KeyCode.F;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        movement = GetComponent<Playermovement>();
        attack = GetComponent<ComboAttack>();
        health = GetComponent<PlayerHealth>();
        PrepareSkill2AimMarker();

        if (skill_1_auraEffect != null)
            skill_1_auraEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (skill_2_auraEffect != null)
            skill_2_auraEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (skill_3_auraEffect != null)
            skill_3_auraEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (skill_4_HealEffect != null)
            skill_4_HealEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        SceneController.Instance.RegisterListener(this);
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

        if (skill_1_auraEffect != null)
            skill_1_auraEffect.Play();

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
        
        if (skill_1_auraEffect != null)
            skill_1_auraEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        yield return RunCooldown(skill_1_cool, v => Skill1CooldownRemaining = v);

        canUseSkill_1 = true;
    }

   


    private IEnumerator Do_changed_skill_2()
    {
        canUseSkill_2 = false;
        OnSkillsActive[1]?.Invoke(skill_2_duration, skill_2_cool);
        float originalSpeed = movement.moveSpeed;
        float originalGravity = movement.fallGravityMultiplier;

        try
        {
            movement.moveSpeed *= skill_2_haste;
            movement.fallGravityMultiplier *= skill_2_haste;

            StartAimingSkill2();

            if (skill_2_auraEffect != null)
                skill_2_auraEffect.Play();
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

        if (skill_2_auraEffect != null)
            skill_2_auraEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);

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

            if (!spawnedGround.TryGetComponent<SkillGroundMarker>(out _))
                spawnedGround.AddComponent<SkillGroundMarker>();

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

        try
        {
            attack.SetAttackSpeedMultiplier(skill_3_attackSpeedMultiplier);

            if (skill_3_auraEffect != null)
                skill_3_auraEffect.Play();

            yield return new WaitForSeconds(skill_3_duration);
        }
        finally
        {
            attack.SetAttackSpeedMultiplier(1f);
            IsSkill3Active = false;

            if (skill_3_auraEffect != null)
                skill_3_auraEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        yield return RunCooldown(skill_3_cool, v => Skill3CooldownRemaining = v);

        canUseSkill_3 = true;
    }

   

    private IEnumerator Do_skill_4()
    {
        canUseSkill_4 = false;
        OnSkillsActive[3]?.Invoke(skill_4_duration, skill_4_cool);
        try
        {
            if (skill_4_HealEffect != null)
                skill_4_HealEffect.Play();

            float elapsed = 0f;
            float tickInterval = 1f;
            float nextTick = tickInterval;
            float healPerTick = skill_4_healAmount / (skill_4_duration / tickInterval);

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
            if (skill_4_HealEffect != null)
            skill_4_HealEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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
 */

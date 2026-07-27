using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Playermovement))]
[RequireComponent(typeof(ComboAttack))]
[RequireComponent(typeof(PlayerHealth))]
public class Skills : MonoBehaviour
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

    private float skill_2_aimOffsetX = 0f;
    private Vector2 skill_2_currentAimPoint;
    private bool skill_2_hasValidAimPoint = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        movement = GetComponent<Playermovement>();
        attack = GetComponent<ComboAttack>();
        health = GetComponent<PlayerHealth>();

        if (skill_1_auraEffect != null)
            skill_1_auraEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (skill_2_auraEffect != null)
            skill_2_auraEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (skill_3_auraEffect != null)
            skill_3_auraEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (skill_4_HealEffect != null)
            skill_4_HealEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A) && canUseSkill_1)
        {
            StartCoroutine(Do_skill_1());
        }

        if (Input.GetKeyDown(KeyCode.S) && canUseSkill_2)
        {
            StartCoroutine(Do_changed_skill_2());
        }
        if (Input.GetKeyDown(KeyCode.D) && canUseSkill_3)
        {
            StartCoroutine(Do_skill_3());
        }

        if (Input.GetKeyDown(KeyCode.F) && canUseSkill_4)
        {
            StartCoroutine(Do_skill_4());
        }

        if (isAiming)
        {
            UpdateAimSkill2();

            if (Input.GetKeyUp(KeyCode.S))
            {
                StopAimingAndSpawnSkill2();
            }
        }
    }


    private IEnumerator Do_skill_1()
    {
        canUseSkill_1 = false;


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

        if (skill_2_aimMarker != null)
            skill_2_aimMarker.gameObject.SetActive(true);
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

        if (skill_2_aimMarker != null && skill_2_hasValidAimPoint)
        {
            skill_2_aimMarker.position = skill_2_currentAimPoint;
        }
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

            yield return new WaitForSeconds(skill_2_duration);
            if (spawnedGround != null)
            {
                Destroy(spawnedGround);
            }
        }
    }

    private IEnumerator Do_skill_3()
    {
        canUseSkill_3 = false;
        IsSkill3Active = true;

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

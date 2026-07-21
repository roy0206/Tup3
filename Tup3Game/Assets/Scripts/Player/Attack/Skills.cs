using System.Collections;
using Unity.Burst.Intrinsics;
using UnityEngine;

public class Skills : MonoBehaviour
{
    private Playermovement movement;
    private ComboAttack attack;

    [Header("1번 스킬설정")]
    public float skill_1_increase = 1.5f;
    public float skill_1_duration = 10f;
    public float skill_1_cool = 10f;

    [Header("변환 1번 스킬 설정")]
    public float changed_skill_1_increase = 1.75f;
    public float changed_skill_1_duration = 10f;
    public float changed_skill_1_cool = 10f;

    [Header("2번 스킬설정 (이동속도/낙하 버프)")]
    public float skill_2_haste = 1.2f;
    public float skill_2_duration = 10f;
    public float skill_2_cool = 10f;

    [Header("변환 2번 스킬 설정")]
    private bool isAiming = false;
    public bool isTransformed = false;
    public float changed_skill_2_cool = 10f;
    public float skill_2_aimRange = 10f;
    public float skill_2_aimMoveSpeed = 8f;
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

    private bool canUseSkill_1 = true;
    private bool canUseSkill_2 = true;
    private bool canUseSkill_3 = true;
    private bool canUse_Changed_Skill_1 = true;
    private bool canUse_Changed_Skill_2 = true;
    private bool canUse_Changed_Skill_3 = true;


    private float skill_2_aimOffsetX = 0f;
    private Vector2 skill_2_currentAimPoint;
    private bool skill_2_hasValidAimPoint = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        movement = GetComponent<Playermovement>();
        attack = GetComponent<ComboAttack>();
        skill_3_auraEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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
            if (isTransformed)
                StartCoroutine(Do_changed_skill_2());
            else
                StartCoroutine(Do_skill_2());
        }
        if (Input.GetKeyDown(KeyCode.D) && canUseSkill_3)
        {
            StartCoroutine(Do_skill_3());
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
        yield return new WaitForSeconds(skill_1_cool);

        canUseSkill_1 = true;
    }

    private IEnumerator Do_skill_2()
    {
        canUseSkill_2 = false;

        float originalSpeed = movement.moveSpeed;
        float originalGravity = movement.fallGravityMultiplier;
        try
        {
            movement.moveSpeed *= skill_2_haste;
            movement.fallGravityMultiplier *= skill_2_haste;
        yield return new WaitForSeconds(skill_2_duration);
        }
        finally
        {
            movement.moveSpeed = originalSpeed;
            movement.fallGravityMultiplier = originalGravity;
        }
        yield return new WaitForSeconds(skill_2_cool);
       
        canUseSkill_2 = true;
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

        yield return new WaitForSeconds(skill_2_cool);
        canUseSkill_2 = true;
    }

    private void StartAimingSkill2()
    {
        isAiming = true;
        skill_2_aimOffsetX = 0f;

        if (skill_2_aimMarker != null)
            skill_2_aimMarker.gameObject.SetActive(true);
    }

    private void UpdateAimSkill2()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        skill_2_aimOffsetX += horizontalInput * skill_2_aimMoveSpeed * Time.deltaTime;
        skill_2_aimOffsetX = Mathf.Clamp(skill_2_aimOffsetX, -skill_2_aimRange, skill_2_aimRange);

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

        attack.SetAttackSpeedMultiplier(skill_3_attackSpeedMultiplier);

        float originalDamage = attack.attackPower;

        if (skill_3_auraEffect != null)
            skill_3_auraEffect.Play();

        yield return new WaitForSeconds(skill_3_duration);

        attack.SetAttackSpeedMultiplier(1f);
        IsSkill3Active = false;

        if (skill_3_auraEffect != null)
            skill_3_auraEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        yield return new WaitForSeconds(skill_3_cool);
        canUseSkill_3 = true;
    }
    public bool IsAiming => isAiming;
}

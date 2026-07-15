using UnityEngine;
using System.Collections;

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

    [Header("2번 스킬설정")]
    public float skill_2_haste = 1.2f;
    public float skill_2_duration = 10f;
    public float skill_2_cool = 10f;

    [Header("변환 2번 스킬 설정")]
    public float changed_skill_2_cool = 10f;

    private bool canUseSkill_1 = true;
    private bool canUseSkill_2 = true;
    private bool canUseSkill_3 = true;
    private bool canUse_Changed_Skill_1 = true;
    private bool canUse_Changed_Skill_2 = true;
    private bool canUse_Changed_Skill_3 = true;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        movement = GetComponent<Playermovement>();
        attack = GetComponent<ComboAttack>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A) && canUseSkill_2)
        {
            StartCoroutine(Do_skill_1());
        }
        if (Input.GetKeyDown(KeyCode.S) && canUseSkill_2)
        {
            StartCoroutine(Do_skill_2());
        }
    }


    private IEnumerator Do_skill_1()
    {
        canUseSkill_1 = false;

        float originalDamage = attack.attackPower;
        attack.attackPower *= skill_1_increase;

        yield return new WaitForSeconds(skill_1_duration);
        attack.attackPower = originalDamage;
        yield return new WaitForSeconds(skill_1_cool);

        canUseSkill_1 = true;
    }

    private IEnumerator Do_skill_2()
    {
        canUseSkill_2 = false;

        float originalSpeed = movement.moveSpeed;
        float originalGravity = movement.fallGravityMultiplier;

        movement.moveSpeed *= skill_2_haste;
        movement.fallGravityMultiplier *= skill_2_haste;
        yield return new WaitForSeconds(skill_2_duration);

        movement.moveSpeed = originalSpeed;
        movement.fallGravityMultiplier = originalGravity;
        yield return new WaitForSeconds(skill_2_cool);
       
        canUseSkill_2 = true;
    }
}

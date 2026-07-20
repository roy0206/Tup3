using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Playermovement))]

public class ComboAttack : MonoBehaviour
{

    public float currentDamage { get; private set; }
    public int maxCombo = 3;

    [Header("공격 이펙트")]
    public Attack_animation attackEffect;

    [Header("공격 설정")]
    public BoxCollider2D attackCollider;
    public float comboDelay = 0.08f;
    public float comboInputWindow = 0.4f;
    public float attackPower = 10f;

    [Header("콤보 설정")]
    public float attack1Distance;   // 1단 전진 거리 (캐릭터 크기의 1/3 → 인스펙터에서 조절)
    public float attack2Distance;   // 2단 전진 거리
    public float attack3Distance;
    public float attack1Duration = 0.3f;  // 1단 시전 시간
    public float attack2Duration = 0.3f;  // 2단 시전 시간
    public float attack3ChargeTime = 0.2f;  // 3단 준비 동작
    public float attack3Duration = 0.4f;

    private bool isAttacking = false;
    private bool isLunging = false;
    private bool comboQueued = false;
    private int comboStep = 0;

    private Playermovement movement;

    public bool IsLunging => isLunging;
    void Awake()
    {
        movement = GetComponent<Playermovement>();
        attackCollider.enabled = false;


    }
    void Start()
    {
        if (attack1Distance == 0) attack1Distance = movement.BodySizeX / 3f;
        if (attack2Distance == 0) attack2Distance = movement.BodySizeX / 3f;
        if (attack3Distance == 0) attack3Distance = movement.BodySizeX;
    }
    // Update is called once per frame
    void Update()
    {
        if (movement.IsDashing())
            return;

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!isAttacking)
            {
             
                StartCoroutine(Comboattack());
                Debug.Log("콤보스탭은" + comboStep);
            }
            else
                comboQueued = true;
        }
    }
    private IEnumerator Comboattack()
    {
        isAttacking = true;
        comboStep = 0;
        comboQueued = false;
        try
        {
            while (true)
            {
                comboStep++;
                Debug.Log("현재 콤보 수: " + comboStep);
                float facingDirection = movement.GetFacingDirection();
                int num = comboStep;
               
                
                // 바라보는 방향으로 공격

                Vector2 pos = attackCollider.transform.localPosition;
                pos.x = Mathf.Abs(pos.x) * facingDirection;
                attackCollider.transform.localPosition = pos;

                // 여기서 comboStep에 따라 애니메이션 트리거, 데미지, 히트박스 크기 등을 다르게 조작

                float bodyLength = movement.BodySizeX;
                if (movement.animator != null)
                { 
                    movement.animator.SetTrigger("AttackTrigger");
                    movement.animator.SetInteger("AttackIndex", comboStep);
                }

                if (attackEffect != null)
                {
                    attackEffect.PlayEffect(comboStep, facingDirection);
                }

                switch (comboStep)
                {
                    case 1:
                        currentDamage = attackPower * 1.0f;
                        isLunging = true;
                        yield return StartCoroutine(DashForward(attack1Distance, attack1Duration, facingDirection));
                        attackEffect.HideEffect();
                        isLunging = false;
                        break;
                    case 2:
                        currentDamage = attackPower * 1.0f;
                        isLunging = true;
                        yield return StartCoroutine(DashForward(attack2Distance, attack2Duration, facingDirection));
                        attackEffect.HideEffect();
                        isLunging = false;
                        break;
                    case 3:
                        currentDamage = attackPower * 2.5f;
                        isLunging = true;
                        yield return new WaitForSeconds(attack3ChargeTime);
                        yield return StartCoroutine(DashForward(attack3Distance, attack3Duration, facingDirection));
                        attackEffect.HideEffect();
                        isLunging = false;
                        break;

                }

                // 막타였으면 종료
                if (comboStep >= maxCombo)
                    break;
                yield return new WaitForSeconds(comboDelay);

                // 입력 대기 창: comboInputWindow (콤보공격인정시간) 안에 예약이 들어오면 다음 타로
                float timer = 0f;
                while (!comboQueued && timer < comboInputWindow)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }

                // 시간초과 > 콤보 종료
                if (!comboQueued)
                    break;
                comboQueued = false;
            }
        }
        finally
        {
            attackEffect.HideEffect();
            comboQueued = false;
            comboStep = 0;
            isAttacking = false;
        }
    }


    private IEnumerator DashForward(float distance, float duration, float direction)
    {
        attackCollider.enabled = true;

        float traveled = 0f;
        float speed = distance / duration;

        while (traveled < distance)
        {
            float step = speed * Time.deltaTime;
            step = Mathf.Min(step, distance - traveled);

            movement.Move(new Vector2(direction * step, 0f));

            traveled += step;
            yield return null;
        }
        attackCollider.enabled = false;
    }
}

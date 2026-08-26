
using System;
using UnityEngine;
using DG.Tweening;

public class FlyingSword : MonoBehaviour
{
    [SerializeField] private float speed;

    [Header("쳐내기 / 반사")]
    [SerializeField] private float parryDetectRadius = 0.6f;
    [SerializeField] private float reflectedSpeed = 16f;
    [SerializeField] private float reflectedLifeTime = 4f;
    [SerializeField] private float bossHitRadius = 1f;

    private PlayerKnockBack player;
    private Collider2D playerCollider;
    private Gold boss;
    private FinalBoss finalBoss;
    private Hitbox hitbox;
    BoxCollider2D bc;
    bool isStoped = false;
    bool isFixed = false;
    bool isReflected = false;
    float reflectedTimer;


    public float Timer { get; set; }
    private void OnEnable()
    {
        player = FindAnyObjectByType<PlayerKnockBack>();
        playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
        boss = FindAnyObjectByType<Gold>();
        finalBoss = boss != null ? null : FindAnyObjectByType<FinalBoss>();
        bc = GetComponent<BoxCollider2D>();
        hitbox = GetComponent<Hitbox>();
        if (hitbox != null) hitbox.enabled = true;
        bc.enabled = true;
        isStoped = false;
        isFixed = false;
        isReflected = false;
        reflectedTimer = 0f;

        int randLength = UnityEngine.Random.Range(3, 4);
        float randAngle = UnityEngine.Random.Range(30f, 150f);
        Vector3 targetPos = transform.position + new Vector3(randLength * Mathf.Cos(randAngle *  Mathf.Deg2Rad), randLength * Mathf.Sin(randAngle *  Mathf.Deg2Rad));
        transform.DOMove(targetPos, 1f);
        Timer = UnityEngine.Random.Range(3f, 5f);
    }

    private void OnDisable()
    {
        transform.DOKill();
    }

    private void FixedUpdate()
    {
        if (PauseManager.IsPaused) return;

        if (isReflected)
        {
            UpdateReflected();
            return;
        }

        Timer -= Time.fixedDeltaTime;

        if (!isStoped && DetectParry())
        {
            Reflect();
            return;
        }

        if (player == null) return;

        var vec = PlayerAimPoint - transform.position;
        if (!isFixed)
        {
            var angle = Mathf.Atan2(vec.y, vec.x) * Mathf.Rad2Deg;
            if (angle > transform.eulerAngles.y)
            {
                transform.rotation = Quaternion.Euler(0,0, Mathf.Lerp(transform.eulerAngles.z,angle, Time.fixedDeltaTime));
            }
            else
            {
                transform.rotation = Quaternion.Euler(0,0, Mathf.Lerp(angle, transform.eulerAngles.z, Time.fixedDeltaTime));
            }

        }
        if (Timer <= 0f && !isStoped)
        {
            CheckGround();
            isFixed = true;
            transform.Translate(Vector2.right * speed * Time.fixedDeltaTime, Space.Self);
        }
    }

    void CheckGround()
    {
        if(isStoped) return;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 0.2f);
        foreach (var col in colliders)
        {
            if (col.gameObject.layer == 6)
            {
                isStoped = true;
                bc.enabled = false;
            }
        }
    }

    private Vector3 PlayerAimPoint =>
        playerCollider != null ? playerCollider.bounds.center : player.transform.position;

    private bool DetectParry()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, parryDetectRadius);
        foreach (var col in colliders)
        {
            if (IsPlayerAttack(col)) return true;
        }
        return false;
    }

    private bool IsPlayerAttack(Collider2D col)
    {
        if (col == null) return false;
        if (col.GetComponent<Attackhitbox>() != null) return true;
        return col.GetComponentInParent<Attackhitbox>() != null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isReflected || isStoped) return;
        if (!IsPlayerAttack(other)) return;
        Reflect();
    }

    private void Reflect()
    {
        if (isReflected) return;

        isReflected = true;
        isFixed = true;
        isStoped = false;
        reflectedTimer = reflectedLifeTime;
        transform.DOKill();
        if (hitbox != null) hitbox.enabled = false;
        if (bc != null) bc.enabled = true;
        AimAtBoss();
        Debug.Log("<color=#00FFFF>[금 보스] 날아드는 검 쳐내기 성공! 검이 보스에게 되돌아간다</color>");
    }

    private Transform BossTransform =>
        boss != null ? boss.transform : finalBoss != null ? finalBoss.transform : null;

    private bool IsBossGone =>
        BossTransform == null || (boss != null && boss.IsDead) || (finalBoss != null && finalBoss.IsDead);

    private void NotifyBossHit()
    {
        if (boss != null) boss.NotifyReflectedSwordHit();
        else if (finalBoss != null) finalBoss.NotifyReflectedSwordHit();
    }

    private void AimAtBoss()
    {
        Transform target = BossTransform;
        if (target == null) return;
        Vector3 dir = target.position - transform.position;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    private void UpdateReflected()
    {
        reflectedTimer -= Time.fixedDeltaTime;

        if (IsBossGone || reflectedTimer <= 0f)
        {
            ReleaseSelf();
            return;
        }

        Vector3 dir = BossTransform.position - transform.position;
        if (dir.sqrMagnitude <= bossHitRadius * bossHitRadius)
        {
            NotifyBossHit();
            ReleaseSelf();
            return;
        }

        AimAtBoss();
        transform.Translate(Vector2.right * (reflectedSpeed * Time.fixedDeltaTime), Space.Self);
    }

    private void ReleaseSelf()
    {
        isReflected = false;
        transform.DOKill();
        if (hitbox != null) hitbox.enabled = true;

        if (PoolManager.Instance != null) PoolManager.Instance.Release(gameObject);
        else gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, parryDetectRadius);
    }
}

/* [파일 노트]
 *
 * 패턴3(날아드는 검)의 카운터플레이 담당.
 *
 * 흐름
 *   1) 소환 직후엔 기존대로 랜덤 위치로 떠올랐다가(DOMove) Timer 가 끝나면 플레이어를 향해 직진한다.
 *      조준점은 플레이어 피벗(transform.position, 발밑)이 아니라 콜라이더 중심(bounds.center)이다.
 *      피벗을 조준하면 검이 몸통 앞 바닥에 먼저 박혀 데미지가 안 들어가는 경우가 있었다.
 *   2) 날아오는 도중(정확히는 땅에 박히기 전 언제든) 플레이어 공격 히트박스와 겹치면 쳐내기 성공.
 *      감지는 두 갈래다.
 *        - FixedUpdate 의 Physics2D.OverlapCircleAll(parryDetectRadius) → Attackhitbox 탐색
 *        - OnTriggerEnter2D → Attackhitbox 탐색
 *      OverlapCircleAll 쪽이 주력이다. 플레이어의 공격 콜라이더는 휘두르는 순간에만 enabled 가 되고
 *      비활성 콜라이더는 오버랩 질의에 잡히지 않으므로, 별도 타이밍 처리 없이 "휘두르는 중"만 걸러진다.
 *      트리거/리지드바디 세팅에 의존하지 않아 프리팹 설정이 바뀌어도 동작한다.
 *   3) 쳐내기 성공 시 Reflect(): 진행 트윈 Kill, Hitbox(플레이어 피해 판정) 비활성,
 *      보스 방향으로 회전 후 reflectedSpeed 로 유도 비행.
 *   4) 보스와의 거리가 bossHitRadius 이내가 되면 Gold.NotifyReflectedSwordHit() 를 호출하고 즉시 반납.
 *      보스 체력은 깎지 않는다(그로기만이 유일한 피해 구간이라는 원칙). 5회 누적은 Gold 가 센다.
 *   5) reflectedLifeTime 안에 도달하지 못하거나 보스가 사망하면 그냥 반납한다.
 *
 * 반사된 검은 유도(매 FixedUpdate 재조준)라 사실상 반드시 보스에 명중한다. 직선으로 날려 빗나갈 수
 * 있게 하려면 UpdateReflected 의 AimAtBoss() 호출만 빼면 된다.
 *
 * Pattern3 은 소환 시 PoolManager.Release(sword, 10f) 로 지연 반납을 예약해 두는데,
 * PoolManager 의 Release 는 중복 호출과 세대(generation) 검사가 되어 있어 여기서 먼저 반납해도 안전하다.
 *
 * 최종보스(FinalBoss) 호환 : OnEnable 에서 Gold 를 먼저 찾고 없으면 FinalBoss 를 찾는다.
 * 반사 유도/명중 통지는 BossTransform / NotifyBossHit 헬퍼가 두 보스를 공통 처리한다.
 * FinalBoss 쪽 NotifyReflectedSwordHit 은 enableGroggy 가 꺼져 있으면 카운트만 하고 보상이 없다
 * (패링 = 해당 검 무효화만). 둘 다 없는 씬이면 반사된 검은 즉시 반납된다.
 */

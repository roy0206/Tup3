
using System;
using UnityEngine;
using DG.Tweening;

public class FlyingSword : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float aimLerpSpeed = 3f;

    [Header("쳐내기 / 반사")]
    [SerializeField] private float parryDetectRadius = 0.6f;
    [SerializeField] private float reflectedSpeed = 16f;
    [SerializeField] private float reflectedLifeTime = 4f;
    [SerializeField] private float bossHitRadius = 1f;

    [Header("발사 경로 위협 히트박스")]
    [SerializeField] private bool showLaunchThreatHitbox = true;
    [SerializeField] private float launchThreatLength = 20f;
    [SerializeField] private float launchThreatWidth;
    [SerializeField] private Color launchThreatColor = new Color(1f, 0.08f, 0.02f, 0.4f);
    [Range(0f, 1f)]
    [SerializeField] private float launchThreatFillAlpha = 0.7f;
    [SerializeField] private float launchThreatInset = 0.05f;
    [SerializeField] private float launchThreatPulseSpeed = 20f;
    [SerializeField] private int launchThreatSortingOrderOffset = -1;

    [Header("사운드")]
    [SerializeField] private float clashVolume = 1f;
    [SerializeField] private float clashMinInterval = 0.08f;

    private const string ClashSound = "Sword_Clash";

    private PlayerKnockBack player;
    private Collider2D playerCollider;
    private Gold boss;
    private FinalBoss finalBoss;
    private Hitbox hitbox;
    private SpriteRenderer swordRenderer;
    private ThreatHitboxVisual launchThreatVisual;
    BoxCollider2D bc;
    bool isStoped = false;
    bool isFixed = false;
    bool isReflected = false;
    float reflectedTimer;
    float launchThreatDuration;


    public float Timer { get; set; }
    private void OnEnable()
    {
        player = FindAnyObjectByType<PlayerKnockBack>();
        playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
        boss = FindAnyObjectByType<Gold>();
        finalBoss = boss != null ? null : FindAnyObjectByType<FinalBoss>();
        bc = GetComponent<BoxCollider2D>();
        hitbox = GetComponent<Hitbox>();
        if (swordRenderer == null) swordRenderer = GetComponentInChildren<SpriteRenderer>(true);
        launchThreatVisual = GetComponent<ThreatHitboxVisual>();
        if (launchThreatVisual == null)
            launchThreatVisual = gameObject.AddComponent<ThreatHitboxVisual>();
        launchThreatVisual.Configure(
            swordRenderer,
            launchThreatColor,
            launchThreatFillAlpha,
            launchThreatInset,
            launchThreatPulseSpeed,
            launchThreatSortingOrderOffset);

        if (hitbox != null) hitbox.enabled = false;
        if (bc != null) bc.enabled = true;
        isStoped = false;
        isFixed = false;
        isReflected = false;
        reflectedTimer = 0f;

        int randLength = UnityEngine.Random.Range(3, 4);
        float randAngle = UnityEngine.Random.Range(30f, 150f);
        Vector3 targetPos = transform.position + new Vector3(randLength * Mathf.Cos(randAngle *  Mathf.Deg2Rad), randLength * Mathf.Sin(randAngle *  Mathf.Deg2Rad));
        transform.DOMove(targetPos, 1f);
        Timer = UnityEngine.Random.Range(3f, 5f);
        launchThreatDuration = Timer;
        ShowLaunchThreatHitbox();
    }

    private void OnDisable()
    {
        transform.DOKill();
        if (launchThreatVisual != null) launchThreatVisual.Hide();
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
            // LerpAngle 을 써야 한다 — eulerAngles.z 는 0~360 으로 읽히므로 음수 목표각(아래 조준)을
            // 일반 Lerp 로 섞으면 조준이 목표보다 위에서 수렴한다(최종보스처럼 높은 곳에서
            // 아래로 조준할수록 오차가 커지던 버그).
            var angle = Mathf.Atan2(vec.y, vec.x) * Mathf.Rad2Deg;
            float smoothed = Mathf.LerpAngle(transform.eulerAngles.z, angle, aimLerpSpeed * Time.fixedDeltaTime);
            transform.rotation = Quaternion.Euler(0, 0, smoothed);
        }
        UpdateLaunchThreatHitbox();
        if (Timer <= 0f && !isStoped)
        {
            CheckGround();
            if (isStoped) return;
            if (!isFixed) Launch();
            transform.Translate(Vector2.right * speed * Time.fixedDeltaTime, Space.Self);
        }
    }

    private void ShowLaunchThreatHitbox()
    {
        if (!showLaunchThreatHitbox || launchThreatVisual == null || bc == null) return;

        float length = Mathf.Max(0.01f, launchThreatLength);
        float width = launchThreatWidth > 0f ? launchThreatWidth : bc.size.y;
        float startX = bc.offset.x + bc.size.x * 0.5f;
        launchThreatVisual.ShowLocalBox(
            new Vector2(length, Mathf.Max(0.01f, width)),
            new Vector2(startX + length * 0.5f, bc.offset.y),
            ThreatFillDirection.LeftToRight);
    }

    private void UpdateLaunchThreatHitbox()
    {
        if (launchThreatVisual == null || !launchThreatVisual.IsVisible) return;
        if (Timer <= 0f || launchThreatDuration <= 0f)
        {
            launchThreatVisual.Hide();
            return;
        }

        float elapsed = launchThreatDuration - Timer;
        launchThreatVisual.SetProgress(
            Mathf.Clamp01(elapsed / launchThreatDuration),
            Mathf.Max(0f, elapsed));
    }

    private void Launch()
    {
        isFixed = true;

        // 발사 순간 조준각을 정확히 스냅 — 보간이 덜 수렴했어도 조준점(콜라이더 중심)을 지나가게 보장.
        // 보간이 이미 수렴한 정상 상황에서는 사실상 변화가 없다.
        Vector3 vec = PlayerAimPoint - transform.position;
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(vec.y, vec.x) * Mathf.Rad2Deg);

        if (launchThreatVisual != null) launchThreatVisual.Hide();
        if (hitbox != null) hitbox.enabled = true;
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
        if (launchThreatVisual != null) launchThreatVisual.Hide();
        if (hitbox != null) hitbox.enabled = false;
        if (bc != null) bc.enabled = true;
        AimAtBoss();
        BossSound.PlayThrottled(ClashSound, clashVolume, clashMinInterval);
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
        if (launchThreatVisual != null) launchThreatVisual.Hide();
        if (hitbox != null) hitbox.enabled = false;

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
 *      대기 중에는 실제 BoxCollider2D 높이의 발사 통로를 검 앞에서 launchThreatLength 만큼 펼치고,
 *      Timer 진행률에 맞춰 검 쪽부터 플레이어 방향으로 채운다. 이 동안 Hitbox 는 꺼져 있어 떠오르는
 *      검에 미리 닿아도 피해가 없고, 통로가 가득 차 Launch()가 호출될 때만 피해 판정을 켠다.
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
 *
 * 사운드 Sword_Clash : Reflect() — 플레이어의 검이 날아오는 검을 쳐내는 바로 그 순간.
 * "검과 검이 부딪치는" 사건에 가장 정확히 대응하는 지점이라 여기를 골랐다.
 * 금보스 Parried() / 최종보스 OnParrySuccess() 의 Parry_Success 와 겹치지 않는다는 점도 근거다 —
 * 어검 쳐내기는 그 두 메서드를 거치지 않는다(금보스는 5회 누적돼야 Parried 로 가고, 최종보스는
 * enableGroggy 가 꺼져 있어 아예 가지 않는다). 즉 한 번의 쳐내기에 두 소리가 겹치는 경우가 없다.
 * clashMinInterval(기본 0.08초) : 검 5자루가 몰려 있을 때 한 번의 휘두르기가 여러 자루를 동시에
 * 쳐내면 같은 프레임에 Reflect 가 여러 번 일어날 수 있어 겹침을 막는다.
 */

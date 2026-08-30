using System;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;


public class Soil : BossBase
{
    private List<float> curTimes;
    [SerializeField] List<Transform> hitboxTransforms = new List<Transform>();
    private GameObject player;
    private bool isFacingRight;

    [Header("이동")]
    [SerializeField] private float moveSpeed = 3f;

    [SerializeField] private float gravity = -40f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 0.1f;

    private List<float> attackRange = new() { 0, 3, 100, 3, 3 };

    private BoxCollider2D bodyCollider;
    private float verticalVelocity;

    [SerializeField] private float pattern1Cooltime;
    [SerializeField] private float pattern2Cooltime;
    [SerializeField] private float pattern3Cooltime;

    [Header("사망 페이드")]
    [SerializeField] private bool fadeOnDeath = true;
    [SerializeField] private float deathFadeDelay = 0.6f;
    [SerializeField] private float deathFadeDuration = 1.2f;
    [SerializeField] private Ease deathFadeEase = Ease.InQuad;

    private Sequence deathFadeSequence;
    private bool deathFadeStarted;

    [Header("사운드")]
    [SerializeField] private float smashSoundVolume = 1f;
    [SerializeField] private float footstepSoundVolume = 0.6f;
    [SerializeField] private float footstepMinInterval = 0.4f;

    private const string SmashSound = "Soil_Smash";
    private const string FootstepSound = "Soil_Footstep";
    private const string DeathSound = "Soil_Death";

    private float footstepTimer;

    protected override string DefaultDeathSoundName => DeathSound;

    new void Awake()
    {
        base.Awake();
        behaviorTree = new BehaviorTreeBuilder(gameObject)
            .Selector("Root")
                .Sequence("DeadSecquence")
                    .Do("Dead",Dead)
                .End()
                .Selector("PatternSelector")
                    .Sequence("1")
                        .Do("Cool1", () => PatternStarter(1))
                        .Do("A1", Pattern1)
                    .End()
                    .Sequence("2")
                        .Do("Cool2", () => PatternStarter(2))
                        .Do("A2", Pattern2)
                    .End()
                    .Sequence("3")
                        .Do("Cool3", () => PatternStarter(3))
                        .Do("A3", Pattern3)
                    .End()
                .End()
                .Do("Go", Move)
                .Do("Stay", Stay)
            .End()
            .Build();
        curTimes = new List<float>()
        {
            0, pattern1Cooltime, pattern2Cooltime, pattern3Cooltime
        };

        animationController = GetComponent<AnimationController>();
        player = GameObject.FindGameObjectWithTag("Player");
        bodyCollider = boxColliders.Count > 0 ? boxColliders[0] : GetComponent<BoxCollider2D>();

        bool initiallyFacingRight = transform.localScale.x < 0f ||
            Mathf.Approximately(transform.rotation.eulerAngles.y, 180f);

        Vector3 initialScale = transform.localScale;
        initialScale.x = Mathf.Abs(initialScale.x);
        transform.localScale = initialScale;

        if (Mathf.Approximately(transform.rotation.eulerAngles.y, 180f))
        {
            transform.rotation = Quaternion.identity;
        }
        SetFacing(initiallyFacingRight);

        SnapToGround();
        if (!snappedToGround) StartCoroutine(SnapToGroundWhenReady());

        OnDeath += PlayDeathFade;
        OnDeath += DisableCollidersOnDeath;
    }

    private void OnDestroy()
    {
        OnDeath -= PlayDeathFade;
        OnDeath -= DisableCollidersOnDeath;

        if (deathFadeSequence == null) return;
        Sequence seq = deathFadeSequence;
        deathFadeSequence = null;
        seq.Kill();
    }

    private void PlayDeathFade()
    {
        if (!fadeOnDeath || deathFadeStarted) return;
        deathFadeStarted = true;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0) return;

        float delay = Mathf.Max(0f, deathFadeDelay);
        float duration = Mathf.Max(0.01f, deathFadeDuration);

        deathFadeSequence = DOTween.Sequence().SetTarget(this);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null) continue;
            deathFadeSequence.Insert(delay, renderer.DOFade(0f, duration).SetEase(deathFadeEase));
        }

        deathFadeSequence.OnComplete(() => deathFadeSequence = null);
    }

    // 사망 시 몸통·히트박스 콜라이더 전부 비활성 — 시체가 플레이어 이동을 막지 않게 한다.
    // 컴포넌트(enabled)만 끄므로 패턴의 지연 DelayedCall 이 히트박스 오브젝트를 SetActive(true)
    // 해도 콜라이더는 살아나지 않는다. 접지 검사는 bodyCollider 의 size/offset 을 직접 계산해
    // 비활성 상태에서도 유효하다(ApplyGravity 참고).
    private void DisableCollidersOnDeath()
    {
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
            col.enabled = false;
    }

    private IEnumerator SnapToGroundWhenReady()
    {
        for (int i = 0; i < 30 && !snappedToGround; i++)
        {
            yield return null;
            SnapToGround();
        }
    }

    private void Update()
    {
        if (PauseManager.IsPaused || DialogueManager.IsDialogueActive) return;

        for (int i = 0; i < curTimes.Count; i++)
        {
            curTimes[i] -= Time.deltaTime;
        }
        behaviorTree.Tick();
        ApplyGravity();
    }

    private TaskStatus Dead()
    {
        if(!IsDead) return TaskStatus.Failure;


        animationController.Play(0);
        gameObject.layer = LayerMask.GetMask("Default");

        return TaskStatus.Success;
    }

    private TaskStatus PatternStarter(int num)
    {
        if (curTimes[num] > 0) return TaskStatus.Failure;
        if (HorizontalDistance > attackRange[num]) return TaskStatus.Failure;

        // 패턴 선택은 Move/Stay보다 먼저 실행된다. 여기서 방향을 갱신하지 않으면
        // 플레이어가 등 뒤로 넘어간 직후에도 이전 정면을 향해 공격을 시작한다.
        FacePlayer();
        return TaskStatus.Success;
    }

    private bool isPatternSetup;
    private TaskStatus Pattern1()
    {
        if(IsDead) return TaskStatus.Failure;
        if (!isPatternSetup)
        {
            curTimes[1] = pattern1Cooltime;
            curTimes[0] = 2;
            animationController.Play(1);
            isPatternSetup = true;
            /*DOVirtual.DelayedCall(0.11f, () =>
            {
                hitboxTransforms[0].gameObject.SetActive(true);
            } );
            DOVirtual.DelayedCall(0.5f, () =>
            {
                hitboxTransforms[0].gameObject.SetActive(false);
            } );*/
            DOVirtual.DelayedCall(0.6f, () =>
            {
                hitboxTransforms[1].gameObject.SetActive(true);
                BossSound.Play(SmashSound, smashSoundVolume);
            } );
            DOVirtual.DelayedCall(0.7f, () =>
            {
                hitboxTransforms[2].gameObject.SetActive(true);
                hitboxTransforms[2]
                    .DOMoveX(hitboxTransforms[2].transform.position.x + (isFacingRight ? 10f : -10f), 1f)
                    .OnComplete(() =>{ hitboxTransforms[2].gameObject.SetActive(false);hitboxTransforms[2].localPosition = new Vector3(-2f, 2f, 0f); });
            } );
            DOVirtual.DelayedCall(0.9f, () =>
            {
                hitboxTransforms[1].gameObject.SetActive(false);
            } );
        }
        if (curTimes[0] > 0) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;

    }

    private IEnumerator SoilDrop()
    {
        for (int i = 0; i < 12; i++)
        {
            yield return PauseManager.WaitWhilePaused();

            var bossX = transform.position.x;
            bool bossFlip = isFacingRight;
            Vector2 position;
            if(bossFlip)
                position = new Vector2(UnityEngine.Random.Range(bossX -2f, bossX + 10f), 5);
            else 
                position = new Vector2(UnityEngine.Random.Range(bossX -10f, bossX + 2f), 5);
            var drop = PoolManager.Instance.Get("SoilDrop", position, Quaternion.identity);
            PoolManager.Instance.Release(drop, 4f);

            yield return new WaitForSeconds(0.4f);
        }
    }

    
    private TaskStatus Pattern2()
    {
        if(IsDead) return TaskStatus.Failure;
        if (!isPatternSetup)
        {
            curTimes[2] = pattern2Cooltime;
            curTimes[0] = 5;
            animationController.Play(2);
            isPatternSetup = true;
            DOVirtual.DelayedCall(0.5f, () => StartCoroutine(SoilDrop()));
            

        }
        if (curTimes[0] > 0) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;

    }
    
        
    private TaskStatus Pattern3()
    {
        if(IsDead) return TaskStatus.Failure;
        if (!isPatternSetup)
        {
            curTimes[3] = pattern3Cooltime;
            curTimes[0] = 2;
            animationController.Play(3);
            isPatternSetup = true;
            DOVirtual.DelayedCall(0.7f, () =>
            {
                hitboxTransforms[3].gameObject.SetActive(true);
            } );
            DOVirtual.DelayedCall(1f, () =>
            {
                hitboxTransforms[3].gameObject.SetActive(false);
            } );
            
        }
        if (curTimes[0] > 0) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;

    }
    
    private float HorizontalDistance => Mathf.Abs(player.transform.position.x - transform.position.x);

    private TaskStatus Move()
    {
        if (HorizontalDistance <= attackRange[4]) return TaskStatus.Failure;

        animationController.Play(5);
        float dir = Mathf.Sign(player.transform.position.x - transform.position.x);
        Face(dir);
        transform.Translate(Vector3.right * (dir * moveSpeed * Time.deltaTime), Space.World);
        PlayFootstep();
        return TaskStatus.Success;
    }

    private void PlayFootstep()
    {
        footstepTimer -= Time.deltaTime;
        if (footstepTimer > 0f) return;

        footstepTimer = Mathf.Max(0.05f, footstepMinInterval);
        BossSound.Play(FootstepSound, footstepSoundVolume);
    }

    private TaskStatus Stay()
    {
        animationController.Play(4);
        FacePlayer();
        return TaskStatus.Success;
    }

    private void FacePlayer()
    {
        if (player == null) return;
        Face(Mathf.Sign(player.transform.position.x - transform.position.x));
    }
    
    private void Face(float dir)
    {
        if (Mathf.Approximately(dir, 0f)) return;
        SetFacing(dir > 0f);
    }

    private void SetFacing(bool facingRight)
    {
        if (facingRight == isFacingRight) return;

        isFacingRight = facingRight;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? -1f : 1f);
        transform.localScale = scale;
    }

    [SerializeField] private float fallRescueDepth = 12f;
    private Vector3 lastGroundedPosition;
    private bool hasGroundedPosition;
    private bool snappedToGround;

    private Bounds GroundProbeBounds()
    {
        if (bodyCollider != null)
        {
            Vector3 s = transform.lossyScale;
            Vector2 center = (Vector2)transform.position
                + new Vector2(bodyCollider.offset.x * s.x, bodyCollider.offset.y * s.y);
            Vector2 size = new Vector2(
                Mathf.Abs(bodyCollider.size.x * s.x),
                Mathf.Abs(bodyCollider.size.y * s.y));
            return new Bounds(center, size);
        }

        return new Bounds(transform.position, Vector3.one);
    }

    private bool IsOwnCollider(Collider2D col)
    {
        return col != null && col.transform.IsChildOf(transform);
    }

    private RaycastHit2D GroundCast(Vector2 origin, Vector2 size, float distance)
    {
        foreach (var hit in Physics2D.BoxCastAll(origin, size, 0f, Vector2.down, distance, groundMask))
        {
            if (!IsOwnCollider(hit.collider)) return hit;
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
    }

    private void RememberGrounded()
    {
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

        verticalVelocity += gravity * Time.deltaTime;
        transform.Translate(Vector3.up * (verticalVelocity * Time.deltaTime), Space.World);

        if (hasGroundedPosition && transform.position.y < lastGroundedPosition.y - fallRescueDepth)
        {
            transform.position = lastGroundedPosition;
            verticalVelocity = 0f;
        }
    }

}

/* [파일 노트]
 * 일시정지 대응 : Update 첫 줄 PauseManager.IsPaused 게이트로 BT/쿨타임/중력이 멈춘다.
 * 패턴의 지연 히트박스(DOVirtual.DelayedCall)와 이동 트윈은 PauseManager 의 DOTween.PauseAll 로 멈추고,
 * SoilDrop 소환 코루틴은 루프마다 WaitWhilePaused 로 일시정지 동안 추가 소환을 하지 않는다.
 *
 * 접지/중력 (FinalBoss 와 동일한 방식): Awake 에서 SnapToGround 로 지면 표면에 1회 스냅 —
 * BossRoom 이 도입 대사 동안 이 컴포넌트를 꺼둬도(Update 정지) 보스가 공중에 떠 있지 않게 한다.
 * ApplyGravity 는 낙하속도 비례 캐스트 + 착지 스냅 + 파묻힘 밀어올림으로 터널링을 막고,
 * 마지막 접지 위치를 기억해 fallRescueDepth 이상 낙하(콜라이더 없는 구멍)하면 복귀한다.
 * 접지 검사 박스는 bodyCollider 의 size/offset 을 스케일 반영해 직접 계산한다(비활성에도 유효).
 *
 * 좌우반전: 토보스는 본 리깅 캐릭터(Body 하위 다중 SpriteRenderer)라 flipX 로는 그래픽이 안
 * 뒤집힌다. 애니메이션 클립이 Body 자체의 위치/회전도 구동하므로 Body.localScale.x 를 음수로
 * 만들면 "애니메이션 회전 후 반전"이 되어 오른쪽 모션의 관절 궤적이 틀어진다. 대신 애니메이션
 * 루트보다 한 단계 위인 보스 transform.localScale.x 를 반전해 Body 이하의 완성된 포즈 전체를
 * 미러링한다. 같은 루트 아래의 히트박스도 자동으로 함께 뒤집히므로 개별 MirrorChild 는 쓰지 않는다.
 * 예전의 루트 Y축 180도 회전은 자식 체력바 캔버스까지 카메라 반대편으로 뒤집어 안 보이게 했지만,
 * 음수 X 스케일은 카메라 반대편으로 돌리지 않아 그 문제도 피한다.
 * 기본(스프라이트 원본)은 왼쪽 보기 = isFacingRight false 이고, 씬에 Y=180 으로 저장돼 있던 경우를
 * 위해 Awake 에서 회전을 identity 로 되돌리고 facing 상태로 변환한다. 패턴1의 전진 히트박스와
 * SoilDrop 낙하 방향도 회전 대신 isFacingRight 를 본다.
 * BT는 패턴 선택을 Move/Stay보다 먼저 검사하므로 PatternStarter 성공 시 FacePlayer를 먼저 호출한다.
 * 따라서 플레이어가 등 뒤로 넘어간 직후 패턴이 시작돼도 실제 위치를 향해 돌아본 뒤 공격하며,
 * 패턴이 이미 시작된 뒤에는 방향을 다시 바꾸지 않아 공격 후방으로 넘어가는 회피는 유지된다.
 *
 * 사운드
 *   Soil_Smash  : 패턴1(내려치기). 패턴 시작이 아니라 기존 0.6초 DelayedCall — 즉 첫 히트박스가
 *                 켜지는(= 팔이 땅에 닿는) 순간 — 안에서 1회 재생해 모션과 타격음을 맞춘다.
 *                 패턴1을 고른 근거 : 이 패턴이 내리치며 전방으로 전진하는 히트박스(파동)를 뿜는
 *                 동작이고, 최종보스의 "토 파동"이 같은 애니(SoilPattern1)와 같은 타임라인
 *                 (근접 0.6~0.9 / 파동 0.7)을 그대로 이식한 것이라 둘이 같은 소리를 공유한다.
 *   Soil_Death  : DefaultDeathSoundName 으로 BossBase 에 넘긴다(체력 0 시점 1회).
 *   Soil_Footstep : Move 태스크가 성공한 프레임마다 footstepMinInterval(기본 0.4초) 간격으로 재생.
 *                 Move 는 매 프레임 호출되므로 간격 제한이 없으면 초당 수십 번 울린다.
 *                 타이머는 이동한 프레임에만 줄어들어 멈춰 있는 동안에는 발소리가 나지 않는다.
 *   Soil_RockFall : 낙석 본체(SoilDrop.cs)가 스스로 재생한다.
 *
 * ── 사망 페이드 (2026-08-29 유저 요청) ────────────────────────────────────────
 * 체력이 0 이 되면 자식 SpriteRenderer 전부의 알파를 0 으로 내린다.
 * 구동은 BT 의 Dead() 태스크가 아니라 BossBase.OnDeath 구독이다 — Dead() 는 Tick 마다 불리는데
 * Update 가 PauseManager.IsPaused / DialogueManager.IsDialogueActive 에서 조기 return 하므로
 * 승리 대사가 시작되면 Tick 자체가 멈춘다. OnDeath 는 체력 0 시점에 정확히 1회 발생해 타이밍이 확실하다.
 * (deathFadeStarted 로 한 번 더 막아 둔다.)
 *
 * 페이드 대상은 GetComponentsInChildren<SpriteRenderer>(true) 전부다. 토보스는 본 리깅이라
 * 몸통 파츠가 자식 SR 여러 개로 쪼개져 있어 루트 하나만 건드리면 안 된다.
 *
 * 피격 점멸(SpriteFlashGroup)과 충돌하지 않는다 — 점멸은 MaterialPropertyBlock 의
 * _FlashAmount/_FlashColor 만 쓰고 SpriteRenderer.color 는 건드리지 않으며, 셰이더가 알파는
 * 원본(=여기서 낮추는 값)을 그대로 통과시키기 때문에 페이드가 그대로 먹는다.
 *
 * DOTween 기반이라 PauseManager 의 DOTween.PauseAll 에 함께 멈춘다.
 * 기본값 0.6초 대기 후 1.2초 페이드 = 총 1.8초로, BossRoom 의 victoryDelay(씬 값 2초) 안에 끝나
 * 승리 대사가 뜨는 시점에는 이미 사라져 있다. 대기 시간에 사망 애니메이션(animationController.Play(0))이
 * 보이도록 delay 를 둔 것이므로, 애니메이션 길이를 바꾸면 deathFadeDelay 도 같이 조정할 것.
 *
 * 오브젝트를 비활성화하거나 콜라이더를 끄지는 않는다 — 승리 처리(BossRoom)와 BossExit 가
 * 보스 오브젝트를 참조하고 있어 파괴/비활성은 별개 판단이 필요하다. 알파만 0 이라 몸체 콜라이더는
 * 그대로 남는다는 점만 유의(현재 Dead() 가 layer 를 바꾸므로 플레이어 공격 판정에는 걸리지 않는다).
 */

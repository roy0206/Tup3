using System;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;


public class Fire : BossBase
{
    private List<float> curTimes;
    [SerializeField] List<Transform> hitboxTransforms = new List<Transform>();
    private GameObject player;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform worldCanvas;
    private bool rushFacingRight;

    [Header("이동")]
    [SerializeField] private float returnSpeed = 3f;
    [SerializeField] private Vector2 idlePosition;
    [SerializeField] private Vector2 endPosition;
    [SerializeField] private float defaultDamage;
    [SerializeField] private float defaultKnockBackForce = 1f;

    [Header("부유 펄스 (대기 중 위아래 흔들림)")]
    [SerializeField] private bool enableFloatPulse = true;
    [SerializeField] private float floatPulseAmplitude = 0.25f;
    [SerializeField] private float floatPulsePeriod = 1.8f;
    [SerializeField] private Ease floatPulseEase = Ease.InOutSine;

    private Tween floatPulseTween;
    private float floatPulseBaseY;
    private bool floatPulseActive;

    [Header("사운드")]
    [SerializeField] private float roarSoundVolume = 1f;
    [SerializeField] private float rushHitSoundVolume = 1f;

    private const string RoarSound = "Fire_Roar";
    private const string RushHitSound = "Fire_RushHit";
    private const string RushHitSound2 = "Fire_RushHit2";

    private int skillOrder = 3;

    new void Awake()
    {
        base.Awake();
        behaviorTree = new BehaviorTreeBuilder(gameObject)
            .Selector("Root")
                .Sequence("DeadSecquence")
                    .Do("Dead",Dead)
                .End()
                .Selector("PatternSelector")
                    .Sequence("3")
                        .Do("O3", ()=>Orchestrate(3))  
            .Do("WaitForRush", () => Idle(0.5f))
            .Do("Targeting", ()=>Targeting(3))
            .Do("WaitForRush", () => Idle(0.5f))
            .Do("RushDown", ()=>Rush(0.5f, targetPosition, 5))
            .Do("Freeze",  ()=>Freeze(1f))
            .Do("Return", ()=>Return(1f))

                    .End()
                    .Sequence("1")
                        .Do("O1", ()=>Orchestrate(1))  
                        .Do("MoveHorizontal", ()=> MoveHorizontal(2f))
                        .Do("3Idle1", ()=> Idle(1))
                        .Do("A3", ()=> Pattern1(1))
                        .Do("Idle2", ()=> Idle(1))
                        .Do("Return",()=> Return(0.5f))
                    .End()
                    .Sequence("2")
                        .Do("O2", ()=>Orchestrate(2))  
                        .Do("Land", ()=> Land(2f))
                        .Do("Idle1", ()=> Idle(1))
                        .Do("Rush",  ()=> Rush(1, new Vector2(endPosition.x * (transform.position.x > 0 ? -1 : 1), endPosition.y),3))
                        .Do("Freeze", ()=> Freeze(1f))
                        .Do("Return",()=> Return(0.5f))
                        .Do("Idle2", ()=> Idle(3))
                    .End()
                .End()
            .End()
            .Build();
        curTimes = new List<float>()
        {
            0, 0, 0, 0
        };

        animationController = GetComponent<AnimationController>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        worldCanvas = transform.Find("WorldCanvas");
        player = GameObject.FindGameObjectWithTag("Player");
    }

    private TaskStatus Orchestrate(int order)
    {
        if(skillOrder != order) return TaskStatus.Failure;
        switch (skillOrder)
        {
             case 1: skillOrder = 2; break;
             case 2: skillOrder = 3; break;
             case 3: skillOrder = 1; break;
             default: Debug.LogWarning($"Skill order '{skillOrder}' is invalid."); return TaskStatus.Failure;
        }
        return TaskStatus.Success;
        
    }

    private void Update()
    {
        if (PauseManager.IsPaused || DialogueManager.IsDialogueActive) return;

        for (int i = 0; i < curTimes.Count; i++)
        {
            curTimes[i] -= Time.deltaTime;
        }
        behaviorTree.Tick();
    }

    private TaskStatus Dead()
    {
        if(!IsDead) return TaskStatus.Failure;

        StopFloatPulse();
        animationController.Play(0);
        gameObject.layer = LayerMask.GetMask("Default");

        return TaskStatus.Success;
    }

    private bool isPatternSetup;

    private TaskStatus Land(float waitTime)
    {
        if (!isPatternSetup)
        {
            isPatternSetup = true;
            curTimes[0] = waitTime;
            Vector2 landPosition = new Vector2(endPosition.x * (UnityEngine.Random.value > 0.5f ? -1 : 1), endPosition.y);
            Face(landPosition.x - transform.position.x);
            transform.DOMove(landPosition,
                waitTime).SetEase(Ease.InOutQuad);
        }
        if (curTimes[0] <= 0)
        {
            isPatternSetup = false;
            return TaskStatus.Success;
        }

        return TaskStatus.Continue;
    }
    [SerializeField] private LayerMask rushObstacleMask = (1 << 6) | (1 << 10);
    [SerializeField] private float rushBodyRadius = 0.5f;
    [SerializeField] private float rushMaxDistance = 40f;

    private Vector2 ExtendRushToObstacle(Vector2 start, Vector2 aimPos)
    {
        Vector2 dir = aimPos - start;
        if (dir.sqrMagnitude < 0.0001f) return aimPos;
        dir.Normalize();

        foreach (var hit in Physics2D.CircleCastAll(start, rushBodyRadius, dir, rushMaxDistance, rushObstacleMask))
        {
            if (hit.distance > 0.2f)
                return start + dir * hit.distance;
        }

        return start + dir * rushMaxDistance;
    }

    private TaskStatus Rush(float waitTime, Vector2 pos, int lavaCount)
    {
        if (!isPatternSetup)
        {
            isPatternSetup = true;
            Vector2 start = transform.position;
            Vector2 extended = ExtendRushToObstacle(start, pos);
            float baseDistance = Vector2.Distance(start, pos);
            float duration = baseDistance > 0.01f
                ? waitTime * (Vector2.Distance(start, extended) / baseDistance)
                : waitTime;
            curTimes[0] = duration;
            animator.SetBool("Rush", true);
            FaceRush(extended - start);
            transform.DOMove(extended,
                duration).SetEase(Ease.InCubic);
        }
        if (curTimes[0] <= 0)
        {
            isPatternSetup = false;
            animator.SetBool("Rush", false);
            SetRushRotation(0f);
            BossSound.Play(BossSound.PickVariant(RushHitSound, RushHitSound2), rushHitSoundVolume);
            LavaJet(lavaCount);
            return TaskStatus.Success;

        }

        return TaskStatus.Continue;
    }
    private TaskStatus MoveHorizontal(float waitTime)
    {
        if (!isPatternSetup)
        {
            isPatternSetup = true;
            curTimes[0] = waitTime;
            animator.SetBool("Rush", true);
            float targetX = endPosition.x * (transform.position.x > 0 ? -1 : 1);
            Face(targetX - transform.position.x);
            transform.DOMoveX(targetX,
                waitTime);
        }
        if (curTimes[0] <= 0)
        {
            isPatternSetup = false;
            animator.SetBool("Rush", false);
            return TaskStatus.Success;

        }

        return TaskStatus.Continue;
    }
    private TaskStatus Pattern1(float waitTime)
    {
        if (!isPatternSetup)
        {
            isPatternSetup = true;
            curTimes[0] = waitTime;
            animator.SetBool("Rush", true);
            float targetX = endPosition.x * (transform.position.x > 0 ? -1 : 1);
            Face(targetX - transform.position.x);
            transform.DOMoveX(targetX,
                waitTime).SetEase(Ease.Linear);
            for (int i = 1; i <= 16; i++)
            {
                DOVirtual.DelayedCall(
                    waitTime / 16 * i,
                    () =>
                    {
                        var obj = PoolManager.Instance.Get("FireColumn", new Vector2(transform.position.x, -5),
                            Quaternion.identity);
                        PoolManager.Instance.Release(obj, 0.2f);
                    });
            }

        }
        if (curTimes[0] <= 0)
        {
            isPatternSetup = false;
            animator.SetBool("Rush", false);
            return TaskStatus.Success;

        }

        return TaskStatus.Continue;
    }

    private void LavaJet(int num)
    {
        while (num > 0)
        {
            num -= 1;
            var lava = PoolManager.Instance.Get("Lava", transform.position, Quaternion.identity);
            PoolManager.Instance.Release(lava, 5);
        }
    }
    
    
    private float HorizontalDistance => Mathf.Abs(player.transform.position.x - transform.position.x);

    private TaskStatus Freeze(float waitTime)
    {
        if (!isPatternSetup)
        {
            isPatternSetup = true;
            curTimes[0] = waitTime;
            animator.SetBool("Stun", true);

        }
        if(curTimes[0] <= 0)
        {
            isPatternSetup = false;
            animator.SetBool("Stun", false);
            return TaskStatus.Success;
        }
        return TaskStatus.Continue;
    }

    GameObject aimObject;
    private Vector2 targetPosition;
    private TaskStatus Targeting(float waitTime)
    {
        if (!isPatternSetup)
        {
            isPatternSetup = true;
            curTimes[0] = waitTime;
            animator.SetBool("Warn", true);
            BossSound.Play(RoarSound, roarSoundVolume);
            aimObject =  PoolManager.Instance.Get("FireAim", transform.position, Quaternion.identity);
            aimObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
            aimObject.GetComponent<SpriteRenderer>().DOFade(1, 0.2f).SetEase(Ease.InQuad);
        }
        if(curTimes[0] <= 0)
        {
            isPatternSetup = false;
            animator.SetBool("Warn", false);
            
            targetPosition = aimObject.transform.position;
            
            aimObject.GetComponent<SpriteRenderer>().DOFade(0, 0.4f).SetEase(Ease.InQuad);
            PoolManager.Instance.Release(aimObject, 0.5f);
            return TaskStatus.Success;
        }
        
        aimObject.transform.position = Vector2.Lerp(aimObject.transform.position, player.transform.position, 0.5f);
        if ((aimObject.transform.position - player.transform.position).sqrMagnitude < 0.1f)
            aimObject.transform.position = player.transform.position;
        
        return TaskStatus.Continue;
    }
    
    private TaskStatus Return(float waitTime)
    {
        /*Vector2 dir = (Vector2)transform.position - idlePosition;
        if (dir.magnitude <= 0.1f)
        {
            transform.position = idlePosition;
            return TaskStatus.Success;
        }
        transform.Translate(dir * Time.deltaTime * returnSpeed, Space.World);
        return TaskStatus.Continue; */

        if (!isPatternSetup)
        {
            isPatternSetup = true;
            Face(idlePosition.x - transform.position.x);
            transform.DOMove(idlePosition, waitTime);
            curTimes[0] = waitTime;
        }
        if(curTimes[0] <= 0)
        {
            isPatternSetup = false;
            return TaskStatus.Success;
        }
        return TaskStatus.Continue;
    }

    private TaskStatus Idle(float waitTime)
    {
        if (!isPatternSetup)
        {
            isPatternSetup = true;
            curTimes[0] = waitTime;
            StartFloatPulse();
        }

        if(curTimes[0] <= 0)
        {
            isPatternSetup = false;
            StopFloatPulse();
            return TaskStatus.Success;
        }
        return TaskStatus.Continue;
    }

    private void StartFloatPulse()
    {
        if (!enableFloatPulse || floatPulseActive || IsDead) return;

        floatPulseActive = true;
        floatPulseBaseY = transform.position.y;
        floatPulseTween = transform
            .DOMoveY(floatPulseBaseY + floatPulseAmplitude, Mathf.Max(0.05f, floatPulsePeriod) * 0.5f)
            .SetEase(floatPulseEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopFloatPulse()
    {
        if (!floatPulseActive) return;

        floatPulseActive = false;
        if (floatPulseTween != null && floatPulseTween.IsActive()) floatPulseTween.Kill();
        floatPulseTween = null;

        Vector3 position = transform.position;
        position.y = floatPulseBaseY;
        transform.position = position;
    }

    private void OnDisable()
    {
        StopFloatPulse();
    }

    
    private void Face(float dir)
    {
        if (Mathf.Approximately(dir, 0f)) return;
        rushFacingRight = dir > 0f;
        spriteRenderer.flipX = rushFacingRight;
    }

    private void FaceRush(Vector2 dir)
    {
        if (dir.sqrMagnitude <= Mathf.Epsilon) return;
        Face(dir.x);
        float angle = Vector2.SignedAngle(rushFacingRight ? Vector2.right : Vector2.left, dir);
        SetRushRotation(angle);
    }

    private void SetRushRotation(float angle)
    {
        Vector3 canvasPosition = worldCanvas.position;
        Quaternion canvasRotation = worldCanvas.rotation;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        worldCanvas.SetPositionAndRotation(canvasPosition, canvasRotation);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerKnockBack knockBack))
        {
            if (other.TryGetComponent(out PlayerHealth _))
                Debug.LogError($"[화 보스] '{other.name}' 에 PlayerKnockBack 이 없어 넉백·무적 점멸이 적용되지 않습니다.", this);
            return;
        }

        knockBack.TakeHit(transform.position, defaultKnockBackForce, Mathf.RoundToInt(defaultDamage));
    }
}

/* [파일 노트]
 * 돌진(Rush)은 조준점에서 멈추지 않는다 — 조준 방향으로 CircleCast(rushObstacleMask: ground+wall,
 * rushBodyRadius) 해서 벽/바닥에 닿는 지점까지 연장하고, 속도 유지를 위해 지속시간을 거리 비례로
 * 늘린다(플레이어가 점프 중에 조준이 끝나 공중 좌표가 잡혀도 지형까지 계속 돌진). 시작점과 겹친
 * 콜라이더(distance<=0.2)는 무시하고, 아무것도 안 맞으면 rushMaxDistance 까지 간다.
 *
 * 일시정지 대응 : Update 첫 줄 PauseManager.IsPaused 게이트로 BT/타이머가 멈추고,
 * 이동(DOMove/DOMoveX)과 FireColumn 소환 예약(DOVirtual.DelayedCall)은 DOTween.PauseAll 로 함께 멈춘다.
 * 몸통 접촉 피해는 PlayerKnockBack.TakeHit 쪽 게이트가 차단한다.
 *
 * 몸통 접촉 피해 경로 : 예전에는 OnTriggerEnter2D 에서 PlayerHealth.TakeDamage 를 직접 불러
 * 넉백·무적 점멸이 통째로 빠졌다. 지금은 같은 오브젝트의 PlayerKnockBack.TakeHit 로 일원화했다.
 * 검사 대상을 other 오브젝트 자신으로 유지한 것은 의도적이다 — GetComponentInParent 로 넓히면
 * 플레이어의 Attack 자식 트리거가 겹칠 때도 피해가 들어가 기존 판정 범위가 바뀐다.
 * PlayerKnockBack 과 PlayerHealth 는 둘 다 플레이어 루트에 있으므로 판정 대상 집합은 그대로다.
 * 부작용 : 이제 몸통 접촉도 TakeHit 의 0.5초 무적과 대시 중 무적을 따른다(지속 접촉 시 최대 초당 2회).
 *
 * 부유 펄스(enableFloatPulse) : 불사의 새 컨셉에 맞춘 대기 중 상하 부유.
 * 루트 transform 을 DOMoveY 로 Yoyo 무한 반복시킨다 — 화보스는 중력·접지 로직이 전혀 없고
 * (ApplyGravity 같은 것이 없다) 스프라이트가 루트에 붙어 있어 움직일 비주얼 자식이 따로 없으므로
 * 루트 방식이 유일하게 자연스러운 선택이다. 히트박스도 함께 떠서 판정이 몸을 따라간다.
 * 위치를 만지는 다른 트윈(Land/Rush/MoveHorizontal/Return 의 DOMove 계열)과 싸우지 않도록
 * Idle 태스크 진입 시에만 시작하고 Idle 이 끝나는 순간 Kill 후 시작 y 로 복원한다
 * (복원하지 않으면 다음 DOMove 의 시작점이 흔들린 위치가 되어 경기장 좌표가 밀린다).
 * 사망(Dead 태스크)·비활성(OnDisable)에서도 같은 StopFloatPulse 로 정리해 시체가 떠다니지 않는다.
 * DOTween 이므로 PauseManager 의 DOTween.PauseAll 에 함께 멈춘다.
 * 최종보스의 화 환영(FirePhantom)은 빌더가 스프라이트+애니메이션만으로 새로 만든 오브젝트라
 * Fire.cs 가 붙지 않는다 — 이 펄스의 영향을 받지 않는다.
 *
 * 사운드
 *   Fire_Roar : Targeting 태스크 진입(animator "Warn" = 조준 경고 모션 시작) 시 1회.
 *               화보스에는 등장 연출이 따로 없고, 조준 경고가 유일하게 "보스가 크게 예고하는" 구간이라
 *               포효를 여기에 붙였다. 패턴3 사이클마다 1회이므로 반복 부담이 없다.
 *   Fire_RushHit / Fire_RushHit2 : Rush 태스크가 끝나는 프레임 — 즉 돌진이 지형에 처박히고
 *               LavaJet 이 터지는 바로 그 순간 — 에 둘 중 하나를 무작위로 재생한다
 *               (BossSound.PickVariant, 50:50). 두 파일이 같은 소리의 변형이라 반복감을 줄이는 용도다.
 *               Rush 는 패턴2(수평 돌진)와 패턴3(조준 낙하 돌진)이 공유하므로 두 패턴 모두 적용된다.
 *               충돌 판정 성립 여부와 무관하게 "돌진이 끝나 부딪히는" 연출 시점이라 항상 울린다.
 *   Fire_Column   : 불기둥 본체(FireColumn.cs)가 스스로 재생한다.
 *   Fire_LavaLand : 화염구 착지(Lava.cs)가 재생한다.
 *   Fire_LavaSizzle : 용암 장판(LavaPool.cs)이 재생한다.
 */

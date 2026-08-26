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
    private SpriteRenderer spriteRenderer;
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

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (Mathf.Approximately(transform.rotation.eulerAngles.y, 180f))
        {
            transform.rotation = Quaternion.identity;
            SetFacing(true);
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
            } );
            DOVirtual.DelayedCall(0.7f, () =>
            {
                hitboxTransforms[2].gameObject.SetActive(true);
                hitboxTransforms[2]
                    .DOMoveX(hitboxTransforms[2].transform.position.x + (isFacingRight ? 10f : -10f), 1f)
                    .OnComplete(() =>{ hitboxTransforms[2].gameObject.SetActive(false);hitboxTransforms[2].localPosition = new Vector3(isFacingRight ? 2f : -2f, 2f, 0); });
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
        return TaskStatus.Success;
    }

    private TaskStatus Stay()
    {
        animationController.Play(4);
        Face(Mathf.Sign(player.transform.position.x - transform.position.x));
        return TaskStatus.Success;
    }
    
    private void Face(float dir)
    {
        if (Mathf.Approximately(dir, 0f)) return;
        SetFacing(dir > 0f);
    }

    private void SetFacing(bool facingRight)
    {
        if (spriteRenderer != null) spriteRenderer.flipX = facingRight;
        if (facingRight == isFacingRight) return;

        isFacingRight = facingRight;
        foreach (var hitbox in hitboxTransforms)
            MirrorChild(hitbox);
    }

    private static void MirrorChild(Transform t)
    {
        if (t == null) return;

        Vector3 pos = t.localPosition;
        pos.x = -pos.x;
        t.localPosition = pos;

        Vector3 scale = t.localScale;
        scale.x = -scale.x;
        t.localScale = scale;
    }

    private void ApplyGravity()
    {
        Bounds bounds = bodyCollider.bounds;
        bool grounded = Physics2D.BoxCast(bounds.center, bounds.size, 0f,
            Vector2.down, groundCheckDistance, groundMask).collider != null;

        if (grounded && verticalVelocity <= 0f)
        {
            verticalVelocity = 0f;
            return;
        }

        verticalVelocity += gravity * Time.deltaTime;
        transform.Translate(Vector3.up * (verticalVelocity * Time.deltaTime), Space.World);
    }

}

/* [파일 노트]
 * 일시정지 대응 : Update 첫 줄 PauseManager.IsPaused 게이트로 BT/쿨타임/중력이 멈춘다.
 * 패턴의 지연 히트박스(DOVirtual.DelayedCall)와 이동 트윈은 PauseManager 의 DOTween.PauseAll 로 멈추고,
 * SoilDrop 소환 코루틴은 루프마다 WaitWhilePaused 로 일시정지 동안 추가 소환을 하지 않는다.
 *
 * 좌우반전은 Gold 보스와 같은 spriteRenderer.flipX 방식이다 — 예전의 루트 Y축 180도 회전은
 * 자식 체력바 캔버스까지 카메라 반대편으로 뒤집어 안 보이게 만들었다. flipX 는 자식을 안 뒤집으므로
 * 방향이 바뀔 때 SetFacing 이 hitboxTransforms 전체의 localPosition.x / localScale.x 를 미러링한다.
 * 기본(스프라이트 원본)은 왼쪽 보기 = isFacingRight false 이고, 씬에 Y=180 으로 저장돼 있던 경우를
 * 위해 Awake 에서 회전을 identity 로 되돌리고 facing 상태로 변환한다. 패턴1의 전진 히트박스와
 * SoilDrop 낙하 방향도 회전 대신 isFacingRight 를 본다.
 */

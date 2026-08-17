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
    private Transform bodyTransform;
    private Transform worldCanvas;
    private bool rushFacingRight;

    [Header("이동")]
    [SerializeField] private float returnSpeed = 3f;
    [SerializeField] private Vector2 idlePosition;
    [SerializeField] private Vector2 endPosition;
    [SerializeField] private float defaultDamage;

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
        bodyTransform = transform.Find("Body");
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
        for (int i = 0; i < curTimes.Count; i++)
        {
            curTimes[i] -= Time.deltaTime;
        }
        behaviorTree.Tick();
    }

    private TaskStatus Dead()
    {
        if(!IsDead) return TaskStatus.Failure;


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
    private TaskStatus Rush(float waitTime, Vector2 pos, int lavaCount)
    {
        if (!isPatternSetup)
        {
            isPatternSetup = true;
            curTimes[0] = waitTime;
            animator.SetBool("Rush", true);
            FaceRush(pos - (Vector2)transform.position);
            transform.DOMove(pos,
                waitTime).SetEase(Ease.InCubic);
        }
        if (curTimes[0] <= 0)
        {
            isPatternSetup = false;
            animator.SetBool("Rush", false);
            SetRushRotation(0f);
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
                        var obj = PoolManager.Instance.Get("FireColumn", new Vector2(transform.position.x, endPosition.y),
                            Quaternion.identity);
                        PoolManager.Instance.Release(obj, 0.5f);
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
        }
        if(curTimes[0] <= 0)
        {
            isPatternSetup = false;
            return TaskStatus.Success;
        }
        return TaskStatus.Continue;
    }

    
    private void Face(float dir)
    {
        if (Mathf.Approximately(dir, 0f)) return;
        rushFacingRight = dir > 0f;
        spriteRenderer.flipX = rushFacingRight;
        Vector3 scale = bodyTransform.localScale;
        scale.x = Mathf.Abs(scale.x) * (rushFacingRight ? -1f : 1f);
        bodyTransform.localScale = scale;
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
        if (other.gameObject.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(defaultDamage);
        }
    }
}

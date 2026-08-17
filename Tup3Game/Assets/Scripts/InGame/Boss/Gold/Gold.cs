using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class Gold : BossBase
{
    private readonly List<float> attackRange = new() { 0f, 3f, 100f, 3f, 3f };
    private List<float> curTimes;
    private GameObject player;

    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float gravity = -40f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 0.1f;
    
    [SerializeField] private Animator animator;
    private BoxCollider2D bodyCollider;
    private float verticalVelocity;
    private bool isPatternSetup;
    private bool isCounterAttackReady;
    private bool isCounterAttacking;
    private float groggyTime;

    public float GroggyTime => groggyTime;

    new void Awake()
    {
        base.Awake();
        behaviorTree = new BehaviorTreeBuilder(gameObject)
            .Selector("Root")
                .Sequence("DeadSequence")
                    .Do("Dead", Dead)
                .End()
                .Do("Groggy", Groggy)

                .Selector("PatternSelector")
            .Do("Counter", CounterAttack)
            
                    .Sequence("Pattern1")
                        .Do("CanUsePattern1", () => PatternStarter(1))
                        .Do("UsePattern1", Pattern1)
                    .End()
                    .Sequence("Pattern2")
                        .Do("CanUsePattern2", () => PatternStarter(2))
                        .Do("UsePattern2", Pattern2)
                    .End()
                    .Sequence("Pattern3")
                        .Do("CanUsePattern3", () => PatternStarter(3))
                        .Do("UsePattern3", Pattern3)
                    .End()
                .End()
                .Do("Move", Move)
                .Do("Idle", Idle)
            .End()
            .Build();

        curTimes = new List<float> { 0f, 0f, 0f, 0f };
        if (animator == null) animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");
        bodyCollider = boxColliders.Count > 0 ? boxColliders[0] : GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        for (int i = 0; i < curTimes.Count; i++)
        {
            curTimes[i] -= Time.deltaTime;
        }
        isCounterAttackReady = false;
        groggyTime -= Time.deltaTime;
        animator.SetBool("IsDead", IsDead);
        animator.SetBool("IsGroggy", !IsDead && GroggyTime >= 0f);
        behaviorTree.Tick();
        ApplyGravity();
    }

    private TaskStatus Dead()
    {
        if (!IsDead) return TaskStatus.Failure;

        animator.SetBool("IsMoving", false);
        animator.SetBool("IsIdle", false);
        gameObject.layer = LayerMask.GetMask("Default");
        return TaskStatus.Success;
    }
    

    private TaskStatus Groggy()
    {
        if (IsDead || GroggyTime < 0) return TaskStatus.Failure;
        animator.SetBool("IsMoving", false);
        animator.SetBool("IsIdle", false);
        return TaskStatus.Success;
    }

    private TaskStatus CounterAttack()
    {
        if (IsDead || GroggyTime > 0)
        {
            isPatternSetup = false;
            return TaskStatus.Failure;
        }
        if(!isCounterAttacking) return  TaskStatus.Failure;
        if (!isPatternSetup)
        {
            curTimes[0] = 1f;
            isPatternSetup = true;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsIdle", false);
            animator.SetTrigger("CounterAttack");
            DOVirtual.DelayedCall(0.2f,
                () => player.GetComponent<PlayerKnockBack>().TakeHit(transform.position, 0.5f, 20));
        }

        isCounterAttackReady = true;
        if (curTimes[0] > 0f) return TaskStatus.Continue;
        isCounterAttacking = false;
        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private TaskStatus PatternStarter(int patternIndex)
    {
        if (IsDead || GroggyTime > 0) return TaskStatus.Failure;
        if (curTimes[patternIndex] > 0f) return TaskStatus.Failure;
        if (HorizontalDistance > attackRange[patternIndex]) return TaskStatus.Failure;
        return TaskStatus.Success;
    }

    private TaskStatus Pattern1()
    {
        if (IsDead || GroggyTime > 0)
        {
            isPatternSetup = false;
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            curTimes[0] = 1f;
            curTimes[1] = 10f;
            isPatternSetup = true;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsIdle", false);
            animator.SetTrigger("Pattern1");
        }

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private TaskStatus Pattern2()
    {
        if (IsDead || GroggyTime > 0)
        {
            isPatternSetup = false;
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            curTimes[0] = 3f;
            curTimes[2] = 10f;
            isPatternSetup = true;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsIdle", false);
            animator.SetTrigger("Pattern2");

            DOVirtual.DelayedCall(1f, () =>
            {
                GameObject swordTrap = PoolManager.Instance.Get(
                    "SwordTrap",
                    transform.position + Vector3.right,
                    Quaternion.identity);
                PoolManager.Instance.Release(swordTrap, 10f);

                swordTrap = PoolManager.Instance.Get(
                    "SwordTrap",
                    transform.position + Vector3.left,
                    Quaternion.identity);
                PoolManager.Instance.Release(swordTrap, 10f);
            });
        }

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private TaskStatus Pattern3()
    {
        if (IsDead || GroggyTime > 0)
        {
            isPatternSetup = false;
            return TaskStatus.Failure;
        }

        if (!isPatternSetup)
        {
            curTimes[0] = 8f;
            curTimes[3] = 60f;
            isPatternSetup = true;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsIdle", false);
            animator.SetTrigger("Pattern3");

            DOVirtual.DelayedCall(1f, () =>
            {
                for (int i = 0; i < 5; i++)
                {
                    GameObject flyingSword = PoolManager.Instance.Get(
                        "FlyingSword",
                        transform.position,
                        Quaternion.identity);
                    PoolManager.Instance.Release(flyingSword, 10f);
                }
            });
        }

        if (curTimes[0] > 0f) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private float HorizontalDistance => Mathf.Abs(player.transform.position.x - transform.position.x);

    private TaskStatus Move()
    {
        if (IsDead || GroggyTime > 0 || isCounterAttacking) return TaskStatus.Failure;
        if (HorizontalDistance <= attackRange[4]) return TaskStatus.Failure;
        
        isCounterAttackReady = true;
        animator.SetBool("IsMoving", true);
        animator.SetBool("IsIdle", false);
        float direction = Mathf.Sign(player.transform.position.x - transform.position.x);
        Face(direction);
        transform.Translate(Vector3.right * (direction * moveSpeed * Time.deltaTime), Space.World);
        return TaskStatus.Success;
    }

    private TaskStatus Idle()
    {
        if (IsDead || GroggyTime > 0 || isCounterAttacking) return TaskStatus.Failure;
        isCounterAttackReady = true;
        animator.SetBool("IsMoving", false);
        animator.SetBool("IsIdle", true);
        Face(Mathf.Sign(player.transform.position.x - transform.position.x));
        return TaskStatus.Success;
    }

    private void Face(float direction)
    {
        transform.localRotation = Quaternion.Euler(0f, direction > 0f ? 180f : 0f, 0f);
    }

    private void ApplyGravity()
    {
        Bounds bounds = bodyCollider.bounds;
        bool grounded = Physics2D.BoxCast(
            bounds.center,
            bounds.size,
            0f,
            Vector2.down,
            groundCheckDistance,
            groundMask).collider != null;

        if (grounded && verticalVelocity <= 0f)
        {
            verticalVelocity = 0f;
            return;
        }

        verticalVelocity += gravity * Time.deltaTime;
        transform.Translate(Vector3.up * (verticalVelocity * Time.deltaTime), Space.World);
    }

    public override bool DoDamage(float damage)
    {
        if (isCounterAttackReady || !isCounterAttacking)
        {
            isCounterAttacking = true;
            return false;
        }
        groggyTime = 3;
        return base.DoDamage(damage);

    }
}

using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class Water : BossBase
{
    private List<float> curTimes;
    [SerializeField] List<Transform> hitboxTransforms = new List<Transform>();
    private GameObject player;

    [Header("이동")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float gravity = -40f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 0.1f;

    [SerializeField] private WaterSpawnZone Vertical_spawnZone;
    [SerializeField] private WaterSpawnZone Horizontal_spawnZone;


    private BoxCollider2D bodyCollider;
    private float verticalVelocity;

    new void Awake()
    {
        base.Awake();
        behaviorTree = new BehaviorTreeBuilder(gameObject)
            .Selector("Root")
                .Sequence("DeadSecquence")
                    .Do("Dead", Dead)
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
            .End()
            .Build();
        curTimes = new List<float>()
        {
            0, 0, 0, 0
        };

        animationController = GetComponent<AnimationController>();
        player = GameObject.FindGameObjectWithTag("Player");
        bodyCollider = boxColliders.Count > 0 ? boxColliders[0] : GetComponent<BoxCollider2D>();
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
        if (!IsDead) return TaskStatus.Failure;


        animationController.Play(0);

        return TaskStatus.Success;
    }


    private TaskStatus Pattern1()
    {
        if (IsDead) return TaskStatus.Failure;
        return TaskStatus.Success;
    }

    private TaskStatus Pattern2()
    {
        if (IsDead) return TaskStatus.Failure;
        return TaskStatus.Success;
    }
    private TaskStatus Pattern3()
    {
        if (IsDead) return TaskStatus.Failure;
        return TaskStatus.Success;
    }

    private TaskStatus PatternStarter(int num)
    {
        if (curTimes[num] > 0) return TaskStatus.Failure;

        return TaskStatus.Success;
    }
}

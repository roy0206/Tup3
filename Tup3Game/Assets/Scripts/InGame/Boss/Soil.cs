using System;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using System.Collections.Generic;
using DG.Tweening;




public class Soil : BossBase
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

    private BoxCollider2D bodyCollider;
    private float verticalVelocity;

    private void Awake()
    {
        behaviorTree = new BehaviorTreeBuilder(gameObject)
            .Selector("Root")
                .Sequence("DeadSecquence")
                    .Do("Dead", () => TaskStatus.Failure)
                .End()
                .Selector("PatternSelector")
                    .Sequence("1")
                        .Do("Cool1", () => PatternStarter(1))
                        .Do("A1", Pattern1)
                    .End()
                .End()
                .Do("Go", Move)
                .Do("Stay", Stay)
            .End()
            .Build();
        curTimes = new List<float>()
        {
            0, 0, 0
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
        ApplyGravity();
    }

    protected override void OnDead()
    {
        Debug.Log("Soil Dead");
    }

    private TaskStatus PatternStarter(int num)
    {
        if (curTimes[num] > 0) return TaskStatus.Failure;
        if (HorizontalDistance > attackRange) return TaskStatus.Failure;

        return TaskStatus.Success;
    }

    private bool isParrernSetup;
    private TaskStatus Pattern1()
    {
        if (!isParrernSetup)
        {
            curTimes[1] = 5;
            curTimes[0] = 2;
            animationController.Play(1);
            isParrernSetup = true;
            DOVirtual.DelayedCall(0.1f, () =>
            {
                hitboxTransforms[0].gameObject.SetActive(true);
            } );
            DOVirtual.DelayedCall(0.5f, () =>
            {
                hitboxTransforms[0].gameObject.SetActive(false);
            } );
            DOVirtual.DelayedCall(0.6f, () =>
            {
                hitboxTransforms[1].gameObject.SetActive(true);
            } );
            DOVirtual.DelayedCall(0.7f, () =>
            {
                hitboxTransforms[2].gameObject.SetActive(true);
                hitboxTransforms[2]
                    .DOMoveX(hitboxTransforms[2].transform.position.x - hitboxTransforms[2].transform.right.x * 10, 1f)
                    .OnComplete(() =>{ hitboxTransforms[2].gameObject.SetActive(false);hitboxTransforms[2].localPosition = new Vector3(0, 1.72f, 0); });
            } );
            DOVirtual.DelayedCall(0.9f, () =>
            {
                hitboxTransforms[1].gameObject.SetActive(false);
            } );
        }
        if (curTimes[0] > 0) return TaskStatus.Continue;

        isParrernSetup = false;
        return TaskStatus.Success;

    }
    
    private float HorizontalDistance => Mathf.Abs(player.transform.position.x - transform.position.x);

    private TaskStatus Move()
    {
        if (HorizontalDistance <= attackRange) return TaskStatus.Failure;

        float dir = Mathf.Sign(player.transform.position.x - transform.position.x);
        Face(dir);
        transform.Translate(Vector3.right * (dir * moveSpeed * Time.deltaTime), Space.World);
        return TaskStatus.Success;
    }

    private TaskStatus Stay()
    {
        Face(Mathf.Sign(player.transform.position.x - transform.position.x));
        return TaskStatus.Success;
    }
    
    private void Face(float dir)
    {
        transform.localRotation = Quaternion.Euler(0f, dir > 0f ? 180f : 0f, 0f);
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

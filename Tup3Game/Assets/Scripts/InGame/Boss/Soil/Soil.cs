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

    [Header("이동")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float gravity = -40f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 0.1f;

    private BoxCollider2D bodyCollider;
    private float verticalVelocity;

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
        if (HorizontalDistance > attackRange) return TaskStatus.Failure;

        return TaskStatus.Success;
    }

    private bool isParrernSetup;
    private TaskStatus Pattern1()
    {
        if(IsDead) return TaskStatus.Failure;
        if (!isParrernSetup)
        {
            curTimes[1] = 10;
            curTimes[0] = 2;
            animationController.Play(1);
            isParrernSetup = true;
            DOVirtual.DelayedCall(0.11f, () =>
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

    private IEnumerator SoilDrop()
    {
        for (int i = 0; i < 12; i++)
        {
            var bossX = transform.position.x;
            bool bossFlip = transform.rotation.eulerAngles.y == 180;
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
        if (!isParrernSetup)
        {
            curTimes[2] = 20;
            curTimes[0] = 5;
            animationController.Play(2);
            isParrernSetup = true;
            DOVirtual.DelayedCall(0.5f, () => StartCoroutine(SoilDrop()));
            

        }
        if (curTimes[0] > 0) return TaskStatus.Continue;

        isParrernSetup = false;
        return TaskStatus.Success;

    }
    
        
    private TaskStatus Pattern3()
    {
        if(IsDead) return TaskStatus.Failure;
        if (!isParrernSetup)
        {
            curTimes[3] = 0;
            curTimes[0] = 2;
            animationController.Play(3);
            isParrernSetup = true;
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

        isParrernSetup = false;
        return TaskStatus.Success;

    }
    
    private float HorizontalDistance => Mathf.Abs(player.transform.position.x - transform.position.x);

    private TaskStatus Move()
    {
        if (HorizontalDistance <= attackRange) return TaskStatus.Failure;

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

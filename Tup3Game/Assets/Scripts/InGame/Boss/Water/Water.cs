using System;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class Water : BossBase
{
    private List<float> curTimes;
    private GameObject player;

    [Header("빗방울(웅덩이) 패턴")]
    [SerializeField] private Water_Sprout_Zone watersproutzone;
    [SerializeField] private int rainSpawnCount = 3;

    [Header("얼음총알 패턴 (좌우 두 지점 중 랜덤)")]
    [SerializeField] private IceBulletSpawnZone[] iceBulletSpawnZones = new IceBulletSpawnZone[2];
    [SerializeField] private int iceBulletSpawnCount = 3;

    [Header("Water Eye 소환")]
    [SerializeField] private GameObject[] eyePrefab;
    [SerializeField] private Transform[] eyeSpawnPoints;
    private GameObject Eye;
    
    new void Awake()
    {
        base.Awake();
        behaviorTree = new BehaviorTreeBuilder(gameObject)
            .Selector("Root")
                .Sequence("DeadSequence")
                    .Do("Dead", Dead)
                .End()
                .Selector("PatternSelector")
                    .Sequence("1")
                        .Do("Cool1", () => PatternStarter(1))
                        .Do("A1_IceBullet", Pattern1_IceBullet)
                    .End()
                    .Sequence("2")
                        .Do("Cool2", () => PatternStarter(2))
                        .Do("A2_WaterPump", Pattern2_WaterPump)
                    .End()
                    .Sequence("3")
                        .Do("Cool3", () => PatternStarter(3))
                        .Do("A3_Basic", Pattern3_Basic)
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
        gameObject.layer = LayerMask.GetMask("Default");

        return TaskStatus.Success;
    }

    private TaskStatus PatternStarter(int num)
    {
        if (curTimes[num] > 0) return TaskStatus.Failure;
        return TaskStatus.Success;
    }

    private bool isPatternSetup;

    private void SpawnEye(int patternIndex)
    {
        if (eyePrefab == null) return;
        if (eyeSpawnPoints == null || patternIndex >= eyeSpawnPoints.Length || eyeSpawnPoints[patternIndex] == null) return;

        Eye = Instantiate(eyePrefab[patternIndex], eyeSpawnPoints[patternIndex].position, Quaternion.identity);

        if (Eye.TryGetComponent(out Water_eye eyeComponent))
        {
            eyeComponent.Init(this); // this = Water(BossBase 상속) 자신을 넘김
        }
    }

    private TaskStatus Pattern1_IceBullet()
    {
        if (IsDead) return TaskStatus.Failure;
        if (!isPatternSetup)
        {
            curTimes[1] = 20f; // TODO: 쿨타임 값 조정
            curTimes[0] = 10f; // TODO: 패턴 총 지속시간 (애니메이션 길이에 맞춰서)
            isPatternSetup = true;
            SpawnEye(0);
            Destroy(Eye, curTimes[0]);
            if (iceBulletSpawnZones != null && iceBulletSpawnZones.Length > 0)
            {
                IceBulletSpawnZone chosenZone = iceBulletSpawnZones[UnityEngine.Random.Range(0, iceBulletSpawnZones.Length)];
                if (chosenZone != null)
                {
                    DOVirtual.DelayedCall(0.3f, () =>
                    {
                        chosenZone.SpawnIceBullets(iceBulletSpawnCount);
                    });
                }
            }
        }

        if (curTimes[0] > 0) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private TaskStatus Pattern2_WaterPump()
    {
        if (IsDead) return TaskStatus.Failure;
        if (!isPatternSetup)
        {
            curTimes[2] = 20f; // TODO: 쿨타임 값 조정
            curTimes[0] = 10f;  // TODO: 패턴 총 지속시간
            isPatternSetup = true;
            SpawnEye(1);
            Destroy(Eye, curTimes[0]);
            if (watersproutzone != null)
            {
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    watersproutzone.SpawnWaterBullets(rainSpawnCount);
                });
            }
        }

        if (curTimes[0] > 0) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private TaskStatus Pattern3_Basic()
    {
        if (IsDead) return TaskStatus.Failure;
        if (!isPatternSetup)
        {
            curTimes[3] = 30f; // TODO: 쿨타임 값 조정
            curTimes[0] = 10f; // TODO: 패턴 총 지속시간
            isPatternSetup = true;
            SpawnEye(2);
            Destroy(Eye, curTimes[0]);
        }

        if (curTimes[0] > 0) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;
    }
    
}

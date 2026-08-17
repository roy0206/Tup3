using System;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public enum BossPhase
{
    Normal,                // 평상시 - 눈1/눈2/눈3 패턴 순환
    EncroachmentTelegraph, // 전조 진행중 - 바닥 갈라짐, 파훼 가능
    Encroached              // 잠식 성공 - 수중모드, 눈1+2 동시발동 등
}

public class Water : BossBase
{
    private List<float> curTimes;
    private GameObject player;

    [Header("분수 시작점")]
    [SerializeField] private Water_Sprout_Zone watersproutzone;

    [Header("분수 패턴")]
    [SerializeField] private int WaterSprout_SpawnCount = 3;
    [SerializeField] private int WaterSprout_DelayTime = 3;
    [SerializeField] private int WaterSprout_CoolTime = 7;

    [Header("얼음총알 패턴 (좌우 두 지점 중 랜덤)")]
    [SerializeField] private IceBulletSpawnZone[] iceBulletSpawnZones = new IceBulletSpawnZone[2];

    [Header("얼음총알 패턴")]
    [SerializeField] private int iceBullet_SpawnCount = 3;
    [SerializeField] private int iceBullet_DelayTime = 3;
    [SerializeField] private int iceBullet_CoolTime = 6;

    [Header("Water Eye 소환")]
    [SerializeField] private GameObject eyePrefab;
    [SerializeField] private float[] Scale;
    [SerializeField] private Transform[] eyeSpawnPoints;
    private Water_eye currentEye;
    private GameObject Eye;

    [Header("소용돌이 패턴")]
    [SerializeField] private Storm stormPrefab;
    [SerializeField] private Transform stormSpawnPoint;
    [SerializeField] private float stormSpawnDelay = 0.5f;
    [SerializeField] private float stormPatternDuration = 10f;
    [SerializeField] private float stormCoolTime = 30f;

    [Header("전기 구체 패턴")]
    [SerializeField] private Electric_ball electricBallPrefab;
    [SerializeField] private Transform electricBallSpawnPoint;
    [SerializeField] private float electricBallSpawnDelay = 0.5f;
    [SerializeField] private float electricBallPatternDuration = 1.5f;
    [SerializeField] private float electricBallCoolTime = 30f;

    [Header("잠식 2페이즈")]
    [SerializeField] private RisingWaterPhase risingWaterPhase;
    [SerializeField, Range(0f, 1f)] private float encroachmentHpRatio = 0.5f;

    private BossPhase currentPhase = BossPhase.Normal;
    private float encroachmentTriggerHp;
    private bool hasCleanedUpDeath;

    new void Awake()
    {
        base.Awake();
        behaviorTree = new BehaviorTreeBuilder(gameObject)
            .Selector("Root")
                .Sequence("DeadSequence")
                    .Do("Dead", Dead)
                .End()
                .Sequence("EncroachmentSequence")
                    .Do("CheckEncroachmentTrigger", CheckEncroachmentTrigger)
                    .Do("EnterEncroachmentPhase", EnterEncroachmentPhase)
                .End()
                .Selector("PatternSelector")
                    .Sequence("1")
                        .Do("Cool1", () => PatternStarter(1))
                        .Do("A1_IceBullet", Pattern1_IceBullet)
                    .End()
                    .Sequence("2")
                        .Do("Cool2", () => PatternStarter(2))
                        .Do("A2_WaterSprout", Pattern2_WaterSprout)
                    .End()
                    .Sequence("3")
                        .Do("Cool3", () => PatternStarter(3))
                        .Do("A3_ThunderStorm", Pattern3_Storm)
                    .End()
                    .Sequence("4")
                        .Do("Cool4", () => PatternStarter(4))
                        .Do("A4_ElectricBall", Pattern4_ElectricBall)
                    .End()
                .End()
            .End()
            .Build();

        curTimes = new List<float>()
        {
            0, 0, 0, 0, 0
        };

        animationController = GetComponent<AnimationController>();
        player = GameObject.FindGameObjectWithTag("Player");
        encroachmentTriggerHp = Hp * encroachmentHpRatio;
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

        if (!hasCleanedUpDeath)
        {
            risingWaterPhase?.StopAndHide();
            hasCleanedUpDeath = true;
        }

        gameObject.layer = LayerMask.NameToLayer("Default");

        return TaskStatus.Success;
    }

    private TaskStatus CheckEncroachmentTrigger()
    {
        if (currentPhase != BossPhase.Normal)
            return TaskStatus.Failure;

        return Hp <= encroachmentTriggerHp
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }

    private TaskStatus EnterEncroachmentPhase()
    {
        if (currentPhase == BossPhase.Normal)
        {
            currentPhase = BossPhase.EncroachmentTelegraph;
            isPatternSetup = false;

            if (risingWaterPhase == null || !risingWaterPhase.BeginRise())
            {
                Debug.LogError("Water: Rising Water Phase가 연결되지 않아 수위 상승 없이 2페이즈로 전환합니다.", this);
                currentPhase = BossPhase.Encroached;
                return TaskStatus.Success;
            }
        }

        if (currentPhase == BossPhase.EncroachmentTelegraph &&
            risingWaterPhase.HasReachedTarget)
        {
            currentPhase = BossPhase.Encroached;
            return TaskStatus.Success;
        }

        return TaskStatus.Continue;
    }

    private TaskStatus PatternStarter(int num)
    {
        if (curTimes[num] > 0) return TaskStatus.Failure;
        return TaskStatus.Success;
    }

    private bool isPatternSetup;

    private void SpawnEye(int patternIndex, float lifeTime)
    {
        if (eyePrefab == null) return;
        if (eyeSpawnPoints == null || patternIndex >= eyeSpawnPoints.Length || eyeSpawnPoints[patternIndex] == null) return;

        Eye = Instantiate(eyePrefab, eyeSpawnPoints[patternIndex].position, Quaternion.identity);

        currentEye = Eye.GetComponent<Water_eye>();

        if (currentEye != null)
        {
            currentEye.Init(this, lifeTime, Scale[patternIndex]);
        }
    }

    private TaskStatus Pattern1_IceBullet()
    {
        if (IsDead) return TaskStatus.Failure;
        if (!isPatternSetup)
        {
            curTimes[1] = iceBullet_CoolTime; // TODO: 쿨타임 값 조정
            curTimes[0] = iceBullet_DelayTime; // TODO: 패턴 총 지속시간 (애니메이션 길이에 맞춰서)
            isPatternSetup = true;
            
            SpawnEye(0, iceBullet_DelayTime);
            
            if (iceBulletSpawnZones != null && iceBulletSpawnZones.Length > 0)
            {
                IceBulletSpawnZone chosenZone = iceBulletSpawnZones[UnityEngine.Random.Range(0, iceBulletSpawnZones.Length)];
                if (chosenZone != null)
                {
                    DOVirtual.DelayedCall(0.3f, () =>
                    {
                        chosenZone.SpawnIceBullets(iceBullet_SpawnCount);
                    });
                }
            }
        }

        if (curTimes[0] > 0)
        {
            return TaskStatus.Continue;
        }

        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private TaskStatus Pattern2_WaterSprout()
    {
        if (IsDead) return TaskStatus.Failure;
        if (!isPatternSetup)
        {
            curTimes[2] = WaterSprout_CoolTime; // TODO: 쿨타임 값 조정
            curTimes[0] = WaterSprout_DelayTime;  // TODO: 패턴 총 지속시간
            isPatternSetup = true;
            SpawnEye(1, WaterSprout_DelayTime);
            if (watersproutzone != null)
            {
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    watersproutzone.SpawnWaterBullets(WaterSprout_SpawnCount);
                });
            }
        }
        
        if (curTimes[0] > 0)
        {
            return TaskStatus.Continue;
        }

        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private TaskStatus Pattern3_Storm()
    {
        if (IsDead) return TaskStatus.Failure;
        if (!isPatternSetup)
        {
            curTimes[3] = 30f; // TODO: 쿨타임 값 조정
            curTimes[0] = 10f; // TODO: 패턴 총 지속시간
            isPatternSetup = true;
            SpawnEye(2, stormPatternDuration);
            Destroy(Eye, curTimes[0]);
            DOVirtual.DelayedCall(stormSpawnDelay, () =>
            {
                if (IsDead)
                    return;

                if (stormPrefab != null && stormSpawnPoint != null)
                {
                    Instantiate(
                        stormPrefab,
                        stormSpawnPoint.position,
                        stormSpawnPoint.rotation
                    );
                }
            }).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        if (curTimes[0] > 0) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private TaskStatus Pattern4_ElectricBall()
    {
        if (IsDead) return TaskStatus.Failure;
        if (!isPatternSetup)
        {
            curTimes[4] = electricBallCoolTime;
            curTimes[0] = electricBallPatternDuration;
            isPatternSetup = true;
            SpawnEye(3, electricBallPatternDuration);
            DOVirtual.DelayedCall(electricBallSpawnDelay, () =>
            {
                if (IsDead)
                    return;

                if (electricBallPrefab != null &&
                    electricBallSpawnPoint != null)
                {
                    Instantiate(
                        electricBallPrefab,
                        electricBallSpawnPoint.position,
                        electricBallSpawnPoint.rotation
                    );
                }
            }).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        if (curTimes[0] > 0) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;
    }
}

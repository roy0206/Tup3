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
    [SerializeField] private float normalEyeOpenTime = 5.5f;
    private readonly List<Water_eye> activeEyes = new();

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

    [Header("잠식 전조 및 파훼")]
    [SerializeField] private GameObject encroachmentWarningPrefab;
    [SerializeField] private Transform encroachmentWarningPoint;
    [SerializeField] private float encroachmentTelegraphDuration = 5f;
    [SerializeField] private Vector2 encroachmentSealCheckSize = new Vector2(2f, 2f);
    [SerializeField] private LayerMask encroachmentSealMask = -1;

    private BossPhase currentPhase = BossPhase.Normal;
    private float encroachmentTriggerHp;
    private float encroachmentTelegraphRemaining;
    private bool hasAttemptedEncroachment;
    private bool hasStartedWaterRise;
    private GameObject encroachmentWarningInstance;
    private Water_eye encroachmentEye;
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
            CleanupEncroachmentWarning();
            risingWaterPhase?.StopAndHide();

            foreach (Water_eye eye in activeEyes)
            {
                if (eye != null)
                    eye.ExpireByTime();
            }

            WaterBossAbsorption absorption = GetComponent<WaterBossAbsorption>();
            if (absorption != null)
                absorption.enabled = true;

            hasCleanedUpDeath = true;
        }

        gameObject.layer = LayerMask.NameToLayer("Default");

        return TaskStatus.Success;
    }

    private TaskStatus CheckEncroachmentTrigger()
    {
        if (currentPhase != BossPhase.Normal || hasAttemptedEncroachment)
            return TaskStatus.Failure;

        return Hp <= encroachmentTriggerHp
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }

    private TaskStatus EnterEncroachmentPhase()
    {
        if (currentPhase == BossPhase.Normal)
        {
            hasAttemptedEncroachment = true;
            currentPhase = BossPhase.EncroachmentTelegraph;
            isPatternSetup = false;
            hasStartedWaterRise = false;
            encroachmentTelegraphRemaining = encroachmentTelegraphDuration;

            encroachmentEye = SpawnEye(
                2,
                encroachmentTelegraphDuration + 5f,
                true
            );

            Transform warningPoint = encroachmentWarningPoint != null
                ? encroachmentWarningPoint
                : stormSpawnPoint;

            if (encroachmentWarningPrefab != null && warningPoint != null)
            {
                encroachmentWarningInstance = Instantiate(
                    encroachmentWarningPrefab,
                    warningPoint.position,
                    warningPoint.rotation
                );
            }
        }

        if (!hasStartedWaterRise && IsEncroachmentSealed())
        {
            CleanupEncroachmentWarning();
            if (encroachmentEye != null)
                encroachmentEye.ExpireByTime();

            currentPhase = BossPhase.Normal;
            return TaskStatus.Success;
        }

        if (!hasStartedWaterRise)
        {
            encroachmentTelegraphRemaining -= Time.deltaTime;
            if (encroachmentTelegraphRemaining > 0f)
                return TaskStatus.Continue;

            CleanupEncroachmentWarning();
            hasStartedWaterRise = true;

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

    private bool IsEncroachmentSealed()
    {
        Transform warningPoint = encroachmentWarningPoint != null
            ? encroachmentWarningPoint
            : stormSpawnPoint;

        if (warningPoint == null)
            return false;

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(
            warningPoint.position,
            encroachmentSealCheckSize,
            0f,
            encroachmentSealMask
        );

        foreach (Collider2D overlap in overlaps)
        {
            if (overlap != null && overlap.CompareTag("STOP"))
                return true;
        }

        return false;
    }

    private void CleanupEncroachmentWarning()
    {
        if (encroachmentWarningInstance != null)
        {
            Destroy(encroachmentWarningInstance);
            encroachmentWarningInstance = null;
        }
    }

    private TaskStatus PatternStarter(int num)
    {
        if (currentPhase == BossPhase.EncroachmentTelegraph)
            return TaskStatus.Failure;

        if (currentPhase != BossPhase.Encroached && num >= 3)
            return TaskStatus.Failure;

        if (curTimes[num] > 0) return TaskStatus.Failure;
        return TaskStatus.Success;
    }

    private bool isPatternSetup;

    private Water_eye SpawnEye(int eyeIndex, float lifeTime, bool damageable = true)
    {
        if (eyePrefab == null)
            return null;

        if (eyeSpawnPoints == null ||
            eyeIndex < 0 ||
            eyeIndex >= eyeSpawnPoints.Length ||
            eyeSpawnPoints[eyeIndex] == null)
        {
            Debug.LogWarning($"Water: 눈 {eyeIndex + 1} 생성 위치가 연결되지 않았습니다.", this);
            return null;
        }

        GameObject eyeObject = Instantiate(
            eyePrefab,
            eyeSpawnPoints[eyeIndex].position,
            eyeSpawnPoints[eyeIndex].rotation
        );

        Water_eye eye = eyeObject.GetComponent<Water_eye>();

        if (eye != null)
        {
            float eyeScale = Scale != null && eyeIndex < Scale.Length
                ? Scale[eyeIndex]
                : 1f;

            eye.Init(this, lifeTime, eyeScale, damageable);
            activeEyes.Add(eye);
        }

        return eye;
    }

    private TaskStatus Pattern1_IceBullet()
    {
        if (IsDead) return TaskStatus.Failure;
        if (!isPatternSetup)
        {
            curTimes[1] = iceBullet_DelayTime + iceBullet_CoolTime;
            curTimes[0] = iceBullet_DelayTime; // TODO: 패턴 총 지속시간 (애니메이션 길이에 맞춰서)
            isPatternSetup = true;
            
            // 얼음 발사는 눈2가 담당한다.
            SpawnEye(1, normalEyeOpenTime);
            
            if (iceBulletSpawnZones != null && iceBulletSpawnZones.Length > 0)
            {
                IceBulletSpawnZone chosenZone = iceBulletSpawnZones[UnityEngine.Random.Range(0, iceBulletSpawnZones.Length)];
                if (chosenZone != null)
                {
                    DOVirtual.DelayedCall(0.3f, () =>
                    {
                        if (!IsDead)
                            chosenZone.SpawnIceBullets(iceBullet_SpawnCount);
                    }).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
                }
            }

            // 잠식 이후에는 눈1 분출과 눈2 얼음발사가 함께 발동될 수 있다.
            if (currentPhase == BossPhase.Encroached && watersproutzone != null)
            {
                curTimes[2] = WaterSprout_DelayTime + WaterSprout_CoolTime;
                SpawnEye(0, normalEyeOpenTime);
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    if (!IsDead)
                        watersproutzone.SpawnWaterBullets(WaterSprout_SpawnCount);
                }).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
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
            curTimes[2] = WaterSprout_DelayTime + WaterSprout_CoolTime;
            curTimes[0] = WaterSprout_DelayTime;  // TODO: 패턴 총 지속시간
            isPatternSetup = true;
            // 물 분출은 눈1이 담당한다.
            SpawnEye(0, normalEyeOpenTime);
            if (watersproutzone != null)
            {
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    if (!IsDead)
                        watersproutzone.SpawnWaterBullets(WaterSprout_SpawnCount);
                }).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
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
            curTimes[3] = stormPatternDuration + stormCoolTime;
            curTimes[0] = stormPatternDuration;
            isPatternSetup = true;
            SpawnEye(2, stormPatternDuration);
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
            curTimes[4] = electricBallPatternDuration + electricBallCoolTime;
            curTimes[0] = electricBallPatternDuration;
            isPatternSetup = true;
            SpawnEye(3, normalEyeOpenTime, false);
            StartCoroutine(SpawnElectricBalls());
        }

        if (curTimes[0] > 0) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private IEnumerator SpawnElectricBalls()
    {
        yield return new WaitForSeconds(electricBallSpawnDelay);

        if (electricBallPrefab == null || electricBallSpawnPoint == null)
            yield break;

        for (int i = 0; i < 3; i++)
        {
            if (IsDead || currentPhase != BossPhase.Encroached)
                yield break;

            Instantiate(
                electricBallPrefab,
                electricBallSpawnPoint.position,
                electricBallSpawnPoint.rotation
            );

            if (i < 2)
                yield return new WaitForSeconds(electricBallPrefab.ChargeDuration);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform warningPoint = encroachmentWarningPoint != null
            ? encroachmentWarningPoint
            : stormSpawnPoint;

        if (warningPoint == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(warningPoint.position, encroachmentSealCheckSize);
    }
}

using UnityEngine;
using UnityEngine.Rendering;
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
    [SerializeField] private float iceBulletTelegraphTime = 1f;
    [SerializeField] private float iceBullet_CoolTime = 3f;

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
    [SerializeField] private float electricEyeOpenDuration = 0.35f;
    [SerializeField] private int electricBallShotCount = 3;
    [SerializeField] private float electricBallRechargeDelay = 3f;
    [SerializeField] private float electricBallPatternDuration = 1.5f;
    [SerializeField] private float electricBallCoolTime = 30f;

    [Header("잠식 2페이즈")]
    [SerializeField] private RisingWaterPhase risingWaterPhase;
    [SerializeField, Range(0f, 1f)] private float encroachmentHpRatio = 0.5f;

    [Header("사운드")]
    [SerializeField] private float roarSoundVolume = 1f;

    private const string RoarSound = "Water_Roar";

    private bool hasPlayedIntroRoar;
    private bool hasPlayedEncroachmentRoar;

    [Header("잠식 전조 및 파훼")]
    [SerializeField] private GameObject encroachmentWarningPrefab;
    [SerializeField] private Material encroachmentWarningMaterial;
    [SerializeField] private Color encroachmentWarningColor = Color.white;
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
        encroachmentTriggerHp = Hp * encroachmentHpRatio;
    }

    private void Update()
    {
        if (PauseManager.IsPaused || DialogueManager.IsDialogueActive) return;

        if (!hasPlayedIntroRoar && !IsDead)
        {
            hasPlayedIntroRoar = true;
            BossSound.Play(RoarSound, roarSoundVolume);
        }

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
            StopAllCoroutines();
            CleanupEncroachmentWarning();
            CleanupActiveHazards();
            risingWaterPhase?.BeginDrainAndHide();
            ExpireAllEyes();

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
            StopAllCoroutines();
            CleanupActiveHazards();
            ExpireAllEyes();
            isPatternSetup = false;
            curTimes[0] = 0f;
            hasStartedWaterRise = false;
            encroachmentTelegraphRemaining = encroachmentTelegraphDuration;

            if (!hasPlayedEncroachmentRoar)
            {
                hasPlayedEncroachmentRoar = true;
                BossSound.Play(RoarSound, roarSoundVolume);
            }

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

                ApplyEncroachmentWarningVisibility(encroachmentWarningInstance);

                MatchWarningAnimationToTelegraph(
                    encroachmentWarningInstance,
                    encroachmentTelegraphDuration
                );
            }
        }

        if (!hasStartedWaterRise && IsEncroachmentSealed())
        {
            CleanupEncroachmentWarning();

            currentPhase = BossPhase.Normal;
            isPatternSetup = false;
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

    private void ApplyEncroachmentWarningVisibility(GameObject warningInstance)
    {
        if (warningInstance == null)
            return;

        SpriteRenderer[] renderers = warningInstance.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer targetRenderer in renderers)
        {
            if (encroachmentWarningMaterial != null)
                targetRenderer.sharedMaterial = encroachmentWarningMaterial;

            targetRenderer.color = encroachmentWarningColor;
        }
    }

    private static void MatchWarningAnimationToTelegraph(
        GameObject warningInstance,
        float telegraphDuration
    )
    {
        if (warningInstance == null || telegraphDuration <= 0f)
            return;

        Animator warningAnimator = warningInstance.GetComponentInChildren<Animator>();
        RuntimeAnimatorController controller = warningAnimator != null
            ? warningAnimator.runtimeAnimatorController
            : null;

        if (controller == null)
            return;

        AnimationClip[] clips = controller.animationClips;
        if (clips == null || clips.Length == 0 || clips[0] == null)
            return;

        warningAnimator.speed = clips[0].length / telegraphDuration;
    }

    private void ExpireAllEyes()
    {
        for (int i = activeEyes.Count - 1; i >= 0; i--)
        {
            Water_eye eye = activeEyes[i];
            if (eye != null)
                eye.ExpireByTime();
        }

        activeEyes.Clear();
    }

    private void CleanupActiveHazards()
    {
        DeactivateAndDestroyAll<Water_Sprout>();
        DeactivateAndDestroyAll<Ice_Bullet>();
        DeactivateAndDestroyAll<Storm>();
        DeactivateAndDestroyAll<Electric_ball>();
    }

    private static void DeactivateAndDestroyAll<T>() where T : MonoBehaviour
    {
        T[] hazards = FindObjectsByType<T>(FindObjectsSortMode.None);
        foreach (T hazard in hazards)
        {
            if (hazard == null)
                continue;

            hazard.gameObject.SetActive(false);
            Destroy(hazard.gameObject);
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

    /// <summary>
    /// 2페이즈에서 수위가 올라오면 물(waterRoot)이 앞을 덮으므로,
    /// 런타임에 생성한 연출물의 sortingOrder 를 물보다 위로 끌어올린다.
    /// </summary>
    private void LiftAboveWater(GameObject target, int offset = 1)
    {
        RisingWaterPhase.LiftAboveWater(target, offset);
    }

    private Water_eye SpawnEye(
        int eyeIndex,
        float lifeTime,
        bool damageable = true,
        bool startClosed = false
    )
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

            eye.Init(this, lifeTime, eyeScale, damageable, startClosed);

            if (risingWaterPhase != null)
                eye.SetMinimumSortingOrder(risingWaterPhase.GetSortingOrderAboveWater());

            activeEyes.RemoveAll(activeEye => activeEye == null);
            activeEyes.Add(eye);
        }
        else
        {
            Debug.LogError("Water: Eye Prefab에 Water_eye 컴포넌트가 없습니다.", eyeObject);
            Destroy(eyeObject);
        }

        return eye;
    }

    private TaskStatus Pattern1_IceBullet()
    {
        if (IsDead) return TaskStatus.Failure;
        if (!isPatternSetup)
        {
            IceBulletSpawnZone chosenZone = null;
            if (iceBulletSpawnZones != null && iceBulletSpawnZones.Length > 0)
                chosenZone = iceBulletSpawnZones[UnityEngine.Random.Range(0, iceBulletSpawnZones.Length)];

            float patternDuration = chosenZone != null
                ? chosenZone.GetPatternDuration(iceBulletTelegraphTime)
                : iceBulletTelegraphTime;

            curTimes[1] = patternDuration + iceBullet_CoolTime;
            curTimes[0] = patternDuration;
            isPatternSetup = true;
            
            // 얼음 발사는 눈2가 담당한다.
            SpawnEye(1, normalEyeOpenTime);
            
            if (chosenZone != null)
            {
                chosenZone.SpawnIceBullets(
                    iceBullet_SpawnCount,
                    iceBulletTelegraphTime
                );
            }

            // 잠식 이후에는 눈1 분출과 눈2 얼음발사가 함께 발동될 수 있다.
            if (currentPhase == BossPhase.Encroached && watersproutzone != null)
            {
                curTimes[2] = WaterSprout_DelayTime + WaterSprout_CoolTime;
                SpawnEye(0, normalEyeOpenTime);
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    if (!IsDead && currentPhase == BossPhase.Encroached)
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
                    if (!IsDead && currentPhase != BossPhase.EncroachmentTelegraph)
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
                if (IsDead || currentPhase != BossPhase.Encroached)
                    return;

                if (stormPrefab != null && stormSpawnPoint != null)
                {
                    Storm storm = Instantiate(
                        stormPrefab,
                        stormSpawnPoint.position,
                        stormSpawnPoint.rotation
                    );
                    LiftAboveWater(storm.gameObject);
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
            float patternDuration = GetElectricBallSequenceDuration();
            curTimes[4] = patternDuration + electricBallCoolTime;
            curTimes[0] = patternDuration;
            isPatternSetup = true;
            Water_eye electricEye = SpawnEye(
                3,
                patternDuration,
                false,
                true
            );
            StartCoroutine(SpawnElectricBalls(electricEye));
        }

        if (curTimes[0] > 0) return TaskStatus.Continue;

        isPatternSetup = false;
        return TaskStatus.Success;
    }

    private float GetElectricBallSequenceDuration()
    {
        float chargeDuration = electricBallPrefab != null
            ? electricBallPrefab.ChargeDuration
            : 0f;
        int shotCount = Mathf.Max(1, electricBallShotCount);
        int rechargeCount = Mathf.Max(0, shotCount - 1);
        float sequenceDuration =
            electricBallSpawnDelay +
            electricEyeOpenDuration +
            chargeDuration * shotCount +
            electricBallRechargeDelay * rechargeCount;

        return Mathf.Max(electricBallPatternDuration, sequenceDuration);
    }

    private IEnumerator SpawnElectricBalls(Water_eye electricEye)
    {
        yield return new WaitForSeconds(electricBallSpawnDelay);

        if (IsDead || currentPhase != BossPhase.Encroached)
            yield break;

        if (electricEye != null)
            electricEye.OpenEye();

        if (electricEyeOpenDuration > 0f)
            yield return new WaitForSeconds(electricEyeOpenDuration);

        if (electricBallPrefab == null || electricBallSpawnPoint == null)
            yield break;

        int shotCount = Mathf.Max(1, electricBallShotCount);
        for (int i = 0; i < shotCount; i++)
        {
            yield return PauseManager.WaitWhilePaused();

            if (IsDead || currentPhase != BossPhase.Encroached)
                yield break;

            Electric_ball electricBall = Instantiate(
                electricBallPrefab,
                electricBallSpawnPoint.position,
                electricBallSpawnPoint.rotation
            );
            LiftAboveWater(electricBall.gameObject);

            if (electricBallPrefab.ChargeDuration > 0f)
                yield return new WaitForSeconds(electricBallPrefab.ChargeDuration);

            if (i < shotCount - 1 && electricBallRechargeDelay > 0f)
                yield return new WaitForSeconds(electricBallRechargeDelay);
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

    private void OnValidate()
    {
        WaterSprout_SpawnCount = Mathf.Clamp(WaterSprout_SpawnCount, 1, 5);
        WaterSprout_DelayTime = Mathf.Max(0, WaterSprout_DelayTime);
        WaterSprout_CoolTime = Mathf.Max(0, WaterSprout_CoolTime);

        iceBullet_SpawnCount = Mathf.Clamp(iceBullet_SpawnCount, 1, 5);
        iceBulletTelegraphTime = Mathf.Max(0f, iceBulletTelegraphTime);
        iceBullet_CoolTime = Mathf.Max(0, iceBullet_CoolTime);

        normalEyeOpenTime = Mathf.Max(0.1f, normalEyeOpenTime);
        stormSpawnDelay = Mathf.Max(0f, stormSpawnDelay);
        stormPatternDuration = Mathf.Max(0.1f, stormPatternDuration);
        stormCoolTime = Mathf.Max(0f, stormCoolTime);

        electricBallSpawnDelay = Mathf.Max(0f, electricBallSpawnDelay);
        electricEyeOpenDuration = Mathf.Max(0f, electricEyeOpenDuration);
        electricBallShotCount = Mathf.Max(1, electricBallShotCount);
        electricBallRechargeDelay = Mathf.Max(0f, electricBallRechargeDelay);
        electricBallPatternDuration = Mathf.Max(0.1f, electricBallPatternDuration);
        electricBallCoolTime = Mathf.Max(0f, electricBallCoolTime);

        encroachmentTelegraphDuration = Mathf.Max(0f, encroachmentTelegraphDuration);
        encroachmentSealCheckSize.x = Mathf.Max(0.1f, encroachmentSealCheckSize.x);
        encroachmentSealCheckSize.y = Mathf.Max(0.1f, encroachmentSealCheckSize.y);
    }

    private void OnDestroy()
    {
        CleanupEncroachmentWarning();
    }
}

/* [파일 노트]
 * 일시정지 대응 : Update 첫 줄 PauseManager.IsPaused 게이트로 BT/쿨타임/잠식 전조 타이머가 멈춘다.
 * 얼음총알·분수·스톰 소환 예약(DOVirtual.DelayedCall)과 수위 상승 트윈(RisingWaterPhase)은
 * DOTween.PauseAll 로 함께 멈추고, 전기 구체 연속 소환 코루틴은 루프마다 WaitWhilePaused 로 대기한다.
 * Water_eye 의 수명 타이머(Invoke)는 실시간으로 흘러 일시정지 중 만료될 수 있다(플레이어에게 불리하지 않음).
 *
 * 사운드 Water_Roar (등장 / 포효) — 두 지점에서 각각 1회씩만 울린다.
 *   1) 등장 : Update 의 일시정지·대사 게이트를 처음 통과하는 프레임. 수보스에는 별도의 등장 연출
 *      메서드가 없고, BossRoom 이 도입 대사 동안 DialogueManager 로 이 Update 를 막아 두므로
 *      "게이트를 처음 통과한 순간 = 전투가 실제로 시작된 순간"이다. hasPlayedIntroRoar 로 1회 고정.
 *   2) 잠식 전조 진입(EnterEncroachmentPhase 의 최초 진입 블록) : 체력 절반에서 수위 상승을 예고하는
 *      페이즈 전환 순간이라 같은 포효를 다시 쓴다. hasPlayedEncroachmentRoar 로 1회 고정 —
 *      전조가 STOP 으로 파훼되면 currentPhase 가 Normal 로 돌아가지만 이 플래그는 유지되므로
 *      (hasAttemptedEncroachment 와 마찬가지로) 재시도 시 포효가 다시 울리지는 않는다.
 * 나머지 수보스 소리는 전부 소환물 쪽에 있다 :
 *   Water_Sprout(Water_Sprout.cs) / Water_IceBullet(Ice_Bullet.cs) / Water_Tornado(Storm.cs) /
 *   Water_Skill(Electric_ball.cs) / Water_Rising · Water_Splash(RisingWaterPhase.cs).
 *
 * ── 2페이즈 생성물이 물에 가리는 문제 (2026-08-29) ────────────────────────────
 * 수위가 올라오면 물(Boss_Water 의 Water_start, sortingOrder 9)이 화면 앞을 덮는다.
 * RisingWaterPhase.KeepCombatantsVisibleAboveWater() 는 BeginRise 시점에 한 번만 돌면서
 * 씬에 미리 놓인 발판과 플레이어만 올려 주므로, 수위가 오른 뒤 Instantiate 되는 것들은 그대로 묻힌다.
 * LiftAboveWater(go) 가 그 구멍을 메운다 — risingWaterPhase 에게 물의 실제 순서를 물어 +1 로 올린다.
 * 적용 대상 :
 *   - 눈(Water_eye)   : SpawnEye 안에서 eye.SetMinimumSortingOrder(...) 로 처리 (모든 패턴 공용)
 *   - 폭풍(Storm)     : Pattern3_Storm — 2페이즈 전용 패턴
 *   - 전기구슬        : SpawnElectricBalls — 2페이즈 전용 패턴. 프리팹 순서가 0 이라 특히 심했다.
 * 패턴3·4 는 PatternStarter 의 `num >= 3` 가드 때문에 Encroached(2페이즈)에서만 도는 패턴이라
 * 물에 가리면 패턴 자체가 보이지 않는 것과 같다.
 * 얼음탄(IceBullet)은 프리팹 순서가 7 / 경로 6 으로 물(9)보다 낮지만 1·2페이즈 공용 패턴이고
 * 별도 minimumPathSortingOrder 체계를 갖고 있어 이번 변경 범위에서 제외했다 — 별도 확인 필요.
 */

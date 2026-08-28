using UnityEngine;

public class ShallowWaterZone : MonoBehaviour
{
    [Header("이동 감속")]
    [SerializeField, Range(0.1f, 1f)] private float playerSpeedMultiplier = 0.7f;

    [Header("물튀김")]
    [SerializeField] private float surfaceLocalY = 0.3f;
    [SerializeField] private float footSplashInterval = 0.16f;
    [SerializeField] private float footSplashMinSpeed = 1.5f;
    [SerializeField] private int footSplashCount = 5;
    [SerializeField] private float landSplashFallSpeed = 5f;
    [SerializeField] private int landSplashCount = 26;
    [SerializeField] private Color splashColor = new Color(0.75f, 0.85f, 0.95f, 0.85f);
    [SerializeField] private int splashSortingOrder = 31;

    [Header("첨벙 소리")]
    [SerializeField, Range(0f, 1f)] private float wadeStepVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float wadeSplashVolume = 0.9f;
    [SerializeField] private float wadeMinInterval = 0.2f;

    private const string SoundWade = "Water_Wade";

    private float lastWadeTime = -999f;

    private Playermovement slowedPlayer;
    private Transform trackedPlayer;
    private ParticleSystem splashPs;

    private Vector3 lastPlayerPos;
    private float lastVerticalSpeed;
    private float footSplashTimer;
    private bool hasLastPos;

    private void Awake()
    {
        BuildSplashParticles();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryAttachWaterBody(other);

        var pm = other.GetComponent<Playermovement>();
        if (pm == null) return;

        if (slowedPlayer == null)
        {
            slowedPlayer = pm;
            pm.moveSpeed *= playerSpeedMultiplier;
        }

        trackedPlayer = pm.transform;
        hasLastPos = false;
        EmitSplash(new Vector2(trackedPlayer.position.x, SurfaceY), landSplashCount / 2, 1f);
        PlayWade(wadeSplashVolume, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var pm = other.GetComponent<Playermovement>();
        if (pm == null) return;

        if (pm == slowedPlayer) RestoreSpeed();
        if (trackedPlayer == pm.transform) trackedPlayer = null;
    }

    private void OnDisable()
    {
        RestoreSpeed();
        trackedPlayer = null;
    }

    private void Update()
    {
        if (PauseManager.IsPaused) return;
        if (trackedPlayer == null) return;

        Vector3 pos = trackedPlayer.position;
        if (!hasLastPos)
        {
            lastPlayerPos = pos;
            lastVerticalSpeed = 0f;
            hasLastPos = true;
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float horizontalSpeed = Mathf.Abs(pos.x - lastPlayerPos.x) / dt;
        float verticalSpeed = (pos.y - lastPlayerPos.y) / dt;

        if (horizontalSpeed >= footSplashMinSpeed)
        {
            footSplashTimer -= dt;
            if (footSplashTimer <= 0f)
            {
                footSplashTimer = footSplashInterval;
                EmitSplash(new Vector2(pos.x, SurfaceY), footSplashCount, 0.6f);
                PlayWade(wadeStepVolume, false);
            }
        }
        else
        {
            footSplashTimer = 0f;
        }

        bool landedNow = lastVerticalSpeed < -landSplashFallSpeed && verticalSpeed > -0.5f;
        if (landedNow)
        {
            float power = Mathf.Clamp01(-lastVerticalSpeed / (landSplashFallSpeed * 3f)) + 0.6f;
            EmitSplash(new Vector2(pos.x, SurfaceY), landSplashCount, power);
            PlayWade(Mathf.Clamp01(wadeSplashVolume * power), true);
        }

        lastPlayerPos = pos;
        lastVerticalSpeed = verticalSpeed;
    }

    private float SurfaceY => transform.position.y + surfaceLocalY;

    private void BuildSplashParticles()
    {
        var go = new GameObject("SplashParticles");
        go.transform.SetParent(transform, false);

        splashPs = go.AddComponent<ParticleSystem>();
        var main = splashPs.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 400;
        main.gravityModifier = 2.4f;
        main.startColor = splashColor;

        var emission = splashPs.emission;
        emission.enabled = false;

        var shape = splashPs.shape;
        shape.enabled = false;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = splashSortingOrder;
    }

    private void EmitSplash(Vector2 origin, int count, float power)
    {
        if (splashPs == null || count <= 0) return;

        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            ep.position = origin + new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(0f, 0.08f));
            ep.velocity = new Vector3(
                Random.Range(-1.6f, 1.6f) * power,
                Random.Range(1.6f, 3.8f) * power,
                0f);
            ep.startLifetime = Random.Range(0.25f, 0.5f);
            ep.startSize = Random.Range(0.05f, 0.14f);
            splashPs.Emit(ep, 1);
        }
    }

    private void PlayWade(float volume, bool bypassInterval)
    {
        if (!bypassInterval && Time.time - lastWadeTime < wadeMinInterval) return;

        lastWadeTime = Time.time;
        AudioManager.Instance.PlaySound(SoundWade, volume);
    }

    private void RestoreSpeed()
    {
        if (slowedPlayer == null) return;

        slowedPlayer.moveSpeed /= playerSpeedMultiplier;
        slowedPlayer = null;
    }

    private void TryAttachWaterBody(Collider2D other)
    {
        if (other.transform.IsChildOf(transform)) return;
        if (other.GetComponent<SimWater.WaterBody>() != null) return;

        bool isCharacter = other.GetComponentInParent<Playermovement>() != null
            || other.GetComponentInParent<BossBase>() != null;
        bool isMovingObject = other.attachedRigidbody != null;
        if (!isCharacter && !isMovingObject) return;

        other.gameObject.AddComponent<SimWater.WaterBody>();
    }
}

/* [파일 노트]
 * 얕은 물(삼도천) 존 — 발목 높이 물에 있는 동안 플레이어 이동속도에 배율(기본 0.7 = 30% 감속)을
 * 곱하고, 물리 반응처럼 보이는 물튀김 파티클을 낸다.
 *
 * 감지 : 프로젝트 관례(UnderWater 참고)대로 존 쪽 Kinematic Rigidbody2D(simulated) + 트리거로
 *        플레이어 루트 콜라이더의 Enter/Exit 를 받는다.
 * 감속 : 절대값 저장/복원이 아닌 곱하기/나누기 방식 — 스킬 2(이속 버프)의 저장→배율→복원과
 *        겹쳐도 대체로 안전. 전장 전체가 물이라 전투 중 존을 벗어날 일이 없다.
 *        OnDisable 복원으로 씬 전환 시 감속 잔류를 막는다.
 * 물튀김 : 에셋 없이 코드로 구성한 ParticleSystem(Sprites/Default 사각 입자, 월드 공간, 중력).
 *        - 발걸음: 수평 속도가 footSplashMinSpeed 이상인 동안 footSplashInterval 마다 소량 분출.
 *        - 착수: 직전 프레임 낙하 속도가 landSplashFallSpeed 를 넘다가 멈추면 낙하 속도에
 *          비례한 큰 물보라. 존 진입 순간에도 중간 크기 분출.
 *        - 분출 높이는 SurfaceY(transform.y + surfaceLocalY) — 물 표면 기준.
 *        입자 색·양·정렬(splashSortingOrder, 앞 물 레이어 30보다 위)은 인스펙터 조절.
 * 일시정지 : Update 첫 줄 게이트. 이미 떠 있는 입자는 PauseManager 의 파티클 정지가 함께 멈춘다.
 * 보스는 감속·물튀김 대상이 아니다(도플갱어는 삼도천의 주인이라는 설정 — 필요 시 trackedPlayer
 * 방식을 일반화해 확장).
 * 물 시뮬 연동 : Tavern_Gamejam_CAU_SSU 에서 이식한 SimWater 물리 수면과 공존한다.
 *        TryAttachWaterBody — 존에 들어온 모든 움직이는 물체(플레이어/보스 계열 + Rigidbody2D 가
 *        붙은 투사체 전부: SoilWave·FlyingSword 등)에 SimWater.WaterBody 를 런타임
 *        AddComponent(씬/프리팹 무수정 원칙). 물 시스템 자기 자신(자식)과 RB 없는 정적
 *        지형은 제외. WaterBody 는 RB 가 Dynamic 이 아니면 트랜스폼 델타로 속도를 추정하고
 *        수평 속력 일부를 웨이크로 섞어 수면(SimWater.Water)을 출렁이게 한다.
 *        풀 재사용 투사체는 WaterBody 가 남아 있어도 가드로 중복 부착되지 않고,
 *        OnEnable 에서 속도 추정이 리셋돼 리스폰 순간 스파이크가 없다.
 *
 * 효과음 배선 (Water_Wade)
 *  파티클과 소리가 따로 놀지 않도록 EmitSplash 를 부르는 세 지점에 그대로 붙였다.
 *   - 존 진입(OnTriggerEnter2D) : 착수 물보라와 함께 wadeSplashVolume 로 1회.
 *   - 발걸음(Update, footSplashInterval 마다) : wadeStepVolume 로. 파티클 간격(0.16초)이 짧아
 *     소리가 겹칠 수 있으므로 PlayWade 가 wadeMinInterval(기본 0.2초) 게이트를 추가로 건다.
 *   - 착수(landedNow) : 낙하 속도에 비례한 power 를 볼륨에 곱해 재생. 이 두 경우(진입/착수)는
 *     드물고 연출상 중요해 bypassInterval 로 간격 게이트를 건너뛰되 타이머는 갱신한다
 *     (착수 직후 발소리가 곧바로 겹쳐 나지 않게).
 *  Playermovement 의 Player_Footstep 과는 별개 채널이라 얕은 물에서는 발소리 + 첨벙이 함께 난다.
 */

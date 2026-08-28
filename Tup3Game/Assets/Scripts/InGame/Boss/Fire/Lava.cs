using UnityEngine;

public class Lava : MonoBehaviour
{
    [SerializeField] private float gravity;
    [SerializeField] private float limitX;
    [SerializeField] private float groundY;

    private Vector2 origin;
    private Vector2 initialVelocity;
    private float flightTime;
    private float curTime;

    private bool hardenOnLand;
    private float hardenDuration;
    private int hardenMaxCount;
    private Color hardenedColor;

    [Header("사운드")]
    [SerializeField] private float landVolume = 0.8f;
    [SerializeField] private float landMinInterval = 0.12f;

    private const string LandSound = "Fire_LavaLand";

    private void OnEnable()
    {
        curTime = 0;
        origin = transform.position;

        hardenOnLand = false;

        float dx = UnityEngine.Random.Range(-limitX, limitX) - origin.x;
        float dy = groundY - origin.y;

        flightTime = UnityEngine.Random.Range(2f, 3f);
        initialVelocity = new Vector2(
            dx / flightTime,
            (dy + 0.5f * gravity * flightTime * flightTime) / flightTime);
    }

    private void Update()
    {
        if (PauseManager.IsPaused) return;

        curTime += Time.deltaTime;

        if (curTime >= flightTime)
        {
            Land();
            return;
        }
        var targetPos = PositionAt(curTime);
        var targetVec = targetPos - (Vector2)transform.position;
        transform.position = targetPos;

        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(targetVec.y, targetVec.x) * Mathf.Rad2Deg + 90);
    }

    public void LaunchTo(Vector2 landPoint, Vector2 flightTimeRange)
    {
        curTime = 0f;
        origin = transform.position;

        float dx = landPoint.x - origin.x;
        float dy = landPoint.y - origin.y;

        flightTime = UnityEngine.Random.Range(flightTimeRange.x, flightTimeRange.y);
        if (flightTime <= 0.01f) flightTime = 0.01f;

        initialVelocity = new Vector2(
            dx / flightTime,
            (dy + 0.5f * gravity * flightTime * flightTime) / flightTime);
    }

    public void SetHardenOnLand(float duration, int maxCount, Color color)
    {
        hardenOnLand = true;
        hardenDuration = duration;
        hardenMaxCount = maxCount;
        hardenedColor = color;
    }

    private Vector2 PositionAt(float t)
    {
        return new Vector2(
            origin.x + initialVelocity.x * t,
            origin.y + initialVelocity.y * t - 0.5f * gravity * t * t);
    }

    private void Land()
    {
        BossSound.PlayThrottled(LandSound, landVolume, landMinInterval);

        var pool = PoolManager.Instance.Get("LavaPool", PositionAt(flightTime), Quaternion.identity);
        if (pool != null)
        {
            if (hardenOnLand && pool.TryGetComponent(out LavaPool lavaPool))
                lavaPool.Harden(hardenDuration, hardenMaxCount, hardenedColor);
            else
                PoolManager.Instance.Release(pool, 10f);
        }

        PoolManager.Instance.Release(gameObject);
    }
}

/* [파일 노트]
 * 기본 경로(화보스 LavaJet) : OnEnable 에서 화보스 경기장 기준(limitX/groundY 직렬화 값)으로
 * 무작위 착지점과 체공시간을 스스로 계산한다.
 *
 * LaunchTo(landPoint, flightTimeRange) : 외부 소환자(최종보스 화 돌진 마무리)가 OnEnable 과
 * "완전히 같은 공식"으로 궤적을 다시 푸는 경로다 — 무작위 체공시간에 맞춰 포물선 초기속도를
 * 역산한다(dx/T, (dy + 0.5·g·T²)/T). 다른 점은 착지점을 누가 정하느냐뿐이다:
 * 화보스는 경기장이 원점 대칭이라 절대 x 범위(-limitX~limitX)와 절대 groundY 로 스스로 뽑지만,
 * 삼도천은 지면이 유한하므로 호출 측(FinalBoss)이 착지 x 를 뽑고 그 x 의 실제 지면 높이를
 * 레이캐스트로 재서 (x, y) 를 통째로 넘긴다. 착지점을 호출 측이 알아야 지면 검증이 가능하다.
 * 직렬화 필드(limitX/groundY)는 읽지 않으므로 화보스 프리팹 값에 영향이 없다.
 *
 * SetHardenOnLand(duration, maxCount, color) : 착지 시 생성되는 LavaPool 을 "굳는 용암" 으로
 * 만든다. 이 경우 Land() 는 LavaPool 에 Release 예약을 걸지 않고 LavaPool.Harden 에 위임한다
 * (수명/상한/데미지 해제는 LavaPool 이 관리). 플래그는 OnEnable 에서 항상 false 로 리셋되므로
 * 같은 풀 인스턴스를 화보스가 다시 꺼내 써도 기본 경로(10초 뒤 반납)로 돌아간다.
 *
 * 사운드 Fire_LavaLand : Land() 진입 즉시 재생 — 화염구가 지면에 닿아 장판으로 바뀌는 순간이다.
 * 이 파일은 화보스(LavaJet)와 최종보스(화 돌진 마무리 화염구)가 공유하는데, 두 경로 모두
 * "화염구가 떨어져 착지한다"는 같은 사건이고 배정된 소리도 같으므로 호출자 주입(SetHardenOnLand /
 * LaunchTo 같은 방식)을 쓰지 않고 공용으로 두었다. 최종보스만 다른 소리를 원하게 되면
 * 그때 소환자 주입 세터를 추가하면 된다.
 * landMinInterval(기본 0.12초) 스로틀이 필요한 이유 : 최종보스는 화염구를 한 번에 8개 쏘고
 * 체공시간이 2~3초 무작위라 여러 개가 거의 동시에 착지할 수 있다.
 */

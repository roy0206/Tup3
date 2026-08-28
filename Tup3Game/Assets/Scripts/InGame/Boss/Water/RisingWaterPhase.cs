using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

public class RisingWaterPhase : MonoBehaviour
{
    [Header("물 오브젝트")]
    [SerializeField] private Transform waterRoot;
    [SerializeField] private Transform surfacePoint;

    [Header("수위")]
    [SerializeField] private float startY = -8f;
    [SerializeField] private float targetY = 1.5f;
    [SerializeField] private float riseDuration = 4f;
    [SerializeField] private Ease riseEase = Ease.InOutSine;

    [Header("수영 판정 여유")]
    [SerializeField, Min(0f)] private float enterDepth = 0.05f;
    [SerializeField, Min(0f)] private float exitDepth = 0.15f;

    [Header("수면 앞쪽 표시")]
    [Tooltip("물이 올라와도 발판이 물보다 앞에 보이도록 보정할 최소 순서")]
    [SerializeField] private int minimumPlatformSortingOrder = 1;
    [SerializeField] private int playerOrderAbovePlatforms = 1;

    [Header("사운드")]
    [SerializeField] private float risingVolume = 1f;
    [SerializeField] private float splashVolume = 0.7f;
    [SerializeField] private float splashMinInterval = 0.4f;

    private const string RisingSound = "Water_Rising";
    private const string SplashSound = "Water_Splash";

    private Playermovement player;
    private Collider2D playerCollider;
    private Tween riseTween;
    private bool hasStarted;
    private bool playerIsSwimming;
    private float sceneStartY;
    private bool hasCachedSceneStartY;

    public bool HasReachedTarget { get; private set; }

    private void Awake()
    {
        CacheSceneStartPosition();
        FindPlayer();
        PrepareWater();
    }

    private void Update()
    {
        if (PauseManager.IsPaused) return;

        if (!hasStarted)
            return;

        if (player == null)
            FindPlayer();

        if (player == null || waterRoot == null)
            return;

        float surfaceY = GetCurrentSurfaceY();

        float playerBodyY = playerCollider != null
            ? playerCollider.bounds.center.y
            : player.transform.position.y;

        float submergedDepth = surfaceY - playerBodyY;

        if (!playerIsSwimming && submergedDepth >= enterDepth)
        {
            SetPlayerSwimming(true);
        }
        else if (playerIsSwimming && submergedDepth <= -exitDepth)
        {
            SetPlayerSwimming(false);
        }
    }

    public bool BeginRise()
    {
        if (hasStarted)
            return true;

        if (waterRoot == null)
        {
            Debug.LogError("RisingWaterPhase: Water Root가 연결되지 않았습니다.", this);
            return false;
        }

        hasStarted = true;
        HasReachedTarget = false;
        BossSound.Play(RisingSound, risingVolume);

        float riseStartY = GetRiseStartY();
        float riseEndY = GetRiseEndY();

        waterRoot.gameObject.SetActive(true);
        SetWaterY(riseStartY);
        KeepCombatantsVisibleAboveWater();

        riseTween?.Kill();

        if (riseDuration <= 0f)
        {
            SetWaterY(riseEndY);
            HasReachedTarget = true;
            return true;
        }

        riseTween = waterRoot
            .DOMoveY(riseEndY, riseDuration)
            .SetEase(riseEase)
            .OnComplete(() => HasReachedTarget = true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        return true;
    }

    public void StopAndHide()
    {
        riseTween?.Kill();
        riseTween = null;

        hasStarted = false;
        HasReachedTarget = false;
        SetPlayerSwimming(false);

        if (waterRoot != null)
        {
            SetWaterY(GetRiseStartY());
            waterRoot.gameObject.SetActive(false);
        }
    }

    private void PrepareWater()
    {
        if (waterRoot == null)
            return;

        CacheSceneStartPosition();
        SetWaterY(GetRiseStartY());
        KeepCombatantsVisibleAboveWater();
        waterRoot.gameObject.SetActive(false);
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
            return;

        player = playerObject.GetComponent<Playermovement>();
        playerCollider = playerObject.GetComponent<Collider2D>();

        if (player != null && waterRoot != null)
            KeepCombatantsVisibleAboveWater();
    }

    private float GetCurrentSurfaceY()
    {
        if (waterRoot == null)
            return float.NegativeInfinity;

        if (surfacePoint == null)
            return waterRoot.position.y;

        // Surface Point가 Water Root의 자식이면 실제 수면 위치로 사용한다.
        if (surfacePoint == waterRoot || surfacePoint.IsChildOf(waterRoot))
            return surfacePoint.position.y;

        // 현재 씬처럼 서로 형제인 경우 Surface Point가 실제 이동 도착점이다.
        // 도착점과 Water Root의 현재 위치 차이만큼 현재 수면도 함께 이동한다.
        return surfacePoint.position.y + (waterRoot.position.y - GetRiseEndY());
    }

    private void CacheSceneStartPosition()
    {
        if (waterRoot == null || hasCachedSceneStartY)
            return;

        sceneStartY = waterRoot.position.y;
        hasCachedSceneStartY = true;
    }

    private float GetRiseStartY()
    {
        CacheSceneStartPosition();
        return hasCachedSceneStartY ? sceneStartY : startY;
    }

    private float GetRiseEndY()
    {
        return surfacePoint != null ? surfacePoint.position.y : targetY;
    }

    private void KeepCombatantsVisibleAboveWater()
    {
        if (waterRoot == null)
            return;

        int highestWaterOrder = 0;
        bool foundWaterRenderer = false;

        foreach (Renderer renderer in waterRoot.GetComponentsInChildren<Renderer>(true))
        {
            highestWaterOrder = foundWaterRenderer
                ? Mathf.Max(highestWaterOrder, renderer.sortingOrder)
                : renderer.sortingOrder;
            foundWaterRenderer = true;
        }

        int platformOrder = Mathf.Max(
            minimumPlatformSortingOrder,
            highestWaterOrder + 1
        );

        Collider2D[] colliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        foreach (Collider2D platformCollider in colliders)
        {
            if (platformCollider == null ||
                (!platformCollider.CompareTag("ChangeablePlatform") &&
                 !platformCollider.CompareTag("Slippery")))
            {
                continue;
            }

            SetMinimumSortingOrder(platformCollider.gameObject, platformOrder);
        }

        if (player != null)
        {
            SetMinimumSortingOrder(
                player.gameObject,
                platformOrder + Mathf.Max(0, playerOrderAbovePlatforms)
            );
        }
    }

    private static void SetMinimumSortingOrder(GameObject target, int minimumOrder)
    {
        foreach (SpriteRenderer renderer in target.GetComponentsInChildren<SpriteRenderer>(true))
            renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, minimumOrder);

        foreach (SortingGroup sortingGroup in target.GetComponentsInChildren<SortingGroup>(true))
            sortingGroup.sortingOrder = Mathf.Max(sortingGroup.sortingOrder, minimumOrder);
    }

    private void SetWaterY(float y)
    {
        Vector3 position = waterRoot.position;
        position.y = y;
        waterRoot.position = position;
    }

    private void SetPlayerSwimming(bool value)
    {
        if (playerIsSwimming == value)
            return;

        playerIsSwimming = value;

        if (value && hasStarted)
            BossSound.PlayThrottled(SplashSound, splashVolume, splashMinInterval);

        if (player != null)
            player.SetInWater(this, value);
    }

    private void OnDisable()
    {
        riseTween?.Kill();
        riseTween = null;
        hasStarted = false;
        HasReachedTarget = false;
        SetPlayerSwimming(false);
    }

    private void OnDestroy()
    {
        riseTween?.Kill();
        SetPlayerSwimming(false);
    }

    private void OnValidate()
    {
        riseDuration = Mathf.Max(0f, riseDuration);
        enterDepth = Mathf.Max(0f, enterDepth);
        exitDepth = Mathf.Max(enterDepth, exitDepth);
        playerOrderAbovePlatforms = Mathf.Max(0, playerOrderAbovePlatforms);
    }
}

/* [파일 노트]
 * 사운드
 *   Water_Rising : BeginRise() 가 실제로 수위 상승을 시작하는 지점에서 1회.
 *                  BeginRise 는 hasStarted 가 이미 true 면 맨 위에서 즉시 return 하므로
 *                  중복 호출로 두 번 울리지 않는다.
 *   Water_Splash : 플레이어가 수면 아래로 들어가 수영 상태로 바뀌는 순간(SetPlayerSwimming(true)).
 *                  "물소리(범용)" 를 여기에 쓴 근거 : 수보스 스크립트 안에서 플레이어와 물이 직접
 *                  부딪히는 사건이 이곳 하나뿐이고, 나머지 물 관련 사건(분출·고드름·토네이도·수위)은
 *                  전용 이름이 이미 배정돼 있다.
 *                  물에서 나오는 전환에는 붙이지 않았다 — 정리 경로(StopAndHide/OnDisable/OnDestroy)가
 *                  전부 SetPlayerSwimming(false) 를 부르므로 보스 사망·씬 종료 때 엉뚱하게 울린다.
 *                  splashMinInterval(기본 0.4초)은 수면 근처에서 오르내릴 때의 연타를 막는다
 *                  (enterDepth/exitDepth 히스테리시스가 이미 있지만 파도 위에서 반복될 수 있다).
 */

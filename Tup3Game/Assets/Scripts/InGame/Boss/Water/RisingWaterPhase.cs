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

    [Header("사망 시 배수")]
    [SerializeField] private float drainDuration = 2.5f;
    [SerializeField] private Ease drainEase = Ease.InSine;

    [Header("수영 판정 여유")]
    [SerializeField, Min(0f)] private float enterDepth = 0.05f;
    [SerializeField, Min(0f)] private float exitDepth = 0.15f;

    [Header("수면 앞쪽 표시")]
    [Tooltip("물이 올라와도 발판이 물보다 앞에 보이도록 보정할 최소 순서")]
    [SerializeField] private int minimumPlatformSortingOrder = 1;
    [SerializeField] private int playerOrderAbovePlatforms = 1;

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

    public void BeginDrainAndHide()
    {
        riseTween?.Kill();
        riseTween = null;
        HasReachedTarget = false;

        if (waterRoot == null)
        {
            hasStarted = false;
            SetPlayerSwimming(false);
            return;
        }

        if (!waterRoot.gameObject.activeSelf)
        {
            StopAndHide();
            return;
        }

        // Update의 수면 판정을 유지해 물이 플레이어 아래로 빠질 때 수영 상태도 해제한다.
        hasStarted = true;
        float drainEndY = GetRiseStartY();

        if (drainDuration <= 0f || Mathf.Approximately(waterRoot.position.y, drainEndY))
        {
            StopAndHide();
            return;
        }

        riseTween = waterRoot
            .DOMoveY(drainEndY, drainDuration)
            .SetEase(drainEase)
            .OnComplete(CompleteDrain)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
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

        if (player != null)
            player.SetInWater(this, value);
    }

    private void CompleteDrain()
    {
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
        drainDuration = Mathf.Max(0f, drainDuration);
        enterDepth = Mathf.Max(0f, enterDepth);
        exitDepth = Mathf.Max(enterDepth, exitDepth);
        playerOrderAbovePlatforms = Mathf.Max(0, playerOrderAbovePlatforms);
    }
}

using UnityEngine;
using System.Collections;
public class Ice_Bullet : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0.1f, 1, 1f);
    [SerializeField] private float accelDuration = 0.6f;
    [SerializeField] private float startDelay = 0.4f;

    [Header("피해")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private LayerMask hitMask;

    [Header("판정 범위")]
    [SerializeField] private BoxCollider2D playerHitbox;
    [SerializeField] private Vector2 playerHitboxSize = new Vector2(1f, 0.2f);
    [SerializeField] private Vector2 freezeProbeSize = new Vector2(0.15f, 0.12f);
    [SerializeField] private LayerMask platformMask = 1 << 6;

    [Header("경로 예고 (스프라이트 방식)")]
    [SerializeField] private SpriteRenderer pathSprite;
    [SerializeField] private float pathLength = 15f;
    [SerializeField] private float pathWidth = 0.2f;
    [SerializeField] private float pathFadeDuration = 0.4f;
    [SerializeField] private int minimumPathSortingOrder = 6;

    [Header("생성 연출 (고드름 자라나는 효과)")]
    [SerializeField] private Transform iceVisual;
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("사운드")]
    [SerializeField] private float iceBulletVolume = 0.8f;
    [SerializeField] private float iceBulletMinInterval = 0.12f;

    private const string IceBulletSound = "Water_IceBullet";

    private Vector2 direction = Vector2.right;
    private float elapsed;
    private float delayElapsed;
    private bool hasHit;
    private bool hasFrozenPlatform;
    private bool isLaunching;
    private readonly RaycastHit2D[] playerCastResults = new RaycastHit2D[4];
    private readonly RaycastHit2D[] platformCastResults = new RaycastHit2D[8];

    public float TelegraphDuration => startDelay;

    private void Awake()
    {
        if (playerHitbox == null)
            playerHitbox = GetComponent<BoxCollider2D>();

        if (playerHitbox != null)
        {
            playerHitbox.isTrigger = true;
            playerHitbox.size = playerHitboxSize;
        }
    }

    public float GetTotalLifetime(float telegraphDuration)
    {
        return Mathf.Max(lifeTime, telegraphDuration + 0.1f);
    }

    public void Launch(Vector2 dir)
    {
        Launch(dir, startDelay);
    }

    public void Launch(Vector2 dir, float telegraphDuration)
    {
        direction = dir.normalized;
        transform.right = direction;
        startDelay = Mathf.Max(0f, telegraphDuration);
        Destroy(gameObject, GetTotalLifetime(startDelay));

        ShowPathPreview(startDelay);
        hasHit = false;
        hasFrozenPlatform = false;
        isLaunching = false;
        delayElapsed = 0f;
        elapsed = 0f;
    }

    private void Update()
    {
        if (PauseManager.IsPaused) return;
        if (hasHit) return;

        if (!isLaunching)
        {
            delayElapsed += Time.deltaTime;
            
            if (iceVisual != null)
            {
                float growT = startDelay <= 0f
                    ? 1f
                    : Mathf.Clamp01(delayElapsed / startDelay);
                float scaleX = growCurve.Evaluate(growT);
                iceVisual.localScale = new Vector3(scaleX, 1f, 1f);
            }

            if (delayElapsed >= startDelay)
            {
                isLaunching = true; // 정지 끝, 이제부터 가속 시작
                BossSound.PlayThrottled(IceBulletSound, iceBulletVolume, iceBulletMinInterval);
                if (iceVisual != null)
                    iceVisual.localScale = Vector3.one;
            }
            return; // 정지 구간 동안은 이동 안 함
        }

        elapsed += Time.deltaTime;
        float t = accelDuration <= 0f
            ? 1f
            : Mathf.Clamp01(elapsed / accelDuration);
        float speed = speedCurve.Evaluate(t) * maxSpeed;
        Vector2 start = transform.position;
        float moveDistance = speed * Time.deltaTime;
        Vector2 end = start + direction * moveDistance;

        if (TryHitPlayerBetween(start, moveDistance))
            return;

        TryFreezePlatformBetween(start, moveDistance);
        transform.position = end;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private bool TryHitPlayerBetween(Vector2 start, float distance)
    {
        if (hasHit || distance <= 0f)
            return false;

        ContactFilter2D filter = CreateContactFilter(hitMask);
        int hitCount = Physics2D.BoxCast(
            start,
            playerHitboxSize,
            transform.eulerAngles.z,
            direction,
            filter,
            playerCastResults,
            distance
        );

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = playerCastResults[i];
            if (hit.collider != null && TryDamagePlayer(hit.collider))
                return true;
        }

        return false;
    }

    private bool TryDamagePlayer(Collider2D other)
    {
        if (!isLaunching || hasHit || other == null || other.transform.IsChildOf(transform))
            return false;

        bool layerCanBeHit = hitMask.value == 0 ||
                             (hitMask.value & (1 << other.gameObject.layer)) != 0;
        PlayerKnockBack playerKnockback = layerCanBeHit
            ? other.GetComponentInParent<PlayerKnockBack>()
            : null;

        if (playerKnockback != null)
        {
            playerKnockback.TakeHit(transform.position, 5f, Mathf.RoundToInt(damage));
            hasHit = true;
            Destroy(gameObject, 0.05f);
            return true;
        }

        return false;
    }

    private void TryFreezePlatformBetween(Vector2 start, float distance)
    {
        if (hasFrozenPlatform || distance <= 0f)
            return;

        ContactFilter2D filter = CreateContactFilter(platformMask);
        int hitCount = Physics2D.BoxCast(
            start,
            freezeProbeSize,
            transform.eulerAngles.z,
            direction,
            filter,
            platformCastResults,
            distance
        );

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = platformCastResults[i];
            Collider2D platformCollider = hit.collider;
            if (platformCollider == null ||
                (!platformCollider.CompareTag("ChangeablePlatform") &&
                 !platformCollider.CompareTag("Slippery")))
            {
                continue;
            }

            FreezablePlatform platform = platformCollider.GetComponent<FreezablePlatform>();
            if (platform == null)
                platform = platformCollider.gameObject.AddComponent<FreezablePlatform>();

            platform.Freeze();
            hasFrozenPlatform = true;
            return;
        }
    }

    private static ContactFilter2D CreateContactFilter(LayerMask mask)
    {
        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = true
        };
        filter.SetLayerMask(mask.value == 0 ? Physics2D.AllLayers : mask.value);
        return filter;
    }

    private void ShowPathPreview(float previewDuration)
    {
        if (pathSprite == null) return;

        pathSprite.gameObject.SetActive(true);
        pathSprite.sortingOrder = Mathf.Max(pathSprite.sortingOrder, minimumPathSortingOrder);

        // 스프라이트 pivot이 Left(왼쪽)라고 가정, 자신의 위치에서 진행방향으로 길게 늘림
        pathSprite.transform.localPosition = Vector3.zero;
        pathSprite.transform.localRotation = Quaternion.identity;
        pathSprite.transform.localScale = new Vector3(pathLength, pathWidth, 1f);

        Color c = pathSprite.color;
        c.a = 0.6f;
        pathSprite.color = c;

        StartCoroutine(FadePathSprite(previewDuration));
    }
    private IEnumerator FadePathSprite(float previewDuration)
    {
        if (pathSprite == null)
            yield break;

        float fadeDuration = Mathf.Min(
            Mathf.Max(pathFadeDuration, 0.0001f),
            Mathf.Max(previewDuration, 0.0001f)
        );
        float holdDuration = Mathf.Max(0f, previewDuration - fadeDuration);
        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        float t = 0f;
        Color start = pathSprite.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(start.a, 0f, t / fadeDuration);
            Color c = pathSprite.color;
            c.a = alpha;
            pathSprite.color = c;
            yield return null;
        }

        if (pathSprite != null)
            pathSprite.gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        maxSpeed = Mathf.Max(0f, maxSpeed);
        lifeTime = Mathf.Max(0.01f, lifeTime);
        accelDuration = Mathf.Max(0f, accelDuration);
        startDelay = Mathf.Max(0f, startDelay);
        damage = Mathf.Max(0f, damage);
        pathLength = Mathf.Max(0.01f, pathLength);
        pathWidth = Mathf.Max(0.01f, pathWidth);
        pathFadeDuration = Mathf.Max(0f, pathFadeDuration);
        playerHitboxSize.x = Mathf.Max(0.01f, playerHitboxSize.x);
        playerHitboxSize.y = Mathf.Max(0.01f, playerHitboxSize.y);
        freezeProbeSize.x = Mathf.Max(0.01f, freezeProbeSize.x);
        freezeProbeSize.y = Mathf.Max(0.01f, freezeProbeSize.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, playerHitboxSize);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, freezeProbeSize);
    }
}

/* [파일 노트]
 * 사운드 Water_IceBullet : 전조(startDelay, 고드름이 자라는 구간)가 끝나고 isLaunching 이 서는
 * 프레임 — 즉 고드름이 실제로 발사되는 순간 — 에 재생한다. Launch() 시점이 아닌 이유는
 * Launch 가 "예고 시작"이고 그때 소리를 내면 발사 타이밍과 어긋나기 때문이다.
 * iceBulletMinInterval(기본 0.12초) 스로틀 : IceBulletSpawnZone 이 3발을 같은 프레임에 소환하고
 * 전조 시간도 동일해 세 발이 정확히 동시에 발사되므로, 스로틀이 없으면 소리가 3중으로 겹친다.
 */

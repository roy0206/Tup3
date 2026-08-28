using UnityEngine;
using System.Collections;


public class Water_Sprout : MonoBehaviour
{
    private enum SproutState
    {
        Waiting,
        Growing,
        Active,
        Ending
    }

    [Header("딜레이")]
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private float startDelay = 0.4f;

    [Header("Path Collider Growth")]
    [SerializeField] private BoxCollider2D pathCollider;
    [SerializeField] private float growDuration = 0.3f;
    [SerializeField] private Vector2 targetSize; // x = 자라나는 길이(진행방향), y = 가로 폭(진행방향에 수직)
    private float baseOffsetY;

    [Header("피해")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private LayerMask hitMask;

    [Header("경로 예고 (스프라이트 방식)")]
    [SerializeField] private SpriteRenderer pathSprite;
    [SerializeField] private float pathLength = 15f;
    [SerializeField] private float pathWidth = 0.2f; // SetTargetWidth로 덮어씀
    [SerializeField] private float pathFadeDuration = 0.4f;
    [SerializeField] private int minimumSortingOrder = 10;
    [SerializeField] private bool showPathPreview = true;
    [SerializeField] private bool drawHitboxGizmo = true;
    private bool sortingOrderOverridden;
    private int sortingOrderOverride;
    private const float spriteNativeHeight = 5.6f; // waterspout_2 스프라이트 실제 높이(유닛), 피벗은 좌하단


    [Header("생성 연출 (옆에서부터 자라나는 효과)")]
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private float targetScaleY;  // SetTargetLength에서 계산된 최종 scale.y 캐싱
    private float targetPosX;    // SetTargetLength에서 계산된 최종 localPosition.x 캐싱


    [Header("사운드")]
    [SerializeField] private float sproutVolume = 0.8f;
    [SerializeField] private float sproutMinInterval = 0.12f;

    private const string SproutSound = "Water_Sprout";

    private float delayElapsed;
    private float growTimer;
    private Animator watersproutAnimator;
    private SproutState state = SproutState.Ending;
    [SerializeField] private Transform Watersprout_render;


    private void Awake()
    {
        if (pathCollider == null)
            pathCollider = GetComponent<BoxCollider2D>();

        if (pathCollider != null)
            baseOffsetY = pathCollider.offset.y; // 원래 위치(높이) 저장

        watersproutAnimator = GetComponentInChildren<Animator>();
        if (watersproutAnimator != null)
            watersproutAnimator.SetBool("On", false);

        ApplySortingOrder();
    }

    private void Update()
    {
        if (PauseManager.IsPaused) return;

        if (state == SproutState.Waiting)
        {
            delayElapsed += Time.deltaTime;

            if (Watersprout_render != null)
            {
                float growT = startDelay <= 0f
                    ? 1f
                    : Mathf.Clamp01(delayElapsed / startDelay);
                float scaleT = growCurve.Evaluate(growT);
                Watersprout_render.localScale = new Vector3(pathWidth, targetScaleY * scaleT, 1f);
                Watersprout_render.localPosition = new Vector3(targetPosX * scaleT, 0f, 0f);
            }

            if (delayElapsed >= startDelay)
            {
                state = SproutState.Growing;
                growTimer = 0f;
                BossSound.PlayThrottled(SproutSound, sproutVolume, sproutMinInterval);
                if (watersproutAnimator != null)
                    watersproutAnimator.SetBool("On", true);
            }
        }

        if (state == SproutState.Growing)
        {
            growTimer += Time.deltaTime;
            float t = growDuration <= 0f
                ? 1f
                : Mathf.Clamp01(growTimer / growDuration);

            float growingX = Mathf.Lerp(0f, targetSize.x, t);

            if (pathCollider != null)
            {
                pathCollider.size = new Vector2(growingX, targetSize.y);
                pathCollider.offset = new Vector2(growingX * 0.5f, baseOffsetY);
            }

            if (t >= 1f)
                state = SproutState.Active;
        }
    }

    public void Launch(Vector2 dir)
    {
        // 재사용(풀링) 대비: 이전 상태 초기화
        StopAllCoroutines();
        state = SproutState.Waiting;
        delayElapsed = 0f;
        growTimer = 0f;

        Vector2 launchDirection = dir.sqrMagnitude > 0f ? dir.normalized : Vector2.up;
        transform.right = launchDirection;

        if (pathCollider != null)
            pathCollider.enabled = true;
        if (watersproutAnimator != null)
            watersproutAnimator.SetBool("On", false);

        StartCoroutine(LifeTimeRoutine());
        ShowPathPreview();
    }

    private IEnumerator LifeTimeRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, lifeTime));

        state = SproutState.Ending;
        if (watersproutAnimator != null)
            watersproutAnimator.SetBool("On", false);
        
        if (pathCollider != null)
            pathCollider.enabled = false;
        
        yield return null;

        float exitAnimLength = watersproutAnimator != null
            ? watersproutAnimator.GetCurrentAnimatorStateInfo(0).length
            : 0f;

        if (exitAnimLength > 0f)
            yield return new WaitForSeconds(exitAnimLength);
        
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        if (hitMask.value != 0 && (hitMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        if (!other.TryGetComponent(out PlayerKnockBack playerKnockback))
            return;

        playerKnockback.TakeHit(transform.position, 0f, Mathf.RoundToInt(damage));
    }

    private void ShowPathPreview()
    {
        if (pathSprite != null)
        {
            if (showPathPreview)
            {
                pathSprite.gameObject.SetActive(true);

                // x: 진행방향(로컬), y: 가로 폭 -> baseOffsetY로 세로(콜라이더 기준) 위치도 맞춰줌
                pathSprite.transform.localPosition = new Vector3(0f, baseOffsetY, 0f);
                pathSprite.transform.localRotation = Quaternion.identity;
                pathSprite.transform.localScale = new Vector3(pathLength, pathWidth, 1f);

                Color c = pathSprite.color;
                c.a = 0.6f;
                pathSprite.color = c;

                StartCoroutine(FadePathSprite());
            }
            else
            {
                pathSprite.gameObject.SetActive(false);
            }
        }

        if (pathCollider != null)
        {
            pathCollider.size = new Vector2(0f, targetSize.y);
            pathCollider.offset = new Vector2(0f, baseOffsetY); // 시작점부터 자라도록 초기화
            growTimer = 0f;
        }

        if (Watersprout_render != null)
        {
            Watersprout_render.localScale = new Vector3(pathWidth, 0f, 1f);
            Watersprout_render.localPosition = new Vector3(0f, 0f, 0f);
        }
    }

    private IEnumerator FadePathSprite()
    {
        if (pathSprite == null)
            yield break;

        float t = 0f;
        Color start = pathSprite.color;
        float duration = Mathf.Max(pathFadeDuration, 0.0001f);
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(start.a, 0f, t / duration);
            Color c = pathSprite.color;
            c.a = alpha;
            pathSprite.color = c;
            yield return null;
        }

        if (pathSprite != null)
            pathSprite.gameObject.SetActive(false);
    }

    // dir이 Vector2.up일 때 transform.right = direction으로 인해
    // 로컬 X축 = 월드 위쪽(자라나는 방향), 로컬 Y축 = 월드 가로(폭) 이 됩니다.
    // 그래서 "가로로 꽉 채우기"는 targetSize.y / pathWidth 쪽에 반영해야 합니다.
    public void SetTargetWidth(float width)
    {
        pathWidth = Mathf.Max(0.01f, width);
        targetSize.y = pathWidth;
    }

    public void Configure(float warnDelay, float damageOverride)
    {
        startDelay = Mathf.Max(0f, warnDelay);
        damage = Mathf.Max(0f, damageOverride);
    }

    public void SetTargetLength(float length)
    {
        pathLength = Mathf.Max(0.01f, length);
        targetSize.x = pathLength;
        targetScaleY = pathLength / spriteNativeHeight * 0.7f;
        targetPosX = pathLength / spriteNativeHeight * 3.5f * 0.7f;

        if (Watersprout_render != null)
        {
            Watersprout_render.localScale = new Vector3(pathWidth, targetScaleY, 1f);
            Watersprout_render.localPosition = new Vector3(targetPosX, 0f, 0f);
        }
    }

    public void SetSortingOrder(int order)
    {
        sortingOrderOverridden = true;
        sortingOrderOverride = order;
        ApplySortingOrder();
    }

    public void SetPathPreviewVisible(bool visible)
    {
        showPathPreview = visible;
        if (!visible && pathSprite != null) pathSprite.gameObject.SetActive(false);
    }

    public void SetHitboxGizmoVisible(bool visible)
    {
        drawHitboxGizmo = visible;
    }

    private void ApplySortingOrder()
    {
        if (Watersprout_render != null)
        {
            foreach (SpriteRenderer renderer in Watersprout_render.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.sortingOrder = sortingOrderOverridden
                    ? sortingOrderOverride
                    : Mathf.Max(renderer.sortingOrder, minimumSortingOrder);
        }

        if (pathSprite != null)
            pathSprite.sortingOrder = sortingOrderOverridden
                ? sortingOrderOverride
                : Mathf.Max(pathSprite.sortingOrder, minimumSortingOrder + 1);
    }

    private void OnValidate()
    {
        lifeTime = Mathf.Max(0f, lifeTime);
        startDelay = Mathf.Max(0f, startDelay);
        growDuration = Mathf.Max(0f, growDuration);
        pathFadeDuration = Mathf.Max(0f, pathFadeDuration);
        pathWidth = Mathf.Max(0.01f, pathWidth);
        pathLength = Mathf.Max(0.01f, pathLength);
    }

    private void OnDrawGizmos()
    {
        if (!drawHitboxGizmo) return;
        if (pathCollider == null) return;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        float offsetY = Application.isPlaying ? baseOffsetY : pathCollider.offset.y;

        Vector3 currentCenter = new Vector3(pathCollider.offset.x, pathCollider.offset.y, 0f);
        Vector3 currentSize = new Vector3(pathCollider.size.x, pathCollider.size.y, 0.1f);
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawCube(currentCenter, currentSize);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(currentCenter, currentSize);

        Vector3 targetCenter = new Vector3(targetSize.x * 0.5f, offsetY, 0f);
        Vector3 targetSizeVec = new Vector3(targetSize.x, targetSize.y, 0.1f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(targetCenter, targetSizeVec);

        Gizmos.matrix = oldMatrix;
    }
}

/* [파일 노트]
 * Configure(warnDelay, damage) : 외부 소환자(최종보스 물기둥 패턴)가 전조 시간(startDelay)과
 * 데미지를 프리팹 값 대신 지정할 때 쓴다. 수보스 경로(Water_Sprout_Zone)는 호출하지 않으므로
 * 기존 동작은 프리팹 직렬화 값 그대로다. 수명이 끝나면 Destroy(gameObject) 로 스스로 사라지므로
 * PoolManager 로 꺼냈더라도 반납 없이 파괴된다(풀은 매번 새로 Instantiate).
 *
 * 호출자 주입 3종 (전부 최종보스 전용, 수보스 경로는 호출하지 않아 프리팹 직렬화 값 그대로 동작)
 *   - SetSortingOrder(order) : sortingOrderOverridden 이 서면 ApplySortingOrder 가
 *     "Max(기존, minimumSortingOrder)" 대신 지정값을 그대로 쓴다. Awake 의 ApplySortingOrder 가
 *     이미 돈 뒤에 호출되므로 세터가 즉시 재적용한다.
 *   - SetPathPreviewVisible(false) : "하늘색 사각형" = pathSprite(Water_Pump 프리팹의 Warning_sprite,
 *     색 0.25/0.92/0.89 하늘색)를 끈다. ShowPathPreview 는 이 플래그와 무관하게 콜라이더 초기화와
 *     Watersprout_render 스케일 리셋을 계속 수행하므로 성장 로직·데미지 판정은 그대로다.
 *   - SetHitboxGizmoVisible(false) : OnDrawGizmos 의 시안색 DrawCube(=pathCollider 시각화)를 끈다.
 *     에디터 전용 그림이라 게임 로직에는 영향이 없다.
 * 세 값 모두 인스턴스 필드이고 Water_Sprout 는 수명이 끝나면 Destroy 되어 매번 새로 Instantiate
 * 되므로, 재사용으로 값이 새는 경로가 없다(별도 OnEnable 리셋이 불필요한 이유).
 *
 * 사운드 Water_Sprout : 전조(startDelay)가 끝나고 Waiting → Growing 으로 넘어가는 프레임,
 * 즉 애니메이터 "On" 을 켜는 실제 분출 순간에 재생한다(Launch 시점이 아니다 — 그러면 전조 중에
 * 소리가 먼저 나 버린다).
 * 이 파일은 수보스(Water_Sprout_Zone)와 최종보스(수 물기둥 패턴)가 공유하는데, 두 경로 모두
 * 같은 "물기둥 분출" 사건이고 배정된 소리도 Water_Sprout 하나뿐이라 호출자 주입(Configure/
 * SetTargetWidth 같은 세터) 없이 공용으로 두었다.
 * sproutMinInterval(기본 0.12초) 스로틀이 필요한 이유 : 수보스는 3개를 같은 프레임에 한꺼번에
 * 소환하므로 전조 시간도 같아 세 기둥이 정확히 동시에 분출한다(스로틀이 없으면 소리가 3중으로 겹친다).
 * 최종보스는 0.25초 간격으로 5개를 차례로 뿌리므로 스로틀 간격보다 넓어 기둥마다 따로 울린다.
 */

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
    private const float spriteNativeHeight = 5.6f; // waterspout_2 스프라이트 실제 높이(유닛), 피벗은 좌하단


    [Header("생성 연출 (옆에서부터 자라나는 효과)")]
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private float targetScaleY;  // SetTargetLength에서 계산된 최종 scale.y 캐싱
    private float targetPosX;    // SetTargetLength에서 계산된 최종 localPosition.x 캐싱


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

        PlayerKnockBack playerKnockback = other.GetComponentInParent<PlayerKnockBack>();
        if (playerKnockback != null)
        {
            playerKnockback.TakeHit(transform.position, 0f, Mathf.RoundToInt(damage));
        }
    }

    private void ShowPathPreview()
    {
        if (pathSprite == null) return;
        pathSprite.gameObject.SetActive(true);

        // x: 진행방향(로컬), y: 가로 폭 -> baseOffsetY로 세로(콜라이더 기준) 위치도 맞춰줌
        pathSprite.transform.localPosition = new Vector3(0f, baseOffsetY, 0f);
        pathSprite.transform.localRotation = Quaternion.identity;
        pathSprite.transform.localScale = new Vector3(pathLength, pathWidth, 1f);

        Color c = pathSprite.color;
        c.a = 0.6f;
        pathSprite.color = c;

        StartCoroutine(FadePathSprite());

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

    private void ApplySortingOrder()
    {
        if (Watersprout_render != null)
        {
            foreach (SpriteRenderer renderer in Watersprout_render.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, minimumSortingOrder);
        }

        if (pathSprite != null)
            pathSprite.sortingOrder = Mathf.Max(pathSprite.sortingOrder, minimumSortingOrder + 1);
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

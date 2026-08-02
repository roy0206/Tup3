using UnityEngine;
using System.Collections;

public class WaterPump : MonoBehaviour
{
    [Header("딜레이")]
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float startDelay = 0.4f;


    [Header("Path Collider Growth")]
    [SerializeField] private BoxCollider2D pathCollider;
    [SerializeField] private float growDuration = 0.3f;
    [SerializeField] private Vector2 targetSize;
    private float baseOffsetY; // Inspector에 세팅된 원래 offset.y를 기억

    [Header("피해")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private LayerMask hitMask;

    [Header("경로 예고 (스프라이트 방식)")]
    [SerializeField] private SpriteRenderer pathSprite;
    [SerializeField] private float pathLength = 15f;
    [SerializeField] private float pathWidth = 0.2f;
    [SerializeField] private float pathFadeDuration = 0.4f;

    private Vector2 direction;
    private float delayElapsed = 0f;
    private float elapsed = 0f;
    private bool isGrowing = false;
    private float growTimer = 0f;

    private void Awake()
    {
        if (pathCollider != null)
            baseOffsetY = pathCollider.offset.y; // 원래 위치(높이) 저장
    }

    private void Update()
    {
        if (!isGrowing)
        {
            delayElapsed += Time.deltaTime;
            if (delayElapsed >= startDelay)
            {
                isGrowing = true; // 정지 끝, 이제부터 가속 시작
            }
            return;
        }

        if (isGrowing)
        {
            growTimer += Time.deltaTime;
            float t = Mathf.Clamp01(growTimer / growDuration);

            float growingX = Mathf.Lerp(0f, targetSize.x, t); // 발사 방향(X)만 점점 커짐

            pathCollider.size = new Vector2(growingX, targetSize.y);
            pathCollider.offset = new Vector2(growingX * 0.5f, baseOffsetY); // Y는 원래 위치 유지

            if (t >= 1f)
            {
                isGrowing = false;
            }
        }
    }

    public void Launch(Vector2 dir)
    {
        isGrowing = false;
        direction = dir.normalized;
        transform.right = direction;
        Destroy(gameObject, lifeTime);
        ShowPathPreview();
        delayElapsed = 0f;
        elapsed = 0f;
    }

    private void ShowPathPreview()
    {
        if (pathSprite == null) return;
        pathSprite.gameObject.SetActive(true);

        pathSprite.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
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
    }

    private IEnumerator FadePathSprite()
    {
        float t = 0f;
        Color start = pathSprite.color;
        while (t < pathFadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(start.a, 0f, t / pathFadeDuration);
            Color c = pathSprite.color;
            c.a = alpha;
            pathSprite.color = c;
            yield return null;
        }
        pathSprite.gameObject.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        if (pathCollider == null) return;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        // Edit 모드에서는 Awake가 안 돌기 때문에 pathCollider.offset.y를 그대로 사용
        float offsetY = Application.isPlaying ? baseOffsetY : pathCollider.offset.y;

        // 현재 콜라이더 크기 (실시간)
        Vector3 currentCenter = new Vector3(pathCollider.offset.x, pathCollider.offset.y, 0f);
        Vector3 currentSize = new Vector3(pathCollider.size.x, pathCollider.size.y, 0.1f);
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawCube(currentCenter, currentSize);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(currentCenter, currentSize);

        // 최종 목표 크기 - 항상 고정된 위치로 표시
        Vector3 targetCenter = new Vector3(targetSize.x * 0.5f, offsetY, 0f);
        Vector3 targetSizeVec = new Vector3(targetSize.x, targetSize.y, 0.1f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(targetCenter, targetSizeVec);

        Gizmos.matrix = oldMatrix;
    }
}
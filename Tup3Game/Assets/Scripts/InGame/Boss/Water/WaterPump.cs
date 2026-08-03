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

    private Vector2 direction;
    private float delayElapsed = 0f;
    private bool hasHit;
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

            float growingX = Mathf.Lerp(0f, targetSize.x, t);

            pathCollider.size = new Vector2(growingX, targetSize.y);
            pathCollider.offset = new Vector2(growingX * 0.5f, baseOffsetY);

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
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (((1 << other.gameObject.layer) & hitMask) == 0) return;

        if (other.TryGetComponent(out PlayerKnockBack playerKnockback))
        {
            playerKnockback.TakeHit(transform.position, 5f, (int)damage);
        }

        hasHit = true;
    }

    private void ShowPathPreview()
    {
        if (pathSprite == null) return;
        pathSprite.gameObject.SetActive(true);

        // x: 진행방향(로컬), y: 가로 폭 -> baseOffsetY로 세로(콜라이더 기준) 위치도 맞춰줌
        pathSprite.transform.localPosition = new Vector3(-0.5f, baseOffsetY, 0f);
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
    public void SetTargetWidth(float width)
    {
        targetSize.y = width;
        pathWidth = width;
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
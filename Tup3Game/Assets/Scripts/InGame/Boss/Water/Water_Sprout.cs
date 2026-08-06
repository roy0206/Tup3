using UnityEngine;
using System.Collections;


public class Water_Sprout: MonoBehaviour
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

    private const float spriteNativeHeight = 5.6f; // waterspout_2 스프라이트 실제 높이(유닛), 피벗은 좌하단

    private Vector2 direction;
    private float delayElapsed = 0f;
    private bool hasHit;
    private bool isGrowing = false;
    private float growTimer = 0f;
    private Animator Watersprout_Anmimation;
    [SerializeField] private Transform Watersprout_render;


    private void Awake()
    {
        if (pathCollider != null)
            baseOffsetY = pathCollider.offset.y; // 원래 위치(높이) 저장
        Watersprout_Anmimation = GetComponentInChildren<Animator>();
        Watersprout_Anmimation.SetBool("On", false);
    }

    private void Update()
    {
        if (!isGrowing)
        {
            delayElapsed += Time.deltaTime;
            if (delayElapsed >= startDelay)
            {
                isGrowing = true;
                Watersprout_Anmimation.SetBool("On", true);
            }
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
        // 재사용(풀링) 대비: 이전 상태 초기화
        StopAllCoroutines();  
        hasHit = false;
        isGrowing = false;
        delayElapsed = 0f;
        growTimer = 0f;

        direction = dir.normalized;
        transform.right = direction;

        StartCoroutine(LifeTimeRoutine());
        ShowPathPreview();
    }

    private IEnumerator LifeTimeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        Watersprout_Anmimation.SetBool("On", false);
        if (pathCollider != null)
            pathCollider.enabled = false;
        yield return null;
        float exitAnimLength = Watersprout_Anmimation.GetCurrentAnimatorStateInfo(0).length;
        
        yield return new WaitForSeconds(exitAnimLength);
        
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerKnockBack playerKnockback))
        {
            playerKnockback.TakeHit(this.transform.position, 0f, (int)damage);
        }
    }

    private void ShowPathPreview()
    {
        Debug.Log("ShowPathPreview 호출됨. pathSprite null? " + (pathSprite == null));
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

    // dir이 Vector2.up일 때 transform.right = direction으로 인해
    // 로컬 X축 = 월드 위쪽(자라나는 방향), 로컬 Y축 = 월드 가로(폭) 이 됩니다.
    // 그래서 "가로로 꽉 채우기"는 targetSize.y / pathWidth 쪽에 반영해야 합니다.
    public void SetTargetWidth(float width)
    {
        Debug.Log($"SetTargetWidth 호출됨. 전달받은 width = {width}");
        targetSize.y = width;
        pathWidth = width;
    }

    public void SetTargetLength(float length)
    {
        Debug.Log($"SetTargetLength 호출됨. 전달받은 width = {length}");
        targetSize.x = length;
        pathLength = length;
        Watersprout_render.localScale = new Vector3(pathWidth, length / spriteNativeHeight * 0.7f, 1f);
        Watersprout_render.localPosition = new Vector3((length / spriteNativeHeight * 3.5f * 0.7f), 0, 0);
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
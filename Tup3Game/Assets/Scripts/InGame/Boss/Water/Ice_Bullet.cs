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

    [Header("경로 예고 (스프라이트 방식)")]
    [SerializeField] private SpriteRenderer pathSprite;
    [SerializeField] private float pathLength = 15f;
    [SerializeField] private float pathWidth = 0.2f;
    [SerializeField] private float pathFadeDuration = 0.4f;

    [Header("생성 연출 (고드름 자라나는 효과)")]
    [SerializeField] private Transform iceVisual; 
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector2 direction = Vector2.right;
    private float elapsed;
    private float delayElapsed;
    private bool hasHit;
    private bool isLaunching;

    public void Launch(Vector2 dir)
    {
        direction = dir.normalized;
        transform.right = direction;
        Destroy(gameObject, lifeTime);

        ShowPathPreview();
        hasHit = false;
        isLaunching = false;
        delayElapsed = 0f;
        elapsed = 0f;
    }

    private void Update()
    {
        if (hasHit) return;

        if (!isLaunching)
        {
            delayElapsed += Time.deltaTime;
            
            if (iceVisual != null)
            {
                float growT = Mathf.Clamp01(delayElapsed / startDelay);
                float scaleX = growCurve.Evaluate(growT);
                iceVisual.localScale = new Vector3(scaleX, 1f, 1f);
            }

            if (delayElapsed >= startDelay)
            {
                isLaunching = true; // 정지 끝, 이제부터 가속 시작
                if (iceVisual != null)
                    iceVisual.localScale = Vector3.one;
            }
            return; // 정지 구간 동안은 이동 안 함
        }

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / accelDuration);
        float speed = speedCurve.Evaluate(t) * maxSpeed;
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (other.TryGetComponent(out PlayerKnockBack playerKnockback))
        {
            playerKnockback.TakeHit(transform.position, 5f, (int)damage);
            hasHit = true;
            Destroy(gameObject, 0.05f);
        }

        if (other.CompareTag("ChangeablePlatform"))
        {
            other.gameObject.tag = "Slippery"; 
            
            if (other.TryGetComponent(out SpriteRenderer sprite))
            {
                sprite.color = Color.skyBlue;
            }
            
        }
    }

    private void ShowPathPreview()
    {
        if (pathSprite == null) return;

        pathSprite.gameObject.SetActive(true);

        // 스프라이트 pivot이 Left(왼쪽)라고 가정, 자신의 위치에서 진행방향으로 길게 늘림
        pathSprite.transform.localPosition = Vector3.zero;
        pathSprite.transform.localRotation = Quaternion.identity;
        pathSprite.transform.localScale = new Vector3(pathLength, pathWidth, 1f);

        Color c = pathSprite.color;
        c.a = 0.6f;
        pathSprite.color = c;

        StartCoroutine(FadePathSprite());
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
}

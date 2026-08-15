using System.Collections;
using UnityEngine;

public class Storm : MonoBehaviour
{
    [Header("소용돌이 설정")]
    [SerializeField] private float pullPower = 3f;
    [SerializeField] private float lifeTime = 10f;
    [SerializeField] private float delay;
    [SerializeField] private float targetScale;

    [Header("피해 설정")]
    [SerializeField] private float damage;
    [SerializeField] private float damageInterval = 1f;
    
    [Header("참조")]
    [SerializeField] private Playermovement player;
    [SerializeField] private CircleCollider2D hitCollider;


    private bool isAlive;
    private float nextDamageTime;
    private Vector3 baseScale;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.GetComponent<Playermovement>();
        }

        if (hitCollider == null)
            hitCollider = GetComponent<CircleCollider2D>();

        baseScale = transform.localScale;
    }

    private void Update()
    {
        if (!isAlive || player == null)
            return;
        player.ApplyGravityPull(
                transform.position,
                pullPower
            );
    }


    void Start()
    {
        StartCoroutine(SpawnStorm());
    }

    private IEnumerator SpawnStorm()
    {
        isAlive = false;

        if (hitCollider != null)
            hitCollider.enabled = false;

        Vector3 smallScale = baseScale * 0.05f;
        Vector3 finalScale = baseScale * targetScale;

        transform.localScale = smallScale;

        float timer = 0f;

        // 소용돌이가 점점 커지는 과정
        while (timer < delay)
        {
            timer += Time.deltaTime;

            float ratio = Mathf.Clamp01(timer / delay);
            ratio = Mathf.SmoothStep(0f, 1f, ratio);

            transform.localScale =
                Vector3.Lerp(smallScale, finalScale, ratio);

            yield return null;
        }

        transform.localScale = finalScale;

        isAlive = true;

        if (hitCollider != null)
            hitCollider.enabled = true;

        // 완성된 상태로 유지
        yield return new WaitForSeconds(lifeTime);

        isAlive = false;

        if (hitCollider != null)
            hitCollider.enabled = false;

        // 사라질 때 작아지는 효과
        timer = 0f;
        float disappearDuration = 0.3f;

        while (timer < disappearDuration)
        {
            timer += Time.deltaTime;

            float ratio =
                Mathf.Clamp01(timer / disappearDuration);

            transform.localScale =
                Vector3.Lerp(finalScale, Vector3.zero, ratio);

            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isAlive)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (Time.time < nextDamageTime)
            return;

        nextDamageTime = Time.time + damageInterval;

        // 플레이어 체력 스크립트에 맞게 변경
        PlayerHealth playerHealth =
            other.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }
}

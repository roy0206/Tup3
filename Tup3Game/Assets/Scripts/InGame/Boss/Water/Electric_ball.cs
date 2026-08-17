using System.Collections;
using UnityEngine;

public class Electric_ball : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float lifeTime = 8f;

    [Header("충전 설정")]
    [SerializeField] private float chargeDuration = 0.8f;
    [SerializeField] private float startScale = 0.1f;
    [SerializeField] private float finalScale = 1f;
    [SerializeField] private float damage = 10f;

    private Transform player;
    private Vector3 moveDirection;
    private bool isLaunched;
    private float nextDamageTime;

    public float ChargeDuration => chargeDuration;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
            player = playerObject.transform;
    }
    public void Init(float Movespeed, float lifetime, float charge, float start, float final, float Damage)
    {
        moveSpeed = Movespeed;
        lifeTime = lifetime;
        chargeDuration = charge;
        startScale = start;
        finalScale = final;
        damage = Damage;
    }

    private void Start()
    {
        StartCoroutine(ChargeAndLaunch());
    }
    // Update is called once per frame
    private void Update()
    {
        if (!isLaunched)
            return;

        transform.position +=
            moveDirection * moveSpeed * Time.deltaTime;
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
        if (!isLaunched || Time.time < nextDamageTime)
            return;

        if (other.TryGetComponent(out PlayerKnockBack playerKnockback))
        {
            nextDamageTime = Time.time + 1f;
            playerKnockback.TakeHit(transform.position, 5f, (int)damage);
        }
    }

    private IEnumerator ChargeAndLaunch()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 smallScale = originalScale * startScale;
        Vector3 targetScale = originalScale * finalScale;

        transform.localScale = smallScale;

        float timer = 0f;

        while (timer < chargeDuration)
        {
            timer += Time.deltaTime;

            float ratio = Mathf.Clamp01(
                timer / chargeDuration
            );

            ratio = Mathf.SmoothStep(0f, 1f, ratio);

            transform.localScale =
                Vector3.Lerp(smallScale, targetScale, ratio);

            yield return null;
        }

        transform.localScale = targetScale;

        if (player != null)
        {
            // 충전 완료 시점의 플레이어 위치를 조준
            moveDirection =
                (player.position - transform.position).normalized;

            isLaunched = true;
            Destroy(gameObject, lifeTime);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

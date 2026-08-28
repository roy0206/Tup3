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

    [Header("사운드")]
    [SerializeField] private float skillVolume = 0.8f;
    [SerializeField] private float skillMinInterval = 0.12f;

    private const string SkillSound = "Water_Skill";

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
        if (PauseManager.IsPaused) return;

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

        PlayerKnockBack playerKnockback = other.GetComponentInParent<PlayerKnockBack>();
        if (playerKnockback != null)
        {
            nextDamageTime = Time.time + 1f;
            playerKnockback.TakeHit(transform.position, 5f, Mathf.RoundToInt(damage));
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
            if (PauseManager.IsPaused)
            {
                yield return null;
                continue;
            }

            timer += Time.deltaTime;

            float ratio = chargeDuration <= 0f
                ? 1f
                : Mathf.Clamp01(timer / chargeDuration);

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
            BossSound.PlayThrottled(SkillSound, skillVolume, skillMinInterval);
            Destroy(gameObject, lifeTime);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        lifeTime = Mathf.Max(0.01f, lifeTime);
        chargeDuration = Mathf.Max(0f, chargeDuration);
        startScale = Mathf.Max(0f, startScale);
        finalScale = Mathf.Max(startScale, finalScale);
        damage = Mathf.Max(0f, damage);
    }
}

/* [파일 노트]
 * 사운드 Water_Skill : 충전(ChargeAndLaunch)이 끝나고 플레이어를 조준해 발사되는 순간에 재생한다.
 * "Water_Skill = 그 외 수보스 스킬" 을 전기 구체에 배정한 근거 : 수보스의 남은 패턴 중
 * 분출(Water_Sprout)·고드름(Water_IceBullet)·토네이도(Water_Tornado)·수위 상승(Water_Rising)은
 * 전용 이름이 이미 있고, 이름이 없는 패턴은 전기 구체(패턴4) 하나뿐이다.
 * skillMinInterval(기본 0.12초) 스로틀 : 구체는 ChargeDuration 간격으로 3개가 순차 발사되므로
 * 보통은 전부 울리지만, 충전 시간을 0 에 가깝게 줄여도 소리가 뭉치지 않게 한다.
 */

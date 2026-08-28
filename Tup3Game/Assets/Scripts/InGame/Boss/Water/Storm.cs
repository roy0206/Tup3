using System.Collections;
using UnityEngine;

public class Storm : MonoBehaviour
{
    [Header("소용돌이 설정")]
    [SerializeField] private float pullPower = 3f;
    [SerializeField] private float lifeTime = 10f;
    [SerializeField] private float delay;
    [SerializeField] private float targetScale;
    [SerializeField] private float growDuration = 0.3f;

    [Tooltip("바닥에 표시할 균열 프리팹")]
    [SerializeField] private GameObject warningPrefab;
    [SerializeField] private AnimationClip warningClip;
    [Header("전조 위치 설정")]
    [SerializeField]
    private Vector3 warningOffset =
    new Vector3(0f, -0.5f, 0f);


    [Tooltip("스톰 본체의 스프라이트 또는 이펙트")]
    [SerializeField] private GameObject stormVisual;

    [Header("피해 설정")]
    [SerializeField] private int damage;
    [SerializeField] private float damageInterval = 1f;
    
    [Header("참조")]
    [SerializeField] private Playermovement player;
    [SerializeField] private PolygonCollider2D hitCollider;


    [Header("사운드")]
    [SerializeField] private bool loopTornadoSound = true;
    [SerializeField] private float tornadoVolume = 0.7f;

    private const string TornadoSound = "Water_Tornado";

    private int tornadoSoundId = -1;

    private bool isAlive;
    private bool isWarning;
    private bool isCancelled;

    private float nextDamageTime;
    private Vector3 baseScale;

    private GameObject warningInstance;

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
            hitCollider = GetComponent<PolygonCollider2D>();

        baseScale = transform.localScale;
    }


    void Start()
    {
        StartCoroutine(SpawnStorm());
    }
    private void Update()
    {
        if (PauseManager.IsPaused) return;

        if (!isAlive)
            return;

        if (player == null)
            FindPlayer();

        if (player == null)
            return;

        player.ApplyGravityPull(
                transform.position,
                pullPower
            );
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.GetComponent<Playermovement>();
    }

    private IEnumerator SpawnStorm()
    {
        isAlive = false;
        isWarning = true;
        isCancelled = false;

        if (hitCollider != null)
        {
            hitCollider.enabled = false;
            hitCollider.isTrigger = true;
        }

        if (stormVisual != null)
        {
            stormVisual.SetActive(false);
        }

        if (warningPrefab != null)
        {
            Vector3 warningPosition =
    transform.position + warningOffset;

            warningInstance = Instantiate(
                warningPrefab,
                warningPosition,
                Quaternion.identity
            );

            Animator warningAnimator =
        warningInstance.GetComponentInChildren<Animator>();

            if (warningAnimator != null &&
                warningClip != null &&
                delay > 0f)
            {
                warningAnimator.speed =
                    warningClip.length / delay;
            }
        }
        float timer = 0f;

        while (timer < delay)
        {
            if (isCancelled)
            {
                CancelStorm();
                yield break;
            }

            if (!PauseManager.IsPaused) timer += Time.deltaTime;
            yield return null;
        }

        // 대기 종료 순간에 취소됐는지 다시 검사
        if (isCancelled)
        {
            CancelStorm();
            yield break;
        }

        isWarning = false;

        // 균열 전조 이펙트 제거
        if (warningInstance != null)
        {
            Destroy(warningInstance);
            warningInstance = null;
        }

        // 스톰 본체 표시
        if (stormVisual != null)
        {
            stormVisual.SetActive(true);
        }

        StartTornadoSound();

        Vector3 smallScale = baseScale * 0.05f;
        Vector3 finalScale = baseScale * Mathf.Max(targetScale, 0.01f);

        transform.localScale = smallScale;

        timer = 0f;

        // 소용돌이가 점점 커지는 과정
        while (timer < growDuration)
        {
            if (PauseManager.IsPaused)
            {
                yield return null;
                continue;
            }

            timer += Time.deltaTime;

            float ratio = Mathf.Clamp01(timer / growDuration);
            ratio = Mathf.SmoothStep(0f, 1f, ratio);

            transform.localScale =
                Vector3.Lerp(smallScale, finalScale, ratio);

            yield return null;
        }

        transform.localScale = finalScale;
        isAlive = true;
        nextDamageTime = Time.time;

        if (hitCollider != null)
        {
            hitCollider.enabled = true;
        }

        //완성된 상태로 유지
        yield return new WaitForSeconds(lifeTime);

        isAlive = false;
        StopTornadoSound();

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 전조 상태에서만 STOP 감지
        if (!isWarning)
            return;
/*
        if (other.CompareTag("STOP"))
        {
            isCancelled = true;
        }*/
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isAlive)
            return;

        if (Time.time < nextDamageTime)
            return;

        PlayerKnockBack damageReceiver = other.GetComponentInParent<PlayerKnockBack>();

        if (damageReceiver != null)
        {
            nextDamageTime = Time.time + damageInterval;
            damageReceiver.TakeHit(transform.position, 0f, damage);
        }
    }

    private void CancelStorm()
    {
        isAlive = false;
        isWarning = false;

        if (warningInstance != null)
        {
            Destroy(warningInstance);
        }

        if (hitCollider != null)
        {
            hitCollider.enabled = false;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // 스톰이 외부 원인으로 제거되더라도 균열이 남지 않게 처리
        if (warningInstance != null)
        {
            Destroy(warningInstance);
        }

        StopTornadoSound();
    }

    private void StartTornadoSound()
    {
        if (tornadoSoundId >= 0) return;

        tornadoSoundId = loopTornadoSound
            ? BossSound.PlayLoop(TornadoSound, tornadoVolume)
            : BossSound.Play(TornadoSound, tornadoVolume);
    }

    private void StopTornadoSound()
    {
        if (!loopTornadoSound) return;

        BossSound.Stop(tornadoSoundId);
        tornadoSoundId = -1;
    }

    private void OnValidate()
    {
        pullPower = Mathf.Max(0f, pullPower);
        lifeTime = Mathf.Max(0f, lifeTime);
        delay = Mathf.Max(0f, delay);
        targetScale = Mathf.Max(0.01f, targetScale);
        growDuration = Mathf.Max(0f, growDuration);
        damage = Mathf.Max(0, damage);
        damageInterval = Mathf.Max(0.05f, damageInterval);
    }
}

/* [파일 노트]
 * 사운드 Water_Tornado : 전조(균열)가 끝나고 소용돌이 본체가 나타나는 순간에 시작해서,
 * lifeTime 이 끝나 사라지기 시작할 때 멈춘다. 소용돌이는 기본 10초를 버티는 지속형 장애물이라
 * 단발 재생보다 루프가 맞다 — loopTornadoSound(기본 켬)면 AudioManager.PlayLoopingSound 로 틀고
 * 반환된 채널 id 를 들고 있다가 StopSound 로 반납한다.
 * 정지 지점이 두 곳인 이유 : 정상 종료(수명 만료)와 비정상 종료(전조 중 취소, 보스 사망 시
 * Water.CleanupActiveHazards 의 Destroy, 씬 전환) 모두를 덮어야 루프가 영원히 남지 않는다.
 * 후자는 전부 OnDestroy 를 거치므로 거기에 같은 StopTornadoSound 를 둔다(중복 호출은 무해).
 * loopTornadoSound 를 끄면 등장 순간 1회만 재생하고 id 를 잡지 않는다(길이가 긴 클립을 넣었을 때용).
 */

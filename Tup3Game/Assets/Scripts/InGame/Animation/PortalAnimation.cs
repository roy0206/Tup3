using UnityEngine;

public class PortalAnimation : MonoBehaviour
{
    [Header("z축 회전")]
    [SerializeField] private bool rotate = true;
    [SerializeField] private Vector2 rotationSpeedRange = new Vector2(25f, 45f);
    [SerializeField] private bool randomizeDirection = true;

    [Header("스케일 펄스")]
    [SerializeField] private bool pulse = true;
    [SerializeField] private Vector2 pulseScaleRange = new Vector2(0.94f, 1.06f);
    [SerializeField] private Vector2 pulsePeriodRange = new Vector2(1.2f, 2.4f);

    [Header("시작값 랜덤")]
    [SerializeField] private bool randomizeStartAngle = true;
    [SerializeField] private bool randomizeStartPhase = true;

    private Vector3 baseScale;
    private float rotationSpeed;
    private float pulseFrequency;
    private float angle;
    private float phase;

    private void Awake()
    {
        baseScale = transform.localScale;

        rotationSpeed = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y);
        if (randomizeDirection && Random.value < 0.5f) rotationSpeed = -rotationSpeed;

        float period = Mathf.Max(0.01f, Random.Range(pulsePeriodRange.x, pulsePeriodRange.y));
        pulseFrequency = 1f / period;

        angle = randomizeStartAngle ? Random.Range(0f, 360f) : transform.localEulerAngles.z;
        phase = randomizeStartPhase ? Random.Range(0f, 1f) : 0f;

        Apply();
    }

    private void Update()
    {
        if (PauseManager.IsPaused) return;

        angle = Mathf.Repeat(angle + rotationSpeed * Time.deltaTime, 360f);
        phase = Mathf.Repeat(phase + pulseFrequency * Time.deltaTime, 1f);

        Apply();
    }

    private void Apply()
    {
        if (rotate)
        {
            Vector3 euler = transform.localEulerAngles;
            euler.z = angle;
            transform.localEulerAngles = euler;
        }

        if (!pulse) return;

        float t = (Mathf.Sin(phase * Mathf.PI * 2f) + 1f) * 0.5f;
        transform.localScale = baseScale * Mathf.Lerp(pulseScaleRange.x, pulseScaleRange.y, t);
    }
}

/* [파일 노트]
 *
 * 포탈 장식용 무한 애니메이션. z축 회전 + 스케일 펄스를 Update 에서 직접 돌린다.
 *
 * ── DOTween 을 쓰지 않는 이유 (유저 지시) ────────────────────────────────────
 * 끝나지 않는 배경 연출이라 트윈을 하나 물고 계속 살려 두는 것보다 각도/위상 두 float 를
 * 굴리는 편이 가볍고, 씬에 포탈이 여러 개 깔려도 트윈 인스턴스가 늘지 않는다.
 * 대신 DOTween.PauseAll 의 혜택을 못 받으므로 Update 첫 줄에서 PauseManager.IsPaused 를
 * 직접 검사한다 — 이 프로젝트에서 Update 기반 연출이 지켜야 하는 관례다
 * (Time.timeScale 은 건드리지 않는 방침이라 게이트가 없으면 일시정지 중에도 계속 돈다).
 *
 * ── 시작값 랜덤 ──────────────────────────────────────────────────────────────
 * Awake 에서 회전 속도·회전 방향·펄스 주기·시작 각도·펄스 위상을 전부 무작위로 뽑는다.
 * 로비에 포탈이 5개(토/수/화/금/삼도천) 깔리는데 이게 없으면 전부 같은 각도에서 같은 박자로
 * 돌아 눈에 띄게 어색하다. 속도와 주기까지 흔들어야 시간이 지나도 다시 겹치지 않는다.
 * 고정값으로 쓰고 싶으면 각 Range 의 x 와 y 를 같게 두면 된다.
 *
 * ── 스케일 기준 ──────────────────────────────────────────────────────────────
 * baseScale 은 Awake 시점의 localScale 이다. 펄스는 이 값에 배율을 곱하므로 오브젝트마다
 * 다른 크기로 배치해 두어도 그 비율이 유지된다. 비활성화 후 다시 켜도 다시 뽑지 않는다
 * (켤 때마다 각도가 튀는 것보다 이어지는 편이 자연스럽다).
 */

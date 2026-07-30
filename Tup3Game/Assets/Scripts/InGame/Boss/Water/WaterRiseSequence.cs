using System.Collections;
using UnityEngine;

[RequireComponent(typeof(WaterSurface))]
public class WaterRiseSequence : MonoBehaviour
{
    [Header("타임라인")]
    [SerializeField] private float riseDuration = 1.2f;
    [SerializeField] private float holdDuration = 2f;
    [SerializeField] private float fallDuration = 1f;
    [SerializeField] private float targetLevel = 0f;      // 완전히 찼을 때 기준 수위 (로컬 y)
    [SerializeField] private float hiddenLevel = -3f;     // 완전히 빠졌을 때 기준 수위 (WaterSurface의 waterBottomY와 맞추기)

    [Header("밀려오는 연출")]
    [SerializeField] private float incomingImpulseForce = 6f;
    [SerializeField] private float incomingDelayPerColumn = 0.02f; // 컬럼 간 딜레이 (작을수록 빠르게 훑고 지나감)
    [SerializeField] private float incomingJitter = 0.01f;         // 딜레이에 섞을 랜덤 지터

    [Header("데미지 콜라이더")]
    [SerializeField] private BoxCollider2D damageCollider; // 자식 오브젝트에 붙은 콜라이더
    [SerializeField] private float damageColliderBottomY = -3f;

    private WaterSurface surface;

    private void Awake()
    {
        surface = GetComponent<WaterSurface>();
        surface.BaseLevel = hiddenLevel;

        if (damageCollider != null)
            damageCollider.enabled = false;
    }

    /// <summary>보스 패턴 코드에서 이걸 호출해서 시퀀스를 시작합니다.</summary>
    public void StartSequence()
    {
        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        // 1) 밀려오는 연출: 왼쪽 끝부터 오른쪽으로 순차 impulse
        yield return StartCoroutine(PlayIncomingWave());

        // 2) 차오르는 구간
        yield return StartCoroutine(LerpBaseLevel(hiddenLevel, targetLevel, riseDuration));

        if (damageCollider != null)
            damageCollider.enabled = true;

        // 3) 유지 구간 (기준 수위 고정, 컬럼은 알아서 감쇠되며 잔잔해짐)
        float holdElapsed = 0f;
        while (holdElapsed < holdDuration)
        {
            holdElapsed += Time.deltaTime;
            UpdateDamageCollider();
            yield return null;
        }

        // 4) 빠지는 구간
        if (damageCollider != null)
            damageCollider.enabled = false;

        yield return StartCoroutine(LerpBaseLevel(targetLevel, hiddenLevel, fallDuration));
    }

    private IEnumerator PlayIncomingWave()
    {
        int count = surface.ColumnCount;
        float leftX = surface.GetLeftEdgeWorldX();
        float spacing = surface.Width / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float x = leftX + i * spacing;
            surface.AddImpulse(x, incomingImpulseForce);

            float delay = incomingDelayPerColumn + Random.Range(-incomingJitter, incomingJitter);
            yield return new WaitForSeconds(Mathf.Max(0f, delay));
        }
    }

    private IEnumerator LerpBaseLevel(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            surface.BaseLevel = Mathf.Lerp(from, to, t);
            UpdateDamageCollider();
            yield return null;
        }
        surface.BaseLevel = to;
    }

    private void UpdateDamageCollider()
    {
        if (damageCollider == null) return;

        float top = surface.BaseLevel;
        float height = Mathf.Max(0f, top - damageColliderBottomY);

        damageCollider.size = new Vector2(surface.Width, height);
        damageCollider.offset = new Vector2(0f, damageColliderBottomY + height / 2f);
    }
}


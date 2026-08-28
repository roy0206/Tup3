using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BossHealthView : MonoBehaviour
{
    [Header("대상 (비우면 씬에서 BossBase 를 찾는다)")]
    [SerializeField] private GameObject healthSubject;
    [SerializeField] private bool hideWhenNoBoss = true;

    [Header("바 파트 — 검정 (비우면 이름으로 찾는다)")]
    [SerializeField] private RectTransform leftCap;
    [SerializeField] private RectTransform middle;
    [SerializeField] private RectTransform rightCap;

    [Header("게이지 — 빨강")]
    [SerializeField] private RectTransform fill;
    [SerializeField] private RectTransform leftCapFill;
    [SerializeField] private RectTransform rightCapFill;

    [Header("바 길이 (최대 체력 → 중간 파트 x 스케일)")]
    [SerializeField] private float scalePerHp = 0.06f;
    [SerializeField] private float minMiddleScale = 2f;
    [SerializeField] private float maxMiddleScale = 24f;

    [Header("등장 연출 (BossRoom 이 전투 시작 시 PlayIntro 호출)")]
    [SerializeField] private bool playIntroOnStart;
    [SerializeField] private float introDuration = 0.6f;
    [SerializeField] private Ease introEase = Ease.OutCubic;

    [Header("체력 변화 연출")]
    [SerializeField] private float fillDuration = 0.25f;
    [SerializeField] private Ease fillEase = Ease.OutQuad;

    private const string LeftName = "1";
    private const string MiddleName = "2";
    private const string RightName = "3";
    private const string FillChildName = "Back";

    private BossBase boss;
    private float middleScale;
    private float ratio = 1f;
    private bool introPlayed;
    private Tween introTween;
    private Tween fillTween;

    private void Start()
    {
        DisableAutoLayout();
        ResolveParts();
        NormalizePartImages();
        CenterParts();

        if (!ResolveBoss())
        {
            if (hideWhenNoBoss) gameObject.SetActive(false);
            return;
        }

        DetachFill();

        middleScale = Mathf.Clamp(boss.MaxHp * scalePerHp, minMiddleScale, maxMiddleScale);
        ratio = boss.MaxHp > 0f ? Mathf.Clamp01(boss.Hp / boss.MaxHp) : 1f;

        boss.OnHealthChanged += HandleHealthChanged;

        ApplyLayout(0f);
        SetPartsVisible(false);

        if (playIntroOnStart) PlayIntro();
    }

    public void PlayIntro()
    {
        if (boss == null) return;
        if (introPlayed) return;
        introPlayed = true;

        float target = Mathf.Clamp(boss.MaxHp * scalePerHp, minMiddleScale, maxMiddleScale);

        SetPartsVisible(true);

        if (introDuration <= 0f)
        {
            ApplyLayout(target);
            return;
        }

        introTween?.Kill();
        introTween = DOTween.To(ApplyLayout, 0f, target, introDuration)
            .SetEase(introEase)
            .SetTarget(this);
    }

    private void SetPartsVisible(bool value)
    {
        SetActiveSafe(leftCap, value);
        SetActiveSafe(middle, value);
        SetActiveSafe(rightCap, value);
        SetActiveSafe(fill, value);
    }

    private static void SetActiveSafe(RectTransform target, bool value)
    {
        if (target == null) return;
        if (target.gameObject.activeSelf == value) return;
        target.gameObject.SetActive(value);
    }

    private void OnDestroy()
    {
        introTween?.Kill();
        fillTween?.Kill();
        if (boss != null) boss.OnHealthChanged -= HandleHealthChanged;
    }

    private bool ResolveBoss()
    {
        if (healthSubject != null) boss = healthSubject.GetComponent<BossBase>();
        if (boss == null) boss = FindObjectOfType<BossBase>();
        return boss != null;
    }

    private void ResolveParts()
    {
        if (leftCap == null) leftCap = FindPart(LeftName);
        if (middle == null) middle = FindPart(MiddleName);
        if (rightCap == null) rightCap = FindPart(RightName);

        if (fill == null && middle != null) fill = FindFill(middle);
        if (leftCapFill == null && leftCap != null) leftCapFill = FindFill(leftCap);
        if (rightCapFill == null && rightCap != null) rightCapFill = FindFill(rightCap);
    }

    private RectTransform FindPart(string partName)
    {
        Transform found = transform.Find(partName);
        return found as RectTransform;
    }

    private RectTransform FindFill(RectTransform part)
    {
        Transform found = part.Find(FillChildName);
        return found as RectTransform;
    }

    private void DisableAutoLayout()
    {
        foreach (LayoutGroup group in GetComponents<LayoutGroup>()) group.enabled = false;
        foreach (ContentSizeFitter fitter in GetComponents<ContentSizeFitter>()) fitter.enabled = false;
    }

    private void NormalizePartImages()
    {
        NormalizeImage(leftCap);
        NormalizeImage(middle);
        NormalizeImage(rightCap);
    }

    private static void NormalizeImage(RectTransform target)
    {
        if (target == null) return;
        if (!target.TryGetComponent(out Image image)) return;

        image.type = Image.Type.Simple;
        image.fillAmount = 1f;
    }

    private void CenterParts()
    {
        CenterAnchor(leftCap);
        CenterAnchor(middle);
        CenterAnchor(rightCap);
        CenterAnchor(leftCapFill);
        CenterAnchor(rightCapFill);
    }

    private static void CenterAnchor(RectTransform target)
    {
        if (target == null) return;

        target.anchorMin = new Vector2(0.5f, 0.5f);
        target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = Vector2.zero;
    }

    private void DetachFill()
    {
        if (fill == null) return;

        fill.SetParent(transform, false);
        CenterAnchor(fill);
        fill.SetAsLastSibling();
    }

    private void HandleHealthChanged(float current, float max)
    {
        ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        fillTween?.Kill();

        if (fillDuration <= 0f)
        {
            ApplyFill(ratio);
            return;
        }

        float from = CurrentFillRatio();
        fillTween = DOTween.To(ApplyFill, from, ratio, fillDuration)
            .SetEase(fillEase)
            .SetTarget(this);
    }

    private float CurrentFillRatio()
    {
        if (fill == null || middleScale <= 0f) return ratio;
        return Mathf.Clamp01(fill.localScale.x / middleScale);
    }

    private void ApplyLayout(float scale)
    {
        middleScale = scale;

        if (middle != null)
        {
            Vector3 s = middle.localScale;
            s.x = scale;
            middle.localScale = s;

            Vector2 p = middle.anchoredPosition;
            p.x = 0f;
            middle.anchoredPosition = p;
        }

        float half = MiddleWidth() * 0.5f;
        PlaceCap(leftCap, -(half + CapWidth(leftCap) * 0.5f));
        PlaceCap(rightCap, half + CapWidth(rightCap) * 0.5f);

        ApplyFill(ratio);
    }

    private void PlaceCap(RectTransform cap, float x)
    {
        if (cap == null) return;

        Vector2 p = cap.anchoredPosition;
        p.x = x;
        p.y = middle != null ? middle.anchoredPosition.y : p.y;
        cap.anchoredPosition = p;
    }

    private void ApplyFill(float value)
    {
        float clamped = Mathf.Clamp01(value);

        if (fill != null)
        {
            Vector3 s = fill.localScale;
            s.x = middleScale * clamped;
            fill.localScale = s;

            float width = MiddleWidth();
            Vector2 p = fill.anchoredPosition;
            p.x = width * (clamped - 1f) * 0.5f;
            p.y = middle != null ? middle.anchoredPosition.y : p.y;
            fill.anchoredPosition = p;
        }

        if (leftCapFill != null) leftCapFill.gameObject.SetActive(clamped > 0f);
        if (rightCapFill != null) rightCapFill.gameObject.SetActive(clamped >= 1f);
    }

    private float MiddleWidth()
    {
        if (middle == null) return 0f;
        return middle.rect.width * middleScale;
    }

    private static float CapWidth(RectTransform cap)
    {
        if (cap == null) return 0f;
        return cap.rect.width * cap.localScale.x;
    }
}

/* [파일 노트]
 *
 * PlayerUI 프리팹의 BossHealth 바를 구동한다. 파트 구성은 RoundRed.png 하나를 틴트만 바꿔 쓴다 —
 * "1"(좌 캡) / "2"(중간) / "3"(우 캡) 이 검정 틴트이고, 각자의 자식 "Back" 이 흰색 틴트라
 * 스프라이트 원색인 빨강으로 보인다.
 *
 * ── 길이 = 최대 체력 (2026-08-29 유저 확정) ──────────────────────────────────
 * 중간 파트의 localScale.x 를 maxHp * scalePerHp 로 정해 보스마다 바 길이가 달라진다.
 * min/maxMiddleScale 로 상한·하한을 두어 체력이 극단적인 보스가 화면을 넘지 않게 한다.
 * 0 → 목표 스케일로 늘어나는 연출이며, 그 동안 캡과 게이지가 매 프레임 재배치된다.
 * 좌우 캡은 중간 파트의 양 끝에 붙도록 x 를 계산해 놓으므로 프리팹에서 위치를 맞춰 둘 필요가 없다.
 *
 * ── 등장 시점 = 전투 시작 (2026-08-29 유저 확정) ─────────────────────────────
 * Start 에서는 바를 접어 두고(스케일 0) 파트를 전부 비활성화한 뒤 대기한다. 스케일만 0 으로 두면
 * 좌우 캡이 중앙에 점 두 개로 남아 보이므로 오브젝트 자체를 꺼야 한다.
 * 실제 재생은 BossRoom/FinalBossRoom 이 RoomState.Battle 에 진입할 때 PlayIntro() 를 부른다 —
 * 도입 대사가 끝나야 도달하는 상태이므로 "대사 후 전투 시작"과 정확히 일치한다.
 * introPlayed 가드로 중복 호출은 무시한다.
 * playIntroOnStart 는 보스방을 단독으로 열어 확인할 때 쓰는 디버그용 스위치다(기본 꺼짐).
 *
 * ── 빨강 게이지를 형제로 재배치하는 이유 ─────────────────────────────────────
 * 프리팹에서는 빨강이 검정의 자식이라 부모를 스케일하면 빨강도 같이 늘어나 현재 체력을 표현할 수 없다.
 * Start 에서 DetachFill 이 BossHealth 직속으로 올리고(SetParent(transform, false)) 앵커·피벗을
 * 중앙으로 맞춘 뒤 마지막 형제로 보내 검정 위에 그려지게 한다. 유저가 런타임 재배치를 승인했다.
 *
 * ── 중앙 정렬 ────────────────────────────────────────────────────────────────
 * 프리팹의 "1"/"2"/"3" 은 앵커가 (0,0) 이고 자식 "Back" 은 (0,1) 이었다. 앵커가 (0,0) 이면
 * anchoredPosition 0 이 부모의 중앙이 아니라 좌하단 모서리라, 계산한 x 를 그대로 넣으면
 * 바 전체가 한쪽으로 밀린다. 그래서 Start 에서 CenterParts 가 모든 파트의 앵커·피벗을
 * (0.5, 0.5) 로 정규화하고 위치를 0 으로 초기화한다. 그 뒤 ApplyLayout 이 중간 파트를 x=0 에 두고
 * 좌우 캡을 대칭으로 배치하므로, 바는 BossHealth 루트를 기준으로 정확히 가운데 정렬된다.
 * 화면상 위치를 옮기려면 프리팹에서 BossHealth 루트의 위치만 바꾸면 된다.
 *
 * ── x 좌표 보정 ──────────────────────────────────────────────────────────────
 * 게이지는 왼쪽 끝에 고정된 채 오른쪽으로 줄어들어야 하는데, 피벗이 중앙(0.5)이라 스케일만 줄이면
 * 가운데로 모인다. 그래서 x 를 width * (ratio - 1) / 2 만큼 왼쪽으로 민다.
 * width 는 중간 파트의 실제 폭(rect.width * middleScale)이므로, 바 길이가 바뀌면 x 도 함께 바뀐다
 * — 즉 "검정이 늘어나면 빨강의 x 도 같이 바뀐다"가 이 식 하나로 성립한다.
 *
 * ── 캡의 빨강 ────────────────────────────────────────────────────────────────
 * 둥근 양 끝도 체력이 있을 때 빨갛게 보여야 자연스러우므로, 좌측 캡의 빨강은 ratio > 0 일 때,
 * 우측 캡의 빨강은 ratio >= 1(만피)일 때만 켠다. 필요 없으면 두 참조를 비워 두면 된다.
 *
 * ── 연결 ─────────────────────────────────────────────────────────────────────
 * healthSubject 를 비워 두면 씬에서 BossBase 를 찾는다(HealthView 는 인스펙터 지정 방식이지만,
 * 이 바는 보스 씬마다 대상이 바뀌므로 자동 탐색이 맞다). 보스가 없는 씬(로비 등)에서는
 * hideWhenNoBoss 로 바 자체를 끈다. 체력 갱신은 BossBase 의 IHealthUIEvent.OnHealthChanged 구독.
 * DOTween 기반이라 PauseManager 의 DOTween.PauseAll 에 함께 멈춘다.
 */

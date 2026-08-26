using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CreditsView : MonoBehaviour
{
    [Header("글꼴")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private float fontSize = 30f;

    [Header("색")]
    [SerializeField] private Color backColor = new Color(0f, 0f, 0f, 0.92f);
    [SerializeField] private Color textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private Color hintColor = new Color(1f, 1f, 1f, 0.35f);

    [Header("스크롤")]
    [SerializeField] private float scrollSpeed = 90f;
    [SerializeField] private float fastMultiplier = 8f;
    [SerializeField] private float contentWidth = 1100f;
    [SerializeField] private float endHoldDuration = 0.6f;
    [SerializeField] private int sortingOrder = 800;

    [Header("공통 출처 구간")]
    [SerializeField] private string commonCreditsResourcePath = "Credits/CommonCredits";

    private const string HintMessage = "V / 클릭 — 빨리 감기";

    private RectTransform canvasRect;
    private RectTransform contentRect;
    private TextMeshProUGUI contentLabel;
    private Action onFinished;
    private float contentHeight;
    private float holdTimer;
    private bool built;
    private bool playing;

    public void SetScrollSpeed(float speed, float fastMult)
    {
        if (speed > 0f) scrollSpeed = speed;
        if (fastMult >= 1f) fastMultiplier = fastMult;
    }

    public void Play(string personalContent, Action finishedCallback)
    {
        onFinished = finishedCallback;

        EnsureBuilt();
        gameObject.SetActive(true);

        contentLabel.text = ComposeText(personalContent);

        Canvas.ForceUpdateCanvases();
        contentHeight = Mathf.Max(1f, contentLabel.preferredHeight);
        contentRect.sizeDelta = new Vector2(contentWidth, contentHeight);
        contentRect.anchoredPosition = Vector2.zero;

        holdTimer = 0f;
        playing = true;
    }

    private void Update()
    {
        if (!playing) return;
        if (PauseManager.IsPaused) return;

        float speed = scrollSpeed;
        if (Input.GetKey(KeyCode.V) || Input.GetMouseButton(0)) speed *= fastMultiplier;

        Vector2 pos = contentRect.anchoredPosition;
        pos.y += speed * Time.deltaTime;
        contentRect.anchoredPosition = pos;

        float canvasHeight = canvasRect != null ? canvasRect.rect.height : 1080f;
        if (pos.y < canvasHeight + contentHeight) return;

        holdTimer += Time.deltaTime;
        if (holdTimer < endHoldDuration) return;

        Finish();
    }

    private void Finish()
    {
        playing = false;
        gameObject.SetActive(false);

        var callback = onFinished;
        onFinished = null;
        callback?.Invoke();
    }

    private string ComposeText(string personalContent)
    {
        string common = LoadCommonCredits();
        string personal = string.IsNullOrWhiteSpace(personalContent) ? string.Empty : personalContent.Trim();

        if (string.IsNullOrEmpty(personal)) return common;
        if (string.IsNullOrEmpty(common)) return personal;
        return personal + "\n\n\n\n" + common;
    }

    private string LoadCommonCredits()
    {
        var asset = Resources.Load<TextAsset>(commonCreditsResourcePath);
        if (asset == null)
        {
            Debug.LogError($"[CreditsView] 공통 크레딧 'Resources/{commonCreditsResourcePath}' 를 찾지 못했습니다");
            return string.Empty;
        }
        return asset.text.Trim();
    }

    private void EnsureBuilt()
    {
        if (built) return;
        built = true;

        if (fontAsset == null) fontAsset = UiViewBuilder.FindFallbackFont(transform);

        UiViewBuilder.SetupOverlayCanvas(gameObject, sortingOrder);
        canvasRect = (RectTransform)transform;

        Image dim = UiViewBuilder.BuildDim(transform, backColor);
        dim.raycastTarget = false;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentRect = (RectTransform)contentGo.transform;
        contentRect.SetParent(transform, false);
        contentRect.anchorMin = new Vector2(0.5f, 0f);
        contentRect.anchorMax = new Vector2(0.5f, 0f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(contentWidth, 100f);

        contentLabel = contentGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) contentLabel.font = fontAsset;
        contentLabel.fontSize = fontSize;
        contentLabel.color = textColor;
        contentLabel.alignment = TextAlignmentOptions.Top;
        contentLabel.textWrappingMode = TextWrappingModes.Normal;
        contentLabel.raycastTarget = false;
        contentLabel.text = string.Empty;

        var hint = UiViewBuilder.BuildLabel(transform, "Hint", HintMessage, fontAsset, 22f, hintColor);
        hint.alignment = TextAlignmentOptions.BottomRight;
        var hintRect = (RectTransform)hint.transform;
        hintRect.anchorMin = new Vector2(1f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.anchoredPosition = new Vector2(-32f, 24f);
        hintRect.sizeDelta = new Vector2(420f, 36f);
    }
}

/* [파일 노트]
 *
 * 엔딩 크레딧 스크롤 뷰. PauseMenuView 와 같은 "코드 생성 플랫 UI" 관례(UiViewBuilder)로
 * 첫 Play 때 오버레이 캔버스를 조립한다 — 씬/프리팹 배치가 없어도 Ending.cs 가
 * 빈 GameObject 에 AddComponent 해서 바로 쓸 수 있다.
 *
 * ── 텍스트 구성 ──────────────────────────────────────────────────────────────
 *   [개별 콘텐츠 구간] + 빈 줄 4개 + [공통 출처 구간] 순서로 TMP 텍스트 하나에 합친다.
 *   - 개별 구간 : Ending.cs 의 인스펙터 TextArea(씬=엔딩마다 다른 내용)를 Play 인자로 받는다.
 *   - 공통 구간 : Resources/Credits/CommonCredits.txt 를 로드(모든 엔딩 씬 공유).
 *     BGM 출처(CC-BY 표기)·서드파티 에셋·엔진 표기가 들어 있고, 한 파일만 고치면 전 엔딩에 반영된다.
 *     파일이 없으면 에러 로그 후 개별 구간만 흘린다(예외 없음).
 *
 * ── 스크롤 ───────────────────────────────────────────────────────────────────
 *   Content(pivot 상단, 캔버스 하단 앵커)를 위로 이동. 시작 y=0 이면 텍스트 전체가 화면 아래에
 *   있고, y 가 "캔버스 높이 + 텍스트 높이"를 넘으면 전부 위로 빠져나간 것이므로
 *   endHoldDuration 만큼 여운을 두고 종료 → 자신을 비활성화하고 Play 의 콜백을 1회 호출한다.
 *   텍스트 높이는 Play 에서 Canvas.ForceUpdateCanvases 후 preferredHeight 로 확정한다.
 *
 * ── 입력 ─────────────────────────────────────────────────────────────────────
 *   V 키 또는 마우스 왼쪽 버튼을 "누르고 있는 동안" fastMultiplier 배속(기본 8배) — 가속이 곧
 *   스킵 역할을 한다(끝까지 감기). 프로젝트 전반이 legacy Input 이라 여기도 Input.GetKey 를 쓴다.
 *   V 는 DialogueManager 의 진행 키와 같지만, 크레딧은 대사 종료 후에만 재생되므로 충돌하지 않는다.
 *   PauseManager.IsPaused 동안은 스크롤이 멈춘다.
 *
 * ── 스타일 파라미터 ──────────────────────────────────────────────────────────
 *   전부 SerializeField 라 씬에 미리 배치해 꾸밀 수도 있고, Ending.cs 가 SetScrollSpeed 로
 *   속도/배속만 주입할 수도 있다. sortingOrder 800 은 DialogueUI(10)보다 위,
 *   PauseMenuView(900)보다 아래.
 */

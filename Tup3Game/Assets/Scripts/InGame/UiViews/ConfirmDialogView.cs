using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ConfirmDialogView : MonoBehaviour
{
    [Header("글꼴")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private float titleFontSize = 42f;
    [SerializeField] private float messageFontSize = 26f;
    [SerializeField] private float buttonFontSize = 28f;

    [Header("문구")]
    [SerializeField] private string titleText = "확인";
    [SerializeField, TextArea(2, 8)] private string messageText = "정말 진행할까요?";
    [SerializeField] private string confirmText = "확인";
    [SerializeField] private string cancelText = "취소";

    [Header("색")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.8f);
    [SerializeField] private Color panelColor = new Color(0.06f, 0.05f, 0.04f, 0.95f);
    [SerializeField] private Color buttonColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color textColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color titleColor = new Color(1f, 0.84f, 0.42f, 1f);
    [SerializeField] private Color messageColor = new Color(0.82f, 0.82f, 0.82f, 1f);
    [SerializeField] private Color confirmTextColor = new Color(0.88f, 0.55f, 0.45f, 1f);

    [Header("배치")]
    [SerializeField] private Vector2 buttonSize = new Vector2(300f, 62f);
    [SerializeField] private float messageWidth = 680f;
    [SerializeField] private float messageMinHeight = 96f;
    [SerializeField] private float spacing = 20f;
    [SerializeField] private int sortingOrder = 980;

    public event Action Confirmed;
    public event Action Canceled;

    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI messageLabel;
    private TextMeshProUGUI confirmLabel;
    private TextMeshProUGUI cancelLabel;
    private bool built;

    public void SetContent(string title, string message, string confirm, string cancel)
    {
        if (!string.IsNullOrEmpty(title)) titleText = title;
        if (!string.IsNullOrEmpty(message)) messageText = message;
        if (!string.IsNullOrEmpty(confirm)) confirmText = confirm;
        if (!string.IsNullOrEmpty(cancel)) cancelText = cancel;

        ApplyContent();
    }

    public void Show()
    {
        EnsureBuilt();
        ApplyContent();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void ApplyContent()
    {
        if (!built) return;

        if (titleLabel != null) titleLabel.text = titleText;
        if (messageLabel != null) messageLabel.text = messageText;
        if (confirmLabel != null) confirmLabel.text = confirmText;
        if (cancelLabel != null) cancelLabel.text = cancelText;
    }

    private void EnsureBuilt()
    {
        if (built) return;
        built = true;

        if (fontAsset == null) fontAsset = UiViewBuilder.FindFallbackFont(transform);

        UiViewBuilder.SetupOverlayCanvas(gameObject, sortingOrder);
        UiViewBuilder.BuildDim(transform, dimColor);

        RectTransform panel = UiViewBuilder.BuildCenterPanel(transform, panelColor, spacing);

        titleLabel = UiViewBuilder.BuildLabel(panel, "Title", titleText, fontAsset, titleFontSize, titleColor);

        messageLabel = UiViewBuilder.BuildLabel(panel, "Message", messageText, fontAsset, messageFontSize, messageColor);
        messageLabel.textWrappingMode = TextWrappingModes.Normal;
        messageLabel.alignment = TextAlignmentOptions.Center;

        var messageLayout = messageLabel.gameObject.AddComponent<LayoutElement>();
        messageLayout.preferredWidth = messageWidth;
        messageLayout.minWidth = messageWidth;
        messageLayout.minHeight = messageMinHeight;

        Button confirmButton = UiViewBuilder.BuildButton(
            panel, "ConfirmButton", confirmText, fontAsset, buttonFontSize, buttonColor, confirmTextColor, buttonSize);
        confirmButton.onClick.AddListener(() => Confirmed?.Invoke());
        confirmLabel = confirmButton.GetComponentInChildren<TextMeshProUGUI>(true);

        Button cancelButton = UiViewBuilder.BuildButton(
            panel, "CancelButton", cancelText, fontAsset, buttonFontSize, buttonColor, textColor, buttonSize);
        cancelButton.onClick.AddListener(() => Canceled?.Invoke());
        cancelLabel = cancelButton.GetComponentInChildren<TextMeshProUGUI>(true);
    }
}

/* [파일 노트]
 *
 * "확인 / 취소" 두 선택지를 가진 범용 확인 모달의 표시 전담 뷰. PauseMenuView·GameOverView 와
 * 완전히 같은 관례다 — 첫 Show 때 UiViewBuilder 로 코드 생성하고, 로직은 전혀 갖지 않으며
 * Show()/Hide() 와 Confirmed/Canceled 두 이벤트 발화만 한다. 트윈/애니메이터를 쓰지 않는다
 * (DOTween.PauseAll 과 함께 쓰여도 안전하도록 즉시 표시/숨김).
 *
 * ── 문구 주입 ────────────────────────────────────────────────────────────────
 *   제목/본문/확인/취소 문구는 전부 인스펙터 노출이고, 호출자가 SetContent 로 덮어쓸 수도 있다
 *   (빈 문자열 인자는 무시 → 인스펙터 값 유지). SetContent 는 아직 UI 가 생성되기 전이어도
 *   안전하며(문자열만 보관), 실제 반영은 EnsureBuilt 이후 ApplyContent 가 한다. Show 는 항상
 *   ApplyContent 를 거치므로 "SetContent → Show" / "Show → SetContent" 어느 순서든 동작한다.
 *   현재 유일한 호출자는 Ending.cs 의 "마지막 체크포인트로 돌아가기" 경고창이다.
 *
 * ── 레이아웃 ─────────────────────────────────────────────────────────────────
 *   BuildCenterPanel(세로 레이아웃 + ContentSizeFitter) 위에 제목 → 본문 → 확인 → 취소 순.
 *   본문만 UiViewBuilder 로 만들 수 없는 요구(줄바꿈 폭 제한)가 있어 이 파일에서 직접
 *   LayoutElement(preferredWidth/minWidth = messageWidth)를 붙이고 TMP 를 Normal 줄바꿈으로
 *   바꾼다 — UiViewBuilder 는 공용 파일이라 손대지 않는다.
 *   확인 버튼만 confirmTextColor 로 구분한다(되돌릴 수 없는 선택이라는 신호, GameOverView 관례).
 *
 * ── sortingOrder ─────────────────────────────────────────────────────────────
 *   기본 980. DialogueUI(10) < 엔딩 Canvas(20) < CreditsView(800) < PauseMenuView(900)
 *   < GameOverView(950) < 이 모달(980) 순서라, 어떤 화면 위에서도 확인창이 가장 위에 뜨고
 *   Dim(raycastTarget=true)이 뒤쪽 버튼 클릭을 막는다.
 *
 * ── 인스턴스 출처 ────────────────────────────────────────────────────────────
 *   호출자는 씬에 배치된 인스턴스를 먼저 찾고 없으면 자기 자식으로 AddComponent 하는
 *   폴백을 쓴다(BossRoom 의 GameOverView 처리와 동일). 씬 배치본이어도 EnsureBuilt 는 그대로
 *   돌아 같은 구조를 만들고, 구독은 호출자가 인스턴스 출처와 무관하게 붙인다.
 *   폰트를 비워 두면 씬 안의 기존 TMP 텍스트 폰트 → TMP 기본 폰트 순으로 폴백한다.
 */

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AchievementsPanelView : MonoBehaviour
{
    [Header("글꼴")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private float titleFontSize = 40f;
    [SerializeField] private float progressFontSize = 22f;
    [SerializeField] private float entryTitleFontSize = 25f;
    [SerializeField] private float entryDescFontSize = 19f;
    [SerializeField] private float markFontSize = 26f;
    [SerializeField] private float buttonFontSize = 26f;

    [Header("문구")]
    [SerializeField] private string titleText = "도전과제";
    [SerializeField] private string closeText = "뒤로";
    [SerializeField] private string lockedDescription = "아직 달성하지 못했습니다.";

    [Header("색")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.8f);
    [SerializeField] private Color panelColor = new Color(0.06f, 0.05f, 0.04f, 0.95f);
    [SerializeField] private Color buttonColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color titleColor = new Color(1f, 0.84f, 0.42f, 1f);
    [SerializeField] private Color progressColor = new Color(0.72f, 0.7f, 0.66f, 1f);
    [SerializeField] private Color textColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color descColor = new Color(0.72f, 0.72f, 0.72f, 1f);
    [SerializeField] private Color slotColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color markColor = new Color(0.08f, 0.07f, 0.06f, 1f);

    [Header("클리어 / 미클리어")]
    [SerializeField] private Color clearedColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);

    [Header("배치")]
    [SerializeField] private float rowWidth = 640f;
    [SerializeField] private Vector2 slotSize = new Vector2(56f, 56f);
    [SerializeField] private Vector2 buttonSize = new Vector2(240f, 56f);
    [SerializeField] private float spacing = 18f;
    [SerializeField] private int sortingOrder = 905;

    [Header("조작")]
    [SerializeField] private bool cancelKeyCloses = true;

    public event Action CloseRequested;

    private sealed class Row
    {
        public AchievementInfo Info;
        public Image Slot;
        public TextMeshProUGUI Mark;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Desc;
    }

    private readonly List<Row> rows = new List<Row>();
    private TextMeshProUGUI progressLabel;
    private Button closeButton;
    private UiFocusKeeper focus;
    private GameObject outsideSelection;
    private bool pauseBlocked;
    private bool built;

    public void Show()
    {
        EnsureBuilt();
        Refresh();

        gameObject.SetActive(true);
        outsideSelection = UiFocus.Focus(transform, focus);
        BlockPause();
    }

    public void Hide()
    {
        ReleasePause();
        UiFocus.Blur(transform, outsideSelection);
        outsideSelection = null;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        ReleasePause();
    }

    private void BlockPause()
    {
        if (!cancelKeyCloses || pauseBlocked) return;
        pauseBlocked = true;
        PauseManager.BlockPause();
    }

    private void ReleasePause()
    {
        if (!pauseBlocked) return;
        pauseBlocked = false;
        PauseManager.UnblockPause();
    }

    public void Refresh()
    {
        if (!built) return;

        for (int i = 0; i < rows.Count; i++)
        {
            Row row = rows[i];
            bool cleared = AchievementCatalog.IsUnlocked(row.Info);
            Color state = cleared ? clearedColor : lockedColor;

            if (row.Slot != null) row.Slot.color = row.Info.Tint * state;
            if (row.Mark != null) row.Mark.color = markColor * state;
            if (row.Title != null) row.Title.color = (cleared ? titleColor : textColor) * state;

            if (row.Desc != null)
            {
                row.Desc.text = cleared ? row.Info.Description : lockedDescription;
                row.Desc.color = descColor * state;
            }
        }

        if (progressLabel != null)
            progressLabel.text = $"{AchievementCatalog.UnlockedEndingCount()} / {AchievementCatalog.Endings.Length}";
    }

    private void EnsureBuilt()
    {
        if (built) return;
        built = true;

        UiFocus.EnsureEventSystem();

        if (fontAsset == null) fontAsset = UiViewBuilder.FindFallbackFont(transform);

        UiViewBuilder.SetupOverlayCanvas(gameObject, sortingOrder);
        UiViewBuilder.BuildDim(transform, dimColor);

        RectTransform panel = UiViewBuilder.BuildCenterPanel(transform, panelColor, spacing);

        UiViewBuilder.BuildLabel(panel, "Title", titleText, fontAsset, titleFontSize, titleColor);
        progressLabel = UiViewBuilder.BuildLabel(panel, "Progress", "0 / 0", fontAsset, progressFontSize, progressColor);

        AchievementInfo[] endings = AchievementCatalog.Endings;
        for (int i = 0; i < endings.Length; i++)
            rows.Add(BuildRow(panel, endings[i]));

        closeButton = UiViewBuilder.BuildButton(
            panel, "CloseButton", closeText, fontAsset, buttonFontSize, buttonColor, textColor, buttonSize);
        closeButton.onClick.AddListener(() => CloseRequested?.Invoke());

        UiFocus.LinkVertical(true, closeButton);
        focus = UiFocus.AttachKeeper(gameObject, sortingOrder, closeButton);

        if (cancelKeyCloses)
            closeButton.gameObject.AddComponent<UiCancelRelay>().Setup(() => CloseRequested?.Invoke());
    }

    private Row BuildRow(Transform parent, AchievementInfo info)
    {
        var go = new GameObject("Row_" + info.Id, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var rowElement = go.AddComponent<LayoutElement>();
        rowElement.preferredWidth = rowWidth;
        rowElement.minWidth = rowWidth;

        var slot = new GameObject("Slot", typeof(RectTransform));
        slot.transform.SetParent(go.transform, false);

        var slotImage = slot.AddComponent<Image>();
        slotImage.color = slotColor;
        slotImage.raycastTarget = false;

        var slotElement = slot.AddComponent<LayoutElement>();
        slotElement.preferredWidth = slotSize.x;
        slotElement.preferredHeight = slotSize.y;
        slotElement.minWidth = slotSize.x;
        slotElement.minHeight = slotSize.y;

        TextMeshProUGUI mark = UiViewBuilder.BuildLabel(
            slot.transform, "Mark", info.Mark, fontAsset, markFontSize, markColor);
        var markRect = (RectTransform)mark.transform;
        markRect.anchorMin = Vector2.zero;
        markRect.anchorMax = Vector2.one;
        markRect.offsetMin = Vector2.zero;
        markRect.offsetMax = Vector2.zero;

        var column = new GameObject("Texts", typeof(RectTransform));
        column.transform.SetParent(go.transform, false);

        var columnLayout = column.AddComponent<VerticalLayoutGroup>();
        columnLayout.padding = new RectOffset(0, 0, 0, 0);
        columnLayout.spacing = 2f;
        columnLayout.childAlignment = TextAnchor.MiddleLeft;
        columnLayout.childControlWidth = true;
        columnLayout.childControlHeight = true;
        columnLayout.childForceExpandWidth = false;
        columnLayout.childForceExpandHeight = false;

        float textWidth = rowWidth - slotSize.x - 16f;

        var columnElement = column.AddComponent<LayoutElement>();
        columnElement.preferredWidth = textWidth;
        columnElement.minWidth = textWidth;

        TextMeshProUGUI title = UiViewBuilder.BuildLabel(
            column.transform, "Name", info.Title, fontAsset, entryTitleFontSize, textColor);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        AddTextElement(title, textWidth, entryTitleFontSize * 1.5f);

        TextMeshProUGUI desc = UiViewBuilder.BuildLabel(
            column.transform, "Desc", info.Description, fontAsset, entryDescFontSize, descColor);
        desc.alignment = TextAlignmentOptions.TopLeft;
        desc.textWrappingMode = TextWrappingModes.Normal;
        AddTextElement(desc, textWidth, entryDescFontSize * 1.5f);

        return new Row { Info = info, Slot = slotImage, Mark = mark, Title = title, Desc = desc };
    }

    private static void AddTextElement(TextMeshProUGUI label, float width, float minHeight)
    {
        var element = label.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = width;
        element.minWidth = width;
        element.minHeight = minHeight;
    }
}

/* [파일 노트]
 *
 * 도전과제 목록 화면의 "표시"만 담당하는 뷰. 항목 정의는 AchievementCatalog 가, 열고 닫는 로직은
 * 호출자(StartScene 의 Menu 상태)가 갖는다. PauseMenuView / OptionsPanelView / ConfirmDialogView 와
 * 완전히 같은 관례다 — 첫 Show 때 UiViewBuilder 로 코드 생성하고 Show()/Hide() + CloseRequested
 * 이벤트 하나만 노출한다. 트윈/애니메이터를 쓰지 않는다.
 *
 * ── 무엇이 보이나 ────────────────────────────────────────────────────────────
 *   제목 "도전과제" → 진행도 "n / 3" → 엔딩2·3·4 세 줄 → "뒤로" 버튼.
 *   줄 하나 = [슬롯(로마숫자 II/III/IV)] + [업적 이름 / 설명] 이며 구성은
 *   AchievementCatalog.Endings 배열을 그대로 따른다. 엔딩1 은 게임에서 제거되어 그 배열에
 *   없으므로 여기에도 나오지 않는다(2026-08-29 유저 확정).
 *   해금 전에는 설명 대신 lockedDescription("아직 달성하지 못했습니다.")을 보여 준다 —
 *   이름과 로마숫자는 그대로 두어 "몇 번 엔딩이 남았는지"는 알 수 있게 했다.
 *   Clear 업적은 목록에 넣지 않았다. 엔딩 셋 중 아무거나 하나만 받아도 켜지는 파생 값이라
 *   "엔딩 2·3·4 를 각각 표시한다"는 요구에 노이즈만 되기 때문이다(카탈로그에는 남아 있다).
 *
 * ── 클리어 / 미클리어 표현 ───────────────────────────────────────────────────
 *   EndingBadgeView 와 정확히 같은 규칙을 쓴다 — 상태색을 곱한다.
 *     - 클리어  : clearedColor = (1, 1, 1, 1)            + 이름이 금색(titleColor)으로 바뀐다
 *     - 미클리어: lockedColor  = (0.35, 0.35, 0.35, 0.5) → 어둡고 흐리게
 *   두 화면의 값을 따로 인스펙터 노출한 이유는 배지(작고 멀리서 보는 것)와 목록(가까이 읽는 것)의
 *   적정 대비가 달라질 수 있어서다. 기본값은 일부러 동일하게 맞춰 두었다.
 *
 * ── ESC 처리 (ConfirmDialogView 와 같은 방식) ────────────────────────────────
 *   cancelKeyCloses(기본 켜짐)이면 "뒤로" 버튼에 UiCancelRelay 를 붙여 ESC 로 CloseRequested 를
 *   발화하고, 떠 있는 동안 PauseManager.BlockPause() 를 건다. Start 씬의 ESC 는
 *   PauseManager.HandleEscape → ToggleOptionsOnly 인데, 차단 중에는 HandleEscape 가
 *   "IsPauseBlocked && !IsPaused" 에서 곧바로 return 하므로 ESC 한 번에 "도전과제가 닫히고
 *   옵션이 열리는" 이중 동작이 나지 않는다. Hide 와 OnDisable 양쪽에서 플래그로 정확히 한 번만
 *   UnblockPause 하고, 씬이 바뀌면 PauseManager 가 카운터를 0 으로 되돌리므로 새지 않는다.
 *
 * ── 키보드 조작 ──────────────────────────────────────────────────────────────
 *   선택 가능한 항목은 "뒤로" 하나뿐이라 UiFocus.LinkVertical 로 자기 자신만 Explicit 로 묶는다
 *   (좌우/상하 전부 끊긴 상태). 이렇게 해 두지 않으면 기본값 Automatic 이 Dim 을 뚫고 뒤쪽
 *   타이틀 메뉴 항목으로 새어 나간다 — UiFocus 파일 노트가 경고하는 바로 그 상황이다.
 *   Show 에서 UiFocus.Focus, Hide 에서 UiFocus.Blur 로 열기 직전 선택("도전과제" 항목)을 복원한다.
 *
 * ── sortingOrder ─────────────────────────────────────────────────────────────
 *   905. 타이틀 메뉴(800)와 엔딩 배지(780) 위, 옵션 패널(910)과 일시정지 메뉴(900) 사이다.
 *   Dim(raycastTarget=true)이 뒤쪽 타이틀 메뉴 클릭을 막는다.
 */

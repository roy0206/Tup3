using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameOverView : MonoBehaviour
{
    [Header("글꼴")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private float titleFontSize = 56f;
    [SerializeField] private float buttonFontSize = 30f;

    [Header("문구")]
    [SerializeField] private string titleText = "패배";
    [SerializeField] private string continueLabel = "마지막 지점에서 다시";
    [SerializeField] private string titleSceneLabel = "시작 화면으로";

    [Header("색")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.8f);
    [SerializeField] private Color panelColor = new Color(0.06f, 0.05f, 0.04f, 0.92f);
    [SerializeField] private Color buttonColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color textColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color titleColor = new Color(0.86f, 0.3f, 0.28f, 1f);

    [Header("배치")]
    [SerializeField] private Vector2 buttonSize = new Vector2(360f, 64f);
    [SerializeField] private float spacing = 18f;
    [SerializeField] private int sortingOrder = 950;

    public event Action ContinueRequested;
    public event Action TitleRequested;

    private bool built;
    private Button continueButton;
    private Button titleButton;
    private UiFocusKeeper focus;
    private GameObject outsideSelection;

    public void Show()
    {
        EnsureBuilt();
        gameObject.SetActive(true);
        outsideSelection = UiFocus.Focus(transform, focus);
    }

    public void Hide()
    {
        UiFocus.Blur(transform, outsideSelection);
        outsideSelection = null;
        gameObject.SetActive(false);
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

        continueButton = UiViewBuilder.BuildButton(
            panel, "ContinueButton", continueLabel, fontAsset, buttonFontSize, buttonColor, textColor, buttonSize);
        continueButton.onClick.AddListener(() => ContinueRequested?.Invoke());

        titleButton = UiViewBuilder.BuildButton(
            panel, "TitleButton", titleSceneLabel, fontAsset, buttonFontSize, buttonColor, textColor, buttonSize);
        titleButton.onClick.AddListener(() => TitleRequested?.Invoke());

        UiFocus.LinkVertical(true, continueButton, titleButton);
        focus = UiFocus.AttachKeeper(gameObject, sortingOrder, continueButton, titleButton);
    }
}

/* [파일 노트]
 *
 * 보스방 패배 UI 의 "표시"만 담당하는 뷰. PauseMenuView 와 같은 구조이며 로직은 전부 BossRoom 에 있다.
 * 이 컴포넌트는 Show()/Hide() 와 ContinueRequested/TitleRequested 두 이벤트 발화만 한다.
 *
 * - UI 는 첫 Show 때 UiViewBuilder 로 코드 생성한다(프리팹/씬 배치 불필요).
 *   스타일·문구가 전부 인스펙터 노출이므로 씬에 미리 배치해 꾸며 두면 BossRoom 이 그것을 우선 사용한다.
 * - 버튼 순서는 "마지막 지점에서 다시" → "시작 화면으로".
 *     · 마지막 지점 = 보스방에 들어오기 직전의 로비(BossRoom.defeatReturnScene, 기본 "Lobby").
 *       로비 좌표/체력/스킬은 Lobby.OnSceneLoadComplete 가 세이브에서 복원한다.
 *     · 시작 화면 = BossRoom.titleSceneName(기본 "Start").
 * - sortingOrder 는 PauseMenuView(900) 보다 위인 950. 패배 UI 가 떠 있는 동안 ESC 로 일시정지를 열어도
 *   패배 UI 가 위에 남아 버튼을 계속 누를 수 있다. 씬 전환 시 PauseManager 가 일시정지를 자동 해제한다.
 * - DOTween.PauseAll() 과 함께 쓰이므로 이 뷰는 트윈/애니메이터를 쓰지 않는다(즉시 표시/숨김).
 * - 폰트를 비워 두면 씬 안의 기존 TMP 텍스트 폰트 → TMP 기본 폰트 순으로 폴백한다.
 *
 * ── 키보드 조작 (2026-08-28 유저 확정) ───────────────────────────────────────
 *   Show 때 UiFocus.Focus 가 "마지막 지점에서 다시"(첫 항목)를 선택하므로 곧바로 ↑/↓ + Enter 로
 *   고를 수 있다. 두 항목은 UiFocus.LinkVertical 로 Explicit 순환 연결한다.
 *   위 파일 노트대로 이 UI 가 떠 있는 동안에도 ESC 로 일시정지를 열 수 있는데(패배 UI 950 >
 *   일시정지 900 이라 패배 UI 가 계속 위에 보인다), 이때 일시정지 메뉴가 선택을 가져가고
 *   PauseMenuView.Hide 의 UiFocus.Blur 가 여기 있던 항목으로 선택을 되돌려 준다.
 *   그래서 ESC → ESC 를 오가도 Enter 가 엉뚱한 버튼을 누르지 않는다.
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PauseMenuView : MonoBehaviour
{
    [Header("글꼴")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private float titleFontSize = 46f;
    [SerializeField] private float buttonFontSize = 30f;

    [Header("색")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.66f);
    [SerializeField] private Color panelColor = new Color(0.06f, 0.05f, 0.04f, 0.92f);
    [SerializeField] private Color buttonColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color textColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color titleColor = new Color(1f, 0.84f, 0.42f, 1f);

    [Header("배치")]
    [SerializeField] private Vector2 buttonSize = new Vector2(320f, 64f);
    [SerializeField] private float spacing = 18f;
    [SerializeField] private int sortingOrder = 900;

    public event Action ResumeRequested;
    public event Action OptionsRequested;

    private bool built;

    public void Show()
    {
        EnsureBuilt();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void EnsureBuilt()
    {
        if (built) return;
        built = true;

        if (fontAsset == null) fontAsset = UiViewBuilder.FindFallbackFont(transform);

        UiViewBuilder.SetupOverlayCanvas(gameObject, sortingOrder);
        UiViewBuilder.BuildDim(transform, dimColor);

        RectTransform panel = UiViewBuilder.BuildCenterPanel(transform, panelColor, spacing);

        UiViewBuilder.BuildLabel(panel, "Title", "일시정지", fontAsset, titleFontSize, titleColor);

        Button resumeButton = UiViewBuilder.BuildButton(
            panel, "ResumeButton", "계속하기", fontAsset, buttonFontSize, buttonColor, textColor, buttonSize);
        resumeButton.onClick.AddListener(() => ResumeRequested?.Invoke());

        Button optionsButton = UiViewBuilder.BuildButton(
            panel, "OptionsButton", "옵션", fontAsset, buttonFontSize, buttonColor, textColor, buttonSize);
        optionsButton.onClick.AddListener(() => OptionsRequested?.Invoke());
    }
}

/* [파일 노트]
 *
 * 일시정지 메뉴의 "표시"만 담당하는 뷰. 로직(정지/재개, ESC 라우팅)은 전부 PauseManager 에 있고
 * 이 컴포넌트는 Show()/Hide() 호출과 ResumeRequested/OptionsRequested 이벤트 발화만 한다.
 *
 * - UI 는 첫 Show 때 UiViewBuilder 로 코드 생성한다(DialogueChoiceView 와 같은 플랫 스타일,
 *   프리팹/씬 배치 불필요). 스타일 파라미터는 전부 인스펙터 노출이므로 씬에 미리 배치해 꾸밀 수도 있다.
 * - PauseManager 는 씬에서 PauseMenuView 를 먼저 찾고 없으면 자기 자식으로 생성한다.
 *   → UI 를 교체하려면 같은 public API(Show/Hide/두 이벤트)를 가진 이 컴포넌트를
 *     씬(또는 프리팹)에 배치해 원하는 모양으로 꾸미거나, 이 파일의 EnsureBuilt 만 갈아끼우면 된다.
 * - DOTween.PauseAll() 과 함께 쓰이므로 이 뷰는 트윈/애니메이터를 절대 쓰지 않는다(즉시 표시/숨김).
 * - 폰트를 비워 두면 씬 안의 기존 TMP 텍스트 폰트 → TMP 기본 폰트 순으로 폴백한다.
 */

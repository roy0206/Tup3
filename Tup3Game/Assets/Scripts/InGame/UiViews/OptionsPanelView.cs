using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class OptionsPanelView : MonoBehaviour
{
    [Header("글꼴")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private float titleFontSize = 40f;
    [SerializeField] private float labelFontSize = 26f;
    [SerializeField] private float buttonFontSize = 26f;

    [Header("색")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.66f);
    [SerializeField] private Color panelColor = new Color(0.06f, 0.05f, 0.04f, 0.92f);
    [SerializeField] private Color buttonColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color textColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color titleColor = new Color(1f, 0.84f, 0.42f, 1f);
    [SerializeField] private Color trackColor = new Color(0.22f, 0.2f, 0.18f, 1f);
    [SerializeField] private Color fillColor = new Color(1f, 0.78f, 0.32f, 1f);
    [SerializeField] private Color handleColor = new Color(0.95f, 0.92f, 0.85f, 1f);

    [Header("배치")]
    [SerializeField] private Vector2 sliderSize = new Vector2(360f, 28f);
    [SerializeField] private Vector2 buttonSize = new Vector2(240f, 56f);
    [SerializeField] private float spacing = 22f;
    [SerializeField] private int sortingOrder = 910;

    public event Action<float> BgmChanged;
    public event Action<float> SfxChanged;
    public event Action CloseRequested;

    private bool built;
    private Slider bgmSlider;
    private Slider sfxSlider;
    private TextMeshProUGUI bgmValueLabel;
    private TextMeshProUGUI sfxValueLabel;

    public void Show(float bgmValue, float sfxValue)
    {
        EnsureBuilt();

        bgmSlider.SetValueWithoutNotify(Mathf.Clamp01(bgmValue));
        sfxSlider.SetValueWithoutNotify(Mathf.Clamp01(sfxValue));
        RefreshValueLabels();

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

        UiViewBuilder.BuildLabel(panel, "Title", "옵션", fontAsset, titleFontSize, titleColor);

        (bgmSlider, bgmValueLabel) = BuildVolumeRow(panel, "BgmRow", "배경음");
        bgmSlider.onValueChanged.AddListener(value =>
        {
            RefreshValueLabels();
            BgmChanged?.Invoke(value);
        });

        (sfxSlider, sfxValueLabel) = BuildVolumeRow(panel, "SfxRow", "효과음");
        sfxSlider.onValueChanged.AddListener(value =>
        {
            RefreshValueLabels();
            SfxChanged?.Invoke(value);
        });

        Button closeButton = UiViewBuilder.BuildButton(
            panel, "CloseButton", "뒤로", fontAsset, buttonFontSize, buttonColor, textColor, buttonSize);
        closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
    }

    private (Slider, TextMeshProUGUI) BuildVolumeRow(Transform parent, string name, string labelText)
    {
        var row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);

        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var label = UiViewBuilder.BuildLabel(row.transform, "Label", labelText, fontAsset, labelFontSize, textColor);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        var labelElement = label.gameObject.AddComponent<LayoutElement>();
        labelElement.preferredWidth = 110f;

        Slider slider = UiViewBuilder.BuildSlider(
            row.transform, "Slider", sliderSize, trackColor, fillColor, handleColor);

        var valueLabel = UiViewBuilder.BuildLabel(row.transform, "Value", "100%", fontAsset, labelFontSize, textColor);
        valueLabel.alignment = TextAlignmentOptions.MidlineRight;
        var valueElement = valueLabel.gameObject.AddComponent<LayoutElement>();
        valueElement.preferredWidth = 80f;

        return (slider, valueLabel);
    }

    private void RefreshValueLabels()
    {
        if (bgmValueLabel != null) bgmValueLabel.text = $"{Mathf.RoundToInt(bgmSlider.value * 100f)}%";
        if (sfxValueLabel != null) sfxValueLabel.text = $"{Mathf.RoundToInt(sfxSlider.value * 100f)}%";
    }
}

/* [파일 노트]
 *
 * 볼륨 옵션 패널의 "표시"만 담당하는 뷰. 값의 실제 적용/저장은 PauseManager 가 구독해 둔
 * VolumeSettings(SetBgm/SetSfx/SaveIfDirty)가 처리한다.
 *
 * - 구성 : 제목 "옵션" + 배경음/효과음 슬라이더(0..1, % 표시) + "뒤로" 버튼. 전부 코드 생성 플랫 스타일.
 * - Show(bgm, sfx) 는 SetValueWithoutNotify 로 초기값을 넣으므로 열자마자 콜백이 튀지 않는다.
 * - 일시정지 메뉴에서 열리는 경우와 Start(타이틀) 씬에서 단독으로 열리는 경우 모두 같은 인스턴스를
 *   재사용한다(분기는 PauseManager 담당). 트윈/애니메이터는 쓰지 않는다(DOTween.PauseAll 과 공존).
 * - UI 교체 방법은 PauseMenuView 와 동일: 같은 API 를 가진 이 컴포넌트를 씬에 직접 배치하면
 *   PauseManager 가 그것을 우선 사용한다.
 * - 조작은 마우스 기준(EventSystem 은 PauseManager 가 보장). 키보드 내비게이션은 추후 확장.
 */

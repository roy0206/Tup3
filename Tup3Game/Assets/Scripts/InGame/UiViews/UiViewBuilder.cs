using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UiViewBuilder
{
    public static void SetupOverlayCanvas(GameObject root, int sortingOrder)
    {
        var canvas = root.GetComponent<Canvas>();
        if (canvas == null) canvas = root.AddComponent<Canvas>();

        var cam = Camera.main;
        if (cam != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        canvas.sortingOrder = sortingOrder;

        var scaler = root.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (root.GetComponent<GraphicRaycaster>() == null)
            root.AddComponent<GraphicRaycaster>();
    }

    public static Image BuildDim(Transform parent, Color color)
    {
        var go = new GameObject("Dim", typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    public static RectTransform BuildCenterPanel(Transform parent, Color backColor, float spacing)
    {
        var go = new GameObject("Panel", typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = backColor;
        image.raycastTarget = true;

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 36, 36);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = go.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rect;
    }

    public static TextMeshProUGUI BuildLabel(
        Transform parent, string name, string text,
        TMP_FontAsset font, float fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var label = go.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    public static Button BuildButton(
        Transform parent, string name, string text,
        TMP_FontAsset font, float fontSize,
        Color backColor, Color textColor, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.color = backColor;
        image.raycastTarget = true;

        var layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var label = BuildLabel(go.transform, "Label", text, font, fontSize, textColor);
        var labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    public static Slider BuildSlider(
        Transform parent, string name, Vector2 size,
        Color trackColor, Color fillColor, Color handleColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;

        var background = new GameObject("Background", typeof(RectTransform));
        var backgroundRect = (RectTransform)background.transform;
        backgroundRect.SetParent(go.transform, false);
        backgroundRect.anchorMin = new Vector2(0f, 0.3f);
        backgroundRect.anchorMax = new Vector2(1f, 0.7f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        var backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = trackColor;
        backgroundImage.raycastTarget = true;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        var fillAreaRect = (RectTransform)fillArea.transform;
        fillAreaRect.SetParent(go.transform, false);
        fillAreaRect.anchorMin = new Vector2(0f, 0.3f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.7f);
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill", typeof(RectTransform));
        var fillRect = (RectTransform)fill.transform;
        fillRect.SetParent(fillAreaRect, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = fill.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.raycastTarget = false;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        var handleAreaRect = (RectTransform)handleArea.transform;
        handleAreaRect.SetParent(go.transform, false);
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(8f, 0f);
        handleAreaRect.offsetMax = new Vector2(-8f, 0f);

        var handle = new GameObject("Handle", typeof(RectTransform));
        var handleRect = (RectTransform)handle.transform;
        handleRect.SetParent(handleAreaRect, false);
        handleRect.sizeDelta = new Vector2(16f, 0f);
        var handleImage = handle.AddComponent<Image>();
        handleImage.color = handleColor;
        handleImage.raycastTarget = true;

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        return slider;
    }

    public static TMP_FontAsset FindFallbackFont(Transform origin)
    {
        var texts = Object.FindObjectsByType<TextMeshProUGUI>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].font != null && !texts[i].transform.IsChildOf(origin))
                return texts[i].font;
        }

        return TMP_Settings.defaultFontAsset;
    }
}

/* [파일 노트]
 *
 * 일시정지/옵션 뷰가 공용으로 쓰는 코드 생성 uGUI 빌더 모음(정적 클래스, 상태 없음).
 * DialogueChoiceView 의 "코드로 만드는 플랫 UI" 관례를 따르며 스프라이트 없이 단색 Image 로만 구성한다.
 *
 * - SetupOverlayCanvas : ScreenSpaceOverlay 캔버스 + CanvasScaler(1920x1080 기준) + GraphicRaycaster.
 * - BuildDim           : 화면 전체 반투명 차단막. raycastTarget=true 라 뒤쪽 UI 클릭도 막는다.
 * - BuildCenterPanel   : 중앙 정렬 세로 레이아웃 패널(내용 크기에 맞춰 자동 확장).
 * - BuildButton        : Image + Button(ColorTint) + TMP 라벨. 트윈 없이 기본 틴트 전환만 쓴다.
 * - BuildSlider        : uGUI Slider 를 배경/채움/핸들 구조로 조립(0..1, 가로).
 * - FindFallbackFont   : 씬의 기존 TMP 텍스트 폰트를 물려받고, 없으면 TMP 기본 폰트.
 */

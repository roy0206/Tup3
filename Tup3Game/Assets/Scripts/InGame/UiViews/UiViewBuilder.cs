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
        ApplySelectionTint(button);

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

    public static void ApplySelectionTint(Selectable selectable)
    {
        ApplySelectionTint(selectable, Color.white, 0.16f, 0.36f, 0.54f);
    }

    public static void ApplySelectionTint(
        Selectable selectable, Color accent, float highlighted, float selected, float pressed)
    {
        if (selectable == null) return;

        selectable.transition = Selectable.Transition.ColorTint;

        Graphic graphic = selectable.targetGraphic;
        ColorBlock colors = selectable.colors;

        Color baseColor = colors.normalColor;
        if (graphic != null)
        {
            baseColor *= graphic.color;
            graphic.color = Color.white;
        }

        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, accent, highlighted);
        colors.selectedColor = Color.Lerp(baseColor, accent, selected);
        colors.pressedColor = Color.Lerp(baseColor, accent, pressed);
        colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * 0.4f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;

        selectable.colors = colors;
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
 * - ApplySelectionTint : Selectable 의 ColorBlock 을 "선택이 눈에 보이도록" 다시 계산해 넣는다.
 * - FindFallbackFont   : 씬의 기존 TMP 텍스트 폰트를 물려받고, 없으면 TMP 기본 폰트.
 *
 * ── ApplySelectionTint — 검은 버튼에서 틴트가 보이지 않던 문제 (2026-08-28) ──
 *   uGUI 의 ColorTint 는 targetGraphic 의 색에 ColorBlock 색을 "곱한다".
 *   예전 BuildButton 은 Image.color = buttonColor(대개 (0,0,0,0.75) 검정)로 두고
 *   normalColor = 흰색, highlighted/selected = (1.35,1.35,1.35) 로 잡았는데,
 *   검정에 무엇을 곱해도 검정이라 강조 상태가 알파만 아주 조금 달라질 뿐 사실상 보이지 않았다.
 *   마우스로만 쓸 때는 커서 위치가 곧 강조라 티가 안 났지만, 키보드 조작에서는
 *   "지금 어느 항목에 있는지"가 전혀 읽히지 않아 치명적이다.
 *   그래서 색을 다음처럼 뒤집는다.
 *     - 원래 보이던 색(= normalColor × Image.color)을 계산해 normalColor 로 옮기고
 *       Image.color 는 흰색으로 만든다. 평상시 겉모습은 그대로다.
 *     - highlighted / selected / pressed 는 그 기준색에서 accent(기본 흰색) 쪽으로
 *       0.16 / 0.36 / 0.54 만큼 보간한다. 검은 패널 위에서 선택 항목만 확실히 밝아지고,
 *       마우스 호버(0.16)보다 키보드 선택(0.36)이 더 강해 둘이 섞여도 구분된다.
 *   Image.color 를 흰색으로 접어 넣는 계산이라 두 번 호출해도 결과가 같다(멱등).
 *   씬에 미리 배치된 버튼(Ending 씬의 ReturnButton 등)에도 그대로 쓸 수 있다 —
 *   Ending.cs 가 복제한 버튼까지 포함해 이 함수를 통과시킨다.
 */

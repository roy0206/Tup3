using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DialogueChoiceView : MonoBehaviour
{
    [Header("배치")]
    [SerializeField] private float bottomOffset = 160f;
    [SerializeField] private float optionSpacing = 24f;
    [SerializeField] private Vector2 optionPadding = new Vector2(40f, 20f);
    [SerializeField] private float labelToBarSpacing = 8f;
    [SerializeField] private int maxOptionCount = 4;

    [Header("글꼴")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private float fontSize = 34f;

    [Header("색")]
    [SerializeField] private Color normalBackColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color selectedBackColor = new Color(0.10f, 0.08f, 0.05f, 0.94f);
    [SerializeField] private Color normalTextColor = new Color(0.66f, 0.66f, 0.66f, 1f);
    [SerializeField] private Color selectedTextColor = new Color(1f, 0.84f, 0.42f, 1f);
    [SerializeField] private Color accentBarColor = new Color(1f, 0.78f, 0.32f, 1f);
    [SerializeField] private float accentBarHeight = 3f;

    [Header("트윈")]
    [SerializeField] private float selectedScale = 1.08f;
    [SerializeField] private float normalScale = 0.96f;
    [SerializeField] private float highlightDuration = 0.12f;
    [SerializeField] private float fadeDuration = 0.15f;

    private class Option
    {
        public GameObject root;
        public RectTransform rect;
        public Image back;
        public TextMeshProUGUI label;
        public Image accent;
        public Tween backTween;
        public Tween textTween;
        public Tween accentTween;
        public Tween scaleTween;
    }

    private RectTransform row;
    private CanvasGroup group;
    private Tween fadeTween;
    private readonly List<Option> options = new();
    private int visibleCount;
    private int highlightIndex = -1;

    public bool IsShown { get; private set; }

    public void Show(IList<DialogueManager.Choice> choices, int selectedIndex)
    {
        if (choices == null || choices.Count == 0)
        {
            Hide();
            return;
        }

        EnsureBuilt();

        visibleCount = Mathf.Min(choices.Count, options.Count);

        for (int i = 0; i < options.Count; i++)
        {
            bool used = i < visibleCount;
            options[i].root.SetActive(used);
            if (used) options[i].label.text = choices[i].label;
        }

        gameObject.SetActive(true);
        row.gameObject.SetActive(true);
        IsShown = true;

        LayoutRebuilder.ForceRebuildLayoutImmediate(row);

        highlightIndex = -1;
        ApplyHighlight(Mathf.Clamp(selectedIndex, 0, visibleCount - 1), true);

        fadeTween?.Kill();
        group.alpha = 0f;
        fadeTween = group.DOFade(1f, fadeDuration).SetUpdate(true);
    }

    public void SetHighlight(int index)
    {
        if (!IsShown || visibleCount == 0) return;
        ApplyHighlight(Mathf.Clamp(index, 0, visibleCount - 1), false);
    }

    public void Hide()
    {
        if (row == null)
        {
            IsShown = false;
            return;
        }

        IsShown = false;
        fadeTween?.Kill();
        fadeTween = group.DOFade(0f, fadeDuration)
            .SetUpdate(true)
            .OnComplete(() => row.gameObject.SetActive(false));
    }

    public void HideInstant()
    {
        IsShown = false;
        if (row == null) return;
        fadeTween?.Kill();
        group.alpha = 0f;
        row.gameObject.SetActive(false);
    }

    private void ApplyHighlight(int index, bool instant)
    {
        if (highlightIndex == index && !instant) return;
        highlightIndex = index;

        for (int i = 0; i < visibleCount; i++)
        {
            Option o = options[i];
            bool selected = i == index;

            Color back = selected ? selectedBackColor : normalBackColor;
            Color text = selected ? selectedTextColor : normalTextColor;
            Color accent = accentBarColor;
            accent.a = selected ? accentBarColor.a : 0f;
            float scale = selected ? selectedScale : normalScale;

            o.backTween?.Kill();
            o.textTween?.Kill();
            o.accentTween?.Kill();
            o.scaleTween?.Kill();

            if (instant || highlightDuration <= 0f)
            {
                o.back.color = back;
                o.label.color = text;
                o.accent.color = accent;
                o.rect.localScale = Vector3.one * scale;
                continue;
            }

            o.backTween = o.back.DOColor(back, highlightDuration).SetUpdate(true);
            o.textTween = o.label.DOColor(text, highlightDuration).SetUpdate(true);
            o.accentTween = o.accent.DOColor(accent, highlightDuration).SetUpdate(true);
            o.scaleTween = o.rect.DOScale(scale, highlightDuration).SetUpdate(true);
        }
    }

    private void EnsureBuilt()
    {
        if (row != null) return;

        if (fontAsset == null) fontAsset = FindFallbackFont();

        GameObject rowObject = new GameObject("ChoiceRow", typeof(RectTransform));
        row = rowObject.GetComponent<RectTransform>();
        row.SetParent(transform, false);
        row.anchorMin = new Vector2(0.5f, 0f);
        row.anchorMax = new Vector2(0.5f, 0f);
        row.pivot = new Vector2(0.5f, 0f);
        row.anchoredPosition = new Vector2(0f, bottomOffset);

        HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = optionSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = rowObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        group = rowObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        int count = Mathf.Max(1, maxOptionCount);
        for (int i = 0; i < count; i++) options.Add(BuildOption(i));

        rowObject.SetActive(false);
    }

    private Option BuildOption(int index)
    {
        GameObject root = new GameObject($"Choice_{index}", typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(row, false);

        Image back = root.AddComponent<Image>();
        back.color = normalBackColor;
        back.raycastTarget = false;

        VerticalLayoutGroup inner = root.AddComponent<VerticalLayoutGroup>();
        inner.padding = new RectOffset(
            Mathf.RoundToInt(optionPadding.x),
            Mathf.RoundToInt(optionPadding.x),
            Mathf.RoundToInt(optionPadding.y),
            Mathf.RoundToInt(optionPadding.y));
        inner.spacing = labelToBarSpacing;
        inner.childAlignment = TextAnchor.MiddleCenter;
        inner.childControlWidth = true;
        inner.childControlHeight = true;
        inner.childForceExpandWidth = true;
        inner.childForceExpandHeight = false;

        ContentSizeFitter fitter = root.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(rect, false);
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) label.font = fontAsset;
        label.fontSize = fontSize;
        label.color = normalTextColor;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        GameObject accentObject = new GameObject("AccentBar", typeof(RectTransform));
        accentObject.transform.SetParent(rect, false);
        Image accent = accentObject.AddComponent<Image>();
        Color hidden = accentBarColor;
        hidden.a = 0f;
        accent.color = hidden;
        accent.raycastTarget = false;

        LayoutElement accentLayout = accentObject.AddComponent<LayoutElement>();
        accentLayout.minHeight = accentBarHeight;
        accentLayout.preferredHeight = accentBarHeight;
        accentLayout.flexibleWidth = 1f;

        rect.localScale = Vector3.one * normalScale;

        return new Option
        {
            root = root,
            rect = rect,
            back = back,
            label = label,
            accent = accent
        };
    }

    private TMP_FontAsset FindFallbackFont()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform search = canvas != null ? canvas.transform : transform.root;
        TextMeshProUGUI[] texts = search.GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].font != null) return texts[i].font;
        }
        return null;
    }

    private void KillAllTweens()
    {
        fadeTween?.Kill();
        fadeTween = null;

        for (int i = 0; i < options.Count; i++)
        {
            options[i].backTween?.Kill();
            options[i].textTween?.Kill();
            options[i].accentTween?.Kill();
            options[i].scaleTween?.Kill();
        }
    }

    private void OnDisable()
    {
        KillAllTweens();
        IsShown = false;
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }
}

/* [파일 노트]
 *
 * 1) 역할
 *    DialogueManager 의 선택지 "표시"만 담당하는 뷰. 로직(선택 이동/확정/점프)은 전부 DialogueManager 에 남아 있고
 *    이 컴포넌트는 Show(choices, selectedIndex) / SetHighlight(index) / Hide() 세 가지만 호출받는다.
 *    DialogueManager 는 Awake 에서 dialogueRoot 하위를 GetComponentInChildren<DialogueChoiceView>(true) 로 훑어
 *    이 컴포넌트를 자동으로 찾는다. 못 찾으면 기존 ChoicePanel 방식으로 폴백한다.
 *
 * 2) 씬 배치 절차 (씬 파일을 직접 고치지 않고 에디터에서)
 *    - Hierarchy 에서 DialogueManager 의 dialogueRoot(= PlayerPanel/BossPanel/ChoicePanel 이 들어 있는 캔버스 하위 오브젝트)
 *      를 우클릭 → Create Empty → 이름을 "ChoiceView" 로 바꾼다.
 *    - 그 오브젝트에 DialogueChoiceView 컴포넌트를 붙인다. 그게 전부다.
 *    - 폰트를 명시하고 싶으면 fontAsset 에 Assets/GameAssets/Fonts/PRETENDARD-MEDIUM SDF 를 넣는다.
 *      비워두면 같은 Canvas 안의 기존 TMP 텍스트(대화창)가 쓰는 폰트를 그대로 물려받는다.
 *    - 기존 ChoicePanel 오브젝트는 지우지 않아도 된다. 뷰가 있으면 아예 켜지지 않는다.
 *
 * 3) 런타임 구성
 *    첫 Show 때 EnsureBuilt() 가 자식으로 UI 를 만든다. 계층은 다음과 같다.
 *      (this)
 *        └ ChoiceRow           : RectTransform(하단 중앙 앵커) + HorizontalLayoutGroup + ContentSizeFitter + CanvasGroup
 *            └ Choice_0..N     : Image(배경) + VerticalLayoutGroup + ContentSizeFitter
 *                 ├ Label      : TextMeshProUGUI
 *                 └ AccentBar  : Image + LayoutElement(높이 고정, 가로 확장)
 *    선택지 개수는 maxOptionCount(기본 4) 만큼 미리 만들어 두고 필요한 개수만 SetActive(true) 한다.
 *    라벨 길이에 따른 폭은 ContentSizeFitter 가 잡아 주므로 별도 계산이 없다.
 *
 * 4) 스타일 파라미터(전부 인스펙터 노출)
 *    - 배치      : bottomOffset(화면 하단에서 띄우는 높이), optionSpacing, optionPadding(가로/세로 내부 여백),
 *                  labelToBarSpacing, maxOptionCount
 *    - 글꼴      : fontAsset, fontSize
 *    - 색        : normalBackColor / selectedBackColor / normalTextColor / selectedTextColor / accentBarColor,
 *                  accentBarHeight
 *    - 트윈      : selectedScale(1.08), normalScale(0.96), highlightDuration(0.12), fadeDuration(0.15)
 *
 * 5) 트윈
 *    DOTween 으로 배경색·글자색·강조바 알파·스케일을 동시에 보간한다. 모든 트윈에 SetUpdate(true) 를 걸어
 *    Time.timeScale 을 0 으로 만드는 연출이 들어와도 선택지 UI 는 계속 반응한다.
 *    OnDisable/OnDestroy 에서 전부 Kill 하므로 씬 전환 중 파괴된 대상에 트윈이 남는 사고는 없다.
 *
 * 6) HideInstant()
 *    페이드 없이 즉시 끈다. DialogueManager 가 대화를 통째로 종료(EndDialogue)할 때 쓴다.
 */

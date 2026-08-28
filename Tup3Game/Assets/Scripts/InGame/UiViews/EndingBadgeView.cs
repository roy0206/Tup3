using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EndingBadgeView : MonoBehaviour
{
    [Header("아이콘 (엔딩2·3·4 순서 — 비우면 단색 사각형 + 로마숫자로 대체)")]
    [SerializeField] private Sprite[] icons;

    [Header("글꼴")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private float markFontSize = 30f;
    [SerializeField] private float captionFontSize = 17f;

    [Header("색 — 클리어 / 미클리어")]
    [SerializeField] private Color clearedColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
    [SerializeField] private Color frameColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color markColor = new Color(0.08f, 0.07f, 0.06f, 1f);
    [SerializeField] private Color captionColor = new Color(0.92f, 0.92f, 0.92f, 1f);

    [Header("배치 — 화면 오른쪽 구석")]
    [SerializeField] private Vector2 cornerAnchor = new Vector2(1f, 1f);
    [SerializeField] private Vector2 cornerOffset = new Vector2(-56f, -48f);
    [SerializeField] private Vector2 badgeSize = new Vector2(72f, 72f);
    [SerializeField] private float badgeSpacing = 16f;
    [SerializeField] private float iconPadding = 8f;
    [SerializeField] private bool showCaption = true;
    [SerializeField] private float captionSpacing = 6f;
    [SerializeField] private int sortingOrder = 780;

    private sealed class Badge
    {
        public AchievementInfo Info;
        public Image Icon;
        public TextMeshProUGUI Mark;
        public TextMeshProUGUI Caption;
    }

    private readonly List<Badge> badges = new List<Badge>();
    private bool built;

    public void Show()
    {
        EnsureBuilt();
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        for (int i = 0; i < badges.Count; i++)
        {
            Badge badge = badges[i];
            bool cleared = AchievementCatalog.IsUnlocked(badge.Info);
            Color state = cleared ? clearedColor : lockedColor;

            if (badge.Icon != null)
            {
                Color baseColor = badge.Icon.sprite != null ? Color.white : badge.Info.Tint;
                badge.Icon.color = baseColor * state;
            }

            if (badge.Mark != null) badge.Mark.color = markColor * state;
            if (badge.Caption != null) badge.Caption.color = captionColor * state;
        }
    }

    private void EnsureBuilt()
    {
        if (built) return;
        built = true;

        if (fontAsset == null) fontAsset = UiViewBuilder.FindFallbackFont(transform);

        UiViewBuilder.SetupOverlayCanvas(gameObject, sortingOrder);

        RectTransform row = BuildRow();

        AchievementInfo[] endings = AchievementCatalog.Endings;
        for (int i = 0; i < endings.Length; i++)
            badges.Add(BuildBadge(row, endings[i], ResolveIcon(i)));
    }

    private Sprite ResolveIcon(int index)
    {
        if (icons == null || index < 0 || index >= icons.Length) return null;
        return icons[index];
    }

    private RectTransform BuildRow()
    {
        var go = new GameObject("Badges", typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);

        Vector2 corner = new Vector2(Mathf.Clamp01(cornerAnchor.x), Mathf.Clamp01(cornerAnchor.y));
        rect.anchorMin = corner;
        rect.anchorMax = corner;
        rect.pivot = corner;
        rect.anchoredPosition = cornerOffset;

        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = badgeSpacing;
        layout.childAlignment = corner.y > 0.5f ? TextAnchor.UpperRight : TextAnchor.LowerRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = go.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rect;
    }

    private Badge BuildBadge(Transform parent, AchievementInfo info, Sprite icon)
    {
        var column = new GameObject("Badge_" + info.Id, typeof(RectTransform));
        column.transform.SetParent(parent, false);

        var columnLayout = column.AddComponent<VerticalLayoutGroup>();
        columnLayout.padding = new RectOffset(0, 0, 0, 0);
        columnLayout.spacing = captionSpacing;
        columnLayout.childAlignment = TextAnchor.UpperCenter;
        columnLayout.childControlWidth = true;
        columnLayout.childControlHeight = true;
        columnLayout.childForceExpandWidth = false;
        columnLayout.childForceExpandHeight = false;

        var frame = new GameObject("Frame", typeof(RectTransform));
        frame.transform.SetParent(column.transform, false);

        var frameImage = frame.AddComponent<Image>();
        frameImage.color = frameColor;
        frameImage.raycastTarget = false;

        var frameElement = frame.AddComponent<LayoutElement>();
        frameElement.preferredWidth = badgeSize.x;
        frameElement.preferredHeight = badgeSize.y;
        frameElement.minWidth = badgeSize.x;
        frameElement.minHeight = badgeSize.y;

        var iconGo = new GameObject("Icon", typeof(RectTransform));
        var iconRect = (RectTransform)iconGo.transform;
        iconRect.SetParent(frame.transform, false);
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(iconPadding, iconPadding);
        iconRect.offsetMax = new Vector2(-iconPadding, -iconPadding);

        var iconImage = iconGo.AddComponent<Image>();
        iconImage.sprite = icon;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        TextMeshProUGUI mark = null;
        if (icon == null)
        {
            mark = UiViewBuilder.BuildLabel(
                frame.transform, "Mark", info.Mark, fontAsset, markFontSize, markColor);
            var markRect = (RectTransform)mark.transform;
            markRect.anchorMin = Vector2.zero;
            markRect.anchorMax = Vector2.one;
            markRect.offsetMin = Vector2.zero;
            markRect.offsetMax = Vector2.zero;
            mark.alignment = TextAlignmentOptions.Center;
        }

        TextMeshProUGUI caption = null;
        if (showCaption)
        {
            caption = UiViewBuilder.BuildLabel(
                column.transform, "Caption", ShortName(info), fontAsset, captionFontSize, captionColor);

            var captionElement = caption.gameObject.AddComponent<LayoutElement>();
            captionElement.preferredWidth = badgeSize.x;
            captionElement.preferredHeight = captionFontSize * 1.4f;
        }

        return new Badge { Info = info, Icon = iconImage, Mark = mark, Caption = caption };
    }

    private static string ShortName(AchievementInfo info)
    {
        if (info == null) return string.Empty;
        if (!info.Id.StartsWith("Ending")) return info.Title;

        return "엔딩 " + info.Id.Substring("Ending".Length);
    }
}

/* [파일 노트]
 *
 * 시작(Start) 화면 오른쪽 구석에 엔딩2·3·4 클리어 여부를 배지 3개로 보여 주는 표시 전담 뷰.
 * 상호작용이 전혀 없다 — Button 도 없고 모든 Graphic 이 raycastTarget=false 라
 * 타이틀 메뉴의 마우스 클릭·키보드 내비게이션을 절대 가로채지 않는다.
 * 이 프로젝트의 UI 관례대로 첫 Show() 때 UiViewBuilder 로 코드 생성한다(씬 배치 불필요).
 *
 * ── 어디서 만들어지고 언제 보이나 ────────────────────────────────────────────
 *   StartScene 의 Menu 상태가 ResolveBadgeView() 로 확보하고(씬에 배치본이 있으면 그것을,
 *   없으면 GameObject 를 하나 만들어 이 컴포넌트를 붙인다 — TitleMenuView 와 같은 폴백),
 *   타이틀 메뉴가 뜨는 DelayedCall(1) 안에서 함께 Show() 한다.
 *   즉 인트로 컷신 동안에는 보이지 않고, 메뉴와 함께 페이드인된다.
 *   sortingOrder 780 은 타이틀 메뉴(800)보다 아래, 페이드 스프라이트(1000)보다 아래라
 *   메뉴·옵션 패널과 겹치지 않으면서 페이드 연출을 그대로 따라간다.
 *
 * ── 배치 : 앵커 기반 (해상도·레터박스에 안전) ────────────────────────────────
 *   AspectRatioEnforcer 가 Overlay 캔버스를 ScreenSpaceCamera 로 바꾸고 카메라 rect 를
 *   16:9 로 좁히므로, 좌표를 절대값으로 잡으면 화면비가 바뀔 때 밀린다. 그래서
 *   cornerAnchor(기본 (1,1) = 오른쪽 위)로 anchorMin/Max/pivot 을 같은 지점에 몰아 두고
 *   cornerOffset 만큼만 안쪽으로 들여놓는다. 어떤 해상도에서도 오른쪽 구석에 붙는다.
 *   cornerAnchor 를 (1,0) 으로 바꾸면 오른쪽 아래로 내려가고, 레이아웃 정렬(UpperRight/
 *   LowerRight)도 y 값을 보고 함께 뒤집힌다. 캔버스는 UiViewBuilder.SetupOverlayCanvas 가
 *   Camera.main 에 묶으므로 AspectRatioEnforcer 의 sortingOrder +100 시프트 대상이 아니다
 *   (그 시프트는 Overlay 로 남아 있는 캔버스에만 걸린다 — 타이틀 메뉴와 조건이 같다).
 *
 * ── 계층 ─────────────────────────────────────────────────────────────────────
 *   EndingBadges (Canvas 780 + CanvasScaler 1920x1080)
 *     └ Badges (HorizontalLayoutGroup + ContentSizeFitter, 오른쪽 구석 앵커)
 *         └ Badge_Ending2 / Badge_Ending3 / Badge_Ending4 (VerticalLayoutGroup)
 *             ├ Frame  (Image, 반투명 검정 슬롯 72x72 — 미해금이어도 자리는 보인다)
 *             │   ├ Icon (Image, 전면 스트레치 - iconPadding)
 *             │   └ Mark (TMP, 아이콘 스프라이트가 없을 때만 생성 — "II"/"III"/"IV")
 *             └ Caption (TMP, "엔딩 2" 등 — showCaption 을 끄면 아이콘만 남는다)
 *   항목 구성은 AchievementCatalog.Endings 배열을 그대로 따른다. 엔딩1 은 게임에서
 *   제거되어 그 배열에 없으므로 배지도 3개다. 배열이 늘면 배지도 자동으로 늘어난다.
 *
 * ── 클리어 / 미클리어 표현 (2026-08-29 유저 요청: "미클리어 흐리고 어둡게") ──
 *   Refresh() 가 배지마다 AchievementCatalog.IsUnlocked 로 상태를 읽고 상태색을 곱한다.
 *     - 클리어  : clearedColor = (1, 1, 1, 1)          → 원래 색 그대로, 밝고 불투명
 *     - 미클리어: lockedColor  = (0.35, 0.35, 0.35, 0.5) → 명도 35% + 알파 50%
 *   RGB 를 함께 낮추므로 "어둡게", 알파를 낮추므로 배경에 묻혀 "흐리게" 보인다.
 *   진짜 블러(가우시안)는 별도 셰이더/RT 가 필요해 쓰지 않았다 — 이 프로젝트의 코드 생성
 *   UI 는 전부 스프라이트·셰이더 없는 플랫 구성이라 관례를 깨지 않는 쪽을 택했다.
 *   곱셈이라 아이콘 스프라이트를 꽂아도, 폴백 단색이어도 같은 규칙이 그대로 적용된다.
 *   Frame(슬롯 바탕)만 상태와 무관하게 고정이다 — 미해금 배지가 완전히 사라져 버리면
 *   "몇 개 중 몇 개를 모았는지"가 읽히지 않기 때문이다.
 *
 * ── 아이콘 (유저가 꽂을 자리) ────────────────────────────────────────────────
 *   icons 는 엔딩2·3·4 순서의 Sprite 배열이고 비워 두는 것이 기본값이다.
 *   프로젝트에 엔딩 전용 아이콘이 아직 없어서(Assets/GameAssets 전수 확인) 비어 있을 때는
 *   AchievementInfo.Tint 색 사각형 + Mark 로마숫자로 대신 그린다. 즉 아무것도 꽂지 않아도
 *   화면에 정상적으로 나온다. 나중에 아이콘이 생기면 인스펙터에서 3칸을 채우기만 하면 되고,
 *   그때는 Mark 글자가 생성되지 않고 스프라이트가 흰색 기준으로 밝기 조절된다.
 *   배열 길이가 3보다 짧거나 칸이 비어 있어도 그 항목만 폴백으로 떨어진다(ResolveIcon).
 *
 * ── 다시 그리는 시점 ─────────────────────────────────────────────────────────
 *   Show() 가 항상 Refresh() 를 부른다. Start 씬은 엔딩을 마치고 돌아오는 목적지라
 *   (Ending.cs 의 "시작 화면으로" → 업적 해금 → Start 로드) 씬에 들어올 때마다 다시 읽으면
 *   충분하고, 화면이 떠 있는 동안 업적이 바뀌는 경로는 없다. 매 프레임 폴링하지 않는다.
 */

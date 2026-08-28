using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TitleMenuView : MonoBehaviour
{
    [Header("글꼴")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private float itemFontSize = 34f;

    [Header("문구")]
    [SerializeField] private string newGameLabel = "게임 시작";
    [SerializeField] private string continueLabel = "이어하기";
    [SerializeField] private string optionsLabel = "옵션";
    [SerializeField] private string quitLabel = "게임 종료";

    [Header("색")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color hitAreaColor = new Color(1f, 1f, 1f, 0f);

    [Header("배치")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, -180f);
    [SerializeField] private Vector2 itemSize = new Vector2(420f, 54f);
    [SerializeField] private float spacing = 8f;
    [SerializeField] private TextAlignmentOptions itemAlignment = TextAlignmentOptions.Center;
    [SerializeField] private int sortingOrder = 800;

    [Header("조작")]
    [SerializeField] private bool keyboardNavigation = true;

    [Header("계층 (미리 배치한 경우 자동 연결)")]
    [SerializeField] private RectTransform itemsRoot;
    [SerializeField] private Button newGameItem;
    [SerializeField] private Button continueItem;
    [SerializeField] private Button optionsItem;
    [SerializeField] private Button quitItem;

    public const string ItemsRootName = "Items";
    public const string NewGameItemName = "NewGameItem";
    public const string ContinueItemName = "ContinueItem";
    public const string OptionsItemName = "OptionsItem";
    public const string QuitItemName = "QuitItem";
    public const string ItemLabelName = "Label";

    public event Action NewGameRequested;
    public event Action ContinueRequested;
    public event Action OptionsRequested;
    public event Action QuitRequested;

    private bool built;

    public void Show()
    {
        Show(false);
    }

    public void Show(bool canContinue)
    {
        EnsureBuilt();

        if (continueItem != null) continueItem.gameObject.SetActive(canContinue);

        gameObject.SetActive(true);

        ApplyNavigation();

        if (EventSystem.current == null) return;

        if (!keyboardNavigation)
        {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        Button first = newGameItem != null ? newGameItem : optionsItem;
        if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    private void ApplyNavigation()
    {
        if (!keyboardNavigation) return;

        var chain = new List<Selectable>();
        AddNavigable(chain, newGameItem);
        AddNavigable(chain, continueItem);
        AddNavigable(chain, optionsItem);
        AddNavigable(chain, quitItem);

        UiFocus.LinkVertical(true, chain.ToArray());
    }

    private static void AddNavigable(List<Selectable> chain, Button item)
    {
        if (item == null || !item.gameObject.activeSelf) return;
        chain.Add(item);
    }

    public void Hide()
    {
        ClearOwnSelection();
        gameObject.SetActive(false);
    }

    private void EnsureBuilt()
    {
        if (built) return;
        built = true;

        UiFocus.EnsureEventSystem();

        if (fontAsset == null) fontAsset = UiViewBuilder.FindFallbackFont(transform);

        UiViewBuilder.SetupOverlayCanvas(gameObject, sortingOrder);

        if (AdoptPlacedHierarchy())
        {
            AttachFocusKeeper();
            return;
        }

        RectTransform column = BuildColumn();

        newGameItem = BuildItem(column, NewGameItemName, newGameLabel, () => NewGameRequested?.Invoke());
        continueItem = BuildItem(column, ContinueItemName, continueLabel, () => ContinueRequested?.Invoke());
        optionsItem = BuildItem(column, OptionsItemName, optionsLabel, () => OptionsRequested?.Invoke());
        quitItem = BuildItem(column, QuitItemName, quitLabel, () => QuitRequested?.Invoke());

        AttachFocusKeeper();
    }

    private bool AdoptPlacedHierarchy()
    {
        if (itemsRoot == null)
        {
            Transform found = transform.Find(ItemsRootName);
            if (found != null) itemsRoot = found as RectTransform;
        }

        if (itemsRoot == null) return false;

        newGameItem = AdoptItem(newGameItem, NewGameItemName, () => NewGameRequested?.Invoke());
        continueItem = AdoptItem(continueItem, ContinueItemName, () => ContinueRequested?.Invoke());
        optionsItem = AdoptItem(optionsItem, OptionsItemName, () => OptionsRequested?.Invoke());
        quitItem = AdoptItem(quitItem, QuitItemName, () => QuitRequested?.Invoke());

        return true;
    }

    private Button AdoptItem(Button assigned, string name, Action callback)
    {
        Button item = assigned;

        if (item == null)
        {
            Transform found = itemsRoot.Find(name);
            if (found != null) item = found.GetComponent<Button>();
        }

        if (item == null) return null;

        item.onClick.AddListener(() =>
        {
            if (!keyboardNavigation && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            callback?.Invoke();
        });

        var label = item.GetComponentInChildren<TextMeshProUGUI>(true);
        var highlighter = item.GetComponent<TitleMenuItemHighlighter>();
        if (highlighter == null) highlighter = item.gameObject.AddComponent<TitleMenuItemHighlighter>();
        highlighter.Setup(label);

        return item;
    }

    private RectTransform BuildColumn()
    {
        var go = new GameObject(ItemsRootName, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
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

    private Button BuildItem(Transform parent, string name, string text, Action callback)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.color = hitAreaColor;
        image.raycastTarget = true;

        var element = go.AddComponent<LayoutElement>();
        element.preferredWidth = itemSize.x;
        element.preferredHeight = itemSize.y;

        var button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        button.onClick.AddListener(() =>
        {
            if (!keyboardNavigation && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            callback?.Invoke();
        });

        TextMeshProUGUI label = UiViewBuilder.BuildLabel(
            go.transform, ItemLabelName, text, fontAsset, itemFontSize, textColor);
        label.alignment = itemAlignment;

        var labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        go.AddComponent<TitleMenuItemHighlighter>().Setup(label);

        return button;
    }

    private void ClearOwnSelection()
    {
        if (EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return;
        if (!selected.transform.IsChildOf(transform)) return;

        EventSystem.current.SetSelectedGameObject(null);
    }

    private void AttachFocusKeeper()
    {
        if (!keyboardNavigation) return;

        UiFocus.AttachKeeper(
            gameObject, sortingOrder, newGameItem, continueItem, optionsItem, quitItem);
    }
}

/* [파일 노트]
 *
 * 타이틀(Start) 씬 메뉴의 "표시"만 담당하는 뷰. PauseMenuView / GameOverView 와 같은 관례이며
 * 로직(세이브 초기화, 씬 이동, 옵션 열기, 종료)은 전부 호출자인 StartScene 의 Menu 상태가 갖는다.
 * 이 컴포넌트는 Show()/Hide() 와 NewGame/Continue/Options/Quit 네 이벤트 발화만 한다.
 *
 * [미니멀 디자인 — 글씨만 보인다]
 * - Dim / 패널 / 버튼 배경이 전부 없다. 배경이 검은 씬 자체이므로 UiViewBuilder.BuildDim,
 *   BuildCenterPanel, BuildButton 은 쓰지 않고(전부 배경 Image 를 전제한다) 이 파일 안에서
 *   RectTransform + VerticalLayoutGroup 으로 직접 조립한다.
 *   UiViewBuilder 에서는 SetupOverlayCanvas / BuildLabel / FindFallbackFont 만 재사용한다.
 * - 클릭 판정은 살아 있어야 하므로 항목마다 Image 를 두되 알파 0(hitAreaColor) + raycastTarget=true 로
 *   둔다. Image 를 빼면 GraphicRaycaster 가 잡지 못해 클릭이 아예 안 된다.
 *   Button.transition 은 None 이다 — ColorTint 는 targetGraphic(투명 Image)에만 걸려 눈에 보이지 않는
 *   전환이라 켜 둘 이유가 없고, 글씨 강조는 아래 TitleMenuItemHighlighter 가 전담한다.
 * - 글씨는 흰색(textColor) 단색. 폰트를 비워 두면 씬의 기존 TMP 텍스트 폰트(한글 지원)를 물려받고,
 *   없으면 TMP 기본 폰트로 폴백한다(FindFallbackFont).
 *
 * [선택 볼드 — 키보드 조작 기준 (2026-08-28 유저 확정)]
 * 항목마다 TitleMenuItemHighlighter 를 붙여 "선택된 항목만" 글씨를 볼드로 바꾼다.
 * 마우스 호버 강조는 의도적으로 없앴다 — 이 메뉴의 의도된 조작은 키보드 방향키이고,
 * 호버 강조가 함께 있으면 방향키로 짚고 있는 항목이 어느 것인지 흐려지기 때문이다.
 * 마우스 클릭은 그대로 동작하며, 클릭하면 uGUI 가 그 버튼을 선택하므로 볼드가 자연히 따라온다.
 * 이 뷰 쪽 전제는 Button.transition 을 None 으로 둘 것(강조는 전부 하이라이터 담당) 하나다.
 * 구현 근거는 TitleMenuItemHighlighter.cs 의 파일 노트를 볼 것.
 *
 * [키보드 내비게이션 — keyboardNavigation]
 * 기본값 켜짐. Show 때 첫 항목을 선택하므로 방향키로 이동(볼드가 따라옴) + Enter 로 실행할 수 있다.
 * 끄면 Show 때 선택을 비우고 클릭할 때마다 다시 비운다(마우스 전용 모드). 이 경우 볼드는
 * 클릭 직후에만 잠깐 보이므로 사실상 강조가 없는 메뉴가 된다.
 *
 * [Navigation — Show 때마다 다시 잇는다 (2026-08-28)]
 * 예전에는 기본값 Automatic 에 맡겼다. Automatic 은 비활성 항목("이어하기"가 숨겨진 경우)을
 * 알아서 건너뛰지만, "씬 안의 모든 활성 Selectable" 중에서 방향으로 가장 가까운 것을 찾기 때문에
 * Start 씬에 아직 남아 있는 예전 버튼(Canvas 밑의 "Button (Legacy)" 2개, Start.cs 가 연결한
 * 것들 — 지워도 되는 잔재다)으로 방향키가 새어 나갈 수 있다. 그러면 화면에 볼드가 하나도 없는
 * 상태로 커서만 어딘가로 사라진 것처럼 보이고, 그 상태에서 Enter 를 누르면 엉뚱한 동작이 난다.
 * 그래서 Show 에서 ApplyNavigation 이 "그 순간 켜져 있는 항목만" 모아 UiFocus.LinkVertical 로
 * Explicit 순환 연결한다. 매번 다시 이으므로 canContinue 가 바뀌어도 막다른 길이 생기지 않고,
 * 좌/우는 끊겨 있으며, 마지막↔첫 항목이 순환한다.
 * (다른 뷰들은 항목 구성이 고정이라 EnsureBuilt 에서 한 번만 연결한다.)
 *
 * [2026-08-28] 공용 헬퍼로 정리
 * - 자체 EnsureEventSystem 을 UiFocus.EnsureEventSystem 으로 교체했다(구현은 동일 —
 *   씬에 EventSystem 이 없을 때만 StandaloneInputModule 로 생성).
 * - keyboardNavigation 이 켜져 있을 때만 UiFocusKeeper 를 붙인다. 두 입력 모듈 모두
 *   "선택 불가능한 곳을 클릭하면 선택 해제"라, 배경(글씨 바깥의 검은 화면)을 한 번 클릭하면
 *   볼드가 사라지고 방향키가 먹지 않게 되던 구멍을 막는다. 파수꾼은 선택이 비었을 때만
 *   개입하므로 클릭 조작을 방해하지 않는다.
 *   마우스 전용 모드(keyboardNavigation=false)에서는 붙이지 않는다 — 그 모드는 선택을
 *   일부러 비우는데 파수꾼이 곧바로 되살려 버리기 때문이다.
 *
 * [겹침 순서]
 * sortingOrder 800 은 PauseMenuView(900) / OptionsPanelView(910) 보다 아래다. Start 씬에서 ESC 나
 * "옵션"으로 옵션 패널이 열리면 그 패널의 Dim 이 이 메뉴를 덮어 클릭까지 막는다(의도된 동작).
 * 패널을 닫으면 이 메뉴가 그대로 다시 드러난다 — 별도 숨김/복원 처리가 필요 없다.
 *
 * [계층 확보 — 배치본 우선, 코드 생성은 폴백]
 * EnsureBuilt 는 두 갈래다.
 *   1) AdoptPlacedHierarchy : 자식으로 "Items"(itemsRoot)가 이미 있으면 그것을 그대로 쓴다.
 *      프리팹/씬에 구워 둔 계층이 이 경로다. 새로 만드는 오브젝트는 하나도 없고,
 *      항목 4개를 인스펙터 참조(없으면 이름으로 Find)로 찾아 onClick 리스너를 걸고
 *      TitleMenuItemHighlighter.Setup(label) 만 호출한다.
 *      → 문구·색·폰트·크기·위치는 배치본에 있는 값이 그대로 화면에 나온다(WYSIWYG).
 *        인스펙터 문자열/색 필드는 이 경로에서 다시 적용하지 않는다. 값을 바꾸려면
 *        오브젝트를 직접 고치거나, 필드를 고친 뒤 TitleMenuPrefabBuilder 를 다시 돌린다.
 *   2) 배치본이 없으면 예전처럼 코드로 만든다(BuildColumn/BuildItem). 이 폴백을 남겨 둔 이유는
 *      StartScene.Menu.ResolveMenuView 가 씬에서 뷰를 못 찾으면 빈 GameObject 에
 *      이 컴포넌트만 붙여서 만들기 때문이다 — 프리팹 배치를 깜빡해도 메뉴가 사라지지 않는다.
 * 어느 경로든 SetupOverlayCanvas 는 매번 호출한다. 캔버스를 ScreenSpaceCamera 로 Camera.main 에
 * 묶는 일은 씬 참조라 프리팹에 구워 둘 수 없고, 이 연결이 있어야 페이드 스프라이트(sortingOrder 1000)가
 * 메뉴 위를 덮어 "메뉴가 같이 서서히 드러나는" 연출이 유지된다.
 *
 * [폰트]
 * fontAsset 이 비면 FindFallbackFont 가 씬의 아무 TMP 텍스트 폰트나 물려받는데, Start 씬에는
 * LiberationSans(한글 글리프 없음) 텍스트가 섞여 있어 한글이 □ 로 깨질 수 있다.
 * 그래서 배치본에는 TitleMenuPrefabBuilder 가 PRETENDARD-REGULAR SDF 를 명시적으로 박아 둔다.
 * 이 필드를 비워 두지 말 것.
 *
 * [교체 방법]
 * StartScene 의 Menu 상태는 씬에 배치된 TitleMenuView 를 먼저 찾고 없을 때만 코드로 만든다
 * (PauseManager 가 PauseMenuView 를 찾는 방식과 동일). 표준 배치본은
 * Assets/Prefabs/TitleMenu.prefab 이며 Tools / Tup3 / Build Title Menu 로 만들고 갱신한다.
 */

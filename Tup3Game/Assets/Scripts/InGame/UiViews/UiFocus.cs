using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UiFocus
{
    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    public static UiFocusKeeper AttachKeeper(GameObject root, int priority, params Selectable[] items)
    {
        if (root == null) return null;

        var keeper = root.GetComponent<UiFocusKeeper>();
        if (keeper == null) keeper = root.AddComponent<UiFocusKeeper>();

        keeper.Setup(priority, items);
        return keeper;
    }

    public static GameObject Focus(Transform owner, UiFocusKeeper keeper)
    {
        EnsureEventSystem();

        EventSystem system = EventSystem.current;
        if (system == null) return null;

        GameObject outside = system.currentSelectedGameObject;
        if (outside != null && IsOwnedBy(owner, outside)) outside = null;

        GameObject target = keeper != null ? keeper.Preferred : null;
        if (target != null) system.SetSelectedGameObject(target);

        return outside;
    }

    public static void Blur(Transform owner, GameObject outside)
    {
        EventSystem system = EventSystem.current;
        if (system == null) return;

        GameObject inside = system.currentSelectedGameObject;
        bool owns = inside != null && IsOwnedBy(owner, inside);

        if (inside != null && !owns) return;

        if (outside != null && outside.activeInHierarchy) system.SetSelectedGameObject(outside);
        else if (owns) system.SetSelectedGameObject(null);
    }

    public static void Select(UiFocusKeeper keeper)
    {
        EnsureEventSystem();

        EventSystem system = EventSystem.current;
        if (system == null || keeper == null) return;

        GameObject target = keeper.Preferred;
        if (target != null) system.SetSelectedGameObject(target);
    }

    public static void Clear(UiFocusKeeper keeper)
    {
        EventSystem system = EventSystem.current;
        if (system == null || keeper == null) return;

        GameObject current = system.currentSelectedGameObject;
        if (current == null || !keeper.Owns(current)) return;

        system.SetSelectedGameObject(null);
    }

    public static void LinkVertical(bool wrap, params Selectable[] items)
    {
        var chain = new List<Selectable>();
        for (int i = 0; i < items.Length; i++)
            if (items[i] != null) chain.Add(items[i]);

        int count = chain.Count;

        for (int i = 0; i < count; i++)
        {
            Navigation nav = chain[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnLeft = null;
            nav.selectOnRight = null;
            nav.selectOnUp = i > 0 ? chain[i - 1] : (wrap && count > 1 ? chain[count - 1] : null);
            nav.selectOnDown = i < count - 1 ? chain[i + 1] : (wrap && count > 1 ? chain[0] : null);
            chain[i].navigation = nav;
        }
    }

    public static bool IsOwnedBy(Transform owner, GameObject candidate)
    {
        if (owner == null || candidate == null) return false;
        return candidate.transform.IsChildOf(owner);
    }
}

/* [파일 노트]
 *
 * 코드 생성 UI 뷰들(PauseMenuView / OptionsPanelView / GameOverView / ConfirmDialogView /
 * TitleMenuView / Ending 의 복귀 버튼)이 공유하는 "키보드 포커스" 정적 헬퍼.
 * 같은 코드(EventSystem 확보 → 첫 항목 선택 → 닫을 때 원래 선택 복원 → 방향키 연결)를
 * 여섯 군데서 반복하게 되어 한곳으로 뽑았다. 상태는 없고 전부 정적 메서드다.
 *
 * ── EnsureEventSystem ────────────────────────────────────────────────────────
 *   씬에 EventSystem 이 하나도 없을 때만 StandaloneInputModule 로 만든다.
 *   PauseManager.EnsureEventSystem / TitleMenuView.EnsureEventSystem 과 완전히 같은 구현이며
 *   그 둘도 이제 이 함수를 호출한다(중복 제거).
 *   구 모듈을 쓰는 이유는 이 프로젝트의 씬 구성이 섞여 있기 때문이다 —
 *   Start / Lobby / Loading / Test 는 InputSystemUIInputModule(신), Ending1~4 는
 *   StandaloneInputModule(구), Boss_Fire/Gold/Soil/Water · Styx · Prologue 는 EventSystem 자체가 없다.
 *   ProjectSettings 의 activeInputHandler 가 2(Both)라 구/신 어느 쪽이든 동작하고,
 *   InputManager.asset 에 Horizontal(←/→) · Vertical(↑/↓/W/S) · Submit(Return/Enter/Space) ·
 *   Cancel(Escape) 축이 전부 정의돼 있어 구 모듈로 만들어도 방향키 내비게이션이 성립한다.
 *   신 모듈로 만들려면 액션 에셋 참조가 필요해 코드 생성에는 부적합하다(씬 배치본이 맡는다).
 *
 * ── Focus / Blur : 모달 겹침에서 선택을 잃지 않기 위한 짝 ────────────────────
 *   Focus(owner, keeper)
 *     - 지금 선택된 오브젝트가 owner 바깥의 UI 라면 그것을 반환한다(= 나중에 되돌릴 대상).
 *     - keeper.Preferred(그 뷰에서 마지막으로 짚고 있던 항목, 없으면 첫 항목)를 선택한다.
 *     - 반드시 gameObject.SetActive(true) 뒤에 불러야 한다. 비활성 오브젝트는 선택되지 않는다.
 *   Blur(owner, outside)
 *     - 지금 선택이 owner 바깥이면(= 이미 다른 UI 가 포커스를 가져갔으면) 아무것도 하지 않는다.
 *       위에 뜬 모달이 먼저 닫히고 나중에 아래 모달이 닫히는 역순 상황에서 남의 선택을 뺏지 않기 위함.
 *     - 그렇지 않으면 Focus 가 돌려준 outside 로 되돌리고, 없으면 선택을 비운다.
 *   덕분에 "일시정지 메뉴 → 옵션" / "엔딩 버튼 → 확인창" / "패배 UI 위에서 ESC 로 일시정지"
 *   같은 겹침에서 Enter 가 뒤쪽 UI 의 버튼을 누르는 사고가 나지 않는다.
 *
 * ── LinkVertical : 왜 Automatic 이 아니라 Explicit 인가 ──────────────────────
 *   Navigation.Mode.Automatic 은 "씬 안의 모든 활성 Selectable" 중에서 방향으로 가장 가까운 것을
 *   찾는다. 이 프로젝트는 모달이 떠도 아래쪽 UI 를 SetActive(false) 하지 않는 경우가 많아
 *   (Start 씬의 타이틀 메뉴 위에 옵션 패널, 엔딩 버튼 위에 확인창) 방향키가 Dim 을 뚫고
 *   뒤쪽 UI 로 새어 나간다. 그래서 세로 목록은 전부 Explicit 로 위/아래만 직접 연결하고
 *   좌/우는 null 로 끊는다. 마지막↔첫 항목 순환(wrap)은 항목이 2~3개뿐이라 되감기가 짧고
 *   "끝에서 한 번 더 눌렀는데 아무 반응이 없다"는 느낌을 없애 주므로 전부 켠다.
 *   주의 : Explicit 는 비활성 항목을 자동으로 건너뛰지 못한다(막다른 길이 된다).
 *   항목이 조건부로 숨겨지는 목록(TitleMenuView 의 "이어하기")에는 쓰지 말 것 — 그쪽은 Automatic 유지.
 *
 *   Slider 에 써도 안전하다. uGUI Slider 는 좌/우 이동을 받았을 때
 *   FindSelectableOnLeft/Right() 가 null 이면 값 조절로 처리하는데, Explicit + selectOnLeft=null 이면
 *   그 조건이 그대로 성립한다. 즉 좌우 = 값 조절, 상하 = 항목 이동이 보장된다.
 *
 * ── Select / Clear ───────────────────────────────────────────────────────────
 *   Show/Hide 짝이 없는 곳(Ending 은 버튼 2개를 SetActive 로만 켜고 끈다)에서 쓰는 단발 함수.
 */

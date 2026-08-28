using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UiFocusKeeper : MonoBehaviour
{
    private static readonly List<UiFocusKeeper> Active = new();

    private readonly List<Selectable> items = new();
    private int priority;
    private GameObject last;

    public void Setup(int order, params Selectable[] targets)
    {
        priority = order;

        items.Clear();
        if (targets == null) return;

        for (int i = 0; i < targets.Length; i++)
            if (targets[i] != null) items.Add(targets[i]);
    }

    public GameObject Preferred
    {
        get
        {
            if (IsUsable(last)) return last;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null) continue;
                if (IsUsable(items[i].gameObject)) return items[i].gameObject;
            }

            return null;
        }
    }

    public bool Owns(GameObject candidate)
    {
        if (candidate == null) return false;

        for (int i = 0; i < items.Count; i++)
            if (items[i] != null && items[i].gameObject == candidate) return true;

        return false;
    }

    private void OnEnable()
    {
        if (!Active.Contains(this)) Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    private void LateUpdate()
    {
        EventSystem system = EventSystem.current;
        if (system == null) return;

        GameObject current = system.currentSelectedGameObject;

        if (current != null)
        {
            if (Owns(current)) last = current;
            return;
        }

        if (!IsTopMost()) return;

        GameObject target = Preferred;
        if (target != null) system.SetSelectedGameObject(target);
    }

    private bool IsTopMost()
    {
        for (int i = 0; i < Active.Count; i++)
        {
            UiFocusKeeper other = Active[i];
            if (other == null || other == this) continue;
            if (other.priority <= priority) continue;
            if (other.Preferred == null) continue;

            return false;
        }

        return true;
    }

    private static bool IsUsable(GameObject candidate)
    {
        if (candidate == null || !candidate.activeInHierarchy) return false;

        var selectable = candidate.GetComponent<Selectable>();
        return selectable != null && selectable.IsInteractable();
    }
}

/* [파일 노트]
 *
 * "열려 있는 UI 는 항상 선택된 항목을 하나 갖는다"를 보장하는 작은 파수꾼 컴포넌트.
 * UiFocus.AttachKeeper(뷰 루트, sortingOrder, 항목들) 로 뷰 루트에 붙는다.
 *
 * ── 왜 필요한가 ──────────────────────────────────────────────────────────────
 *   두 입력 모듈 모두 "선택 불가능한 곳을 클릭하면 선택을 해제"한다
 *   (InputSystemUIInputModule 의 m_DeselectOnBackgroundClick=1, StandaloneInputModule 도 동일 동작).
 *   이 프로젝트의 모달은 전부 Dim(raycastTarget=true)을 깔기 때문에, 유저가 버튼 바깥의 어두운
 *   부분을 한 번 클릭하면 선택이 사라지고 그 뒤로는 방향키를 눌러도 아무 일도 일어나지 않는다
 *   (uGUI 내비게이션은 "현재 선택"에서 출발하므로 선택이 없으면 출발점이 없다).
 *   마우스와 키보드를 섞어 쓸 수 있어야 한다는 요구사항 때문에 이 구멍은 반드시 막아야 한다.
 *
 * ── 동작 ─────────────────────────────────────────────────────────────────────
 *   LateUpdate 에서
 *     - 선택이 있고 그것이 내 항목이면 last 로 기억한다(다음에 이 뷰가 다시 열릴 때 그 자리로 복귀).
 *     - 선택이 null 이고 내가 최상위(topmost)면 last(없으면 첫 항목)를 다시 선택한다.
 *   선택이 null 일 때만 개입하므로 다른 뷰가 포커스를 가져간 상태를 빼앗지 않는다.
 *   즉 모달이 겹쳐도 서로 싸우지 않고, 위 모달이 닫히면서 UiFocus.Blur 가 아래 모달의 항목으로
 *   선택을 되돌려 주면 그 시점부터 아래 모달의 파수꾼이 다시 기억을 이어 간다.
 *
 * ── 최상위 판정(priority) ────────────────────────────────────────────────────
 *   priority 는 각 뷰의 Canvas sortingOrder 를 그대로 쓴다
 *   (TitleMenu 800 < CreditsView 800 < PauseMenu 900 < OptionsPanel 910 < Ending 복귀 920
 *    < GameOver 950 < ConfirmDialog 980).
 *   선택이 비었을 때 지금 화면에 떠 있는 파수꾼 중 가장 위 것만 선택을 가져가므로
 *   "패배 UI 위에 일시정지" 같은 겹침에서도 되찾아 오는 대상이 흔들리지 않는다.
 *   Preferred 가 null 인 파수꾼(항목이 전부 비활성 — 예: 크레딧이 흐르는 동안의 엔딩 버튼)은
 *   판정에서 빠지므로 아래쪽 UI 를 부당하게 막지 않는다.
 *
 * ── 쓰지 않는 곳 ─────────────────────────────────────────────────────────────
 *   TitleMenuView 는 keyboardNavigation 이 꺼진 마우스 전용 모드에서 의도적으로 선택을 비우므로
 *   그 모드에서는 붙이지 않는다(파수꾼이 곧바로 되살려 버리기 때문).
 */

using System;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class UiCancelRelay : MonoBehaviour, ICancelHandler
{
    private Action callback;

    public void Setup(Action action)
    {
        callback = action;
    }

    public void OnCancel(BaseEventData eventData)
    {
        callback?.Invoke();
    }
}

/* [파일 노트]
 *
 * "이 항목이 선택된 상태에서 취소 키를 누르면 이 함수를 부른다"만 하는 최소 중계 컴포넌트.
 *
 * ── 왜 버튼마다 붙이는가 ─────────────────────────────────────────────────────
 *   두 입력 모듈 모두 취소 이벤트를 ExecuteEvents.Execute(currentSelectedGameObject, ...) 로
 *   "지금 선택된 오브젝트 하나"에만 보낸다(ExecuteHierarchy 가 아니다). 즉 뷰 루트에 붙이면
 *   영원히 호출되지 않는다. 그래서 선택 대상이 되는 버튼마다 하나씩 붙여 같은 콜백을 물린다.
 *
 * ── 어디에 쓰는가 (중요) ─────────────────────────────────────────────────────
 *   현재 유일한 사용처는 ConfirmDialogView 다. PauseMenuView / OptionsPanelView 에는
 *   절대 붙이지 말 것 — 그쪽 ESC 는 PauseManager.HandleEscape(구 Input 의 KeyCode.Escape)가
 *   이미 라우팅하므로, 취소 이벤트까지 받으면 ESC 한 번에 두 단계가 닫힌다.
 *   ConfirmDialogView 가 예외인 이유와 중복 방지 방법은 그 파일의 노트를 볼 것.
 */

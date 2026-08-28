using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class TitleMenuItemHighlighter : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private TextMeshProUGUI label;
    private FontStyles baseStyle;
    private bool selected;

    public void Setup(TextMeshProUGUI target)
    {
        label = target;
        if (label != null) baseStyle = label.fontStyle;
        Apply();
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        Apply();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        Apply();
    }

    private void OnDisable()
    {
        selected = false;
        Apply();
    }

    private void Apply()
    {
        if (label == null) return;
        label.fontStyle = selected ? baseStyle | FontStyles.Bold : baseStyle;
    }
}

/* [파일 노트]
 *
 * TitleMenuView 의 항목 하나에 붙어 "선택된 글씨만 볼드"를 만드는 아주 작은 컴포넌트.
 * 버튼 오브젝트에 AddComponent 된 뒤 Setup(label) 로 대상 TMP 라벨을 받는다.
 *
 * [볼드 = 키보드 선택 피드백 (2026-08-28 유저 확정)]
 * 볼드는 오직 EventSystem 의 선택 상태(ISelectHandler/IDeselectHandler)에만 반응한다.
 * 마우스 호버(IPointerEnterHandler/IPointerExitHandler)는 의도적으로 구현하지 않는다 —
 * 이 메뉴의 의도된 조작은 키보드 방향키이고, 호버 강조가 함께 있으면 "지금 방향키로 짚고 있는 항목"이
 * 어느 것인지 흐려지기 때문이다. 마우스로 클릭하는 것은 그대로 가능하며(Button 은 살아 있다),
 * 클릭하면 uGUI 가 그 버튼을 선택 상태로 만들므로 자연히 볼드가 따라온다.
 *
 * [왜 Button.transition 이 아닌가]
 * uGUI Button 의 Transition(ColorTint/SpriteSwap/Animation)으로는 폰트 스타일 자체를 바꿀 수 없다.
 * 그래서 TitleMenuView 는 Button.transition 을 None 으로 두고 강조를 전부 이 컴포넌트에 맡긴다.
 * EventTrigger 를 쓰지 않는 이유도 같다 — EventTrigger 는 포인터 계열 UnityEvent 도구라
 * Select/Deselect 를 다루지 못한다.
 *
 * [상태 보존]
 * 원래 fontStyle 을 baseStyle 로 기억한 뒤 Bold 비트만 더했다 빼므로, 씬/프리팹에 미리 배치해
 * 다른 스타일(예: Italic)을 준 경우에도 그 스타일이 보존된다.
 * OnDisable 에서 상태를 되돌리므로 메뉴가 닫힐 때 볼드인 채로 굳는 잔상이 남지 않는다.
 */

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class TitleMenuItemHighlighter : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private TextMeshProUGUI label;
    private FontStyles baseStyle;
    private bool hovered;
    private bool selected;

    public void Setup(TextMeshProUGUI target)
    {
        label = target;
        if (label != null) baseStyle = label.fontStyle;
        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        Apply();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
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
        hovered = false;
        selected = false;
        Apply();
    }

    private void Apply()
    {
        if (label == null) return;
        label.fontStyle = hovered || selected ? baseStyle | FontStyles.Bold : baseStyle;
    }
}

/* [파일 노트]
 *
 * TitleMenuView 의 항목 하나에 붙어 "짚은 글씨만 볼드"를 만드는 아주 작은 컴포넌트.
 * 버튼 오브젝트에 AddComponent 된 뒤 Setup(label) 로 대상 TMP 라벨을 받는다.
 *
 * [왜 EventTrigger 가 아니라 인터페이스 직접 구현인가]
 * 1) EventTrigger 는 포인터 계열 이벤트를 UnityEvent 로 노출하는 도구다. 키보드/게임패드로 항목을
 *    옮길 때 오는 것은 포인터 이벤트가 아니라 Select/Deselect 라, EventTrigger 만으로는
 *    "선택된 항목이 볼드"가 되지 않는다. ISelectHandler/IDeselectHandler 를 함께 구현해야
 *    마우스와 키보드가 같은 규칙으로 동작한다.
 * 2) EventTrigger 는 자기 오브젝트의 이벤트를 통째로 받아 처리하는 성격이라 같은 오브젝트의 Button 과
 *    겹칠 때 예기치 않은 부작용을 만들 수 있다. 필요한 네 개만 구현하는 편이 안전하고 가볍다.
 * 3) uGUI Button 의 Transition(ColorTint/SpriteSwap/Animation)으로는 폰트 스타일 자체를 바꿀 수 없다.
 *    그래서 TitleMenuView 는 Button.transition 을 None 으로 두고 강조를 전부 이 컴포넌트에 맡긴다.
 *
 * [상태 합성]
 * hovered 와 selected 를 따로 들고 "둘 중 하나라도 참이면 볼드"로 합친다. 마우스로 짚은 항목을
 * 키보드로 옮기거나 그 반대 상황에서도 볼드가 두 곳에 남거나 사라지지 않는다.
 * 원래 fontStyle 을 baseStyle 로 기억한 뒤 Bold 비트만 더했다 빼므로, 씬에 미리 배치해 다른 스타일
 * (예: Italic)을 준 경우에도 그 스타일이 보존된다.
 * OnDisable 에서 상태를 되돌리므로 메뉴가 닫힐 때 볼드인 채로 굳는 잔상이 남지 않는다
 * (오브젝트가 꺼지면 OnPointerExit 이 오지 않기 때문에 필요한 처리다).
 */

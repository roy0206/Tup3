using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UiSelectionTint : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private readonly List<Graphic> targets = new();
    private Color normalColor = Color.white;
    private Color selectedColor = Color.white;
    private bool isSelected;

    public void Setup(Color normal, Color selected, params Graphic[] graphics)
    {
        normalColor = normal;
        selectedColor = selected;

        targets.Clear();
        if (graphics != null)
        {
            for (int i = 0; i < graphics.Length; i++)
                if (graphics[i] != null) targets.Add(graphics[i]);
        }

        Apply();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        Apply();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        Apply();
    }

    private void OnDisable()
    {
        isSelected = false;
        Apply();
    }

    private void Apply()
    {
        Color color = isSelected ? selectedColor : normalColor;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;
            targets[i].color = color;
        }
    }
}

/* [파일 노트]
 *
 * "선택된 동안 지정한 Graphic 들의 색을 바꾼다"만 하는 최소 컴포넌트.
 * TitleMenuItemHighlighter 와 같은 발상(EventSystem 의 ISelectHandler/IDeselectHandler 에만 반응,
 * 마우스 호버는 의도적으로 다루지 않음)이지만, 그쪽은 TMP 폰트 스타일(볼드)을 바꾸는 전용이고
 * 이쪽은 임의의 Graphic 색을 바꾸는 범용이라 별도로 둔다.
 *
 * ── 왜 Button 의 ColorTint 로 안 되는가 ──────────────────────────────────────
 *   Selectable 의 ColorTint 전환은 targetGraphic "하나"에만 걸린다. 현재 유일한 사용처인
 *   OptionsPanelView 의 볼륨 슬라이더는 targetGraphic 이 핸들이고, 핸들은 골드색 Fill 위에
 *   올라앉아 있어 색을 바꿔도 채움 막대와 뭉개져 "지금 이 슬라이더가 선택돼 있다"가 읽히지 않는다.
 *   그래서 슬라이더 오브젝트에 이 컴포넌트를 붙여 같은 행의 이름표("배경음"/"효과음")와
 *   값 표시("70%")를 함께 강조색으로 물들인다 — 슬라이더 자체가 아니라 행 전체가 켜지는 느낌이라
 *   버튼의 배경 밝아짐과 대등한 강도의 신호가 된다.
 *
 * ── 상태 ─────────────────────────────────────────────────────────────────────
 *   Setup 이 normal/selected 두 색을 받아 두고 선택 상태에 따라 그대로 덮어쓴다.
 *   OnDisable 에서 normal 로 되돌리므로 패널이 닫힐 때 강조색으로 굳는 잔상이 없다.
 */

using UnityEngine;

public class LobbyDialogueInteraction : InteractionBase, ILobbyIntroStep
{
    [Header("도입부")]
    [SerializeField] private int stepOrder;

    [Header("기즈모")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.85f, 0.2f, 1f);

    private bool consumed;

    public int StepOrder => stepOrder;

    protected override bool CanInteract()
    {
        if (consumed) return false;
        LobbyIntroDirector director = LobbyIntroDirector.Current;
        return director != null && director.IsNextStep(this);
    }

    public override bool OnInteract()
    {
        if (!base.OnInteract()) return false;

        LobbyIntroDirector director = LobbyIntroDirector.Current;
        if (director == null || !director.TryFire(this)) return false;

        consumed = true;
        ShutDown();
        return true;
    }

    public void OnIntroDisabled()
    {
        consumed = true;
        ShutDown();
    }

    private void ShutDown()
    {
        if (InteractionManager.Current != null) InteractionManager.Current.Unregister(this);
        enabled = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.35f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(interactionDistance, 0.1f));

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, $"상호작용 대사 #{stepOrder}");
#endif
    }
}

/* [파일 노트]
 * 기존 상호작용 시스템(InteractionBase / InteractionManager / InteractionView) 위에 얹은 도입부 대사 트리거.
 * 플레이어가 오브젝트 근처에서 V 키를 interactionDuration 만큼 홀드하면 다음 대사가 진행된다.
 *
 * - 발동 판정은 위치형 트리거(LobbyDialogueZone)와 완전히 동일하게 LobbyIntroDirector 가 담당한다.
 *   CanInteract() 에서 IsNextStep() 을 확인하므로 자기 순번이 아니면 홀드를 채워도 실패 사운드만 나고 진행되지 않는다.
 * - 성공하면 InteractionManager 에서 스스로 Unregister 해서 one-shot 이 된다.
 *   (InteractionBase 의 interactOnce 옵션과 기능이 겹치므로 인스펙터에서는 그 값을 신경 쓰지 않아도 된다.)
 * - InteractionBase 의 Start() 는 private 이라 파생 클래스에서 Start 를 다시 선언하면 등록이 날아간다.
 *   그래서 이 클래스에는 Start/Update 를 두지 않았다. 초기화가 더 필요하면 Awake 를 쓸 것.
 * - InteractionManager 는 거리만 보고 InteractionView 프롬프트를 띄우기 때문에,
 *   자기 순번이 아닐 때도 V 프롬프트 자체는 표시된다. 이게 거슬리면 InteractionManager 쪽에
 *   "상호작용 가능 여부" 질의를 추가해야 하는데, 공용 파일이라 여기서는 건드리지 않았다.
 */

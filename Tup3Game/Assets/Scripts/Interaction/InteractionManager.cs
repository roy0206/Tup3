using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class InteractionManager : DomainSingleton<InteractionManager>
{
    private List<InteractionBase> interactionObjects = new List<InteractionBase>();
    private Playermovement player;
    private InteractionView view;
    

    private InteractionBase currentInteraction = null;

    private Action heldCallback;
    private Action upCallback;

    private void Start()
    {
        player = FindFirstObjectByType<Playermovement>();
        if (player == null)
        {
            Debug.LogError("No Playermovement component found on this gameobject");
        }

        // 익명 람다로 등록하면 해제할 수 없으므로 델리게이트를 들고 있다가 OnDestroy 에서 뺀다.
        // UnityEngine.Object 의 == 오버로드를 타야 파괴된 인스턴스를 걸러낼 수 있으므로 ?. 는 쓰지 않는다.
        heldCallback = () => { if (currentInteraction != null) currentInteraction.OnInteract(); };
        upCallback = () => { if (currentInteraction != null) currentInteraction.OnHoldUP(); };
        UserInput.Instance.AddKeyListener(KeyCode.V, KeyPhase.Held, heldCallback);
        UserInput.Instance.AddKeyListener(KeyCode.V, KeyPhase.Up, upCallback);
        view = FindAnyObjectByType<InteractionView>(FindObjectsInactive.Include);
        if (view != null) view.Disable();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // UserInput 은 DontDestroyOnLoad 라 씬이 바뀌어도 살아 있다. 여기서 리스너를 빼지 않으면
        // 다음 씬에서 V 를 눌렀을 때 파괴된 이전 씬의 InteractionView(Image) 를 건드려
        // MissingReferenceException 이 난다.
        if (UserInput.Instance != null)
        {
            UserInput.Instance.RemoveKeyListener(KeyCode.V, KeyPhase.Held, heldCallback);
            UserInput.Instance.RemoveKeyListener(KeyCode.V, KeyPhase.Up, upCallback);
        }
    }

    private void FixedUpdate()
    {
        if (player == null || view == null) return;

        var nearInteractions =
            interactionObjects.OrderBy(x => (player.transform.position - x.transform.position).sqrMagnitude);
        currentInteraction = null;
        foreach (var interaction in nearInteractions)
        {
            if (!interaction.IsInteractionVisible) continue; // 잠긴 상호작용은 UI 자체를 띄우지 않는다

            if ((player.transform.position - interaction.transform.position).magnitude <=
                interaction.InteractionDistance)
            {
                currentInteraction = interaction;
                break;
            }
        }
        if(currentInteraction == null)
        {
            view.Disable();
            return;
        }

        view.Enable();
        view.SetPosition(currentInteraction.transform.position);
    }

    public bool Register(InteractionBase interactionBase)
    {
        if (interactionObjects.Contains(interactionBase))
        {
            Debug.LogWarning("Interaction object already exists " + interactionBase.name);
            return false;
        }
        interactionObjects.Add(interactionBase);
        return true;
    }

    public bool Unregister(InteractionBase interactionBase)
    {
        return interactionObjects.Remove(interactionBase);
    }
    
}

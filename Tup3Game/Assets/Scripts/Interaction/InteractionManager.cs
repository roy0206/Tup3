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
    private void Start()
    {
        player = FindFirstObjectByType<Playermovement>();
        if (player == null)
        {
            Debug.LogError("No Playermovement component found on this gameobject");
        }
        
        UserInput.Instance.AddKeyListener(KeyCode.V, KeyPhase.Held, ()=>
        {
            currentInteraction?.OnInteract();
        });
        UserInput.Instance.AddKeyListener(KeyCode.V, KeyPhase.Up, ()=>
        {
            currentInteraction?.OnHoldUP();
        });
        view = FindAnyObjectByType<InteractionView>(FindObjectsInactive.Include);
        if (view != null) view.Disable();
    }

    private void FixedUpdate()
    {
        if (player == null || view == null) return;

        var nearInteractions =
            interactionObjects.OrderBy(x => (player.transform.position - x.transform.position).sqrMagnitude);
        currentInteraction = null;
        foreach (var interaction in nearInteractions)
        {
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

using UnityEngine;
using UnityEngine.Events;

public class CustomInteraction : InteractionBase
{
    [SerializeField] private UnityEvent interactionBehavior;
    public UnityEvent InteractionBehavior => interactionBehavior;
    protected override bool CanInteract()
    {
        return true;
    }

    public override bool OnInteract()
    {
        if (base.OnInteract())
        {
            interactionBehavior.Invoke();
            return true;
        } 
        return false;
    }
}

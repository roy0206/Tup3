using UnityEngine;
using System.Collections.Generic;
public class Torch : InteractionBase
{
    protected override bool CanInteract()
    {
        return true;
    }

    public override bool OnInteract()
    {
        if (base.OnInteract())
        {
            Debug.Log("Touch Interact");
            return true;
        }

        return false;
    }
}

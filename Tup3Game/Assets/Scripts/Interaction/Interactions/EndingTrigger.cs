using UnityEngine;
using System.Collections.Generic;
public class EndingTrigger : InteractionBase
{
    protected override bool CanInteract()
    {
        return true;
    }

    public override bool OnInteract()
    {
        if (base.OnInteract())
        {
            SceneController.Instance.LoadScene("Ending");
            return true;
        }

        return false;
    }
}

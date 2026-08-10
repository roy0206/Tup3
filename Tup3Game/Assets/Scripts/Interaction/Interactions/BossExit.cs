using UnityEngine;
using System.Collections.Generic;
public class BossExit : InteractionBase
{
    protected override bool CanInteract()
    {
        return true;
    }

    public override bool OnInteract()
    {
        if (base.OnInteract())
        {
            SceneController.Instance.LoadScene("Lobby");
            return true;
        }

        return false;
    }
}

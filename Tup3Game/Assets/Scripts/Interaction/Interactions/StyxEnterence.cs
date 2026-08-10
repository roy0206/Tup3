using UnityEngine;
using System;
using System.Collections.Generic;
public class StyxEnterence : InteractionBase, ISceneEventListener
{

    private void Awake()
    {
        SceneController.Instance.RegisterListener(this);
    }

    protected override bool CanInteract()
    {
        return (int)UserDataManager.Instance.Data.Play.clearedBosses == 15;
    }

    public override bool OnInteract()
    {
        if (base.OnInteract())
        {
            SceneController.Instance.LoadScene("Styx");
            return true;
        }
        

        return false;
    }

    public void OnSceneLoadComplete(string sceneName)
    {
        /*if (UserDataManager.Instance.Data.Play.clearedBosses.HasFlag(boss))
        {
            InteractionManager.Current.Unregister(this);
        }*/
    }

    public void OnSceneExit(string sceneName)
    {
        SceneController.Instance.UnregisterListener(this);
    }
}

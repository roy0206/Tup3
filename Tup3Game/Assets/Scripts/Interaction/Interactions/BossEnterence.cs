using UnityEngine;
using System;
using System.Collections.Generic;
public class BossEnterence : InteractionBase, ISceneEventListener
{
    [SerializeField] private BossFlag boss;

    private void Awake()
    {
        SceneController.Instance.RegisterListener(this);
    }

    protected override bool CanInteract()
    {
        return true;
    }

    public override bool OnInteract()
    {
        if (base.OnInteract())
        {
            SceneController.Instance.LoadScene("Boss_"+boss.ToString());
            return true;
        }

        return false;
    }

    public void OnSceneLoadComplete(string sceneName)
    {
        if (UserDataManager.Instance.Data.Play.clearedBosses.HasFlag(boss))
        {
            InteractionManager.Current.Unregister(this);
        }
    }

    public void OnSceneExit(string sceneName)
    {
        SceneController.Instance.UnregisterListener(this);
    }
}

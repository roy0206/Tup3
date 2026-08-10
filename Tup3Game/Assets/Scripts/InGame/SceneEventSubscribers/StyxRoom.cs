using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class StyxRoom : MonoBehaviour, ISceneEventListener
{
    private Playermovement player;

    public void OnSceneLoadComplete(string sceneName)
    {
        player = FindObjectOfType<Playermovement>();
        
    }

    public void OnSceneExit(string sceneName)
    {
        UserDataManager.Instance.SaveAsync();
        SceneController.Instance.UnregisterListener(this);
    }

    private void Awake()
    {
        SceneController.Instance.RegisterListener(this);
    }
}

using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Lobby : MonoBehaviour, ISceneEventListener
{
    private Playermovement player;
    public void OnSceneLoadStart(string sceneName)
    {
        
    }

    public void OnSceneLoadComplete(string sceneName)
    {
        UserDataManager.Instance.LoadAsync();
        player = FindObjectOfType<Playermovement>();
        player.transform.position = UserDataManager.Instance.Data.Play.position;
        //Health 등도 동기화
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
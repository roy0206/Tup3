using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Lobby : MonoBehaviour, ISceneEventListener
{
    private Playermovement player;

    public void OnSceneLoadComplete(string sceneName)
    {
        player = FindObjectOfType<Playermovement>();
        if(UserDataManager.Instance.Data != null)
            player.transform.position = UserDataManager.Instance.Data.Play.position.ToVector3();
        //Health 등도 동기화
    }

    public void OnSceneExit(string sceneName)
    {
        UserDataManager.Instance.Data.Play.position = player.transform.position.ToSerializedVector();
        SceneController.Instance.UnregisterListener(this);
    }

    private void Awake()
    {
        SceneController.Instance.RegisterListener(this);
    }
}
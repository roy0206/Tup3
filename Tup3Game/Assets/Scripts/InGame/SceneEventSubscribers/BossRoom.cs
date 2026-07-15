using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BossRoom : MonoBehaviour, ISceneEventListener
{
    private Playermovement player;
    [SerializeField] private BossFlag boss;

    public void OnSceneLoadComplete(string sceneName)
    {
        player = FindObjectOfType<Playermovement>();
        
    }

    public void OnSceneExit(string sceneName)
    {
        UserDataManager.Instance.Data.Play.clearedBosses |= boss;
        //체력 저장
        UserDataManager.Instance.SaveAsync();
        SceneController.Instance.UnregisterListener(this);
    }

    private void Awake()
    {
        SceneController.Instance.RegisterListener(this);
    }
}

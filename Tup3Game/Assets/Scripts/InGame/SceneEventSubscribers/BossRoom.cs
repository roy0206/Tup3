using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BossRoom : MonoBehaviour, ISceneEventListener
{
    private Playermovement player;
    [SerializeField] private int bossNum;
    public void OnSceneLoadStart(string sceneName)
    {

    }

    public void OnSceneLoadComplete(string sceneName)
    {
        player = FindObjectOfType<Playermovement>();
        
    }

    public void OnSceneExit(string sceneName)
    {
        UserDataManager.Instance.Data.Play.clearedBosses += (int)Mathf.Pow(2, bossNum);
    }

    private void Awake()
    {
        SceneController.Instance.RegisterListener(this);
    }
}

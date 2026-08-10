using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Start : MonoBehaviour, ISceneEventListener
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button resumeButton;

    public void OnSceneLoadComplete(string sceneName)
    {
        UserDataManager.Instance.Data.Achievements.Unlock("Clear");
        startButton.onClick.AddListener(()=> UserDataManager.Instance.ClearPlayData()); 
        startButton.onClick.AddListener(()=> SceneController.Instance.LoadScene("Lobby")); 

        resumeButton.onClick.AddListener(()=> SceneController.Instance.LoadScene("Lobby")); 
    }
    

    public void OnSceneExit(string sceneName)
    {
        SceneController.Instance.UnregisterListener(this);
    }

    private void Awake()
    {
        SceneController.Instance.RegisterListener(this);
    }
}
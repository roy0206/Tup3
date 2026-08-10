using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Ending : MonoBehaviour, ISceneEventListener
{


    public void OnSceneLoadComplete(string sceneName)
    {
        UserDataManager.Instance.Data.Achievements.Unlock("Clear");
        FindAnyObjectByType<Button>().onClick.AddListener(()=> SceneController.Instance.LoadScene("Start")); 
    }
    

    public void OnSceneExit(string sceneName)
    {
        UserDataManager.Instance.ClearPlayData();
        SceneController.Instance.UnregisterListener(this);
    }

    private void Awake()
    {
        SceneController.Instance.RegisterListener(this);
    }
}
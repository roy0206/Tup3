using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Lobby : MonoBehaviour, ISceneEventListener
{


    public void OnSceneLoadComplete(string sceneName)
    {
        Playermovement player = FindObjectOfType<Playermovement>();
        var data = UserDataManager.Instance.Data;
        if (data != null)
        {
            player.transform.position = data.Play.position.ToVector3();
            player.GetComponent<PlayerHealth>().SetHealth(data.Play.health);
            var skills = player.GetComponent<Skills>().IsSkillEquiped;
            for(int i = 0; i < 4; i++)
            {
                skills[i] = data.Play.skills[i];
            }
        }

        //Health 등도 동기화
        
    }

    public void OnSceneExit(string sceneName)
    {
        Playermovement player = FindObjectOfType<Playermovement>();
        var data = UserDataManager.Instance.Data;
        if (data != null)
        {
            data.Play.position = player.transform.position.ToSerializedVector();
            data.Play.skills = player.GetComponent<Skills>().IsSkillEquiped;
            data.Play.health = player.GetComponent<PlayerHealth>().CurrentHealth;
        }
        
        
        SceneController.Instance.UnregisterListener(this);
    }

    private void Awake()
    {
        SceneController.Instance.RegisterListener(this);
    }
}
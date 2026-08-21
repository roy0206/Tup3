using System;
using UnityEngine;
using UnityEngine.EventSystems;

public enum RoomState
{
    None,
    Prepare,
    Cutscene,
    Dialogue,
    Battle,
    PostCutscene,
    PostDialogue,
    Clear
}

public class BossRoom : MonoBehaviour, ISceneEventListener
{
    private Playermovement player;
    [SerializeField] private BossFlag boss;
    [SerializeField] private DialogueManager DM;
    
    public RoomState CurrentRoomState { get; private set; } = RoomState.None;

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


    public void ChangeState(RoomState newState)
    {
        if(CurrentRoomState == newState) return;
        
        switch (CurrentRoomState)
        {
            case RoomState.Prepare: break;
            case RoomState.Cutscene: break;
            case RoomState.Dialogue: break;
            case RoomState.Battle: break;
            case RoomState.PostCutscene: break;
            case RoomState.PostDialogue: break;
            case RoomState.Clear: break;
        }
        CurrentRoomState = newState;

        switch (CurrentRoomState)
        {
            case RoomState.Prepare: break;
            case RoomState.Cutscene: break;
            case RoomState.Dialogue: break;
            case RoomState.Battle: break;
            case RoomState.PostCutscene: break;
            case RoomState.PostDialogue: break;
            case RoomState.Clear: break;
        }
    }

    private void Update()
    {
        switch (CurrentRoomState)
        {
            case RoomState.Prepare: break;
            case RoomState.Cutscene: break;
            case RoomState.Dialogue: break;
            case RoomState.Battle: break;
            case RoomState.PostCutscene: break;
            case RoomState.PostDialogue: break;
            case RoomState.Clear: break;
        }
    }
}

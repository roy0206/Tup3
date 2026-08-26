using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Start : MonoBehaviour, ISceneEventListener
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;

    public void OnSceneLoadComplete(string sceneName)
    {
        startButton.onClick.AddListener(()=> UserDataManager.Instance.ClearPlayData());
        startButton.onClick.AddListener(()=> SceneController.Instance.LoadScene("Prologue"));

        resumeButton.onClick.AddListener(()=> SceneController.Instance.LoadScene("Lobby"));

        if (optionsButton != null)
            optionsButton.onClick.AddListener(()=> PauseManager.Instance.ToggleOptionsOnly());
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

/* [파일 노트]
 * 새 게임(startButton)은 세이브를 초기화한 뒤 Prologue 씬으로 간다.
 * 프롤로그가 끝나거나 스킵되면 PrologueScene 스크립트가 Lobby 로 넘겨준다.
 * 이어하기(resumeButton)는 프롤로그를 건너뛰고 바로 Lobby 로 간다.
 * optionsButton 은 선택 배선이다 — 씬에 옵션 버튼을 만들어 연결하면 PauseManager 의 옵션 패널을
 * 연다(ToggleOptionsOnly). 연결하지 않아도 ESC 키로 같은 패널이 열리므로 씬 수정 없이도 동작한다.
 */
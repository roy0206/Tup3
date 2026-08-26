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

/* [파일 노트]
 * FinalBossRoom 으로 대체 예정, 씬에서 교체 필요.
 * Styx 씬이 최종보스방으로 개조되면서 이 껍데기 컴포넌트의 역할은
 * FinalBossRoom.cs(대사 → 전투 → 승패 즉시 엔딩 분기)가 전부 넘겨받는다.
 * Styx 씬에서 StyxRoom 오브젝트를 제거(또는 컴포넌트 교체)하고 FinalBossRoom 을 배치할 것.
 * 씬 교체 확인 전까지 파일은 삭제하지 않고 남겨 둔다.
 */

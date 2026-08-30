using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Lobby : MonoBehaviour, ISceneEventListener
{
    [Header("진행도 게이트 벽 (인스펙터에서 직접 할당)")]
    [SerializeField] private GameObject firstWall;
    [SerializeField] private List<GameObject> soilGateWalls = new();


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

        ApplyProgressGates();

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

    private void ApplyProgressGates()
    {
        var play = UserDataManager.Instance.Data?.Play;
        if (play == null) return;

        bool introDone = play.styxIntroDone;
        bool soilCleared = play.clearedBosses.HasFlag(BossFlag.Soil);

        // 토보스 게이트 벽 2개 : 최종보스 첫 조우(StyxIntro)를 마치면 열린다.
        bool anySoilWall = false;
        foreach (var wall in soilGateWalls)
        {
            if (wall == null) continue;
            anySoilWall = true;
            wall.SetActive(!introDone);
        }
        if (!anySoilWall)
            Debug.LogWarning("[Lobby] soilGateWalls 가 비어 있습니다 — Lobby 컴포넌트 인스펙터에 토보스 게이트 타일맵 2개를 할당하세요", this);

        // FIrstWall : 토보스를 클리어하면 열려 나머지 보스 포탈이 해금된다.
        if (firstWall != null) firstWall.SetActive(!soilCleared);
        else Debug.LogWarning("[Lobby] firstWall 이 할당되지 않았습니다 — 보스 포탈 잠금이 동작하지 않습니다", this);

        Debug.Log($"[Lobby] 진행도 게이트 적용 — 조우 완료:{introDone}, 토보스 클리어:{soilCleared}");
    }
}

/* [파일 노트]
 * 진행도 게이트 벽 (2026-08-31 유저 확정 구조)
 *   새 게임 직후 로비에서는 Styx 포탈만 열려 있어야 한다. 나머지 보스 포탈은 포탈 로직이 아니라
 *   벽 타일맵으로 물리적으로 막는다:
 *     - 토보스 게이트 벽 2개 (soilGateWalls) : styxIntroDone(최종보스 첫 조우 완료) 이면 비활성화 → 토보스 포탈 개방.
 *     - FIrstWall (firstWall)               : 토보스 클리어(clearedBosses 에 Soil) 이면 비활성화 → 나머지 포탈 전부 개방.
 *   벽 등록은 인스펙터 할당만 지원한다(이름 탐색은 2026-08-31 유저 요청으로 제거).
 *   로비 씬의 GameManager → Lobby 컴포넌트에서 soilGateWalls 리스트와 firstWall 필드에 직접 드래그할 것.
 *   할당이 비면 경고 로그만 남기고 해당 게이트는 건너뛴다.
 *   씬에 저장된 벽의 켜짐/꺼짐 상태는 무관하다 — 진입 때마다 세이브 기준으로 SetActive 를 강제한다.
 *   전체 흐름은 StyxIntro.cs 파일 노트 참고.
 */

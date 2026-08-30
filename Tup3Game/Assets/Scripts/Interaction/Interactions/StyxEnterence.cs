using UnityEngine;
using System;
using System.Collections.Generic;
public class StyxEnterence : InteractionBase, ISceneEventListener
{
    [Header("행선지")]
    [SerializeField] private string introSceneName = "StyxIntro";
    [SerializeField] private string finalBossSceneName = "Styx";

    [Header("모습")]
    [Tooltip("진입 가능 상태(인트로/최종보스전 모두)에서 쓸 스프라이트. 비우면 교체하지 않는다.")]
    [SerializeField] private Sprite activeSprite;

    private const int AllBossesCleared = 15;

    private SpriteRenderer spriteRenderer;
    private Sprite originalSprite;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        SceneController.Instance.RegisterListener(this);
    }

    private static bool IntroDone()
    {
        var data = UserDataManager.Instance.Data;
        return data != null && data.Play != null && data.Play.styxIntroDone;
    }

    private static bool AllCleared()
    {
        return (int)UserDataManager.Instance.Data.Play.clearedBosses == AllBossesCleared;
    }

    protected override bool CanInteract()
    {
        // 첫 조우 전이면 항상 열려 있고(StyxIntro 행), 조우 후에는 4보스 클리어 시에만 열린다(Styx 행).
        return !IntroDone() || AllCleared();
    }

    // 잠김 상태에서는 홀드 아이콘(Interaction UI)도 띄우지 않는다 (2026-08-31 유저 요청).
    public override bool IsInteractionVisible => CanInteract();

    public override bool OnInteract()
    {
        if (base.OnInteract())
        {
            SceneController.Instance.LoadScene(IntroDone() ? finalBossSceneName : introSceneName);
            return true;
        }

        return false;
    }

    public void OnSceneLoadComplete(string sceneName)
    {
        if (spriteRenderer == null) return;

        if (CanInteract())
        {
            if (activeSprite != null) spriteRenderer.sprite = activeSprite;
            spriteRenderer.color = new Color(1, 1, 1, 1);
        }
        else
        {
            spriteRenderer.sprite = originalSprite;
            spriteRenderer.color = new Color(0.2f, 0.2f, 0.2f, 1);
        }
    }

    public void OnSceneExit(string sceneName)
    {
        SceneController.Instance.UnregisterListener(this);
    }
}

/* [파일 노트]
 * Styx 포탈은 게임 진행에 따라 행선지가 바뀐다 (2026-08-31 유저 확정):
 *   - styxIntroDone == false : 항상 열려 있고 StyxIntro 씬(최종보스 첫 조우, 대사만)으로 간다.
 *     새 게임 직후에는 이 포탈이 유일한 진행 경로다 — 보스 포탈들은 로비의 벽 타일맵
 *     (토보스 게이트 2개 + FIrstWall)이 물리적으로 막고 있다(Lobby.cs 참고).
 *   - styxIntroDone == true  : 기존 규칙으로 복귀. 4보스 전부 클리어(clearedBosses == 15)일 때만
 *     열리고 진짜 최종보스전(Styx 씬)으로 간다.
 * 포탈 모습 (2026-08-31): 진입 가능 상태(인트로행·최종보스행 모두)에서는 activeSprite
 * (potal_final.png, 씬에서 할당)로 교체하고 색을 원래대로(흰색) 되돌린다.
 * 잠김 상태에서는 원래 스프라이트로 되돌리고 어둡게(0.2) 표시한다.
 * 원래 스프라이트는 Awake 에서 저장해 두므로 상태가 오가도 안전하다.
 */

using UnityEngine;

public class StyxIntro : MonoBehaviour, ISceneEventListener
{
    [Header("참조")]
    [SerializeField] private DialogueManager DM;

    [Header("대사")]
    [SerializeField] private string dialogueFileName = "S03_LOBBY";
    [SerializeField] private string dialogueStartId = "";

    [Header("연출")]
    [SerializeField] private bool allowManualAdvance = true;

    [Header("복귀")]
    [SerializeField] private string returnSceneName = "Lobby";

    private ComboAttack playerCombo;
    private Skills playerSkills;

    private bool dialogueStarted;
    private bool dialogueRunning;
    private bool finished;
    private bool subscribed;

    /// <summary>보스 상호작용(StyxIntroInteraction)이 이미 소비됐는지 여부.</summary>
    public bool DialogueStarted => dialogueStarted;

    private void Awake()
    {
        SceneController.Instance.RegisterListener(this);
    }

    private void Start()
    {
        // 씬 진입 직후 선제 잠금 (BossRoom 과 같은 이유 — NotifyLoadComplete 전 프레임 대비)
        ResolveReferences();
        SetSceneCombatDisabled();
    }

    public void OnSceneLoadComplete(string sceneName)
    {
        ResolveReferences();
        SetSceneCombatDisabled();
        Subscribe();
    }

    public void OnSceneExit(string sceneName)
    {
        Unsubscribe();
        SceneController.Instance.UnregisterListener(this);
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void ResolveReferences()
    {
        if (DM == null) DM = DialogueManager.Current;

        Playermovement player = FindObjectOfType<Playermovement>();
        if (player != null)
        {
            playerCombo = player.GetComponent<ComboAttack>();
            playerSkills = player.GetComponent<Skills>();
        }
    }

    private void SetSceneCombatDisabled()
    {
        // 이 씬의 보스는 연출용 배경이다 — AI 를 전부 정지시킨다.
        foreach (BossBase boss in FindObjectsOfType<BossBase>())
            boss.enabled = false;

        // 전투 씬에서 복사돼 남아 있을 수 있는 요소 정리 (빌더가 지우지만 이중 안전장치)
        foreach (FinalBossRoom room in FindObjectsOfType<FinalBossRoom>(true))
            room.enabled = false;

        foreach (BossHealthView view in FindObjectsOfType<BossHealthView>(true))
            view.gameObject.SetActive(false);

        if (playerCombo != null) playerCombo.enabled = false;
        if (playerSkills != null) playerSkills.enabled = false;
    }

    private void Subscribe()
    {
        if (subscribed || DM == null) return;
        subscribed = true;
        DM.OnDialogueEnd += HandleDialogueEnd;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        subscribed = false;
        if (DM != null) DM.OnDialogueEnd -= HandleDialogueEnd;
    }

    private void Update()
    {
        if (PauseManager.IsPaused || finished || !dialogueStarted) return;

        if (!dialogueRunning) Finish();
    }

    /// <summary>보스 상호작용에서 호출된다 — 조우 대사를 시작한다.</summary>
    public void BeginDialogue()
    {
        if (dialogueStarted || finished) return;
        dialogueStarted = true;

        if (DM == null) ResolveReferences();
        Subscribe();

        if (DM == null)
        {
            Debug.LogError($"[StyxIntro] DialogueManager 가 없어 '{dialogueFileName}' 대사를 건너뜁니다", this);
            return;
        }

        DM.SetAutoAdvance(false);
        DM.SetAllowSkip(allowManualAdvance);
        DM.StartDialogueFromCsv(dialogueFileName, string.IsNullOrWhiteSpace(dialogueStartId) ? null : dialogueStartId);

        dialogueRunning = DM.IsPlaying;
        if (!dialogueRunning)
            Debug.LogError($"[StyxIntro] '{dialogueFileName}' 대사를 시작하지 못했습니다 — 바로 로비로 복귀합니다", this);
    }

    private void HandleDialogueEnd()
    {
        dialogueRunning = false;
    }

    private void Finish()
    {
        if (finished) return;
        finished = true;

        var data = UserDataManager.Instance.Data;
        if (data != null && data.Play != null)
        {
            data.Play.styxIntroDone = true;
            // 도입부 대사(S03_LOBBY)를 이 씬에서 이미 봤으므로 로비 복도 도입부는 다시 재생하지 않는다.
            data.Play.lobbyIntroDone = true;
            UserDataManager.Instance.SaveAsync();
        }

        Debug.Log("[StyxIntro] 최종보스 조우 완료 — 로비로 복귀합니다");
        SceneController.Instance.LoadScene(returnSceneName);
    }
}

/* [파일 노트 — 최종보스 첫 조우 씬 (StyxIntro)]
 * 게임 시작 직후의 진행 순서는 다음과 같다 (2026-08-31 유저 확정):
 *   새 게임 → 로비(모든 보스 포탈이 벽으로 막힘, Styx 포탈만 열림)
 *   → Styx 포탈 → StyxIntro 씬(이 파일) : 최종보스와 첫 조우, S03_LOBBY 대사 재생
 *   → 대사 종료 → styxIntroDone 저장 → 로비 복귀, 토보스 게이트 벽 2개가 열림
 *   → 토보스 클리어 → FIrstWall 이 열려 나머지 보스 포탈 해금 (Lobby.cs 참고)
 *   → 4보스 클리어 → Styx 포탈이 이번엔 진짜 최종보스전(Styx 씬)으로 연결 (StyxEnterence.cs 참고)
 *
 * 씬 자체는 StyxIntroSceneBuilder(Tools/Tup3/Setup Styx Intro Scene) 가 Styx.unity 를 복사해 만든다.
 * 맵·보스 배치는 Styx 와 동일하고, FinalBossRoom 컴포넌트를 제거한 자리에 이 컴포넌트가 들어간다.
 *
 * 대사 시작은 자동이 아니라 보스와의 상호작용이다 (2026-08-31 유저 확정):
 * 빌더가 보스 오브젝트에 StyxIntroInteraction 을 붙이고, 로비 씬에서 InteractionManager 와
 * InteractionIcon(Canvas) 을 복사해 온다. 플레이어가 보스 근처에서 V 를 홀드하면
 * BeginDialogue() 가 불려 대사가 시작된다. 시작 전까지 이 컴포넌트의 Update 는 아무것도 하지 않는다.
 *
 * 대사 파일은 기존 로비 도입부의 S03_LOBBY.csv 를 그대로 쓴다(유저 확정). 원래 이 파일은
 * LobbyIntroDirector 가 로비 복도의 존 트리거로 한 줄씩 진행시켰지만, 여기서는 통짜 재생하고
 * 수동 진행(allowManualAdvance)으로 넘긴다. 종료 시 lobbyIntroDone 도 같이 true 로 만들어
 * 로비의 LobbyIntroDirector 가 같은 대사를 또 틀지 않게 한다.
 *
 * 보스 정지 방식은 BossRoom 과 동일하게 BossBase.enabled=false (Update 안의 behaviorTree.Tick 이 멈춘다).
 * 플레이어는 ComboAttack/Skills 만 끈다 — Playermovement 를 끄면 중력까지 멈추기 때문(BossRoom 노트 참고).
 * 대사 중 이동 정지는 Playermovement 의 IsDialogueActive 게이트가 알아서 처리한다.
 *
 * 대사 시작 실패(파일 없음 등) 시에도 Finish 로 빠져 로비로 복귀하므로 소프트락은 없다.
 * 단 이 경우에도 styxIntroDone 은 true 로 저장된다 — 진행이 막히는 것보다 낫다고 판단.
 */

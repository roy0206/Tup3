using UnityEngine;

public class FinalBossRoom : MonoBehaviour, ISceneEventListener
{
    [Header("참조")]
    [SerializeField] private DialogueManager DM;
    [SerializeField] private BossBase bossBehaviour;
    [SerializeField] private BossHealthView healthView;

    [Header("대사")]
    [SerializeField] private string introDialogueFile = "S10_FINAL_BOSS";
    [SerializeField] private string introStartId = "";

    [Header("연출 대기 시간")]
    [SerializeField] private float introDelay = 0.5f;
    [SerializeField] private float victoryDelay = 2f;
    [SerializeField] private float defeatDelay = 1.5f;

    [Header("엔딩 분기 (성불 횟수 판독)")]
    [SerializeField] private int totalBossCount = 4;
    [SerializeField] private string saengNoneEndingId = "Ending2";
    [SerializeField] private string saengSomeEndingId = "Ending3";
    [SerializeField] private string saengAllEndingId = "Ending4";

    [Header("패배 연출")]
    [SerializeField] private DefeatCutscene defeatCutscene;
    [SerializeField] private float defeatCutsceneTimeout = 20f;

    [Header("패배 복귀 (다른 보스방과 동일)")]
    [SerializeField] private GameOverView gameOverView;
    [SerializeField] private string defeatReturnScene = "Lobby";
    [SerializeField] private string titleSceneName = "Start";
    [SerializeField] private bool restoreHealthOnDefeat = true;

    [Header("옵션")]
    [SerializeField] private bool allowManualAdvance = true;
    [SerializeField] private bool lockMovementDuringDialogue = false;

    private Playermovement player;
    private PlayerHealth playerHealth;
    private ComboAttack playerCombo;
    private Skills playerSkills;

    private float stateTimer;
    private bool dialogueRunning;
    private bool subscribed;
    private bool endingResolved;
    private bool defeatCutsceneDone;
    private bool pauseBlocked;
    private bool gameOverSubscribed;
    private bool leavingRoom;

    public RoomState CurrentRoomState { get; private set; } = RoomState.None;
    public BattleOutcome Outcome { get; private set; } = BattleOutcome.None;

    private void Awake()
    {
        SceneController.Instance.RegisterListener(this);
    }

    private void Start()
    {
        ResolveReferences();
        SetBossActive(false);
        SetPlayerCombatEnabled(false);
    }

    public void OnSceneLoadComplete(string sceneName)
    {
        ResolveReferences();
        Subscribe();
        ChangeState(RoomState.Prepare);
    }

    public void OnSceneExit(string sceneName)
    {
        Unsubscribe();
        UserDataManager.Instance.SaveAsync();
        SceneController.Instance.UnregisterListener(this);
    }

    private void ResolveReferences()
    {
        player = FindObjectOfType<Playermovement>();
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            playerCombo = player.GetComponent<ComboAttack>();
            playerSkills = player.GetComponent<Skills>();
        }

        if (DM == null) DM = DialogueManager.Current;
        if (bossBehaviour == null) bossBehaviour = FindObjectOfType<BossBase>();
        if (defeatCutscene == null) defeatCutscene = FindObjectOfType<DefeatCutscene>(true);
        if (healthView == null) healthView = FindObjectOfType<BossHealthView>(true);
    }

    private void Subscribe()
    {
        if (subscribed) return;
        subscribed = true;

        if (bossBehaviour != null) bossBehaviour.OnDeath += HandleBossDeath;
        if (playerHealth != null) playerHealth.OnDeath += HandlePlayerDeath;
        if (DM != null) DM.OnDialogueEnd += HandleDialogueEnd;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        subscribed = false;

        if (bossBehaviour != null) bossBehaviour.OnDeath -= HandleBossDeath;
        if (playerHealth != null) playerHealth.OnDeath -= HandlePlayerDeath;
        if (DM != null) DM.OnDialogueEnd -= HandleDialogueEnd;
    }

    private void OnDestroy()
    {
        SetPauseBlocked(false);
        Unsubscribe();
    }

    public void ChangeState(RoomState newState)
    {
        if (CurrentRoomState == newState) return;

        if (CurrentRoomState == RoomState.Dialogue) StopRunningDialogue();
        if (CurrentRoomState == RoomState.DefeatCutscene) StopDefeatCutscene();

        CurrentRoomState = newState;
        stateTimer = 0f;

        switch (CurrentRoomState)
        {
            case RoomState.Prepare:
                SetBossActive(false);
                SetPlayerCombatEnabled(false);
                break;

            case RoomState.Cutscene:
                stateTimer = introDelay;
                break;

            case RoomState.Dialogue:
                PlayDialogue(introDialogueFile, introStartId);
                break;

            case RoomState.Battle:
                SetBossActive(true);
                SetPlayerCombatEnabled(true);
                if (healthView != null) healthView.PlayIntro();
                Debug.Log("[FinalBossRoom] 최종보스전 시작");
                break;

            case RoomState.PostCutscene:
                SetPlayerCombatEnabled(false);
                if (Outcome == BattleOutcome.Defeat) SetBossActive(false);
                stateTimer = Outcome == BattleOutcome.Victory ? victoryDelay : defeatDelay;
                break;

            case RoomState.DefeatCutscene:
                StartDefeatCutscene();
                break;

            case RoomState.GameOver:
                ShowGameOver();
                break;
        }
    }

    private void Update()
    {
        if (PauseManager.IsPaused) return;

        switch (CurrentRoomState)
        {
            case RoomState.Prepare:
                ChangeState(RoomState.Cutscene);
                break;

            case RoomState.Cutscene:
                if (TickTimer()) ChangeState(RoomState.Dialogue);
                break;

            case RoomState.Dialogue:
                if (!dialogueRunning) ChangeState(RoomState.Battle);
                break;

            case RoomState.Battle:
                DetectBattleOutcome();
                if (Outcome != BattleOutcome.None) ChangeState(RoomState.PostCutscene);
                break;

            case RoomState.PostCutscene:
                if (!TickTimer()) break;
                if (Outcome == BattleOutcome.Defeat) ChangeState(RoomState.DefeatCutscene);
                else ResolveEnding();
                break;

            case RoomState.DefeatCutscene:
                TickDefeatCutscene();
                break;

            case RoomState.GameOver:
                break;
        }
    }

    private void StartDefeatCutscene()
    {
        defeatCutsceneDone = false;
        stateTimer = defeatCutsceneTimeout;
        SetPauseBlocked(true);

        if (defeatCutscene == null)
        {
            defeatCutsceneDone = true;
            return;
        }

        defeatCutscene.Play(HandleDefeatCutsceneComplete);
    }

    private void HandleDefeatCutsceneComplete()
    {
        if (CurrentRoomState != RoomState.DefeatCutscene) return;
        defeatCutsceneDone = true;
    }

    private void TickDefeatCutscene()
    {
        if (defeatCutsceneDone)
        {
            ChangeState(RoomState.GameOver);
            return;
        }

        if (defeatCutsceneTimeout <= 0f) return;

        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        Debug.LogWarning($"[FinalBossRoom] 패배 컷씬이 {defeatCutsceneTimeout}초 안에 완료 콜백을 호출하지 않아 게임오버로 넘어갑니다");
        ChangeState(RoomState.GameOver);
    }

    private void ShowGameOver()
    {
        EnsureGameOverView();

        if (gameOverView == null)
        {
            Debug.LogError("[FinalBossRoom] GameOverView 를 준비하지 못해 곧바로 복귀합니다");
            ReturnToLobby();
            return;
        }

        gameOverView.Show();
    }

    private void EnsureGameOverView()
    {
        if (gameOverView == null) gameOverView = FindObjectOfType<GameOverView>(true);

        if (gameOverView == null)
        {
            var go = new GameObject("GameOverView");
            go.transform.SetParent(transform, false);
            gameOverView = go.AddComponent<GameOverView>();
        }

        if (gameOverSubscribed) return;
        gameOverSubscribed = true;

        gameOverView.ContinueRequested += ReturnToLobby;
        gameOverView.TitleRequested += ReturnToTitle;
    }

    private void ReturnToLobby()
    {
        LeaveRoom(defeatReturnScene);
    }

    private void ReturnToTitle()
    {
        LeaveRoom(titleSceneName);
    }

    private void LeaveRoom(string sceneName)
    {
        if (leavingRoom) return;
        leavingRoom = true;

        if (gameOverView != null) gameOverView.Hide();
        SetPauseBlocked(false);

        if (restoreHealthOnDefeat && playerHealth != null)
            playerHealth.SetHealth(playerHealth.MaxHealth);

        SceneController.Instance.LoadScene(sceneName);
    }

    private void StopDefeatCutscene()
    {
        defeatCutsceneDone = false;
        SetPauseBlocked(false);
        if (defeatCutscene != null) defeatCutscene.Stop();
    }

    private void SetPauseBlocked(bool blocked)
    {
        if (pauseBlocked == blocked) return;
        pauseBlocked = blocked;

        if (blocked) PauseManager.BlockPause();
        else PauseManager.UnblockPause();
    }

    private bool TickTimer()
    {
        if (stateTimer <= 0f) return true;
        stateTimer -= Time.deltaTime;
        return stateTimer <= 0f;
    }

    private void DetectBattleOutcome()
    {
        if (Outcome != BattleOutcome.None) return;

        if (bossBehaviour != null && bossBehaviour.IsDead) Outcome = BattleOutcome.Victory;
        else if (playerHealth != null && playerHealth.IsDead) Outcome = BattleOutcome.Defeat;
    }

    private void HandleBossDeath()
    {
        if (CurrentRoomState != RoomState.Battle) return;
        if (Outcome != BattleOutcome.None) return;
        Outcome = BattleOutcome.Victory;
        Debug.Log("[FinalBossRoom] 최종보스 격파");
    }

    private void HandlePlayerDeath()
    {
        if (CurrentRoomState != RoomState.Battle) return;
        if (Outcome != BattleOutcome.None) return;
        Outcome = BattleOutcome.Defeat;
        Debug.Log("[FinalBossRoom] 플레이어 사망");
    }

    private void PlayDialogue(string fileName, string startId)
    {
        dialogueRunning = false;

        if (string.IsNullOrWhiteSpace(fileName)) return;
        if (DM == null)
        {
            Debug.LogError($"[FinalBossRoom] DialogueManager 가 없어 '{fileName}' 대사를 건너뜁니다");
            return;
        }

        DM.SetAutoAdvance(false);
        DM.SetAllowSkip(allowManualAdvance);
        DM.StartDialogueFromCsv(fileName, string.IsNullOrWhiteSpace(startId) ? null : startId);

        dialogueRunning = DM.IsPlaying;
        if (!dialogueRunning)
            Debug.LogError($"[FinalBossRoom] '{fileName}' 대사를 시작하지 못했습니다");
    }

    private void StopRunningDialogue()
    {
        if (!dialogueRunning) return;
        dialogueRunning = false;
        if (DM != null && DM.IsPlaying) DM.StopDialogue();
    }

    private void HandleDialogueEnd()
    {
        dialogueRunning = false;
    }

    private void ResolveEnding()
    {
        if (endingResolved) return;
        endingResolved = true;
        SetPauseBlocked(false);

        var data = UserDataManager.Instance.Data;
        int saengCount = data != null && data.Play != null ? data.Play.willCoins : 0;

        string endingId;
        if (saengCount <= 0) endingId = saengNoneEndingId;
        else if (saengCount >= Mathf.Max(1, totalBossCount)) endingId = saengAllEndingId;
        else endingId = saengSomeEndingId;

        if (data != null && data.Play != null) data.Play.endingId = endingId;
        UserDataManager.Instance.SaveAsync();

        Debug.Log($"[FinalBossRoom] 승리 / 성불 {saengCount}회 → 엔딩 씬 '{endingId}' 로드");
        SceneController.Instance.LoadScene(endingId);
    }

    private void SetBossActive(bool active)
    {
        if (bossBehaviour == null) return;
        bossBehaviour.enabled = active;
    }

    private void SetPlayerCombatEnabled(bool enabled)
    {
        if (playerCombo != null) playerCombo.enabled = enabled;
        if (playerSkills != null) playerSkills.enabled = enabled;
        if (lockMovementDuringDialogue && player != null) player.enabled = enabled;
    }
}

/* [파일 노트]
 *
 * 최종보스방(Styx) 전용 상태 머신. BossRoom.cs 의 흐름을 참고해 새로 작성했다(BossRoom 은 수정하지 않음).
 * RoomState / BattleOutcome enum 은 BossRoom.cs 에 선언된 것을 그대로 쓴다.
 *
 * ── 흐름 ─────────────────────────────────────────────────────────────────────
 *   Prepare      : 보스 컴포넌트 enabled=false(AI 정지), 플레이어 공격/스킬 잠금.
 *   Cutscene     : introDelay 대기.
 *   Dialogue     : introDialogueFile(기본 S10_FINAL_BOSS) 재생. 끝나면 Battle.
 *   Battle       : 보스/플레이어 활성. BossBase.OnDeath → Victory, PlayerHealth.OnDeath → Defeat.
 *                  이벤트 누락 대비 IsDead 폴링(DetectBattleOutcome) 병행.
 *   PostCutscene : 공격/스킬 재잠금. 승리면 보스를 켠 채 victoryDelay(사망 연출),
 *                  패배면 보스 즉시 정지 후 defeatDelay.
 *                  시간이 다 되면 승리는 곧바로 ResolveEnding, 패배는 DefeatCutscene 으로 간다.
 *   DefeatCutscene : (2026-08-28 신설) defeatCutscene.Play(onComplete) 를 한 번 호출하고 콜백을 기다린 뒤
 *                  ResolveEnding 으로 간다. 즉 패배 흐름은 사망 → 컷씬 → (씬 전환 페이드) → 엔딩 씬 이다.
 *                  필드가 비어 있으면 씬에서 DefeatCutscene 을 찾고, 그래도 없으면 구간을 건너뛴다.
 *                  콜백이 오지 않으면 defeatCutsceneTimeout(기본 20초) 후 경고 로그와 함께 강제 진행한다.
 *                  이 구간에는 PauseManager.BlockPause() 로 일시정지를 막는다(BossRoom 과 동일 정책).
 *                  컷씬 구현체는 PlayerDeathCutscene(카메라 포커스 + 비네트 + 혼백 파티클 + 소멸).
 *                  화면 페이드는 컷씬이 아니라 SceneController 의 씬 전환 효과가 담당한다.
 *
 * ── 결과 처리 (승리 선택지·보상·재도전 없음, 즉시 엔딩 분기) ──────────────────
 *   PlayData.willCoins 판독 (willCoins == 성불 횟수):
 *     코인 0      → saengNoneEndingId ("Ending2", 성불 0회 / 약한 의지)
 *     코인 1~3    → saengSomeEndingId ("Ending3", 성불 1~3회 / 보통 의지)
 *     코인 4 이상 → saengAllEndingId  ("Ending4", 성불 4회 / 강한 의지)
 *
 *   코인이 곧 성불 횟수인 근거 : 시작값 4(UserData.PlayData.willCoins) 에서 극(剋) 승리마다
 *   geukWillCoinCost(1) 만큼 빠지고 생(生) 승리는 증감이 없다. Styx 입장이 clearedBosses == 15
 *   (4보스 전부 클리어)로 막혀 있으므로 성불 + 극 = 4 가 항상 성립하고,
 *   따라서 남은 코인 = 4 - 극 = 성불 횟수다.
 *   예전에는 생 승리가 +5 를 줘서 코인이 0/6/12/18/24 로만 튀었고, 그 탓에 옛 임계값의
 *   1~3 구간에 걸리는 경우가 없어 Ending3 이 도달 불가능했다. BossRoom 에서 그 지급을 없앴다.
 *   보스 씬에 남아 있는 saengWillCoinReward: 5 직렬화 값은 필드가 사라져 무시된다.
 *
 * ── 대체 관계 ────────────────────────────────────────────────────────────────
 *   기존 StyxRoom(빈 껍데기)을 이 컴포넌트가 대체한다 — 씬에서 StyxRoom 을 제거하고 이걸 배치할 것.
 *   EndingTrigger(임시 엔딩 분기 상호작용)도 폐기 대상이므로 씬에서 제거해야 한다.
 *
 * ── 일시정지 ─────────────────────────────────────────────────────────────────
 *   Update 첫 줄 PauseManager.IsPaused 게이트로 상태 타이머·전이가 멈춘다.
 *   보스 정지는 BossRoom 과 같은 방식(BossBase.enabled 토글) — FinalBoss 는 OnDisable 에서
 *   진행 중인 연출(거합 시퀀스/돌진 트윈/환영/반격 예약)을 스스로 정리한다.
 */

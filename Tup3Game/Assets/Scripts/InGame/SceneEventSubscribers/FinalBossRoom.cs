using UnityEngine;

public class FinalBossRoom : MonoBehaviour, ISceneEventListener
{
    [Header("참조")]
    [SerializeField] private DialogueManager DM;
    [SerializeField] private BossBase bossBehaviour;

    [Header("대사")]
    [SerializeField] private string introDialogueFile = "S10_FINAL_BOSS";
    [SerializeField] private string introStartId = "";

    [Header("연출 대기 시간")]
    [SerializeField] private float introDelay = 0.5f;
    [SerializeField] private float victoryDelay = 2f;
    [SerializeField] private float defeatDelay = 1.5f;

    [Header("엔딩 분기 (willCoins 판독)")]
    [SerializeField] private int trueEndingCoinCount = 4;
    [SerializeField] private string victoryFullCoinEndingId = "Ending2";
    [SerializeField] private string victorySomeCoinEndingId = "Ending3";
    [SerializeField] private string victoryNoCoinEndingId = "Ending4";
    [SerializeField] private string defeatEndingId = "Ending1";

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

    public RoomState CurrentRoomState { get; private set; } = RoomState.None;
    public BattleOutcome Outcome { get; private set; } = BattleOutcome.None;

    private void Awake()
    {
        SceneController.Instance.RegisterListener(this);
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
        Unsubscribe();
    }

    public void ChangeState(RoomState newState)
    {
        if (CurrentRoomState == newState) return;

        if (CurrentRoomState == RoomState.Dialogue) StopRunningDialogue();

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
                Debug.Log("[FinalBossRoom] 최종보스전 시작");
                break;

            case RoomState.PostCutscene:
                SetPlayerCombatEnabled(false);
                if (Outcome == BattleOutcome.Defeat) SetBossActive(false);
                stateTimer = Outcome == BattleOutcome.Victory ? victoryDelay : defeatDelay;
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
                if (TickTimer()) ResolveEnding();
                break;
        }
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

        var data = UserDataManager.Instance.Data;
        int coins = data != null && data.Play != null ? data.Play.willCoins : 0;

        string endingId = defeatEndingId;
        if (Outcome == BattleOutcome.Victory)
        {
            if (coins >= trueEndingCoinCount) endingId = victoryFullCoinEndingId;
            else if (coins > 0) endingId = victorySomeCoinEndingId;
            else endingId = victoryNoCoinEndingId;
        }

        if (data != null && data.Play != null) data.Play.endingId = endingId;
        UserDataManager.Instance.SaveAsync();

        Debug.Log($"[FinalBossRoom] {Outcome} / 의지 코인 {coins}개 → 엔딩 씬 '{endingId}' 로드");
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
 *                  패배면 보스 즉시 정지 후 defeatDelay. 시간이 다 되면 ResolveEnding.
 *
 * ── 결과 처리 (승리 선택지·보상·재도전 없음, 즉시 엔딩 분기) ──────────────────
 *   PlayData.willCoins 판독:
 *     승리 & 코인 >= trueEndingCoinCount(4) → victoryFullCoinEndingId ("Ending2", S12 트루)
 *     승리 & 코인 1~3                       → victorySomeCoinEndingId ("Ending3")
 *     승리 & 코인 0                         → victoryNoCoinEndingId  ("Ending4")
 *     패배                                  → defeatEndingId          ("Ending1", S11 배드)
 *   기획 확정값은 "코인==4 → 트루"지만 생(生) 승리 누적으로 4를 넘길 수 있어 >= 로 판정한다.
 *   endingId 세팅 + SaveAsync 후 endingId 와 같은 이름의 엔딩 전용 씬("Ending1"~"Ending4",
 *   EndingSceneBuilder 가 생성/등록)을 직접 로드한다 — 엔딩 id = 씬 이름 = SceneSO 이름 계약.
 *   endingId 저장은 유지한다(엔딩 씬의 구성 일치 검사 + 통계용).
 *   승패 어느 쪽도 willCoins 를 변경하지 않고, clearedBosses 도 기록하지 않는다.
 *   ※ 씬에 저장된 구 endingSceneName("Ending") 필드는 코드에서 제거됨 — 단일 Ending 씬은 폐기 예정.
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

using System;
using UnityEngine;

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

public enum BattleOutcome
{
    None,
    Victory,
    Defeat
}

public enum VictoryKind
{
    None,
    Geuk,
    Saeng
}

public class BossRoom : MonoBehaviour, ISceneEventListener
{
    [Header("보스 식별")]
    [SerializeField] private BossFlag boss;
    [SerializeField] private int skillIndex = -1;

    [Header("참조")]
    [SerializeField] private DialogueManager DM;
    [SerializeField] private BossBase bossBehaviour;

    [Header("대사 파일")]
    [SerializeField] private string introDialogueFile;
    [SerializeField] private string victoryDialogueFile;
    [SerializeField] private string victoryStartId;
    [SerializeField] private string defeatDialogueFile = "S13_BATTLE_LOSE";

    [Header("지급 판정 행 id (비우면 _skill / _coin 접미사로 자동 판정)")]
    [SerializeField] private string skillGrantEntryId;
    [SerializeField] private string coinGrantEntryId;

    [Header("연출 대기 시간")]
    [SerializeField] private float introDelay = 0.5f;
    [SerializeField] private float victoryDelay = 2f;
    [SerializeField] private float defeatDelay = 1.5f;

    [Header("보상")]
    [SerializeField] private int saengWillCoinReward = 5;
    [SerializeField] private int defeatWillCoinPenalty = 1;
    [SerializeField] private int geukWillCoinCost = 1;

    [Header("옵션")]
    [SerializeField] private bool allowManualAdvance = true;
    [SerializeField] private bool lockMovementDuringDialogue = false;
    [SerializeField] private bool restoreHealthOnDefeat = true;
    [SerializeField] private string defeatReturnScene = "Lobby";

    private int resolvedSkillIndex = -1;

    private Playermovement player;
    private PlayerHealth playerHealth;
    private ComboAttack playerCombo;
    private Skills playerSkills;

    private float stateTimer;
    private bool dialogueRunning;
    private bool subscribed;
    private bool isCleared;
    private bool skillGranted;
    private bool coinGranted;
    private bool returningToLobby;

    public RoomState CurrentRoomState { get; private set; } = RoomState.None;
    public BattleOutcome Outcome { get; private set; } = BattleOutcome.None;
    public VictoryKind Victory { get; private set; } = VictoryKind.None;

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

        var data = UserDataManager.Instance.Data;
        if (data != null && data.Play != null)
        {
            if (isCleared) data.Play.clearedBosses |= boss;
            if (playerHealth != null) data.Play.health = playerHealth.CurrentHealth;
        }

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
        resolvedSkillIndex = skillIndex >= 0 ? skillIndex : DefaultSkillIndex(boss);
    }

    private static int DefaultSkillIndex(BossFlag flag)
    {
        switch (flag)
        {
            case BossFlag.Soil: return 1;
            case BossFlag.Water: return 3;
            case BossFlag.Fire: return 2;
            case BossFlag.Gold: return 0;
            default: return -1;
        }
    }

    private void Subscribe()
    {
        if (subscribed) return;
        subscribed = true;

        if (bossBehaviour != null) bossBehaviour.OnDeath += HandleBossDeath;
        if (playerHealth != null) playerHealth.OnDeath += HandlePlayerDeath;
        if (DM != null)
        {
            DM.OnDialogueEnd += HandleDialogueEnd;
            DM.OnEntryShown += HandleEntryShown;
        }
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        subscribed = false;

        if (bossBehaviour != null) bossBehaviour.OnDeath -= HandleBossDeath;
        if (playerHealth != null) playerHealth.OnDeath -= HandlePlayerDeath;
        if (DM != null)
        {
            DM.OnDialogueEnd -= HandleDialogueEnd;
            DM.OnEntryShown -= HandleEntryShown;
        }
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    public void ChangeState(RoomState newState)
    {
        if (CurrentRoomState == newState) return;

        switch (CurrentRoomState)
        {
            case RoomState.Prepare: break;
            case RoomState.Cutscene: break;
            case RoomState.Dialogue: StopRunningDialogue(); break;
            case RoomState.Battle: break;
            case RoomState.PostCutscene: break;
            case RoomState.PostDialogue: StopRunningDialogue(); break;
            case RoomState.Clear: break;
        }

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
                PlayDialogue(introDialogueFile, null);
                break;

            case RoomState.Battle:
                SetBossActive(true);
                SetPlayerCombatEnabled(true);
                Debug.Log($"[BossRoom] 전투 시작 — {boss}");
                break;

            case RoomState.PostCutscene:
                SetPlayerCombatEnabled(false);
                if (Outcome == BattleOutcome.Defeat) SetBossActive(false);
                stateTimer = Outcome == BattleOutcome.Victory ? victoryDelay : defeatDelay;
                break;

            case RoomState.PostDialogue:
                SetBossActive(false);
                if (Outcome == BattleOutcome.Victory)
                    PlayDialogue(victoryDialogueFile, victoryStartId);
                else
                    EnterDefeatDialogue();
                break;

            case RoomState.Clear:
                MarkCleared();
                SetPlayerCombatEnabled(true);
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
                if (TickTimer()) ChangeState(RoomState.PostDialogue);
                break;

            case RoomState.PostDialogue:
                if (!dialogueRunning) FinishBattleResult();
                break;

            case RoomState.Clear:
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
        Debug.Log($"[BossRoom] 보스 격파 — {boss}");
    }

    private void HandlePlayerDeath()
    {
        if (CurrentRoomState != RoomState.Battle) return;
        if (Outcome != BattleOutcome.None) return;
        Outcome = BattleOutcome.Defeat;
        Debug.Log($"[BossRoom] 플레이어 사망 — {boss}");
    }

    private void PlayDialogue(string fileName, string startId)
    {
        dialogueRunning = false;

        if (string.IsNullOrWhiteSpace(fileName)) return;
        if (DM == null)
        {
            Debug.LogError($"[BossRoom] DialogueManager 가 없어 '{fileName}' 대사를 건너뜁니다");
            return;
        }

        DM.SetAutoAdvance(false);
        DM.SetAllowSkip(allowManualAdvance);
        DM.StartDialogueFromCsv(fileName, startId);

        dialogueRunning = DM.IsPlaying;
        if (!dialogueRunning)
            Debug.LogError($"[BossRoom] '{fileName}' 대사를 시작하지 못했습니다");
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

    private void HandleEntryShown(string id)
    {
        if (CurrentRoomState != RoomState.PostDialogue) return;
        if (Outcome != BattleOutcome.Victory) return;
        if (string.IsNullOrWhiteSpace(id)) return;

        if (IsSkillGrantEntry(id)) GrantSkill();
        else if (IsCoinGrantEntry(id)) GrantWillCoins();
    }

    private bool IsSkillGrantEntry(string id)
    {
        if (!string.IsNullOrWhiteSpace(skillGrantEntryId)) return id == skillGrantEntryId;
        return id.EndsWith("_skill", StringComparison.Ordinal);
    }

    private bool IsCoinGrantEntry(string id)
    {
        if (!string.IsNullOrWhiteSpace(coinGrantEntryId)) return id == coinGrantEntryId;
        return id.EndsWith("_coin", StringComparison.Ordinal);
    }

    private void GrantSkill()
    {
        if (skillGranted) return;
        skillGranted = true;
        Victory = VictoryKind.Geuk;

        var data = UserDataManager.Instance.Data;

        if (resolvedSkillIndex < 0)
        {
            Debug.LogError($"[BossRoom] skillIndex 가 설정되지 않아 스킬을 줄 수 없습니다 — {boss}");
            return;
        }

        if (playerSkills != null) playerSkills.OptainSkill(resolvedSkillIndex);

        if (data != null && data.Play != null)
        {
            if (resolvedSkillIndex < data.Play.skills.Count)
                data.Play.skills[resolvedSkillIndex] = true;

            data.Play.willCoins = Mathf.Max(0, data.Play.willCoins - geukWillCoinCost);
            UserDataManager.Instance.SaveAsync();
        }

        Debug.Log($"[BossRoom] 극(剋) 승리 — 스킬 {resolvedSkillIndex}번 획득, 의지 코인 {geukWillCoinCost}개 차감 (보유 {UserDataManager.Instance.Data?.Play?.willCoins}개)");
    }

    private void GrantWillCoins()
    {
        if (coinGranted) return;
        coinGranted = true;
        Victory = VictoryKind.Saeng;

        var data = UserDataManager.Instance.Data;
        if (data != null && data.Play != null)
        {
            data.Play.willCoins += saengWillCoinReward;
            UserDataManager.Instance.SaveAsync();
            Debug.Log($"[BossRoom] 생(生) 승리 — 의지 코인 {saengWillCoinReward}개 획득 (보유 {data.Play.willCoins}개)");
        }
    }

    private void EnterDefeatDialogue()
    {
        var data = UserDataManager.Instance.Data;
        if (data != null && data.Play != null)
        {
            data.Play.willCoins = Mathf.Max(0, data.Play.willCoins - defeatWillCoinPenalty);
            UserDataManager.Instance.SaveAsync();
            Debug.Log($"[BossRoom] 패배 — 의지 코인 {defeatWillCoinPenalty}개 차감 (보유 {data.Play.willCoins}개)");
        }

        PlayDialogue(defeatDialogueFile, null);
    }

    private void FinishBattleResult()
    {
        if (Outcome == BattleOutcome.Victory)
        {
            ChangeState(RoomState.Clear);
            return;
        }

        ReturnToLobby();
    }

    private void ReturnToLobby()
    {
        if (returningToLobby) return;
        returningToLobby = true;

        if (restoreHealthOnDefeat && playerHealth != null)
            playerHealth.SetHealth(playerHealth.MaxHealth);

        SceneController.Instance.LoadScene(defeatReturnScene);
    }

    private void MarkCleared()
    {
        if (isCleared) return;
        isCleared = true;

        var data = UserDataManager.Instance.Data;
        if (data != null && data.Play != null)
        {
            data.Play.clearedBosses |= boss;
            UserDataManager.Instance.SaveAsync();
        }

        Debug.Log($"[BossRoom] 클리어 확정 — {boss} ({Victory})");
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
 * ── 상태 머신 흐름 ────────────────────────────────────────────────────────────
 *
 *   None
 *     └ OnSceneLoadComplete → 참조 수집 + 이벤트 구독 → Prepare
 *
 *   Prepare      : 보스 컴포넌트 enabled=false(=AI 정지), 플레이어 공격/스킬 잠금.
 *                  Update 첫 프레임에 바로 Cutscene 으로 넘어간다.
 *   Cutscene     : introDelay 초 대기(입장 연출 자리). 시간이 다 되면 Dialogue.
 *   Dialogue     : introDialogueFile 을 처음부터 재생. 대사가 끝나면(OnDialogueEnd) Battle.
 *                  파일명이 비어 있거나 재생 실패면 곧바로 Battle 로 간다.
 *   Battle       : 보스 enabled=true, 플레이어 공격/스킬 해제. 여기서만 승패를 판정한다.
 *                  보스 OnDeath → Victory, 플레이어 OnDeath → Defeat.
 *                  이벤트가 안 오는 경우를 대비해 Update 에서 IsDead 도 같이 폴링한다(DetectBattleOutcome).
 *   PostCutscene : 플레이어 공격/스킬 재잠금. 승리면 보스를 켠 채로 victoryDelay 만큼 기다려
 *                  보스의 사망 연출(각 보스 Dead() BT 태스크)이 재생되게 두고,
 *                  패배면 보스를 즉시 꺼서 시체를 더 때리지 않게 한다(defeatDelay).
 *   PostDialogue : 보스 완전 정지.
 *                  · 승리 → victoryDialogueFile 을 victoryStartId 행부터 재생.
 *                    그 행이 "받아들인다 / 거절한다" 선택지 행이고, 선택 결과로 _skill 또는 _coin 행에 도달한다.
 *                    도달 감지는 DialogueManager.OnEntryShown 으로 하고 거기서 실제 지급이 일어난다.
 *                  · 패배 → 의지 코인을 먼저 차감하고 defeatDialogueFile(S13) 재생.
 *
 * ── 의지 코인 경제 (PlayData.willCoins, 항상 0 미만 금지) ─────────────────────
 *   극(剋) 승리(스킬 획득) : -geukWillCoinCost (기본 1)
 *   생(生) 승리(스킬 거절) : +saengWillCoinReward (기본 5)
 *   패배                  : -defeatWillCoinPenalty (기본 1)
 *   시작값은 0. 잔여 코인은 나중에 엔딩 분기에 사용될 예정(기획: 최종보스 재도전/배드엔딩).
 *   Clear        : clearedBosses 에 이 보스 플래그를 기록하고 즉시 SaveAsync.
 *                  플레이어 조작을 돌려주고, 이후 퇴장은 기존 BossExit 상호작용에 맡긴다.
 *
 *   패배 대사가 끝나면 상태를 바꾸지 않고 ReturnToLobby() 로 defeatReturnScene(기본 "Lobby") 을 로드한다.
 *
 * ── 클리어 판정 버그 수정 ──────────────────────────────────────────────────────
 *   기존 OnSceneExit 은 씬을 나가기만 하면 무조건 clearedBosses 를 기록했다(= 입장 후 그냥 나가도 클리어).
 *   이제 Clear 상태에 들어갔을 때(isCleared) 만 기록한다. Clear 진입 시점에 이미 저장하므로
 *   보스방에서 알트F4 로 꺼도 클리어와 스킬은 남는다.
 *
 * ── 전투 정지 방식 ────────────────────────────────────────────────────────────
 *   보스 파생 클래스는 손대지 않는다. 각 보스의 Update 안에서 behaviorTree.Tick() 이 돌기 때문에
 *   BossBase 컴포넌트의 enabled 만 끄면 AI·패턴·중력이 전부 멈춘다.
 *   Gold 는 OnDisable 에서 패턴4 연출과 패턴1 이펙트를 정리하므로 도중에 꺼도 잔상이 남지 않는다.
 *   플레이어는 ComboAttack / Skills 컴포넌트만 끈다(둘 다 Update 에서 입력을 읽는 구조).
 *   Playermovement 는 기본적으로 끄지 않는다 —— 끄면 중력 처리까지 멈춰 공중에 뜬 채로 굳기 때문이다.
 *   대사 중 완전히 정지시키고 싶으면 lockMovementDuringDialogue 를 켜면 된다.
 *
 * ── 스킬 인덱스 매핑 ──────────────────────────────────────────────────────────
 *   skillIndex 를 -1(기본값) 로 두면 BossFlag 로 자동 매핑한다 (2026-08-25 유저 확정):
 *   토=1(지형생성), 수=3(힐), 화=2(공격속도), 금=0(공격력/데미지).
 *   Skills.cs 인덱스 기준: 0=공격력↑(skill_1), 1=이속+지형생성(skill_2), 2=공속↑(skill_3), 3=힐(skill_4).
 *   WaterBossAbsorption 의 recoverySkillIndex=3 과도 일치한다.
 *
 * ── 물보스 특수 케이스 ────────────────────────────────────────────────────────
 *   Boss_Water 는 WaterBossAbsorption(상호작용)이 스킬 지급과 clearedBosses 기록을 직접 하고 있다.
 *   그 스크립트는 건드리지 않았다. victoryDialogueFile 을 비워 두면 BossRoom 은 승리 대사와 선택지를 건너뛰고
 *   바로 Clear 로 가므로 기존 흡수 연출과 충돌하지 않는다(플래그 OR 와 skills[i]=true 는 둘 다 멱등).
 *   반대로 물보스도 선택지 플로우로 통합하려면 victoryDialogueFile/victoryStartId 를 채우고
 *   WaterBossAbsorption 오브젝트를 비활성화하면 된다.
 *
 * ── 지급 행 판정 ──────────────────────────────────────────────────────────────
 *   skillGrantEntryId / coinGrantEntryId 를 비워 두면 행 id 가 "_skill" / "_coin" 으로 끝나는지로 판정한다.
 *   CSV 네이밍 규칙(to_skill / to_coin / su_skill / su_coin ...)을 지키면 씬에서 따로 채울 필요가 없다.
 *
 * ── DialogueManager 요구 사항 ─────────────────────────────────────────────────
 *   보스방은 자동 진행을 쓰지 않으므로 SetAutoAdvance(false) + SetAllowSkip(allowManualAdvance) 를 매번 걸어 준다.
 *   Boss_Soil 의 DialogueManager 는 allowDialogueSkip 이 false 로 저장돼 있어 이걸 안 하면 V 키로 대사가 넘어가지 않는다.
 *
 * ── 일시정지 대응 ─────────────────────────────────────────────────────────────
 *   Update 첫 줄의 PauseManager.IsPaused 게이트로 방 상태 머신 타이머(Cutscene/PostCutscene 대기)와
 *   상태 전이가 일시정지 중 진행되지 않는다. 대사 중 일시정지도 허용된다(DialogueManager 쪽 게이트 참고).
 */

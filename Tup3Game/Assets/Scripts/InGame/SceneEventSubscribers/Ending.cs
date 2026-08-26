using UnityEngine;
using UnityEngine.UI;

public class Ending : MonoBehaviour, ISceneEventListener
{
    [Header("엔딩 구성 (씬 전용 — 이 씬이 담당하는 엔딩 하나)")]
    [SerializeField] private string endingId = "Ending1";
    [SerializeField] private string dialogueFile = "S11_ENDING_1";
    [SerializeField] private string dialogueStartId = "end1_01";
    [SerializeField] private string achievementId = "Ending1";

    [Header("크레딧 (개별 콘텐츠 구간 — 공통 출처는 Resources/Credits/CommonCredits.txt)")]
    [SerializeField, TextArea(6, 40)] private string creditsContent = "(작성중)";
    [SerializeField] private float creditsScrollSpeed = 90f;
    [SerializeField] private float creditsFastMultiplier = 8f;

    [Header("참조 (비우면 자동 탐색/생성)")]
    [SerializeField] private DialogueManager DM;
    [SerializeField] private Button returnButton;
    [SerializeField] private CreditsView creditsView;

    [Header("옵션")]
    [SerializeField] private string returnScene = "Start";
    [SerializeField] private bool allowManualAdvance = true;

    private bool dialogueRunning;
    private bool subscribed;
    private bool creditsStarted;
    private bool achievementsUnlocked;

    private void Awake()
    {
        SceneController.Instance.RegisterListener(this);
    }

    public void OnSceneLoadComplete(string sceneName)
    {
        ResolveReferences();
        WarnOnEndingMismatch();
        Subscribe();

        if (returnButton != null)
        {
            returnButton.onClick.AddListener(ReturnToStart);
            returnButton.gameObject.SetActive(false);
        }

        PlayEndingDialogue();

        if (!dialogueRunning) StartCredits();
    }

    public void OnSceneExit(string sceneName)
    {
        Unsubscribe();
        UnlockAchievements();
        UserDataManager.Instance.ClearPlayData();
        UserDataManager.Instance.SaveAsync();
        SceneController.Instance.UnregisterListener(this);
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void ResolveReferences()
    {
        if (DM == null) DM = DialogueManager.Current;
        if (returnButton == null) returnButton = FindAnyObjectByType<Button>(FindObjectsInactive.Include);
        if (creditsView == null) creditsView = FindAnyObjectByType<CreditsView>(FindObjectsInactive.Include);
    }

    private void WarnOnEndingMismatch()
    {
        string savedId = UserDataManager.Instance.Data?.Play?.endingId;

        if (string.IsNullOrWhiteSpace(savedId))
        {
            Debug.LogWarning($"[Ending] PlayData.endingId 가 비어 있습니다 — 씬 구성('{endingId}')대로 진행합니다");
            return;
        }

        if (savedId != endingId)
            Debug.LogWarning($"[Ending] 저장된 endingId '{savedId}' 와 씬 구성 '{endingId}' 이 다릅니다 — 씬 구성대로 진행합니다");
    }

    private void Subscribe()
    {
        if (subscribed || DM == null) return;
        subscribed = true;
        DM.OnDialogueEnd += HandleDialogueEnd;
    }

    private void Unsubscribe()
    {
        if (!subscribed || DM == null) return;
        subscribed = false;
        DM.OnDialogueEnd -= HandleDialogueEnd;
    }

    private void PlayEndingDialogue()
    {
        dialogueRunning = false;

        if (string.IsNullOrWhiteSpace(dialogueFile)) return;

        if (DM == null)
        {
            Debug.LogError($"[Ending] DialogueManager 가 없어 '{dialogueFile}' 대사를 건너뜁니다");
            return;
        }

        DM.SetAutoAdvance(false);
        DM.SetAllowSkip(allowManualAdvance);
        DM.StartDialogueFromCsv(dialogueFile, string.IsNullOrWhiteSpace(dialogueStartId) ? null : dialogueStartId);

        dialogueRunning = DM.IsPlaying;
        if (!dialogueRunning)
            Debug.LogError($"[Ending] '{dialogueFile}' 대사를 시작하지 못했습니다");
    }

    private void HandleDialogueEnd()
    {
        if (!dialogueRunning) return;
        dialogueRunning = false;
        StartCredits();
    }

    private void StartCredits()
    {
        if (creditsStarted)
        {
            FinishEnding();
            return;
        }
        creditsStarted = true;

        if (creditsView == null)
        {
            var go = new GameObject("CreditsView");
            creditsView = go.AddComponent<CreditsView>();
        }

        creditsView.SetScrollSpeed(creditsScrollSpeed, creditsFastMultiplier);
        creditsView.Play(creditsContent, FinishEnding);
    }

    private void FinishEnding()
    {
        UnlockAchievements();
        if (returnButton != null) returnButton.gameObject.SetActive(true);
    }

    private void UnlockAchievements()
    {
        if (achievementsUnlocked) return;
        achievementsUnlocked = true;

        var data = UserDataManager.Instance.Data;
        if (data == null) return;

        data.Achievements.Unlock("Clear");
        if (!string.IsNullOrWhiteSpace(achievementId))
            data.Achievements.Unlock(achievementId);

        UserDataManager.Instance.SaveAsync();
        Debug.Log($"[Ending] 업적 해금 — Clear + {achievementId}");
    }

    private void ReturnToStart()
    {
        SceneController.Instance.LoadScene(returnScene);
    }
}

/* [파일 노트]
 *
 * ── 구조 변경 (4슬롯 → 씬 전용) ─────────────────────────────────────────────
 *   과거에는 단일 Ending 씬이 PlayData.endingId 를 판독해 4슬롯 리스트에서 분기했지만,
 *   이제 엔딩마다 전용 씬(Ending1~Ending4, EndingSceneBuilder 가 생성)이 있고 각 씬의
 *   이 컴포넌트는 "자기 엔딩 하나"만 안다 — endingId/dialogueFile/dialogueStartId/achievementId
 *   + 엔딩별 크레딧 콘텐츠(TextArea). FinalBossRoom 이 결과에 맞는 씬을 직접 로드하므로
 *   판독 분기가 필요 없고, 저장된 endingId 와 씬 구성이 다르면 경고 로그만 남긴다.
 *
 * ── 흐름 ─────────────────────────────────────────────────────────────────────
 *   OnSceneLoadComplete
 *     → 참조 수집(DM/버튼/CreditsView 자동 탐색) → endingId 일치 검사(경고만)
 *     → 복귀 버튼 숨김 → 대사 재생(DialogueManager null 안전 — 없으면 대사만 건너뜀)
 *   대사 종료(OnDialogueEnd) 또는 대사 시작 실패
 *     → StartCredits : CreditsView(없으면 즉석 생성)로 크레딧 스크롤 재생.
 *       [개별 콘텐츠(creditsContent)] → [공통 출처(Resources/Credits/CommonCredits.txt)] 순.
 *       V/클릭 홀드로 가속(사실상 스킵)은 CreditsView 가 처리한다.
 *   크레딧 종료(콜백 FinishEnding)
 *     → 업적 해금("Clear" + achievementId) + SaveAsync → 복귀 버튼 표시(누르면 returnScene)
 *   OnSceneExit
 *     → 업적 해금 보장(멱등) → ClearPlayData → SaveAsync (기존 동작 그대로)
 *
 * ── 씬별 기본 매핑 (EndingSceneBuilder 가 배선) ──────────────────────────────
 *   Ending1 = S11_ENDING_1 / end1_01  (패배·배드)
 *   Ending2 = S12_ENDING_2 / end2_01  (코인4 승리·트루)
 *   Ending3 = S14_ENDING_3 / end3_01  (코인1~3 승리)
 *   Ending4 = S15_ENDING_4 / end4_01  (코인0 승리)
 *   업적 id 는 endingId 를 그대로 쓴다. 코드 기본값은 Ending1 세트라 컴포넌트를 그냥
 *   붙여도 최소한 배드엔딩 구성으로는 동작한다.
 *
 * ── 크레딧 수정 방법 ─────────────────────────────────────────────────────────
 *   개별 구간 : 각 엔딩 씬 GameManager 의 이 컴포넌트 인스펙터 creditsContent(TextArea).
 *   공통 구간 : Assets/Resources/Credits/CommonCredits.txt 한 파일만 수정(전 엔딩 공유).
 *
 * ── null 안전 ────────────────────────────────────────────────────────────────
 *   DM 이 없으면 대사만, CreditsView 는 즉석 생성이라 항상 동작, 버튼이 없어도 예외 없이
 *   진행된다(복귀 수단만 없어짐). 크레딧 이중 시작은 creditsStarted 플래그로 방지.
 */

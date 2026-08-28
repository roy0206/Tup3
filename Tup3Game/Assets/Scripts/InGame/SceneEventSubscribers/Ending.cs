using TMPro;
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
    [SerializeField] private Button checkpointButton;
    [SerializeField] private ConfirmDialogView confirmDialog;

    [Header("옵션")]
    [SerializeField] private string returnScene = "Start";
    [SerializeField] private bool allowManualAdvance = true;
    [SerializeField] private string checkpointScene = "Lobby";

    [Header("복귀 선택지 문구")]
    [SerializeField] private string checkpointButtonLabel = "마지막 체크포인트로 돌아가기";
    [SerializeField] private string returnButtonLabel = "시작 화면으로";
    [SerializeField] private string checkpointConfirmTitle = "정말 돌아갈까요?";
    [SerializeField, TextArea(3, 8)]
    private string checkpointConfirmMessage =
        "마지막 체크포인트로 돌아가면 이 엔딩의 업적이 클리어되지 않습니다.\n" +
        "엔딩 업적은 '시작 화면으로'를 선택했을 때만 인정됩니다.";
    [SerializeField] private string checkpointConfirmYes = "돌아가기";
    [SerializeField] private string checkpointConfirmNo = "취소";

    [Header("복귀 선택지 배치 (버튼을 코드로 만들 때만 사용)")]
    [SerializeField] private float checkpointButtonWidth = 560f;
    [SerializeField] private float checkpointButtonGap = 16f;
    [SerializeField] private Vector2 fallbackButtonSize = new Vector2(560f, 68f);
    [SerializeField] private float fallbackButtonFontSize = 30f;
    [SerializeField] private Color fallbackPanelColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color fallbackButtonColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color fallbackTextColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private int fallbackSortingOrder = 920;

    private UiFocusKeeper choiceFocus;
    private bool dialogueRunning;
    private bool subscribed;
    private bool confirmSubscribed;
    private bool creditsStarted;
    private bool achievementsUnlocked;
    private bool leaving;

    private void Awake()
    {
        SceneController.Instance.RegisterListener(this);
    }

    public void OnSceneLoadComplete(string sceneName)
    {
        ResolveReferences();
        WarnOnEndingMismatch();
        Subscribe();

        BuildReturnChoices();
        SetChoicesVisible(false);

        PlayEndingDialogue();

        if (!dialogueRunning) StartCredits();
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
        if (creditsView == null) creditsView = FindAnyObjectByType<CreditsView>(FindObjectsInactive.Include);
        if (confirmDialog == null) confirmDialog = FindAnyObjectByType<ConfirmDialogView>(FindObjectsInactive.Include);
        if (returnButton == null) returnButton = FindSceneReturnButton();
    }

    private Button FindSceneReturnButton()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || button == checkpointButton) continue;
            if (confirmDialog != null && button.transform.IsChildOf(confirmDialog.transform)) continue;
            if (button.transform.IsChildOf(transform)) continue;
            return button;
        }

        return null;
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
        if (subscribed && DM != null)
        {
            subscribed = false;
            DM.OnDialogueEnd -= HandleDialogueEnd;
        }

        if (confirmSubscribed && confirmDialog != null)
        {
            confirmSubscribed = false;
            confirmDialog.Confirmed -= HandleCheckpointConfirmed;
            confirmDialog.Canceled -= HandleCheckpointCanceled;
        }
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
        SetChoicesVisible(true);
    }

    private void BuildReturnChoices()
    {
        EnsureConfirmDialog();

        if (returnButton != null)
        {
            if (checkpointButton == null) checkpointButton = CloneCheckpointButton();
        }
        else
        {
            BuildFallbackChoices();
        }

        ApplyButtonLabel(checkpointButton, checkpointButtonLabel);
        ApplyButtonLabel(returnButton, returnButtonLabel);

        if (checkpointButton != null)
        {
            checkpointButton.onClick.RemoveListener(RequestCheckpointReturn);
            checkpointButton.onClick.AddListener(RequestCheckpointReturn);
        }

        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(ReturnToStart);
            returnButton.onClick.AddListener(ReturnToStart);
        }

        if (returnButton == null && checkpointButton == null)
        {
            Debug.LogError("[Ending] 복귀 버튼을 만들지 못했습니다 — 엔딩에서 빠져나갈 수단이 없습니다");
            return;
        }

        UiFocus.EnsureEventSystem();
        UiViewBuilder.ApplySelectionTint(checkpointButton);
        UiViewBuilder.ApplySelectionTint(returnButton);
        UiFocus.LinkVertical(true, checkpointButton, returnButton);
        choiceFocus = UiFocus.AttachKeeper(gameObject, fallbackSortingOrder, checkpointButton, returnButton);
    }

    private void EnsureConfirmDialog()
    {
        if (confirmDialog == null)
        {
            var go = new GameObject("ConfirmDialogView");
            go.transform.SetParent(transform, false);
            confirmDialog = go.AddComponent<ConfirmDialogView>();
        }

        confirmDialog.SetContent(
            checkpointConfirmTitle, checkpointConfirmMessage, checkpointConfirmYes, checkpointConfirmNo);
        confirmDialog.Hide();

        if (confirmSubscribed) return;
        confirmSubscribed = true;
        confirmDialog.Confirmed += HandleCheckpointConfirmed;
        confirmDialog.Canceled += HandleCheckpointCanceled;
    }

    private Button CloneCheckpointButton()
    {
        var sourceRect = (RectTransform)returnButton.transform;

        GameObject clone = Instantiate(returnButton.gameObject, sourceRect.parent);
        clone.name = "CheckpointButton";

        var button = clone.GetComponent<Button>();
        if (button == null) return null;
        button.onClick = new Button.ButtonClickedEvent();

        var rect = (RectTransform)clone.transform;
        float height = sourceRect.rect.height;
        rect.sizeDelta = new Vector2(Mathf.Max(sourceRect.sizeDelta.x, checkpointButtonWidth), rect.sizeDelta.y);
        rect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, height + checkpointButtonGap);

        return button;
    }

    private void BuildFallbackChoices()
    {
        var go = new GameObject("EndingReturnChoices");
        go.transform.SetParent(transform, false);

        TMP_FontAsset font = UiViewBuilder.FindFallbackFont(transform);
        UiViewBuilder.SetupOverlayCanvas(go, fallbackSortingOrder);

        RectTransform panel = UiViewBuilder.BuildCenterPanel(go.transform, fallbackPanelColor, checkpointButtonGap);

        checkpointButton = UiViewBuilder.BuildButton(
            panel, "CheckpointButton", checkpointButtonLabel, font, fallbackButtonFontSize,
            fallbackButtonColor, fallbackTextColor, fallbackButtonSize);

        returnButton = UiViewBuilder.BuildButton(
            panel, "ReturnButton", returnButtonLabel, font, fallbackButtonFontSize,
            fallbackButtonColor, fallbackTextColor, fallbackButtonSize);
    }

    private void ApplyButtonLabel(Button button, string label)
    {
        if (button == null || string.IsNullOrWhiteSpace(label)) return;

        var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null) text.text = label;
    }

    private void SetChoicesVisible(bool visible)
    {
        if (!visible) UiFocus.Clear(choiceFocus);

        if (checkpointButton != null) checkpointButton.gameObject.SetActive(visible);
        if (returnButton != null) returnButton.gameObject.SetActive(visible);

        if (visible) UiFocus.Select(choiceFocus);
    }

    private void RequestCheckpointReturn()
    {
        if (leaving) return;

        if (confirmDialog == null)
        {
            Debug.LogError("[Ending] 확인창이 없어 체크포인트 복귀를 진행할 수 없습니다");
            return;
        }

        confirmDialog.Show();
    }

    private void HandleCheckpointCanceled()
    {
        if (confirmDialog != null) confirmDialog.Hide();
    }

    private void HandleCheckpointConfirmed()
    {
        if (leaving) return;
        leaving = true;

        if (confirmDialog != null) confirmDialog.Hide();

        Debug.Log($"[Ending] 체크포인트 복귀 — '{achievementId}' 업적은 해금하지 않고 세이브도 유지합니다");
        SceneController.Instance.LoadScene(checkpointScene);
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
        if (leaving) return;
        leaving = true;

        if (confirmDialog != null) confirmDialog.Hide();

        UnlockAchievements();

        UserDataManager.Instance.ClearPlayData();
        UserDataManager.Instance.SaveAsync();

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
 *     → 참조 수집(DM/버튼/CreditsView/ConfirmDialogView 자동 탐색) → endingId 일치 검사(경고만)
 *     → 복귀 선택지 2개 구성 후 숨김 → 대사 재생(DialogueManager null 안전 — 없으면 대사만 건너뜀)
 *   대사 종료(OnDialogueEnd) 또는 대사 시작 실패
 *     → StartCredits : CreditsView(없으면 즉석 생성)로 크레딧 스크롤 재생.
 *       [개별 콘텐츠(creditsContent)] → [공통 출처(Resources/Credits/CommonCredits.txt)] 순.
 *       V/클릭 홀드로 가속(사실상 스킵)은 CreditsView 가 처리한다.
 *   크레딧 종료(콜백 FinishEnding)
 *     → 복귀 선택지 2개를 표시하기만 한다. 여기서는 업적도 세이브도 건드리지 않는다.
 *   OnSceneExit
 *     → 구독 해제 + 리스너 등록 해제만. (세이브 갱신은 SceneController 가 전환 시 수행한다)
 *
 * ── 복귀 분기 (2026-08-28 유저 확정 — 사양 변경) ─────────────────────────────
 *   예전에는 "크레딧이 끝나면 무조건" 업적 해금 + ClearPlayData 였고 OnSceneExit 에도 같은 처리를
 *   멱등하게 한 번 더 걸어 두었다. 이제는 플레이어의 선택에 따라 갈린다 —
 *   업적 해금은 더 이상 "엔딩 도달"이 아니라 "엔딩을 받아들이고 회차를 끝낸다"는 선언이다.
 *
 *   [A] "마지막 체크포인트로 돌아가기" (checkpointButton)
 *       → 곧바로 이동하지 않고 ConfirmDialogView 로 경고 모달을 띄운다
 *         (이 엔딩의 업적이 클리어되지 않는다는 안내 — 문구 4종 전부 인스펙터 노출).
 *       → "취소" : 모달만 닫고 엔딩 화면(선택지 2개)으로 돌아온다.
 *       → "돌아가기" : 업적을 해금하지 않고, ClearPlayData 도 호출하지 않은 채
 *         checkpointScene(기본 "Lobby")을 로드한다. 세이브가 그대로 남으므로 회차가 이어진다.
 *   [B] "시작 화면으로" (returnButton — 기존 필드를 그대로 재사용)
 *       → UnlockAchievements("Clear" + achievementId) → SaveAsync → ClearPlayData → SaveAsync
 *         → returnScene(기본 "Start") 로드.
 *
 *   실행 순서의 근거 : UserData 는 Achievements / Settings / Play 세 덩어리를 가진 한 객체이고
 *   ClearPlayData() 는 Data.Play 만 새 PlayData 로 교체할 뿐 Achievements 는 손대지 않는다
 *   (UserDataManager.ClearPlayData / UserData.cs 파일 노트 참조). 그래도 "해금 → 저장 → 삭제 → 저장"
 *   순서를 지키는 이유는, LocalSaveBackend.SaveAsync 가 File.WriteAllText 로 동기 기록 후
 *   완료된 Task 를 반환하기 때문에 첫 SaveAsync 시점에 업적이 이미 디스크에 확정되기 때문이다.
 *   이후 어떤 단계가 실패하거나 씬 전환이 끼어들어도 업적은 유실되지 않는다.
 *   업적까지 지워 버리는 UserDataManager.ResetAsync 는 이 경로에서 절대 쓰지 않는다.
 *   leaving 플래그로 두 버튼의 중복 입력을 막는다(SceneController.IsTransitioning 과 이중 방어).
 *
 * ── 체크포인트의 정의 ────────────────────────────────────────────────────────
 *   이 프로젝트에는 별도 체크포인트 시스템이 없다. PlayData.position 을 읽고 쓰는 곳은 Lobby.cs
 *   뿐이며(로비를 떠날 때의 로비 좌표를 저장), 좌표·체력·스킬 복원은 Lobby.OnSceneLoadComplete 가
 *   전담한다. 따라서 "마지막 체크포인트 = 로비 씬"이고, BossRoom 의 패배 복귀(defeatReturnScene)와
 *   같은 규약이다. 씬 이름은 checkpointScene 으로 인스펙터에서 바꿀 수 있다.
 *
 * ── 버튼 확보 방식 ───────────────────────────────────────────────────────────
 *   씬(EndingSceneBuilder)은 Canvas 밑에 ReturnButton 하나만 만들어 두므로, 두 번째 버튼은
 *   런타임에 그 버튼을 Instantiate 로 복제해 만든다 — 폰트·색·앵커가 그대로 따라오므로 씬에서
 *   버튼을 꾸며 두면 두 버튼의 모양이 자동으로 일치한다. 복제본은 원본 위(높이 + gap)에 놓고
 *   긴 라벨이 들어가도록 폭만 checkpointButtonWidth 이상으로 넓히며, onClick 은 새 이벤트로
 *   갈아끼워 원본의 연결을 물려받지 않는다. 씬에 checkpointButton 을 직접 배치해 두면 복제는
 *   생략되고 그것을 쓴다.
 *   returnButton 조차 없는 씬에서는 UiViewBuilder 로 버튼 2개짜리 패널을 통째로 코드 생성한다
 *   (fallback* 인스펙터 값으로 스타일 조정). 두 버튼 모두 실패했을 때만 에러 로그를 남긴다.
 *   ConfirmDialogView 는 씬 배치본이 있으면 그것을, 없으면 이 오브젝트의 자식으로 생성한다
 *   (BossRoom 의 GameOverView 처리와 동일한 폴백).
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
 * ── 키보드 조작 (2026-08-28 유저 확정) ───────────────────────────────────────
 *   복귀 선택지 2개는 UiFocus 로 키보드 조작을 붙인다.
 *     - UiViewBuilder.ApplySelectionTint : 씬(EndingSceneBuilder)이 만든 ReturnButton 은
 *       Image.color 가 (0,0,0,0.75) 검정이고 ColorBlock 은 기본값(normal=흰색)이라, ColorTint 가
 *       검정에 흰색을 곱하는 꼴이 되어 선택/호버 상태가 화면에서 전혀 구분되지 않았다.
 *       이 함수가 색을 정규화해 선택 시 배경이 확실히 밝아지게 만든다. 복제본(CheckpointButton)도
 *       같이 통과시키므로 두 버튼의 강조가 일치한다.
 *     - UiFocus.LinkVertical : 위(체크포인트) ↔ 아래(시작 화면)를 Explicit 순환 연결.
 *       Automatic 이면 확인창이 떠 있을 때 방향키가 Dim 을 뚫고 여기로 새어 나온다.
 *     - UiFocus.AttachKeeper : 파수꾼을 이 오브젝트(GameManager)에 붙인다. 버튼 2개가 서로 다른
 *       부모(씬 캔버스 / 코드 생성 패널)에 있을 수 있어 공통 루트를 쓸 수 없기 때문이다.
 *       크레딧이 흐르는 동안에는 버튼이 비활성이라 Preferred 가 null 이고, 파수꾼은 그런 자신을
 *       최상위 판정에서 스스로 제외하므로 다른 UI 를 방해하지 않는다.
 *   기본 선택은 위쪽 "마지막 체크포인트로 돌아가기"다. 첫 항목이기도 하고, 이쪽은 곧바로 씬을
 *   떠나지 않고 확인창을 한 번 더 띄우므로(= 되돌릴 수 있음) 잘못 눌러도 안전하다.
 *   반대편 "시작 화면으로"는 업적 해금 + ClearPlayData 라 되돌릴 수 없어 기본 선택으로 두지 않는다.
 *   확인창이 열리면 ConfirmDialogView 가 선택을 가져갔다가 닫힐 때 여기로 되돌려 준다.
 *
 * ── null 안전 ────────────────────────────────────────────────────────────────
 *   DM 이 없으면 대사만, CreditsView 는 즉석 생성이라 항상 동작, 버튼이 없어도 예외 없이
 *   진행된다. 크레딧 이중 시작은 creditsStarted 플래그로 방지.
 */

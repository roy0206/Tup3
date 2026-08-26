using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class DialogueManager : DomainSingleton<DialogueManager>
{
    public class Choice
    {
        public string label;   // 화면에 보이는 텍스트
        public string targetId; // 고르면 점프할 id (3단계에서 사용)
    }

    private enum State { Inactive, Typing, WaitingForNext, Choosing }
    public enum Speaker { Player, Boss, Narration }
    
    [SerializeField] private bool allowDialogueSkip;
    [SerializeField] private float autoAdvanceDelay = 1.6f;

    [System.Serializable]
    public class DialogueEntry
    {
        public string id;
        public Speaker speaker;
        [TextArea] public string text; 
        public string next;
        public string choices;
    }
    
    [SerializeField] private Transform dialogueRoot;
    [SerializeField] private float charDelay = 0.04f;

    [Header("화자별 대화창")]
    private GameObject playerPanel;
    private TextMeshProUGUI playerText;
    private GameObject bossPanel;
    private TextMeshProUGUI bossText;
    private GameObject narrationPanel;   // dialogueRoot에 "NarrationPanel"이 없으면 null → 플레이어 패널로 대체
    private TextMeshProUGUI narrationText;


    [Header("선택지 UI")]
    private GameObject choicePanel;
    private GameObject[] choiceObjects;
    private TextMeshProUGUI[] choiceTexts;
    private Color normalColor = Color.white;
    private Color selectedColor = Color.yellow;
    private DialogueChoiceView choiceView;

    private List<Choice> currentChoices;
    private int selectedIndex;

    private State state = State.Inactive;
    private TextMeshProUGUI activeText;
    private DialogueEntry[] entries;
    private int currentLine;
    private Coroutine typingRoutine;
    private Coroutine autoAdvanceRoutine;
    private bool autoAdvance;

    public event Action OnDialogueEnd;
    public event Action<int> OnLineShown;
    public event Action<string> OnEntryShown;

    public int ShownLineCount { get; private set; }
    public bool IsPlaying => state != State.Inactive;
    public static bool IsDialogueActive => Current != null && Current.IsPlaying;
    public string CurrentEntryId => currentEntry != null ? currentEntry.id : string.Empty;

    public void SetAllowSkip(bool enabled)
    {
        allowDialogueSkip = enabled;
    }

    public void SetAutoAdvance(bool enabled, float delay = -1f)
    {
        autoAdvance = enabled;
        if (delay > 0f) autoAdvanceDelay = delay;
        if (!enabled) CancelAutoAdvance();
    }

    public void StopDialogue()
    {
        if (state == State.Inactive) return;
        EndDialogue();
    }

    private void CancelAutoAdvance()
    {
        if (autoAdvanceRoutine == null) return;
        StopCoroutine(autoAdvanceRoutine);
        autoAdvanceRoutine = null;
    }

    private IEnumerator AutoAdvanceRoutine()
    {
        yield return new WaitForSeconds(autoAdvanceDelay);
        yield return PauseManager.WaitWhilePaused();
        autoAdvanceRoutine = null;
        if (state == State.WaitingForNext) Advance();
    }


    [System.Serializable]
    public class DialogueData
    {
        public DialogueEntry[] entries;
    }


    protected void Awake()
    {
        base.Awake();
        playerPanel = dialogueRoot.Find("PlayerPanel").gameObject;
        bossPanel = dialogueRoot.Find("BossPanel").gameObject;
        choicePanel = dialogueRoot.Find("ChoicePanel").gameObject;
        playerText = playerPanel.GetComponentInChildren<TextMeshProUGUI>(true);
        bossText = bossPanel.GetComponentInChildren<TextMeshProUGUI>(true);

        var narrationTr = dialogueRoot.Find("NarrationPanel");
        if (narrationTr != null)
        {
            narrationPanel = narrationTr.gameObject;
            narrationText = narrationPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            narrationPanel.SetActive(false);
        }
        choiceView = dialogueRoot.GetComponentInChildren<DialogueChoiceView>(true);

        choiceTexts = choicePanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        choiceObjects = new GameObject[choiceTexts.Length];

        for (int i = 0; i < choiceTexts.Length; i++)
            choiceObjects[i] = choiceTexts[i].transform.parent.gameObject;

        playerPanel.SetActive(false);
        bossPanel.SetActive(false);
        choicePanel.SetActive(false);
    }

    private Dictionary<string, DialogueEntry> entryMap;
    private DialogueEntry currentEntry;

    List<Choice> ParseChoices(string raw)
    {
        var result = new List<Choice>();
        if (string.IsNullOrWhiteSpace(raw)) return result;

        // "/" 로 선택지끼리 나눔
        string[] parts = raw.Split('/');
        foreach (var part in parts)
        {
            // ":" 로 라벨과 targetId 나눔
            string[] pair = part.Split(':');
            if (pair.Length < 2) continue;

            result.Add(new Choice
            {
                label = pair[0].Trim(),
                targetId = pair[1].Trim()
            });
        }
        return result;
    }

    public void StartDialogue(DialogueEntry[] dialogueEntries)
    {
        StartDialogue(dialogueEntries, null);
    }

    public void StartDialogue(DialogueEntry[] dialogueEntries, string startId)
    {
        if (dialogueEntries == null || dialogueEntries.Length == 0)
        {
            Debug.LogError("대사 목록이 비어 있어 대화를 시작할 수 없음");
            return;
        }

        ShownLineCount = 0;
        entries = dialogueEntries;

        // id로 찾을 수 있게 딕셔너리 구성
        entryMap = new Dictionary<string, DialogueEntry>();
        foreach (var e in dialogueEntries)
        {
            if (!string.IsNullOrWhiteSpace(e.id))
                entryMap[e.id] = e;
        }

        string firstId = dialogueEntries[0].id;

        if (!string.IsNullOrWhiteSpace(startId))
        {
            if (entryMap.ContainsKey(startId))
                firstId = startId;
            else
                Debug.LogError($"시작 id '{startId}'를 못 찾음 — 첫 행부터 재생합니다");
        }

        GoToEntry(firstId);
    }
    void GoToEntry(string id)
    {
        if (!entryMap.TryGetValue(id, out currentEntry))
        {
            Debug.LogError($"id '{id}'를 못 찾음");
            EndDialogue();
            return;
        }

        ShowEntry(currentEntry);
    }

    void ShowEntry(DialogueEntry entry)
    {
        CancelAutoAdvance();

        GameObject panel;
        if (entry.speaker == Speaker.Player)
        {
            panel = playerPanel;
            activeText = playerText;
        }
        else if (entry.speaker == Speaker.Narration)
        {
            // NarrationPanel이 씬에 없으면 플레이어 패널로 대신 표시
            panel = narrationPanel != null ? narrationPanel : playerPanel;
            activeText = narrationPanel != null ? narrationText : playerText;
        }
        else
        {
            panel = bossPanel;
            activeText = bossText;
        }

        playerPanel.SetActive(panel == playerPanel);
        bossPanel.SetActive(panel == bossPanel);
        if (narrationPanel != null) narrationPanel.SetActive(panel == narrationPanel);

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeLine(entry.text));

        ShownLineCount++;
        OnLineShown?.Invoke(ShownLineCount);
        OnEntryShown?.Invoke(entry.id);
    }

    IEnumerator TypeLine(string line)
    {
        state = State.Typing;
        activeText.text = line;
        activeText.maxVisibleCharacters = 0;
        activeText.ForceMeshUpdate();
        int total = activeText.textInfo.characterCount;

        int visible = 0;
        while (visible < total)
        {
            if (PauseManager.IsPaused)
            {
                yield return null;
                continue;
            }

            visible++;
            activeText.maxVisibleCharacters = visible;
            yield return new WaitForSeconds(charDelay);
        }
        state = State.WaitingForNext;
        typingRoutine = null;

        if (autoAdvance) autoAdvanceRoutine = StartCoroutine(AutoAdvanceRoutine());
    }

    void Update()
    {
        if (state == State.Inactive) return;
        if (PauseManager.IsPaused) return;

        if (state == State.Choosing)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                selectedIndex--;
                if (selectedIndex < 0) selectedIndex = currentChoices.Count - 1;
                UpdateChoiceHighlight();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                selectedIndex++;
                if (selectedIndex >= currentChoices.Count) selectedIndex = 0;
                UpdateChoiceHighlight();
            }
            else if (Input.GetKeyDown(KeyCode.V))
            {
                ConfirmChoice();
            }
            return;   // 선택지 모드의 V키는 확정 전용이라 아래 진행 처리로 내려가지 않는다
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            if (state == State.Typing)
            {
                // 타이핑 중 V → 즉시 전체 표시 (흔한 UX)
                if (typingRoutine != null) StopCoroutine(typingRoutine);
                typingRoutine = null;
                activeText.maxVisibleCharacters = activeText.textInfo.characterCount;
                state = State.WaitingForNext;

                if (autoAdvance)
                {
                    CancelAutoAdvance();
                    autoAdvanceRoutine = StartCoroutine(AutoAdvanceRoutine());
                }
            }
            else if (state == State.WaitingForNext)
            { 
                if(allowDialogueSkip)
                    Advance();
            }
        }
    }

    public void Advance()
    {
       
        var choices = ParseChoices(currentEntry.choices);
        if (choices.Count > 0)
        {
            ShowChoices(choices);
            return;
        }
        if (string.IsNullOrWhiteSpace(currentEntry.next))
            EndDialogue();                  // next 없으면 끝
        else
            GoToEntry(currentEntry.next);   // next id로 점프
    }

    void EndDialogue()
    {
        CancelAutoAdvance();
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        state = State.Inactive;
        if (playerPanel != null) playerPanel.SetActive(false);
        if (bossPanel != null) bossPanel.SetActive(false);
        if (narrationPanel != null) narrationPanel.SetActive(false);
        HideChoices(true);

        OnDialogueEnd?.Invoke();
    }

    // CSV의 speaker 칸 → enum 매핑 (한글 화자명 지원)
    static Speaker ParseSpeaker(string raw)
    {
        string s = raw.Trim();
        if (System.Enum.TryParse(s, true, out Speaker sp)) return sp;

        switch (s)
        {
            case "주인공":
                return Speaker.Player;
            case "나레이션":
            case "연출":
            case "시스템":
            case "전투":
                return Speaker.Narration;
            default:
                return Speaker.Boss;   // 토보스/수보스/화보스/금보스, 낯선 무언가 등
        }
    }

    public void StartDialogueFromFile(string fileName)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Dialogues/" + fileName);

        if (jsonFile == null)
        {
            Debug.LogError($"대화 파일을 못찾았어요 ㅠㅠ: Dialogues/{fileName}");
            return;
        }

        DialogueData data = JsonUtility.FromJson<DialogueData>(jsonFile.text);

        if (data == null || data.entries == null || data.entries.Length == 0)
        {
            Debug.LogError("대화 파일은 읽었는데 내용이 비었거나 형식이 안 맞아유");
            return;
        }

        StartDialogue(data.entries);   // 기존 함수 재활용
    }


    public void StartDialogueFromCsv(string fileName)
    {
        StartDialogueFromCsv(fileName, null);
    }

    public void StartDialogueFromCsv(string fileName, string startId)
    {
        DialogueEntry[] loaded = LoadEntriesFromCsv(fileName);
        if (loaded == null) return;

        StartDialogue(loaded, startId);
    }

    public DialogueEntry[] LoadEntriesFromCsv(string fileName)
    {
        TextAsset csvFile = Resources.Load<TextAsset>("Dialogues/" + fileName);
        if (csvFile == null)
        {
            Debug.LogError($"CSV 파일을 못 찾음: Dialogues/{fileName}");
            return null;
        }

        var rows = CsvParser.Parse(csvFile.text);
        var list = new System.Collections.Generic.List<DialogueEntry>();

        // 첫 줄(헤더) 건너뛰려고 i=1부터
        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count < 3) continue;              // 빈 줄/불완전한 줄 건너뜀
            if (string.IsNullOrWhiteSpace(row[0])) continue;

            var entry = new DialogueEntry();

            entry.speaker = ParseSpeaker(row[1]);

            entry.id = row[0].Trim();
            entry.text = row[2];                              // 2번 = text
            entry.next = row.Count > 3 ? row[3].Trim() : "";  // 3번 = next
            entry.choices = row.Count > 4 ? row[4] : "";

            list.Add(entry);
        }

        if (list.Count == 0)
        {
            Debug.LogError("CSV는 읽었는데 유효한 대사가 없음");
            return null;
        }

        return list.ToArray();
    }

    void ShowChoices(List<Choice> choices)
    {
        currentChoices = choices;
        selectedIndex = 0;
        state = State.Choosing;

        if (choiceView != null)
        {
            if (choicePanel != null) choicePanel.SetActive(false);
            choiceView.Show(choices, selectedIndex);
            return;
        }

        choicePanel.SetActive(true);

        // 선택지 개수만큼 말풍선 켜고, 남는 건 끔
        for (int i = 0; i < choiceObjects.Length; i++)
        {
            if (i < choices.Count)
            {
                choiceObjects[i].SetActive(true);
                choiceTexts[i].text = choices[i].label;
            }
            else
            {
                choiceObjects[i].SetActive(false);
            }
        }

        UpdateChoiceHighlight();
    }

    // 현재 선택된 것만 색 강조
    void UpdateChoiceHighlight()
    {
        if (choiceView != null)
        {
            choiceView.SetHighlight(selectedIndex);
            return;
        }

        for (int i = 0; i < Mathf.Min(currentChoices.Count, choiceTexts.Length);i++)
        {
            choiceTexts[i].color = (i == selectedIndex) ? selectedColor : normalColor;
        }
    }

    void HideChoices(bool instant)
    {
        if (choiceView != null)
        {
            if (instant) choiceView.HideInstant();
            else choiceView.Hide();
            return;
        }

        if (choicePanel != null) choicePanel.SetActive(false);
    }

    void ConfirmChoice()
    {
        Choice chosen = currentChoices[selectedIndex];
        Debug.Log($"선택함: {chosen.label} → 점프할 id: {chosen.targetId}");

        HideChoices(false);

        if (string.IsNullOrWhiteSpace(chosen.targetId))
        {
            EndDialogue();
        }
        else
        {
            GoToEntry(chosen.targetId);
        }
    }

}

/* [파일 노트 — 자동 진행 모드 추가분]
 *
 * 1) 자동 진행(autoAdvance)
 *    - SetAutoAdvance(true, delay) 로 켜면, 한 줄의 타이핑이 끝난 뒤 autoAdvanceDelay 초를 기다렸다가
 *      스스로 Advance() 를 호출한다. 끄면(false) 예약된 대기도 즉시 취소된다.
 *    - 기본값은 false 이므로 Start / Boss_Soil / Test 등 기존 씬의 동작은 그대로다.
 *      (기존 씬 yaml 에는 autoAdvanceDelay 값이 없으므로 C# 초기값 1.6f 이 그대로 쓰인다.)
 *    - 대기는 AutoAdvanceRoutine 코루틴이 담당하고, 핸들을 autoAdvanceRoutine 에 들고 있다가
 *      ShowEntry / EndDialogue / SetAutoAdvance(false) 시점에 CancelAutoAdvance() 로 취소한다.
 *      → 같은 줄이 두 번 넘어가거나, 대사가 끝난 뒤에도 Advance 가 호출되는 사고를 막는다.
 *    - 선택지(Choosing) 상태에서는 TypeLine 이 끝나도 곧바로 ShowChoices 로 넘어가므로
 *      자동 진행이 선택지를 임의로 골라버리는 일은 없다.
 *
 * 2) 이벤트
 *    - OnDialogueEnd : EndDialogue() 시점(패널을 모두 끈 뒤)에 1회 발생. 프롤로그 연출이 이 신호로 다음 씬을 띄운다.
 *    - OnLineShown(int) : 새 대사가 화면에 뜰 때마다 "지금까지 보여준 줄 수"(1부터)를 넘겨준다.
 *      대사 진행도에 맞춰 조명 단계를 바꾸는 용도.
 *    - ShownLineCount / IsPlaying 는 폴링용 보조 프로퍼티.
 *
 * 3) StopDialogue()
 *    - 스킵 처리용. 외부에서 대사를 강제 종료한다. 내부적으로 EndDialogue() 를 부르므로
 *      OnDialogueEnd 도 함께 발생한다. 스킵 쪽에서 중복 처리를 원하지 않으면
 *      호출 전에 OnDialogueEnd 구독을 먼저 해제할 것.
 *
 * 4) charDelay 에 [SerializeField] 를 붙였다.
 *    기존 씬들은 이 값을 저장해 둔 적이 없으므로 그대로 0.04 로 동작하고,
 *    프롤로그처럼 대사가 긴 씬에서만 인스펙터로 낮춰 쓸 수 있다.
 */

/* [파일 노트 — 보스전 플로우 대응 추가분]
 *
 * 1) 중간부터 재생 : StartDialogueFromCsv(fileName, startId) / StartDialogue(entries, startId)
 *    CSV 를 통째로 읽어 entryMap 을 만든 뒤 startId 행부터 시작한다. 같은 파일 하나에
 *    도입 대사와 승리 대사를 같이 넣어 두고, 보스전 상태에 따라 다른 지점부터 재생하려고 만든 오버로드다.
 *    startId 가 비어 있으면(null/공백) 기존과 똑같이 배열 0번부터 시작한다.
 *    startId 를 못 찾으면 에러 로그를 남기고 0번부터 재생한다(대화가 통째로 죽는 것보다 낫다).
 *    LoadEntriesFromCsv(fileName) 는 파싱 결과만 돌려주는 public 함수로, 재생 없이 내용만 훑고 싶을 때 쓴다.
 *
 * 2) OnEntryShown(string id)
 *    새 행이 화면에 뜰 때마다 그 행의 id 를 넘겨준다. OnLineShown(줄 번호)과 달리 분기해서 건너뛴 대사에도
 *    정확히 대응되므로, BossRoom 이 "스킬 지급 행(_skill)" / "의지 코인 행(_coin)" 에 도달했는지 판정하는 데 쓴다.
 *    CurrentEntryId 는 같은 정보의 폴링용 프로퍼티.
 *
 * 3) SetAllowSkip(bool)
 *    allowDialogueSkip 이 인스펙터에서 false 로 저장된 씬에서는 V 키로 다음 줄로 못 넘어간다.
 *    (Boss_Soil 이 그 상태였다.) 보스방은 자동 진행을 쓰지 않으므로 BossRoom 이 시작 전에 이 함수로 켜 준다.
 *
 * 4) 선택지 뷰 분리
 *    표시는 DialogueChoiceView 가 전담한다. Awake 에서 dialogueRoot 하위를
 *    GetComponentInChildren<DialogueChoiceView>(true) 로 찾아 두고, 있으면 Show/SetHighlight/Hide 만 호출한다.
 *    없으면 기존 ChoicePanel(말풍선 + 노란 하이라이트) 경로를 그대로 탄다 —— 기존 씬은 손대지 않아도 동작한다.
 *    선택 이동(좌우 화살표)·확정(V)·targetId 점프 로직은 전부 이 클래스에 남아 있다.
 *
 * 5) 일시정지 대응 (대화 중 일시정지 허용)
 *    - Update : PauseManager.IsPaused 동안 V/화살표 입력을 무시한다(대사 진행·선택 확정 차단).
 *    - TypeLine : 일시정지 동안 글자 출력을 멈췄다가 해제 시 이어서 타이핑한다.
 *    - AutoAdvanceRoutine : 대기 시간이 끝나도 일시정지 중이면 해제될 때까지 Advance 를 보류한다.
 */

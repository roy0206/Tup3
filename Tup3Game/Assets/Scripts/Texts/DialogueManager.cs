using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class DialogueManager : MonoBehaviour
{
    public class Choice
    {
        public string label;   // 화면에 보이는 텍스트
        public string targetId; // 고르면 점프할 id (3단계에서 사용)
    }

    private enum State { Inactive, Typing, WaitingForNext, Choosing }
    public enum Speaker { Player, Boss }
   
    [System.Serializable]
    public class DialogueEntry
    {
        public string id;
        public Speaker speaker;
        [TextArea] public string text; 
        public string next;
        public string choices;
    }
    
    [SerializeField] private float charDelay = 0.04f;

    [Header("화자별 대화창")]
    [SerializeField] private GameObject playerPanel;
    [SerializeField] private TextMeshProUGUI playerText;
    [SerializeField] private GameObject bossPanel;
    [SerializeField] private TextMeshProUGUI bossText;


    [Header("선택지 UI")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private GameObject[] choiceObjects;      // Choice0, Choice1
    [SerializeField] private TextMeshProUGUI[] choiceTexts;   // Choice0Text, Choice1Text
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    private List<Choice> currentChoices;
    private int selectedIndex;

    private State state = State.Inactive;
    private TextMeshProUGUI activeText;
    private DialogueEntry[] entries;
    private int currentLine;
    private Coroutine typingRoutine;


    [System.Serializable]
    public class DialogueData
    {
        public DialogueEntry[] entries;
    }


    void Awake()
    {
        if (playerPanel != null) playerPanel.SetActive(false);
        if (bossPanel != null) bossPanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
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
        // id로 찾을 수 있게 딕셔너리 구성
        entryMap = new Dictionary<string, DialogueEntry>();
        foreach (var e in dialogueEntries)
        {
            if (!string.IsNullOrWhiteSpace(e.id))
                entryMap[e.id] = e;
        }

        // 첫 대사는 배열의 0번으로 시작 (보통 "start")
        GoToEntry(dialogueEntries[0].id);
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
        if (entry.speaker == Speaker.Player)
        {
            playerPanel.SetActive(true);
            bossPanel.SetActive(false);
            activeText = playerText;
        }
        else
        {
            playerPanel.SetActive(false);
            bossPanel.SetActive(true);
            activeText = bossText;
        }

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeLine(entry.text));

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
            visible++;
            activeText.maxVisibleCharacters = visible;
            yield return new WaitForSeconds(charDelay);
        }
        state = State.WaitingForNext;
    }

    void Update()
    {
        if (state == State.Inactive) return;

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
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                ConfirmChoice();
            }
            return;   // 선택지 모드에선 V키 처리 안 함
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            if (state == State.Typing)
            {
                // 타이핑 중 V → 즉시 전체 표시 (흔한 UX)
                if (typingRoutine != null) StopCoroutine(typingRoutine);
                activeText.maxVisibleCharacters = activeText.textInfo.characterCount;
                state = State.WaitingForNext;
            }
            else if (state == State.WaitingForNext)
            {
                Advance();
            }
        }
    }

    void Advance()
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
        state = State.Inactive;
        if (playerPanel != null) playerPanel.SetActive(false);
        if (bossPanel != null) bossPanel.SetActive(false);
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
        TextAsset csvFile = Resources.Load<TextAsset>("Dialogues/" + fileName);
        if (csvFile == null)
        {
            Debug.LogError($"CSV 파일을 못 찾음: Dialogues/{fileName}");
            return;
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

            // speaker 문자열 → enum 변환
            if (System.Enum.TryParse(row[1].Trim(), out Speaker sp))
                entry.speaker = sp;
            else
            {
                Debug.LogWarning($"{i}번째 줄: 알 수 없는 speaker '{row[1]}' → Player로 처리");
                entry.speaker = Speaker.Player;
            }

            entry.id = row[0].Trim();
            entry.text = row[2];                              // 2번 = text
            entry.next = row.Count > 3 ? row[3].Trim() : "";  // 3번 = next
            entry.choices = row.Count > 4 ? row[4] : "";

            list.Add(entry);
        }

        if (list.Count == 0)
        {
            Debug.LogError("CSV는 읽었는데 유효한 대사가 없음");
            return;
        }

        StartDialogue(list.ToArray());   // 기존 함수 재활용
    }

    void ShowChoices(List<Choice> choices)
    {
        currentChoices = choices;
        selectedIndex = 0;
        state = State.Choosing;

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
        for (int i = 0; i < currentChoices.Count; i++)
        {
            choiceTexts[i].color = (i == selectedIndex) ? selectedColor : normalColor;
        }
    }

    void ConfirmChoice()
    {
        Choice chosen = currentChoices[selectedIndex];
        Debug.Log($"선택함: {chosen.label} → 점프할 id: {chosen.targetId}");

        choicePanel.SetActive(false);

        // 3단계에서 여기에 GoToEntry(chosen.targetId) 넣을 예정
        // 지금은 일단 대화 종료
        EndDialogue();
    }

}

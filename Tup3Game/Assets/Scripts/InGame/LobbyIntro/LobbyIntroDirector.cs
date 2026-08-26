using System.Collections.Generic;
using UnityEngine;

public class LobbyIntroDirector : DomainSingleton<LobbyIntroDirector>, ISceneEventListener
{
    [Header("대사")]
    [SerializeField] private string dialogueFileName = "S03_LOBBY";

    [Header("재생 조건")]
    [SerializeField] private bool playIntro = true;
    [SerializeField] private bool useSaveFlag = true;

    [Header("진행")]
    [SerializeField] private float minStepInterval = 0.25f;
    [SerializeField] private bool logStepFlow = true;

    private readonly List<ILobbyIntroStep> steps = new List<ILobbyIntroStep>();

    private bool resolved;
    private bool introEnabled;
    private bool dialogueStarted;
    private bool introFinished;
    private int cursor;
    private float lastFireTime = -999f;

    public bool IntroFinished => introFinished;

    public int NextStepOrder => (resolved && !introFinished && cursor < steps.Count) ? steps[cursor].StepOrder : -1;

    protected override void Awake()
    {
        base.Awake();
        if (Current != this) return;
        SceneController.Instance.RegisterListener(this);
    }

    public void OnSceneLoadComplete(string sceneName)
    {
        EnsureResolved();
    }

    public void OnSceneExit(string sceneName)
    {
        SceneController.Instance.UnregisterListener(this);
    }

    public bool IsNextStep(ILobbyIntroStep step)
    {
        EnsureResolved();
        if (!introEnabled || introFinished) return false;
        if (step == null || cursor >= steps.Count) return false;
        return ReferenceEquals(steps[cursor], step);
    }

    public bool TryFire(ILobbyIntroStep step)
    {
        EnsureResolved();
        if (!introEnabled || introFinished) return false;
        if (step == null || cursor >= steps.Count) return false;
        if (!ReferenceEquals(steps[cursor], step)) return false;
        if (Time.time - lastFireTime < minStepInterval) return false;

        DialogueManager dialogue = DialogueManager.Current;
        if (dialogue == null)
        {
            Debug.LogError("[LobbyIntroDirector] 씬에 DialogueManager 가 없어 도입부 대사를 진행할 수 없습니다.", this);
            return false;
        }

        if (!dialogueStarted)
        {
            dialogue.StartDialogueFromCsv(dialogueFileName);
            dialogueStarted = true;
        }
        else if (!dialogue.IsPlaying)
        {
            if (logStepFlow) Debug.Log("[LobbyIntroDirector] 대화가 이미 종료되어 남은 트리거를 정리합니다.", this);
            FinishIntro();
            return false;
        }
        else
        {
            dialogue.Advance();
        }

        lastFireTime = Time.time;
        cursor++;

        if (logStepFlow) Debug.Log($"[LobbyIntroDirector] 트리거 순번 {step.StepOrder} 발동 ({cursor}/{steps.Count})", this);

        if (cursor >= steps.Count) FinishIntro();
        return true;
    }

    private void EnsureResolved()
    {
        if (resolved) return;
        resolved = true;

        CollectSteps();

        introEnabled = playIntro && !(useSaveFlag && IsIntroDoneInSave());

        if (!introEnabled)
        {
            introFinished = true;
            DisableRemainingSteps();
            if (logStepFlow) Debug.Log($"[LobbyIntroDirector] 도입부를 건너뜁니다. 트리거 {steps.Count}개 비활성화.", this);
            return;
        }

        if (logStepFlow) Debug.Log($"[LobbyIntroDirector] 도입부 준비 완료. 트리거 {steps.Count}개, 대사 파일 '{dialogueFileName}'.", this);
    }

    private void CollectSteps()
    {
        steps.Clear();

        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is ILobbyIntroStep step) steps.Add(step);
        }

        steps.Sort((a, b) => a.StepOrder.CompareTo(b.StepOrder));

        for (int i = 1; i < steps.Count; i++)
        {
            if (steps[i].StepOrder != steps[i - 1].StepOrder) continue;
            Debug.LogWarning($"[LobbyIntroDirector] 순번 {steps[i].StepOrder} 이 중복됩니다. 발동 순서가 뒤바뀔 수 있습니다.", steps[i] as MonoBehaviour);
        }
    }

    private void FinishIntro()
    {
        if (introFinished) return;
        introFinished = true;
        DisableRemainingSteps();
        MarkIntroDoneInSave();
        if (logStepFlow) Debug.Log("[LobbyIntroDirector] 도입부 종료.", this);
    }

    private void DisableRemainingSteps()
    {
        for (int i = cursor; i < steps.Count; i++)
        {
            if (steps[i] == null) continue;
            if (steps[i] is MonoBehaviour behaviour && behaviour == null) continue;
            steps[i].OnIntroDisabled();
        }
    }

    private bool IsIntroDoneInSave()
    {
        UserData data = UserDataManager.Instance.Data;
        if (data == null || data.Play == null) return false;
        return data.Play.lobbyIntroDone;
    }

    private void MarkIntroDoneInSave()
    {
        if (!useSaveFlag) return;

        UserData data = UserDataManager.Instance.Data;
        if (data == null || data.Play == null) return;
        if (data.Play.lobbyIntroDone) return;

        data.Play.lobbyIntroDone = true;
        UserDataManager.Instance.SaveAsync();
    }
}

/* [파일 노트]
 * 로비 씬 도입부(튜토리얼 복도)의 대사 진행을 총괄하는 씬 전용 싱글톤.
 *
 * [순서 보장 방식]
 * 1) 씬의 모든 ILobbyIntroStep 구현체를 모아 StepOrder 오름차순으로 정렬한다(정렬된 리스트 = 시나리오 순서).
 * 2) cursor 가 "다음에 발동해야 할 트리거"를 가리키며, TryFire() 는 cursor 위치의 트리거와
 *    호출자가 동일할 때만 참을 반환한다. 역주행으로 이전 트리거에 다시 닿거나,
 *    점프로 뒤쪽 트리거에 먼저 닿아도 순번이 맞지 않으므로 발동하지 않는다.
 * 3) 발동에 성공한 트리거는 스스로 one-shot 처리(콜라이더/컴포넌트 비활성화)하고 cursor 가 1 증가한다.
 * 4) 마지막 트리거까지 발동하면 FinishIntro() 로 도입부를 종료하고 세이브 플래그를 기록한다.
 *
 * [대사가 씹히는 문제]
 * 타이핑 중에 다음 트리거가 발동해도 보류하지 않는다(요구사항). DialogueManager.Advance() 는
 * 타이핑을 끊고 다음 줄로 넘어가므로, 트리거끼리는 한 줄을 다 읽을 만큼 충분히 떨어뜨려 배치하는 것을 전제로 한다.
 * 다만 트리거가 붙어 있어 즉시 연속 발동하는 사고를 막기 위해 minStepInterval(기본 0.25초) 쿨다운을 둔다.
 * 쿨다운으로 거절된 위치 트리거는 LobbyDialogueZone 의 retryWhileInside 옵션 덕분에
 * 플레이어가 영역 안에 있는 동안 계속 재시도하므로 대사가 통째로 유실되지는 않는다.
 *
 * [세이브 플래그]
 * UserData.PlayData.lobbyIntroDone 을 사용한다. Start 씬의 "새 게임" 버튼이
 * UserDataManager.ClearPlayData() 로 PlayData 를 통째로 새로 만들기 때문에
 * 새 게임에서는 자동으로 false(=도입부 재생), 이어하기에서는 true(=도입부 생략)가 된다.
 * 업적(AchievementData)은 새 게임에서도 초기화되지 않으므로 이 용도로는 적합하지 않다.
 * 테스트 중에는 인스펙터의 playIntro 를 끄거나 useSaveFlag 를 꺼서 세이브와 무관하게 강제할 수 있다.
 *
 * [주의]
 * - 트리거 목록/재생 여부 판정(EnsureResolved)은 SceneController 의 OnSceneLoadComplete 에서 이뤄진다.
 *   SceneController 없이 로비 씬을 단독 실행하는 경우를 대비해 첫 TryFire/IsNextStep 호출에서도 지연 판정한다.
 *   다만 이때 세이브 로드가 아직 안 끝났다면 Data 가 null 이라 도입부가 재생되는 쪽으로 판단한다.
 * - 트리거 수집은 FindObjectsInactive.Exclude 이므로 씬 로드 시점에 비활성화되어 있는 트리거는 목록에서 빠진다.
 *   런타임에 켜서 쓰려는 트리거가 있다면 활성 상태로 두어야 한다.
 * - DialogueManager 의 State 는 private 이라 "타이핑 중"인지 여부는 알 수 없고, public 인 IsPlaying 만 사용한다.
 *   그래서 대화가 선택지에서 종료된 뒤 남은 트리거가 발동하면 Advance() 대신 도입부를 정리하고 끝낸다.
 * - S03_LOBBY.csv 기준 대사 줄 수: lobby_02~lobby_10(9줄) + lobby_choice(1줄) = 트리거 10개면 선택지 직전까지 진행된다.
 *   선택지 UI 까지 트리거로 열려면 11번째 트리거가 필요하고, 그게 어색하면 DialogueManager 의
 *   allowDialogueSkip 을 켜서 마지막 한 번은 플레이어가 V 키로 넘기게 하는 편이 자연스럽다.
 */

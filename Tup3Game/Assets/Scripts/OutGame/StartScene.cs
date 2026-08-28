using System;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Rendering;

public class StartScene : MonoBehaviour
{
    internal StateMachine<StartScene> stateMachine;
    private void Awake()
    {
        stateMachine = new StateMachine<StartScene>();
        stateMachine.Setup(this, new Booting());
        
    }

    void Update()
    {
        stateMachine.Execute();
    }
}

internal class Booting : State<StartScene>
{
    private float timer = 1;
    public override void Enter(StartScene entity)
    {

    }

    public override void Execute(StartScene entity)
    {
        timer -= Time.deltaTime;
        if(timer <= 0) entity.stateMachine.ChangeState(new IntroCutscene() );
    }

    public override void Exit(StartScene entity)
    {

    }
}

internal class IntroCutscene : State<StartScene>
{
    // 마지막 Advance(23초)가 끝난 뒤 전환되도록 대사 타이밍보다 길게 잡는다
    private float timer = 24;
    private SpriteRenderer fader;
    private readonly List<Tween> delayedCalls = new List<Tween>();
    public override void Enter(StartScene entity)
    {
        Camera.main.transform.DOMoveX(0, 20f).SetEase(Ease.Linear);
        fader = entity.GetComponent<SpriteRenderer>();
        fader.DOFade(0, 1f);
        KeepSubtitlesOutOfPostProcessing();
        delayedCalls.Add(DOVirtual.DelayedCall(2, () =>
            DialogueManager.Current.StartDialogueFromCsv("S00_PROLOGUE")));
        delayedCalls.Add(DOVirtual.DelayedCall(8, () =>
            DialogueManager.Current.Advance()));
        delayedCalls.Add(DOVirtual.DelayedCall(15, () =>
            DialogueManager.Current.Advance()));
        delayedCalls.Add(DOVirtual.DelayedCall(23, () =>
            DialogueManager.Current.Advance()));
    }

    public override void Execute(StartScene entity)
    {
        timer -= Time.deltaTime;
        if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Escape)) timer = 0;
        if(timer <= 0) entity.stateMachine.ChangeState(new Menu());

    }

    private static void KeepSubtitlesOutOfPostProcessing()
    {
        if (DialogueManager.Current == null) return;

        Canvas canvas = DialogueManager.Current.GetComponentInParent<Canvas>();
        if (canvas != null) AspectRatioEnforcer.KeepOverlay(canvas.rootCanvas);
    }

    public override void Exit(StartScene entity)
    {
        foreach (var call in delayedCalls) call.Kill();
        delayedCalls.Clear();
        if (DialogueManager.Current != null) DialogueManager.Current.StopDialogue();
        Camera.main.transform.DOKill();
        fader.DOFade(1, 1f).OnComplete(()=>Camera.main.transform.position =  new Vector3(0, 8, -10));
    }
}

internal class Menu : State<StartScene>
{
    private const string NewGameSceneName = "Prologue";
    private const string ContinueSceneName = "Lobby";

    private SpriteRenderer fader;
    private Volume v;
    private TitleMenuView menuView;
    private EndingBadgeView badgeView;
    private AchievementsPanelView achievementsView;
    private Tween revealCall;

    public override void Enter(StartScene entity)
    {
        fader = entity.GetComponent<SpriteRenderer>();
        v = GameObject.FindAnyObjectByType<Volume>();

        var particles = GameObject.FindAnyObjectByType<ParticleSystem>();
        if (particles != null) particles.Stop();

        menuView = ResolveMenuView();
        menuView.NewGameRequested += StartNewGame;
        menuView.ContinueRequested += ContinueGame;
        menuView.AchievementsRequested += OpenAchievements;
        menuView.OptionsRequested += OpenOptions;
        menuView.QuitRequested += QuitGame;
        menuView.Hide();

        badgeView = ResolveBadgeView();
        badgeView.Hide();

        achievementsView = ResolveAchievementsView();
        achievementsView.CloseRequested += CloseAchievements;
        achievementsView.Hide();

        revealCall = DOVirtual.DelayedCall(1, () =>
        {
            if (fader != null) fader.DOFade(0, 2f).SetEase(Ease.Linear);
            if (v != null) v.gameObject.SetActive(false);
            if (menuView != null) menuView.Show(HasSavedProgress());
            if (badgeView != null) badgeView.Show();
        });
    }

    public override void Execute(StartScene entity)
    {

    }

    public override void Exit(StartScene entity)
    {
        if (revealCall != null) revealCall.Kill();
        revealCall = null;

        if (badgeView != null)
        {
            badgeView.Hide();
            badgeView = null;
        }

        if (achievementsView != null)
        {
            achievementsView.CloseRequested -= CloseAchievements;
            achievementsView.Hide();
            achievementsView = null;
        }

        if (menuView == null) return;

        menuView.NewGameRequested -= StartNewGame;
        menuView.ContinueRequested -= ContinueGame;
        menuView.AchievementsRequested -= OpenAchievements;
        menuView.OptionsRequested -= OpenOptions;
        menuView.QuitRequested -= QuitGame;
        menuView.Hide();
        menuView = null;
    }

    private static TitleMenuView ResolveMenuView()
    {
        var view = GameObject.FindAnyObjectByType<TitleMenuView>(FindObjectsInactive.Include);
        if (view != null) return view;

        return new GameObject("TitleMenuView").AddComponent<TitleMenuView>();
    }

    private static EndingBadgeView ResolveBadgeView()
    {
        var view = GameObject.FindAnyObjectByType<EndingBadgeView>(FindObjectsInactive.Include);
        if (view != null) return view;

        return new GameObject("EndingBadgeView").AddComponent<EndingBadgeView>();
    }

    private static AchievementsPanelView ResolveAchievementsView()
    {
        var view = GameObject.FindAnyObjectByType<AchievementsPanelView>(FindObjectsInactive.Include);
        if (view != null) return view;

        return new GameObject("AchievementsPanelView").AddComponent<AchievementsPanelView>();
    }

    private void OpenAchievements()
    {
        if (achievementsView == null)
        {
            achievementsView = ResolveAchievementsView();
            achievementsView.CloseRequested += CloseAchievements;
        }

        achievementsView.Show();
    }

    private void CloseAchievements()
    {
        if (achievementsView != null) achievementsView.Hide();
    }

    private static bool HasSavedProgress()
    {
        if (UserDataManager.Instance == null) return false;

        UserData data = UserDataManager.Instance.Data;
        if (data == null || data.Play == null) return false;

        if (data.Play.clearedBosses != BossFlag.None) return true;
        if (data.Play.health > 0f) return true;

        return data.Play.lobbyIntroDone;
    }

    private static void StartNewGame()
    {
        UserDataManager.Instance.ClearPlayData();
        SceneController.Instance.LoadScene(NewGameSceneName);
    }

    private static void ContinueGame()
    {
        SceneController.Instance.LoadScene(ContinueSceneName);
    }

    private static void OpenOptions()
    {
        PauseManager.Instance.ToggleOptionsOnly();
    }

    private static void QuitGame()
    {
        PauseManager.Instance.QuitGame();
    }
}

/* [파일 노트]
 * Start 씬 흐름: Booting(1초) → IntroCutscene → Menu.
 * IntroCutscene 은 카메라 20초 패닝 + S00_PROLOGUE 대사를 DelayedCall(2/8/15/23초)로 재생하고
 * 24초 뒤 Menu 로 전환된다. 아무 키나 누르면(Input.anyKeyDown, 마우스 포함) 즉시 스킵된다.
 * 단 ESC 는 스킵 키에서 제외했다 — Start 씬에서 ESC 는 PauseManager 의 옵션 패널 토글 전용이라
 * "옵션을 열었더니 인트로까지 스킵되는" 이중 동작을 막기 위함이다.
 * Exit 에서 DelayedCall 전부 Kill + 대화 강제 종료(StopDialogue) + 카메라 트윈 Kill 을 처리하므로
 * 스킵 시점과 무관하게 잔여 콜백이 남지 않는다.
 * Start 씬은 일시정지 개념이 없다(PauseManager 가 IsPaused 를 세우지 않고 옵션만 띄운다).
 *
 * [Menu 상태 — 타이틀 메뉴]
 * 표시는 TitleMenuView(글씨만 보이는 미니멀 뷰)가 맡고 이 상태가 로직을 갖는다. 뷰는 씬에 배치된
 * 인스턴스를 먼저 찾고 없을 때만 코드로 만든다(PauseManager 가 PauseMenuView 를 찾는 방식과 동일).
 * 메뉴는 기존 연출을 건드리지 않도록 페이드인이 시작되는 DelayedCall(1) 안에서 함께 띄운다.
 * 즉 "검은 화면 1초 → 배경이 2초에 걸쳐 밝아지는 동안 메뉴가 떠 있는" 순서다.
 * 페이드용 스프라이트(Manager 오브젝트의 SpriteRenderer)는 sortingOrder 1000 으로 메뉴 캔버스(800)보다
 * 위에 있어서, 메뉴도 이 페이드를 따라 같이 서서히 드러난다(별도 연출 코드가 필요 없다).
 * DelayedCall 은 revealCall 로 들고 있다가 Exit 에서 Kill 하므로 잔여 콜백이 남지 않는다.
 *
 * 항목별 동작 :
 *   - 게임 시작 : UserDataManager.ClearPlayData() → Prologue. 씬의 Start.cs 가 startButton 에
 *     걸어 두던 두 동작과 순서까지 동일하다(세이브 초기화가 먼저, 그 다음 씬 이동).
 *     ClearPlayData 는 메모리의 PlayData 만 새로 만들고, 디스크 기록은 씬 전환 시
 *     SceneController.LoadSceneRoutine 의 SaveAsync 가 처리한다 — 기존 경로 그대로다.
 *   - 이어하기 : Lobby 로 직행(Start.cs 의 resumeButton 과 동일). 진행이 있을 때만 표시한다.
 *   - 옵션 : PauseManager.ToggleOptionsOnly(). Start 씬의 ESC 와 완전히 같은 경로라 두 입력이
 *     같은 패널을 토글한다. 옵션 패널(sortingOrder 910)이 타이틀 메뉴(800) 위에 Dim 과 함께 떠서
 *     아래 항목 클릭을 막고, 닫으면 메뉴가 그대로 드러난다. ESC 라우팅에 손댈 필요가 없었다.
 *   - 게임 종료 : PauseManager.QuitGame().
 *
 * [이어하기 표시 조건 — HasSavedProgress]
 * UserDataManager 에는 "세이브 파일이 있는가"를 묻는 API 가 없다(LocalSaveBackend 는 파일이 없으면
 * null 을 돌려주고 UserDataManager 가 조용히 빈 UserData 로 대체한다). 게다가 SceneController 가
 * 씬을 옮길 때마다 SaveAsync 를 부르므로 "파일 존재"는 새 게임을 한 번 시작하기만 해도 참이 되어
 * 진행도 판단 기준이 못 된다. 그래서 PlayData 의 내용으로 판단한다.
 *   - clearedBosses != None : 보스를 하나라도 잡았다.
 *   - health > 0            : Lobby.OnSceneExit 이 로비를 떠날 때 기록하는 값. 새 PlayData 는 0 이므로
 *                             0 보다 크다는 것은 로비를 최소 한 번 거쳤다는 뜻이고, 이어하기가 복원하는
 *                             바로 그 상태(위치/체력/스킬)가 세이브에 들어 있다는 뜻이다.
 *   - lobbyIntroDone        : 로비 도입부를 끝냈다.
 * 셋 다 ClearPlayData() 로 초기화되므로 새 게임 직후에는 다시 숨겨진다.
 *
 * [도전과제 + 엔딩 배지 (2026-08-29 유저 요청)]
 * Menu 상태가 뷰 두 개를 더 붙든다. 둘 다 씬에 배치본이 있으면 그것을 쓰고 없으면 코드로
 * 만드는 폴백 구조라(ResolveMenuView 와 동일) Start.unity 를 수정할 필요가 없다.
 *   - EndingBadgeView (오른쪽 위 구석, sortingOrder 780)
 *     엔딩2·3·4 클리어 여부를 배지 3개로 보여 준다. 타이틀 메뉴와 같은 DelayedCall(1) 안에서
 *     Show() 하므로 인트로 동안에는 보이지 않고 메뉴와 함께 페이드인된다. 상호작용이 없어
 *     (Button 없음 + 전 Graphic raycastTarget=false) 메뉴 조작을 가로채지 않는다.
 *   - AchievementsPanelView (중앙 모달, sortingOrder 905)
 *     타이틀 메뉴의 새 항목 "도전과제"(TitleMenuView.AchievementsRequested)로 열리고
 *     "뒤로" 또는 ESC 로 닫힌다. 열려 있는 동안 PauseManager.BlockPause() 가 걸려 ESC 가
 *     옵션 패널까지 여는 이중 동작이 나지 않는다(ConfirmDialogView 와 같은 방식).
 * 두 뷰 모두 Exit 에서 구독 해제 + Hide 한다. 배지는 Show 때마다 업적을 다시 읽으므로
 * 엔딩을 마치고 "시작 화면으로" 돌아오면(= Ending.cs 가 업적 해금 후 Start 씬 로드)
 * 곧바로 밝아진 배지가 보인다.
 */
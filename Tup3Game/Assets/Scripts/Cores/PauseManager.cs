using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseManager : Singleton<PauseManager>
{
    public static bool IsPaused { get; private set; }

    const string StartSceneName = "Start";
    const string PrologueSceneName = "Prologue";

    PauseMenuView menuView;
    OptionsPanelView optionsView;

    readonly List<Animator> frozenAnimators = new();
    readonly List<float> frozenAnimatorSpeeds = new();
    readonly List<ParticleSystem> pausedParticles = new();

    bool optionsOpen;
    bool optionsOnlyMode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        IsPaused = false;
        _ = Instance;
    }

    protected override void OnAwake()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        if (IsPaused) IsPaused = false;
    }

    void OnActiveSceneChanged(Scene previous, Scene next)
    {
        if (IsPaused) ForceResetPauseState();
        else if (optionsOpen) CloseOptionsSilently();
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        HandleEscape();
    }

    void HandleEscape()
    {
        if (SceneController.IsTransitioning) return;

        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == PrologueSceneName) return;

        if (sceneName == StartSceneName)
        {
            ToggleOptionsOnly();
            return;
        }

        if (!IsPaused)
        {
            Pause();
            return;
        }

        if (optionsOpen)
        {
            CloseOptionsToMenu();
            return;
        }

        Resume();
    }

    public void Pause()
    {
        if (IsPaused) return;

        EnsureViews();
        optionsOpen = false;
        optionsOnlyMode = false;
        IsPaused = true;

        FreezeWorld();
        optionsView.Hide();
        menuView.Show();
    }

    public void Resume()
    {
        if (!IsPaused) return;

        optionsOpen = false;
        if (optionsView != null) optionsView.Hide();
        if (menuView != null) menuView.Hide();

        UnfreezeWorld();
        IsPaused = false;
        VolumeSettings.SaveIfDirty();
    }

    public void ToggleOptionsOnly()
    {
        if (IsPaused) return;

        EnsureViews();

        if (optionsOpen)
        {
            CloseOptionsOnly();
            return;
        }

        optionsOnlyMode = true;
        optionsOpen = true;
        optionsView.Show(VolumeSettings.Bgm, VolumeSettings.Sfx);
    }

    void OpenOptionsFromMenu()
    {
        optionsOpen = true;
        menuView.Hide();
        optionsView.Show(VolumeSettings.Bgm, VolumeSettings.Sfx);
    }

    void CloseOptionsToMenu()
    {
        optionsOpen = false;
        optionsView.Hide();
        menuView.Show();
        VolumeSettings.SaveIfDirty();
    }

    void CloseOptionsOnly()
    {
        if (!optionsOpen) return;
        optionsOpen = false;
        optionsOnlyMode = false;
        optionsView.Hide();
        VolumeSettings.SaveIfDirty();
    }

    void CloseOptionsSilently()
    {
        optionsOpen = false;
        optionsOnlyMode = false;
        if (optionsView != null) optionsView.Hide();
        VolumeSettings.SaveIfDirty();
    }

    void OnOptionsCloseRequested()
    {
        if (optionsOnlyMode) CloseOptionsOnly();
        else if (IsPaused) CloseOptionsToMenu();
    }

    void EnsureViews()
    {
        EnsureEventSystem();

        if (menuView == null)
        {
            menuView = FindAnyObjectByType<PauseMenuView>(FindObjectsInactive.Include);
            if (menuView == null)
            {
                var go = new GameObject("PauseMenuView");
                go.transform.SetParent(transform, false);
                menuView = go.AddComponent<PauseMenuView>();
            }
            menuView.ResumeRequested += Resume;
            menuView.OptionsRequested += OpenOptionsFromMenu;
            menuView.Hide();
        }

        if (optionsView == null)
        {
            optionsView = FindAnyObjectByType<OptionsPanelView>(FindObjectsInactive.Include);
            if (optionsView == null)
            {
                var go = new GameObject("OptionsPanelView");
                go.transform.SetParent(transform, false);
                optionsView = go.AddComponent<OptionsPanelView>();
            }
            optionsView.BgmChanged += VolumeSettings.SetBgm;
            optionsView.SfxChanged += VolumeSettings.SetSfx;
            optionsView.CloseRequested += OnOptionsCloseRequested;
            optionsView.Hide();
        }
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        if (FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    void FreezeWorld()
    {
        DOTween.PauseAll();

        frozenAnimators.Clear();
        frozenAnimatorSpeeds.Clear();
        foreach (var animator in FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            if (animator == null) continue;
            frozenAnimators.Add(animator);
            frozenAnimatorSpeeds.Add(animator.speed);
            animator.speed = 0f;
        }

        pausedParticles.Clear();
        foreach (var particle in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            if (particle == null || !particle.isPlaying) continue;
            pausedParticles.Add(particle);
            particle.Pause(false);
        }
    }

    void UnfreezeWorld()
    {
        for (int i = 0; i < frozenAnimators.Count; i++)
        {
            if (frozenAnimators[i] == null) continue;
            frozenAnimators[i].speed = frozenAnimatorSpeeds[i];
        }
        frozenAnimators.Clear();
        frozenAnimatorSpeeds.Clear();

        for (int i = 0; i < pausedParticles.Count; i++)
        {
            if (pausedParticles[i] == null) continue;
            pausedParticles[i].Play(false);
        }
        pausedParticles.Clear();

        DOTween.PlayAll();
    }

    void ForceResetPauseState()
    {
        optionsOpen = false;
        optionsOnlyMode = false;
        if (optionsView != null) optionsView.Hide();
        if (menuView != null) menuView.Hide();

        frozenAnimators.Clear();
        frozenAnimatorSpeeds.Clear();
        pausedParticles.Clear();
        DOTween.PlayAll();

        IsPaused = false;
        VolumeSettings.SaveIfDirty();
    }

    public static IEnumerator WaitWhilePaused()
    {
        while (IsPaused) yield return null;
    }
}

/* [파일 노트]
 *
 * 1) 역할
 *    전역 일시정지 로직 싱글톤. Time.timeScale 은 절대 건드리지 않는다(유저 확정 사항).
 *    정지는 다음 세 가지 조합으로 이뤄진다.
 *      - 정적 플래그 PauseManager.IsPaused : Playermovement / 보스(Soil·Fire·Water·Gold) /
 *        투사체 / BossRoom / DialogueManager / UserInput 등이 Update 첫 줄에서 검사해 스스로 멈춘다.
 *      - DOTween.PauseAll() / PlayAll() : DOVirtual.DelayedCall 로 예약된 보스 패턴 소환,
 *        이동 트윈(Fire 의 DOMove 등), 수위 상승 트윈까지 전부 정지/재개된다.
 *      - Animator 전체 speed=0(이전 값 기억 후 복원) + 재생 중 ParticleSystem Pause/Play.
 *    이 때문에 일시정지 UI(PauseMenuView/OptionsPanelView)는 트윈·애니메이터를 쓰지 않는다.
 *
 * 2) 부트스트랩
 *    [RuntimeInitializeOnLoadMethod] 로 첫 씬 로드 직후 자동 생성되므로 씬/프리팹 배치가 필요 없다.
 *    Singleton 베이스가 DontDestroyOnLoad 처리한다. 뷰 오브젝트도 이 오브젝트의 자식으로 만들어
 *    씬 전환 후에도 재사용된다. 씬에 PauseMenuView/OptionsPanelView 를 직접 배치해 두면
 *    코드 생성 대신 그것을 찾아 쓴다(=UI 교체 지점).
 *
 * 3) ESC 라우팅 (HandleEscape)
 *    - 씬 전환 중(SceneController.IsTransitioning) : 무시.
 *    - Prologue 씬 : ESC 가 스킵 키로 이미 쓰이므로 일시정지 기능 자체를 비활성(충돌 회피).
 *    - Start 씬 : 일시정지 개념 없이 옵션 패널만 토글(ToggleOptionsOnly). IsPaused 는 세우지 않아
 *      타이틀 연출(트윈/타이머)이 계속 돈다.
 *    - 그 외 씬(Lobby, Boss_ 4종, Styx, Ending) : ESC = 일시정지 토글.
 *      일시정지 중 옵션이 열려 있으면 ESC 는 "옵션 닫기(메뉴로 복귀)"로 동작한다.
 *
 * 4) 볼륨 저장 시점
 *    슬라이더 조작은 VolumeSettings 가 SettingsData + AudioManager 믹서에 즉시 반영하고,
 *    옵션 패널이 닫히거나(뒤로/ESC) 일시정지가 해제될 때 SaveIfDirty 로 디스크 저장한다.
 *
 * 5) 안전장치
 *    일시정지 중 씬이 바뀌는 경우(이론상 없지만) activeSceneChanged 에서 ForceResetPauseState 로
 *    상태를 정리한다. WaitWhilePaused() 는 코루틴에서 일시정지 대기가 필요할 때 쓰는 공용 헬퍼.
 *
 * 6) 알려진 한계(목표: "일시정지 중 피해·진행 없음", 프레임 단위 완전 정지는 목표 아님)
 *    - WaitForSeconds / Invoke / Destroy(obj, t) / PoolManager 지연 반납 타이머는 계속 흐른다.
 *      → 일시정지 중 투사체가 수명이 다해 사라질 수 있다(플레이어에게 불리하지 않음).
 *    - 스킬 버프 지속시간·쿨타임(WaitForSeconds 기반)도 실시간으로 흐른다.
 *    - 피해는 PlayerKnockBack.TakeHit / PlayerHealth.TakeDamage 게이트로 전부 차단된다.
 *    - DOTween.PlayAll() 은 "일시정지 이전부터 수동으로 Pause 된 트윈"도 재개시키는데,
 *      현재 코드베이스에는 수동 Pause 트윈이 없어 문제가 되지 않는다.
 */

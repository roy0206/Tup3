using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneBgm : Singleton<SceneBgm>
{
    static readonly Dictionary<string, string> SceneToBgm = new()
    {
        { "Start", "BGM_Title" },
        { "Prologue", "BGM_Prologue" },
        { "Lobby", "BGM_Lobby" },
        { "Boss_Soil", "BGM_Soil" },
        { "Boss_Water", "BGM_Water" },
        { "Boss_Fire", "BGM_Fire" },
        { "Boss_Gold", "BGM_Gold" },
        { "Styx", "BGM_Styx" },
        { "Ending", "BGM_Ending2" },
        { "Ending1", "BGM_Ending1" },
        { "Ending2", "BGM_Ending2" },
        { "Ending3", "BGM_Ending3" },
        { "Ending4", "BGM_Ending4" }
    };

    AudioManager audioManager;
    string currentBgm;
    Coroutine playRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        Instance.ApplyScene(SceneManager.GetActiveScene().name);
    }

    protected override void OnAwake()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void OnActiveSceneChanged(Scene previous, Scene next)
    {
        ApplyScene(next.name);
    }

    void ApplyScene(string sceneName)
    {
        SceneToBgm.TryGetValue(sceneName, out string nextBgm);

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (!string.IsNullOrEmpty(currentBgm) && currentBgm != nextBgm)
        {
            var audio = ResolveAudioManager();
            if (audio != null) audio.StopBGM(currentBgm);
            currentBgm = null;
        }

        if (string.IsNullOrEmpty(nextBgm)) return;

        playRoutine = StartCoroutine(PlayWhenReady(nextBgm));
    }

    IEnumerator PlayWhenReady(string bgmName)
    {
        while (true)
        {
            var audio = ResolveAudioManager();
            if (audio != null && audio.SoundsReady) break;
            yield return null;
        }

        var manager = ResolveAudioManager();
        if (manager != null)
        {
            manager.PlayBGM(bgmName);
            currentBgm = bgmName;
        }
        playRoutine = null;
    }

    AudioManager ResolveAudioManager()
    {
        if (audioManager == null)
            audioManager = FindAnyObjectByType<AudioManager>();
        return audioManager;
    }
}

/* [파일 노트]
 *
 * 1) 역할
 *    씬별 BGM 재생을 한 곳에서 관리하는 싱글톤. PauseManager 와 같은
 *    [RuntimeInitializeOnLoadMethod] 부트스트랩으로 첫 씬 로드 직후 자동 생성되므로
 *    씬/프리팹 배치가 필요 없다(Singleton 베이스가 DontDestroyOnLoad 처리).
 *
 * 2) 동작
 *    - SceneToBgm : 씬 이름 → AudioManager 재생 키(= Addressables 주소 = 클립 이름) 매핑.
 *      AudioManager 는 Addressables 라벨 "Sound" 프리로드 후 clip.name 으로 사전을 만들기
 *      때문에 mp3 파일명(BGM_Title.mp3 등)과 주소, 재생 키를 전부 동일하게 맞췄다.
 *    - 활성 씬 변경(activeSceneChanged) 시 이전 BGM 레이어를 StopBGM 으로 정지하고
 *      매핑된 곡을 PlayBGM(루프)으로 재생한다.
 *    - 매핑에 없는 씬(Loading, Test 등)은 재생 중이던 BGM 정지만 수행한다.
 *    - 같은 곡이 계속되는 씬 재로드(보스 재도전 등)에서는 StopBGM 을 건너뛰고
 *      PlayBGM 이 "이미 재생 중이면 유지" 처리를 하므로 곡이 끊기지 않는다.
 *
 * 3) 안전장치
 *    - AudioManager 의 Addressables 프리로드(SoundsReady)가 끝나기 전에는 코루틴으로
 *      대기했다가 재생한다. 대기 중 씬이 또 바뀌면 코루틴을 끊고 새 매핑으로 갱신.
 *    - AudioManager.Instance 대신 FindAnyObjectByType 조회를 쓰는 이유:
 *      Core 프리팹이 없는 씬에서 Instance 접근 시 믹서/라벨이 비어 있는 빈
 *      AudioManager 가 자동 생성되는 부작용을 막기 위함(없으면 찾을 때까지 대기만 한다).
 *
 * 4) 채택 곡 원제 (CC-BY 4.0, Kevin MacLeod / incompetech.com)
 *    BGM_Title=Eastern Thought, BGM_Prologue=Rites, BGM_Lobby=Meditation Impromptu 01,
 *    BGM_Soil=Undaunted, BGM_Water=Aquarium, BGM_Fire=Ready Aim Fire,
 *    BGM_Gold=Steel Rods, BGM_Styx=Final Battle of the Dark Wizards,
 *    BGM_Ending1=For the Fallen (패배·배드 — 장송/추모),
 *    BGM_Ending2=Rains Will Fall (코인4 승리·트루 — 기존 BGM_Ending 을 개명),
 *    BGM_Ending3=Promises to Keep (코인1~3 승리 — 담담/씁쓸),
 *    BGM_Ending4=Ghostpocalypse - 6 Crossing the Threshold (코인0 승리 — 공허/불길)
 *
 * 5) 구 "Ending" 씬 매핑
 *    단일 Ending 씬은 엔딩별 전용 씬(Ending1~4)으로 대체되어 폐기 예정이지만,
 *    빌드에 남아 있는 동안 무음이 되지 않도록 BGM_Ending2(트루 엔딩 곡)로 연결해 두었다.
 *    씬을 빌드 세팅에서 제거하면 이 항목도 지워도 된다.
 */

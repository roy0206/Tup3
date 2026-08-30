using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StyxIntroSceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Styx.unity";
    private const string ScenePath = "Assets/Scenes/StyxIntro.unity";
    private const string SourceSceneSOPath = "Assets/Scenes/Styx.asset";
    private const string SceneSOPath = "Assets/Scenes/StyxIntro.asset";
    private const string CorePrefabPath = "Assets/Prefabs/Core.prefab";
    private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
    private const string TargetSceneName = "StyxIntro";
    private const string DirectorObjectName = "StyxIntroDirector";

    private static readonly List<string> report = new List<string>();
    private static readonly List<string> todo = new List<string>();

    [MenuItem("Tools/Tup3/Setup Styx Intro Scene (StyxIntro)", false, 14)]
    public static void SetupStyxIntroScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (!File.Exists(SourceScenePath))
        {
            EditorUtility.DisplayDialog("Styx 씬 없음", $"'{SourceScenePath}' 를 찾지 못했습니다.", "확인");
            return;
        }

        report.Clear();
        todo.Clear();

        EnsureSceneCopy();
        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("씬 복사 실패", $"'{ScenePath}' 를 만들지 못했습니다. 콘솔 로그를 확인하세요.", "확인");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        ConvertToIntroScene(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);
        report.Add(saved ? "씬 저장 완료" : "씬 저장 실패 — 수동으로 저장하세요");

        EnsureSceneSO();
        RegisterSceneSOInCorePrefab();
        EnsureBuildSettingsEntry();

        AssetDatabase.SaveAssets();
        ShowReport();
    }

    // ── 씬 복사 ──────────────────────────────────────────────────────────────

    private static void EnsureSceneCopy()
    {
        if (File.Exists(ScenePath))
        {
            report.Add($"'{ScenePath}' 이미 존재 — 복사 생략, 기존 씬을 갱신합니다");
            return;
        }

        if (!AssetDatabase.CopyAsset(SourceScenePath, ScenePath))
        {
            report.Add($"씬 복사 실패: {SourceScenePath} → {ScenePath}");
            return;
        }

        AssetDatabase.Refresh();
        report.Add($"Styx.unity → StyxIntro.unity 복사 완료");
    }

    // ── 씬 개조: 전투 제거 + 조우 연출 배선 ─────────────────────────────────

    private static void ConvertToIntroScene(Scene scene)
    {
        // 1) FinalBossRoom(전투 상태 머신) 제거, 그 자리에 StyxIntro 배치
        GameObject directorGo = null;
        foreach (FinalBossRoom room in Object.FindObjectsOfType<FinalBossRoom>(true))
        {
            directorGo = room.gameObject;
            Object.DestroyImmediate(room);
            report.Add($"FinalBossRoom 컴포넌트 제거 ({directorGo.name})");
        }

        if (directorGo == null)
        {
            StyxIntro existing = Object.FindObjectOfType<StyxIntro>(true);
            directorGo = existing != null ? existing.gameObject : null;
        }

        if (directorGo == null)
        {
            directorGo = new GameObject(DirectorObjectName);
            report.Add($"'{DirectorObjectName}' 오브젝트 생성");
        }
        else if (directorGo.name == "FinalBossRoom")
        {
            directorGo.name = DirectorObjectName;
            report.Add($"오브젝트 이름 변경: FinalBossRoom → {DirectorObjectName}");
        }

        if (directorGo.GetComponent<StyxIntro>() == null)
        {
            directorGo.AddComponent<StyxIntro>();
            report.Add($"StyxIntro 컴포넌트 추가 ({directorGo.name})");
        }
        else
        {
            report.Add($"StyxIntro 컴포넌트 이미 존재 ({directorGo.name})");
        }

        // 2) 보스는 연출용 배경 — AI 정지 (StyxIntro 가 런타임에도 다시 끄지만 씬에서부터 꺼 둔다)
        foreach (BossBase boss in Object.FindObjectsOfType<BossBase>(true))
        {
            if (boss.enabled)
            {
                boss.enabled = false;
                report.Add($"BossBase 비활성화 ({boss.gameObject.name})");
            }
        }

        // 3) 전투 UI 정리
        foreach (BossHealthView view in Object.FindObjectsOfType<BossHealthView>(true))
        {
            if (view.gameObject.activeSelf)
            {
                view.gameObject.SetActive(false);
                report.Add($"BossHealthView 비활성화 ({view.gameObject.name})");
            }
        }

        // 4) 패배 컷씬(전투 전용) 정리 — 남아 있어도 재생 주체가 없지만 혼동 방지 차원
        foreach (DefeatCutscene cutscene in Object.FindObjectsOfType<DefeatCutscene>(true))
        {
            if (cutscene.enabled)
            {
                cutscene.enabled = false;
                report.Add($"DefeatCutscene 비활성화 ({cutscene.gameObject.name})");
            }
        }

        // 5) 상호작용 시스템 준비 + 보스에 대사 시작 상호작용 배선
        EnsureInteractionSystem(scene);
        EnsureBossInteraction(scene, directorGo);

        if (Object.FindObjectOfType<Playermovement>() == null)
            todo.Add("씬에 Player 가 없습니다 — Player 프리팹을 배치하세요");
        if (Object.FindObjectOfType<DialogueManager>(true) == null)
            todo.Add("씬에 DialogueManager(DialogueUI 프리팹)가 없습니다 — 배치하세요");
    }

    // ── 상호작용 시스템: 로비 씬에서 InteractionManager / InteractionIcon 복사 ─

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    private static void EnsureInteractionSystem(Scene scene)
    {
        bool hasManager = FindComponentInScene<InteractionManager>(scene) != null;
        bool hasView = FindComponentInScene<InteractionView>(scene) != null;

        if (hasManager && hasView)
        {
            report.Add("InteractionManager / InteractionView 이미 존재");
            return;
        }

        if (!File.Exists(LobbyScenePath))
        {
            todo.Add($"'{LobbyScenePath}' 가 없어 InteractionManager/InteractionIcon 을 복사하지 못했습니다 — 직접 배치하세요");
            return;
        }

        Scene lobby = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Additive);
        try
        {
            if (!hasManager)
            {
                InteractionManager sourceManager = FindComponentInScene<InteractionManager>(lobby);
                if (sourceManager != null)
                {
                    GameObject clone = Object.Instantiate(sourceManager.gameObject);
                    clone.name = sourceManager.gameObject.name;
                    SceneManager.MoveGameObjectToScene(clone, scene);
                    report.Add("InteractionManager 복사 (Lobby → StyxIntro)");
                }
                else todo.Add("Lobby 씬에서 InteractionManager 를 찾지 못했습니다 — 직접 배치하세요");
            }

            if (!hasView)
            {
                InteractionView sourceView = FindComponentInScene<InteractionView>(lobby);
                if (sourceView != null)
                {
                    // InteractionIcon 은 Canvas 자식이므로 루트(Canvas)째로 복사한다
                    GameObject sourceRoot = sourceView.transform.root.gameObject;
                    GameObject clone = Object.Instantiate(sourceRoot);
                    clone.name = sourceRoot.name;
                    SceneManager.MoveGameObjectToScene(clone, scene);
                    report.Add($"InteractionIcon 복사 (Lobby '{sourceRoot.name}' → StyxIntro)");
                }
                else todo.Add("Lobby 씬에서 InteractionView 를 찾지 못했습니다 — 직접 배치하세요");
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(lobby, true);
        }
    }

    private static void EnsureBossInteraction(Scene scene, GameObject directorGo)
    {
        BossBase boss = FindComponentInScene<BossBase>(scene);
        if (boss == null)
        {
            todo.Add("씬에서 보스(BossBase)를 찾지 못해 StyxIntroInteraction 을 배선하지 못했습니다");
            return;
        }

        StyxIntroInteraction interaction = boss.GetComponent<StyxIntroInteraction>();
        if (interaction == null)
        {
            interaction = boss.gameObject.AddComponent<StyxIntroInteraction>();
            report.Add($"StyxIntroInteraction 추가 ({boss.gameObject.name})");
        }
        else
        {
            report.Add($"StyxIntroInteraction 이미 존재 ({boss.gameObject.name})");
        }

        SerializedObject serialized = new SerializedObject(interaction);
        serialized.FindProperty("director").objectReferenceValue = directorGo.GetComponent<StyxIntro>();

        SerializedProperty distance = serialized.FindProperty("interactionDistance");
        if (distance.floatValue <= 0f) distance.floatValue = 3.5f;

        SerializedProperty duration = serialized.FindProperty("interactionDuration");
        if (duration.floatValue <= 0f) duration.floatValue = 0.4f;

        SerializedProperty text = serialized.FindProperty("interactionText");
        if (string.IsNullOrEmpty(text.stringValue)) text.stringValue = "말을 건다";

        serialized.FindProperty("interactOnce").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        report.Add("StyxIntroInteraction 배선 완료 (director/거리/홀드 시간)");
    }

    // ── SceneSO 생성 (Styx.asset 값 복사, targetSceneName 만 교체) ──────────

    private static void EnsureSceneSO()
    {
        SceneSO so = AssetDatabase.LoadAssetAtPath<SceneSO>(SceneSOPath);

        if (so == null)
        {
            SceneSO source = AssetDatabase.LoadAssetAtPath<SceneSO>(SourceSceneSOPath);
            so = source != null ? Object.Instantiate(source) : ScriptableObject.CreateInstance<SceneSO>();
            AssetDatabase.CreateAsset(so, SceneSOPath);
            report.Add(source != null
                ? "StyxIntro.asset 생성 (Styx.asset 설정 복사)"
                : "StyxIntro.asset 생성 (Styx.asset 이 없어 기본값 사용)");
        }
        else
        {
            report.Add("StyxIntro.asset 이미 존재 — targetSceneName 만 확인");
        }

        if (so.targetSceneName != TargetSceneName)
        {
            so.targetSceneName = TargetSceneName;
            EditorUtility.SetDirty(so);
        }
        so.name = TargetSceneName;
    }

    // ── Core.prefab 의 SceneController.sceneConfigs 에 등록 ─────────────────

    private static void RegisterSceneSOInCorePrefab()
    {
        SceneSO so = AssetDatabase.LoadAssetAtPath<SceneSO>(SceneSOPath);
        if (so == null)
        {
            todo.Add("StyxIntro.asset 로드 실패 — SceneController 등록을 건너뜀");
            return;
        }

        if (!File.Exists(CorePrefabPath))
        {
            todo.Add($"'{CorePrefabPath}' 가 없습니다 — SceneController.sceneConfigs 에 StyxIntro.asset 을 직접 추가하세요");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(CorePrefabPath);
        try
        {
            SceneController controller = root.GetComponentInChildren<SceneController>(true);
            if (controller == null)
            {
                todo.Add("Core.prefab 에서 SceneController 를 찾지 못했습니다 — sceneConfigs 에 StyxIntro.asset 을 직접 추가하세요");
                return;
            }

            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty list = serialized.FindProperty("sceneConfigs");

            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == so)
                {
                    report.Add("SceneController.sceneConfigs 에 이미 등록됨");
                    return;
                }
            }

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = so;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, CorePrefabPath);
            report.Add("Core.prefab 의 SceneController.sceneConfigs 에 StyxIntro.asset 등록");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ── Build Settings 등록 ─────────────────────────────────────────────────

    private static void EnsureBuildSettingsEntry()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == ScenePath))
        {
            report.Add("Build Settings 에 이미 등록됨");
            return;
        }

        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        report.Add("Build Settings 에 StyxIntro.unity 추가");
    }

    // ── 결과 보고 ───────────────────────────────────────────────────────────

    private static void ShowReport()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[StyxIntroSceneBuilder] StyxIntro 조우 씬 구성 완료.");
        foreach (string line in report) sb.AppendLine("  · " + line);
        if (todo.Count > 0)
        {
            sb.AppendLine("남은 수동 작업:");
            foreach (string line in todo) sb.AppendLine("  ! " + line);
        }
        Debug.Log(sb.ToString());

        EditorUtility.DisplayDialog("StyxIntro 씬 구성",
            "StyxIntro(최종보스 첫 조우) 씬 세팅을 완료했습니다.\n\n" +
            "· Styx 씬 복사 → FinalBossRoom 제거, StyxIntro 배선\n" +
            "· 보스 AI/체력바/패배컷씬 비활성화\n" +
            "· 보스 상호작용(V 홀드)으로 대사 시작 — InteractionManager/Icon 은 Lobby 에서 복사\n" +
            "· SceneSO 생성 + Core.prefab 등록 + Build Settings 등록\n" +
            (todo.Count > 0 ? $"\n남은 수동 작업 {todo.Count}건 — 콘솔 로그를 확인하세요." : "\n남은 수동 작업 없음."),
            "확인");
    }
}

/* [파일 노트 — 최종보스 첫 조우(StyxIntro) 씬 빌더]
 *
 * 실행: Tools / Tup3 / Setup Styx Intro Scene (StyxIntro)  — 몇 번을 다시 실행해도 중복 생성 없이 갱신(멱등).
 *
 * 하는 일:
 *   1. Styx.unity 를 StyxIntro.unity 로 복사한다(이미 있으면 기존 씬을 갱신만 한다 — 유저가 씬을
 *      손본 뒤 다시 실행해도 맵 수정이 날아가지 않는다).
 *   2. 전투 요소를 걷어낸다: FinalBossRoom 컴포넌트 제거(오브젝트는 StyxIntroDirector 로 개명),
 *      BossBase(FinalBoss).enabled=false, BossHealthView 오브젝트 비활성, DefeatCutscene 비활성.
 *      보스 비주얼(프리팹 인스턴스)·맵·물·카메라는 Styx 그대로 남는다.
 *   3. 그 자리에 StyxIntro 컴포넌트를 배선한다(대사 재생 → styxIntroDone 저장 → 로비 복귀).
 *      대사 시작은 보스 상호작용이다 (2026-08-31 변경): 보스(BossBase) 오브젝트에 StyxIntroInteraction 을
 *      붙여 director/거리(3.5)/홀드(0.4초)를 채우고, 상호작용 인프라(InteractionManager 오브젝트와
 *      InteractionIcon 이 든 Canvas)는 Lobby 씬을 잠시 추가로 열어 통째로 복사해 온다.
 *      거리·홀드·표시문구는 이미 값이 있으면 덮지 않으므로 씬에서 조정한 값이 보존된다.
 *   4. StyxIntro.asset(SceneSO) 을 Styx.asset 값 복사로 만들고 targetSceneName 만 "StyxIntro" 로 바꾼다.
 *   5. Core.prefab 의 SceneController.sceneConfigs 리스트에 등록한다(SerializedObject 로 프리팹 에셋 직접 수정).
 *      이 등록이 없으면 SceneController.LoadScene("StyxIntro") 가 에러를 내고 무시된다.
 *   6. EditorBuildSettings 에 씬을 추가한다.
 *
 * 로비 쪽 작업은 이 빌더가 하지 않는다 — Lobby.cs 가 이름 기반으로 벽을 찾으므로,
 * 로비 씬에 토보스 게이트 타일맵 2개('SoilGateWall1'/'SoilGateWall2', 또는 Lobby 컴포넌트의
 * soilGateWalls 리스트에 직접 할당)만 준비하면 된다. FIrstWall 은 이미 씬에 있다.
 *
 * 씬 복사에 AssetDatabase.CopyAsset 을 쓰므로 새 GUID 가 발급된다 — Styx 씬을 참조하는
 * 기존 에셋에는 영향이 없다.
 */

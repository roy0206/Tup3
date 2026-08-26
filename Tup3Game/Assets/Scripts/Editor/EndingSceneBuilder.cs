using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class EndingSceneBuilder
{
    private const string SceneFolder = "Assets/Scenes";
    private const string TemplateSceneSOPath = "Assets/Scenes/Ending.asset";
    private const string CorePrefabPath = "Assets/Prefabs/Core.prefab";
    private const string DialogueUIPrefabPath = "Assets/Prefabs/DialogueUI.prefab";
    private const string FontPath = "Assets/GameAssets/Fonts/PRETENDARD-REGULAR SDF.asset";

    private struct EndingDef
    {
        public string sceneName;
        public string dialogueFile;
        public string dialogueStartId;
        public string summary;
        public string defaultCredits;
    }

    private static readonly EndingDef[] Endings =
    {
        new EndingDef
        {
            sceneName = "Ending1", dialogueFile = "S11_ENDING_1", dialogueStartId = "end1_01",
            summary = "패배 · 배드 엔딩",
            defaultCredits = "— Ending 1 · 패배 (배드 엔딩) —\n\n(작성중)"
        },
        new EndingDef
        {
            sceneName = "Ending2", dialogueFile = "S12_ENDING_2", dialogueStartId = "end2_01",
            summary = "코인4 승리 · 트루 엔딩",
            defaultCredits = "— Ending 2 · 의지를 모두 지킨 승리 (트루 엔딩) —\n\n(작성중)"
        },
        new EndingDef
        {
            sceneName = "Ending3", dialogueFile = "S14_ENDING_3", dialogueStartId = "end3_01",
            summary = "코인1~3 승리",
            defaultCredits = "— Ending 3 · 의지를 일부 잃은 승리 —\n\n(작성중)"
        },
        new EndingDef
        {
            sceneName = "Ending4", dialogueFile = "S15_ENDING_4", dialogueStartId = "end4_01",
            summary = "코인0 승리",
            defaultCredits = "— Ending 4 · 의지를 모두 잃은 승리 —\n\n(작성중)"
        },
    };

    private static readonly List<string> report = new List<string>();

    [MenuItem("Tools/Tup3/Setup Ending Scenes", false, 14)]
    public static void SetupEndingScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        report.Clear();

        EnsureDialogueUIPrefab();

        foreach (var def in Endings)
            BuildEndingScene(def);

        var sceneSOs = new List<SceneSO>();
        foreach (var def in Endings)
        {
            var so = EnsureSceneSO(def.sceneName);
            if (so != null) sceneSOs.Add(so);
        }

        EnsureInBuildSettings();
        RegisterToCorePrefab(sceneSOs);

        AssetDatabase.SaveAssets();
        LogSummary();
    }

    private static void BuildEndingScene(EndingDef def)
    {
        string scenePath = $"{SceneFolder}/{def.sceneName}.unity";
        Scene scene;
        bool sceneCreated = !File.Exists(scenePath);

        if (sceneCreated)
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        else
        {
            scene = SceneManager.GetActiveScene();
            if (scene.path != scenePath)
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        EnsureCamera(scene);
        EnsureEventSystem(scene);
        Button returnButton = EnsureCanvasWithReturnButton(scene);
        DialogueManager dm = EnsureDialogueUI(scene);
        CreditsView creditsView = EnsureCreditsView(scene);
        WireEnding(scene, def, returnButton, dm, creditsView);

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, scenePath);

        if (!saved)
        {
            Debug.LogError($"[EndingSceneBuilder] '{scenePath}' 저장에 실패했습니다.");
            return;
        }

        report.Add(sceneCreated
            ? $"{def.sceneName}.unity 생성 ({def.summary} — 대사 {def.dialogueFile}/{def.dialogueStartId})"
            : $"{def.sceneName}.unity 갱신 ({def.summary} — 참조/값 재배선)");
    }

    private static void EnsureCamera(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<Camera>(true) != null) return;

        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.allowHDR = true;

        if (camGo.GetComponent<UniversalAdditionalCameraData>() == null)
            camGo.AddComponent<UniversalAdditionalCameraData>();
    }

    private static void EnsureEventSystem(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<EventSystem>(true) != null) return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Button EnsureCanvasWithReturnButton(Scene scene)
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        GameObject canvasGo = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != "Canvas") continue;
            if (root.GetComponent<Canvas>() == null) continue;
            if (root.GetComponent<DialogueManager>() != null) continue;
            canvasGo = root;
            break;
        }

        if (canvasGo == null)
        {
            canvasGo = new GameObject("Canvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = uiLayer;
        }

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Button button = canvasGo.GetComponentInChildren<Button>(true);
        if (button != null) return button;

        var buttonGo = new GameObject("ReturnButton", typeof(RectTransform));
        buttonGo.layer = uiLayer;
        buttonGo.transform.SetParent(canvasGo.transform, false);

        var rect = (RectTransform)buttonGo.transform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 90f);
        rect.sizeDelta = new Vector2(320f, 72f);

        var image = buttonGo.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.75f);

        button = buttonGo.AddComponent<Button>();
        button.targetGraphic = image;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.layer = uiLayer;
        labelGo.transform.SetParent(buttonGo.transform, false);

        var labelRect = (RectTransform)labelGo.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelGo.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = "처음으로";
        label.fontSize = 32f;
        label.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        return button;
    }

    private static DialogueManager EnsureDialogueUI(Scene scene)
    {
        var existing = Object.FindObjectOfType<DialogueManager>(true);
        if (existing != null) return existing;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueUIPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[EndingSceneBuilder] DialogueUI 프리팹이 없어 배치하지 못했습니다.");
            return null;
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance == null) return null;

        instance.name = "DialogueUI";
        return instance.GetComponent<DialogueManager>();
    }

    private static void EnsureDialogueUIPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(DialogueUIPrefabPath) != null) return;
        DialogueUIPrefabBuilder.CreateDialogueUIPrefab();
        report.Add("DialogueUI 프리팹이 없어 DialogueUIPrefabBuilder 로 생성");
    }

    private static CreditsView EnsureCreditsView(Scene scene)
    {
        var existing = Object.FindObjectOfType<CreditsView>(true);
        if (existing != null) return existing;

        var go = new GameObject("CreditsView");
        return go.AddComponent<CreditsView>();
    }

    private static void WireEnding(Scene scene, EndingDef def, Button returnButton,
        DialogueManager dm, CreditsView creditsView)
    {
        GameObject managerGo = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != "GameManager") continue;
            managerGo = root;
            break;
        }
        if (managerGo == null) managerGo = new GameObject("GameManager");

        var ending = managerGo.GetComponent<Ending>();
        if (ending == null) ending = managerGo.AddComponent<Ending>();

        var so = new SerializedObject(ending);
        SetString(so, "endingId", def.sceneName);
        SetString(so, "dialogueFile", def.dialogueFile);
        SetString(so, "dialogueStartId", def.dialogueStartId);
        SetString(so, "achievementId", def.sceneName);
        SetObjectReference(so, "DM", dm);
        SetObjectReference(so, "returnButton", returnButton);
        SetObjectReference(so, "creditsView", creditsView);

        var creditsProp = so.FindProperty("creditsContent");
        if (creditsProp != null)
        {
            string current = creditsProp.stringValue;
            if (string.IsNullOrWhiteSpace(current) || current.Trim() == "(작성중)")
                creditsProp.stringValue = def.defaultCredits;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SceneSO EnsureSceneSO(string sceneName)
    {
        string soPath = $"{SceneFolder}/{sceneName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<SceneSO>(soPath);
        if (existing != null)
        {
            if (existing.targetSceneName != sceneName)
            {
                existing.targetSceneName = sceneName;
                EditorUtility.SetDirty(existing);
                report.Add($"{sceneName}.asset targetSceneName 교정");
            }
            return existing;
        }

        var so = ScriptableObject.CreateInstance<SceneSO>();
        var template = AssetDatabase.LoadAssetAtPath<SceneSO>(TemplateSceneSOPath);
        if (template != null)
        {
            so.useLoadingScene = template.useLoadingScene;
            so.loadingSceneName = template.loadingSceneName;
            so.leastHoldingDuration = template.leastHoldingDuration;
            so.enterTransition = template.enterTransition;
            so.exitTransition = template.exitTransition;
            so.transitionDuration = template.transitionDuration;
        }
        else
        {
            so.useLoadingScene = false;
            so.loadingSceneName = "Loading";
            so.leastHoldingDuration = 3f;
            so.enterTransition = TransitionEffect.FadeIn;
            so.exitTransition = TransitionEffect.FadeOut;
            so.transitionDuration = 0.5f;
        }

        so.targetSceneName = sceneName;
        AssetDatabase.CreateAsset(so, soPath);
        report.Add($"{sceneName}.asset SceneSO 생성" + (template != null ? " (Ending.asset 값 복사)" : " (기본값)"));
        return so;
    }

    private static void EnsureInBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool changed = false;

        foreach (var def in Endings)
        {
            string scenePath = $"{SceneFolder}/{def.sceneName}.unity";
            int index = scenes.FindIndex(s => s.path == scenePath);

            if (index < 0)
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                changed = true;
                report.Add($"Build Settings 에 {def.sceneName} 추가");
            }
            else if (!scenes[index].enabled)
            {
                scenes[index].enabled = true;
                changed = true;
                report.Add($"Build Settings 의 {def.sceneName} 활성화");
            }
        }

        if (changed) EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void RegisterToCorePrefab(List<SceneSO> sceneSOs)
    {
        var corePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorePrefabPath);
        if (corePrefab == null)
        {
            Debug.LogWarning($"[EndingSceneBuilder] '{CorePrefabPath}' 를 찾지 못했습니다. sceneConfigs 등록을 건너뜁니다.");
            return;
        }

        var controller = corePrefab.GetComponentInChildren<SceneController>(true);
        if (controller == null)
        {
            Debug.LogWarning("[EndingSceneBuilder] Core 프리팹에서 SceneController 를 찾지 못했습니다.");
            return;
        }

        var so = new SerializedObject(controller);
        var list = so.FindProperty("sceneConfigs");
        if (list == null || !list.isArray)
        {
            Debug.LogWarning("[EndingSceneBuilder] SceneController 의 sceneConfigs 를 읽지 못했습니다.");
            return;
        }

        bool changed = false;
        foreach (var sceneSO in sceneSOs)
        {
            bool found = false;
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == sceneSO)
                {
                    found = true;
                    break;
                }
            }
            if (found) continue;

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = sceneSO;
            changed = true;
            report.Add($"Core 프리팹 sceneConfigs 에 {sceneSO.name} 등록");
        }

        if (!changed) return;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(corePrefab);
        PrefabUtility.SavePrefabAsset(corePrefab);
    }

    private static void SetString(SerializedObject so, string path, string value)
    {
        var prop = so.FindProperty(path);
        if (prop == null)
        {
            Debug.LogWarning($"[EndingSceneBuilder] '{path}' 필드를 찾지 못했습니다.");
            return;
        }
        prop.stringValue = value;
    }

    private static void SetObjectReference(SerializedObject so, string path, Object value)
    {
        var prop = so.FindProperty(path);
        if (prop == null)
        {
            Debug.LogWarning($"[EndingSceneBuilder] '{path}' 필드를 찾지 못했습니다.");
            return;
        }
        prop.objectReferenceValue = value;
    }

    private static void LogSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[EndingSceneBuilder] 엔딩 씬 4종 구성 완료.");
        sb.AppendLine("── 수행 내역 ──");
        foreach (var line in report) sb.AppendLine(" · " + line);
        sb.AppendLine("── 유저가 확인할 것 ──");
        sb.AppendLine(" · 각 씬 GameManager > Ending 의 creditsContent(엔딩별 크레딧)를 실제 내용으로 채우기");
        sb.AppendLine(" · 기존 Ending.unity / Ending.asset 은 폐기 예정 — Build Settings 에서 기존 Ending 제거는 유저 판단");
        sb.AppendLine(" · 공통 출처 크레딧은 Assets/Resources/Credits/CommonCredits.txt 한 파일만 수정");

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog(
            "엔딩 씬 구성 완료",
            "Ending1~Ending4 씬을 생성/갱신했습니다.\n\n" +
            "· 각 씬: Main Camera + EventSystem + Canvas(복귀 버튼) + DialogueUI + CreditsView + GameManager(Ending)\n" +
            "· SceneSO 4개 생성 + Build Settings + Core 프리팹 sceneConfigs 등록\n" +
            "· 기존 Ending.unity / Ending.asset 은 그대로 두었습니다(폐기 예정 — 빌드세팅에서 제거는 유저 판단)\n\n" +
            "자세한 내역은 콘솔 로그를 확인하세요.",
            "확인");
    }
}

/* [파일 노트 — 엔딩 씬 4종 빌더]
 *
 * 실행: Tools / Tup3 / Setup Ending Scenes — 몇 번을 다시 실행해도 중복 생성 없이 갱신(멱등).
 *
 * 하는 일 (Ending1~Ending4.unity 를 열거나 새로 만들어 코드로 구성 — .unity 직접 편집 없음):
 *   1. 씬 구성 (기존 단일 Ending.unity 의 구성을 참고, 이름으로 찾고 없을 때만 생성)
 *      - Main Camera : 검정 배경 Orthographic + AudioListener + URP 카메라 데이터.
 *      - EventSystem : StandaloneInputModule (복귀 버튼 클릭용).
 *      - Canvas      : Overlay(sortingOrder 20, 1920x1080 스케일) + ReturnButton("처음으로",
 *                      TMP 라벨, 하단 중앙). 버튼은 씬에서 활성 상태로 저장되고 런타임에
 *                      Ending.cs 가 숨겼다가 크레딧 종료 후 다시 보여준다.
 *      - DialogueUI  : DialogueManager 가 없으면 프리팹 인스턴스 배치(프리팹이 없으면
 *                      DialogueUIPrefabBuilder 를 먼저 호출해 생성).
 *      - CreditsView : 빈 오브젝트 + 컴포넌트만 배치(UI 는 런타임 코드 생성) — 씬별로
 *                      속도/색을 인스펙터에서 조정할 수 있게 하기 위함.
 *      - GameManager : Ending 컴포넌트에 씬별 값 배선 —
 *                      Ending1=S11_ENDING_1/end1_01(패배·배드), Ending2=S12_ENDING_2/end2_01(트루),
 *                      Ending3=S14_ENDING_3/end3_01, Ending4=S15_ENDING_4/end4_01.
 *                      achievementId = endingId. creditsContent 는 비었거나 "(작성중)"일 때만
 *                      엔딩별 placeholder 를 넣는다(유저가 채운 내용은 보존).
 *   2. 씬 등록 3종 세트 자동화
 *      - SceneSO : Assets/Scenes/Ending1~4.asset 생성(기존 Ending.asset 값 복사,
 *        targetSceneName 만 씬별로). SceneController 는 SceneSO 가 없으면 로드 실패하므로 필수.
 *      - EditorBuildSettings : 4씬 추가/활성화 (API 사용).
 *      - Core.prefab : SceneController.sceneConfigs 에 4개 SceneSO 추가(SerializedObject,
 *        PrologueSceneBuilder 와 같은 방식).
 *   3. 기존 Ending.unity / Ending.asset 은 삭제하지 않는다 — 폐기 예정이며,
 *      Build Settings 에서 기존 Ending 을 제거할지는 유저 판단에 맡긴다.
 *
 * 멱등성 규칙: 오브젝트는 이름/컴포넌트로 찾고 없을 때만 생성. 값 배선(SerializedObject)은
 * 매번 다시 적용하되 creditsContent 처럼 "유저가 채울 값"은 placeholder 상태일 때만 덮어쓴다.
 */

using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PrologueSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Prologue.unity";
    private const string SceneSOPath = "Assets/Scenes/Prologue.asset";
    private const string CorePrefabPath = "Assets/Prefabs/Core.prefab";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string PlayerTexturePath = "Assets/GameAssets/Player/Texutres/Player_not_shine.png";
    private const string FontPath = "Assets/GameAssets/Fonts/PRETENDARD-REGULAR SDF.asset";
    private const string LitSpriteMaterialPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

    private const string DialogueFileName = "S01_MONOLOGUE";
    private const string NextSceneName = "Lobby";
    private const string SkipHintMessage = "ESC / SPACE — 스킵";

    [MenuItem("Tools/Tup3/Build Prologue Scene", false, 10)]
    public static void BuildPrologueScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (File.Exists(ScenePath))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Prologue 씬 다시 만들기",
                $"'{ScenePath}' 가 이미 있습니다.\n\n지금 있는 내용을 전부 지우고 프롤로그 씬을 새로 구성합니다.\n계속할까요?",
                "덮어쓰기", "취소");

            if (!overwrite) return;
        }

        EnsureSceneConfigRegistered();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateCoreInstance();

        var globalLight = CreateGlobalLight();
        var playerSprite = CreatePlayerSprite();
        var playerLight = CreatePlayerLight(playerSprite);

        var skipHint = CreateDialogueUI(out _);
        var manager = CreateManager(globalLight, playerLight, playerSprite, skipHint);

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);

        AssetDatabase.SaveAssets();
        EnsureInBuildSettings();

        if (!saved)
        {
            Debug.LogError("[PrologueSceneBuilder] 씬 저장에 실패했습니다.");
            return;
        }

        Selection.activeGameObject = manager;
        Debug.Log($"[PrologueSceneBuilder] '{ScenePath}' 구성 완료.");
        EditorUtility.DisplayDialog(
            "Prologue 씬 생성 완료",
            "프롤로그 씬을 새로 구성했습니다.\n\n" +
            "· Main Camera (검정 배경, Orthographic)\n" +
            "· Core 프리팹 인스턴스\n" +
            "· Global Light 2D / Player Light 2D\n" +
            "· ProloguePlayer (Player.prefab 의 스프라이트만)\n" +
            "· DialogueCanvas (PlayerPanel / BossPanel / NarrationPanel / ChoicePanel / SkipHint)\n" +
            "· PrologueManager (PrologueScene 스크립트)\n\n" +
            "Build Settings 등록과 Core 프리팹의 sceneConfigs 등록도 함께 처리했습니다.",
            "확인");
    }

    private static void CreateCamera()
    {
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        cam.allowHDR = true;

        if (camGo.GetComponent<UniversalAdditionalCameraData>() == null)
            camGo.AddComponent<UniversalAdditionalCameraData>();
    }

    private static void CreateCoreInstance()
    {
        var corePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorePrefabPath);
        if (corePrefab == null)
        {
            Debug.LogWarning($"[PrologueSceneBuilder] '{CorePrefabPath}' 를 찾지 못해 Core 인스턴스를 넣지 못했습니다.");
            return;
        }

        var instance = PrefabUtility.InstantiatePrefab(corePrefab) as GameObject;
        if (instance != null) instance.name = "Core";
    }

    private static Light2D CreateGlobalLight()
    {
        var go = new GameObject("Global Light 2D");
        var light = go.AddComponent<Light2D>();

        light.lightType = Light2D.LightType.Global;
        light.color = Color.white;
        light.intensity = 0f;
        light.shadowsEnabled = false;
        light.targetSortingLayers = AllSortingLayerIds();

        return light;
    }

    private static Light2D CreatePlayerLight(SpriteRenderer playerSprite)
    {
        var go = new GameObject("Player Light 2D");
        var light = go.AddComponent<Light2D>();

        light.lightType = Light2D.LightType.Point;
        light.color = new Color(0.86f, 0.9f, 1f, 1f);
        light.intensity = 0f;
        light.pointLightInnerAngle = 360f;
        light.pointLightOuterAngle = 360f;
        light.pointLightInnerRadius = 0.4f;
        light.pointLightOuterRadius = 1.2f;
        light.falloffIntensity = 0.65f;
        light.shadowsEnabled = false;
        light.targetSortingLayers = AllSortingLayerIds();

        Vector3 basePosition = playerSprite != null ? playerSprite.transform.position : Vector3.zero;
        go.transform.position = basePosition + new Vector3(0f, 0.6f, 0f);

        return light;
    }

    private static SpriteRenderer CreatePlayerSprite()
    {
        var go = new GameObject("ProloguePlayer");
        go.transform.position = new Vector3(0f, -1f, 0f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 1;
        renderer.color = Color.white;

        Sprite sprite = null;
        Material material = null;

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab != null)
        {
            var source = playerPrefab.GetComponent<SpriteRenderer>();
            if (source == null) source = playerPrefab.GetComponentInChildren<SpriteRenderer>(true);

            if (source != null)
            {
                sprite = source.sprite;
                material = source.sharedMaterial;
                renderer.sortingLayerID = source.sortingLayerID;
            }
        }

        if (sprite == null)
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerTexturePath);

        if (material == null)
            material = AssetDatabase.LoadAssetAtPath<Material>(LitSpriteMaterialPath);

        if (sprite == null)
            Debug.LogWarning("[PrologueSceneBuilder] 플레이어 스프라이트를 찾지 못했습니다. ProloguePlayer 에 직접 넣어주세요.");

        if (material == null)
            Debug.LogWarning("[PrologueSceneBuilder] Sprite-Lit-Default 머티리얼을 찾지 못했습니다. Light2D 가 스프라이트에 안 먹을 수 있습니다.");

        renderer.sprite = sprite;
        if (material != null) renderer.sharedMaterial = material;

        return renderer;
    }

    private static TextMeshProUGUI CreateDialogueUI(out DialogueManager dialogueManager)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            Debug.LogWarning($"[PrologueSceneBuilder] '{FontPath}' 를 찾지 못했습니다. TMP 기본 폰트로 만들어집니다.");

        int uiLayer = LayerMask.NameToLayer("UI");

        var canvasGo = new GameObject("DialogueCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.layer = uiLayer;

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.referencePixelsPerUnit = 100f;
        scaler.scaleFactor = 1f;

        var canvasRect = canvasGo.GetComponent<RectTransform>();

        CreateSpeakerPanel("PlayerPanel", "PlayerText", canvasRect, font, uiLayer);
        CreateSpeakerPanel("BossPanel", "BossText", canvasRect, font, uiLayer);
        CreateSpeakerPanel("NarrationPanel", "NarrationText", canvasRect, font, uiLayer);
        CreateChoicePanel(canvasRect, font, uiLayer);

        var skipHint = CreateSkipHint(canvasRect, font, uiLayer);

        dialogueManager = canvasGo.AddComponent<DialogueManager>();

        var so = new SerializedObject(dialogueManager);
        SetObjectReference(so, "dialogueRoot", canvasRect);
        SetBool(so, "allowDialogueSkip", false);
        SetFloat(so, "charDelay", 0.035f);
        SetFloat(so, "autoAdvanceDelay", 1.6f);
        so.ApplyModifiedPropertiesWithoutUndo();

        return skipHint;
    }

    private static void CreateSpeakerPanel(string panelName, string textName, RectTransform parent,
        TMP_FontAsset font, int uiLayer)
    {
        var panel = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = uiLayer;
        panel.transform.SetParent(parent, false);

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 100f);
        rect.sizeDelta = new Vector2(0f, 200f);

        var image = panel.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.6f);
        image.raycastTarget = false;

        var text = CreateText(textName, rect, font, uiLayer, 40f, TextAlignmentOptions.Center);
        var textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(-80f, -40f);

        panel.SetActive(false);
    }

    private static void CreateChoicePanel(RectTransform parent, TMP_FontAsset font, int uiLayer)
    {
        var panel = new GameObject("ChoicePanel", typeof(RectTransform));
        panel.layer = uiLayer;
        panel.transform.SetParent(parent, false);

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 330f);
        rect.sizeDelta = new Vector2(0f, 100f);

        for (int i = 0; i < 2; i++)
        {
            var box = new GameObject($"Choice{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            box.layer = uiLayer;
            box.transform.SetParent(rect, false);

            var boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(i == 0 ? 0.08f : 0.54f, 0f);
            boxRect.anchorMax = new Vector2(i == 0 ? 0.46f : 0.92f, 1f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = Vector2.zero;

            var boxImage = box.GetComponent<Image>();
            boxImage.color = new Color(0f, 0f, 0f, 0.7f);
            boxImage.raycastTarget = false;

            var text = CreateText($"Choice{i}Text", boxRect, font, uiLayer, 32f, TextAlignmentOptions.Center);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(-20f, -20f);
        }

        panel.SetActive(false);
    }

    private static TextMeshProUGUI CreateSkipHint(RectTransform parent, TMP_FontAsset font, int uiLayer)
    {
        var text = CreateText("SkipHint", parent, font, uiLayer, 24f, TextAlignmentOptions.Right);
        text.text = SkipHintMessage;
        text.color = Color.white;
        text.alpha = 0f;

        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-40f, 32f);
        rect.sizeDelta = new Vector2(360f, 40f);

        return text;
    }

    private static TextMeshProUGUI CreateText(string name, RectTransform parent, TMP_FontAsset font,
        int uiLayer, float fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = uiLayer;
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.text = string.Empty;

        return text;
    }

    private static GameObject CreateManager(Light2D globalLight, Light2D playerLight,
        SpriteRenderer playerSprite, TextMeshProUGUI skipHint)
    {
        var go = new GameObject("PrologueManager");
        var prologue = go.AddComponent<PrologueScene>();

        var so = new SerializedObject(prologue);
        SetObjectReference(so, "globalLight", globalLight);
        SetObjectReference(so, "playerLight", playerLight);
        SetObjectReference(so, "playerSprite", playerSprite);
        SetObjectReference(so, "skipHintText", skipHint);
        SetString(so, "dialogueFileName", DialogueFileName);
        SetString(so, "nextSceneName", NextSceneName);
        so.ApplyModifiedPropertiesWithoutUndo();

        return go;
    }

    private static void EnsureSceneConfigRegistered()
    {
        var sceneSO = AssetDatabase.LoadAssetAtPath<SceneSO>(SceneSOPath);
        if (sceneSO == null)
        {
            Debug.LogWarning($"[PrologueSceneBuilder] '{SceneSOPath}' 를 찾지 못했습니다. sceneConfigs 등록을 건너뜁니다.");
            return;
        }

        var corePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorePrefabPath);
        if (corePrefab == null)
        {
            Debug.LogWarning($"[PrologueSceneBuilder] '{CorePrefabPath}' 를 찾지 못했습니다. sceneConfigs 등록을 건너뜁니다.");
            return;
        }

        var controller = corePrefab.GetComponentInChildren<SceneController>(true);
        if (controller == null)
        {
            Debug.LogWarning("[PrologueSceneBuilder] Core 프리팹에서 SceneController 를 찾지 못했습니다.");
            return;
        }

        var so = new SerializedObject(controller);
        var list = so.FindProperty("sceneConfigs");
        if (list == null || !list.isArray)
        {
            Debug.LogWarning("[PrologueSceneBuilder] SceneController 의 sceneConfigs 를 읽지 못했습니다.");
            return;
        }

        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == sceneSO)
            {
                Debug.Log("[PrologueSceneBuilder] Core 프리팹 sceneConfigs 에 Prologue 가 이미 등록돼 있습니다.");
                return;
            }
        }

        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = sceneSO;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(corePrefab);
        PrefabUtility.SavePrefabAsset(corePrefab);
        AssetDatabase.SaveAssets();

        Debug.Log("[PrologueSceneBuilder] Core 프리팹 sceneConfigs 에 Prologue.asset 을 등록했습니다.");
    }

    private static void EnsureInBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == ScenePath)
            {
                if (!scenes[i].enabled)
                {
                    scenes[i].enabled = true;
                    EditorBuildSettings.scenes = scenes.ToArray();
                    Debug.Log("[PrologueSceneBuilder] Build Settings 의 Prologue 항목을 활성화했습니다.");
                }
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();

        Debug.Log("[PrologueSceneBuilder] Build Settings 에 Prologue 씬을 추가했습니다.");
    }

    private static int[] AllSortingLayerIds()
    {
        var layers = SortingLayer.layers;
        var ids = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++) ids[i] = layers[i].id;
        return ids;
    }

    private static void SetObjectReference(SerializedObject so, string path, UnityEngine.Object value)
    {
        var prop = so.FindProperty(path);
        if (prop == null)
        {
            Debug.LogWarning($"[PrologueSceneBuilder] '{path}' 필드를 찾지 못했습니다.");
            return;
        }
        prop.objectReferenceValue = value;
    }

    private static void SetBool(SerializedObject so, string path, bool value)
    {
        var prop = so.FindProperty(path);
        if (prop == null) return;
        prop.boolValue = value;
    }

    private static void SetFloat(SerializedObject so, string path, float value)
    {
        var prop = so.FindProperty(path);
        if (prop == null) return;
        prop.floatValue = value;
    }

    private static void SetString(SerializedObject so, string path, string value)
    {
        var prop = so.FindProperty(path);
        if (prop == null) return;
        prop.stringValue = value;
    }
}

/* [파일 노트 — Prologue 씬 빌더]
 *
 * 1) 실행 방법
 *    Unity 상단 메뉴 → Tools / Tup3 / Build Prologue Scene
 *    - 열려 있는 씬에 수정사항이 있으면 먼저 저장 여부를 묻는다.
 *    - Assets/Scenes/Prologue.unity 가 이미 있으면 덮어쓰기 확인 다이얼로그를 띄운다.
 *    - EmptyScene 으로 새로 만든 뒤 아래 오브젝트를 코드로 배치하고 같은 경로에 저장한다.
 *      (경로가 같으므로 기존 .meta / GUID 가 유지되고, Build Settings 참조도 안 깨진다.)
 *
 * 2) 만들어지는 계층
 *    Main Camera            : Orthographic, size 5, 배경 검정, UniversalAdditionalCameraData 포함
 *    Core                   : Assets/Prefabs/Core.prefab 인스턴스 (SceneController / ScreenFader / UserDataManager)
 *    Global Light 2D        : Light2D(Global), intensity 0 에서 시작 — 런타임 스크립트가 올린다
 *    Player Light 2D        : Light2D(Point), 주인공 머리 위쪽에 배치
 *    ProloguePlayer         : SpriteRenderer 하나만. Player.prefab 은 이동/전투 스크립트가 붙어 있어
 *                             프리팹을 통째로 넣지 않고 sprite 와 sharedMaterial(Sprite-Lit-Default)만 가져온다.
 *    DialogueCanvas         : Canvas(Overlay) + CanvasScaler(ConstantPixelSize) + GraphicRaycaster + DialogueManager
 *      ├ PlayerPanel   / PlayerText
 *      ├ BossPanel     / BossText
 *      ├ NarrationPanel/ NarrationText
 *      ├ ChoicePanel   / Choice0(Choice0Text), Choice1(Choice1Text)
 *      └ SkipHint      : 우측 하단 스킵 안내 TMP
 *    PrologueManager        : PrologueScene 스크립트. 위 참조들이 자동으로 연결된다.
 *
 *    패널 구성은 Start.unity 의 "Canvas (1)" 계층을 그대로 따라 만들었다.
 *    DialogueManager.Awake 가 dialogueRoot 밑에서 이름으로 Find 하므로 이름이 정확해야 하고,
 *    ChoicePanel 은 "선택지 오브젝트 > TMP 자식" 2단 구조여야 choiceObjects 계산이 맞는다.
 *
 * 3) 함께 처리하는 것
 *    - Core.prefab 의 SceneController.sceneConfigs 에 Assets/Scenes/Prologue.asset 이 없으면 추가한다.
 *      (씬을 만들기 전에 먼저 처리하므로 씬에 들어가는 Core 인스턴스도 갱신된 값을 갖는다.)
 *    - EditorBuildSettings.scenes 에 Prologue.unity 가 없으면 맨 뒤에 추가하고, 꺼져 있으면 켠다.
 *
 * 4) private [SerializeField] 필드는 SerializedObject / FindProperty 로 연결한다.
 *    필드 이름이 바뀌면 여기 문자열도 같이 고쳐야 한다:
 *      DialogueManager : dialogueRoot, allowDialogueSkip, charDelay, autoAdvanceDelay
 *      PrologueScene   : globalLight, playerLight, playerSprite, skipHintText, dialogueFileName, nextSceneName
 *    나머지 수치(연출 타이밍, 조명 단계, skipKeys)는 C# 필드 초기값이 그대로 쓰이므로 여기서 건드리지 않는다.
 */

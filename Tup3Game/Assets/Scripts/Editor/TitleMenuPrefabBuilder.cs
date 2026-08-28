using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TitleMenuPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/TitleMenu.prefab";
    private const string FontPath = "Assets/GameAssets/Fonts/PRETENDARD-REGULAR SDF.asset";
    private const string StartScenePath = "Assets/Scenes/Start.unity";
    private const string RootName = "TitleMenu";
    private const string KoreanSample = "게임 시작이어하기도전과제옵션종료";

    private struct Settings
    {
        public float itemFontSize;
        public string newGameLabel;
        public string continueLabel;
        public string achievementsLabel;
        public string optionsLabel;
        public string quitLabel;
        public Color textColor;
        public Color hitAreaColor;
        public Vector2 anchoredPosition;
        public Vector2 itemSize;
        public float spacing;
        public int itemAlignment;
        public int sortingOrder;
    }

    private static readonly List<string> report = new List<string>();

    [MenuItem("Tools/Tup3/Build Title Menu", false, 15)]
    public static void BuildTitleMenu()
    {
        report.Clear();

        GameObject prefab = BuildOrUpdatePrefab();
        if (prefab == null) return;

        PlaceInStartScene(prefab);

        AssetDatabase.SaveAssets();

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        LogSummary();
    }

    private static GameObject BuildOrUpdatePrefab()
    {
        string folder = Path.GetDirectoryName(PrefabPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            Directory.CreateDirectory(folder);

        bool exists = File.Exists(PrefabPath);

        GameObject root = exists
            ? PrefabUtility.LoadPrefabContents(PrefabPath)
            : new GameObject(RootName,
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Configure(root);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);

        if (exists) PrefabUtility.UnloadPrefabContents(root);
        else Object.DestroyImmediate(root);

        if (!success || saved == null)
        {
            Debug.LogError($"[TitleMenuPrefabBuilder] '{PrefabPath}' 저장에 실패했습니다.");
            return null;
        }

        report.Add(exists ? $"{PrefabPath} 갱신" : $"{PrefabPath} 생성");
        return saved;
    }

    private static void Configure(GameObject root)
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 0;

        root.name = RootName;
        root.layer = uiLayer;

        var view = root.GetComponent<TitleMenuView>();
        if (view == null) view = root.AddComponent<TitleMenuView>();

        TMP_FontAsset font = LoadFont();

        var so = new SerializedObject(view);
        Settings settings = ReadSettings(so);

        EnsureCanvas(root, settings.sortingOrder);

        RectTransform items = EnsureItemsRoot(root, uiLayer, ref settings);
        if (items == null) return;

        Button newGame = EnsureItem(items, TitleMenuView.NewGameItemName,
            settings.newGameLabel, settings, font, uiLayer, out string newGameText);
        Button continueItem = EnsureItem(items, TitleMenuView.ContinueItemName,
            settings.continueLabel, settings, font, uiLayer, out string continueText);
        Button achievements = EnsureItem(items, TitleMenuView.AchievementsItemName,
            settings.achievementsLabel, settings, font, uiLayer, out string achievementsText);
        Button options = EnsureItem(items, TitleMenuView.OptionsItemName,
            settings.optionsLabel, settings, font, uiLayer, out string optionsText);
        Button quit = EnsureItem(items, TitleMenuView.QuitItemName,
            settings.quitLabel, settings, font, uiLayer, out string quitText);

        Reorder(newGame, continueItem, achievements, options, quit);

        settings.newGameLabel = newGameText;
        settings.continueLabel = continueText;
        settings.achievementsLabel = achievementsText;
        settings.optionsLabel = optionsText;
        settings.quitLabel = quitText;

        WriteSettings(so, settings, font, items, newGame, continueItem, achievements, options, quit);

        root.SetActive(false);
    }

    private static TMP_FontAsset LoadFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            Debug.LogWarning($"[TitleMenuPrefabBuilder] '{FontPath}' 를 찾지 못했습니다. 폰트 지정을 건너뜁니다.");
        return font;
    }

    private static bool SupportsKorean(TMP_FontAsset font)
    {
        return font != null && font.HasCharacters(KoreanSample);
    }

    private static Settings ReadSettings(SerializedObject so)
    {
        return new Settings
        {
            itemFontSize = GetFloat(so, "itemFontSize", 34f),
            newGameLabel = GetString(so, "newGameLabel", "게임 시작"),
            continueLabel = GetString(so, "continueLabel", "이어하기"),
            achievementsLabel = GetString(so, "achievementsLabel", "도전과제"),
            optionsLabel = GetString(so, "optionsLabel", "옵션"),
            quitLabel = GetString(so, "quitLabel", "게임 종료"),
            textColor = GetColor(so, "textColor", Color.white),
            hitAreaColor = GetColor(so, "hitAreaColor", new Color(1f, 1f, 1f, 0f)),
            anchoredPosition = GetVector2(so, "anchoredPosition", new Vector2(0f, -180f)),
            itemSize = GetVector2(so, "itemSize", new Vector2(420f, 54f)),
            spacing = GetFloat(so, "spacing", 8f),
            itemAlignment = GetInt(so, "itemAlignment", (int)TextAlignmentOptions.Center),
            sortingOrder = GetInt(so, "sortingOrder", 800),
        };
    }

    private static void Reorder(params Button[] ordered)
    {
        int index = 0;
        for (int i = 0; i < ordered.Length; i++)
        {
            if (ordered[i] == null) continue;
            ordered[i].transform.SetSiblingIndex(index);
            index++;
        }
    }

    private static void WriteSettings(SerializedObject so, Settings settings, TMP_FontAsset font,
        RectTransform items, Button newGame, Button continueItem, Button achievements, Button options, Button quit)
    {
        var fontProp = so.FindProperty("fontAsset");
        if (fontProp != null && font != null)
        {
            var current = fontProp.objectReferenceValue as TMP_FontAsset;
            if (!SupportsKorean(current)) fontProp.objectReferenceValue = font;
        }

        SetObject(so, "itemsRoot", items);
        SetObject(so, "newGameItem", newGame);
        SetObject(so, "continueItem", continueItem);
        SetObject(so, "achievementsItem", achievements);
        SetObject(so, "optionsItem", options);
        SetObject(so, "quitItem", quit);

        SetString(so, "newGameLabel", settings.newGameLabel);
        SetString(so, "continueLabel", settings.continueLabel);
        SetString(so, "achievementsLabel", settings.achievementsLabel);
        SetString(so, "optionsLabel", settings.optionsLabel);
        SetString(so, "quitLabel", settings.quitLabel);

        SetVector2(so, "anchoredPosition", settings.anchoredPosition);
        SetFloat(so, "spacing", settings.spacing);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureCanvas(GameObject root, int sortingOrder)
    {
        var canvas = root.GetComponent<Canvas>();
        if (canvas == null) canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = root.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (root.GetComponent<GraphicRaycaster>() == null)
            root.AddComponent<GraphicRaycaster>();
    }

    private static RectTransform EnsureItemsRoot(GameObject root, int uiLayer, ref Settings settings)
    {
        Transform found = root.transform.Find(TitleMenuView.ItemsRootName);
        RectTransform rect;

        if (found == null)
        {
            var go = new GameObject(TitleMenuView.ItemsRootName, typeof(RectTransform));
            go.layer = uiLayer;
            rect = (RectTransform)go.transform;
            rect.SetParent(root.transform, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = settings.anchoredPosition;
            report.Add($"{TitleMenuView.ItemsRootName} 컬럼 생성");
        }
        else
        {
            rect = found as RectTransform;
            if (rect == null)
            {
                Debug.LogError($"[TitleMenuPrefabBuilder] '{TitleMenuView.ItemsRootName}' 에 RectTransform 이 없습니다.");
                return null;
            }
            settings.anchoredPosition = rect.anchoredPosition;
        }

        var layout = rect.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = settings.spacing;
        }
        else
        {
            settings.spacing = layout.spacing;
        }

        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = rect.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rect;
    }

    private static Button EnsureItem(RectTransform parent, string name, string defaultText,
        Settings settings, TMP_FontAsset font, int uiLayer, out string resolvedText)
    {
        Transform found = parent.Find(name);
        GameObject go;

        if (found == null)
        {
            go = new GameObject(name, typeof(RectTransform));
            go.layer = uiLayer;
            go.transform.SetParent(parent, false);
            report.Add($"{name} 생성");
        }
        else
        {
            go = found.gameObject;
        }

        var image = go.GetComponent<Image>();
        if (image == null)
        {
            image = go.AddComponent<Image>();
            image.color = settings.hitAreaColor;
        }
        image.raycastTarget = true;

        var element = go.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = go.AddComponent<LayoutElement>();
            element.preferredWidth = settings.itemSize.x;
            element.preferredHeight = settings.itemSize.y;
        }

        var button = go.GetComponent<Button>();
        if (button == null) button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;

        EnsureLabel(go.transform, name, defaultText, settings, font, uiLayer, out resolvedText);

        if (go.GetComponent<TitleMenuItemHighlighter>() == null)
            go.AddComponent<TitleMenuItemHighlighter>();

        return button;
    }

    private static void EnsureLabel(Transform parent, string itemName, string defaultText,
        Settings settings, TMP_FontAsset font, int uiLayer, out string resolvedText)
    {
        Transform found = parent.Find(TitleMenuView.ItemLabelName);
        TextMeshProUGUI label;

        if (found == null)
        {
            var go = new GameObject(TitleMenuView.ItemLabelName, typeof(RectTransform));
            go.layer = uiLayer;
            go.transform.SetParent(parent, false);
            label = go.AddComponent<TextMeshProUGUI>();
            ApplyLabelContent(label, defaultText, settings);
        }
        else
        {
            label = found.GetComponent<TextMeshProUGUI>();
            if (label == null) label = found.gameObject.AddComponent<TextMeshProUGUI>();
            if (string.IsNullOrEmpty(label.text)) ApplyLabelContent(label, defaultText, settings);
        }

        resolvedText = label.text;

        if (font != null && !SupportsKorean(label.font))
        {
            string before = label.font != null ? label.font.name : "(없음)";
            label.font = font;
            report.Add($"{itemName} 라벨 폰트 교체: {before} → {font.name} (한글 글리프 확인 실패)");
        }

        label.raycastTarget = false;

        var rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ApplyLabelContent(TextMeshProUGUI label, string text, Settings settings)
    {
        label.text = text;
        label.fontSize = settings.itemFontSize;
        label.color = settings.textColor;
        label.alignment = (TextAlignmentOptions)settings.itemAlignment;
    }

    private static void PlaceInStartScene(GameObject prefab)
    {
        if (!File.Exists(StartScenePath))
        {
            report.Add($"'{StartScenePath}' 가 없어 씬 배치를 건너뜀");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            report.Add("씬 저장을 취소해 Start.unity 배치를 건너뜀 (프리팹은 갱신됨)");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != StartScenePath)
            scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);

        TitleMenuView existing = FindView(scene);

        if (existing != null)
        {
            report.Add($"Start.unity 에 이미 TitleMenuView('{existing.gameObject.name}')가 있어 새로 배치하지 않음");
        }
        else
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                Debug.LogError("[TitleMenuPrefabBuilder] Start.unity 에 프리팹 인스턴스를 만들지 못했습니다.");
                return;
            }
            instance.name = RootName;
            report.Add("Start.unity 에 TitleMenu 프리팹 인스턴스 배치 (비활성 상태)");
        }

        EnsureEventSystem(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, StartScenePath))
            Debug.LogError($"[TitleMenuPrefabBuilder] '{StartScenePath}' 저장에 실패했습니다.");
    }

    private static TitleMenuView FindView(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var view = root.GetComponentInChildren<TitleMenuView>(true);
            if (view != null) return view;
        }
        return null;
    }

    private static void EnsureEventSystem(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<EventSystem>(true) != null) return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        report.Add("Start.unity 에 EventSystem 추가");
    }

    private static float GetFloat(SerializedObject so, string path, float fallback)
    {
        var prop = so.FindProperty(path);
        return prop != null ? prop.floatValue : fallback;
    }

    private static int GetInt(SerializedObject so, string path, int fallback)
    {
        var prop = so.FindProperty(path);
        return prop != null ? prop.intValue : fallback;
    }

    private static string GetString(SerializedObject so, string path, string fallback)
    {
        var prop = so.FindProperty(path);
        if (prop == null || string.IsNullOrEmpty(prop.stringValue)) return fallback;
        return prop.stringValue;
    }

    private static Color GetColor(SerializedObject so, string path, Color fallback)
    {
        var prop = so.FindProperty(path);
        return prop != null ? prop.colorValue : fallback;
    }

    private static Vector2 GetVector2(SerializedObject so, string path, Vector2 fallback)
    {
        var prop = so.FindProperty(path);
        return prop != null ? prop.vector2Value : fallback;
    }

    private static void SetString(SerializedObject so, string path, string value)
    {
        var prop = so.FindProperty(path);
        if (prop == null)
        {
            Debug.LogWarning($"[TitleMenuPrefabBuilder] '{path}' 필드를 찾지 못했습니다.");
            return;
        }
        prop.stringValue = value;
    }

    private static void SetFloat(SerializedObject so, string path, float value)
    {
        var prop = so.FindProperty(path);
        if (prop == null)
        {
            Debug.LogWarning($"[TitleMenuPrefabBuilder] '{path}' 필드를 찾지 못했습니다.");
            return;
        }
        prop.floatValue = value;
    }

    private static void SetVector2(SerializedObject so, string path, Vector2 value)
    {
        var prop = so.FindProperty(path);
        if (prop == null)
        {
            Debug.LogWarning($"[TitleMenuPrefabBuilder] '{path}' 필드를 찾지 못했습니다.");
            return;
        }
        prop.vector2Value = value;
    }

    private static void SetObject(SerializedObject so, string path, Object value)
    {
        var prop = so.FindProperty(path);
        if (prop == null)
        {
            Debug.LogWarning($"[TitleMenuPrefabBuilder] '{path}' 필드를 찾지 못했습니다.");
            return;
        }
        prop.objectReferenceValue = value;
    }

    private static void LogSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[TitleMenuPrefabBuilder] 타이틀 메뉴 구성 완료.");
        sb.AppendLine("── 수행 내역 ──");
        foreach (var line in report) sb.AppendLine(" · " + line);
        sb.AppendLine("── 확인할 것 ──");
        sb.AppendLine(" · Start.unity 의 TitleMenu 오브젝트는 비활성으로 저장된다 (인트로 중 노출 방지).");
        sb.AppendLine("   StartScene 의 Menu 상태가 페이드인과 함께 Show() 로 켠다.");
        sb.AppendLine(" · 문구/색/위치는 프리팹의 Label(TMP) 오브젝트를 직접 고치면 된다. 다시 실행해도 보존된다.");

        Debug.Log(sb.ToString());

        EditorUtility.DisplayDialog(
            "타이틀 메뉴 구성 완료",
            $"{PrefabPath} 를 만들고 Start.unity 에 배치했습니다.\n\n" +
            "· Canvas(sortingOrder 800) + TitleMenuView\n" +
            "· Items / NewGameItem · ContinueItem · AchievementsItem · OptionsItem · QuitItem\n" +
            "· 각 항목: 투명 Image(클릭 판정) + Button(transition None) + Label(TMP, Pretendard) + 하이라이터\n\n" +
            "자세한 내역은 콘솔 로그를 확인하세요.",
            "확인");
    }
}

/* [파일 노트 — 타이틀 메뉴 프리팹 빌더]
 *
 * 실행: Unity 상단 메뉴 → Tools / Tup3 / Build Title Menu
 * 결과: Assets/Prefabs/TitleMenu.prefab (+ Start.unity 에 인스턴스 1개)
 *
 * [왜 만들었나]
 * TitleMenuView 는 원래 첫 Show() 때 코드로 계층을 만들었다. 그러면 (1) 에디터에서 미리 볼 수 없고
 * (2) 폰트를 UiViewBuilder.FindFallbackFont 가 "씬에서 처음 찾은 TMP 텍스트의 폰트"로 정하는데
 * Start.unity 에는 LiberationSans(한글 글리프 없음) 텍스트가 섞여 있어 한글이 □ 로 깨졌다.
 * 이 툴이 계층을 실제 오브젝트로 구워 두고 폰트를 명시적으로 박아 두 문제를 함께 없앤다.
 *
 * [폰트를 PRETENDARD-REGULAR 로 고른 근거]
 * 후보는 REGULAR(39MB)와 MEDIUM(2.7MB) 둘인데 MEDIUM 은 쓸 수 없다.
 *   - MEDIUM : m_AtlasPopulationMode 0(Static, 런타임 글리프 추가 불가) + 1024x1024 아틀라스,
 *              수록 문자 903자. "게/임/시/작/이/어/기/옵/션/종" 등 메뉴에 필요한 음절이 실제로 없다.
 *   - REGULAR: 4096x4096 아틀라스에 11,697자(현대 한글 음절 전체 수준). 필요한 음절이 전부 있고
 *              Start·Prologue·Ending 씬이 이미 쓰는 폰트라 룩도 일관된다.
 * 용량은 크지만 이미 프로젝트에 포함되어 다른 씬이 참조하는 에셋이라 이 툴 때문에 늘어나는 빌드
 * 용량은 0 이다. 프로젝트 전역 TMP Settings 의 fallback 은 건드리지 않는다(다른 UI 가 이미 정상).
 *
 * [계층]
 *   TitleMenu (비활성, RectTransform + Canvas(Overlay, sortingOrder 800) + CanvasScaler(1920x1080)
 *              + GraphicRaycaster + TitleMenuView)
 *     └ Items (VerticalLayoutGroup + ContentSizeFitter, anchoredPosition (0,-180))
 *         ├ NewGameItem      ┐ 각 항목 = Image(알파 0, raycastTarget true)
 *         ├ ContinueItem     │        + LayoutElement(420x54)
 *         ├ AchievementsItem │        + Button(transition None, targetGraphic = 그 Image)
 *         ├ OptionsItem      │        + TitleMenuItemHighlighter
 *         └ QuitItem         ┘
 *                                  └ Label (TMP, Pretendard Regular, 흰색 34, raycastTarget false)
 * 미니멀 컨셉 그대로다 — 배경 패널도 dim 도 버튼 외형도 없고 흰 글씨만 보인다. 클릭 판정만 필요해
 * 알파 0 Image 를 두고, 호버/선택 강조(볼드)는 TitleMenuItemHighlighter 가 전담한다.
 *
 * [루트를 비활성으로 저장하는 이유]
 * Start 씬은 Booting(1초) → IntroCutscene(최대 24초) → Menu 순서다. 씬에 활성 상태로 두면
 * 인트로 내내 메뉴가 보인다. 프리팹 루트를 SetActive(false) 로 저장해 두고
 * StartScene.Menu 가 페이드인 타이밍에 Show() 로 켜게 한다(Show 가 SetActive(true) 를 한다).
 * StartScene.ResolveMenuView 는 FindObjectsInactive.Include 로 찾으므로 비활성이어도 발견된다.
 *
 * [Canvas 를 프리팹에서는 Overlay 로 두는 이유]
 * 실제로 원하는 것은 ScreenSpaceCamera(Camera.main)다 — 그래야 페이드 스프라이트(sortingOrder 1000)가
 * 메뉴 위를 덮어 메뉴도 함께 서서히 드러난다. 하지만 카메라는 씬 참조라 프리팹에 구울 수 없다.
 * 그래서 프리팹에는 Overlay 로 저장하고, 런타임에 TitleMenuView.EnsureBuilt 가 배치본 경로에서도
 * UiViewBuilder.SetupOverlayCanvas 를 호출해 Camera.main 에 다시 묶는다.
 *
 * [멱등성 / 값 보존]
 * 프리팹이 이미 있으면 PrefabUtility.LoadPrefabContents 로 열어 "이름으로 찾고 없을 때만 생성"
 * 방식으로 갱신한다(EndingSceneBuilder 와 같은 규칙). 기존 자식의 fileID 가 유지되므로 씬
 * 인스턴스의 오버라이드도 살아남는다.
 *   - 항상 강제하는 것(구조·동작) : Canvas/Scaler/Raycaster 설정, VerticalLayoutGroup·ContentSizeFitter
 *     설정, Image.raycastTarget=true, Button.transition=None + targetGraphic,
 *     Label 의 raycastTarget=false 와 전면 스트레치, 한글이 안 되는 폰트의 교체.
 *     Button.onClick 은 건드리지 않는다 — 네 항목의 동작은 TitleMenuView 가 런타임에
 *     비영구(non-persistent) 리스너로 붙이므로 프리팹에 구울 대상이 아니다.
 *   - 새로 만들 때만 적용하고 이후로는 보존하는 것(내용) : 라벨 문구·색·크기·정렬,
 *     hitAreaColor, LayoutElement 크기.
 *   - 오브젝트 → 인스펙터로 되쓰는 것 : 라벨 4개의 문구, Items 의 anchoredPosition,
 *     VerticalLayoutGroup 의 spacing. 오브젝트를 손으로 고쳐도 인스펙터 필드가 실제와 어긋나지 않는다.
 * 폰트는 "현재 폰트가 없거나 HasCharacters 로 메뉴 한글을 못 그리는 경우"에만 교체한다.
 * 다른 한글 폰트를 일부러 지정해 두었다면 그대로 존중된다.
 *
 * [씬 배치]
 * DialogueUIPrefabBuilder 는 프리팹만 만들지만(여러 씬에 쓰이므로) 타이틀 메뉴는 Start 씬 전용이라
 * EndingSceneBuilder 처럼 배치까지 한다. 이미 TitleMenuView 가 있는 씬에는 다시 넣지 않는다.
 * 씬을 여닫으므로 실행 전에 SaveCurrentModifiedScenesIfUserWantsTo 로 현재 씬 저장을 먼저 묻는다.
 *
 * [도전과제 항목 추가 (2026-08-29)]
 * 항목이 5개가 되었다(게임 시작 / 이어하기 / 도전과제 / 옵션 / 게임 종료).
 * 이 툴을 다시 실행하는 것은 선택 사항이다 — 실행하지 않아도 TitleMenuView 가 런타임에
 * OptionsItem 을 복제해 AchievementsItem 을 만든다(TitleMenuView 파일 노트 참조).
 * 실행하면 그 복제 대신 프리팹에 실제 오브젝트가 생겨 에디터에서 미리 볼 수 있다.
 * 기존 프리팹에 새 항목을 추가하면 자식 맨 뒤에 붙으므로, Reorder 로 다섯 항목의
 * SiblingIndex 를 원하는 순서대로 다시 매긴다(멱등 — 몇 번을 돌려도 결과가 같다).
 * KoreanSample 에 "도전과제"를 추가해 폰트 한글 글리프 검사도 새 문구를 포함한다.
 */

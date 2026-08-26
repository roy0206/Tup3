using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DialogueUIPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/DialogueUI.prefab";
    private const string FontPath = "Assets/GameAssets/Fonts/PRETENDARD-REGULAR SDF.asset";

    [MenuItem("Tools/Tup3/Create Dialogue UI Prefab", false, 11)]
    public static void CreateDialogueUIPrefab()
    {
        if (File.Exists(PrefabPath))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Dialogue UI 프리팹 다시 만들기",
                $"'{PrefabPath}' 가 이미 있습니다.\n\n덮어쓰면 프리팹 내용이 새로 구성됩니다 (씬에 배치된 인스턴스는 유지되며 새 구조를 따라갑니다).\n계속할까요?",
                "덮어쓰기", "취소");

            if (!overwrite) return;
        }

        var root = BuildHierarchy();

        var folder = Path.GetDirectoryName(PrefabPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            Directory.CreateDirectory(folder);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
        Object.DestroyImmediate(root);

        if (!success || prefab == null)
        {
            Debug.LogError($"[DialogueUIPrefabBuilder] '{PrefabPath}' 저장에 실패했습니다.");
            return;
        }

        AssetDatabase.SaveAssets();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log($"[DialogueUIPrefabBuilder] '{PrefabPath}' 생성 완료.");
        EditorUtility.DisplayDialog(
            "Dialogue UI 프리팹 생성 완료",
            "DialogueUI.prefab 을 만들었습니다.\n\n" +
            "구성: Canvas(Overlay) + DialogueManager\n" +
            "· PlayerPanel / BossPanel / NarrationPanel\n" +
            "· ChoicePanel (구형 폴백)\n" +
            "· ChoiceView (신형 선택지 UI)\n\n" +
            "대화가 필요한 씬(보스룸, 로비 등)에 이 프리팹을 드래그해서 넣기만 하면 됩니다.\n" +
            "DialogueManager 는 씬 전용 싱글톤이라 씬당 하나만 배치하세요.",
            "확인");
    }

    private static GameObject BuildHierarchy()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            Debug.LogWarning($"[DialogueUIPrefabBuilder] '{FontPath}' 를 찾지 못했습니다. TMP 기본 폰트로 만들어집니다.");

        int uiLayer = LayerMask.NameToLayer("UI");

        var canvasGo = new GameObject("DialogueUI",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.layer = uiLayer;

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        scaler.referencePixelsPerUnit = 100f;

        var canvasRect = canvasGo.GetComponent<RectTransform>();

        CreateSpeakerPanel("PlayerPanel", "PlayerText", canvasRect, font, uiLayer);
        CreateSpeakerPanel("BossPanel", "BossText", canvasRect, font, uiLayer);
        CreateSpeakerPanel("NarrationPanel", "NarrationText", canvasRect, font, uiLayer);
        CreateChoicePanel(canvasRect, font, uiLayer);
        CreateChoiceView(canvasRect, uiLayer);

        var dialogueManager = canvasGo.AddComponent<DialogueManager>();

        var so = new SerializedObject(dialogueManager);
        var rootProp = so.FindProperty("dialogueRoot");
        if (rootProp != null) rootProp.objectReferenceValue = canvasRect;
        var skipProp = so.FindProperty("allowDialogueSkip");
        if (skipProp != null) skipProp.boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        return canvasGo;
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

    private static void CreateChoiceView(RectTransform parent, int uiLayer)
    {
        var go = new GameObject("ChoiceView", typeof(RectTransform));
        go.layer = uiLayer;
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        go.AddComponent<DialogueChoiceView>();
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
}

/* [파일 노트 — Dialogue UI 프리팹 빌더]
 *
 * 실행: Unity 상단 메뉴 → Tools / Tup3 / Create Dialogue UI Prefab
 * 결과: Assets/Prefabs/DialogueUI.prefab
 *
 * 구성 (PrologueSceneBuilder 의 DialogueCanvas 구성과 동일한 규칙):
 *   DialogueUI (Canvas Overlay, sortingOrder 10, ScaleWithScreenSize 1920x1080) + DialogueManager
 *     ├ PlayerPanel    / PlayerText      (비활성 시작)
 *     ├ BossPanel      / BossText        (비활성 시작)
 *     ├ NarrationPanel / NarrationText   (비활성 시작)
 *     ├ ChoicePanel    / Choice0,1       (구형 선택지 — DialogueChoiceView 폴백용이자
 *     │                                   DialogueManager.Awake 의 Find("ChoicePanel") 필수 대상)
 *     └ ChoiceView (DialogueChoiceView)  (신형 선택지 UI — 있으면 이쪽이 우선 사용됨)
 *
 * - DialogueManager.dialogueRoot 는 캔버스 자신으로 연결돼 있고, allowDialogueSkip 은 true 로 저장된다.
 * - 패널 이름은 DialogueManager.Awake 가 이름으로 Find 하므로 바꾸면 안 된다.
 *   ChoicePanel 은 "선택지 오브젝트 > TMP 자식" 2단 구조여야 choiceObjects 계산이 맞는다.
 * - 씬 배치: 대화가 필요한 씬에 프리팹을 드래그하면 끝. DomainSingleton 이라 씬당 하나만.
 * - Boss_Soil 등 기존 씬에 이미 대화 UI가 있으면 그쪽을 지우고 이 프리팹으로 교체하는 것을 권장
 *   (씬마다 제각각인 구조를 프리팹 하나로 통일).
 * - 캔버스는 Overlay 라 카메라 참조가 필요 없어 어느 씬에서든 그대로 동작한다.
 *   참격(Pattern4Slash, sortingOrder 32767)보다는 아래에 그려진다.
 */

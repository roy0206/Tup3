using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LegacyBossHealthBarCleaner
{
    private const string MenuPath = "Tools/Tup3/Remove Legacy Boss Health Bars";
    private const string CanvasName = "WorldCanvas";

    private static readonly string[] PrefabPaths =
    {
        "Assets/Prefabs/FinalBoss.prefab",
        "Assets/Prefabs/Gold.prefab",
        "Assets/Prefabs/Soil.prefab",
    };

    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Boss_Soil.unity",
        "Assets/Scenes/Boss_Water.unity",
        "Assets/Scenes/Boss_Fire.unity",
        "Assets/Scenes/Boss_Gold.unity",
        "Assets/Scenes/Styx.unity",
    };

    [MenuItem(MenuPath, false, 16)]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[LegacyBossHealthBarCleaner] 씬 저장을 취소해 중단했습니다.");
            return;
        }

        var report = new List<string>();

        foreach (string path in PrefabPaths) CleanPrefab(path, report);
        foreach (string path in ScenePaths) CleanScene(path, report);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string body = report.Count > 0
            ? string.Join("\n", report)
            : "제거할 구 체력바가 없습니다. 이미 정리된 상태입니다.";

        Debug.Log($"[LegacyBossHealthBarCleaner]\n{body}");
        EditorUtility.DisplayDialog("구 보스 체력바 정리", body, "확인");
    }

    private static void CleanPrefab(string path, List<string> report)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null) return;

        int removed = RemoveIn(root, report, path);

        if (removed > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void CleanScene(string path, List<string> report)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) return;

        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        if (!scene.IsValid()) return;

        int removed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            removed += RemoveIn(root, report, path);

        if (removed > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static int RemoveIn(GameObject root, List<string> report, string owner)
    {
        var targets = new List<Transform>();

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == null) continue;
            if (t.name != CanvasName) continue;
            if (t.GetComponentInParent<BossBase>() == null) continue;

            targets.Add(t);
        }

        int removed = 0;
        foreach (Transform t in targets)
        {
            if (t == null) continue;

            string bossName = t.GetComponentInParent<BossBase>().name;
            Object.DestroyImmediate(t.gameObject);
            report.Add($"{System.IO.Path.GetFileName(owner)} : {bossName} 의 {CanvasName} 제거");
            removed++;
        }

        return removed;
    }
}

/* [파일 노트]
 *
 * 구 보스 체력바 제거 도구. 보스 체력바가 PlayerUI 의 BossHealth(BossHealthView)로 옮겨지면서
 * 각 보스의 자식으로 있던 WorldCanvas > Health(HealthView) > Red 계층이 폐기됐다(2026-08-29 유저 확정).
 * 그대로 두면 새 체력바와 구 체력바가 동시에 보인다.
 *
 * 왜 스크립트인가: 프리팹/씬 YAML 을 직접 잘라내면 GameObject·컴포넌트·m_Children 참조를 모두
 * 손으로 맞춰야 해서 계층이 깨질 위험이 크다. Unity 에 맡기면 프리팹 인스턴스 갱신까지 정확히 처리된다.
 *
 * 안전장치:
 * - "WorldCanvas" 라는 이름만으로 지우지 않는다. 부모 계통에 BossBase 가 있는 것만 대상으로 삼으므로
 *   보스와 무관한 월드 캔버스(있다면)는 건드리지 않는다.
 * - 멱등하다. 이미 정리된 프로젝트에서 다시 돌리면 아무것도 지우지 않고 그렇게 보고한다.
 * - 씬을 열기 전에 현재 씬 저장 여부를 먼저 묻는다. 취소하면 아무 작업도 하지 않는다.
 * - 실제로 지운 것이 있을 때만 프리팹/씬을 저장한다.
 *
 * FinalBoss 프리팹은 FinalBossSceneBuilder 의 RemoveLegacyBossHealthBar 도 같은 일을 하므로
 * 어느 쪽을 돌려도 결과가 같다.
 *
 * 주의: 이 도구는 씬을 Single 모드로 열고 닫는다. 실행 후에는 마지막으로 처리된 씬이 열려 있다.
 */

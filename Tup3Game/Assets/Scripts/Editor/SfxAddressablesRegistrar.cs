using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class SfxAddressablesRegistrar
{
    private const string SfxFolder = "Assets/AddressableAssets/Sounds/SFX";
    private const string GroupName = "Sounds";
    private const string LabelName = "Sound";

    private static readonly List<string> report = new List<string>();

    [MenuItem("Tools/Tup3/Register SFX Addressables", false, 30)]
    public static void RegisterSfxAddressables()
    {
        report.Clear();

        if (!AssetDatabase.IsValidFolder(SfxFolder))
        {
            EditorUtility.DisplayDialog("SFX Addressables 등록 실패",
                $"폴더를 찾을 수 없습니다:\n{SfxFolder}", "확인");
            return;
        }

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            EditorUtility.DisplayDialog("SFX Addressables 등록 실패",
                "AddressableAssetSettings 를 찾을 수 없습니다.\n" +
                "Window / Asset Management / Addressables / Groups 를 한 번 연 뒤 다시 실행하세요.", "확인");
            return;
        }

        AddressableAssetGroup group = settings.FindGroup(GroupName);
        if (group == null)
        {
            EditorUtility.DisplayDialog("SFX Addressables 등록 실패",
                $"Addressables 그룹 '{GroupName}' 을 찾을 수 없습니다.\n" +
                "그룹 이름이 바뀌었는지 확인하세요.", "확인");
            return;
        }

        EnsureLabel(settings);

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { SfxFolder });

        int added = 0;
        int updated = 0;
        int untouched = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!IsDirectChild(path)) continue;

            string address = Path.GetFileNameWithoutExtension(path);

            AddressableAssetEntry existing = settings.FindAssetEntry(guid);
            bool isNew = existing == null;
            bool movedGroup = !isNew && existing.parentGroup != group;

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry == null)
            {
                report.Add($"[실패] 엔트리 생성 불가 — {path}");
                continue;
            }

            bool changed = movedGroup;

            if (entry.address != address)
            {
                entry.SetAddress(address, false);
                changed = true;
            }

            if (!entry.labels.Contains(LabelName))
            {
                entry.SetLabel(LabelName, true, false, false);
                changed = true;
            }

            if (isNew)
            {
                added++;
                report.Add($"[추가] {address}  ({path})");
            }
            else if (changed)
            {
                updated++;
                report.Add($"[갱신] {address}  ({path})");
            }
            else
            {
                untouched++;
            }
        }

        EditorUtility.SetDirty(group);
        EditorUtility.SetDirty(settings);
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, null, true, true);
        AssetDatabase.SaveAssets();

        LogSummary(group, added, updated, untouched);
    }

    private static bool IsDirectChild(string assetPath)
    {
        string dir = Path.GetDirectoryName(assetPath);
        if (string.IsNullOrEmpty(dir)) return false;
        return dir.Replace('\\', '/') == SfxFolder;
    }

    private static void EnsureLabel(AddressableAssetSettings settings)
    {
        List<string> labels = settings.GetLabels();
        if (labels != null && labels.Contains(LabelName)) return;

        settings.AddLabel(LabelName, true);
        report.Add($"[라벨] '{LabelName}' 라벨을 새로 생성했습니다.");
    }

    private static void LogSummary(AddressableAssetGroup group, int added, int updated, int untouched)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== SFX Addressables 등록 결과 ===");
        sb.AppendLine($"대상 폴더 : {SfxFolder}");
        sb.AppendLine($"그룹      : {GroupName} (총 엔트리 {group.entries.Count}개)");
        sb.AppendLine($"라벨      : {LabelName}");
        sb.AppendLine($"추가 {added}개 / 갱신 {updated}개 / 변경없음 {untouched}개");

        if (report.Count > 0)
        {
            sb.AppendLine("---");
            foreach (string line in report) sb.AppendLine(line);
        }

        Debug.Log(sb.ToString());

        EditorUtility.DisplayDialog("SFX Addressables 등록 완료",
            $"추가 {added}개 / 갱신 {updated}개 / 변경없음 {untouched}개\n" +
            $"'{GroupName}' 그룹 총 엔트리 {group.entries.Count}개\n\n" +
            "자세한 내역은 콘솔 로그를 확인하세요.",
            "확인");
    }
}

/* [파일 노트 — SFX Addressables 일괄 등록기]
 *
 * 실행: Tools / Tup3 / Register SFX Addressables — 몇 번을 다시 실행해도 중복 엔트리가
 * 생기지 않는다(멱등).
 *
 * 하는 일:
 *   1. Assets/AddressableAssets/Sounds/SFX 바로 아래의 모든 AudioClip 을 찾는다.
 *      (하위 폴더는 훑지 않는다 — IsDirectChild 로 걸러낸다. 지금 구조상 SFX 는 평평하게
 *       두기로 했으므로, 하위 폴더가 생기면 의도치 않은 등록을 막기 위함이다.)
 *   2. 각 클립을 'Sounds' 그룹에 CreateOrMoveEntry 로 등록한다. 이미 다른 그룹에 있으면
 *      Sounds 로 옮기고 '갱신'으로 집계한다.
 *   3. 주소(address)를 확장자 없는 파일명으로 맞춘다. AudioManager 는 Addressables 로
 *      라벨 전체를 프리로드한 뒤 clip.name(= 파일명) 으로 조회하므로, 주소를 파일명과
 *      같게 두어야 Addressables Groups 창에서 보기에도 헷갈리지 않는다.
 *   4. 'Sound' 라벨을 붙인다. 라벨이 설정에 없으면 AddLabel 로 먼저 만든다.
 *      실제 프리로드 라벨은 Core.prefab 의 AudioManager.preloadLabel = "Sound" 이다
 *      (스크립트 기본값 "Audio" 가 아니다 — 프리팹 값이 우선한다).
 *
 * 건드리지 않는 것: 기존 BGM 12개와 ItemObtain 엔트리. 이들은 이미 Sounds 그룹 +
 * Sound 라벨 + 올바른 주소 상태라 '변경없음'으로만 집계된다. 엔트리 삭제는 하지 않는다.
 *
 * 등록 후: Addressables 프로필이 로컬 재생용이면 에디터 Play 는 바로 되지만, 실제
 * 빌드/Existing Build 재생 모드를 쓴다면 Addressables 그룹 빌드를 다시 해야 반영된다.
 */

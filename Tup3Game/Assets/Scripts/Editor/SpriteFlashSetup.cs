using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SpriteFlashSetup
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string FlashFolder = "Assets/Resources/SpriteFlash";

    private struct MaterialDef
    {
        public string shaderName;
        public string assetPath;
        public string materialName;
    }

    private static readonly MaterialDef[] Definitions =
    {
        new MaterialDef
        {
            shaderName = SpriteFlashShaders.LitShaderName,
            assetPath = SpriteFlashShaders.LitMaterialAssetPath,
            materialName = "SpriteFlashLit"
        },
        new MaterialDef
        {
            shaderName = SpriteFlashShaders.UnlitShaderName,
            assetPath = SpriteFlashShaders.UnlitMaterialAssetPath,
            materialName = "SpriteFlashUnlit"
        }
    };

    [MenuItem("Tools/Tup3/Setup Sprite Flash", false, 31)]
    public static void Setup()
    {
        List<string> report = new List<string>();
        List<string> errors = new List<string>();

        if (!EnsureFolder(ResourcesFolder, errors)) { Finish(report, errors); return; }
        if (!EnsureFolder(FlashFolder, errors)) { Finish(report, errors); return; }

        bool touched = false;

        foreach (MaterialDef def in Definitions)
        {
            Shader shader = Shader.Find(def.shaderName);
            if (shader == null)
            {
                errors.Add($"셰이더를 찾을 수 없습니다: {def.shaderName}\n" +
                           "Assets/Shaders/ 안의 .shader 파일이 임포트됐는지 확인하세요.");
                continue;
            }

            if (ShaderUtil.ShaderHasError(shader))
            {
                errors.Add($"셰이더에 컴파일 에러가 있습니다: {def.shaderName}\n" +
                           "Console 창에서 상세 내용을 확인하세요.");
                continue;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(def.assetPath);

            if (material == null)
            {
                material = new Material(shader);
                material.name = def.materialName;
                AssetDatabase.CreateAsset(material, def.assetPath);
                report.Add($"생성: {def.assetPath}");
                touched = true;
            }
            else
            {
                bool changed = false;

                if (material.shader != shader)
                {
                    material.shader = shader;
                    changed = true;
                }

                if (material.HasProperty(SpriteFlashShaders.FlashAmountId) &&
                    !Mathf.Approximately(material.GetFloat(SpriteFlashShaders.FlashAmountId), 0f))
                {
                    material.SetFloat(SpriteFlashShaders.FlashAmountId, 0f);
                    changed = true;
                }

                if (material.HasProperty(SpriteFlashShaders.FlashColorId) &&
                    material.GetColor(SpriteFlashShaders.FlashColorId) != Color.white)
                {
                    material.SetColor(SpriteFlashShaders.FlashColorId, Color.white);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(material);
                    report.Add($"갱신: {def.assetPath}");
                    touched = true;
                }
                else
                {
                    report.Add($"유지: {def.assetPath}");
                }
            }
        }

        if (touched)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        SpriteFlashShaders.ResetCache();
        Finish(report, errors);
    }

    [MenuItem("Tools/Tup3/Add Sprite Flash To Selection", false, 32)]
    public static void AddToSelection()
    {
        GameObject[] targets = Selection.gameObjects;
        if (targets == null || targets.Length == 0)
        {
            EditorUtility.DisplayDialog("Sprite Flash", "Hierarchy 에서 오브젝트를 먼저 선택하세요.", "확인");
            return;
        }

        int added = 0;
        int skipped = 0;

        foreach (GameObject target in targets)
        {
            if (target.GetComponent<SpriteRenderer>() != null)
            {
                if (target.GetComponent<SpriteFlash>() != null) { skipped++; continue; }
                Undo.AddComponent<SpriteFlash>(target);
                added++;
            }
            else
            {
                if (target.GetComponent<SpriteFlashGroup>() != null) { skipped++; continue; }
                Undo.AddComponent<SpriteFlashGroup>(target);
                added++;
            }
        }

        EditorUtility.DisplayDialog("Sprite Flash",
            $"추가 {added}개 / 이미 있어서 건너뜀 {skipped}개", "확인");
    }

    [MenuItem("Tools/Tup3/Add Sprite Flash To Selection", true)]
    public static bool AddToSelectionValidate()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }

    private static bool EnsureFolder(string path, List<string> errors)
    {
        if (AssetDatabase.IsValidFolder(path)) return true;

        int lastSlash = path.LastIndexOf('/');
        string parent = path.Substring(0, lastSlash);
        string leaf = path.Substring(lastSlash + 1);

        if (!AssetDatabase.IsValidFolder(parent))
        {
            errors.Add($"상위 폴더가 없습니다: {parent}");
            return false;
        }

        AssetDatabase.CreateFolder(parent, leaf);
        return AssetDatabase.IsValidFolder(path);
    }

    private static void Finish(List<string> report, List<string> errors)
    {
        if (errors.Count > 0)
        {
            EditorUtility.DisplayDialog("Sprite Flash 셋업 실패",
                string.Join("\n\n", errors), "확인");
            return;
        }

        EditorUtility.DisplayDialog("Sprite Flash 셋업 완료",
            string.Join("\n", report) +
            "\n\n런타임에 SpriteFlash / SpriteFlashGroup 이 이 머티리얼을 자동으로 꽂습니다.", "확인");
    }
}

/* [파일 노트]
 * Tools/Tup3/Setup Sprite Flash 한 번으로 플래시 시스템이 필요로 하는 에셋을 전부 만든다.
 * 만드는 것은 Assets/Resources/SpriteFlash/ 아래의 SpriteFlashLit.mat / SpriteFlashUnlit.mat 두 개다.
 * 이미 있고 셰이더와 기본값이 맞으면 아무것도 건드리지 않는다(멱등).
 *
 * 왜 에디터 스크립트로 .mat 을 만드는가
 *   .mat 을 손으로 YAML 로 쓰려면 셰이더의 GUID 를 알아야 하는데, .shader 파일이 아직 Unity 에
 *   임포트되지 않아 .meta 가 없으므로 GUID 가 존재하지 않는다. 셰이더가 임포트된 뒤 에디터에서
 *   Shader.Find 로 찾아 머티리얼을 생성하는 것이 유일하게 안전한 방법이다.
 *
 * 셰이더 스트립 방지
 *   Shader.Find 만 쓰고 아무 에셋도 셰이더를 참조하지 않으면 빌드에서 셰이더가 빠진다.
 *   여기서 만드는 두 머티리얼이 Assets/Resources/ 안에 있으므로 항상 빌드에 포함되고,
 *   그 참조를 따라 셰이더도 함께 포함된다. 런타임 로드도 이 경로(Resources.Load)로 한다.
 *
 *   Graphics Settings 의 Always Included Shaders 를 쓰지 않은 이유는 변형 폭발 때문이다.
 *   Lit 쪽은 USE_SHAPE_LIGHT_TYPE_0~3(16) x instancing x SKINNED_SPRITE x DEBUG_DISPLAY 조합이라
 *   Always Included 로 넣으면 전 변형을 무조건 컴파일해 빌드 시간과 용량이 크게 늘어난다.
 *   머티리얼 참조 방식은 URP 의 기본 변형 스트리핑이 그대로 적용되므로 필요한 것만 남는다.
 *
 * 셰이더 컴파일 에러 확인
 *   ShaderUtil.ShaderHasError 로 미리 걸러서, 에러 난 셰이더로 머티리얼을 만들어 두고
 *   런타임에 분홍색이 나오는 상황을 막는다.
 *
 * Add Sprite Flash To Selection
 *   선택한 오브젝트에 SpriteRenderer 가 있으면 SpriteFlash 를, 없으면 SpriteFlashGroup 을 붙인다.
 *   Undo 로 되돌릴 수 있고, 이미 붙어 있으면 건너뛴다. 보스는 BossBase 가 런타임에 알아서
 *   SpriteFlashGroup 을 붙이므로 이 메뉴를 쓸 필요가 없다.
 */

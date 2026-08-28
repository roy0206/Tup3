using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;

public static class FinalBossSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Styx.unity";
    private const string FinalBossPrefabPath = "Assets/Prefabs/FinalBoss.prefab";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string DialogueUIPrefabPath = "Assets/Prefabs/DialogueUI.prefab";
    private const string SoilBossPrefabPath = "Assets/Prefabs/Soil.prefab";
    private const string WaterEyePrefabPath = "Assets/Prefabs/Water/Water_eye_1.prefab";
    private const string FireBossScenePath = "Assets/Scenes/Boss_Fire.unity";
    private const string SoilWavePrefabPath = "Assets/AddressableAssets/Final/SoilWave.prefab";
    private const string SoilDropPrefabPath = "Assets/AddressableAssets/Soil/SoilDrop.prefab";

    private const string SoilPhantomSpritePath = "Assets/GameAssets/Bosses/Boss_Soil/boss_soil.png";
    private const string WaterPhantomSpritePath = "Assets/GameAssets/Bosses/Boss_Water/eye3_1.png";
    private const string FirePhantomSpritePath = "Assets/GameAssets/Bosses/Boss_Fire/boss_fire.png";
    private const string SoilWaveSpritePathA = "Assets/GameAssets/Bosses/Boss_Soil/Pattern1/soil_effect2_1.png";
    private const string SoilWaveSpritePathB = "Assets/GameAssets/Bosses/Boss_Soil/Pattern1/soil_effect1_1.png";

    private const string SoilIdleClipPath = "Assets/GameAssets/Bosses/Boss_Soil/Animations/SoilIdle.anim";
    private const string SoilPattern1ClipPath = "Assets/GameAssets/Bosses/Boss_Soil/Animations/SoilPattern1.anim";
    private const string FireIdleClipPath = "Assets/GameAssets/Bosses/Boss_Fire/Animations/Idle.anim";
    private const string FireWarnClipPath = "Assets/GameAssets/Bosses/Boss_Fire/Animations/Warn.anim";
    private const string FireRushClipPath = "Assets/GameAssets/Bosses/Boss_Fire/Animations/Rush.anim";
    private const string WaterIdleClipPath = "Assets/GameAssets/Bosses/Boss_Water/Eye_3_animation/Eye_3.anim";
    private const string PhantomControllerFolder = "Assets/AddressableAssets/Final";

    private const string SquareSpriteGuid = "311925a002f4447b3a28927169b83ea6";
    private const string HealthRedSpriteGuid = "e1a07fe9b17c0ce48847131f88a041a7";

    private const string SpriteShapeProfilePath =
        "Packages/com.unity.2d.spriteshape/Editor/ObjectMenuCreation/DefaultAssets/Sprite Shape Profiles/Sprite Shape Profile.asset";
    private const string SpriteLitDefaultMaterialPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
    private const float WaterSurfaceLocalY = 0.5f;

    private const string AddressableGroupName = "Final";
    private const string SourceGroupName = "Gold";
    private const string SoilWaveAddress = "SoilWave";
    private const string PoolLabel = "Pool";
    private const string BossAssetLabel = "BossAsset";

    private const float BossMaxHp = 300f;
    private static readonly Vector3 BossSpawnPosition = new Vector3(5f, -1f, 0f);
    private static readonly Vector3 BossScale = new Vector3(2f, 2f, 1f);
    private static readonly Color BossTint = new Color(0.42f, 0.16f, 0.36f, 1f);

    private static readonly List<string> report = new List<string>();
    private static readonly List<string> todo = new List<string>();

    [MenuItem("Tools/Tup3/Setup Final Boss Scene (Styx)", false, 12)]
    public static void SetupFinalBossScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Styx 씬 없음", $"'{ScenePath}' 를 찾지 못했습니다.", "확인");
            return;
        }

        report.Clear();
        todo.Clear();

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        EnsureSoilWavePrefab(false);

        FindPlayer(scene);
        GameObject boss;
        if (File.Exists(FinalBossPrefabPath))
        {
            ConfigureBossPrefabAsset();
            boss = EnsureBossInstanceInScene(scene, out bool placedNow);
            ApplySceneOverrides(scene, boss, placedNow);
        }
        else
        {
            Debug.LogWarning($"[FinalBossSceneBuilder] '{FinalBossPrefabPath}' 가 없어 씬에 직접 구성합니다. " +
                             "보스를 프리팹으로 저장(프리팹화)해 두면 이후 빌더가 프리팹 에셋을 직접 갱신합니다 (권장).");
            boss = BuildFinalBossInScene(scene);
        }
        BuildFinalBossRoom(scene, boss);
        BuildShallowWater(scene);
        EnsureDialogueUI(scene);
        CleanupLegacyObjects(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        if (!saved)
        {
            Debug.LogError("[FinalBossSceneBuilder] 씬 저장에 실패했습니다.");
            return;
        }

        Selection.activeGameObject = boss;
        LogSummary();
    }

    [MenuItem("Tools/Tup3/Create SoilWave Prefab", false, 13)]
    public static void CreateSoilWavePrefabMenu()
    {
        bool rebuild = false;
        if (File.Exists(SoilWavePrefabPath))
        {
            rebuild = EditorUtility.DisplayDialog(
                "SoilWave 프리팹 다시 만들기",
                $"'{SoilWavePrefabPath}' 가 이미 있습니다.\n\n덮어쓸까요? (Addressables 등록은 유지됩니다)",
                "덮어쓰기", "취소");
            if (!rebuild) return;
        }

        report.Clear();
        todo.Clear();
        EnsureSoilWavePrefab(rebuild);
        AssetDatabase.SaveAssets();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SoilWavePrefabPath);
        if (prefab != null)
        {
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
        Debug.Log($"[FinalBossSceneBuilder] SoilWave 프리팹 처리 완료:\n - {string.Join("\n - ", report)}");
    }

    private static GameObject FindPlayer(Scene scene)
    {
        var movement = Object.FindObjectOfType<Playermovement>(true);
        if (movement != null) return movement.gameObject;

        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged == null)
            Debug.LogWarning("[FinalBossSceneBuilder] 씬에서 Player 를 찾지 못했습니다.");
        return tagged;
    }

    private static void ConfigureBossPrefabAsset()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(FinalBossPrefabPath);
        try
        {
            ConfigureBoss(root, null);
            PrefabUtility.SaveAsPrefabAsset(root, FinalBossPrefabPath);
            report.Add($"FinalBoss 프리팹 에셋 직접 갱신: {FinalBossPrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static GameObject EnsureBossInstanceInScene(Scene scene, out bool placedNow)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FinalBossPrefabPath);
        GameObject existing = FindInSceneNoCreate(scene, "FinalBoss");

        if (existing != null)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(existing);
            if (source != null && AssetDatabase.GetAssetPath(source) == FinalBossPrefabPath)
            {
                placedNow = false;
                report.Add("씬의 FinalBoss 프리팹 인스턴스 재사용 (위치/스케일 유지)");
                return existing;
            }

            Vector3 keepPos = existing.transform.position;
            Object.DestroyImmediate(existing);
            var replaced = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            replaced.transform.position = keepPos;
            placedNow = true;
            report.Add("씬의 비프리팹 FinalBoss 오브젝트를 프리팹 인스턴스로 교체 (위치 승계)");
            return replaced;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.transform.position = BossSpawnPosition;
        placedNow = true;
        report.Add($"FinalBoss 프리팹 인스턴스 배치 (위치 {BossSpawnPosition})");
        return instance;
    }

    private static void ApplySceneOverrides(Scene scene, GameObject boss, bool placedNow)
    {
        BoxCollider2D normalCol = null;
        var finalBoss = boss.GetComponent<FinalBoss>();
        if (finalBoss != null)
        {
            var so = new SerializedObject(finalBoss);
            SetInt(so, "groundMask", ResolveGroundMask(scene));
            var hurtboxProp = so.FindProperty("normalHurtbox");
            if (hurtboxProp != null) normalCol = hurtboxProp.objectReferenceValue as BoxCollider2D;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        if (normalCol == null) normalCol = boss.GetComponent<BoxCollider2D>();

        SnapBossAboveGround(scene, boss, normalCol, placedNow);
        report.Add("씬 인스턴스 오버라이드 적용 (groundMask/바닥 스냅)");
    }

    private static GameObject BuildFinalBossInScene(Scene scene)
    {
        bool created;
        GameObject boss = FindInScene(scene, "FinalBoss", out created);
        if (created)
        {
            boss.transform.position = BossSpawnPosition;
            boss.transform.localScale = BossScale;
            report.Add($"FinalBoss 오브젝트 생성 (위치 {BossSpawnPosition}, 스케일 {BossScale.x}배)");
        }
        else
        {
            report.Add("FinalBoss 오브젝트 갱신 (위치/스케일은 손대지 않음)");
        }

        BoxCollider2D normalCol = ConfigureBoss(boss, FindMainCamera(scene));

        var finalBoss = boss.GetComponent<FinalBoss>();
        if (finalBoss != null)
        {
            var so = new SerializedObject(finalBoss);
            SetInt(so, "groundMask", ResolveGroundMask(scene));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        SnapBossAboveGround(scene, boss, normalCol, created);
        return boss;
    }

    private static BoxCollider2D ConfigureBoss(GameObject boss, Camera worldCamera)
    {
        boss.tag = "Enemy";
        boss.layer = 0;

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        SpriteRenderer playerSr = null;
        Animator playerAnimator = null;
        if (playerPrefab != null)
        {
            playerSr = playerPrefab.GetComponent<SpriteRenderer>();
            if (playerSr == null) playerSr = playerPrefab.GetComponentInChildren<SpriteRenderer>(true);
            playerAnimator = playerPrefab.GetComponent<Animator>();
            if (playerAnimator == null) playerAnimator = playerPrefab.GetComponentInChildren<Animator>(true);
        }
        else
        {
            Debug.LogWarning($"[FinalBossSceneBuilder] '{PlayerPrefabPath}' 를 찾지 못했습니다.");
        }

        var sr = EnsureComponent<SpriteRenderer>(boss);
        if (playerSr != null)
        {
            sr.sprite = playerSr.sprite;
            sr.sharedMaterial = playerSr.sharedMaterial;
            sr.sortingLayerID = playerSr.sortingLayerID;
        }
        sr.sortingOrder = 1;
        if (sr.color == Color.white)
        {
            sr.color = BossTint;
            report.Add("보스 스프라이트 = 플레이어 스프라이트 + 리컬러 틴트");
        }

        var animator = EnsureComponent<Animator>(boss);
        if (playerAnimator != null && playerAnimator.runtimeAnimatorController != null)
        {
            animator.runtimeAnimatorController = playerAnimator.runtimeAnimatorController;
            report.Add("Animator = 플레이어 RuntimeAnimatorController 공유");
        }

        var colliders = new List<BoxCollider2D>(boss.GetComponents<BoxCollider2D>());
        while (colliders.Count < 4) colliders.Add(boss.AddComponent<BoxCollider2D>());
        BoxCollider2D normalCol = colliders[0];
        BoxCollider2D soilCol = colliders[1];
        BoxCollider2D waterCol = colliders[2];
        BoxCollider2D fireCol = colliders[3];

        Vector2 normalSize = sr.sprite != null ? (Vector2)sr.sprite.bounds.size : new Vector2(1f, 2f);
        Vector2 normalOffset = sr.sprite != null ? (Vector2)sr.sprite.bounds.center : Vector2.zero;
        ApplyCollider(normalCol, normalSize, normalOffset, false, true);
        report.Add($"기본 히트박스: 플레이어 스프라이트 크기 {Fmt(normalSize)}");

        ApplyElementCollider(soilCol, "토", ReadColliderFromPrefab(SoilBossPrefabPath), normalSize, normalOffset);
        ApplyElementCollider(waterCol, "수", ReadColliderFromPrefab(WaterEyePrefabPath), normalSize, normalOffset);
        ApplyElementCollider(fireCol, "화", ReadColliderFromScene(FireBossScenePath, "Fire"), normalSize, normalOffset);

        Sprite squareSprite = LoadSpriteByGuid(SquareSpriteGuid);
        GameObject soilPhantom = BuildSoilPhantomRig(boss);
        GameObject waterPhantom = BuildPhantom(boss, "WaterPhantom", WaterPhantomSpritePath, WaterEyePrefabPath);
        GameObject firePhantom = BuildPhantom(boss, "FirePhantom", FirePhantomSpritePath, null);
        AttachPhantomAnimation(waterPhantom, WaterIdleClipPath);
        AttachPhantomAnimation(firePhantom, FireIdleClipPath, FireWarnClipPath, FireRushClipPath);
        GameObject darkOverlay = BuildIaiDarkOverlay(boss, squareSprite);
        GameObject flashEffect = BuildIaiFlashEffect(boss, squareSprite);

        var finalBoss = EnsureComponent<FinalBoss>(boss);
        var so = new SerializedObject(finalBoss);

        var listProp = so.FindProperty("boxColliders");
        if (listProp != null && listProp.isArray)
        {
            listProp.arraySize = 1;
            listProp.GetArrayElementAtIndex(0).objectReferenceValue = normalCol;
        }

        SetFloat(so, "maxHp", BossMaxHp);
        SetBool(so, "spriteFacesRight", true);
        SetObjectReference(so, "animator", animator);
        SetObjectReference(so, "spriteRenderer", sr);
        SetObjectReference(so, "normalHurtbox", normalCol);
        SetObjectReference(so, "soilHurtbox", soilCol);
        SetObjectReference(so, "waterHurtbox", waterCol);
        SetObjectReference(so, "fireHurtbox", fireCol);
        SetObjectReference(so, "soilPhantom", soilPhantom);
        SetObjectReference(so, "waterPhantom", waterPhantom);
        SetObjectReference(so, "firePhantom", firePhantom);
        SetObjectReference(so, "iaiDarkOverlay", darkOverlay.GetComponent<SpriteRenderer>());
        SetObjectReference(so, "iaiFlashEffect", flashEffect);
        so.ApplyModifiedPropertiesWithoutUndo();

        RemoveLegacyBossHealthBar(boss);

        report.Add("FinalBoss 컴포넌트 배선 완료 (maxHp 300, 히트박스 4종/환영 3종/거합 연출 — groundMask 는 씬 오버라이드)");
        return normalCol;
    }

    private static void SnapBossAboveGround(Scene scene, GameObject boss, BoxCollider2D bodyCol, bool created)
    {
        GameObject square = FindInSceneNoCreate(scene, "Square");
        if (square == null || bodyCol == null) return;

        var groundCol = square.GetComponent<BoxCollider2D>();
        if (groundCol == null) return;

        float groundTop = square.transform.position.y
            + (groundCol.offset.y + groundCol.size.y * 0.5f) * Mathf.Abs(square.transform.lossyScale.y);
        float bottomOffset = (bodyCol.offset.y - bodyCol.size.y * 0.5f) * Mathf.Abs(boss.transform.lossyScale.y);
        float bossBottom = boss.transform.position.y + bottomOffset;

        bool buried = bossBottom < groundTop - 0.001f;
        if (!created && !buried) return;

        Vector3 pos = boss.transform.position;
        pos.y = groundTop - bottomOffset + 0.01f;
        boss.transform.position = pos;
        report.Add($"FinalBoss 를 바닥 표면 위로 스냅 (y={pos.y:F2})");
    }

    private static void ApplyElementCollider(BoxCollider2D col, string label, ColliderInfo? source,
        Vector2 fallbackSize, Vector2 fallbackOffset)
    {
        if (source.HasValue)
        {
            ApplyCollider(col, source.Value.size, source.Value.offset, source.Value.isTrigger, false);
            report.Add($"{label} 히트박스: 원본 보스 몸통 콜라이더 복제 {Fmt(source.Value.size)} ({source.Value.source})");
        }
        else
        {
            ApplyCollider(col, fallbackSize, fallbackOffset, false, false);
            report.Add($"{label} 히트박스: 원본을 읽지 못해 기본 히트박스와 동일 크기 사용");
        }
    }

    private static void ApplyCollider(BoxCollider2D col, Vector2 size, Vector2 offset, bool isTrigger, bool enabled)
    {
        col.size = size;
        col.offset = offset;
        col.isTrigger = isTrigger;
        col.enabled = enabled;
    }

    private struct ColliderInfo
    {
        public Vector2 size;
        public Vector2 offset;
        public bool isTrigger;
        public string source;
    }

    private static ColliderInfo? ReadColliderFromPrefab(string prefabPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return null;

        var box = prefab.GetComponent<BoxCollider2D>();
        var bossBase = prefab.GetComponent<BossBase>();
        if (bossBase != null && bossBase.TryGetComponent(out BoxCollider2D bodyBox)) box = bodyBox;
        if (box != null)
        {
            return new ColliderInfo
            {
                size = box.size,
                offset = box.offset,
                isTrigger = box.isTrigger,
                source = prefabPath
            };
        }

        var circle = prefab.GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            float d = circle.radius * 2f;
            return new ColliderInfo
            {
                size = new Vector2(d, d),
                offset = circle.offset,
                isTrigger = circle.isTrigger,
                source = prefabPath
            };
        }

        return null;
    }

    private static ColliderInfo? ReadColliderFromScene(string scenePath, string bossClassName)
    {
        if (!File.Exists(scenePath)) return null;

        Scene opened = default;
        try
        {
            opened = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            foreach (var root in opened.GetRootGameObjects())
            {
                foreach (var bossBase in root.GetComponentsInChildren<BossBase>(true))
                {
                    if (bossBase.GetType().Name != bossClassName) continue;
                    if (!bossBase.TryGetComponent(out BoxCollider2D box)) continue;

                    return new ColliderInfo
                    {
                        size = box.size,
                        offset = box.offset,
                        isTrigger = box.isTrigger,
                        source = scenePath
                    };
                }
            }
        }
        finally
        {
            if (opened.IsValid() && opened.isLoaded && opened.path != ScenePath)
                EditorSceneManager.CloseScene(opened, true);
        }

        return null;
    }

    private static GameObject BuildPhantom(GameObject boss, string name, string spritePath, string fallbackPrefabPath)
    {
        bool created;
        GameObject phantom = EnsureChild(boss, name, out created);

        var sr = EnsureComponent<SpriteRenderer>(phantom);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null && !string.IsNullOrEmpty(fallbackPrefabPath))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fallbackPrefabPath);
            if (prefab != null)
            {
                var source = prefab.GetComponentInChildren<SpriteRenderer>(true);
                if (source != null) sprite = source.sprite;
            }
        }

        if (sprite != null) sr.sprite = sprite;
        else Debug.LogWarning($"[FinalBossSceneBuilder] {name} 스프라이트를 찾지 못했습니다 ('{spritePath}'). 직접 넣어주세요.");

        sr.sortingOrder = 2;
        sr.color = new Color(1f, 1f, 1f, 0.5f);

        if (created)
        {
            phantom.transform.localPosition = Vector3.zero;
            NeutralizeParentScale(phantom.transform, Vector3.one);
        }

        phantom.SetActive(false);
        return phantom;
    }

    private static GameObject BuildSoilPhantomRig(GameObject boss)
    {
        var soilPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SoilBossPrefabPath);
        if (soilPrefab == null)
        {
            Debug.LogWarning($"[FinalBossSceneBuilder] '{SoilBossPrefabPath}' 를 찾지 못해 SoilPhantom 을 단일 스프라이트 방식으로 구성합니다.");
            GameObject fallback = BuildPhantom(boss, "SoilPhantom", SoilPhantomSpritePath, SoilBossPrefabPath);
            AttachPhantomAnimation(fallback, SoilIdleClipPath, SoilPattern1ClipPath);
            return fallback;
        }

        GameObject phantom = FindDirectChildNoCreate(boss, "SoilPhantom");
        bool isRig = phantom != null
            && phantom.GetComponent<Animation>() != null
            && phantom.transform.Find("Body") != null;

        if (phantom != null && !isRig)
        {
            Object.DestroyImmediate(phantom);
            phantom = null;
            report.Add("구 단일 스프라이트 SoilPhantom 삭제 (리그형으로 재생성)");
        }

        if (phantom == null)
        {
            phantom = Object.Instantiate(soilPrefab, boss.transform);
            phantom.name = "SoilPhantom";
            phantom.transform.localPosition = Vector3.zero;
            phantom.transform.localRotation = Quaternion.identity;
            NeutralizeParentScale(phantom.transform, soilPrefab.transform.localScale);
            report.Add("SoilPhantom = Soil 프리팹 비주얼 계층 복제 (본 리깅, 로직 제거)");
        }
        else
        {
            report.Add("SoilPhantom 리그형 유지 (알파/정렬/클립만 재확인)");
        }

        StripPhantomLogic(phantom);
        EnsureSoilPhantomAnimation(phantom);
        ApplyPhantomRigLook(phantom);

        phantom.SetActive(false);
        return phantom;
    }

    private static void StripPhantomLogic(GameObject phantom)
    {
        foreach (var mono in phantom.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mono is BossBase || mono is AnimationController || mono is Hitbox)
                Object.DestroyImmediate(mono);
        }

        foreach (var col in phantom.GetComponentsInChildren<Collider2D>(true))
            Object.DestroyImmediate(col);

        foreach (var rb in phantom.GetComponentsInChildren<Rigidbody2D>(true))
            Object.DestroyImmediate(rb);
    }

    private static void EnsureSoilPhantomAnimation(GameObject phantom)
    {
        var anim = phantom.GetComponentInChildren<Animation>(true);
        if (anim == null) anim = phantom.AddComponent<Animation>();

        var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SoilIdleClipPath);
        var patternClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SoilPattern1ClipPath);

        if (idleClip != null)
        {
            if (anim.GetClip(idleClip.name) == null) anim.AddClip(idleClip, idleClip.name);
            anim.clip = idleClip;
        }
        else
        {
            Debug.LogWarning($"[FinalBossSceneBuilder] SoilIdle 클립을 찾지 못했습니다 ('{SoilIdleClipPath}').");
        }

        if (patternClip != null)
        {
            if (anim.GetClip(patternClip.name) == null) anim.AddClip(patternClip, patternClip.name);
        }
        else
        {
            Debug.LogWarning($"[FinalBossSceneBuilder] SoilPattern1 클립을 찾지 못했습니다 ('{SoilPattern1ClipPath}').");
        }

        anim.playAutomatically = true;
        anim.wrapMode = WrapMode.Loop;
        report.Add("SoilPhantom 레거시 Animation 확인 (SoilIdle 자동재생·루프 + SoilPattern1 등록)");
    }

    private static void ApplyPhantomRigLook(GameObject phantom)
    {
        var renderers = phantom.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0) return;

        int minOrder = int.MaxValue;
        foreach (var sr in renderers)
            if (sr.sortingOrder < minOrder) minOrder = sr.sortingOrder;
        int shift = minOrder < 2 ? 2 - minOrder : 0;

        foreach (var sr in renderers)
        {
            Color color = sr.color;
            color.a = 0.5f;
            sr.color = color;
            sr.sortingOrder += shift;
        }
    }

    private static GameObject FindDirectChildNoCreate(GameObject parent, string name)
    {
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform child = parent.transform.GetChild(i);
            if (child.name == name) return child.gameObject;
        }
        return null;
    }

    private static void AttachPhantomAnimation(GameObject phantom, string idleClipPath, params string[] extraClipPaths)
    {
        if (phantom == null) return;

        var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(idleClipPath);
        if (idleClip == null)
        {
            Debug.LogWarning($"[FinalBossSceneBuilder] {phantom.name} 애니메이션 클립을 찾지 못했습니다 ('{idleClipPath}'). 환영이 정지 이미지로 표시됩니다.");
            return;
        }

        var extraClips = new List<AnimationClip>();
        foreach (var path in extraClipPaths)
        {
            var extra = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (extra != null) extraClips.Add(extra);
            else Debug.LogWarning($"[FinalBossSceneBuilder] {phantom.name} 추가 클립을 찾지 못했습니다 ('{path}').");
        }

        if (idleClip.legacy)
        {
            var staleAnimator = phantom.GetComponent<Animator>();
            if (staleAnimator != null) Object.DestroyImmediate(staleAnimator);

            var anim = EnsureComponent<Animation>(phantom);
            if (anim.GetClip(idleClip.name) == null) anim.AddClip(idleClip, idleClip.name);
            foreach (var extra in extraClips)
                if (extra.legacy && anim.GetClip(extra.name) == null) anim.AddClip(extra, extra.name);
            anim.clip = idleClip;
            anim.playAutomatically = true;
            anim.wrapMode = WrapMode.Loop;
            report.Add($"{phantom.name} 환영 애니메이션 연결 (레거시: {idleClip.name} + 추가 {extraClips.Count}개)");
        }
        else
        {
            var staleAnimation = phantom.GetComponent<Animation>();
            if (staleAnimation != null) Object.DestroyImmediate(staleAnimation);

            string controllerPath = $"{PhantomControllerFolder}/Phantom_{phantom.name}.controller";
            var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
            if (controller == null)
            {
                if (!AssetDatabase.IsValidFolder(PhantomControllerFolder))
                    AssetDatabase.CreateFolder("Assets/AddressableAssets", "Final");
                controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, idleClip);
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var extra in extraClips)
            {
                bool exists = false;
                foreach (var child in stateMachine.states)
                    if (child.state.name == extra.name) { exists = true; break; }
                if (!exists) controller.AddMotion(extra, 0);
            }

            var animator = EnsureComponent<Animator>(phantom);
            animator.runtimeAnimatorController = controller;
            report.Add($"{phantom.name} 환영 애니메이션 연결 (컨트롤러: {idleClip.name} + 추가 {extraClips.Count}개 상태)");
        }
    }

    private static GameObject BuildIaiDarkOverlay(GameObject boss, Sprite squareSprite)
    {
        bool created;
        GameObject overlay = EnsureChild(boss, "IaiDarkOverlay", out created);

        var sr = EnsureComponent<SpriteRenderer>(overlay);
        if (squareSprite != null) sr.sprite = squareSprite;
        sr.color = new Color(0f, 0f, 0f, 0f);
        sr.sortingOrder = 40;

        if (created)
        {
            overlay.transform.localPosition = Vector3.zero;
            NeutralizeParentScale(overlay.transform, new Vector3(60f, 40f, 1f));
        }

        overlay.SetActive(false);
        return overlay;
    }

    private static GameObject BuildIaiFlashEffect(GameObject boss, Sprite squareSprite)
    {
        bool created;
        GameObject flash = EnsureChild(boss, "IaiFlashEffect", out created);

        var sr = EnsureComponent<SpriteRenderer>(flash);
        if (squareSprite != null) sr.sprite = squareSprite;
        sr.color = new Color(1f, 0.96f, 0.78f, 0.95f);
        sr.sortingOrder = 41;

        if (created)
        {
            flash.transform.localPosition = Vector3.zero;
            flash.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
            NeutralizeParentScale(flash.transform, new Vector3(12f, 0.25f, 1f));
        }

        flash.SetActive(false);
        return flash;
    }

    private static void RemoveLegacyBossHealthBar(GameObject boss)
    {
        Transform legacy = boss.transform.Find("WorldCanvas");
        if (legacy == null) return;

        Object.DestroyImmediate(legacy.gameObject);
        report.Add("구 보스 체력바(WorldCanvas) 제거 — 체력바는 PlayerUI 의 BossHealth(BossHealthView)가 담당한다");
    }

    private static void BuildFinalBossRoom(Scene scene, GameObject boss)
    {
        bool created;
        GameObject roomGo = FindInScene(scene, "FinalBossRoom", out created);
        var room = EnsureComponent<FinalBossRoom>(roomGo);

        var so = new SerializedObject(room);
        SetObjectReference(so, "DM", Object.FindObjectOfType<DialogueManager>(true));
        SetObjectReference(so, "bossBehaviour", boss.GetComponent<FinalBoss>());
        so.ApplyModifiedPropertiesWithoutUndo();

        report.Add(created
            ? "FinalBossRoom 오브젝트 생성 + DM/보스 참조 배선 (대사 S10_FINAL_BOSS 등은 코드 기본값)"
            : "FinalBossRoom 참조 재배선");
    }

    private static void EnsureDialogueUI(Scene scene)
    {
        if (Object.FindObjectOfType<DialogueManager>(true) != null)
        {
            report.Add("DialogueUI: 이미 배치돼 있어 그대로 둠");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueUIPrefabPath);
        if (prefab == null)
        {
            DialogueUIPrefabBuilder.CreateDialogueUIPrefab();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueUIPrefabPath);
        }

        if (prefab == null)
        {
            Debug.LogError("[FinalBossSceneBuilder] DialogueUI 프리팹을 만들지 못했습니다.");
            return;
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance != null) instance.name = "DialogueUI";
        report.Add("DialogueUI 프리팹 인스턴스 배치");
    }

    private static void CleanupLegacyObjects(Scene scene)
    {
        foreach (var styxRoom in Object.FindObjectsOfType<StyxRoom>(true))
        {
            Object.DestroyImmediate(styxRoom);
            report.Add("StyxRoom 컴포넌트 제거 (GameManager)");
        }

        foreach (var trigger in Object.FindObjectsOfType<EndingTrigger>(true))
        {
            report.Add($"EndingTrigger 오브젝트 '{trigger.gameObject.name}' 제거");
            Object.DestroyImmediate(trigger.gameObject);
        }

        foreach (var icon in Object.FindObjectsOfType<InteractionView>(true))
        {
            report.Add($"InteractionIcon '{icon.gameObject.name}' 제거");
            Object.DestroyImmediate(icon.gameObject);
        }

        foreach (var manager in Object.FindObjectsOfType<InteractionManager>(true))
        {
            Object.DestroyImmediate(manager);
            report.Add("InteractionManager 컴포넌트 제거 (상호작용 잔재)");
        }
    }

    private static void EnsureSoilWavePrefab(bool rebuild)
    {
        if (rebuild || !File.Exists(SoilWavePrefabPath))
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SoilWaveSpritePathA);
            if (sprite == null) sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SoilWaveSpritePathB);
            if (sprite == null)
            {
                var soilDrop = AssetDatabase.LoadAssetAtPath<GameObject>(SoilDropPrefabPath);
                if (soilDrop != null)
                {
                    var dropSr = soilDrop.GetComponentInChildren<SpriteRenderer>(true);
                    if (dropSr != null) sprite = dropSr.sprite;
                }
            }
            if (sprite == null)
                Debug.LogWarning("[FinalBossSceneBuilder] SoilWave 스프라이트를 찾지 못했습니다. 프리팹에 직접 넣어주세요.");

            var root = new GameObject(SoilWaveAddress);
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;

            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
            col.offset = sprite != null ? (Vector2)sprite.bounds.center : Vector2.zero;

            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
            rb.useFullKinematicContacts = true;
            rb.gravityScale = 0f;

            root.AddComponent<SoilWave>();
            WireSoilWaveHitbox(root.AddComponent<Hitbox>());

            EnsureFolder(Path.GetDirectoryName(SoilWavePrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, SoilWavePrefabPath, out bool success);
            Object.DestroyImmediate(root);

            if (!success)
            {
                Debug.LogError($"[FinalBossSceneBuilder] '{SoilWavePrefabPath}' 저장에 실패했습니다.");
                return;
            }

            report.Add($"SoilWave 프리팹 생성: {SoilWavePrefabPath} (SpriteRenderer + Trigger BoxCollider2D + Kinematic Rigidbody2D + SoilWave + Hitbox)");
        }
        else
        {
            EnsureSoilWaveHitbox();
            report.Add("SoilWave 프리팹: 이미 있어 유지 (Hitbox 배선만 재확인)");
        }

        RegisterSoilWaveAddressable();
    }

    private static void EnsureSoilWaveHitbox()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SoilWavePrefabPath);
        try
        {
            WireSoilWaveHitbox(EnsureComponent<Hitbox>(root));
            PrefabUtility.SaveAsPrefabAsset(root, SoilWavePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void WireSoilWaveHitbox(Hitbox hitbox)
    {
        var so = new SerializedObject(hitbox);
        SetInt(so, "damage", 15);
        SetFloat(so, "knockbackForce", 1f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RegisterSoilWaveAddressable()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[FinalBossSceneBuilder] Addressables 설정을 찾지 못해 SoilWave 를 등록하지 못했습니다. " +
                           "Window > Asset Management > Addressables 에서 수동 등록이 필요합니다 (라벨 Pool).");
            return;
        }

        string guid = AssetDatabase.AssetPathToGUID(SoilWavePrefabPath);
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogError("[FinalBossSceneBuilder] SoilWave 프리팹 GUID 를 찾지 못했습니다.");
            return;
        }

        var group = settings.FindGroup(AddressableGroupName);
        if (group == null)
        {
            var goldGroup = settings.FindGroup(SourceGroupName);
            group = goldGroup != null
                ? settings.CreateGroup(AddressableGroupName, false, false, false, goldGroup.Schemas)
                : settings.CreateGroup(AddressableGroupName, false, false, false, null,
                    typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema),
                    typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema));
            report.Add($"Addressables 그룹 '{AddressableGroupName}' 생성" +
                       (goldGroup != null ? " (Gold 그룹 스키마 복사)" : ""));
        }

        settings.AddLabel(PoolLabel, false);
        settings.AddLabel(BossAssetLabel, false);

        var entry = settings.CreateOrMoveEntry(guid, group, false, false);
        entry.address = SoilWaveAddress;
        entry.SetLabel(PoolLabel, true, true, false);
        entry.SetLabel(BossAssetLabel, true, true, false);
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
        EditorUtility.SetDirty(settings);

        report.Add($"Addressables 등록: 주소 '{SoilWaveAddress}', 라벨 '{PoolLabel}'/'{BossAssetLabel}' " +
                   "(PoolManager 는 라벨 Pool 을 프리로드, FinalBoss 풀 키 = 프리팹 이름 'SoilWave')");
    }

    private static int ResolveGroundMask(Scene scene)
    {
        GameObject square = FindInSceneNoCreate(scene, "Square");
        if (square != null) return 1 << square.layer;

        int ground = LayerMask.NameToLayer("ground");
        return ground >= 0 ? 1 << ground : 1 << 6;
    }

    private static Camera FindMainCamera(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var cam = root.GetComponentInChildren<Camera>(true);
            if (cam != null) return cam;
        }
        return null;
    }

    private static void BuildShallowWater(Scene scene)
    {
        GameObject square = FindInSceneNoCreate(scene, "Square");
        var groundCol = square != null ? square.GetComponent<BoxCollider2D>() : null;

        float surfaceY = -3f;
        float width = 20f;
        float centerX = 0f;
        if (square != null && groundCol != null)
        {
            surfaceY = square.transform.position.y
                + (groundCol.offset.y + groundCol.size.y * 0.5f) * Mathf.Abs(square.transform.lossyScale.y);
            width = groundCol.size.x * Mathf.Abs(square.transform.lossyScale.x);
            centerX = square.transform.position.x + groundCol.offset.x * square.transform.lossyScale.x;
        }

        bool created;
        GameObject root = FindInScene(scene, "ShallowWater", out created);
        root.transform.position = new Vector3(centerX, surfaceY, 0f);
        root.transform.localScale = Vector3.one;

        var rb = EnsureComponent<Rigidbody2D>(root);
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        var zoneCol = EnsureComponent<BoxCollider2D>(root);
        zoneCol.isTrigger = true;
        zoneCol.size = new Vector2(width, 0.9f);
        zoneCol.offset = new Vector2(0f, 0.15f);

        var zone = EnsureComponent<ShallowWaterZone>(root);
        var zoneSo = new SerializedObject(zone);
        var surfaceProp = zoneSo.FindProperty("surfaceLocalY");
        if (surfaceProp != null)
        {
            surfaceProp.floatValue = WaterSurfaceLocalY;
            zoneSo.ApplyModifiedPropertiesWithoutUndo();
        }

        RemoveLegacyWaterVisuals(root);
        BuildWaterSim(root, width);

        report.Add(created
            ? "얕은 물(ShallowWater) 생성 — 감속 존 + 물리 기반 수면 시뮬(SimWater) (삼도천)"
            : "얕은 물(ShallowWater) 갱신 — 임시 물 레이어를 물리 기반 수면 시뮬(SimWater)로 교체");
    }

    private static void RemoveLegacyWaterVisuals(GameObject root)
    {
        var ocean = root.GetComponent<Ocean_animation>();
        if (ocean != null) Object.DestroyImmediate(ocean);

        foreach (string legacyName in new[] { "WaterBack", "WaterFront" })
        {
            Transform legacy = root.transform.Find(legacyName);
            if (legacy != null) Object.DestroyImmediate(legacy.gameObject);
        }
    }

    private static void BuildWaterSim(GameObject root, float width)
    {
        bool created;
        GameObject sim = EnsureChild(root, "WaterSim", out created);
        sim.transform.localPosition = new Vector3(0f, WaterSurfaceLocalY, 0f);
        sim.transform.localScale = Vector3.one;

        var rb = EnsureComponent<Rigidbody2D>(sim);
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.useFullKinematicContacts = true;

        var poly = EnsureComponent<PolygonCollider2D>(sim);
        poly.isTrigger = true;

        var controller = EnsureComponent<SpriteShapeController>(sim);
        controller.autoUpdateCollider = false;
        var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteShape>(SpriteShapeProfilePath);
        if (profile != null) controller.spriteShape = profile;
        else report.Add($"경고: 기본 SpriteShape 프로파일({SpriteShapeProfilePath})을 찾지 못했습니다.");

        var renderer = sim.GetComponent<SpriteShapeRenderer>();
        var material = AssetDatabase.LoadAssetAtPath<Material>(SpriteLitDefaultMaterialPath);
        if (renderer != null && material != null)
            renderer.sharedMaterials = new[] { material, material };
        if (renderer != null)
        {
            renderer.color = new Color(0.22f, 0.30f, 0.42f, 0.45f);
            renderer.sortingOrder = 30;
        }

        var water = EnsureComponent<SimWater.Water>(sim);
        var waterSo = new SerializedObject(water);
        waterSo.FindProperty("size").vector2Value = new Vector2(width + 0.6f, 0.9f);
        waterSo.FindProperty("useSurface").boolValue = true;
        waterSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(water);
    }

    private static Sprite LoadSpriteByGuid(string guid)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) return null;
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void NeutralizeParentScale(Transform child, Vector3 desiredLocalScale)
    {
        Vector3 parentScale = child.parent != null ? child.parent.localScale : Vector3.one;
        child.localScale = new Vector3(
            SafeDivide(desiredLocalScale.x, parentScale.x),
            SafeDivide(desiredLocalScale.y, parentScale.y),
            desiredLocalScale.z);
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Approximately(divisor, 0f) ? value : value / divisor;
    }

    private static GameObject FindInScene(Scene scene, string name, out bool created)
    {
        GameObject found = FindInSceneNoCreate(scene, name);
        if (found != null)
        {
            created = false;
            return found;
        }

        created = true;
        return new GameObject(name);
    }

    private static GameObject FindInSceneNoCreate(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
            var child = FindChildRecursive(root.transform, name);
            if (child != null) return child.gameObject;
        }
        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name) return child;
            Transform nested = FindChildRecursive(child, name);
            if (nested != null) return nested;
        }
        return null;
    }

    private static GameObject EnsureChild(GameObject parent, string name, out bool created)
    {
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform child = parent.transform.GetChild(i);
            if (child.name == name)
            {
                created = false;
                return child.gameObject;
            }
        }

        created = true;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        if (component == null) component = go.AddComponent<T>();
        return component;
    }

    private static RectTransform EnsureRectTransform(GameObject go)
    {
        var rect = go.GetComponent<RectTransform>();
        if (rect == null) rect = go.AddComponent<RectTransform>();
        return rect;
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder)) return;
        folder = folder.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(folder)) return;

        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
    }

    private static string Fmt(Vector2 v)
    {
        return $"({v.x:0.##} x {v.y:0.##})";
    }

    private static void SetObjectReference(SerializedObject so, string path, Object value)
    {
        var prop = so.FindProperty(path);
        if (prop == null)
        {
            Debug.LogWarning($"[FinalBossSceneBuilder] '{path}' 필드를 찾지 못했습니다.");
            return;
        }
        prop.objectReferenceValue = value;
    }

    private static void SetFloat(SerializedObject so, string path, float value)
    {
        var prop = so.FindProperty(path);
        if (prop != null) prop.floatValue = value;
    }

    private static void SetInt(SerializedObject so, string path, int value)
    {
        var prop = so.FindProperty(path);
        if (prop != null) prop.intValue = value;
    }

    private static void SetBool(SerializedObject so, string path, bool value)
    {
        var prop = so.FindProperty(path);
        if (prop != null) prop.boolValue = value;
    }

    private static void LogSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[FinalBossSceneBuilder] Styx 최종보스 씬 구성 완료.");
        sb.AppendLine("── 수행 내역 ──");
        foreach (var line in report) sb.AppendLine(" · " + line);

        todo.Add("FinalBoss 위치/스케일을 방 크기에 맞게 조정 (기본: x=5, 2배 스케일)");
        todo.Add("보스 SpriteRenderer 틴트 색을 취향대로 조정 (기본: 어두운 보라)");
        todo.Add("spriteFacesRight 확인 — 보스가 플레이어를 등지고 보면 인스펙터에서 꺼주세요");
        todo.Add("환영(Soil/Water/FirePhantom) 위치·크기, IaiDarkOverlay 크기(화면 전체 덮는지), IaiFlashEffect 모양 확인");
        todo.Add("보스 체력바는 PlayerUI > BossHealth(BossHealthView) 담당 — scalePerHp 로 길이 조정");
        todo.Add("Addressables 그룹에 SoilWave 가 등록됐는지 확인 후 플레이 테스트 (에디터 Play Mode Script 가 Use Asset Database 면 별도 빌드 불필요)");

        sb.AppendLine("── 유저가 조정할 것 ──");
        foreach (var line in todo) sb.AppendLine(" · " + line);

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog(
            "최종보스 씬 구성 완료",
            "Styx 씬에 최종보스 세팅을 완료했습니다.\n\n" +
            "· FinalBoss (스프라이트/애니메이터/히트박스 4종/환영 3종/거합 연출/체력바)\n" +
            "· FinalBossRoom 상태머신 배선\n" +
            "· StyxRoom / EndingTrigger / InteractionIcon 정리\n" +
            "· SoilWave 프리팹 + Addressables(Pool) 등록\n\n" +
            "자세한 내역과 조정 목록은 콘솔 로그를 확인하세요.",
            "확인");
    }
}

/* [파일 노트 — 최종보스(Styx) 씬/프리팹 빌더]
 *
 * 실행: Tools / Tup3 / Setup Final Boss Scene (Styx)  — 몇 번을 다시 실행해도 중복 생성 없이 갱신(멱등).
 * 보조: Tools / Tup3 / Create SoilWave Prefab         — SoilWave 프리팹만 다시 만들 때.
 *
 * 보스 구성 대상 — 프리팹 우선:
 *   Assets/Prefabs/FinalBoss.prefab 이 있으면 PrefabUtility.LoadPrefabContents 로 프리팹 에셋을
 *   직접 열어 ConfigureBoss(스프라이트/애니메이터/히트박스 4종/환영 3종/거합 연출/체력바/필드 배선)를
 *   적용하고 SaveAsPrefabAsset → UnloadPrefabContents 한다. 씬 인스턴스가 아니라 에셋이 원본.
 *   프리팹이 없으면(하위 호환) 예전처럼 씬에 직접 구성하고 "프리팹화 권장" 경고를 남긴다.
 *
 * 씬 전용 처리 (프리팹 모드에서도 씬 쪽에서 수행):
 *   - 씬에 FinalBoss 가 없으면 프리팹 인스턴스를 배치(BossSpawnPosition). 이름은 같지만 프리팹과
 *     연결되지 않은 구 오브젝트가 있으면 위치를 승계해 프리팹 인스턴스로 교체한다.
 *   - 인스턴스 오버라이드: groundMask(씬 Square 레이어 — 씬 의존이라 프리팹에 넣지 않음),
 *     SnapBossAboveGround(바닥 위 스냅 — 새 배치 또는 파묻힘 시).
 *   - FinalBossRoom 배선, StyxRoom/EndingTrigger/InteractionIcon 정리, DialogueUI 배치는 기존과 동일.
 *
 * ConfigureBoss 가 하는 일 (프리팹 루트든 씬 오브젝트든 동일):
 *   - SpriteRenderer: Player.prefab 스프라이트/머티리얼 + 리컬러 틴트(흰색일 때만 덮어써 유저값 보존).
 *   - Animator: Player.prefab 의 RuntimeAnimatorController 공유.
 *   - BoxCollider2D 4개 전부 루트에 부착([0]=기본 활성, [1]=토, [2]=수, [3]=화 비활성).
 *     토=Soil.prefab 몸통, 수=Water_eye_1.prefab(원→박스), 화=Boss_Fire.unity Additive 로 읽기.
 *   - FinalBoss 배선: maxHp 300, boxColliders[0]=기본, spriteFacesRight=true. groundMask 는 제외(씬 몫).
 *   - 환영: 수(eye3_1)/화(boss_fire)는 단일 SpriteRenderer + AttachPhantomAnimation(기존 방식).
 *     토는 BuildSoilPhantomRig — 아래 참조.
 *   - 거합 연출(IaiDarkOverlay/IaiFlashEffect).
 *   - 구 체력바 제거(RemoveLegacyBossHealthBar): 예전에는 보스 자식으로 WorldCanvas > Health(HealthView)
 *     를 만들었으나, 보스 체력바가 PlayerUI 의 BossHealth(BossHealthView)로 옮겨져 폐기됐다.
 *     빌더를 다시 돌리면 남아 있는 구 캔버스를 지운다(2026-08-29 유저 확정).
 *
 * SoilPhantom = Soil 프리팹 비주얼 복제 (본 리깅):
 *   토보스 애니메이션(SoilIdle/SoilPattern1, 레거시)은 Body/LArm 등 자식 트랜스폼 경로에 커브가
 *   바인딩된 본 리깅이라 단일 SpriteRenderer 로는 재생 불가. 그래서 Soil.prefab 을 Object.Instantiate
 *   (프리팹 링크 없는 완전 복제)로 보스 자식 "SoilPhantom" 으로 넣고 로직만 제거한다 —
 *   StripPhantomLogic: BossBase(Soil)/AnimationController/Hitbox 스크립트 → Collider2D → Rigidbody2D 순
 *   제거(RequireComponent 역순). 트랜스폼 계층/SpriteRenderer들/레거시 Animation 컴포넌트는 유지.
 *   EnsureSoilPhantomAnimation: SoilIdle 자동재생·루프 + SoilPattern1 클립 등록 보장.
 *   ApplyPhantomRigLook: 전 SpriteRenderer 알파 0.5 + sortingOrder 를 상대 순서 유지한 채
 *   최소값이 2가 되도록 일괄 시프트(보스 본체 order 1 앞에 일관 표시. 이미 2 이상이면 no-op).
 *   멱등 판정: 기존 SoilPhantom 에 Animation 컴포넌트와 Body 자식이 있으면 리그형으로 보고 유지,
 *   아니면(구 단일 SR 버전) 삭제 후 재생성. Soil.prefab 이 없으면 구 단일 SR 방식으로 폴백.
 *   원본 피봇=발이므로 FinalBoss.ComputePhantomAlignY 의 리그 분기가 루트 피봇을 콜라이더 바닥에 정렬.
 *
 * SoilWave 프리팹: SpriteRenderer(토 이펙트, 폴백 SoilDrop) + Trigger BoxCollider2D +
 *   Kinematic Rigidbody2D(simulated, useFullKinematicContacts) + SoilWave(이동/수명 전담) +
 *   Hitbox(damage 15, knockbackForce 1 — 데미지 전담, 같은 트리거 콜라이더 공유).
 *   기존 프리팹이 있어도 EnsureSoilWaveHitbox 가 LoadPrefabContents 로 Hitbox 존재·수치를 재보장(멱등).
 *   Assets/AddressableAssets/Final/SoilWave.prefab 저장 후 Addressables 자동 등록 —
 *   그룹 "Final"(없으면 Gold 스키마 복사), 주소 "SoilWave", 라벨 Pool/BossAsset.
 *   PoolManager 는 라벨 "Pool" 프리로드 + 프리팹 "이름"이 키이므로 이름은 "SoilWave" 여야 한다.
 *
 * 얕은 물(BuildShallowWater) — 물리 기반 수면 시뮬(SimWater, Tavern_Gamejam_CAU_SSU 이식) 통합:
 *   - ShallowWater 루트(감속 존 ShallowWaterZone + Kinematic RB + 트리거 박스)는 유지하되
 *     구 임시 물(WaterBack/WaterFront 쿼드 + Ocean_animation)은 발견 시 제거(교체).
 *   - 자식 "WaterSim": Kinematic Rigidbody2D(useFullKinematicContacts — RB 없는 플레이어/보스의
 *     정적 콜라이더와도 트리거 접촉 생성) + PolygonCollider2D(trigger) + SpriteShapeController
 *     (기본 프로파일, Sprite-Lit-Default 머티리얼 2슬롯, color 반투명 남색, order 30) + SimWater.Water.
 *   - 수면 = 바닥 Square 실측 상단 + WaterSurfaceLocalY(0.5, 발목 높이), 폭 = 바닥 실측 + 0.6,
 *     깊이 0.9. ShallowWaterZone.surfaceLocalY 도 0.5 로 동기화(물튀김 파티클 높이).
 *   - 출렁임 입력: ShallowWaterZone 이 Playermovement/FinalBoss 에 SimWater.WaterBody 를
 *     런타임 AddComponent (씬/프리팹 무수정). WaterSettings 는 Assets/Resources/WaterSettings.asset.
 *
 * 멱등성 규칙: 오브젝트는 이름으로 찾고 없을 때만 생성, 위치/스케일/틴트 등 "유저가 만질 값"은
 * 생성 시에만 초기화한다. 참조 배선(SerializedObject)은 매번 다시 적용한다.
 * 어검(FlyingSword)/거합 참격(Pattern4Slash)은 이미 Gold 그룹에 라벨 Pool 로 등록돼 있어 손대지 않는다.
 */

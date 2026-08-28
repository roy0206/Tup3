using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public static class SpriteFlashShaders
{
    public const string LitShaderName = "Tup3/2D/Sprite Flash Lit";
    public const string UnlitShaderName = "Tup3/2D/Sprite Flash Unlit";

    public const string LitMaterialAssetPath = "Assets/Resources/SpriteFlash/SpriteFlashLit.mat";
    public const string UnlitMaterialAssetPath = "Assets/Resources/SpriteFlash/SpriteFlashUnlit.mat";

    public const string LitMaterialResourcePath = "SpriteFlash/SpriteFlashLit";
    public const string UnlitMaterialResourcePath = "SpriteFlash/SpriteFlashUnlit";

    public const string SpriteLitDefaultShaderName = "Universal Render Pipeline/2D/Sprite-Lit-Default";
    public const string SpriteUnlitDefaultShaderName = "Universal Render Pipeline/2D/Sprite-Unlit-Default";
    public const string SpritesDefaultShaderName = "Sprites/Default";

    public static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    public static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

    private static Material litMaterial;
    private static Material unlitMaterial;
    private static bool litLoaded;
    private static bool unlitLoaded;
    private static bool warnedSetupMissing;

    public static Material LitMaterial
    {
        get
        {
            if (!litLoaded)
            {
                litLoaded = true;
                litMaterial = Resources.Load<Material>(LitMaterialResourcePath);
            }
            return litMaterial;
        }
    }

    public static Material UnlitMaterial
    {
        get
        {
            if (!unlitLoaded)
            {
                unlitLoaded = true;
                unlitMaterial = Resources.Load<Material>(UnlitMaterialResourcePath);
            }
            return unlitMaterial;
        }
    }

    public static bool SupportsFlash(Material material)
    {
        return material != null && material.HasProperty(FlashAmountId);
    }

    public static Material ResolveReplacement(Material current)
    {
        if (SupportsFlash(current)) return null;

        if (current == null || current.shader == null) return LitMaterial;

        string shaderName = current.shader.name;
        if (shaderName == SpriteLitDefaultShaderName) return LitMaterial;
        if (shaderName == SpriteUnlitDefaultShaderName) return UnlitMaterial;
        if (shaderName == SpritesDefaultShaderName) return UnlitMaterial;

        return null;
    }

    public static void WarnSetupMissingOnce(UnityEngine.Object context)
    {
        if (warnedSetupMissing) return;
        warnedSetupMissing = true;
        Debug.LogWarning(
            "[SpriteFlash] 플래시 머티리얼을 찾지 못했습니다. " +
            "메뉴 Tools/Tup3/Setup Sprite Flash 를 한 번 실행해 주세요.", context);
    }

    public static void ResetCache()
    {
        litLoaded = false;
        unlitLoaded = false;
        litMaterial = null;
        unlitMaterial = null;
        warnedSetupMissing = false;
    }
}

public sealed class SpriteFlashRuntime
{
    private static MaterialPropertyBlock block;

    private static readonly SpriteRenderer[] Empty = new SpriteRenderer[0];

    private readonly MonoBehaviour owner;
    private SpriteRenderer[] renderers = Empty;
    private Color color = Color.white;
    private float amount;
    private Tween tween;

    public SpriteFlashRuntime(MonoBehaviour owner)
    {
        this.owner = owner;
    }

    public int RendererCount => renderers.Length;

    public float Amount => amount;

    public Color FlashColor
    {
        get => color;
        set
        {
            color = value;
            Apply();
        }
    }

    public Tween CurrentTween => tween;

    public bool IsFlashing => tween != null && tween.IsActive() && tween.IsPlaying();

    public void SetRenderers(List<SpriteRenderer> collected)
    {
        KillTween();

        if (collected == null || collected.Count == 0)
        {
            renderers = Empty;
            return;
        }

        renderers = collected.ToArray();

        if (!Application.isPlaying) return;

        bool missingSetup = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer target = renderers[i];
            if (target == null) continue;

            Material current = target.sharedMaterial;
            Material replacement = SpriteFlashShaders.ResolveReplacement(current);
            if (replacement != null)
            {
                target.sharedMaterial = replacement;
                continue;
            }

            if (!SpriteFlashShaders.SupportsFlash(target.sharedMaterial)) missingSetup = true;
        }

        if (missingSetup) SpriteFlashShaders.WarnSetupMissingOnce(owner);

        amount = 0f;
        Apply();
    }

    public Tween Flash(Color flashColor, float duration, float peak)
    {
        KillTween();

        color = flashColor;
        amount = Mathf.Clamp01(peak);
        Apply();

        tween = DOTween.To(GetAmount, SetAmountInternal, 0f, SafeDuration(duration))
            .SetEase(Ease.Linear);
        Bind(tween);
        return tween;
    }

    public Tween FlashTo(Color flashColor, float target, float duration)
    {
        KillTween();

        color = flashColor;
        Apply();

        tween = DOTween.To(GetAmount, SetAmountInternal, Mathf.Clamp01(target), SafeDuration(duration))
            .SetEase(Ease.Linear);
        Bind(tween);
        return tween;
    }

    public void SetInstant(Color flashColor, float target)
    {
        KillTween();
        color = flashColor;
        SetAmountInternal(target);
    }

    public void SetInstant(float target)
    {
        KillTween();
        SetAmountInternal(target);
    }

    public void Stop()
    {
        KillTween();
    }

    public void Clear()
    {
        KillTween();
        SetAmountInternal(0f);
    }

    public void KillTween()
    {
        if (tween != null && tween.IsActive()) tween.Kill();
        tween = null;
    }

    public void Apply()
    {
        if (renderers.Length == 0) return;
        if (block == null) block = new MaterialPropertyBlock();

        block.Clear();
        block.SetColor(SpriteFlashShaders.FlashColorId, color);
        block.SetFloat(SpriteFlashShaders.FlashAmountId, amount);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer target = renderers[i];
            if (target == null) continue;
            target.SetPropertyBlock(block);
        }
    }

    private void Bind(Tween created)
    {
        if (created == null || owner == null) return;
        created.SetTarget(owner);
        created.SetLink(owner.gameObject);
    }

    private float GetAmount()
    {
        return amount;
    }

    private void SetAmountInternal(float value)
    {
        amount = Mathf.Clamp01(value);
        Apply();
    }

    private static float SafeDuration(float duration)
    {
        return Mathf.Max(0.0001f, duration);
    }
}

public static class SpriteFlashCollector
{
    private static readonly List<SpriteRenderer> Buffer = new List<SpriteRenderer>();

    public static List<SpriteRenderer> Collect(GameObject root, bool includeChildren, bool includeInactive, Func<SpriteRenderer, bool> filter)
    {
        Buffer.Clear();
        if (root == null) return Buffer;

        if (includeChildren)
        {
            root.GetComponentsInChildren(includeInactive, Buffer);
        }
        else
        {
            SpriteRenderer self = root.GetComponent<SpriteRenderer>();
            if (self != null) Buffer.Add(self);
        }

        if (filter != null)
        {
            for (int i = Buffer.Count - 1; i >= 0; i--)
            {
                if (!filter(Buffer[i])) Buffer.RemoveAt(i);
            }
        }
        else
        {
            for (int i = Buffer.Count - 1; i >= 0; i--)
            {
                if (Buffer[i] == null) Buffer.RemoveAt(i);
            }
        }

        return Buffer;
    }
}

/* [파일 노트]
 * 스프라이트 흰색 플래시(피격 점광) 시스템의 공용 뼈대다. 컴포넌트는 SpriteFlash / SpriteFlashGroup 이고
 * 이 파일은 그 둘이 함께 쓰는 (1) 머티리얼 해석, (2) MaterialPropertyBlock 적용 + DOTween 구동,
 * (3) 렌더러 수집 을 담는다.
 *
 * 왜 SpriteRenderer.color 가 아니라 셰이더인가
 *   SpriteRenderer.color 는 텍스처에 곱해지는 틴트다. 흰색(1,1,1,1)을 곱하면 아무것도 변하지 않으므로
 *   "흰색으로 물들이기"는 원리상 불가능하다(어둡게만 만들 수 있다). 그래서 셰이더에서 최종색을
 *   lerp(원본, _FlashColor, _FlashAmount) 로 덮는 방식으로 바꿨다.
 *
 * SpriteFlashShaders
 *   런타임에 쓸 플래시 머티리얼을 Resources 에서 1회만 로드해 캐시한다.
 *   ResolveReplacement 는 "이 머티리얼을 무엇으로 갈아끼워야 하나"를 답한다.
 *     - 이미 _FlashAmount 를 가진 머티리얼  -> null (그대로 둔다)
 *     - Sprite-Lit-Default                 -> SpriteFlashLit  (프로젝트의 모든 스프라이트가 이 경우다)
 *     - Sprite-Unlit-Default / Sprites-Default -> SpriteFlashUnlit
 *     - 그 외 커스텀 머티리얼               -> null (건드리지 않는다. 남의 연출을 깨지 않기 위해)
 *   라이팅 모델을 유지해야 겉보기가 안 변하므로 Lit 은 Lit 으로, Unlit 은 Unlit 으로만 대응시킨다.
 *
 * 머티리얼 교체는 sharedMaterial 대입이다
 *   renderer.material 을 읽으면 Unity 가 머티리얼 인스턴스를 복제해 배칭이 깨지고 메모리가 샌다.
 *   여기서는 Resources 의 공유 에셋 하나를 그대로 sharedMaterial 에 꽂기만 하므로 인스턴스가 생기지 않는다.
 *   에셋 자체를 고치는 게 아니라 렌더러의 참조만 바꾸는 것이라 다른 오브젝트에 영향도 없다.
 *   _FlashAmount 가 0 일 때 셰이더 코드 경로는 원본 URP 셰이더와 완전히 동일하므로 겉보기 변화가 없다.
 *   에디트 모드에서는 씬이 더러워지므로 Application.isPlaying 일 때만 교체한다.
 *
 * MaterialPropertyBlock
 *   색/강도는 머티리얼이 아니라 렌더러별 MPB 로 넣는다. 그래서 여러 보스가 같은 공유 머티리얼을 쓰면서도
 *   서로 다른 타이밍에 따로 깜빡일 수 있다. static 블록 하나를 재사용하며 매번 Clear 후 두 프로퍼티만 채운다.
 *   GetPropertyBlock 으로 기존 블록을 읽어오지 않는 것은 의도적이다. SpriteRenderer 에 대해
 *   Get -> Set 을 하면 스프라이트 아틀라스/SpriteSkin 환경에서 내부 텍스처 바인딩이 어긋나는 알려진 함정이 있다.
 *   MPB 는 담긴 프로퍼티만 덮어쓰므로 _FlashColor/_FlashAmount 만 넣어도 스프라이트는 정상 렌더된다.
 *   (다만 다른 시스템이 같은 렌더러에 MPB 를 걸어 두었다면 그것은 덮인다. 현재 프로젝트엔 그런 곳이 없다.)
 *
 * DOTween
 *   DOTween.To 로 amount 를 0 까지 되돌린다. 프로젝트 관례대로 트윈이므로 PauseManager 의
 *   DOTween.PauseAll() 에 자동으로 함께 멈춘다. SetTarget(owner) 로 DOTween.Kill(owner) 이 통하고,
 *   SetLink(owner.gameObject) 로 오브젝트가 파괴되면 트윈도 함께 죽는다.
 *   연속 호출 시 KillTween 으로 이전 트윈을 먼저 죽이고 다시 peak 부터 시작하므로 흰색으로 굳지 않는다.
 *   duration 은 0 이하가 들어와도 0.0001 로 클램프한다. 항상 실제 Tween 을 반환해야
 *   호출부에서 .SetEase() / .OnComplete() 를 붙였을 때 NullReference 가 나지 않는다.
 *
 * SpriteFlashCollector
 *   수집 버퍼는 static 이라 프레임마다 GC 를 만들지 않는다. 반환한 List 는 곧바로
 *   SpriteFlashRuntime.SetRenderers 가 배열로 복사하므로 호출자가 오래 붙들면 안 된다.
 */

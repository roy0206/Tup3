using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDeathCutscene : DefeatCutscene
{
    [Header("카메라 포커스")]
    [SerializeField] private float focusDuration = 1.2f;
    [SerializeField] private Ease focusEase = Ease.InOutSine;
    [SerializeField] private float focusZoomSize = 3.5f;
    [SerializeField] private Vector2 focusOffset = new Vector2(0f, 0.4f);
    [SerializeField] private Behaviour[] disableDuringCutscene;

    [Header("비네트")]
    [SerializeField] private float vignetteDelay = 0.2f;
    [SerializeField] private float vignetteDuration = 1.1f;
    [SerializeField] private float vignetteInnerRadius = 0.09f;
    [SerializeField] private float vignetteOuterRadius = 0.3f;
    [SerializeField] private Color vignetteColor = Color.black;
    [SerializeField] private int vignetteSortingOrder = 4000;
    [SerializeField] private int vignetteTextureSize = 512;

    [Header("혼백 파티클")]
    [SerializeField] private float particleDelay = 0.7f;
    [SerializeField] private float particleRampDuration = 2.2f;
    [SerializeField] private float particleRateStart = 3f;
    [SerializeField] private float particleRateEnd = 70f;
    [SerializeField] private Color particleColor = Color.white;
    [SerializeField] private Vector2 particleRiseSpeed = new Vector2(0.8f, 2.2f);
    [SerializeField] private Vector2 particleLifetime = new Vector2(0.8f, 1.8f);
    [SerializeField] private Vector2 particleSize = new Vector2(0.04f, 0.12f);
    [SerializeField] private Vector2 particleSpread = new Vector2(0.55f, 0.9f);
    [SerializeField] private int particleSortingOrder = 3000;

    [Header("플레이어 소멸")]
    [SerializeField] private float playerFadeDelay = 1.3f;
    [SerializeField] private float playerFadeDuration = 2.1f;

    [Header("마무리")]
    [SerializeField] private float holdAfter = 0.6f;

    private Sequence sequence;
    private Canvas vignetteCanvas;
    private Image vignetteImage;
    private Texture2D vignetteTexture;
    private ParticleSystem soulParticles;

    private readonly List<Behaviour> disabled = new();

    public override void Play(Action onComplete)
    {
        Transform target = ResolvePlayer();

        DisableInterferingBehaviours();
        BuildVignette();
        BuildSoulParticles(target);

        sequence = DOTween.Sequence();

        AppendCameraFocus(target);
        AppendVignette();
        AppendSoulParticles();
        AppendPlayerFade(target);

        sequence.AppendInterval(Mathf.Max(0f, holdAfter));
        sequence.SetTarget(this).OnComplete(() => onComplete?.Invoke());
    }

    public override void Stop()
    {
        if (sequence == null) return;

        var seq = sequence;
        sequence = null;
        seq.Kill();
    }

    private Transform ResolvePlayer()
    {
        var movement = FindObjectOfType<Playermovement>();
        return movement != null ? movement.transform : null;
    }

    private void DisableInterferingBehaviours()
    {
        disabled.Clear();
        if (disableDuringCutscene == null) return;

        for (int i = 0; i < disableDuringCutscene.Length; i++)
        {
            var behaviour = disableDuringCutscene[i];
            if (behaviour == null || !behaviour.enabled) continue;

            behaviour.enabled = false;
            disabled.Add(behaviour);
        }
    }

    private void AppendCameraFocus(Transform target)
    {
        var cam = Camera.main;
        if (cam == null || target == null) return;

        float duration = Mathf.Max(0.01f, focusDuration);

        Vector3 destination = target.position + (Vector3)focusOffset;
        destination.z = cam.transform.position.z;
        sequence.Insert(0f, cam.transform.DOMove(destination, duration).SetEase(focusEase));

        if (cam.orthographic && focusZoomSize > 0f)
        {
            sequence.Insert(0f, DOTween.To(
                () => cam.orthographicSize,
                v => cam.orthographicSize = v,
                focusZoomSize,
                duration).SetEase(focusEase));
        }
    }

    private void AppendVignette()
    {
        if (vignetteImage == null) return;

        sequence.Insert(
            Mathf.Max(0f, vignetteDelay),
            vignetteImage.DOFade(1f, Mathf.Max(0.01f, vignetteDuration)).SetEase(Ease.InOutSine));
    }

    private void AppendSoulParticles()
    {
        if (soulParticles == null) return;

        var emission = soulParticles.emission;
        var rate = emission.rateOverTime;
        rate.constant = particleRateStart;
        emission.rateOverTime = rate;

        soulParticles.Play();

        sequence.Insert(Mathf.Max(0f, particleDelay), DOTween.To(
            () => emission.rateOverTime.constant,
            v =>
            {
                var current = emission.rateOverTime;
                current.constant = v;
                emission.rateOverTime = current;
            },
            particleRateEnd,
            Mathf.Max(0.01f, particleRampDuration)).SetEase(Ease.InQuad));
    }

    private void AppendPlayerFade(Transform target)
    {
        if (target == null) return;

        var renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        float duration = Mathf.Max(0.01f, playerFadeDuration);
        float delay = Mathf.Max(0f, playerFadeDelay);

        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null) continue;

            sequence.Insert(delay, renderer.DOFade(0f, duration).SetEase(Ease.InSine));
        }
    }

    private void BuildVignette()
    {
        var go = new GameObject("DeathVignette");
        go.transform.SetParent(transform, false);

        vignetteCanvas = go.AddComponent<Canvas>();
        vignetteCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        vignetteCanvas.sortingOrder = vignetteSortingOrder;

        var imageGo = new GameObject("Overlay", typeof(RectTransform));
        var rect = (RectTransform)imageGo.transform;
        rect.SetParent(go.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        vignetteTexture = BuildVignetteTexture();

        vignetteImage = imageGo.AddComponent<Image>();
        vignetteImage.sprite = Sprite.Create(
            vignetteTexture,
            new Rect(0f, 0f, vignetteTexture.width, vignetteTexture.height),
            new Vector2(0.5f, 0.5f));
        vignetteImage.type = Image.Type.Simple;
        vignetteImage.raycastTarget = false;
        vignetteImage.color = new Color(1f, 1f, 1f, 0f);
    }

    private Texture2D BuildVignetteTexture()
    {
        int size = Mathf.Max(64, vignetteTextureSize);
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float half = size * 0.5f;
        float inner = Mathf.Min(vignetteInnerRadius, vignetteOuterRadius - 0.001f);
        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            float dy = (y + 0.5f - half) / half;
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) * 0.5f;
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, vignetteOuterRadius, distance));
                pixels[y * size + x] = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private void BuildSoulParticles(Transform target)
    {
        var go = new GameObject("SoulParticles");
        go.transform.SetParent(target != null ? target : transform, false);
        go.transform.localPosition = Vector3.zero;

        soulParticles = go.AddComponent<ParticleSystem>();

        var main = soulParticles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 600;
        main.gravityModifier = 0f;
        main.startColor = particleColor;
        main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetime.x, particleLifetime.y);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize.x, particleSize.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(particleRiseSpeed.x, particleRiseSpeed.y);

        var shape = soulParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(particleSpread.x, particleSpread.y, 0f);

        var velocity = soulParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
        velocity.y = new ParticleSystem.MinMaxCurve(particleRiseSpeed.x, particleRiseSpeed.y);

        var colorOverLifetime = soulParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(BuildFadeGradient());

        var emission = soulParticles.emission;
        emission.enabled = true;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = particleSortingOrder;
    }

    private Gradient BuildFadeGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.25f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }
}

/* [파일 노트]
 *
 * 패배 컷씬 구현체(2026-08-28 유저 확정 연출). DefeatCutscene 을 상속하므로
 * BossRoom / FinalBossRoom 의 defeatCutscene 필드에 넣거나 씬에 두기만 하면 자동으로 잡힌다.
 *
 * ── 연출 순서 ─────────────────────────────────────────────────────────────────
 *   1) 카메라가 플레이어에게 포커스(DOMove) + 줌인(orthographicSize 트윈).
 *      → 플레이어가 화면 중앙에 오므로 비네트는 중앙 고정으로 충분하다(매 프레임 추적 불필요).
 *   2) 비네트가 서서히 짙어져 플레이어만 남는다.
 *   3) 흰 파티클이 위로 올라가며 방출량이 점점 늘어난다(particleRateStart → particleRateEnd).
 *   4) 플레이어 SpriteRenderer 들이 흐려지며 사라진다.
 *   5) holdAfter 만큼 머문 뒤 onComplete.
 *   화면 페이드와 다음 씬 이동은 이 컷씬의 책임이 아니다 — 호출자(BossRoom/FinalBossRoom)가
 *   onComplete 이후에 처리한다. 일반 보스는 대사→게임오버 UI, 최종보스는 페이드→엔딩 씬으로 간다.
 *
 * ── 구현 메모 ─────────────────────────────────────────────────────────────────
 * - 비네트는 URP 포스트프로세싱 Volume 이 아니라 코드 생성 방사형 텍스처 오버레이다.
 *   Volume 방식은 카메라 포스트프로세싱 설정에 의존해 런타임 검증 없이는 깨질 위험이 커서 배제했다.
 *   sortingOrder 4000 은 ScreenFader(5000) 아래 — 이후 화면 페이드가 비네트까지 덮는다.
 *   raycastTarget=false 라 뒤이어 뜨는 게임오버 UI 클릭을 막지 않는다.
 * - 파티클은 ShallowWaterZone 과 같은 관례(코드 생성 ParticleSystem + Sprites/Default).
 *   플레이어의 자식으로 붙이되 simulationSpace 는 World 라 입자가 따라다니지 않는다.
 * - disableDuringCutscene : 카메라를 따라다니는 스크립트가 있으면 여기에 넣어야 포커스가 먹는다.
 *   비워 두면 아무것도 끄지 않는다.
 * - Stop() 은 시퀀스만 죽이고 비네트/파티클을 정리하지 않는다. 의도된 동작이다 —
 *   BossRoom 은 컷씬 상태를 벗어날 때 Stop() 을 부르는데, 거기서 오브젝트를 지우면
 *   이어지는 대사·게임오버 구간에서 연출이 갑자기 걷혀 버린다. 생성물은 씬 전환과 함께 사라진다.
 * - 트윈 기반이라 PauseManager 의 DOTween.PauseAll 과 호환되지만, BossRoom 이 컷씬 구간에
 *   일시정지를 차단하므로 실제로는 멈출 일이 없다.
 */

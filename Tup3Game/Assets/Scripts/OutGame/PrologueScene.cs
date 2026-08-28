using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class PrologueScene : MonoBehaviour, ISceneEventListener
{
    [Header("2D 라이트")]
    [SerializeField] internal Light2D globalLight;
    [SerializeField] internal Light2D playerLight;

    [Header("연출 대상")]
    [SerializeField] internal SpriteRenderer playerSprite;
    [SerializeField] internal TextMeshProUGUI skipHintText;

    [Header("대사")]
    [SerializeField] internal string dialogueFileName = "S01_MONOLOGUE";
    [SerializeField] internal float autoAdvanceDelay = 1.6f;

    [Header("연출 타이밍(초)")]
    [SerializeField] internal float introDuration = 3f;
    [SerializeField] internal float outroDuration = 1.8f;

    [Header("조명 단계")]
    [SerializeField] internal float globalIntensityStart = 0.02f;
    [SerializeField] internal float globalIntensityEnd = 0.55f;
    [SerializeField] internal float playerIntensityStart = 1.15f;
    [SerializeField] internal float playerIntensityEnd = 0.35f;
    [SerializeField] internal float playerRadiusStart = 4.5f;
    [SerializeField] internal float playerRadiusEnd = 9f;
    [SerializeField] internal int monologueLineCount = 36;

    [Header("도입 비프음")]
    [SerializeField, Range(0f, 1f)] internal float introBeepVolume = 0.7f;
    [SerializeField] internal int introBeepRepeat = 3;
    [SerializeField] internal int introBeepLineCount = 3;

    internal const string SoundIntroBeep = "Intro_Beep";

    private int introBeepId = -1;

    [Header("씬 이동 / 스킵")]
    [SerializeField] internal string nextSceneName = "Lobby";
    [SerializeField] internal KeyCode skipKey = KeyCode.V;
    [SerializeField] internal float skipHoldDuration = 2f;

    [Header("스킵 게이지 (상호작용 UI 와 동일)")]
    [SerializeField] internal Sprite skipGaugeSprite;
    [SerializeField] internal Vector2 skipGaugeAnchoredPosition = new Vector2(-110f, 96f);
    [SerializeField] internal Vector2 skipGaugeSize = new Vector2(72f, 72f);
    [SerializeField] internal int skipGaugeSortingOrder = 810;
    [SerializeField] internal Color skipGaugeIdleColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] internal Color skipGaugeHoldColor = Color.white;

    private Image skipGaugeImage;
    private float skipHold;

    internal StateMachine<PrologueScene> stateMachine;
    internal bool dialogueFinished;
    internal bool skipRequested;

    private readonly List<Tween> tracked = new List<Tween>();
    private Tween skipHintTween;
    private bool started;
    private bool leaving;
    private float bootTimeout = 5f;

    private void Awake()
    {
        SceneController.Instance.RegisterListener(this);

        if (globalLight != null) globalLight.intensity = 0f;
        if (playerLight != null) playerLight.intensity = 0f;
        if (playerSprite != null) playerSprite.color = Color.white;
        if (skipHintText != null) skipHintText.alpha = 0f;
    }

    public void OnSceneLoadComplete(string sceneName)
    {
        BeginPrologue();
    }

    public void OnSceneExit(string sceneName)
    {
        SceneController.Instance.UnregisterListener(this);
        StopIntroBeep();
        KillTracked();
    }

    private void Update()
    {
        if (!started)
        {
            bootTimeout -= Time.deltaTime;
            if (bootTimeout <= 0f)
            {
                Debug.LogWarning("[Prologue] 씬 로드 완료 신호를 받지 못해 연출을 강제로 시작합니다.");
                BeginPrologue();
            }
            return;
        }

        if (!skipRequested && IsSkipPressed()) RequestSkip();

        stateMachine.Execute();
    }

    private void BeginPrologue()
    {
        if (started) return;
        started = true;

        FadeInSkipHint();

        stateMachine = new StateMachine<PrologueScene>();
        stateMachine.Setup(this, new PrologueAwakening());
    }

    private void FadeInSkipHint()
    {
        if (skipHintText == null) return;

        skipHintTween?.Kill();
        skipHintText.alpha = 0f;
        skipHintTween = DOTween
            .To(() => skipHintText.alpha, v => skipHintText.alpha = v, 0.45f, 1.5f)
            .SetDelay(1f)
            .SetEase(Ease.OutSine);
    }

    private bool IsSkipPressed()
    {
        EnsureSkipGauge();

        float duration = Mathf.Max(0.01f, skipHoldDuration);

        if (Input.GetKey(skipKey))
        {
            skipHold += Time.deltaTime;
            ApplySkipGauge(skipHold / duration, true);
            return skipHold >= duration;
        }

        if (skipHold > 0f)
        {
            skipHold = 0f;
            ApplySkipGauge(0f, false);
        }

        return false;
    }

    private void EnsureSkipGauge()
    {
        if (skipGaugeImage != null) return;

        GameObject root = new GameObject("PrologueSkipGauge");
        root.transform.SetParent(transform, false);
        UiViewBuilder.SetupOverlayCanvas(root, skipGaugeSortingOrder);

        GameObject iconObject = new GameObject("Fill");
        iconObject.transform.SetParent(root.transform, false);

        skipGaugeImage = iconObject.AddComponent<Image>();
        skipGaugeImage.raycastTarget = false;
        skipGaugeImage.sprite = skipGaugeSprite != null ? skipGaugeSprite : FindInteractionSprite();
        skipGaugeImage.type = Image.Type.Filled;
        skipGaugeImage.fillMethod = Image.FillMethod.Radial360;
        skipGaugeImage.fillOrigin = 2;

        RectTransform rect = skipGaugeImage.rectTransform;
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = skipGaugeSize;
        rect.anchoredPosition = skipGaugeAnchoredPosition;

        ApplySkipGauge(0f, false);
    }

    private Sprite FindInteractionSprite()
    {
        InteractionView view = FindAnyObjectByType<InteractionView>(FindObjectsInactive.Include);
        if (view == null) return null;

        Image image = view.GetComponent<Image>();
        return image != null ? image.sprite : null;
    }

    private void ApplySkipGauge(float ratio, bool holding)
    {
        if (skipGaugeImage == null) return;

        skipGaugeImage.fillAmount = Mathf.Clamp01(ratio);
        skipGaugeImage.color = holding ? skipGaugeHoldColor : skipGaugeIdleColor;
    }

    internal void RequestSkip()
    {
        if (skipRequested) return;
        skipRequested = true;

        Debug.Log("[Prologue] 스킵 입력 — 프롤로그를 건너뜁니다.");
        stateMachine.ChangeState(new PrologueOutro(true));
    }

    internal void PlayIntroBeep()
    {
        StopIntroBeep();
        introBeepId = AudioManager.Instance.PlaySound(
            SoundIntroBeep, introBeepVolume, Mathf.Max(1, introBeepRepeat));
    }

    internal void StopIntroBeep()
    {
        if (introBeepId < 0) return;

        AudioManager.Instance.StopSound(introBeepId);
        introBeepId = -1;
    }

    internal void Track(Tween tween)
    {
        if (tween != null) tracked.Add(tween);
    }

    internal void KillTracked()
    {
        for (int i = 0; i < tracked.Count; i++)
        {
            tracked[i]?.Kill();
        }
        tracked.Clear();

        skipHintTween?.Kill();
        skipHintTween = null;
    }

    internal void HideSkipHint()
    {
        skipHintTween?.Kill();
        skipHintTween = null;
        if (skipHintText != null) skipHintText.alpha = 0f;

        skipHold = 0f;
        if (skipGaugeImage != null) skipGaugeImage.gameObject.SetActive(false);
    }

    internal void GoToNextScene()
    {
        if (leaving) return;
        leaving = true;

        Debug.Log($"[Prologue] 프롤로그 종료 — '{nextSceneName}' 씬으로 이동합니다.");
        SceneController.Instance.LoadScene(nextSceneName);
    }

    private void OnDestroy()
    {
        KillTracked();
    }
}


internal class PrologueAwakening : State<PrologueScene>
{
    private float timer;

    public override void Enter(PrologueScene entity)
    {
        timer = entity.introDuration;

        if (entity.globalLight != null)
        {
            entity.globalLight.color = Color.white;
            entity.globalLight.intensity = 0f;
            entity.Track(DOTween
                .To(() => entity.globalLight.intensity, v => entity.globalLight.intensity = v,
                    entity.globalIntensityStart, entity.introDuration)
                .SetEase(Ease.InOutSine));
        }

        if (entity.playerLight != null)
        {
            entity.playerLight.intensity = 0f;
            entity.playerLight.pointLightOuterRadius = 1.2f;

            entity.Track(DOTween
                .To(() => entity.playerLight.intensity, v => entity.playerLight.intensity = v,
                    entity.playerIntensityStart, entity.introDuration)
                .SetEase(Ease.OutSine));

            entity.Track(DOTween
                .To(() => entity.playerLight.pointLightOuterRadius, v => entity.playerLight.pointLightOuterRadius = v,
                    entity.playerRadiusStart, entity.introDuration)
                .SetEase(Ease.OutSine));
        }
    }

    public override void Execute(PrologueScene entity)
    {
        timer -= Time.deltaTime;
        if (timer <= 0f) entity.stateMachine.ChangeState(new PrologueMonologue());
    }

    public override void Exit(PrologueScene entity)
    {
        entity.KillTracked();
    }
}


internal class PrologueMonologue : State<PrologueScene>
{
    private DialogueManager dialogue;
    private Action onDialogueEnd;
    private Action<int> onLineShown;

    private float targetGlobal;
    private float targetPlayerIntensity;
    private float targetPlayerRadius;

    private float globalValue;
    private float playerValue;
    private float radiusValue;

    private float fallbackTimer = 4f;

    public override void Enter(PrologueScene entity)
    {
        entity.dialogueFinished = false;

        targetGlobal = entity.globalIntensityStart;
        targetPlayerIntensity = entity.playerIntensityStart;
        targetPlayerRadius = entity.playerRadiusStart;

        globalValue = entity.globalLight != null ? entity.globalLight.intensity : targetGlobal;
        playerValue = entity.playerLight != null ? entity.playerLight.intensity : targetPlayerIntensity;
        radiusValue = entity.playerLight != null ? entity.playerLight.pointLightOuterRadius : targetPlayerRadius;

        dialogue = DialogueManager.Current;
        if (dialogue == null)
        {
            Debug.LogError("[Prologue] DialogueManager 를 찾지 못했습니다. 독백 없이 마무리 연출로 넘어갑니다.");
            return;
        }

        onDialogueEnd = () => entity.dialogueFinished = true;
        onLineShown = lineNumber => UpdateLightTargets(entity, lineNumber);

        dialogue.OnDialogueEnd += onDialogueEnd;
        dialogue.OnLineShown += onLineShown;

        entity.PlayIntroBeep();

        dialogue.SetAutoAdvance(true, entity.autoAdvanceDelay);
        dialogue.StartDialogueFromCsv(entity.dialogueFileName);
    }

    public override void Execute(PrologueScene entity)
    {
        DriveLights(entity);

        if (dialogue == null)
        {
            fallbackTimer -= Time.deltaTime;
            if (fallbackTimer <= 0f) entity.stateMachine.ChangeState(new PrologueOutro(false));
            return;
        }

        if (entity.dialogueFinished) entity.stateMachine.ChangeState(new PrologueOutro(false));
    }

    public override void Exit(PrologueScene entity)
    {
        if (dialogue != null)
        {
            dialogue.OnDialogueEnd -= onDialogueEnd;
            dialogue.OnLineShown -= onLineShown;
            dialogue.SetAutoAdvance(false);
        }

        onDialogueEnd = null;
        onLineShown = null;

        entity.StopIntroBeep();
        entity.KillTracked();
    }

    private void UpdateLightTargets(PrologueScene entity, int lineNumber)
    {
        if (lineNumber > entity.introBeepLineCount) entity.StopIntroBeep();

        int span = Mathf.Max(1, entity.monologueLineCount - 1);
        float t = Mathf.Clamp01((lineNumber - 1) / (float)span);

        targetGlobal = Mathf.Lerp(entity.globalIntensityStart, entity.globalIntensityEnd, t);
        targetPlayerIntensity = Mathf.Lerp(entity.playerIntensityStart, entity.playerIntensityEnd, t);
        targetPlayerRadius = Mathf.Lerp(entity.playerRadiusStart, entity.playerRadiusEnd, t);
    }

    private void DriveLights(PrologueScene entity)
    {
        globalValue = Mathf.MoveTowards(globalValue, targetGlobal, Time.deltaTime * 0.25f);
        playerValue = Mathf.MoveTowards(playerValue, targetPlayerIntensity, Time.deltaTime * 0.4f);
        radiusValue = Mathf.MoveTowards(radiusValue, targetPlayerRadius, Time.deltaTime * 2f);

        if (entity.globalLight != null) entity.globalLight.intensity = globalValue;

        if (entity.playerLight != null)
        {
            float flicker = Mathf.Sin(Time.time * 5.3f) * 0.035f + Mathf.Sin(Time.time * 1.7f) * 0.02f;
            entity.playerLight.intensity = Mathf.Max(0f, playerValue + flicker);
            entity.playerLight.pointLightOuterRadius = radiusValue;
        }
    }
}


internal class PrologueOutro : State<PrologueScene>
{
    private readonly bool skipped;
    private float timer;

    public PrologueOutro(bool skipped)
    {
        this.skipped = skipped;
    }

    public override void Enter(PrologueScene entity)
    {
        entity.HideSkipHint();
        entity.StopIntroBeep();

        var dialogue = DialogueManager.Current;
        if (dialogue != null)
        {
            dialogue.SetAutoAdvance(false);
            dialogue.StopDialogue();
        }

        float duration = skipped ? 0.35f : entity.outroDuration;
        timer = duration;

        if (entity.globalLight != null)
        {
            float target = skipped ? entity.globalIntensityEnd : 1.4f;
            entity.Track(DOTween
                .To(() => entity.globalLight.intensity, v => entity.globalLight.intensity = v, target, duration)
                .SetEase(Ease.InQuad));
        }

        if (entity.playerLight != null)
        {
            entity.Track(DOTween
                .To(() => entity.playerLight.intensity, v => entity.playerLight.intensity = v, 0f, duration)
                .SetEase(Ease.InQuad));
        }
    }

    public override void Execute(PrologueScene entity)
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            return;
        }

        entity.GoToNextScene();
    }

    public override void Exit(PrologueScene entity)
    {
        entity.KillTracked();
    }
}

/* [파일 노트 — Prologue 씬 연출]
 *
 * 1) 전체 흐름
 *    PrologueAwakening → PrologueMonologue → PrologueOutro → SceneController.LoadScene("Lobby")
 *    StartScene.cs 와 같은 StateMachine<T> / State<T> 패턴을 쓰고,
 *    씬 진입 시점은 ISceneEventListener.OnSceneLoadComplete 로 잡는다.
 *    (Awake 에서 RegisterListener → SceneController 가 씬 로드 완료 후 통지)
 *    혹시 Core(SceneController) 가 없어 통지가 오지 않는 경우를 대비해
 *    Update 에서 bootTimeout(5초) 후 강제로 시작하는 안전장치를 뒀다.
 *
 * 2) 조명 연출 (URP 2D Light2D)
 *    프로젝트는 URP 17.3 + 2D Renderer(Assets/Settings/Renderer2D.asset)를 실제로 쓰고 있고,
 *    Player.prefab 의 SpriteRenderer 도 Sprite-Lit-Default 머티리얼이라 Light2D 가 스프라이트에 그대로 먹는다.
 *    - Global Light 2D : 화면 전체 밝기. 거의 암흑(0.02)에서 시작해 독백이 진행될수록 서서히 밝아진다.
 *    - Player Light 2D : 주인공을 비추는 Point 라이트. 처음엔 좁고 강하게(반경 4.5 / 세기 1.15) 시작해서
 *      마지막에는 넓고 옅게(반경 9 / 세기 0.35) 퍼진다. "자아가 흩어진다"는 독백 내용에 맞춘 방향.
 *    - Point 라이트에는 두 개의 사인파를 겹친 미세한 flicker 를 얹어 흔들리는 느낌을 준다.
 *    - 인트로/아웃트로처럼 "한 번에 목표까지" 가는 구간은 DOTween(DOTween.To)으로,
 *      독백 중 대사마다 조금씩 변하는 구간은 매 프레임 Mathf.MoveTowards 로 목표값을 따라가게 했다.
 *      대사 36줄마다 트윈을 새로 만들면 같은 프로퍼티를 두 트윈이 동시에 건드려 값이 튀기 때문이다.
 *    - 조명 단계는 DialogueManager.OnLineShown(지금까지 보여준 줄 수)에 맞춰 0~1 로 정규화한다.
 *      monologueLineCount 기본값 36 은 S01_MONOLOGUE.csv 의 mono_02~mono_37 줄 수와 같다.
 *
 * 3) 대사 자동 진행
 *    DialogueManager.SetAutoAdvance(true, autoAdvanceDelay) 로 켜면 타이핑이 끝난 뒤
 *    autoAdvanceDelay 초 후 자동으로 다음 줄로 넘어간다. V 키 수동 진행은 건드리지 않는다.
 *    마지막 줄(mono_37)은 next 가 비어 있어 DialogueManager 가 EndDialogue → OnDialogueEnd 를 쏘고,
 *    그 신호로 PrologueOutro 로 전환된다.
 *
 * 4) 스킵
 *    skipKeys(기본 Esc / Space / Enter) 중 하나가 눌리면 즉시 PrologueOutro(skipped: true) 로 넘어간다.
 *    Exit 에서 이벤트 구독을 먼저 해제한 뒤 Outro 에서 StopDialogue() 를 부르므로
 *    강제 종료로 발생하는 OnDialogueEnd 때문에 중복 처리되는 일은 없다.
 *    스킵 시에는 0.35초짜리 짧은 암전만 하고 바로 Lobby 로 넘어간다.
 *
 * 5) 트윈 수명
 *    DOTween.To 는 대상(target)이 없는 트윈이라 씬이 내려가도 자동으로 죽지 않는다.
 *    그래서 모든 트윈을 entity.Track() 으로 모아 두고 각 State 의 Exit / OnSceneExit / OnDestroy 에서
 *    KillTracked() 로 정리한다. StartScene.cs 의 delayedCalls 처리와 같은 방식이다.
 *
 * 6) 입력
 *    프로젝트 전반이 legacy Input 을 쓰고 있어 여기서도 Input.GetKeyDown 을 그대로 쓴다.
 *
 * 7) 도입 비프음 (Intro_Beep)
 *    "이승에서 죽음을 맞는 순간, 뚜뚜뚜 하는 하트비트 비프음과 함께 첫 대사가 흐른다"는 기획 의도대로
 *    PrologueMonologue.Enter 에서 StartDialogueFromCsv 직전에 PlayIntroBeep() 으로 시작한다.
 *    (조명이 켜지는 PrologueAwakening 이 아니라 대사 시작 시점에 맞물린다.)
 *    AudioManager.PlaySound 의 repeatTime(introBeepRepeat, 기본 3)으로 "뚜-뚜-뚜" 세 번을 만들고,
 *    반환된 채널 id 를 들고 있다가 StopIntroBeep() 으로 중단한다. 중단 시점은 세 곳이다.
 *      - OnLineShown 이 introBeepLineCount(기본 3) 줄을 넘어갈 때 (도입 3줄이 지나가면 정지)
 *      - PrologueMonologue.Exit / PrologueOutro.Enter (독백 종료·스킵)
 *      - PrologueScene.OnSceneExit (씬 이탈)
 *    AudioManager 는 DontDestroyOnLoad 라 씬을 떠나도 채널이 남을 수 있어 명시적 정지가 필요하다.
 *    파일이 짧아 3회로 부족하거나 길이가 맞지 않으면 introBeepRepeat / introBeepLineCount 로 조절한다.
  *
 * ── 스킵을 V 홀드 2초로 변경 (2026-08-29 유저 요청) ──────────────────────────
 * 예전에는 skipKeys(Esc/Space/Enter) 중 하나를 누르면 즉시 스킵됐다. 이제 skipKey(V)를
 * skipHoldDuration(2초) 동안 누르고 있어야 넘어간다 — 보스방 상호작용과 같은 규약이다.
 * 게이지도 상호작용 UI 와 같은 방식이다: 같은 스프라이트(PlayerUI 의 InteractionIcon 이 쓰는
 * key_v_loading)를 Image.Type.Filled + Radial360 + fillOrigin 2 로 채운다.
 * 프롤로그 씬에는 PlayerUI(InteractionView)가 없으므로 씬을 고치지 않고 코드로 생성한다.
 * 스프라이트는 skipGaugeSprite 로 직접 지정할 수 있고, 비어 있으면 씬의 InteractionView 에서
 * 가져오며, 그것도 없으면 스프라이트 없이 색 사각형으로 그려진다(동작에는 지장 없음).
 * 키를 떼면 게이지가 0 으로 돌아간다(InteractionBase.OnHoldUP 과 같은 동작).
*/

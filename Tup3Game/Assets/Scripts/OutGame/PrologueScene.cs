using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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

    [Header("씬 이동 / 스킵")]
    [SerializeField] internal string nextSceneName = "Lobby";
    [SerializeField] internal KeyCode[] skipKeys = { KeyCode.Escape, KeyCode.Space, KeyCode.Return };

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
        if (skipKeys == null) return false;

        for (int i = 0; i < skipKeys.Length; i++)
        {
            if (Input.GetKeyDown(skipKeys[i])) return true;
        }
        return false;
    }

    internal void RequestSkip()
    {
        if (skipRequested) return;
        skipRequested = true;

        Debug.Log("[Prologue] 스킵 입력 — 프롤로그를 건너뜁니다.");
        stateMachine.ChangeState(new PrologueOutro(true));
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

        entity.KillTracked();
    }

    private void UpdateLightTargets(PrologueScene entity, int lineNumber)
    {
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
 */

using System;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Rendering;

public class StartScene : MonoBehaviour
{
    internal StateMachine<StartScene> stateMachine;
    private void Awake()
    {
        stateMachine = new StateMachine<StartScene>();
        stateMachine.Setup(this, new Booting());
        
    }

    void Update()
    {
        stateMachine.Execute();
    }
}

internal class Booting : State<StartScene>
{
    private float timer = 1;
    public override void Enter(StartScene entity)
    {

    }

    public override void Execute(StartScene entity)
    {
        timer -= Time.deltaTime;
        if(timer <= 0) entity.stateMachine.ChangeState(new IntroCutscene() );
    }

    public override void Exit(StartScene entity)
    {

    }
}

internal class IntroCutscene : State<StartScene>
{
    // 마지막 Advance(23초)가 끝난 뒤 전환되도록 대사 타이밍보다 길게 잡는다
    private float timer = 24;
    private SpriteRenderer fader;
    private readonly List<Tween> delayedCalls = new List<Tween>();
    public override void Enter(StartScene entity)
    {
        Camera.main.transform.DOMoveX(0, 20f).SetEase(Ease.Linear);
        fader = entity.GetComponent<SpriteRenderer>();
        fader.DOFade(0, 1f);
        delayedCalls.Add(DOVirtual.DelayedCall(2, () =>
            DialogueManager.Current.StartDialogueFromCsv("S00_PROLOGUE")));
        delayedCalls.Add(DOVirtual.DelayedCall(8, () =>
            DialogueManager.Current.Advance()));
        delayedCalls.Add(DOVirtual.DelayedCall(15, () =>
            DialogueManager.Current.Advance()));
        delayedCalls.Add(DOVirtual.DelayedCall(23, () =>
            DialogueManager.Current.Advance()));
    }

    public override void Execute(StartScene entity)
    {
        timer -= Time.deltaTime;
        if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Escape)) timer = 0;
        if(timer <= 0) entity.stateMachine.ChangeState(new Menu());

    }

    public override void Exit(StartScene entity)
    {
        foreach (var call in delayedCalls) call.Kill();
        delayedCalls.Clear();
        if (DialogueManager.Current != null) DialogueManager.Current.StopDialogue();
        Camera.main.transform.DOKill();
        fader.DOFade(1, 1f).OnComplete(()=>Camera.main.transform.position =  new Vector3(0, 8, -10));
    }
}

internal class Menu : State<StartScene>
{
    private SpriteRenderer fader;
    private float timer = 10;
    private Volume v;
    public override void Enter(StartScene entity)
    {
        fader = entity.GetComponent<SpriteRenderer>();
        v = GameObject.FindAnyObjectByType<Volume>();
        GameObject.FindAnyObjectByType<ParticleSystem>().Stop();

        DOVirtual.DelayedCall(1, () =>
        {
            fader.DOFade(0, 2f).SetEase(Ease.Linear);
            v.gameObject.SetActive(false);
        });
    }

    public override void Execute(StartScene entity)
    {

    }

    public override void Exit(StartScene entity)
    {

    }
}

/* [파일 노트]
 * Start 씬 흐름: Booting(1초) → IntroCutscene → Menu.
 * IntroCutscene 은 카메라 20초 패닝 + S00_PROLOGUE 대사를 DelayedCall(2/8/15/23초)로 재생하고
 * 24초 뒤 Menu 로 전환된다. 아무 키나 누르면(Input.anyKeyDown, 마우스 포함) 즉시 스킵된다.
 * 단 ESC 는 스킵 키에서 제외했다 — Start 씬에서 ESC 는 PauseManager 의 옵션 패널 토글 전용이라
 * "옵션을 열었더니 인트로까지 스킵되는" 이중 동작을 막기 위함이다.
 * Exit 에서 DelayedCall 전부 Kill + 대화 강제 종료(StopDialogue) + 카메라 트윈 Kill 을 처리하므로
 * 스킵 시점과 무관하게 잔여 콜백이 남지 않는다.
 * Start 씬은 일시정지 개념이 없다(PauseManager 가 IsPaused 를 세우지 않고 옵션만 띄운다).
 */
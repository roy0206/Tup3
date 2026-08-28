using System;
using UnityEngine;

public abstract class DefeatCutscene : MonoBehaviour
{
    public abstract void Play(Action onComplete);

    public virtual void Stop() { }
}

/* [파일 노트]
 *
 * 보스방 패배 연출의 훅. BossRoom 이 패배 판정 후 defeatDelay 를 기다린 다음
 * Play(onComplete) 를 한 번 호출하고, onComplete 가 불릴 때까지 대사·게임오버 UI 를 미룬다.
 *
 * ── 쓰는 법 ───────────────────────────────────────────────────────────────────
 *   이 클래스를 상속해 DOTween 으로 연출을 짜고, 보스 씬 아무 오브젝트에 붙인 뒤
 *   BossRoom 의 defeatCutscene 필드에 넣는다(비워 두면 씬에서 자동으로 찾는다).
 *
 *     public class MyDefeatCutscene : DefeatCutscene
 *     {
 *         Sequence seq;
 *
 *         public override void Play(Action onComplete)
 *         {
 *             seq = DOTween.Sequence();
 *             seq.Append(...);
 *             seq.OnComplete(() => onComplete?.Invoke());
 *         }
 *
 *         public override void Stop()
 *         {
 *             seq?.Kill();
 *             seq = null;
 *         }
 *     }
 *
 * ── 계약 ──────────────────────────────────────────────────────────────────────
 * - onComplete 는 반드시 호출해야 다음 단계(패배 대사)로 넘어간다. 호출하지 않으면
 *   BossRoom 의 defeatCutsceneTimeout(기본 20초) 이 지난 뒤 경고 로그와 함께 강제로 넘어간다.
 * - onComplete 를 두 번 이상 불러도 BossRoom 쪽에서 무시하므로 안전하다.
 * - Stop() 은 연출이 자연 종료된 경우에도 상태 전이 시 호출될 수 있다. 멱등하게 구현할 것.
 * - 컷씬을 붙이지 않으면(필드 null) 이 구간은 그대로 건너뛴다 — 패배 → 대사 → UI 로만 흐른다.
 * - DOTween 을 쓰면 PauseManager 의 DOTween.PauseAll() 에 자동으로 함께 멈춘다.
 *   코루틴/Invoke 로 짜면 일시정지 중에도 진행되므로 트윈 기반을 권장한다.
 * - 카메라만 움직이는 단순 연출이면 CutsceneManager/CutsceneSO 를 이 안에서 호출해도 된다.
 */

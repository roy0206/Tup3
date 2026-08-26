using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneManager : Singleton<CutsceneManager>
{
    const string ResourcesFolder = "Cutscenes";

    public bool IsPlaying => current != null;

    readonly Dictionary<string, CutsceneSO> registry = new(StringComparer.Ordinal);

    Sequence current;
    Transform currentCamera;
    Vector3 cameraStartPosition;
    Action pendingOnComplete;
    bool registryLoaded;

    protected override void OnAwake()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void OnActiveSceneChanged(Scene previous, Scene next)
    {
        if (IsPlaying) Stop(restoreCamera: false);
    }

    public void Register(CutsceneSO cutscene)
    {
        if (cutscene == null) return;
        registry[cutscene.name] = cutscene;
    }

    public bool Play(string cutsceneName, Action onComplete = null, Camera targetCamera = null)
    {
        EnsureRegistryLoaded();
        if (!registry.TryGetValue(cutsceneName, out var so))
        {
            Debug.LogError($"[CutsceneManager] '{cutsceneName}' 컷씬을 찾을 수 없습니다. Resources/{ResourcesFolder}/ 에 있는지 확인하세요.");
            onComplete?.Invoke();
            return false;
        }
        return Play(so, onComplete, targetCamera);
    }

    public bool Play(CutsceneSO cutscene, Action onComplete = null, Camera targetCamera = null)
    {
        if (cutscene == null || cutscene.steps == null || cutscene.steps.Count == 0)
        {
            Debug.LogWarning("[CutsceneManager] 재생할 컷씬이 비어 있습니다.");
            onComplete?.Invoke();
            return false;
        }

        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogError("[CutsceneManager] 대상 카메라를 찾을 수 없습니다.");
            onComplete?.Invoke();
            return false;
        }

        if (IsPlaying)
        {
            Debug.LogWarning($"[CutsceneManager] 이미 재생 중인 컷씬을 중단하고 '{cutscene.name}' 을 시작합니다.");
            Stop(restoreCamera: false);
        }

        currentCamera = cam.transform;
        cameraStartPosition = currentCamera.position;
        pendingOnComplete = onComplete;

        var seq = DOTween.Sequence();
        foreach (var step in cutscene.steps)
        {
            Vector3 target = ResolveTarget(step);
            if (cutscene.keepCameraZ) target.z = cameraStartPosition.z;

            if (step.moveDuration <= 0f)
            {
                var captured = target;
                seq.AppendCallback(() => currentCamera.position = captured);
            }
            else
            {
                seq.Append(currentCamera.DOMove(target, step.moveDuration).SetEase(step.ease));
            }

            if (step.holdDuration > 0f)
                seq.AppendInterval(step.holdDuration);
        }

        if (cutscene.returnToStart)
        {
            if (cutscene.returnDuration <= 0f)
                seq.AppendCallback(() => currentCamera.position = cameraStartPosition);
            else
                seq.Append(currentCamera.DOMove(cameraStartPosition, cutscene.returnDuration).SetEase(Ease.InOutSine));
        }

        seq.SetTarget(this).OnKill(HandleSequenceEnd);
        current = seq;
        return true;
    }

    public void Stop(bool restoreCamera = false)
    {
        if (!IsPlaying) return;

        if (restoreCamera && currentCamera != null)
            currentCamera.position = cameraStartPosition;

        var seq = current;
        current = null;
        seq.Kill();
    }

    Vector3 ResolveTarget(CutsceneSO.Step step)
    {
        if (string.IsNullOrWhiteSpace(step.anchorName)) return step.position;

        var anchor = GameObject.Find(step.anchorName);
        if (anchor != null) return anchor.transform.position;

        Debug.LogWarning($"[CutsceneManager] 앵커 '{step.anchorName}' 를 씬에서 찾지 못해 position 값으로 대체합니다.");
        return step.position;
    }

    void HandleSequenceEnd()
    {
        current = null;
        currentCamera = null;

        var callback = pendingOnComplete;
        pendingOnComplete = null;
        callback?.Invoke();
    }

    void EnsureRegistryLoaded()
    {
        if (registryLoaded) return;
        registryLoaded = true;

        foreach (var so in Resources.LoadAll<CutsceneSO>(ResourcesFolder))
            registry[so.name] = so;
    }
}

/* [파일 노트]
 * SO 기반 카메라 컷씬 재생기. 이름 또는 SO 참조로 호출한다.
 *   CutsceneManager.Instance.Play("CAM_Example", () => { 후처리 });
 * 코루틴에서 대기하려면: bool done = false; Play(이름, () => done = true);
 * yield return new WaitUntil(() => done);
 *
 * 규칙:
 * - Resources/Cutscenes/ 의 CutsceneSO 를 첫 재생 시점에 전부 등록한다(에셋 이름 = 호출 이름).
 *   폴더 밖 SO 는 Register() 로 수동 등록하거나 Play(so) 로 직접 재생한다.
 * - 대상 카메라는 기본 Camera.main, 파라미터로 교체 가능.
 * - onComplete 는 자연 종료뿐 아니라 Stop()/씬 전환으로 중단돼도 반드시 1회 호출된다
 *   (호출자 상태머신이 영원히 대기하는 것을 막기 위함).
 * - 씬 전환 시 자동 중단(카메라 복원 없음). 일시정지는 PauseManager 의 DOTween.PauseAll 이
 *   시퀀스를 함께 멈추므로 별도 처리가 필요 없다.
 * - 입력 잠금·대사 연동 같은 연출 제어는 호출자 책임이다. 이 클래스는 카메라만 움직인다.
 */

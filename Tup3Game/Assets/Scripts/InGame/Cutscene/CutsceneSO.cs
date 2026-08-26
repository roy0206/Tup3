using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CutsceneSO",
    menuName = "CutsceneSO")]
public class CutsceneSO : ScriptableObject
{
    [Serializable]
    public class Step
    {
        [Tooltip("씬 오브젝트 이름으로 목표 지점을 지정한다. 비워두면 아래 position 을 사용한다.")]
        public string anchorName;

        [Tooltip("anchorName 이 비어 있을 때 사용하는 월드 좌표 목표 지점")]
        public Vector3 position;

        [Tooltip("이 지점까지 카메라가 이동하는 시간 (초). 0 이면 즉시 이동.")]
        [Min(0f)]
        public float moveDuration = 1f;

        [Tooltip("이동에 사용할 DOTween 이징")]
        public Ease ease = Ease.InOutSine;

        [Tooltip("도착 후 머무는 시간 (초)")]
        [Min(0f)]
        public float holdDuration = 0f;
    }

    [Header("컷씬 단계")]
    [Tooltip("카메라가 순서대로 거쳐 갈 지점 목록")]
    public List<Step> steps = new();

    [Header("옵션")]
    [Tooltip("카메라의 z 값을 유지한다 (2D 카메라 권장). 끄면 목표 지점의 z 를 그대로 사용한다.")]
    public bool keepCameraZ = true;

    [Tooltip("컷씬 종료 후 카메라를 시작 위치로 되돌린다")]
    public bool returnToStart = false;

    [Tooltip("시작 위치로 되돌아가는 시간 (초). returnToStart 가 true 일 때만 사용된다.")]
    [Min(0f)]
    public float returnDuration = 1f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (steps == null || steps.Count == 0)
            Debug.LogWarning($"[CutsceneSO] '{name}' : steps 가 비어 있습니다.", this);
    }
#endif
}

/* [파일 노트]
 * 카메라 무브먼트 전용 컷씬 데이터. Create > CutsceneSO 로 에셋을 만들고
 * Assets/Resources/Cutscenes/ 에 두면 CutsceneManager 가 에셋 이름으로 찾아 재생한다.
 * 각 Step 은 "어디로(anchorName 또는 position), 얼마 동안(moveDuration), 어떤 이징으로(ease),
 * 도착 후 얼마나 머물지(holdDuration)"만 정의한다. anchorName 은 재생 시점에
 * GameObject.Find 로 해석되므로 씬에 존재하는 오브젝트 이름이어야 하며,
 * 못 찾으면 경고 후 position 으로 대체된다.
 * 연출 잠금(입력 차단 등)은 이 시스템의 책임이 아니다 — 호출자가 BossRoom 처럼 직접 관리한다.
 */

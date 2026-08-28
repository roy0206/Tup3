using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SwordTrap : MonoBehaviour
{
    [Header("등장 플래시")]
    [SerializeField] private bool playSpawnFlash = true;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Ease flashEase = Ease.OutQuad;

    [Header("소멸")]
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Ease fadeEase = Ease.InQuad;
    [SerializeField] private bool disableDamageOnFade = true;

    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private readonly List<Behaviour> damageBehaviours = new();
    private readonly List<Collider2D> damageColliders = new();

    private Sequence lifeSequence;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) baseColors[i] = renderers[i].color;
        }

        damageBehaviours.Clear();
        foreach (Hitbox hitbox in GetComponentsInChildren<Hitbox>(true))
        {
            if (hitbox != null) damageBehaviours.Add(hitbox);
        }

        damageColliders.Clear();
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
        {
            if (col != null) damageColliders.Add(col);
        }
    }

    private void OnEnable()
    {
        ResetVisualState();

        PlaySpawnFlash();

        ScheduleDespawn();
    }

    private void OnDisable()
    {
        KillLifeSequence();
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null) renderer.DOKill();
        }
    }

    private void PlaySpawnFlash()
    {
        if (!playSpawnFlash || flashDuration <= 0f) return;

        SpriteFlashGroup group = SpriteFlashGroup.GetOrAdd(gameObject);
        if (group == null) return;

        Tween flash = group.Flash(flashColor, flashDuration);
        if (flash != null) flash.SetEase(flashEase);
    }

    private void ResetVisualState()
    {
        KillLifeSequence();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].DOKill();
            renderers[i].color = baseColors[i];
        }

        SetDamageEnabled(true);
    }

    private void ScheduleDespawn()
    {
        float life = Mathf.Max(0f, lifeTime);
        float fade = Mathf.Max(0f, fadeDuration);

        lifeSequence = DOTween.Sequence().SetTarget(this);
        lifeSequence.AppendInterval(life);

        lifeSequence.AppendCallback(() =>
        {
            if (disableDamageOnFade) SetDamageEnabled(false);
        });

        if (fade > 0f)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                lifeSequence.Insert(life, renderers[i].DOFade(0f, fade).SetEase(fadeEase));
            }
        }

        lifeSequence.OnComplete(Despawn);
    }

    private void Despawn()
    {
        lifeSequence = null;

        if (PoolManager.Instance != null) PoolManager.Instance.Release(gameObject);
        else gameObject.SetActive(false);
    }

    private void SetDamageEnabled(bool value)
    {
        foreach (Behaviour behaviour in damageBehaviours)
        {
            if (behaviour != null) behaviour.enabled = value;
        }

        foreach (Collider2D col in damageColliders)
        {
            if (col != null) col.enabled = value;
        }
    }

    private void KillLifeSequence()
    {
        if (lifeSequence == null) return;

        Sequence seq = lifeSequence;
        lifeSequence = null;
        seq.Kill();
    }
}

/* [파일 노트]
 *
 * 금보스 패턴2 가 소환하는 검 함정. PoolManager 로 꺼내 쓰므로 모든 상태를 OnEnable 에서 초기화한다.
 *
 * ── 이전 상태 ────────────────────────────────────────────────────────────────
 * 내용이 통째로 주석 처리돼 있어 **소환된 함정이 영원히 사라지지 않았다**(풀 반납 경로 없음).
 * Gold.Pattern2 도 Release 를 예약하지 않으므로 수명 관리는 이 클래스의 책임이다.
 *
 * ── 등장 플래시 (2026-08-29 유저 요청) ───────────────────────────────────────
 * SpriteFlashGroup.GetOrAdd(gameObject).Flash(...) 로 자식 SpriteRenderer 전부를 한 트윈으로
 * 흰색까지 물들였다 되돌린다. color 를 곱하는 방식이 아니라 셰이더 _FlashAmount 를 쓰므로
 * 원래 색이 흰색이어도 제대로 밝아진다.
 * ※ 확장 메서드 gameObject.DOFlashAll(...) 을 쓰지 않는다 — 정의는 SpriteFlashExtensions 에
 *   있는데 Unity 의 Assembly-CSharp 컴파일에서 확장 메서드 해석에 실패하는 사례가 있었다
 *   (Roslyn 단독 컴파일은 통과, Unity 만 CS1061). 정적 API 직접 호출은 그 영향을 받지 않는다.
 * ※ 이 효과는 Tools/Tup3/Setup Sprite Flash 를 한 번 실행해 플래시 머티리얼이 있어야 보인다.
 *   없으면 SpriteFlash 가 경고 1회만 남기고 조용히 넘어가므로 함정 동작 자체에는 지장이 없다.
 *
 * ── 소멸 ─────────────────────────────────────────────────────────────────────
 * lifeTime(3초) 유지 → fadeDuration(0.5초) 동안 알파 0 으로 페이드 → 풀 반납.
 * 페이드가 시작되는 순간 disableDamageOnFade 로 Hitbox 와 Collider2D 를 꺼서, 사라지는 중인
 * 함정에 맞는 상황을 막는다(끄고 싶지 않으면 인스펙터에서 해제).
 * 알파를 건드리므로 풀에서 다시 꺼낼 때 ResetVisualState 가 원래 색을 복원하고 판정을 되살린다.
 * 원래 색은 Awake 에서 1회 캐시한다 — OnEnable 에서 읽으면 페이드된 알파가 기준이 되어버린다.
 *
 * DOTween 기반이라 PauseManager 의 DOTween.PauseAll 에 함께 멈춘다(일시정지 중 수명이 흐르지 않는다).
 * OnDisable 에서 시퀀스와 렌더러 트윈을 모두 정리해 풀 재사용 시 잔여 트윈이 남지 않는다.
 */

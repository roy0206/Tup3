using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LavaPool : MonoBehaviour
{
    SpriteRenderer spriteRenderer;

    private Hitbox[] hitboxes;
    private Collider2D[] hitColliders;
    private bool[] hitColliderDefaults;
    private Color baseColor = Color.white;
    private bool isHardened;
    private Tween hardenTween;

    private static readonly List<LavaPool> hardenedPools = new List<LavaPool>();

    [Header("사운드")]
    [SerializeField] private float sizzleVolume = 0.6f;
    [SerializeField] private float sizzleMinInterval = 0.5f;

    private const string SizzleSound = "Fire_LavaSizzle";

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        hitboxes = GetComponentsInChildren<Hitbox>(true);
        hitColliders = GetComponentsInChildren<Collider2D>(true);
        hitColliderDefaults = new bool[hitColliders.Length];
        for (int i = 0; i < hitColliders.Length; i++)
            hitColliderDefaults[i] = hitColliders[i] != null && hitColliders[i].enabled;

        if (spriteRenderer != null) baseColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ResetHardening();
        BossSound.PlayThrottled(SizzleSound, sizzleVolume, sizzleMinInterval);
    }

    private void OnDisable()
    {
        KillHardenTween();
        hardenedPools.Remove(this);
    }

    private void FixedUpdate()
    {
        if (PauseManager.IsPaused) return;
        if (isHardened) return;

        spriteRenderer.flipX = !spriteRenderer.flipX;
    }

    public void Harden(float duration, int maxCount, Color hardenedColor)
    {
        ResetHardening();
        RegisterHardened(this, maxCount);

        float fadeTime = Mathf.Max(0.01f, duration);
        if (spriteRenderer != null)
        {
            hardenTween = spriteRenderer
                .DOColor(hardenedColor, fadeTime)
                .SetEase(Ease.Linear)
                .OnComplete(CompleteHardening);
            return;
        }

        hardenTween = DOVirtual.DelayedCall(fadeTime, CompleteHardening);
    }

    private void CompleteHardening()
    {
        hardenTween = null;
        isHardened = true;
        SetDamageEnabled(false);
    }

    private void ResetHardening()
    {
        KillHardenTween();
        isHardened = false;
        if (spriteRenderer != null) spriteRenderer.color = baseColor;
        SetDamageEnabled(true);
    }

    private void KillHardenTween()
    {
        if (hardenTween != null && hardenTween.IsActive()) hardenTween.Kill();
        hardenTween = null;
    }

    private void SetDamageEnabled(bool value)
    {
        if (hitboxes == null || hitColliders == null || hitColliderDefaults == null) return;

        for (int i = 0; i < hitboxes.Length; i++)
            if (hitboxes[i] != null) hitboxes[i].enabled = value;

        for (int i = 0; i < hitColliders.Length; i++)
            if (hitColliders[i] != null) hitColliders[i].enabled = value && hitColliderDefaults[i];
    }

    private static void RegisterHardened(LavaPool pool, int maxCount)
    {
        hardenedPools.Remove(pool);
        hardenedPools.Add(pool);
        if (maxCount <= 0) return;

        while (hardenedPools.Count > maxCount)
        {
            LavaPool oldest = hardenedPools[0];
            hardenedPools.RemoveAt(0);
            if (oldest == null) continue;

            if (PoolManager.Instance != null) PoolManager.Instance.Release(oldest.gameObject);
            else oldest.gameObject.SetActive(false);
        }
    }
}

/* [파일 노트]
 * 기본 경로(화보스) : 착지한 용암 장판. FixedUpdate 마다 flipX 를 뒤집어 일렁이는 연출을 내고,
 * 데미지는 같은 오브젝트의 표준 Hitbox 가 담당하며, 수명은 소환자(Lava.Land)의
 * PoolManager.Release(pool, 10f) 예약이 관리한다. 이 경로는 Harden 을 호출하지 않으므로
 * 아래 굳음 로직이 전혀 개입하지 않는다.
 *
 * 굳음 경로(최종보스 화 돌진 화염구) : Lava.SetHardenOnLand 가 걸린 화염구가 착지하면
 * Harden(duration, maxCount, color) 이 호출된다.
 *   - duration 동안 DOColor 로 스프라이트를 서서히 검게 물들인다(삼도천 얕은 물에 식는 연출).
 *     이 동안 Hitbox/Collider2D 는 그대로 살아 있어 데미지 판정이 유지된다.
 *   - 트윈이 끝나면 isHardened 로 일렁임을 멈추고 Hitbox 와 Collider2D 를 전부 꺼서 무해해진다.
 *   - Release 예약을 걸지 않으므로 굳은 덩어리는 맵에 계속 남는다.
 *   - 무한 누적 방지용으로 static 리스트에 등록하고, maxCount 를 넘으면 가장 오래된 것부터
 *     풀에 반납한다(maxCount <= 0 이면 상한 없음). 수치는 FinalBoss 쪽 SerializeField 가 정한다.
 *
 * 풀 재사용 안전장치 : OnEnable 의 ResetHardening 이 색·Hitbox·Collider·트윈을 원상 복구하므로
 * 굳어서 반납된 인스턴스를 화보스가 다시 꺼내 써도 정상 장판으로 동작한다.
 * baseColor/Hitbox/Collider 참조는 Awake 에서 1회 캐시한다.
 *
 * 사운드 Fire_LavaSizzle : 장판이 생기는 순간(OnEnable) 재생한다. 장판은 화보스가 돌진 1회당 5개,
 * 최종보스가 8개를 만들므로 sizzleMinInterval(기본 0.5초) 스로틀로 겹침을 막는다.
 * Lava 의 착지음(Fire_LavaLand, 0.12초 스로틀)과는 다른 이름이라 서로의 스로틀에 영향을 주지 않고,
 * 착지 순간에 "쿵(LavaLand) + 치익(LavaSizzle)"이 겹쳐 들리도록 의도한 것이다.
 * 굳음 완료(CompleteHardening)에는 소리를 붙이지 않았다 — 배정된 파일이 없다.
 */

using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SkillGroundMarker : MonoBehaviour
{
    [Header("투사체 소멸")]
    [SerializeField] private bool consumeProjectiles = true;
    [SerializeField] private LayerMask projectileMask = ~0;
    [SerializeField] private float projectileFadeDuration = 0.25f;
    [SerializeField] private Ease projectileFadeEase = Ease.OutQuad;
    [SerializeField] private float overlapPadding = 0.05f;

    [SerializeField]
    private string[] projectileTypeNames =
    {
        "Lava", "LavaPool", "Ice_Bullet", "Storm", "Water_Sprout", "WaterPump",
        "Electric_ball", "SoilWave", "SwordTrap",
    };

    private SpriteRenderer originalRenderer;
    private SpriteSequencePlayer visualPlayer;

    private Collider2D bodyCollider;
    private readonly HashSet<int> consumed = new HashSet<int>();
    private readonly Collider2D[] overlapBuffer = new Collider2D[32];

    private void Awake()
    {
        bodyCollider = GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        if (!consumeProjectiles || bodyCollider == null) return;
        if (PauseManager.IsPaused) return;

        Bounds bounds = bodyCollider.bounds;
        Vector2 size = (Vector2)bounds.size + Vector2.one * Mathf.Max(0f, overlapPadding) * 2f;

        int count = Physics2D.OverlapBoxNonAlloc(bounds.center, size, 0f, overlapBuffer, projectileMask);
        for (int i = 0; i < count; i++)
        {
            Transform root = ResolveProjectileRoot(overlapBuffer[i]);
            if (root != null) ConsumeProjectile(root.gameObject);
        }
    }

    private Transform ResolveProjectileRoot(Collider2D other)
    {
        if (other == null) return null;
        if (other.transform.IsChildOf(transform)) return null;

        for (Transform t = other.transform; t != null; t = t.parent)
        {
            MonoBehaviour[] behaviours = t.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null) continue;
                if (IsProjectileType(behaviours[i].GetType().Name)) return t;
            }
        }

        return null;
    }

    private bool IsProjectileType(string typeName)
    {
        if (projectileTypeNames == null) return false;

        for (int i = 0; i < projectileTypeNames.Length; i++)
        {
            if (string.Equals(projectileTypeNames[i], typeName, System.StringComparison.Ordinal)) return true;
        }

        return false;
    }

    private void ConsumeProjectile(GameObject projectile)
    {
        if (projectile == null) return;
        if (!consumed.Add(projectile.GetInstanceID())) return;

        DOTween.Kill(projectile.transform);

        List<Behaviour> disabled = new List<Behaviour>();
        foreach (MonoBehaviour behaviour in projectile.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null || !behaviour.enabled) continue;
            behaviour.enabled = false;
            disabled.Add(behaviour);
        }

        List<Collider2D> colliders = new List<Collider2D>();
        foreach (Collider2D col in projectile.GetComponentsInChildren<Collider2D>(true))
        {
            if (col == null || !col.enabled) continue;
            col.enabled = false;
            colliders.Add(col);
        }

        foreach (Rigidbody2D body in projectile.GetComponentsInChildren<Rigidbody2D>(true))
        {
            if (body == null) continue;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        SpriteRenderer[] renderers = projectile.GetComponentsInChildren<SpriteRenderer>(true);
        Color[] baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) baseColors[i] = renderers[i].color;
        }

        float duration = Mathf.Max(0.01f, projectileFadeDuration);
        Sequence fade = DOTween.Sequence().SetTarget(this);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            fade.Insert(0f, renderers[i].DOFade(0f, duration).SetEase(projectileFadeEase));
        }

        fade.OnComplete(() => ReleaseProjectile(projectile, renderers, baseColors, disabled, colliders));
    }

    private void ReleaseProjectile(
        GameObject projectile,
        SpriteRenderer[] renderers,
        Color[] baseColors,
        List<Behaviour> disabled,
        List<Collider2D> colliders)
    {
        if (projectile == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].color = baseColors[i];
        }

        for (int i = 0; i < disabled.Count; i++)
        {
            if (disabled[i] != null) disabled[i].enabled = true;
        }

        for (int i = 0; i < colliders.Count; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = true;
        }

        consumed.Remove(projectile.GetInstanceID());

        if (PoolManager.Instance != null && PoolManager.Instance.IsPooled(projectile))
            PoolManager.Instance.Release(projectile);
        else
            Destroy(projectile);
    }

    public void ApplyBarrierVisual(
        IList<Sprite> frames,
        float frameRate,
        Vector2 localOffset,
        float scale,
        int sortingOrderOffset,
        bool hideOriginalSprite)
    {
        if (frames == null || frames.Count == 0) return;
        if (visualPlayer != null) return;

        originalRenderer = GetComponent<SpriteRenderer>();
        if (hideOriginalSprite && originalRenderer != null) originalRenderer.enabled = false;

        GameObject visual = new GameObject("BarrierVisual");
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = localOffset;
        visual.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        if (originalRenderer != null)
        {
            renderer.sortingLayerID = originalRenderer.sortingLayerID;
            renderer.sortingOrder = originalRenderer.sortingOrder + sortingOrderOffset;
        }
        else
        {
            renderer.sortingOrder = sortingOrderOffset;
        }

        visualPlayer = visual.AddComponent<SpriteSequencePlayer>();
        visualPlayer.SetSequence(frames, frameRate, false, false);
        visualPlayer.Restart();
    }
}

/* [파일 노트]
 * 플레이어 스킬2(지형생성)로 소환된 지형을 식별하는 마커 컴포넌트.
 * Skills.SpawnGroundAfterDelay 가 지형 Instantiate 직후 AddComponent 로 붙인다.
 * 최종보스의 토 파동 투사체(SoilWave)가 이 마커를 감지하면 즉시 소멸한다(속성 상성).
 *
 * 장벽 이펙트 (토 속성 soil_1~4)
 *  ApplyBarrierVisual 은 소환된 지형(s_skill 프리팹)의 겉모습을 토 스프라이트 시퀀스로 갈아끼운다.
 *  프리팹(Assets/Scripts/Player/Attack/s_skill.prefab)을 직접 고치지 않기 위해 전부 런타임 구성이다.
 *   - 프리팹 원본 SpriteRenderer 는 enabled = false 로 숨긴다(hideOriginalSprite).
 *     BoxCollider2D 는 건드리지 않으므로 충돌 크기/판정은 그대로다.
 *   - 자식 "BarrierVisual" 오브젝트를 만들어 거기서 시퀀스를 재생한다. 자식으로 분리한 덕에
 *     이펙트 스케일/오프셋을 콜라이더와 독립적으로 조절할 수 있다(스프라이트 실측 크기가
 *     프리팹의 1x1 콜라이더와 달라 스케일 보정이 필요할 수 있다).
 *   - 정렬은 원본 렌더러의 SortingLayer 를 그대로 쓰고 sortingOrder 에 오프셋만 더한다.
 *   - loop = false, deactivateOnComplete = false 로 재생한다. 즉 생성 연출로 4프레임을 한 번만
 *     재생하고, 끝난 뒤에는 마지막 프레임이 그대로 남아 벽의 정지 외형이 된다.
 *     (스킬 지속시간이 끝나면 Skills 가 지형 오브젝트째로 Destroy 한다.)
 *     원본 렌더러를 숨기는 구조라 이 "마지막 프레임 유지"는 필수다 — 시퀀스가 스스로 꺼지면
 *     벽이 통째로 안 보이게 된다. 이중 표시 걱정은 없다(원본은 이미 enabled=false).
 *  프레임/스케일/오프셋/정렬 수치는 전부 Skills 인스펙터에 있고 이 함수는 인자로만 받는다 —
 *  마커는 런타임 AddComponent 라 자기 [SerializeField] 값을 가질 수 없기 때문이다.
 *
 * 프레임 순서 실측 (soil_1~4, 각 256x256 / PPU 100 / pivot Center)
 *  파일명 순서 != 애니메이션 순서다. 알파 실측 결과 실제 순서는 soil_1 → soil_4 → soil_2 → soil_3.
 *   - 흙기둥 본체(가로 80px 이상 불투명 런이 있는 행)의 최상단 y : 135 → 112 → 69 → 65 (벽이 솟아오름)
 *   - 본체 행 수                                              : 61 → 84 → 127 → 131 (점점 높아짐)
 *   - 반투명 먼지 비율(0<a<200)                                : 32.0% → 15.9% → 3.7% → 3.6% (먼지가 걷힘)
 *   - 검은 외곽선 픽셀 수                                       : 891 → 1058 → 1698 → 1814 (테두리가 닫힘)
 *  네 지표가 모두 같은 방향으로 단조 변화하므로 순서는 확정적이다.
 *  soil_3 만 사방 외곽선이 완전히 닫힌 깨끗한 벽 블록이라 마지막 프레임으로 둔다.
 *  soil_2 는 아직 아래쪽 테두리가 뚫려 있고 흙가시가 남아 있어 완성 직전 프레임이다.
 *  네 장 모두 바닥선이 y=196~197 로 정렬돼 있어 프레임별 오프셋 보정은 필요 없다.
 */

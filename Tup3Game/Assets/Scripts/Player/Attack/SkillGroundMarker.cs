using System.Collections.Generic;
using UnityEngine;

public class SkillGroundMarker : MonoBehaviour
{
    private SpriteRenderer originalRenderer;
    private SpriteSequencePlayer visualPlayer;

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
 *  프레임/스케일/오프셋/정렬 수치는 전부 Skills 인스펙터에 있고 이 함수는 인자로만 받는다 —
 *  마커는 런타임 AddComponent 라 자기 [SerializeField] 값을 가질 수 없기 때문이다.
 */

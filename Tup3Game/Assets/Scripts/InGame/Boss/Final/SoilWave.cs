using UnityEngine;

public class SoilWave : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private bool spriteFacesRight = false;

    private float direction = 1f;
    private float remaining;
    private bool released;

    public void Launch(float dir)
    {
        direction = dir >= 0f ? 1f : -1f;
        float visualSign = spriteFacesRight ? direction : -direction;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * visualSign;
        transform.localScale = scale;
    }

    private void OnEnable()
    {
        remaining = lifeTime;
        released = false;
    }

    private void Update()
    {
        if (PauseManager.IsPaused) return;
        if (released) return;

        transform.Translate(Vector3.right * (direction * speed * Time.deltaTime), Space.World);
        remaining -= Time.deltaTime;
        if (remaining <= 0f) ReleaseSelf();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandleHit(other);
    }

    private void HandleHit(Collider2D other)
    {
        if (released) return;

        if (other.GetComponentInParent<SkillGroundMarker>() != null)
        {
            ReleaseSelf();
        }
    }

    private void ReleaseSelf()
    {
        released = true;
        if (PoolManager.Instance != null) PoolManager.Instance.Release(gameObject);
        else gameObject.SetActive(false);
    }
}

/* [파일 노트]
 * 최종보스 "토 파동" 투사체. 토보스 패턴1의 전진 히트박스(hitboxTransforms[2] 10유닛 전진)를
 * 풀 기반 투사체로 옮긴 것이다.
 *
 * 역할 분리
 *   - 이 스크립트는 이동·수명·SkillGroundMarker 접촉 소멸만 담당한다.
 *   - 플레이어 피해는 같은 오브젝트의 표준 Hitbox 컴포넌트(damage/knockbackForce)가 담당하며,
 *     같은 트리거 콜라이더를 공유한다(Hitbox 의 OnTriggerStay2D 가 PlayerKnockBack.TakeHit 호출).
 *     빌더(Tools/Tup3/Create SoilWave Prefab)가 Hitbox(damage 15, knockback 1)를 자동 배선한다.
 *
 * 사용법
 *   - PoolManager 에 등록(Addressable 라벨 "Pool", 프리팹 이름 = FinalBoss 의 soilWavePoolKey, 기본 "SoilWave").
 *   - 프리팹 구성: SpriteRenderer + Trigger Collider2D + Kinematic Rigidbody2D + 이 스크립트 + Hitbox.
 *     Rigidbody2D 가 있어야 정지한 트리거 지형과의 OnTrigger 판정이 보장된다.
 *   - FinalBoss 가 Get 직후 Launch(방향) 을 호출한다. 방향은 localScale.x 부호로 스프라이트에도 반영된다.
 *
 * 판정
 *   - 속성 상성: SkillGroundMarker(플레이어 스킬2 지형)가 부모 체인에 있으면 즉시 소멸.
 *     일반 지형·벽은 무시하고 lifeTime 이 다하면 스스로 반납한다.
 *   - 파동은 플레이어를 관통한다(무적 프레임이 Hitbox 의 Stay 연타를 막는다).
 *
 * 일시정지: Update 첫 줄 PauseManager.IsPaused 게이트. 피해는 TakeHit 쪽 게이트가 차단.
 * 수명 관리는 자체 반납이므로 소환 측에서 Release 예약이 필요 없다.
 */

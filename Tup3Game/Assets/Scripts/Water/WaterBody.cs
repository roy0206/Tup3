using System;
using UnityEngine;

namespace SimWater
{
public class WaterBody : MonoBehaviour
{
    [HideInInspector]
    public Rigidbody2D rigidbody;
    [HideInInspector]
    public Collider2D collider;

    [SerializeField] private float horizontalWakeFactor = 0.08f;

    private Vector3 _lastPosition;
    private float _transformVelocityY;
    private float _transformVelocityX;

    public Bounds bounds => collider.bounds;

    public float velocityY
    {
        get
        {
            float vy;
            float vx;
            if (rigidbody != null && rigidbody.bodyType == RigidbodyType2D.Dynamic)
            {
                vy = rigidbody.linearVelocityY;
                vx = rigidbody.linearVelocityX;
            }
            else
            {
                vy = _transformVelocityY;
                vx = _transformVelocityX;
            }
            return vy - Mathf.Abs(vx) * horizontalWakeFactor;
        }
    }

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        _lastPosition = transform.position;
        _transformVelocityY = 0f;
        _transformVelocityX = 0f;
    }

    void Update()
    {
        if (Time.deltaTime > 0f)
        {
            Vector3 delta = transform.position - _lastPosition;
            _transformVelocityY = delta.y / Time.deltaTime;
            _transformVelocityX = delta.x / Time.deltaTime;
        }
        _lastPosition = transform.position;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.TryGetComponent(out Water water))
        {
            WaterSystem.Collide(water, this);
        }
    }
}
}

/* [파일 노트]
 * Tavern_Gamejam_CAU_SSU 프로젝트(Assets/Scripts/Water/WaterBody.cs)에서 이식한 물 상호작용체 —
 * 물(SimWater.Water) 트리거 안에 있는 동안 WaterSystem.Collide 로 수면에 속도를 전달한다.
 * 수정 사항:
 * - namespace SimWater 로 감쌌다(클래스명 충돌 방지).
 * - [RequireComponent(typeof(Rigidbody2D))] 제거 + velocityY 프로퍼티 추가.
 *   Tup3 플레이어/최종보스는 Rigidbody2D 없이 트랜스폼으로 움직이므로, RB 가 없으면
 *   Update 에서 트랜스폼 y 델타로 수직 속도를 추정해 대신 쓴다.
 *   원본에서 WaterSystem 이 직접 읽던 body.rigidbody.linearVelocityY 는 body.velocityY 로 대체됐다.
 * - RB 속도는 Dynamic 바디일 때만 신뢰한다 — 투사체(SoilWave/FlyingSword)는 Kinematic RB 를
 *   transform.Translate/트윈으로 움직여 linearVelocity 가 항상 0 이므로, Kinematic 도 트랜스폼
 *   델타 추정을 쓴다.
 * - horizontalWakeFactor: 원본 시뮬은 수직 속도만 파동에 전달해 수평으로 스치는 물체가 물을
 *   못 가른다. 수평 속력의 일부(기본 0.2)를 하향 속도로 섞어 지나가는 자리에 물살(웨이크)을
 *   만든다 — 걷는 플레이어의 잔물결, 수면 위를 나는 파동·어검의 물가름이 여기서 나온다.
 *   0 이면 원본 동작.
 */

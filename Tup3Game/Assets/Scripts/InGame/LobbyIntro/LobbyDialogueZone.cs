using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class LobbyDialogueZone : MonoBehaviour, ILobbyIntroStep
{
    [Header("도입부")]
    [SerializeField] private int stepOrder;
    [SerializeField] private bool retryWhileInside = true;

    [Header("기즈모")]
    [SerializeField] private Color gizmoColor = new Color(0.25f, 0.8f, 1f, 0.25f);
    [SerializeField] private Vector2 fallbackGizmoSize = new Vector2(1f, 4f);

    private Collider2D zoneCollider;
    private bool consumed;

    public int StepOrder => stepOrder;

    private void Reset()
    {
        var collider = GetComponent<BoxCollider2D>();
        collider.isTrigger = true;

        var body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.simulated = true;
    }

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
        zoneCollider.isTrigger = true;

        var body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.simulated = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryAttempt(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!retryWhileInside) return;
        TryAttempt(other);
    }

    private void TryAttempt(Collider2D other)
    {
        if (consumed) return;
        if (other == null || other.GetComponentInParent<Playermovement>() == null) return;
        Attempt();
    }

    private void Attempt()
    {
        LobbyIntroDirector director = LobbyIntroDirector.Current;
        if (director == null) return;
        if (!director.TryFire(this)) return;

        consumed = true;
        ShutDown();
    }

    public void OnIntroDisabled()
    {
        consumed = true;
        ShutDown();
    }

    private void ShutDown()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
        enabled = false;
    }

    private void OnDrawGizmos()
    {
        Bounds bounds = ResolveBounds();
        Vector3 size = new Vector3(bounds.size.x, bounds.size.y, 0.01f);

        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(bounds.center, size);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(bounds.center, size);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(bounds.center + Vector3.up * (bounds.extents.y + 0.4f), $"대사 트리거 #{stepOrder}");
#endif
    }

    private Bounds ResolveBounds()
    {
        Collider2D own = zoneCollider != null ? zoneCollider : GetComponent<Collider2D>();
        if (own != null && own.enabled && own.bounds.size.sqrMagnitude > 0.0001f) return own.bounds;
        return new Bounds(transform.position, new Vector3(fallbackGizmoSize.x, fallbackGizmoSize.y, 0f));
    }
}

/* [파일 노트]
 * 플레이어가 지나가면 발동하는 위치형 도입부 대사 트리거. 순번은 stepOrder 로 지정한다.
 *
 * [감지 방식]
 * 이 프로젝트의 트리거 존 관례대로, 트리거 오브젝트 쪽에 Kinematic Rigidbody2D 를 붙여
 * OnTriggerEnter2D / OnTriggerStay2D 물리 콜백으로 감지한다 (UnderWater 등과 동일 패턴).
 * RequireComponent 로 Rigidbody2D 를 강제하고 Reset/Awake 에서 Kinematic + isTrigger 를 자동 설정하므로
 * 컴포넌트만 붙이면 별도 설정이 필요 없다.
 * 플레이어 판정은 태그가 아니라 Playermovement 컴포넌트로 한다(프로젝트에 Player 태그 규약이 일관되지 않음).
 *
 * [발동 규칙]
 * - 발동 가능 여부는 LobbyIntroDirector 가 순번으로 판정한다. 자기 순번이 아니면 TryFire() 가 거짓을 돌려주므로
 *   역주행으로 이전 트리거에 다시 닿거나 점프로 뒤쪽 트리거에 먼저 닿아도 대사가 진행되지 않는다.
 * - 발동에 성공하면 콜라이더와 컴포넌트를 모두 꺼서 one-shot 이 된다.
 *   (물리 콜백은 스크립트 비활성화만으로는 멈추지 않으므로 콜라이더를 끄는 것이 핵심이고, consumed 가 이중 안전장치)
 * - retryWhileInside 가 켜져 있으면 영역 안에 있는 동안 OnTriggerStay2D 로 재시도한다.
 *   Director 의 쿨다운(minStepInterval)에 걸려 거절된 경우를 자동으로 복구해 주지만,
 *   앞 순번이 끝나기 전에 이 영역에 미리 들어와 있으면 차례가 오는 즉시 발동한다는 뜻이기도 하다.
 *   "지나가는 그 순간에만" 발동시키고 싶으면 끄면 된다.
 * - 기즈모는 콜라이더 실제 영역과 순번 라벨을 그린다. 콜라이더가 없거나 꺼져 있으면 fallbackGizmoSize 로 그린다.
 */

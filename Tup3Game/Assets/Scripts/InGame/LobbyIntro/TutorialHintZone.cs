using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class TutorialHintZone : MonoBehaviour
{
    [Header("안내 문구")]
    [SerializeField] private TMP_Text hintText;
    [SerializeField, TextArea] private string message = "X 키로 점프";

    [Header("표시 설정")]
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private bool showOnce;

    [Header("기즈모")]
    [SerializeField] private Color gizmoColor = new Color(0.4f, 1f, 0.5f, 0.2f);
    [SerializeField] private Vector2 fallbackGizmoSize = new Vector2(3f, 3f);

    private Collider2D zoneCollider;
    private bool alreadyShown;
    private float alpha;
    private float targetAlpha;

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

        if (hintText == null) hintText = GetComponentInChildren<TMP_Text>(true);
        if (hintText == null) return;

        if (!string.IsNullOrEmpty(message)) hintText.text = message;
        hintText.gameObject.SetActive(true);

        alpha = 0f;
        targetAlpha = 0f;
        ApplyAlpha();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (showOnce && alreadyShown) return;

        alreadyShown = true;
        targetAlpha = 1f;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        targetAlpha = 0f;
    }

    private static bool IsPlayer(Collider2D other)
    {
        return other != null && other.GetComponentInParent<Playermovement>() != null;
    }

    private void Update()
    {
        if (hintText == null) return;
        if (Mathf.Approximately(alpha, targetAlpha)) return;

        float step = fadeDuration <= 0f ? 1f : Time.deltaTime / fadeDuration;
        alpha = Mathf.MoveTowards(alpha, targetAlpha, step);
        ApplyAlpha();
    }

    private void ApplyAlpha()
    {
        Color color = hintText.color;
        color.a = alpha;
        hintText.color = color;
    }

    private void OnDrawGizmos()
    {
        Collider2D own = zoneCollider != null ? zoneCollider : GetComponent<Collider2D>();
        Bounds bounds = (own != null && own.enabled && own.bounds.size.sqrMagnitude > 0.0001f)
            ? own.bounds
            : new Bounds(transform.position, new Vector3(fallbackGizmoSize.x, fallbackGizmoSize.y, 0f));

        Vector3 size = new Vector3(bounds.size.x, bounds.size.y, 0.01f);
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(bounds.center, size);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(bounds.center, size);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(bounds.center + Vector3.up * (bounds.extents.y + 0.4f), "튜토리얼 안내");
#endif
    }
}

/* [파일 노트]
 * 튜토리얼 복도의 조작 안내(예: 단차 앞에서 "X 키로 점프")를 월드 텍스트로 띄우는 최소 컴포넌트.
 *
 * - 이 프로젝트의 트리거 존 관례대로 Kinematic Rigidbody2D + OnTriggerEnter2D/Exit2D 로 감지한다.
 *   RequireComponent 와 Reset/Awake 자동 설정 덕에 컴포넌트만 붙이면 된다.
 * - 플레이어가 영역에 들어오면 페이드 인, 나가면 페이드 아웃한다. showOnce 를 켜면 최초 1회만 보여준다.
 * - hintText 를 비워 두면 자식에서 TMP_Text 를 자동으로 찾는다.
 *   월드 스페이스 TextMeshPro(3D)든 월드 스페이스 Canvas 의 TextMeshProUGUI 든 둘 다 TMP_Text 라 그대로 동작한다.
 * - 페이드는 DOTween 대신 색상 알파 보간으로 처리했다. DOTween 의 TMP 모듈 활성화 여부에 의존하지 않기 위함이다.
 * - 대사 진행과는 무관한 순수 안내용이라 ILobbyIntroStep 을 구현하지 않는다. 대사가 필요하면 LobbyDialogueZone 을 쓸 것.
 */

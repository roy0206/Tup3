using DG.Tweening;
using UnityEngine;

public class RisingWaterPhase : MonoBehaviour
{
    [Header("물 오브젝트")]
    [SerializeField] private Transform waterRoot;
    [SerializeField] private Transform surfacePoint;

    [Header("수위")]
    [SerializeField] private float startY = -8f;
    [SerializeField] private float targetY = 1.5f;
    [SerializeField] private float riseDuration = 4f;
    [SerializeField] private Ease riseEase = Ease.InOutSine;

    [Header("수영 판정 여유")]
    [SerializeField, Min(0f)] private float enterDepth = 0.05f;
    [SerializeField, Min(0f)] private float exitDepth = 0.15f;

    private Playermovement player;
    private Collider2D playerCollider;
    private Tween riseTween;
    private bool hasStarted;
    private bool playerIsSwimming;

    public bool HasReachedTarget { get; private set; }

    private void Awake()
    {
        FindPlayer();
        PrepareWater();
    }

    private void Update()
    {
        if (!hasStarted)
            return;

        if (player == null)
            FindPlayer();

        if (player == null || waterRoot == null)
            return;

        float surfaceY = surfacePoint != null
            ? surfacePoint.position.y
            : waterRoot.position.y;

        float playerBodyY = playerCollider != null
            ? playerCollider.bounds.center.y
            : player.transform.position.y;

        float submergedDepth = surfaceY - playerBodyY;

        if (!playerIsSwimming && submergedDepth >= enterDepth)
        {
            SetPlayerSwimming(true);
        }
        else if (playerIsSwimming && submergedDepth <= -exitDepth)
        {
            SetPlayerSwimming(false);
        }
    }

    public bool BeginRise()
    {
        if (hasStarted)
            return true;

        if (waterRoot == null)
        {
            Debug.LogError("RisingWaterPhase: Water Root가 연결되지 않았습니다.", this);
            return false;
        }

        hasStarted = true;
        HasReachedTarget = false;

        waterRoot.gameObject.SetActive(true);
        SetWaterY(startY);

        riseTween?.Kill();

        if (riseDuration <= 0f)
        {
            SetWaterY(targetY);
            HasReachedTarget = true;
            return true;
        }

        riseTween = waterRoot
            .DOMoveY(targetY, riseDuration)
            .SetEase(riseEase)
            .OnComplete(() => HasReachedTarget = true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        return true;
    }

    public void StopAndHide()
    {
        riseTween?.Kill();
        riseTween = null;

        hasStarted = false;
        HasReachedTarget = false;
        SetPlayerSwimming(false);

        if (waterRoot != null)
        {
            SetWaterY(startY);
            waterRoot.gameObject.SetActive(false);
        }
    }

    private void PrepareWater()
    {
        if (waterRoot == null)
            return;

        SetWaterY(startY);
        waterRoot.gameObject.SetActive(false);
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
            return;

        player = playerObject.GetComponent<Playermovement>();
        playerCollider = playerObject.GetComponent<Collider2D>();
    }

    private void SetWaterY(float y)
    {
        Vector3 position = waterRoot.position;
        position.y = y;
        waterRoot.position = position;
    }

    private void SetPlayerSwimming(bool value)
    {
        if (playerIsSwimming == value)
            return;

        playerIsSwimming = value;

        if (player != null)
            player.SetInWater(value);
    }

    private void OnDisable()
    {
        riseTween?.Kill();
        riseTween = null;
        hasStarted = false;
        HasReachedTarget = false;
        SetPlayerSwimming(false);
    }

    private void OnDestroy()
    {
        riseTween?.Kill();
        SetPlayerSwimming(false);
    }

    private void OnValidate()
    {
        riseDuration = Mathf.Max(0f, riseDuration);
        enterDepth = Mathf.Max(0f, enterDepth);
        exitDepth = Mathf.Max(enterDepth, exitDepth);
    }
}

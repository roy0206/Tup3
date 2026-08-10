using UnityEngine;

public class Storm_animation : MonoBehaviour
{
    [Header("좌우 흔들림")]
    [SerializeField] private float swayDistance = 0.08f;
    [SerializeField] private float swaySpeed = 3f;

    [Header("크기 변화")]
    [SerializeField] private float widthPulse = 0.06f;
    [SerializeField] private float heightPulse = 0.02f;
    [SerializeField] private float pulseSpeed = 4f;

    [Header("상하 움직임")]
    [SerializeField] private float floatDistance = 0.03f;
    [SerializeField] private float floatSpeed = 2f;

    private Vector3 startPosition;
    private Vector3 startScale;

    private void Awake()
    {
        startPosition = transform.localPosition;
        startScale = transform.localScale;
    }

    private void Update()
    {
        float time = Time.time;

        float sway =
            Mathf.Sin(time * swaySpeed) * swayDistance;

        float floating =
            Mathf.Sin(time * floatSpeed) * floatDistance;

        transform.localPosition =
            startPosition + new Vector3(sway, floating, 0f);

        float scaleX =
            1f + Mathf.Sin(time * pulseSpeed) * widthPulse;

        float scaleY =
            1f + Mathf.Sin(time * pulseSpeed * 0.7f) * heightPulse;

        transform.localScale = new Vector3(
            startScale.x * scaleX,
            startScale.y * scaleY,
            startScale.z
        );
    }
}

using UnityEngine;

public class Storm_animation : MonoBehaviour
{
    [Header("소용돌이 조각")]
    [SerializeField] private Transform top;
    [SerializeField] private Transform middle;
    [SerializeField] private Transform bottom;

    [Header("회전")]
    [SerializeField] private float topAngle = 4f;
    [SerializeField] private float middleAngle = 6f;
    [SerializeField] private float bottomAngle = 8f;
    [SerializeField] private float rotationSpeed = 4f;

    [Header("좌우 움직임")]
    [SerializeField] private float topMove = 0.08f;
    [SerializeField] private float middleMove = 0.05f;
    [SerializeField] private float bottomMove = 0.025f;

    [Header("폭 변화")]
    [SerializeField] private float widthPulse = 0.04f;

    private Vector3 topPosition;
    private Vector3 middlePosition;
    private Vector3 bottomPosition;

    private Vector3 topScale;
    private Vector3 middleScale;
    private Vector3 bottomScale;

    private void Awake()
    {
        topPosition = top.localPosition;
        middlePosition = middle.localPosition;
        bottomPosition = bottom.localPosition;

        topScale = top.localScale;
        middleScale = middle.localScale;
        bottomScale = bottom.localScale;
    }

    private void Update()
    {
        float time = Time.time * rotationSpeed;

        AnimatePart(
            top,
            topPosition,
            topScale,
            Mathf.Sin(time),
            topAngle,
            topMove
        );

        AnimatePart(
            middle,
            middlePosition,
            middleScale,
            Mathf.Sin(time + 2.1f),
            middleAngle,
            middleMove
        );

        AnimatePart(
            bottom,
            bottomPosition,
            bottomScale,
            Mathf.Sin(time + 4.2f),
            bottomAngle,
            bottomMove
        );
    }

    private void AnimatePart(
        Transform part,
        Vector3 originalPosition,
        Vector3 originalScale,
        float wave,
        float angle,
        float moveDistance)
    {
        part.localRotation =
            Quaternion.Euler(0f, 0f, wave * angle);

        part.localPosition =
            originalPosition + Vector3.right * wave * moveDistance;

        float scaleX = 1f + wave * widthPulse;

        part.localScale = new Vector3(
            originalScale.x * scaleX,
            originalScale.y,
            originalScale.z
        );
    }
}

using UnityEngine;

public class Ocean_animation : MonoBehaviour
{
    [System.Serializable]
    private class WaterLayer
    {
        public Transform target;

        [Header("좌우 움직임")]
        public float horizontalDistance = 0.15f;
        public float horizontalSpeed = 1f;

        [Header("상하 움직임")]
        public float verticalDistance = 0.05f;
        public float verticalSpeed = 1.5f;

        [Header("시작 시간 차이")]
        public float phase;

        [HideInInspector]
        public Vector3 startPosition;
    }

    [SerializeField] private WaterLayer[] layers;

    private void Awake()
    {
        foreach (WaterLayer layer in layers)
        {
            if (layer.target != null)
                layer.startPosition = layer.target.localPosition;
        }
    }

    private void Update()
    {
        float time = Time.time;

        foreach (WaterLayer layer in layers)
        {
            if (layer.target == null)
                continue;

            float x = Mathf.Sin(
                time * layer.horizontalSpeed + layer.phase
            ) * layer.horizontalDistance;

            float y = Mathf.Sin(
                time * layer.verticalSpeed + layer.phase
            ) * layer.verticalDistance;

            layer.target.localPosition =
                layer.startPosition + new Vector3(x, y, 0f);
        }
    }
}
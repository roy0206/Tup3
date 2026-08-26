using UnityEngine;

public class Lava : MonoBehaviour
{
    [SerializeField] private float gravity;
    [SerializeField] private float limitX;
    [SerializeField] private float groundY;

    private Vector2 origin;
    private Vector2 initialVelocity;
    private float flightTime;
    private float curTime;

    private void OnEnable()
    {
        curTime = 0;
        origin = transform.position;

        float dx = UnityEngine.Random.Range(-limitX, limitX) - origin.x;
        float dy = groundY - origin.y;

        flightTime = UnityEngine.Random.Range(2f, 3f);
        initialVelocity = new Vector2(
            dx / flightTime,
            (dy + 0.5f * gravity * flightTime * flightTime) / flightTime);
    }

    private void Update()
    {
        if (PauseManager.IsPaused) return;

        curTime += Time.deltaTime;

        if (curTime >= flightTime)
        {
            Land();
            return;
        }
        var targetPos = PositionAt(curTime);
        var targetVec = targetPos - (Vector2)transform.position;
        transform.position = targetPos;
        
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(targetVec.y, targetVec.x) * Mathf.Rad2Deg + 90);
    }

    private Vector2 PositionAt(float t)
    {
        return new Vector2(
            origin.x + initialVelocity.x * t,
            origin.y + initialVelocity.y * t - 0.5f * gravity * t * t);
    }

    private void Land()
    {
        var pool = PoolManager.Instance.Get("LavaPool", PositionAt(flightTime), Quaternion.identity);
        if (pool != null) PoolManager.Instance.Release(pool, 10f);

        PoolManager.Instance.Release(gameObject);
    }
}

using System.Collections;
using UnityEngine;

/// <summary>
/// 화면 한쪽 끝에서 반대쪽으로 파도가 밀고 들어오는 연출.
/// 선두(front)가 지나가는 컬럼마다 즉시 기준 수위를 올리고 큰 impulse를 줘서
/// "덮치며 솟구쳤다가 가라앉는" 크레스트 느낌을 냅니다 (리그오브레전드 나미 궁극기 참고).
/// </summary>
[RequireComponent(typeof(WaterSurface))]
public class TsunamiWaveSequence : MonoBehaviour
{
    [SerializeField] private WaterSurface surface;

    [Header("진행 방향 / 속도")]
    [SerializeField] private bool moveRight = true;   // true: 왼쪽 → 오른쪽, false: 오른쪽 → 왼쪽
    [SerializeField] private float travelSpeed = 8f;   // 초당 이동 거리 (world unit). 클수록 빠르게 훑고 지나감

    [Header("파도 크레스트 (치솟았다 무너지는 연출)")]
    [SerializeField] private float crestPeakHeight = 4f;      // 선두 도달 순간 순간적으로 치솟는 높이 (targetLevel 기준 상대값). 클수록 압도적으로 높이 솟았다가 무너짐
    [SerializeField] private float crestForwardImpulse = 4f;  // 치솟음에 더해지는 약간의 전방 impulse (옆으로 무너지는 느낌 보강용, 0으로 둬도 무방)
    [SerializeField] private float targetLevel = 0f;          // 파도가 지나간 자리에 남는 최종 수위

    [Header("유지 / 빠짐")]
    [SerializeField] private float holdDuration = 2f;
    [SerializeField] private float recedeDuration = 1f;
    [SerializeField] private float hiddenLevel = -3f;

    [Header("벽(장애물)에 막히면 정지")]
    [SerializeField] private bool stopAtWalls = true; // 켜두면 막힌 경계를 만났을 때 선두가 잠시 멈춤 (수위가 벽을 넘을 때까지)

    private bool[] wetted;

    private void Awake()
    {
        if (surface == null)
            surface = GetComponent<WaterSurface>();
    }

    /// <summary>보스 패턴 코드에서 이걸 호출해서 쓰나미를 시작합니다.</summary>
    public void StartTsunami()
    {
        StartCoroutine(RunTsunami());
    }

    private IEnumerator RunTsunami()
    {
        int count = surface.ColumnCount;
        wetted = new bool[count];

        float startX = moveRight ? surface.GetLeftEdgeWorldX() : surface.GetRightEdgeWorldX();
        float endX = moveRight ? surface.GetRightEdgeWorldX() : surface.GetLeftEdgeWorldX();
        float totalDistance = Mathf.Abs(endX - startX);
        float travelDuration = Mathf.Max(0.01f, totalDistance / travelSpeed);

        // 마지막으로 물이 도달한 컬럼 인덱스 (벽에 막혔을 때 여기서 대기)
        int lastWetIndex = moveRight ? -1 : count;

        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelDuration);
            float frontX = Mathf.Lerp(startX, endX, t);

            for (int step = 0; step < count; step++)
            {
                int i = moveRight ? lastWetIndex + 1 : lastWetIndex - 1;
                if (i < 0 || i >= count) break;
                if (wetted[i]) break;

                float colX = surface.GetColumnWorldX(i);
                bool reachedByTime = moveRight ? colX <= frontX : colX >= frontX;
                if (!reachedByTime) break;

                // 벽 체크: 이전 컬럼과 이 컬럼 사이 경계가 막혀 있으면 여기서 대기
                if (stopAtWalls && lastWetIndex >= 0 && lastWetIndex < count)
                {
                    int boundaryIndex = moveRight ? lastWetIndex : i;
                    if (surface.IsBoundaryBlocked(boundaryIndex))
                        break;
                }

                wetted[i] = true;
                surface.SetColumnBaseLevel(i, targetLevel);
                surface.SetColumnHeightOffset(i, crestPeakHeight); // 순간적으로 확 치솟게 세팅 → 스프링이 알아서 무너뜨림
                if (crestForwardImpulse != 0f)
                    surface.AddImpulseAtColumn(i, moveRight ? -crestForwardImpulse : crestForwardImpulse); // 진행 방향으로 살짝 기울어지며 무너지는 느낌
                lastWetIndex = i;
            }

            bool allWetted = moveRight ? lastWetIndex >= count - 1 : lastWetIndex <= 0;
            if (allWetted || elapsed >= travelDuration * 3f) // 벽에 너무 오래 막혀 있으면 안전장치로 종료
                break;

            yield return null;
        }

        // 혹시 못 채운 컬럼이 남아있다면(벽에 끝까지 막힌 경우) 그대로 둠 - 벽이 사라지면 다음 프레임부터 spread로 자연스럽게 이어짐

        yield return new WaitForSeconds(holdDuration);

        // 빠지는 구간: 전체를 hiddenLevel로 서서히 내림
        float recedeElapsed = 0f;
        while (recedeElapsed < recedeDuration)
        {
            recedeElapsed += Time.deltaTime;
            float rt = Mathf.Clamp01(recedeElapsed / recedeDuration);

            for (int i = 0; i < count; i++)
            {
                surface.SetColumnBaseLevel(i, Mathf.Lerp(targetLevel, hiddenLevel, rt));
            }
            yield return null;
        }
    }
}

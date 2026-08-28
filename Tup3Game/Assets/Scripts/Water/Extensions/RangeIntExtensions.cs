using UnityEngine;

namespace SimWater
{
public static class RangeIntExtensions
{
    public static RangeInt Intersect(this RangeInt a, RangeInt b)
    {
        int start = Mathf.Max(a.start, b.start);
        int end = Mathf.Min(a.end, b.end);
        return start >= end ? new RangeInt(0, 0) : new RangeInt(start, end - start);
    }
}
}

/* [파일 노트]
 * Tavern_Gamejam_CAU_SSU 프로젝트(Assets/Scripts/Extensions/RangeIntExtensions.cs)에서 이식.
 * 수정 사항: namespace SimWater 로 감쌌다(내용 동일).
 */
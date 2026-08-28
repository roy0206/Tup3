using UnityEngine;

namespace SimWater
{
public static class RectExtensions
{
    public static float SqrDistance(this Rect rect, Vector2 point)
    {
        point -= rect.center;
        point = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - rect.size / 2;
        if (point.x < 0 && point.y < 0) return 0f;
        return new Vector2(Mathf.Max(0, point.x), Mathf.Max(0, point.y)).sqrMagnitude;
    }
}
}

/* [파일 노트]
 * Tavern_Gamejam_CAU_SSU 프로젝트(Assets/Scripts/Extensions/RectExtensions.cs)에서 이식.
 * 수정 사항: namespace SimWater 로 감쌌다(내용 동일).
 */
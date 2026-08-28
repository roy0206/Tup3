using Unity.Collections;
using UnityEngine;

namespace SimWater
{
public static class NativeArrayExtensions
{
    public static NativeArray<T> GetSubArray<T>(this NativeArray<T> array, RangeInt range) where T : struct
        => array.GetSubArray(range.start, range.length);
}
}

/* [파일 노트]
 * Tavern_Gamejam_CAU_SSU 프로젝트(Assets/Scripts/Extensions/NativeArrayExtensions.cs)에서 이식.
 * 수정 사항: namespace SimWater 로 감쌌다(내용 동일).
 */
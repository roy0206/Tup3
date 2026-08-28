using System;
using UnityEngine;
using UnityEngine.LowLevel;

namespace SimWater
{
public static class PlayerLoopExtensions
{
    public static void RegisterTo<TParent>(this PlayerLoopSystem system, bool deregisterOnApplicationQuit = true)
    {
        var root = PlayerLoop.GetCurrentPlayerLoop();
        ref var parent = ref root.Find<TParent>();
        Array.Resize(ref parent.subSystemList, parent.subSystemList.Length + 1);
        parent.subSystemList[^1] = system;
        PlayerLoop.SetPlayerLoop(root);

        if (deregisterOnApplicationQuit)
            Application.quitting += () => system.DeregisterFrom<TParent>();
    }
    
    static void DeregisterFrom<TParent>(this PlayerLoopSystem system)
    {
        var root = PlayerLoop.GetCurrentPlayerLoop();
        ref var parent = ref root.Find<TParent>();
        int index = Array.IndexOf(parent.subSystemList, system);
        if (index < 0) return;
        for (int i = index; i < parent.subSystemList.Length - 1; i++)
            parent.subSystemList[i] = parent.subSystemList[i + 1];
        Array.Resize(ref parent.subSystemList, parent.subSystemList.Length - 1);
        PlayerLoop.SetPlayerLoop(root);
    }

    static ref PlayerLoopSystem Find<T>(this PlayerLoopSystem root)
    {
        for (int i = 0; i < root.subSystemList.Length; i++)
            if(root.subSystemList[i].type == typeof(T))
                return ref root.subSystemList[i];

        throw new Exception($"PlayerLoopSystem of {typeof(T)} is not found.");
    }
}
}

/* [파일 노트]
 * Tavern_Gamejam_CAU_SSU 프로젝트(Assets/Scripts/Extensions/PlayerLoopExtensions.cs)에서 이식 —
 * PlayerLoop 서브시스템 등록/해제 유틸(WaterSystem 이 사용).
 * 수정 사항: namespace SimWater 로 감쌌다(내용 동일).
 */
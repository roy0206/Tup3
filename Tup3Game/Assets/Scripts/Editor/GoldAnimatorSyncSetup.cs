using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class GoldAnimatorSyncSetup
{
    private const string MenuPath = "Tools/Tup3/금 보스 애니메이터 동기화 설정";
    private const string ControllerPath = "Assets/GameAssets/Bosses/Boss_Gold/Animations/GoldBoss.controller";
    private const string SpeedParameter = "AnimSpeed";

    private static readonly string[] SpeedDrivenStates =
    {
        "Cut1",
        "Cut2",
        "Pattern3",
        "Pattern4",
        "CounterAttack",
    };

    [MenuItem(MenuPath, false, 17)]
    public static void Run()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"[GoldAnimatorSyncSetup] 컨트롤러를 찾지 못했습니다: {ControllerPath}");
            return;
        }

        var report = new List<string>();
        bool changed = EnsureSpeedParameter(controller, report);

        foreach (AnimatorState state in CollectStates(controller))
        {
            if (System.Array.IndexOf(SpeedDrivenStates, state.name) < 0) continue;
            changed |= ApplySpeedParameter(state, report);
        }

        foreach (string missing in FindMissingStates(controller))
        {
            report.Add($"경고 : '{missing}' 상태를 찾지 못했습니다.");
        }

        if (changed)
        {
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        string body = report.Count > 0
            ? string.Join("\n", report)
            : "이미 설정돼 있습니다. 변경 없음.";

        Debug.Log($"[GoldAnimatorSyncSetup]\n{body}");
        EditorUtility.DisplayDialog("금 보스 애니메이터 동기화 설정", body, "확인");
    }

    private static bool EnsureSpeedParameter(AnimatorController controller, List<string> report)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name != SpeedParameter) continue;
            if (parameter.type == AnimatorControllerParameterType.Float) return false;

            report.Add($"경고 : 파라미터 '{SpeedParameter}' 가 Float 이 아닙니다({parameter.type}). 손으로 지우고 다시 실행하세요.");
            return false;
        }

        controller.AddParameter(new AnimatorControllerParameter
        {
            name = SpeedParameter,
            type = AnimatorControllerParameterType.Float,
            defaultFloat = 1f,
        });

        report.Add($"파라미터 추가 : {SpeedParameter} (Float, 기본값 1)");
        return true;
    }

    private static bool ApplySpeedParameter(AnimatorState state, List<string> report)
    {
        bool changed = false;

        if (!Mathf.Approximately(state.speed, 1f))
        {
            state.speed = 1f;
            changed = true;
        }

        if (state.speedParameter != SpeedParameter)
        {
            state.speedParameter = SpeedParameter;
            changed = true;
        }

        if (!state.speedParameterActive)
        {
            state.speedParameterActive = true;
            changed = true;
        }

        if (changed) report.Add($"상태 '{state.name}' : 재생 속도를 {SpeedParameter} 로 연결");
        return changed;
    }

    private static IEnumerable<string> FindMissingStates(AnimatorController controller)
    {
        var found = new HashSet<string>();
        foreach (AnimatorState state in CollectStates(controller)) found.Add(state.name);

        foreach (string name in SpeedDrivenStates)
        {
            if (!found.Contains(name)) yield return name;
        }
    }

    private static IEnumerable<AnimatorState> CollectStates(AnimatorController controller)
    {
        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            foreach (AnimatorState state in CollectStates(layer.stateMachine)) yield return state;
        }
    }

    private static IEnumerable<AnimatorState> CollectStates(AnimatorStateMachine machine)
    {
        if (machine == null) yield break;

        foreach (ChildAnimatorState child in machine.states) yield return child.state;

        foreach (ChildAnimatorStateMachine child in machine.stateMachines)
        {
            foreach (AnimatorState state in CollectStates(child.stateMachine)) yield return state;
        }
    }
}

/* [파일 노트]
 *
 * 금 보스 애니메이터(GoldBoss.controller)에 재생 속도 파라미터를 붙이는 일회성 도구다.
 * 메뉴 : Tools/Tup3/금 보스 애니메이터 동기화 설정
 *
 * 왜 필요한가
 * ─────────────────────────────────────────────────────────────
 * 금 보스의 공격 클립 길이는 코드의 패턴 지속시간과 전혀 맞지 않는다(측정값).
 *   Cut1     1.667초  ← 패턴1(1.0초) / 패턴4(3.0초) / 반격(1.0초) 세 상태가 공유
 *   Pattern2 3.000초  ← 패턴2(3.0초)
 *   Pattern3 5.083초  ← 패턴3(8.0초)
 * 클립은 아트 자산이라 손대지 않는 것이 원칙이므로, 상태의 재생 속도를 런타임에 조절해
 * "클립 재생 길이 = 패턴 지속시간" 이 항상 성립하도록 만든다. 속도는 Gold.cs 가 자기 타이밍
 * 수치로부터 직접 계산해 AnimSpeed 파라미터에 넣는다(코드가 단일 진실 공급원).
 *
 * 하는 일 (멱등)
 * ─────────────────────────────────────────────────────────────
 * 1) Float 파라미터 AnimSpeed(기본값 1) 를 없으면 추가한다.
 * 2) Cut1 / Cut2 / Pattern3 / Pattern4 / CounterAttack 상태의 Speed 를 1 로 두고
 *    Multiplier 를 AnimSpeed 파라미터에 연결한다(m_SpeedParameterActive).
 *    실제 재생 속도 = state.speed(1) * AnimSpeed 이므로 파라미터 값이 그대로 배속이 된다.
 * GoldIdle / Walk / Groggy / Dead 는 건드리지 않는다. 배속이 의미 없는 상태들이다.
 *
 * 이미 설정된 프로젝트에서 다시 실행하면 아무것도 바꾸지 않고 "변경 없음" 을 보고한다.
 * 실제로 바뀐 것이 있을 때만 에셋을 저장한다.
 *
 * 파라미터가 없어도 Gold.cs 는 죽지 않는다. Awake 에서 파라미터 존재 여부를 한 번 확인하고
 * 없으면 SetFloat 를 건너뛴다(트리거 타이밍 보정만 적용되고 배속 보정은 빠진다).
 * 즉 이 메뉴를 안 돌리면 "덜 고쳐진" 상태가 될 뿐 에러는 나지 않는다.
 */

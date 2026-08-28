using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class DialogueSpeakerPalette
{
    public readonly struct SpeakerColors
    {
        public readonly Color OnDark;
        public readonly Color OnLight;

        public SpeakerColors(Color onDark, Color onLight)
        {
            OnDark = onDark;
            OnLight = onLight;
        }
    }

    public static readonly Color FallbackOnDark = Rgb(143, 203, 180);
    public static readonly Color FallbackOnLight = Rgb(31, 99, 80);

    private static readonly Dictionary<string, SpeakerColors> Table = BuildTable();
    private static readonly HashSet<string> WarnedSpeakers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<KeyValuePair<string, SpeakerColors>> Defaults => Table;

    public static string Normalize(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;

        var sb = new StringBuilder(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (char.IsWhiteSpace(c)) continue;
            if (c == '"' || c == '\'') continue;
            sb.Append(c);
        }
        return sb.ToString();
    }

    public static bool TryGet(string speaker, out SpeakerColors colors)
    {
        colors = new SpeakerColors(FallbackOnDark, FallbackOnLight);

        string key = Normalize(speaker);
        if (key.Length == 0) return false;

        return Table.TryGetValue(key, out colors);
    }

    public static Color Resolve(string speaker, bool lightBackground)
    {
        if (TryGet(speaker, out SpeakerColors colors))
            return lightBackground ? colors.OnLight : colors.OnDark;

        WarnUnknownOnce(speaker);
        return Fallback(lightBackground);
    }

    public static Color Fallback(bool lightBackground)
    {
        return lightBackground ? FallbackOnLight : FallbackOnDark;
    }

    public static void WarnUnknownOnce(string speaker)
    {
#if UNITY_EDITOR
        string key = Normalize(speaker);
        if (key.Length == 0) key = "(빈 화자)";
        if (!WarnedSpeakers.Add(key)) return;

        Debug.LogWarning(
            $"[DialogueSpeakerPalette] 색이 지정되지 않은 화자 '{speaker}' — 기본색으로 표시합니다. " +
            "DialogueSpeakerPalette 의 기본표나 DialogueManager 의 speakerColorOverrides 에 추가하세요.");
#endif
    }

    private static Color Rgb(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    private static Dictionary<string, SpeakerColors> BuildTable()
    {
        var table = new Dictionary<string, SpeakerColors>(StringComparer.OrdinalIgnoreCase);

        Add(table, new SpeakerColors(Rgb(255, 255, 255), Rgb(20, 20, 20)),
            "주인공", "Player", "플레이어");

        Add(table, new SpeakerColors(Rgb(154, 160, 166), Rgb(100, 105, 110)),
            "나레이션", "Narration", "연출", "내레이션");

        Add(table, new SpeakerColors(Rgb(110, 122, 133), Rgb(59, 74, 87)),
            "시스템", "System", "전투");

        Add(table, new SpeakerColors(Rgb(190, 132, 73), Rgb(122, 74, 28)),
            "토보스", "토의 보스", "토");

        Add(table, new SpeakerColors(Rgb(220, 196, 162), Rgb(126, 100, 64)),
            "토의 정령", "흙의 정령", "대지의 정령");

        Add(table, new SpeakerColors(Rgb(79, 199, 238), Rgb(14, 92, 125)),
            "수보스", "수의 보스", "수");

        Add(table, new SpeakerColors(Rgb(255, 110, 74), Rgb(163, 44, 18)),
            "화보스", "화의 보스", "화");

        Add(table, new SpeakerColors(Rgb(247, 206, 75), Rgb(126, 94, 11)),
            "금보스", "금의 보스", "금");

        Add(table, new SpeakerColors(Rgb(181, 140, 230), Rgb(85, 48, 131)),
            "최종보스", "최종 보스", "FinalBoss");

        Add(table, new SpeakerColors(Rgb(205, 187, 232), Rgb(107, 78, 153)),
            "낯선 무언가", "낯선무언가");

        return table;
    }

    private static void Add(Dictionary<string, SpeakerColors> table, SpeakerColors colors, params string[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            string key = Normalize(keys[i]);
            if (key.Length == 0) continue;
            table[key] = colors;
        }
    }
}

/* [파일 노트 — 화자별 대사 색표]
 *
 * ── 역할 ─────────────────────────────────────────────────────────────────────
 *   CSV 의 speaker 칸(원문 문자열) → 대사 본문 색. 색을 정하는 곳은 이 파일 하나뿐이고,
 *   DialogueManager 는 여기서 색을 받아 TMP_Text.color 에 그대로 넣기만 한다.
 *   씬(.unity)에 저장된 값이 전혀 없어도 이 코드 기본표만으로 동작한다 —— 씬을 손대지 않고
 *   색을 바꾸려면 이 표를 고치면 되고, 씬/프리팹 단위로 예외를 두고 싶을 때만
 *   DialogueManager 인스펙터의 speakerColorOverrides 리스트에 항목을 추가한다(그쪽이 우선).
 *
 * ── 색 두 벌(OnDark / OnLight) ───────────────────────────────────────────────
 *   대사 패널 배경은 보통 불투명 검정이지만, 엔딩4 처럼 화면 전체가 하얀 연출에서는
 *   DialogueManager 가 패널을 밝게 바꾸므로 같은 화자라도 어두운 잉크 색이 필요하다.
 *   그래서 화자마다 (어두운 배경용 / 밝은 배경용) 두 색을 쌍으로 들고 있고,
 *   DialogueManager.SetLightBackground(bool) 이 어느 쪽을 쓸지 결정한다.
 *   문자열에 <color> 태그를 끼워 넣지 않는 이유 : 타이핑 연출이 maxVisibleCharacters 로
 *   글자 수를 세는 방식이라 리치텍스트 태그가 섞이면 글자 수 계산과 충돌한다.
 *
 * ── 색 선정 근거 (오행 컨셉) ─────────────────────────────────────────────────
 *   주인공        #FFFFFF  순백 — 유저 확정. 기준색이라 다른 누구도 흰색을 쓰지 않는다.
 *   나레이션      #9AA0A6  채도 없는 회색. 화자가 아니라 서술이므로 존재감을 낮춘다.
 *   시스템        #6E7A85  나레이션보다 어둡고 살짝 푸른 회색 — 같은 "서술" 계열이되 구분된다.
 *   토(土) 보스   #BE8449  황토/흙. 갈색기를 남겨 금(金)의 노랑과 갈라 놓는다.
 *   토의 정령     #DCC4A2  같은 흙 계열의 옅은 모래빛 — 보스보다 밝고 채도가 낮다.
 *   수(水) 보스   #4FC7EE  물빛 청록. 진한 남색은 검정 배경에서 죽어서 밝은 쪽으로 올렸다.
 *   화(火) 보스   #FF6E4A  불꽃의 주황빛 적색.
 *   금(金) 보스   #F7CE4B  금색/황동. 순백으로 잡으면 주인공과 구분되지 않으므로 노랑 쪽.
 *   최종보스      #B58CE6  죽음/공허의 차가운 보라. 오행 어디에도 속하지 않는 색이다.
 *   낯선 무언가   #CDBBE8  최종보스와 같은 보라 계열의 옅은 색. 로비의 "나는 너다"가
 *                          최종보스(타자화된 자기)와 같은 존재라는 복선을 색으로 잇는다.
 *   미지정(폴백)  #8FCBB4  위 어디에도 없는 옅은 청록. 주인공의 흰색과 확실히 다르고,
 *                          "표에 없는 화자"임을 개발 중에 눈으로 알아채기 쉽다.
 *
 * ── 배경 대비 (WCAG 상대 명도비) ─────────────────────────────────────────────
 *   대사 패널 배경 = 불투명 검정(#000000, DialogueUI.prefab 의 Image). 이 위에서
 *     주인공 21.0 / 나레이션 7.95 / 시스템 4.79 / 토보스 6.59 / 토의 정령 12.46 /
 *     수보스 10.74 / 화보스 7.58 / 금보스 13.88 / 최종보스 7.86 / 낯선 무언가 11.87 /
 *     폴백 11.37 —— 전부 WCAG AA(4.5:1) 이상이다.
 *   밝은 패널(엔딩4, 실효 #F5F5F5) 위의 OnLight 색은
 *     주인공 16.90 / 나레이션 5.09 / 시스템 8.36 / 토보스 6.82 / 토의 정령 5.09 /
 *     수보스 6.77 / 화보스 6.58 / 금보스 5.51 / 최종보스 9.01 / 낯선 무언가 6.07 /
 *     폴백 6.51 —— 역시 전부 4.5:1 이상.
 *
 * ── 키 처리 ──────────────────────────────────────────────────────────────────
 *   Normalize() 가 공백과 따옴표를 전부 제거하므로 "토의 정령" / "토의정령" / " 토의 정령 "
 *   이 같은 키로 취급된다. 사전은 OrdinalIgnoreCase 라 "Player" / "player" 도 같다.
 *   영어 별칭(Player/Narration/System/FinalBoss)은 옛 테스트 CSV 와 JSON 경로 대응용이다.
 *   "Boss" 는 일부러 넣지 않았다 —— 어느 보스인지 알 수 없는 더미 화자라 폴백색으로 보내
 *   에디터 경고가 뜨게 하는 편이 실수를 빨리 찾는 데 낫다.
 *
 * ── 경고 로그 ────────────────────────────────────────────────────────────────
 *   WarnUnknownOnce 는 UNITY_EDITOR 에서만, 같은 화자당 딱 한 번만 경고한다
 *   (WarnedSpeakers 해시셋). 매 줄마다 로그가 도배되는 일은 없다. 빌드에는 아예 없다.
 *   static 캐시라 플레이 모드를 다시 시작해도(도메인 리로드를 끈 경우) 남아 있을 수 있는데,
 *   경고는 "한 번은 봤다"는 뜻이므로 문제되지 않는다.
 */

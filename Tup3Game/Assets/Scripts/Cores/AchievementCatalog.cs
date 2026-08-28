using UnityEngine;

public sealed class AchievementInfo
{
    public string Id;
    public string Title;
    public string Description;
    public string Mark;
    public Color Tint;
}

public static class AchievementCatalog
{
    public const string ClearId = "Clear";

    public static readonly AchievementInfo Clear = new AchievementInfo
    {
        Id = ClearId,
        Title = "여정의 끝",
        Description = "엔딩을 하나라도 받아들이고 회차를 마쳤다.",
        Mark = "★",
        Tint = new Color(1f, 0.84f, 0.42f, 1f),
    };

    public static readonly AchievementInfo[] Endings =
    {
        new AchievementInfo
        {
            Id = "Ending2",
            Title = "엔딩 2 · 약한 의지",
            Description = "네 타자에게서 모두 힘을 빼앗은 채 최종보스를 넘어섰다. (성불 0회)",
            Mark = "II",
            Tint = new Color(0.66f, 0.62f, 0.76f, 1f),
        },
        new AchievementInfo
        {
            Id = "Ending3",
            Title = "엔딩 3 · 보통 의지",
            Description = "몇을 보내 주고 몇에게서는 빼앗은 채 최종보스를 넘어섰다. (성불 1~3회)",
            Mark = "III",
            Tint = new Color(0.74f, 0.80f, 0.88f, 1f),
        },
        new AchievementInfo
        {
            Id = "Ending4",
            Title = "엔딩 4 · 강한 의지",
            Description = "아무것도 빼앗지 않고 넷 모두를 보내 준 뒤 최종보스를 넘어섰다. (성불 4회)",
            Mark = "IV",
            Tint = new Color(1f, 0.84f, 0.42f, 1f),
        },
    };

    public static AchievementInfo Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (id == ClearId) return Clear;

        for (int i = 0; i < Endings.Length; i++)
            if (Endings[i].Id == id) return Endings[i];

        return null;
    }

    public static AchievementData Data
    {
        get
        {
            UserDataManager manager = UserDataManager.Instance;
            if (manager == null) return null;

            UserData data = manager.Data;
            return data != null ? data.Achievements : null;
        }
    }

    public static bool IsUnlocked(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        AchievementData achievements = Data;
        return achievements != null && achievements.IsUnlocked(id);
    }

    public static bool IsUnlocked(AchievementInfo info)
    {
        return info != null && IsUnlocked(info.Id);
    }

    public static int UnlockedEndingCount()
    {
        int count = 0;
        for (int i = 0; i < Endings.Length; i++)
            if (IsUnlocked(Endings[i].Id)) count++;

        return count;
    }
}

/* [파일 노트]
 *
 * 도전과제(업적)의 "무엇이 있는가" 목록. 해금 상태 자체는 UserData 의 AchievementData
 * (Dictionary<string,bool>)가 들고 있고, 이 파일은 그 id 에 붙는 표시용 정보 —
 * 제목·설명·배지 글자(Mark)·배지 색(Tint) — 만 담는다. 데이터 구조를 건드리지 않으므로
 * 기존 세이브와 100% 호환된다.
 *
 * ── 왜 만들었나 ──────────────────────────────────────────────────────────────
 *   업적 id 는 Ending.cs 가 "Clear" 와 endingId("Ending2"~"Ending4")를 문자열로 해금할 뿐
 *   어디에도 "이 id 가 무엇을 뜻하는지"가 없었다. 도전과제를 화면에 보여 주려면
 *   id → 사람이 읽는 이름이 필요하고, 그 대응표를 UI 파일마다 복붙하지 않기 위해 한곳에 모았다.
 *   AchievementsPanelView(목록)와 EndingBadgeView(시작 화면 배지) 둘 다 이 배열 하나를 읽는다.
 *   즉 엔딩을 추가/수정할 때 고칠 곳은 이 파일 하나다.
 *
 * ── 엔딩 1 이 없는 이유 ──────────────────────────────────────────────────────
 *   엔딩1(패배·배드)은 게임에서 제거되어 도달할 수 없다(2026-08-29 유저 확정).
 *   그래서 Endings 배열은 2·3·4 세 개뿐이다. 되살릴 일이 생기면 이 배열에 항목을 하나
 *   추가하기만 하면 목록과 배지 양쪽에 자동으로 나타난다(양쪽 다 Length 로 도는 코드다).
 *
 * ── 해금 조건 (Ending.cs 와 FinalBossRoom.cs 의 기존 사양을 옮겨 적은 것) ────
 *   Ending2 : 의지 코인 0 + 최종보스 승리 (성불 0회 / 약한 의지)
 *   Ending3 : 의지 코인 1~3 + 최종보스 승리 (성불 1~3회 / 보통 의지)
 *   Ending4 : 의지 코인 4 이상 + 최종보스 승리 (성불 4회 / 강한 의지)
 *   의지 코인 == 성불 횟수다 — 시작 4개에서 극(剋) 승리마다 1개씩 빠지고 생(生) 승리는 증감이 없다.
 *   2026-08-29 이전 서술(코인 4 = 트루 = Ending2)은 FinalBossRoom 분기가 뒤집혀 있던 시절의
 *   낡은 정보였다. 지금은 Ending4 가 아무것도 빼앗지 않은 쪽이다.
 *   세 엔딩 모두 "엔딩 화면에서 '시작 화면으로'를 골랐을 때만" 해금된다.
 *   '마지막 체크포인트로 돌아가기'를 고르면 해금되지 않는다(Ending.cs 파일 노트 참조).
 *   Clear 는 그 순간 함께 해금되는 공통 업적이라 Endings 배열에는 넣지 않고 따로 두었다.
 *
 * ── Mark / Tint ─────────────────────────────────────────────────────────────
 *   전용 아이콘 스프라이트가 프로젝트에 없을 때 배지가 그리는 폴백용 값이다.
 *   Mark 는 배지 안에 찍는 짧은 글자(로마숫자), Tint 는 그 배지의 바탕색.
 *   로마숫자를 Ⅱ/Ⅲ/Ⅳ(전각 단일 문자)가 아니라 II/III/IV(라틴 대문자)로 쓴 이유는
 *   전각 로마숫자 글리프가 없는 폰트에서 □ 로 깨질 수 있기 때문이다.
 *   EndingBadgeView 에 스프라이트를 꽂으면 Tint 대신 그 스프라이트가 쓰이고 Mark 는 숨는다.
 *
 * ── Data 프로퍼티의 null 안전 ────────────────────────────────────────────────
 *   UserDataManager.Instance 는 없으면 GameObject 를 만들어서라도 돌려주지만, 그 시점의
 *   Data 는 LoadAsync 전이라 null 일 수 있다. 그래서 매 단계 null 을 확인하고
 *   모르면 "잠김"으로 답한다 — 업적 화면이 예외로 죽는 것보다 낫다.
 */

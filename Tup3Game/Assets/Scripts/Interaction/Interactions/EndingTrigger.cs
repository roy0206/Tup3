using UnityEngine;

public class EndingTrigger : InteractionBase
{
    [Header("임시 엔딩 분기 (최종보스 구현 시 교체)")]
    [SerializeField] private string badEndingId = "Ending1";
    [SerializeField] private string trueEndingId = "Ending2";

    protected override bool CanInteract()
    {
        return true;
    }

    public override bool OnInteract()
    {
        if (base.OnInteract())
        {
            SetEndingIdByWillCoins();
            SceneController.Instance.LoadScene("Ending");
            return true;
        }

        return false;
    }

    private void SetEndingIdByWillCoins()
    {
        var data = UserDataManager.Instance.Data;
        if (data == null || data.Play == null) return;

        data.Play.endingId = data.Play.willCoins <= 0 ? badEndingId : trueEndingId;
        UserDataManager.Instance.SaveAsync();
        Debug.Log($"[EndingTrigger] (임시) 의지 코인 {data.Play.willCoins}개 → endingId = {data.Play.endingId}");
    }
}

/* [파일 노트]
 * ※ 임시 로직 — 최종보스가 구현되면 교체된다.
 *   기획상 endingId 는 최종보스전 결과가 세팅해야 한다:
 *     승리 → 트루엔딩("Ending2"), 패배 시 willCoins > 0 이면 -1 후 재도전, willCoins == 0 이면 배드엔딩("Ending1").
 *   최종보스가 없는 현재는 Styx 의 이 트리거가 상호작용 시점에 willCoins 만 보고 세팅한다:
 *     willCoins == 0 → 배드엔딩, 그 외 → 트루엔딩.
 *   최종보스 구현 시 SetEndingIdByWillCoins() 호출을 제거하고(또는 이 트리거 자체를 보스전 진입으로 바꾸고),
 *   보스전 결과 처리 쪽에서 UserDataManager.Instance.Data.Play.endingId 를 세팅한 뒤 "Ending" 씬을 로드하면 된다.
 *   Ending.cs 는 endingId("Ending1"~"Ending4")를 판독만 한다.
 *
 * ※ 최종보스 구현으로 폐기 예정(씬에서 제거).
 *   FinalBossRoom.cs 가 승패에 따라 endingId(Ending1~4)를 직접 세팅하고 Ending 씬을 로드하므로
 *   이 트리거는 더 이상 필요 없다. Styx 씬에서 EndingTrigger 오브젝트를 제거할 것.
 */

using UnityEngine;

public class BossExit : InteractionBase
{
    [Header("등장 조건")]
    [SerializeField] private BossBase boss;
    [SerializeField] private bool hideUntilBossDefeated = true;

    private SpriteRenderer[] visuals;
    private bool revealed;
    private bool subscribed;

    protected override void Start()
    {
        base.Start();

        visuals = GetComponentsInChildren<SpriteRenderer>(true);

        if (!hideUntilBossDefeated)
        {
            revealed = true;
            return;
        }

        if (boss == null) boss = FindObjectOfType<BossBase>();

        if (boss == null)
        {
            Debug.LogWarning($"[BossExit] 보스를 찾지 못했습니다. 출구를 바로 노출합니다 — {name}");
            revealed = true;
            return;
        }

        if (boss.IsDead)
        {
            revealed = true;
            return;
        }

        Hide();

        boss.OnDeath += Reveal;
        subscribed = true;
    }

    private void LateUpdate()
    {
        if (revealed) return;
        if (boss == null || !boss.IsDead) return;

        Reveal();
    }

    private void OnDestroy()
    {
        if (!subscribed || boss == null) return;

        boss.OnDeath -= Reveal;
        subscribed = false;
    }

    private void Hide()
    {
        revealed = false;
        SetVisualsEnabled(false);
        if (InteractionManager.Current != null) InteractionManager.Current.Unregister(this);
    }

    private void Reveal()
    {
        if (revealed) return;

        revealed = true;
        SetVisualsEnabled(true);
        if (InteractionManager.Current != null) InteractionManager.Current.Register(this);
    }

    private void SetVisualsEnabled(bool value)
    {
        if (visuals == null) return;

        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] == null) continue;
            visuals[i].enabled = value;
        }
    }

    protected override bool CanInteract()
    {
        return revealed;
    }

    public override bool OnInteract()
    {
        if (base.OnInteract())
        {
            SceneController.Instance.LoadScene("Lobby");
            return true;
        }

        return false;
    }
}

/* [파일 노트]
 *
 * 보스방 출구. 상호작용하면 로비로 돌아간다.
 *
 * ── 보스를 잡아야만 등장 (2026-08-28 유저 확정) ──────────────────────────────
 * hideUntilBossDefeated(기본 켜짐) 이면 씬 진입 시 출구를 숨긴다.
 *   - 스프라이트: 자식까지 포함해 SpriteRenderer.enabled 를 끈다. 오브젝트 자체를 비활성화하지 않는 이유는
 *     이 컴포넌트의 Start/LateUpdate 가 계속 돌아야 보스 사망을 감지할 수 있기 때문이다.
 *   - 상호작용: InteractionManager 에서 Unregister 한다. CanInteract 만 false 로 두면 보이지도 않는
 *     출구에 상호작용 프롬프트가 떠 버리므로 등록 자체를 빼는 편이 맞다.
 * 보스가 죽으면 다시 Register 하고 스프라이트를 켠다.
 *
 * 감지는 이중이다 — BossBase.OnDeath 이벤트 구독 + LateUpdate 에서 IsDead 폴링.
 * BossRoom 도 같은 이유로 이벤트와 폴링을 병행한다(이벤트가 누락되는 경우가 있어서).
 * 폴링이 있으므로 이벤트가 안 와도 출구가 영영 안 뜨는 일은 없다.
 *
 * 보스 참조를 비워 두면 씬에서 BossBase 를 찾는다. 그래도 못 찾으면 경고를 남기고 바로 노출한다
 * (보스가 없는 씬에 잘못 놓였을 때 플레이어가 갇히지 않게 하는 안전장치).
 * 클리어 여부가 아니라 "보스 사망"을 기준으로 삼는 이유: BossRoom 의 Clear 상태는 승리 대사가
 * 끝나야 오는데, 대사 필드가 비어 있는 보스 씬에서는 그 경로를 타지 않을 수 있다.
 *
 * ── InteractionBase.Start 가 protected virtual 인 이유 ───────────────────────
 * 원래 private 이었다. 파생 클래스에서 Start 를 선언하면 베이스의 Start 가 가려져
 * InteractionManager 등록과 view 참조가 통째로 날아간다. 이 클래스가 Start 를 써야 해서
 * protected virtual 로 열고 base.Start() 를 먼저 호출하도록 했다.
 * 다른 파생(BossEnterence / StyxEnterence)은 Awake 를 쓰고 있어 영향이 없다.
 */

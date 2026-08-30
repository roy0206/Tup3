using UnityEngine;

public class StyxIntroInteraction : InteractionBase
{
    [SerializeField] private StyxIntro director;

    protected override void Start()
    {
        base.Start();
        if (director == null) director = FindObjectOfType<StyxIntro>();
        if (director == null)
            Debug.LogError("[StyxIntroInteraction] 씬에 StyxIntro 가 없어 조우 대사를 시작할 수 없습니다", this);
    }

    protected override bool CanInteract()
    {
        return director != null && !director.DialogueStarted;
    }

    public override bool OnInteract()
    {
        if (base.OnInteract())
        {
            director.BeginDialogue();
            if (InteractionManager.Current != null) InteractionManager.Current.Unregister(this);
            return true;
        }

        return false;
    }
}

/* [파일 노트]
 * StyxIntro 씬에서 최종보스에게 붙는 상호작용. 플레이어가 보스 근처에서 V 를 홀드하면
 * StyxIntro.BeginDialogue() 로 조우 대사(S03_LOBBY)를 시작한다.
 * 성공 즉시 InteractionManager 에서 자기를 빼서 대사 중/후에 다시 발동하지 않는다
 * (CanInteract 의 DialogueStarted 검사는 이중 안전장치).
 * 배선은 StyxIntroSceneBuilder 가 한다 — 보스(BossBase) 오브젝트에 이 컴포넌트를 붙이고
 * director 참조·거리·홀드 시간을 채운다. director 가 비어 있으면 Start 에서 씬 탐색으로 보충한다.
 */

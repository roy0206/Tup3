public interface ILobbyIntroStep
{
    int StepOrder { get; }

    void OnIntroDisabled();
}

/* [파일 노트]
 * 로비 도입부 대사 트리거가 공통으로 구현하는 인터페이스.
 * - StepOrder: 씬에 배치된 트리거의 순번. LobbyIntroDirector 가 이 값으로 정렬해서 발동 순서를 정한다.
 * - OnIntroDisabled(): 도입부를 재생하지 않거나(이어하기) 도입부가 끝났을 때 Director 가 호출한다.
 *   구현체는 여기서 자기 자신을 비활성화해서 다시는 발동되지 않게 한다.
 * MonoBehaviour 를 상속한 클래스와 InteractionBase 를 상속한 클래스가 같은 규약을 공유해야 하므로
 * 공통 부모 클래스가 아니라 인터페이스로 만들었다.
 */

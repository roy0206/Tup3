using UnityEngine;

public class DummyLogue : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogue;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            dialogue.StartDialogueFromCsv("boss_intro");   // 확장자 빼고 파일 이름만
        }
    }
}

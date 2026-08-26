using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSequencePlayer : MonoBehaviour
{
    [SerializeField] private List<Sprite> sprites = new List<Sprite>();
    [SerializeField] private float frameRate = 12f;
    [SerializeField] private bool loop;
    [SerializeField] private bool deactivateOnComplete;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (sprites.Count > 0) StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        var interval = new WaitForSeconds(1f / frameRate);
        do
        {
            foreach (var sprite in sprites)
            {
                spriteRenderer.sprite = sprite;
                yield return interval;
            }
        } while (loop);

        if (deactivateOnComplete) gameObject.SetActive(false);
    }
}

/* [파일 노트]
 * SpriteRenderer 의 스프라이트를 순서대로 교체하는 커스텀 시퀀스 재생기. Animator 없이 이펙트를 재생할 때 쓴다.
 * OnEnable 에서 자동 재생되므로 SetActive(true) 만으로 발동한다 (금보스 패턴4 참격/섬광 이펙트 등).
 * deactivateOnComplete 를 켜면 1회 재생 후 스스로 SetActive(false) 되어, 켜는 쪽에서 끄는 타이밍을 맞출 필요가 없다.
 * loop 와 deactivateOnComplete 를 동시에 켜면 루프가 끝나지 않으므로 deactivateOnComplete 는 무시되는 셈이 된다.
 */

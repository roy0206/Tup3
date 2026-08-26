using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Image))]
public class ImageSequencePlayer : MonoBehaviour
{
    [SerializeField] private List<Sprite> sprites = new List<Sprite>();
    [SerializeField] private float frameRate = 12f;
    [SerializeField] private bool loop;
    [SerializeField] private bool deactivateOnComplete;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
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
                image.sprite = sprite;
                yield return interval;
            }
        } while (loop);

        if (deactivateOnComplete) gameObject.SetActive(false);
    }
}

/* [파일 노트]
 * SpriteSequencePlayer 의 UI 판. SpriteRenderer 대신 UnityEngine.UI.Image 의 sprite 를 순서대로 교체한다.
 * 화면 전체를 덮어야 해서 Screen Space Overlay 캔버스에 올려야 하는 이펙트(금보스 패턴4 참격 등)는
 * 월드 스프라이트가 아니라 Image 라 이 컴포넌트를 쓴다. 구조·필드명·동작은 SpriteSequencePlayer 와 동일하다.
 * OnEnable 에서 자동 재생되므로 SetActive(true) 만으로 발동한다.
 * deactivateOnComplete 를 켜면 1회 재생 후 스스로 SetActive(false) 된다.
 * 단, 오브젝트 풀에서 꺼내 쓰는 프리팹의 "자식"에 붙일 때는 이 옵션을 끄고 반납 쪽(PoolManager.Release)에
 * 맡길 것. 자식이 스스로 비활성화되면 풀이 루트를 다시 켜도 그 자식은 꺼진 채로 남아 다음 재생이 안 보인다.
 * loop 와 deactivateOnComplete 를 동시에 켜면 루프가 끝나지 않으므로 deactivateOnComplete 는 무시된다.
 */

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

    public void SetSequence(IList<Sprite> frames, float fps, bool loopSequence, bool deactivateWhenComplete)
    {
        sprites.Clear();
        if (frames != null)
        {
            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i] != null) sprites.Add(frames[i]);
            }
        }

        frameRate = Mathf.Max(0.01f, fps);
        loop = loopSequence;
        deactivateOnComplete = deactivateWhenComplete;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (sprites.Count > 0) spriteRenderer.sprite = sprites[0];
    }

    public void Restart()
    {
        if (!isActiveAndEnabled) return;

        StopAllCoroutines();
        if (sprites.Count > 0) StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        float interval = 1f / Mathf.Max(0.01f, frameRate);
        do
        {
            foreach (var sprite in sprites)
            {
                spriteRenderer.sprite = sprite;
                yield return WaitFrame(interval);
            }
        } while (loop);

        if (deactivateOnComplete) gameObject.SetActive(false);
    }

    private IEnumerator WaitFrame(float interval)
    {
        float elapsed = 0f;
        while (elapsed < interval)
        {
            if (!PauseManager.IsPaused) elapsed += Time.deltaTime;
            yield return null;
        }
    }
}

/* [파일 노트]
 * SpriteRenderer 의 스프라이트를 순서대로 교체하는 커스텀 시퀀스 재생기. Animator 없이 이펙트를 재생할 때 쓴다.
 * OnEnable 에서 자동 재생되므로 SetActive(true) 만으로 발동한다 (금보스 패턴4 참격/섬광 이펙트 등).
 * deactivateOnComplete 를 켜면 1회 재생 후 스스로 SetActive(false) 되어, 켜는 쪽에서 끄는 타이밍을 맞출 필요가 없다.
 * loop 와 deactivateOnComplete 를 동시에 켜면 루프가 끝나지 않으므로 deactivateOnComplete 는 무시되는 셈이 된다.
 *
 * 코드 생성 대응 (플레이어 스킬 이펙트에서 추가)
 *  - SetSequence(frames, fps, loop, deactivateOnComplete) : 인스펙터 없이 런타임에 시퀀스를 주입한다.
 *    AddComponent 직후에는 OnEnable 이 빈 리스트로 한 번 지나가므로(재생 없음), 이 함수로 프레임을
 *    넣은 뒤 Restart() 를 부르거나 오브젝트를 SetActive(false)→(true) 로 토글해 재생을 시작한다.
 *    첫 프레임을 즉시 반영해 두어 재생 전 한 프레임 동안 빈 스프라이트가 보이지 않는다.
 *  - Restart() : 진행 중인 재생을 끊고 1프레임부터 다시 튼다(활성 상태에서만 동작).
 *  - 프레임 대기를 WaitForSeconds 에서 PauseManager.IsPaused 게이트가 걸린 수동 타이머로 바꿨다.
 *    이 프로젝트는 Time.timeScale 을 건드리지 않으므로 WaitForSeconds 는 일시정지 중에도 흘러
 *    정지 중 이펙트만 혼자 진행되는 문제가 있었다. 기존 인스펙터 설정 동작(OnEnable 자동 재생,
 *    loop, deactivateOnComplete)은 그대로다.
 */

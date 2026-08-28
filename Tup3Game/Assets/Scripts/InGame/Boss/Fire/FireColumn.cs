using System;
using UnityEngine;
using DG.Tweening;

public class FireColumn : MonoBehaviour
{

    SpriteRenderer spriteRenderer;
    BoxCollider2D boxCollider2D;

    [Header("사운드")]
    [SerializeField] private float columnVolume = 0.7f;
    [SerializeField] private float columnMinInterval = 0.25f;

    private const string ColumnSound = "Fire_Column";

    void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        boxCollider2D.enabled = true;
        BossSound.PlayThrottled(ColumnSound, columnVolume, columnMinInterval);
        transform.DOMoveY(1.5f, 0.1f).OnComplete(() =>
        {
            transform.DOMoveY(-5f, 0.1f);
            boxCollider2D.enabled = false;
        });
    }

    private void Update()
    {
        if (PauseManager.IsPaused) return;

        spriteRenderer.flipX = !spriteRenderer.flipX;
    }
}

/* [파일 노트]
 * 사운드 Fire_Column : 불기둥이 풀에서 꺼내져 솟는 순간(OnEnable) 재생한다.
 * 화보스 패턴1은 2초 동안 16개를 0.125초 간격으로 뿌리므로 그대로 두면 초당 8회가 울린다.
 * columnMinInterval(기본 0.25초) 스로틀을 걸어 훑고 지나가는 화염벽이 네댓 번의 분출음으로
 * 들리게 했다. 값을 0 으로 두면 모든 기둥이 각각 소리를 낸다.
 */

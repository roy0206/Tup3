using System;
using UnityEngine;

public class SoilDrop : MonoBehaviour
{
    [SerializeField] private float accel;
    private float speed = 0;

    [Header("사운드")]
    [SerializeField] private float rockFallVolume = 0.7f;
    [SerializeField] private float rockFallMinInterval = 0.15f;

    private const string RockFallSound = "Soil_RockFall";

    private void OnEnable()
    {
        speed = 0;
        BossSound.PlayThrottled(RockFallSound, rockFallVolume, rockFallMinInterval);
    }

    private void Update()
    {
        if (PauseManager.IsPaused) return;

        speed += accel * Time.deltaTime;
        transform.Translate(Vector3.down * speed * Time.deltaTime);
    }
}

/* [파일 노트]
 * 사운드 Soil_RockFall : 낙석이 풀에서 꺼내져 활성화되는 순간(OnEnable) 1회 재생한다.
 * 소환 측(Soil.SoilDrop 코루틴)이 아니라 여기에 둔 이유는 소환 경로가 늘어나도 자동으로 따라오고,
 * 풀 재사용마다 정확히 한 번씩 울리기 때문이다.
 * rockFallMinInterval(기본 0.15초)로 겹침을 막는다. 현재 소환 간격은 0.4초라 평상시에는 전부
 * 재생되지만, 소환 간격을 줄이거나 다른 패턴이 동시에 낙석을 뿌려도 소리가 뭉치지 않는다.
 */

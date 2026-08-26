using UnityEngine;

public class SkillGroundMarker : MonoBehaviour
{
}

/* [파일 노트]
 * 플레이어 스킬2(지형생성)로 소환된 지형을 식별하는 빈 마커 컴포넌트.
 * Skills.SpawnGroundAfterDelay 가 지형 Instantiate 직후 AddComponent 로 붙인다.
 * 최종보스의 토 파동 투사체(SoilWave)가 이 마커를 감지하면 즉시 소멸한다(속성 상성).
 * 로직이 전혀 없으므로 프리팹 수정 없이 코드만으로 식별이 성립한다.
 */

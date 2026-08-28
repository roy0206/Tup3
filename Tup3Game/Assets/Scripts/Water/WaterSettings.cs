using UnityEngine;

namespace SimWater
{
[CreateAssetMenu(fileName = "WaterSettings", menuName = "ScriptableObjects/WaterSettings")]
public class WaterSettings : ScriptableObject
{
    static WaterSettings _currentSettings;
    public static WaterSettings currentSettings =>
        _currentSettings ??= Resources.Load<WaterSettings>("WaterSettings");
    
    [Header("Physics")]
    public float tension;
    public float damping;
    public float spread;
    public int iterationsPerFrame = 1;
    
    [Header("Collision")]
    public float surfaceCollisionDistance;
    public float collisionVelocityTransfer;
    public float collisionBlendSpeed = 30f;
    public float maxNodeSpeed = 6f;

    [Header("Optimization")]
    public float simulationDistance;
    public float nodePerUnit;
}
}

/* [파일 노트]
 * Tavern_Gamejam_CAU_SSU 프로젝트(Assets/Scripts/Water/WaterSettings.cs)에서 이식한 물 시뮬 설정 SO.
 * Resources/WaterSettings.asset 을 Resources.Load 로 읽는다(에셋도 함께 이식).
 * 수정 사항: namespace SimWater 로 감쌌다(내용 동일). 이식한 WaterSettings.asset 의
 * m_EditorClassIdentifier 도 SimWater.WaterSettings 로 맞춰 수정했다.
 */
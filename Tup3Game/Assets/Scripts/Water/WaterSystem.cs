using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.LowLevel;

namespace SimWater
{
public class WaterSystem
{
    static WaterSystem _instance;
    protected static WaterSystem instance => _instance ??= new WaterSystem();
    
    List<Water> _waters = new();
    List<WaterBody> _bodies = new();
    WaterNodeMappingData _waterNodeMappingData = new();
    SimulationData _simulationData = new();
    
    WaterSettings settings => WaterSettings.currentSettings;
    public static Vector2 simulationCenter
    {
        get
        {
            var cam = Camera.main;
            return cam != null ? (Vector2)cam.transform.position : Vector2.zero;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RuntimeInitializeOnLoad()
    {
        _instance = new WaterSystem();
        PlayerLoopSystem system = new PlayerLoopSystem
        {
            type = typeof(UpdateWaterSystem),
            updateDelegate = () => instance.Update()
        };
        system.RegisterTo<UnityEngine.PlayerLoop.PostLateUpdate>();
        Application.quitting += () => instance._simulationData.Dispose();
    }
    void Register_Internal(Water water) => _waters.Add(water);
    void Deregister_Internal(Water water) => _waters.Remove(water);

    void Update()
    {
        if (PauseManager.IsPaused) return;

        UpdatePhysics();
        
        /*foreach(var water in _simulationData.waters)
        {
            var nodeRange = _simulationData.mappingData.GetNodeRange(water);
            var waterRange = _simulationData.mappingData.GetWaterRange(water);
            for (int i = nodeRange.start; i < nodeRange.end; i++)
            {
                int waterIndex = i - nodeRange.start + waterRange.start;
                float x = water.IndexToX(waterIndex);
                float y = _simulationData.positions[i] + water.bounds.yMax;
                Debug.DrawLine(new Vector3(x,y-0.1f,0),new Vector3(x,y+0.1f,0));
            }
        }*/
    }

    bool GetPositions_Internal(Water water, out NativeArray<float> positions, out RangeInt waterRange)
    {
        (positions, waterRange) = (default, default);
        if (!_simulationData.Contains(water)) return false;
        waterRange = _simulationData.mappingData.GetWaterRange(water);
        positions = _simulationData.GetPositions(water);
        return true;
    }

    void Collide_Internal(Water water, WaterBody body)
    {
        if (!_simulationData.Contains(water)) return;
        var positions = _simulationData.GetPositions(water);
        var velocities = _simulationData.GetVelocities(water);
        var waterRange = _simulationData.mappingData.GetWaterRange(water);
        var intersect = waterRange.Intersect(water.InnerIndexRange(body.bounds.min.x, body.bounds.max.x));
        for (int i = intersect.start; i < intersect.end; i++)
        {
            int waterIndex = i - waterRange.start;
            float py = positions[waterIndex];
            float v = velocities[waterIndex];
            Vector2 point = new Vector2(water.IndexToX(i), py + water.bounds.yMax);
            if (Mathf.Abs(py) < settings.surfaceCollisionDistance && body.collider.OverlapPoint(point))
            {
                if (Mathf.Abs(body.velocityY) > Mathf.Abs(v) || body.velocityY * v < 0)
                {
                    float target = body.velocityY * settings.collisionVelocityTransfer
                        * (1f - Mathf.Abs(py) / settings.surfaceCollisionDistance);
                    float blended = Mathf.MoveTowards(v, target, settings.collisionBlendSpeed * Time.deltaTime);
                    velocities[waterIndex] = Mathf.Clamp(blended, -settings.maxNodeSpeed, settings.maxNodeSpeed);
                }
            }
        }
    }

    void UpdatePhysics()
    {
        _waterNodeMappingData.Clear();
        foreach (var water in _waters) _waterNodeMappingData.Add(water, settings, simulationCenter);
        _simulationData.Update(_waterNodeMappingData);
        
        foreach (var water in _simulationData.waters)
        {
            var velocities = _simulationData.GetVelocities(water);
            var positions = _simulationData.GetPositions(water);
            for (int i = 0; i< settings.iterationsPerFrame; i++) {
                var job = new WaterSimulationJobs.VelocityJob(settings, velocities, positions, Time.deltaTime/settings.iterationsPerFrame);
                var handle = job.Schedule(job.velocities.Length, 8);
                handle.Complete();
                var job2 = new WaterSimulationJobs.PositionJob(velocities, positions, Time.deltaTime/settings.iterationsPerFrame);
                var handle2 = job2.Schedule(job2.velocities.Length, 8);
                handle2.Complete();
            }
        }
    }

    public static void Register(Water water) => instance.Register_Internal(water);    
    public static void Deregister(Water water) => instance.Deregister_Internal(water);
    public static void Collide(Water water, WaterBody body) => instance.Collide_Internal(water, body);
    public static bool GetPositions(Water water, out NativeArray<float> positions, out RangeInt range) 
        => instance.GetPositions_Internal(water, out positions, out range);

    class WaterNodeMappingData
    {
        public int totalNodeCount;
        public List<Water> waters = new();
        public List<RangeInt> nodeRanges = new();
        public List<RangeInt> waterRanges = new();

        public void Add(Water water, WaterSettings settings, Vector2 simulationCenter)
        {
            float r = settings.simulationDistance;
            float nodePerUnit = settings.nodePerUnit;
            if (!water.simulatable) return;

            float y = water.bounds.yMax - simulationCenter.y;
            float sqrt = Mathf.Sqrt(r * r - y * y);
            if (float.IsNaN(sqrt)) return;
            RangeInt range = water.OuterIndexRange(simulationCenter.x - sqrt, simulationCenter.x + sqrt);

            waters.Add(water);
            nodeRanges.Add(new RangeInt(totalNodeCount, range.length));
            waterRanges.Add(range);
            totalNodeCount += range.length;
        }

        public RangeInt GetNodeRange(Water water) => nodeRanges[waters.IndexOf(water)];
        public RangeInt GetWaterRange(Water water) => waterRanges[waters.IndexOf(water)];

        public void CopyFrom(WaterNodeMappingData other)
        {
            totalNodeCount = other.totalNodeCount;
            waters.Clear();
            nodeRanges.Clear();
            waterRanges.Clear();
            waters.AddRange(other.waters);
            nodeRanges.AddRange(other.nodeRanges);
            waterRanges.AddRange(other.waterRanges);
        }

        public void Clear()
        {
            totalNodeCount = 0;
            waters.Clear();
            nodeRanges.Clear();
            waterRanges.Clear();
        }
    }
    class SimulationData : IDisposable
    {
        public NativeArray<float> velocities;
        public NativeArray<float> positions;
        public WaterNodeMappingData mappingData = new();
        public List<Water> waters => mappingData.waters;
        public void Update(WaterNodeMappingData newMappingData)
        {
            NativeArray<float> newVelocities = new (newMappingData.totalNodeCount, Allocator.Persistent);
            NativeArray<float> newPositions = new (newMappingData.totalNodeCount, Allocator.Persistent);

            foreach (var water in newMappingData.waters)
            {
                if (!mappingData.waters.Contains(water)) continue;
                RangeInt nodeRange = mappingData.GetNodeRange(water);
                RangeInt waterRange = mappingData.GetWaterRange(water);
                RangeInt newNodeRange = newMappingData.GetNodeRange(water);
                RangeInt newWaterRange = newMappingData.GetWaterRange(water);
                RangeInt waterIntersect = waterRange.Intersect(newWaterRange);
                if (waterIntersect.length == 0) continue;

                NativeArray<float> source =
                    velocities.GetSubArray(nodeRange.start + waterIntersect.start - waterRange.start, waterIntersect.length);
                NativeArray<float> target =
                    newVelocities.GetSubArray(newNodeRange.start + waterIntersect.start - newWaterRange.start, waterIntersect.length);
                target.CopyFrom(source);
                
                source = positions.GetSubArray(nodeRange.start + waterIntersect.start - waterRange.start, waterIntersect.length);
                target = newPositions.GetSubArray(newNodeRange.start + waterIntersect.start - newWaterRange.start, waterIntersect.length);
                target.CopyFrom(source);
            }

            mappingData.CopyFrom(newMappingData);
            if (velocities.IsCreated) velocities.Dispose();
            if (positions.IsCreated) positions.Dispose();
            velocities = newVelocities;
            positions = newPositions;
        }
        
        public NativeArray<float> GetVelocities(Water water) => velocities.GetSubArray( mappingData.GetNodeRange(water));
        public NativeArray<float> GetPositions(Water water) => positions.GetSubArray( mappingData.GetNodeRange(water));
        
        public bool Contains(Water water) => mappingData.waters.Contains(water);
        public void Dispose()
        {
            if (velocities.IsCreated) velocities.Dispose();
            if (positions.IsCreated) positions.Dispose();
        }
    }
    struct UpdateWaterSystem
    {
    }
}
}

/* [파일 노트]
 * Tavern_Gamejam_CAU_SSU 프로젝트(Assets/Scripts/Water/WaterSystem.cs)에서 이식한 물 시뮬 코어 —
 * PlayerLoop(PostLateUpdate)에 커스텀 업데이트를 등록하고, 카메라 주변(simulationDistance)의
 * 물 노드만 NativeArray 로 관리하며 Burst Job 으로 파동을 적분한다.
 * 수정 사항:
 * - namespace SimWater 로 감쌌다(클래스명 충돌 방지).
 * - 사용되지 않던 using Unity.VisualScripting 제거.
 * - Collide_Internal 의 body.rigidbody.linearVelocityY → body.velocityY (WaterBody 가
 *   Rigidbody2D 없는 트랜스폼 구동 오브젝트도 지원하도록 변경된 것에 대응).
 * - simulationCenter 에 Camera.main null 가드 추가 — 이 업데이트는 PlayerLoop 에 등록돼 모든 씬에서
 *   돌므로, MainCamera 태그 카메라가 없는 씬에서 매 프레임 NRE 가 나는 것을 방지.
 
 *
 * Tup3 추가 수정 (물리 사실감 개선):
 * - Collide: 원본은 노드 속도를 몸 속도로 '대입'해 몸이 수면에 겹친 동안 수면이 몸에 고정되고
 *   떠나는 순간 튕겼다. MoveTowards(collisionBlendSpeed) 블렌드 + maxNodeSpeed 클램프로
 *   임펄스형 반응이 되게 바꿈.
 * - Update: PauseManager.IsPaused 동안 시뮬 정지 (PlayerLoop 직등록이라 일시정지를 몰랐음).
 * - 파라미터는 Resources/WaterSettings.asset 에서 조절 — tension(복원 탄성)/damping(감쇠)/
 *   spread(파동 전파)/iterationsPerFrame(안정성: spread*dt/iter*2 < 1 유지) 참고.
 */
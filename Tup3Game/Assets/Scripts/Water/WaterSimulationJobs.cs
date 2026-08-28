using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SimWater
{
public class WaterSimulationJobs
{

    [BurstCompile(CompileSynchronously = true)]
    public struct VelocityJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> positions;
        [ReadOnly] public float damping;
        [ReadOnly] public float tension;
        [ReadOnly] public float spread;
        [ReadOnly] public float deltaTime;
        public NativeArray<float> velocities;

        public VelocityJob(WaterSettings settings, NativeArray<float> velocities, NativeArray<float> positions, float deltaTime)
        {
            damping = settings.damping;
            tension = settings.tension;
            spread = settings.spread;
            this.velocities = velocities;
            this.positions = positions;
            this.deltaTime = deltaTime;
        }

        public void Execute(int i)
        {
            float loss = -velocities[i] * damping;
            float force = tension * (0f - positions[i]);
            velocities[i] += (force + loss) * deltaTime;

            if (i > 0)
                velocities[i] += spread * (positions[i - 1] - positions[i]) * deltaTime;

            if (i < velocities.Length - 1)
                velocities[i] += spread * (positions[i + 1] - positions[i]) * deltaTime;
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct PositionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> velocities;
        [ReadOnly] public float deltaTime;
        public NativeArray<float> positions;

        public PositionJob(NativeArray<float> velocities, NativeArray<float> positions, float deltaTime)
        {
            this.velocities = velocities;
            this.positions = positions;
            this.deltaTime = deltaTime;
        }

        public void Execute(int i) => positions[i] += velocities[i] * deltaTime;
    }
}
}

/* [파일 노트]
 * Tavern_Gamejam_CAU_SSU 프로젝트(Assets/Scripts/Water/WaterSimulationJobs.cs)에서 이식한
 * Burst 컴파일 스프링-파동 Job(속도/위치 적분).
 * 수정 사항: namespace SimWater 로 감쌌다(내용 동일).
 */
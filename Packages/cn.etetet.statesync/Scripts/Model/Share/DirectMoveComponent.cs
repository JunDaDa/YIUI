using Unity.Mathematics;

namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class DirectMoveComponent : Entity, IAwake, IDestroy
    {
        public float3 Direction;   // current move direction (zero = stopped)
        public long MoveTimer;     // frame timer handle
        public float Speed;        // cached movement speed (m/s)
        public long LastTickTime;  // last frame time for delta computation
    }
}

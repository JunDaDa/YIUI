using System;
using Unity.Mathematics;

namespace ET.Server
{
    [EntitySystemOf(typeof(DirectMoveComponent))]
    public static partial class DirectMoveComponentSystem
    {
        [Invoke(TimerInvokeType.DirectMoveTimer)]
        public class DirectMoveTimer : ATimer<DirectMoveComponent>
        {
            protected override void Run(DirectMoveComponent self)
            {
                try
                {
                    self.Tick();
                }
                catch (Exception e)
                {
                    Log.Error($"direct move timer error: {self.Id}\n{e}");
                }
            }
        }

        [EntitySystem]
        private static void Awake(this DirectMoveComponent self)
        {
            self.Direction = float3.zero;
            self.MoveTimer = 0;
            self.Speed = 0f;
            self.LastTickTime = 0;
        }

        [EntitySystem]
        private static void Destroy(this DirectMoveComponent self)
        {
            self.StopMove();
        }

        public static void SetDirection(this DirectMoveComponent self, float3 direction, float speed)
        {
            self.Direction = direction;
            self.Speed = speed;
            self.LastTickTime = TimeInfo.Instance.ServerFrameTime();

            // Start frame timer if not already running
            if (self.MoveTimer == 0)
            {
                self.MoveTimer = self.Root().GetComponent<TimerComponent>()
                    .NewFrameTimer(TimerInvokeType.DirectMoveTimer, self);
            }
        }

        public static void StopMove(this DirectMoveComponent self)
        {
            self.Direction = float3.zero;
            if (self.MoveTimer != 0)
            {
                self.Root().GetComponent<TimerComponent>()?.Remove(ref self.MoveTimer);
            }
        }

        private static void Tick(this DirectMoveComponent self)
        {
            if (math.lengthsq(self.Direction) < 0.0001f)
            {
                return;
            }

            Unit unit = self.GetParent<Unit>();
            long now = TimeInfo.Instance.ServerFrameTime();
            float dt = (now - self.LastTickTime) / 1000f;
            self.LastTickTime = now;

            if (dt <= 0f || dt > 0.5f)
            {
                return; // skip abnormal delta
            }

            // Direct velocity-based movement: position += direction * speed * dt
            float3 newPos = unit.Position + self.Direction * self.Speed * dt;
            unit.Position = newPos;

            // Broadcast position to all visible clients
            M2C_JoystickMove msg = M2C_JoystickMove.Create();
            msg.Id = unit.Id;
            msg.Position = unit.Position;
            msg.Direction = self.Direction;
            MapMessageHelper.Broadcast(unit, msg);
        }
    }
}

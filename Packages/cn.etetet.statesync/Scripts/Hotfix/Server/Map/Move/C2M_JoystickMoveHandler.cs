using Unity.Mathematics;

namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_JoystickMoveHandler : MessageLocationHandler<Unit, C2M_JoystickMove>
    {
        protected override async ETTask Run(Unit unit, C2M_JoystickMove message)
        {
            float3 direction = message.Direction;

            if (math.lengthsq(direction) < 0.001f)
            {
                // Stop: direction is zero
                DirectMoveComponent dmc = unit.GetComponent<DirectMoveComponent>();
                if (dmc != null)
                {
                    dmc.StopMove();
                }

                // Send stop with position correction
                unit.SendStop(0);
            }
            else
            {
                // Cancel any active pathfinding movement first (MOVE-05 coexistence)
                MoveComponent moveComponent = unit.GetComponent<MoveComponent>();
                if (moveComponent != null && !moveComponent.IsArrived())
                {
                    moveComponent.Stop(false);
                }

                // Ensure DirectMoveComponent exists on unit
                DirectMoveComponent dmc = unit.GetComponent<DirectMoveComponent>();
                if (dmc == null)
                {
                    dmc = unit.AddComponent<DirectMoveComponent>();
                }

                float speed = unit.GetComponent<NumericDataComponent>().GetAsFloat(ENumericType.Speed0);
                dmc.SetDirection(math.normalize(direction), speed);
            }

            await ETTask.CompletedTask;
        }
    }
}

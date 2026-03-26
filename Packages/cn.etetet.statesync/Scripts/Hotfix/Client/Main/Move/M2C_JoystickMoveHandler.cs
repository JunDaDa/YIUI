namespace ET.Client
{
    [MessageHandler(SceneType.StateSync)]
    public class M2C_JoystickMoveHandler : MessageHandler<Scene, M2C_JoystickMove>
    {
        protected override async ETTask Run(Scene root, M2C_JoystickMove message)
        {
            Unit unit = root.CurrentScene().GetComponent<UnitComponent>().Get(message.Id);
            if (unit == null)
            {
                return;
            }

            // Server authoritative position update
            // Unit.Position setter auto-publishes ChangePosition event
            // ChangePosition_SyncGameObjectPos handles Transform sync
            unit.Position = message.Position;

            await ETTask.CompletedTask;
        }
    }
}

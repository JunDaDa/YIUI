namespace ET.Client
{
    [Event(SceneType.Current)]
    public class MoveStop_SpineIdle : AEvent<Scene, MoveStop>
    {
        protected override async ETTask Run(Scene scene, MoveStop args)
        {
            Unit unit = args.Unit;
            SpineComponent spineComponent = unit.GetComponent<SpineComponent>();
            if (spineComponent != null)
            {
                spineComponent.PlayByMotionType(MotionType.Idle);
            }

            await ETTask.CompletedTask;
        }
    }
}

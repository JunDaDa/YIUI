namespace ET.Client
{
    /// <summary>
    /// 路径寻路移动开始时播放 run 动画。
    /// 注意：WASD 摇杆移动走 M2C_JoystickMove 协议，不经过此事件。
    /// </summary>
    [Event(SceneType.Current)]
    public class MoveStart_SpineRun : AEvent<Scene, MoveStart>
    {
        protected override async ETTask Run(Scene scene, MoveStart args)
        {
            Unit unit = args.Unit;
            SpineComponent spineComponent = unit.GetComponent<SpineComponent>();
            if (spineComponent != null)
            {
                spineComponent.PlayByMotionType(MotionType.Run);
            }

            await ETTask.CompletedTask;
        }
    }
}

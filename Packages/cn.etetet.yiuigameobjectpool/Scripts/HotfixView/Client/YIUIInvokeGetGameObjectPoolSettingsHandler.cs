using YIUIFramework;

namespace ET.Client
{
    [Invoke]
    public class YIUIInvokeGetGameObjectPoolSettingsHandler : AInvokeHandler<YIUIInvokeGetGameObjectPoolSettings, IYIUIGameObjectPoolSettingsConfig>
    {
        public override IYIUIGameObjectPoolSettingsConfig Handle(YIUIInvokeGetGameObjectPoolSettings args)
        {
            return GameObjectPoolSettingsConfigCategory.Instance?.GetOrDefault(args.ResName);
        }
    }
}

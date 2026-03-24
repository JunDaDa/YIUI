using I2.Loc;

namespace ET.Client
{
    [Invoke(EYIUIInvokeType.Sync)]
    public class YIUIInvokeI2LocalizationSyncHandler : AInvokeEntityHandler<EventView_ChangeLanguage>
    {
        public override void Handle(Entity entity, EventView_ChangeLanguage args)
        {
            entity?.DynamicEvent(args).NoContext();
        }
    }

    [Invoke]
    public class YIUIInvoke_Localization_GetTranslation_Handler : AInvokeHandler<Invoke_Localization_GetTranslation, string>
    {
        public override string Handle(Invoke_Localization_GetTranslation args)
        {
            return LocalizationManager.GetTranslation(args.Key);
        }
    }
}
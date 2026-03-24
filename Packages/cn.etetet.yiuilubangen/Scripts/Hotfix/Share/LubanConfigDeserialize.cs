using System;
using Luban;

namespace ET
{
    [Invoke(ConfigType.Luban)]
    public class ConfigDeserialize_Luban: AInvokeHandler<ConfigLoader.ConfigDeserialize, object>
    {
        public override object Handle(ConfigLoader.ConfigDeserialize args)
        {
            return Activator.CreateInstance(args.Type, new ByteBuf(args.ConfigBytes));
        }
    }
}
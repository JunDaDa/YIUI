#if DOTNET
using System;
using System.Collections.Generic;
using System.IO;

namespace ET
{
    [Invoke]
    public class LubanServerLoaderInvokerGetAll : AInvokeHandler<ConfigLoader.LubanGetAllConfigBytes, ETTask<Dictionary<Type, byte[]>>>
    {
        public override async ETTask<Dictionary<Type, byte[]>> Handle(ConfigLoader.LubanGetAllConfigBytes args)
        {
            var output   = new Dictionary<Type, byte[]>();
            var allTypes = CodeTypes.Instance.GetTypes(typeof(ConfigAttribute));
            var codeMode = "Server";

            foreach (Type configType in allTypes)
            {
                var configAttribute = configType.GetCustomAttributes(typeof(ConfigAttribute), false)[0] as ConfigAttribute;

                var configBytes = await EventSystem.Instance.Invoke<ConfigLoader.LubanGetConfigBytes, ETTask<byte[]>>(configAttribute.ConfigType, new ConfigLoader.LubanGetConfigBytes
                {
                    Type     = configType,
                    CodeMode = codeMode
                });

                if (configBytes != null)
                {
                    output[configType] = configBytes;
                }
                else
                {
                    Log.Error($"没有读取到配置,{codeMode},{configType}");
                }
            }

            return output;
        }
    }

    [Invoke(ConfigType.Luban)]
    public class LubanGetConfigBytes_Luban : AInvokeHandler<ConfigLoader.LubanGetConfigBytes, ETTask<byte[]>>
    {
        public override async ETTask<byte[]> Handle(ConfigLoader.LubanGetConfigBytes args)
        {
            var configType = args.Type;
            var codeMode   = args.CodeMode;

            string configFilePath;

            if (LubanHelper.StartConfigs.Contains(configType.Name))
            {
                Log.Error($"Options.Instance.StartConfig {Options.Instance.StartConfig}");

                configFilePath = Path.Combine($"{LubanHelper.ConfigResPath}/{Options.Instance.StartConfig}/Binary/{codeMode}/{configType.Name}.bytes");
            }
            else
            {
                configFilePath = Path.Combine($"{LubanHelper.ConfigResPath}/Config/Binary/{codeMode}/{configType.Name}.bytes");
            }

            return await File.ReadAllBytesAsync(configFilePath);
        }
    }

    [Invoke(ConfigType.Luban)]
    public class LubanServerLoaderInvokerGetOne : AInvokeHandler<ConfigLoader.LubanGetOneConfigBytes, ETTask<byte[]>>
    {
        public override async ETTask<byte[]> Handle(ConfigLoader.LubanGetOneConfigBytes args)
        {
            return await File.ReadAllBytesAsync($"{LubanHelper.ConfigResPath}/Config/Binary/Server/{args.ConfigName}.bytes");
        }
    }
}
#endif
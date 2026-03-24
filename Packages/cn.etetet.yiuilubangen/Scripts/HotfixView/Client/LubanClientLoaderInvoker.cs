using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ET
{
    [Invoke]
    public class LubanClientLoaderInvokerGetAll : AInvokeHandler<ConfigLoader.LubanGetAllConfigBytes, ETTask<Dictionary<Type, byte[]>>>
    {
        public override async ETTask<Dictionary<Type, byte[]>> Handle(ConfigLoader.LubanGetAllConfigBytes args)
        {
            var output = new Dictionary<Type, byte[]>();
            var allTypes = CodeTypes.Instance.GetTypes(typeof(ConfigAttribute));

            var isEditor = false;

            #if ET10
            #if UNITY_EDITOR
            isEditor = true;
            #else
            isEditor = false;
            #endif
            #else
            isEditor = Define.IsEditor;
            #endif

            if (isEditor)
            {
                var globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
                var codeMode = globalConfig.CodeMode.ToString();
                foreach (Type configType in allTypes)
                {
                    var configAttribute = configType.GetCustomAttributes(typeof(ConfigAttribute), false)[0] as ConfigAttribute;

                    var configBytes = await EventSystem.Instance.Invoke<ConfigLoader.LubanGetConfigBytes, ETTask<byte[]>>(configAttribute.ConfigType, new ConfigLoader.LubanGetConfigBytes
                    {
                        Type = configType,
                        CodeMode = codeMode
                    });

                    if (configBytes != null)
                    {
                        output[configType] = configBytes;
                    }
                }
            }
            else
            {
                foreach (Type type in allTypes)
                {
                    var v = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>(type.Name);
                    output[type] = v.bytes;
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
            var codeMode = args.CodeMode;

            string configFilePath;

            if (LubanHelper.StartConfigs.Contains(configType.Name))
            {
                configFilePath = $"{LubanHelper.ConfigResPath}/StartConfig/{Options.Instance.StartConfig}/Binary/{CodeMode.Server}/{configType.Name}.bytes";
            }
            else
            {
                configFilePath = $"{LubanHelper.ConfigResPath}/Config/Binary/{codeMode}/{configType.Name}.bytes";
            }

            return await File.ReadAllBytesAsync(configFilePath);
        }
    }

    [Invoke(ConfigType.Luban)]
    public class LubanClientLoaderInvokerGetOne : AInvokeHandler<ConfigLoader.LubanGetOneConfigBytes, ETTask<byte[]>>
    {
        public override async ETTask<byte[]> Handle(ConfigLoader.LubanGetOneConfigBytes args)
        {
            var globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
            var codeMode = globalConfig.CodeMode.ToString();
            var configFilePath = $"{LubanHelper.ConfigResPath}/Config/Binary/{codeMode}/{args.ConfigName}.bytes";
            return await File.ReadAllBytesAsync(configFilePath);
        }
    }
}
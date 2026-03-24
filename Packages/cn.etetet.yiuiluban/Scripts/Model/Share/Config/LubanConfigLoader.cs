using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Luban;
#if DOTNET || UNITY_STANDALONE
using System.Threading.Tasks;
#endif

namespace ET
{
    public partial class ConfigLoader : Singleton<ConfigLoader>, ISingletonAwake
    {
        public struct ConfigDeserialize
        {
            public Type   Type;
            public byte[] ConfigBytes;
        }

        public struct LubanGetConfigBytes
        {
            public Type   Type;
            public string CodeMode;
        }

        public struct GetAllConfigBytes
        {
        }

        public struct GetOneConfigBytes
        {
            public string ConfigName;
        }

        public struct LubanGetAllConfigBytes
        {
        }

        public struct LubanGetOneConfigBytes
        {
            public string ConfigName;
        }

        private readonly ConcurrentDictionary<Type, ILubanConfig> m_AllConfig = new();

        public void Awake()
        {
        }

        public async ETTask Reload(Type configType)
        {
            var configAttribute = configType.GetCustomAttributes(typeof(ConfigAttribute), false)[0] as ConfigAttribute;
            var oneConfigBytes = await EventSystem.Instance.Invoke<LubanGetOneConfigBytes, ETTask<byte[]>>(configAttribute.ConfigType, new LubanGetOneConfigBytes()
            {
                ConfigName = configType.Name
            });
            LoadOneConfig(configType, oneConfigBytes);
            ResolveRef();
            ConfigProcess();
        }

        public async ETTask LoadAsync()
        {
            m_AllConfig.Clear();

            var configBytes = await EventSystem.Instance.Invoke<LubanGetAllConfigBytes, ETTask<Dictionary<Type, byte[]>>>(new LubanGetAllConfigBytes());

            #if DOTNET || UNITY_STANDALONE
            using ListComponent<Task> listTasks = ListComponent<Task>.Create();

            foreach (Type type in configBytes.Keys)
            {
                var  oneConfigBytes = configBytes[type];
                Task task           = Task.Run(() => LoadOneConfig(type, oneConfigBytes));
                listTasks.Add(task);
            }

            await Task.WhenAll(listTasks.ToArray());
            #else
            foreach (Type type in configBytes.Keys)
            {
                LoadOneConfig(type, configBytes[type]);
            }
            #endif
            ResolveRef();
            ConfigProcess();
        }

        private void LoadOneConfig(Type configType, byte[] oneConfigBytes)
        {
            var configAttribute = configType.GetCustomAttributes(typeof(ConfigAttribute), false)[0] as ConfigAttribute;

            var category = EventSystem.Instance.Invoke<ConfigDeserialize, object>(configAttribute.ConfigType, new ConfigDeserialize() { Type = configType, ConfigBytes = oneConfigBytes });

            if (category is not ILubanConfig iConfig)
            {
                Log.Error($"{configType}, 数据类型错误 实际 {category.GetType()}");
                return;
            }

            m_AllConfig[configType] = iConfig;
            World.Instance.AddSingleton(category as ASingleton);
        }

        private void ResolveRef()
        {
            foreach (var targetConfig in m_AllConfig.Values)
            {
                targetConfig.ResolveRef();
            }

            foreach (var targetConfig in m_AllConfig.Values)
            {
                Initialized(targetConfig);
            }
        }

        private void Initialized(ILubanConfig configCategory)
        {
            var iConfigSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(configCategory.GetType(), typeof(ILubanConfigSystem));
            if (iConfigSystems == null)
            {
                return;
            }

            foreach (ILubanConfigSystem aConfigSystem in iConfigSystems)
            {
                if (aConfigSystem == null)
                {
                    continue;
                }

                try
                {
                    aConfigSystem.LubanConfig(configCategory);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        private void ConfigProcess()
        {
            var hashSet = CodeTypes.Instance.GetTypes(typeof(ConfigProcessAttribute));
            foreach (Type type in hashSet)
            {
                object obj = Activator.CreateInstance(type);
                if (obj is ISingletonAwake awakeSingleton)
                {
                    awakeSingleton.Awake();
                }

                World.Instance.AddSingleton((ASingleton)obj);
            }
        }
    }
}
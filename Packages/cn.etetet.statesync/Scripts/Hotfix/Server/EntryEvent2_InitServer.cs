namespace ET.Server
{
    [Event(SceneType.StateSync)]
    public class EntryEvent2_InitServer : AEvent<Scene, EntryEvent2>
    {
        protected override async ETTask Run(Scene root, EntryEvent2 args)
        {
            if (Options.Instance.Console == 1)
            {
                root.AddComponent<ConsoleComponent>();
            }

            World.Instance.AddSingleton<NavmeshComponent>();

            int process = root.Fiber.Process;
            StartProcessConfig startProcessConfig = StartProcessConfigCategory.Instance.Get(process);

            if (!Options.Instance.IsLocalNetwork && startProcessConfig.Port != 0)
            {
                await FiberManager.Instance.Create(SchedulerType.ThreadPool, SceneType.NetInner, 0, SceneType.NetInner, "NetInner");
            }

            // Local模式：创建 MuxTransport，UdpTransport 构造时会自动注册到它
            if (Options.Instance.IsLocalNetwork)
            {
                InMemoryTransportRegistry.ThreadSafe = false;
                InMemoryTransportRegistry.Clear();
                InMemoryTransportRegistry.RegisterMux(new InMemoryMuxTransport());
            }

            // 根据配置创建纤程
            var scenes = StartSceneConfigCategory.Instance.GetByProcess(process);

            foreach (StartSceneConfig startConfig in scenes)
            {
                // Local模式跳过路由相关场景
                if (Options.Instance.IsLocalNetwork)
                {
                    string sceneTypeName = startConfig.SceneType;
                    if (sceneTypeName == "Router" || sceneTypeName == "RouterManager")
                    {
                        continue;
                    }
                }

                int sceneType = SceneTypeSingleton.Instance.GetSceneType(startConfig.SceneType);
                SchedulerType schedulerType = Options.Instance.IsLocalNetwork
                    ? SchedulerType.Main
                    : SchedulerType.ThreadPool;
                await FiberManager.Instance.Create(schedulerType, startConfig.Id, startConfig.Zone, sceneType, startConfig.Name);
            }
        }
    }
}

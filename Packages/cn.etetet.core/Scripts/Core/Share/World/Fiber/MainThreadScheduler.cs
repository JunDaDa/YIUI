using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Profiling;

namespace ET
{
    internal class MainThreadScheduler: IScheduler
    {
        private readonly ConcurrentQueue<int> idQueue = new();
        private readonly ConcurrentQueue<int> addIds = new();
        private readonly FiberManager fiberManager;
        private readonly ThreadSynchronizationContext threadSynchronizationContext = new();
        private Fiber firstFiber;

        public MainThreadScheduler(FiberManager fiberManager)
        {
            SynchronizationContext.SetSynchronizationContext(this.threadSynchronizationContext);
            this.fiberManager = fiberManager;
        }

        public void Dispose()
        {
            this.addIds.Clear();
            this.idQueue.Clear();
        }

        public void Update()
        {
            SynchronizationContext.SetSynchronizationContext(this.threadSynchronizationContext);

            Profiler.BeginSample("ET.SyncContext.Update");
            this.threadSynchronizationContext.Update();
            Profiler.EndSample();

            int count = this.idQueue.Count;
            while (count-- > 0)
            {
                if (!this.idQueue.TryDequeue(out int id))
                {
                    continue;
                }

                Fiber fiber = this.fiberManager.Get(id);
                if (fiber == null)
                {
                    continue;
                }

                if (fiber.IsDisposed)
                {
                    continue;
                }

                Fiber.Instance = fiber;
                SynchronizationContext.SetSynchronizationContext(fiber.ThreadSynchronizationContext);
                Profiler.BeginSample(string.IsNullOrEmpty(fiber.Root.Name) ? "Fiber" : fiber.Root.Name);
                fiber.Update();
                Profiler.EndSample();
                this.idQueue.Enqueue(id);
            }

            Fiber.Instance = this.firstFiber;
            // Fiber调度完成，要还原成默认的上下文，否则unity的回调会找不到正确的上下文
            SynchronizationContext.SetSynchronizationContext(this.threadSynchronizationContext);
        }

        public void LateUpdate()
        {
            int count = this.idQueue.Count;
            while (count-- > 0)
            {
                if (!this.idQueue.TryDequeue(out int id))
                {
                    continue;
                }

                Fiber fiber = this.fiberManager.Get(id);
                if (fiber == null)
                {
                    continue;
                }

                if (fiber.IsDisposed)
                {
                    continue;
                }

                Fiber.Instance = fiber;
                SynchronizationContext.SetSynchronizationContext(fiber.ThreadSynchronizationContext);
                Profiler.BeginSample(string.IsNullOrEmpty(fiber.Root.Name) ? "Fiber" : fiber.Root.Name);
                fiber.LateUpdate();
                Profiler.EndSample();
                this.idQueue.Enqueue(id);
            }

            while (this.addIds.Count > 0)
            {
                this.addIds.TryDequeue(out int result);
                this.idQueue.Enqueue(result);
            }
            
            Fiber.Instance = this.firstFiber;
            // Fiber调度完成，要还原成默认的上下文，否则unity的回调会找不到正确的上下文
            SynchronizationContext.SetSynchronizationContext(this.threadSynchronizationContext);
        }


        public void Add(int fiberId = 0)
        {
            this.addIds.Enqueue(fiberId);
            this.firstFiber ??= this.fiberManager.Get(fiberId);
        }
    }
}
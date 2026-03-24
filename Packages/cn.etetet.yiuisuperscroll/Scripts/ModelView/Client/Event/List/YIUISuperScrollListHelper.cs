//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public static class YIUISuperScrollListHelper
    {
        public static int GetPrefabIndex(Type getPrefabIndexType, Entity self, YIUISuperScrollListComponent superScrollList, int index)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), getPrefabIndexType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} 没有具体实现的事件 IYIUISuperScrollListGetPrefab 请检查");
                return -1;
            }

            foreach (IYIUISuperScrollListGetPrefab eventSystem in iEventSystems)
            {
                try
                {
                    return eventSystem.GetPrefabIndex(self, superScrollList, index);
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} 事件回调错误 IYIUISuperScrollListGetPrefab 请检查 {e}");
                }
            }

            return -1;
        }

        public static void Renderer(Type rendererType, Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), rendererType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 没有具体实现的事件 IYIUISuperScrollListRenderer 请检查");
                return;
            }

            foreach (IYIUISuperScrollListRenderer eventSystem in iEventSystems)
            {
                try
                {
                    eventSystem.Renderer(self, item, superScrollList, index, select);
                    return;
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 事件回调错误 IYIUISuperScrollListRenderer 请检查 {e}");
                }
            }
        }

        public static void OnClick(Type onclickType, Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), onclickType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 没有具体实现的事件 IYIUISuperScrollListOnClick 请检查");
                return;
            }

            foreach (IYIUISuperScrollListOnClick eventSystem in iEventSystems)
            {
                try
                {
                    eventSystem.OnClick(self, item, superScrollList, index, select);
                    return;
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 事件回调错误 IYIUISuperScrollListOnClick 请检查 {e}");
                }
            }
        }

        public static bool OnClickCheck(Type onclickCheckType, Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), onclickCheckType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 没有具体实现的事件 IYIUILoopOnClickCheck 请检查");
                return false;
            }

            foreach (IYIUISuperScrollListOnClickCheck eventSystem in iEventSystems)
            {
                try
                {
                    return eventSystem.OnClickCheck(self, item, superScrollList, index, select);
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 事件回调错误 IYIUILoopOnClick 请检查 {e}");
                    return false;
                }
            }

            return false;
        }

        public static void Finished(Type rendererType, Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), rendererType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 没有具体实现的事件 IYIUISuperScrollListFinished 请检查");
                return;
            }

            foreach (IYIUISuperScrollListFinished eventSystem in iEventSystems)
            {
                try
                {
                    eventSystem.Finished(self, item, superScrollList, index, select);
                    return;
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 事件回调错误 IYIUISuperScrollListFinished 请检查 {e}");
                }
            }
        }

        public static void Changed(Type rendererType, Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), rendererType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 没有具体实现的事件 IYIUISuperScrollListChanged 请检查");
                return;
            }

            foreach (IYIUISuperScrollListChanged eventSystem in iEventSystems)
            {
                try
                {
                    eventSystem.Changed(self, item, superScrollList, index, select);
                    return;
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 事件回调错误 IYIUISuperScrollListChanged 请检查 {e}");
                }
            }
        }

        public static void Moved(Type rendererType, Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), rendererType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 没有具体实现的事件 IYIUISuperScrollListMoved 请检查");
                return;
            }

            foreach (IYIUISuperScrollListMoved eventSystem in iEventSystems)
            {
                try
                {
                    eventSystem.Moved(self, item, superScrollList, index, select);
                    return;
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 事件回调错误 IYIUISuperScrollListMoved 请检查 {e}");
                }
            }
        }
    }
}
//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public static class YIUISuperScrollStaggeredGridHelper
    {
        public static int GetPrefabIndex(Type getPrefabIndexType, Entity self, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), getPrefabIndexType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} 没有具体实现的事件 IYIUISuperScrollStaggeredGridGetPrefab 请检查");
                return -1;
            }

            foreach (IYIUISuperScrollStaggeredGridGetPrefab eventSystem in iEventSystems)
            {
                try
                {
                    return eventSystem.GetPrefabIndex(self, superScrollStaggeredGrid, index);
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} 事件回调错误 IYIUISuperScrollStaggeredGridGetPrefab 请检查 {e}");
                }
            }

            return -1;
        }

        public static void Renderer(Type rendererType, Entity self, Entity item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index, bool select)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), rendererType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 没有具体实现的事件 IYIUISuperScrollStaggeredGridRenderer 请检查");
                return;
            }

            foreach (IYIUISuperScrollStaggeredGridRenderer eventSystem in iEventSystems)
            {
                try
                {
                    eventSystem.Renderer(self, item, superScrollStaggeredGrid, index, select);
                    return;
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 事件回调错误 IYIUISuperScrollStaggeredGridRenderer 请检查 {e}");
                }
            }
        }

        public static void OnClick(Type onclickType, Entity self, Entity item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index, bool select)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), onclickType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 没有具体实现的事件 IYIUISuperScrollStaggeredGridOnClick 请检查");
                return;
            }

            foreach (IYIUISuperScrollStaggeredGridOnClick eventSystem in iEventSystems)
            {
                try
                {
                    eventSystem.OnClick(self, item, superScrollStaggeredGrid, index, select);
                    return;
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 事件回调错误 IYIUISuperScrollStaggeredGridOnClick 请检查 {e}");
                }
            }
        }

        public static bool OnClickCheck(Type onclickCheckType, Entity self, Entity item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index, bool select)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), onclickCheckType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 没有具体实现的事件 IYIUISuperScrollStaggeredGridOnClickCheck 请检查");
                return false;
            }

            foreach (IYIUISuperScrollStaggeredGridOnClickCheck eventSystem in iEventSystems)
            {
                try
                {
                    return eventSystem.OnClickCheck(self, item, superScrollStaggeredGrid, index, select);
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 事件回调错误 IYIUISuperScrollStaggeredGridOnClickCheck 请检查 {e}");
                    return false;
                }
            }

            return false;
        }

        public static (float, float) GetItemSize(Type getItemSizeType, Entity self, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), getItemSizeType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} 没有具体实现的事件 IYIUISuperScrollStaggeredGridGetItemSize 请检查");
                return (100f, 0f);
            }

            foreach (IYIUISuperScrollStaggeredGridGetItemSize eventSystem in iEventSystems)
            {
                try
                {
                    return eventSystem.GetItemSize(self, superScrollStaggeredGrid, index);
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} 事件回调错误 IYIUISuperScrollStaggeredGridGetItemSize 请检查 {e}");
                    return (100f, 0f);
                }
            }

            return (100f, 0f);
        }
    }
}
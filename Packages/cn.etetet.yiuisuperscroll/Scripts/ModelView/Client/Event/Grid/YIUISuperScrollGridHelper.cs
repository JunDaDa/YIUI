//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public static class YIUISuperScrollGridHelper
    {
        public static int GetPrefabIndex(Type getPrefabIndexType, Entity self, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), getPrefabIndexType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} 没有具体实现的事件 IYIUISuperScrollGridGetPrefab 请检查");
                return -1;
            }

            foreach (IYIUISuperScrollGridGetPrefab eventSystem in iEventSystems)
            {
                try
                {
                    return eventSystem.GetPrefabIndex(self, superScrollGrid, index, row, column);
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} 事件回调错误 IYIUISuperScrollGridGetPrefab 请检查 {e}");
                }
            }

            return -1;
        }

        public static void Renderer(Type rendererType, Entity self, Entity item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), rendererType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 没有具体实现的事件 IYIUISuperScrollGridRenderer 请检查");
                return;
            }

            foreach (IYIUISuperScrollGridRenderer eventSystem in iEventSystems)
            {
                try
                {
                    eventSystem.Renderer(self, item, superScrollGrid, index, row, column, select);
                    return;
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 事件回调错误 IYIUISuperScrollGridRenderer 请检查 {e}");
                }
            }
        }

        public static void OnClick(Type onclickType, Entity self, Entity item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), onclickType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 没有具体实现的事件 IYIUISuperScrollGridOnClick 请检查");
                return;
            }

            foreach (IYIUISuperScrollGridOnClick eventSystem in iEventSystems)
            {
                try
                {
                    eventSystem.OnClick(self, item, superScrollGrid, index, row, column, select);
                    return;
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 事件回调错误 IYIUISuperScrollGridOnClick 请检查 {e}");
                }
            }
        }

        public static bool OnClickCheck(Type onclickCheckType, Entity self, Entity item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), onclickCheckType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 没有具体实现的事件 IYIUISuperScrollGridOnClickCheck 请检查");
                return false;
            }

            foreach (IYIUISuperScrollGridOnClickCheck eventSystem in iEventSystems)
            {
                try
                {
                    return eventSystem.OnClickCheck(self, item, superScrollGrid, index, row, column, select);
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 事件回调错误 IYIUISuperScrollGridOnClickCheck 请检查 {e}");
                    return false;
                }
            }

            return false;
        }

        public static void Finished(Type rendererType, Entity self, Entity item, YIUISuperScrollGridComponent superScrollGrid, int index, bool select)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), rendererType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 没有具体实现的事件 IYIUISuperScrollGridFinished 请检查");
                return;
            }

            foreach (IYIUISuperScrollGridFinished eventSystem in iEventSystems)
            {
                try
                {
                    eventSystem.Finished(self, item, superScrollGrid, index, select);
                    return;
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 事件回调错误 IYIUISuperScrollGridFinished 请检查 {e}");
                }
            }
        }

        public static void Changed(Type rendererType, Entity self, Entity item, YIUISuperScrollGridComponent superScrollGrid, int index, bool select)
        {
            var iEventSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(self.GetType(), rendererType);
            if (iEventSystems is not { Count: > 0 })
            {
                Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 没有具体实现的事件 IYIUISuperScrollGridChanged 请检查");
                return;
            }

            foreach (IYIUISuperScrollGridChanged eventSystem in iEventSystems)
            {
                try
                {
                    eventSystem.Changed(self, item, superScrollGrid, index, select);
                    return;
                }
                catch (Exception e)
                {
                    Log.Error($"类:{self.GetType().Name} Item:{item.GetType().Name} 事件回调错误 IYIUISuperScrollGridChanged 请检查 {e}");
                }
            }
        }
    }
}
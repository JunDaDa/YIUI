#if YIUI && ENABLE_VIEW && UNITY_EDITOR
using System;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ET
{
    public partial class YIUIGameObjectEntityRef
    {
        [GUIColor(0.4f, 0.8f, 1)]
        [Button("打开数值GM面板", 30, Icon = SdfIconType.Cpu, IconAlignment = IconAlignment.LeftOfText)]
        private void OpenNumericGMView()
        {
            if (Entity == null)
            {
                Debug.LogError($"没有找到Entity");
                return;
            }

            try
            {
                var modelAssembly = Assembly.Load("ET.Model");
                var numericDataComponentType = modelAssembly.GetType("ET.NumericDataComponent");

                var getComponentMethod = typeof(Entity).GetMethod("GetComponent",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null).MakeGenericMethod(numericDataComponentType);
                var numericDataComponent = getComponentMethod.Invoke(Entity, null);

                var numericDataField = numericDataComponentType.GetField("NumericData", BindingFlags.Public | BindingFlags.Instance);
                var numericData = numericDataField.GetValue(numericDataComponent);

                var editorAssembly = Assembly.Load("ET.YIUI.Numeric.Editor");
                var gmWindowType = editorAssembly.GetType("ET.NumericGMWindow");

                var switchWindowMethod = gmWindowType.GetMethod("SwitchWindow", BindingFlags.Public | BindingFlags.Static);
                switchWindowMethod.Invoke(null, new[] { numericData });
            }
            catch (Exception e)
            {
                Debug.LogError($"打开数值GM面板失败: {e}");
            }
        }
    }
}
#endif
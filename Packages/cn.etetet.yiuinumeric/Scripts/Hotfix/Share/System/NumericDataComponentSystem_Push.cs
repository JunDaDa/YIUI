using System.Collections.Generic;

namespace ET
{
    [FriendOf(typeof(NumericDataComponent))]
    public static partial class NumericDataComponentSystem
    {
        /// <summary>
        /// 数值改变推送
        /// 会根据改变类型做不同分发
        /// 这个类型不会太多扩展 如果你需要一直扩展 那就是设计有问题
        /// </summary>
        public static void PushEvent(this NumericDataComponent self, int numericType, long newValue, long oldValue)
        {
            var numericChange = NumericChangeHelper.Create(self.Parent, numericType, oldValue, newValue);
            EventSystem.Instance.Publish(self.Scene(), numericChange);
        }

        /// <summary>
        /// 全数值推送
        /// </summary>
        public static void PushEventAll(this NumericDataComponent self)
        {
            var pushList = new Dictionary<int, long>(self.NumericDic);
            foreach (var data in pushList)
            {
                self.PushEvent(data.Key, data.Value, 0);
            }
        }
    }
}
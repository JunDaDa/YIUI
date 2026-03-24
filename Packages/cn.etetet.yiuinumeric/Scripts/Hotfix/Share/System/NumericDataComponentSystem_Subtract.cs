using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 相减
    /// </summary>
    [FriendOf(typeof(NumericDataComponent))]
    public static partial class NumericDataComponentSystem
    {
        #region 相减 得到一个新的结果

        /*
         注意新结果是没有owner的 所以不会有事件推送 需要自行处理
         因为不知道拿这个新结果是干嘛  所以当前与目标 都不会主动回收
         如果你拿到这个数据是打算覆盖某个目标 如果有必要需要自己手动回收
         */

        public static NumericData Subtract(this NumericDataComponent self, NumericData target, Entity owner = null)
        {
            var newData = self.NumericData.Subtract(target);
            if (owner != null)
            {
                newData.UpdateOwnerEntity(owner);
            }

            return newData;
        }

        [Obsolete("请使用下面的方法 根据使用需求最好传入owner")]
        public static NumericData Subtract(this NumericDataComponent self, params NumericData[] allData)
        {
            return self.NumericData.Subtract(allData);
        }

        public static NumericData Subtract(this NumericDataComponent self, Entity owner, params NumericData[] allData)
        {
            var newData = self.NumericData.Subtract(allData);
            if (owner != null)
            {
                newData.UpdateOwnerEntity(owner);
            }

            return newData;
        }

        public static NumericData Subtract(this NumericDataComponent self, IEnumerable<NumericData> allData, Entity owner = null)
        {
            var newData = self.NumericData.Subtract(allData);
            if (owner != null)
            {
                newData.UpdateOwnerEntity(owner);
            }

            return newData;
        }

        public static NumericData Subtract(this NumericDataComponent self, NumericDataComponent target, Entity owner = null)
        {
            var newData = self.NumericData.Subtract(target);
            if (owner != null)
            {
                newData.UpdateOwnerEntity(owner);
            }

            return newData;
        }

        [Obsolete("请使用下面的方法 根据使用需求最好传入owner")]
        public static NumericData Subtract(this NumericDataComponent self, params NumericDataComponent[] allData)
        {
            return self.NumericData.Subtract(allData);
        }

        public static NumericData Subtract(this NumericDataComponent self, Entity owner, params NumericDataComponent[] allData)
        {
            var newData = self.NumericData.Subtract(allData);
            if (owner != null)
            {
                newData.UpdateOwnerEntity(owner);
            }

            return newData;
        }

        public static NumericData Subtract(this NumericDataComponent self, IEnumerable<NumericDataComponent> allData, Entity owner = null)
        {
            var newData = self.NumericData.Subtract(allData);
            if (owner != null)
            {
                newData.UpdateOwnerEntity(owner);
            }

            return newData;
        }

        #endregion

        #region 自身与目标相减后修改自己的数据 慎用

        public static void SubtractChange(this NumericDataComponent self, NumericData target, bool isPushEvent = false)
        {
            var lastData = self.NumericData;
            self.NumericData = self.NumericData.Subtract(target);
            self.NumericData.UpdateOwnerEntity(self);
            if (isPushEvent)
            {
                self.PushEventAll();
            }

            lastData.Dispose();
        }

        public static void SubtractChange(this NumericDataComponent self, bool isPushEvent = false, params NumericData[] allData)
        {
            var lastData = self.NumericData;
            self.NumericData = self.NumericData.Subtract(allData);
            self.NumericData.UpdateOwnerEntity(self);
            if (isPushEvent)
            {
                self.PushEventAll();
            }

            lastData.Dispose();
        }

        public static void SubtractChange(this NumericDataComponent self, IEnumerable<NumericData> allData, bool isPushEvent = false)
        {
            var lastData = self.NumericData;
            self.NumericData = self.NumericData.Subtract(allData);
            self.NumericData.UpdateOwnerEntity(self);
            if (isPushEvent)
            {
                self.PushEventAll();
            }

            lastData.Dispose();
        }

        public static void SubtractChange(this NumericDataComponent self, NumericDataComponent target, bool isPushEvent = false)
        {
            var lastData = self.NumericData;
            self.NumericData = self.NumericData.Subtract(target);
            self.NumericData.UpdateOwnerEntity(self);
            if (isPushEvent)
            {
                self.PushEventAll();
            }

            lastData.Dispose();
        }

        public static void SubtractChange(this NumericDataComponent self, bool isPushEvent = false, params NumericDataComponent[] allData)
        {
            var lastData = self.NumericData;
            self.NumericData = self.NumericData.Subtract(allData);
            self.NumericData.UpdateOwnerEntity(self);
            if (isPushEvent)
            {
                self.PushEventAll();
            }

            lastData.Dispose();
        }

        public static void SubtractChange(this NumericDataComponent self, IEnumerable<NumericDataComponent> allData, bool isPushEvent = false)
        {
            var lastData = self.NumericData;
            self.NumericData = self.NumericData.Subtract(allData);
            self.NumericData.UpdateOwnerEntity(self);
            if (isPushEvent)
            {
                self.PushEventAll();
            }

            lastData.Dispose();
        }

        #endregion
    }
}
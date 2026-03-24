using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 相加
    /// </summary>
    [FriendOf(typeof(NumericDataComponent))]
    public static partial class NumericDataComponentSystem
    {
        #region 相加得到一个新的结果

        /*
         注意新结果是没有owner的 所以不会有事件推送 需要自行处理
         因为不知道拿这个新结果是干嘛  所以当前与目标 都不会主动回收
         如果你拿到这个数据是打算覆盖某个目标 如果有必要需要自己手动回收
         */

        public static NumericData Add(this NumericDataComponent self, NumericData target, Entity owner = null)
        {
            var newData = self.NumericData.Add(target);
            if (owner != null)
            {
                newData.UpdateOwnerEntity(owner);
            }

            return newData;
        }

        [Obsolete("请使用下面的方法 根据使用需求最好传入owner")]
        public static NumericData Add(this NumericDataComponent self, params NumericData[] allData)
        {
            return self.NumericData.Add(allData);
        }

        public static NumericData Add(this NumericDataComponent self, Entity owner, params NumericData[] allData)
        {
            var newData = self.NumericData.Add(allData);
            if (owner != null)
            {
                newData.UpdateOwnerEntity(owner);
            }

            return newData;
        }

        public static NumericData Add(this NumericDataComponent self, IEnumerable<NumericData> allData, Entity owner = null)
        {
            var newData = self.NumericData.Add(allData);
            if (owner != null)
            {
                newData.UpdateOwnerEntity(owner);
            }

            return newData;
        }

        public static NumericData Add(this NumericDataComponent self, NumericDataComponent target, Entity owner = null)
        {
            var newData = self.NumericData.Add(target);
            if (owner != null)
            {
                newData.UpdateOwnerEntity(owner);
            }

            return newData;
        }

        [Obsolete("请使用下面的方法 根据使用需求最好传入owner")]
        public static NumericData Add(this NumericDataComponent self, params NumericDataComponent[] allComponent)
        {
            return self.NumericData.Add(allComponent);
        }

        public static NumericData Add(this NumericDataComponent self, Entity owner, params NumericDataComponent[] allComponent)
        {
            var newData = self.NumericData.Add(allComponent);
            if (owner != null)
            {
                newData.UpdateOwnerEntity(owner);
            }

            return newData;
        }

        public static NumericData Add(this NumericDataComponent self, IEnumerable<NumericDataComponent> allComponent, Entity owner = null)
        {
            var newData = self.NumericData.Add(allComponent);
            if (owner != null)
            {
                newData.UpdateOwnerEntity(owner);
            }

            return newData;
        }

        #endregion

        #region 自身与目标相加后修改自己的数据 慎用

        public static void AddChange(this NumericDataComponent self, NumericData target, bool isPushEvent = false)
        {
            var lastData = self.NumericData;
            self.NumericData = self.NumericData.Add(target);
            self.NumericData.UpdateOwnerEntity(self);
            if (isPushEvent)
            {
                self.PushEventAll();
            }

            lastData.Dispose();
        }

        public static void AddChange(this NumericDataComponent self, bool isPushEvent = false, params NumericData[] allData)
        {
            var lastData = self.NumericData;
            self.NumericData = self.NumericData.Add(allData);
            self.NumericData.UpdateOwnerEntity(self);
            if (isPushEvent)
            {
                self.PushEventAll();
            }

            lastData.Dispose();
        }

        public static void AddChange(this NumericDataComponent self, IEnumerable<NumericData> allData, bool isPushEvent = false)
        {
            var lastData = self.NumericData;
            self.NumericData = self.NumericData.Add(allData);
            self.NumericData.UpdateOwnerEntity(self);
            if (isPushEvent)
            {
                self.PushEventAll();
            }

            lastData.Dispose();
        }

        public static void AddChange(this NumericDataComponent self, NumericDataComponent target, bool isPushEvent = false)
        {
            var lastData = self.NumericData;
            self.NumericData = self.NumericData.Add(target);
            self.NumericData.UpdateOwnerEntity(self);
            if (isPushEvent)
            {
                self.PushEventAll();
            }

            lastData.Dispose();
        }

        public static void AddChange(this NumericDataComponent self, bool isPushEvent = false, params NumericDataComponent[] allComponent)
        {
            var lastData = self.NumericData;
            self.NumericData = self.NumericData.Add(allComponent);
            self.NumericData.UpdateOwnerEntity(self);
            if (isPushEvent)
            {
                self.PushEventAll();
            }

            lastData.Dispose();
        }

        public static void AddChange(this NumericDataComponent self, IEnumerable<NumericDataComponent> allComponent, bool isPushEvent = false)
        {
            var lastData = self.NumericData;
            self.NumericData = self.NumericData.Add(allComponent);
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
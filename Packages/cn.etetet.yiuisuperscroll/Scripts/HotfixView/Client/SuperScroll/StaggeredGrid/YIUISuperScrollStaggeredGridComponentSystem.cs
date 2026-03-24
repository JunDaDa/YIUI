//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;
using SuperScrollView;

namespace ET.Client
{
    [FriendOf(typeof(YIUISuperScrollStaggeredGridComponent))]
    [EntitySystemOf(typeof(YIUISuperScrollStaggeredGridComponent))]
    public static partial class YIUISuperScrollStaggeredGridComponentSystem
    {
        [EntitySystem]
        private static void Awake(this YIUISuperScrollStaggeredGridComponent self, LoopStaggeredGridView staggeredGridView, GridViewLayoutParam layoutParam)
        {
            self.m_YIUIBindRef = self.Scene().YIUIBind();
            self.m_OwnerEntity = self.GetParent<Entity>();
            self.m_Owner       = staggeredGridView;
            var prefabCount = self.Owner.ItemPrefabDataList.Count;
            if (prefabCount <= 0)
            {
                Log.Error($"[{self.Owner.name}]的ItemPrefabDataList为空");
                return;
            }

            if (prefabCount == 1)
            {
                self.Owner.InitListView(0, layoutParam, self.OnGetOneItemByItemIndex);
            }
            else
            {
                self.Owner.InitListView(0, layoutParam, self.OnGetOtherItemByItemIndex);
            }

            self.AwakeOnClick();
        }

        static partial void AwakeOnClick(this YIUISuperScrollStaggeredGridComponent self);

        [EntitySystem]
        private static void Destroy(this YIUISuperScrollStaggeredGridComponent self)
        {
        }

        //只有一个所以直接使用第一个对象池的数据
        //只有一个的不需要实现GetPrefabIndex
        private static LoopStaggeredGridViewItem OnGetOneItemByItemIndex(this YIUISuperScrollStaggeredGridComponent self, LoopStaggeredGridView staggeredGridView, int itemIndex)
        {
            var item = self.OnGetItemByPrefabIndex(staggeredGridView, 0);
            if (item == null)
            {
                return null;
            }

            var getType = self.GetItemRendererType(item);
            var select  = self.m_OnClickItemHashSet.Contains(itemIndex);
            YIUISuperScrollStaggeredGridHelper.Renderer(getType, self.OwnerEntity, item.OwnerEntity, self, itemIndex, select);
            return item;
        }

        //有多个的情况下根据实际返回索引去获取对象池的数据
        private static LoopStaggeredGridViewItem OnGetOtherItemByItemIndex(this YIUISuperScrollStaggeredGridComponent self, LoopStaggeredGridView staggeredGridView, int itemIndex)
        {
            var prefabIndex = YIUISuperScrollStaggeredGridHelper.GetPrefabIndex(self.GetPrefabIndexType, self.OwnerEntity, self, itemIndex);
            var item        = self.OnGetItemByPrefabIndex(staggeredGridView, prefabIndex);
            if (item == null)
            {
                return null;
            }

            var getType = self.GetItemRendererType(item);
            var select  = self.m_OnClickItemHashSet.Contains(itemIndex);
            YIUISuperScrollStaggeredGridHelper.Renderer(getType, self.OwnerEntity, item.OwnerEntity, self, itemIndex, select);
            return item;
        }

        // 获取项目大小的回调
        private static (float, float) OnGetItemSizeByItemIndex(this YIUISuperScrollStaggeredGridComponent self, int itemIndex)
        {
            var getItemSizeType = self.GetItemSizeSystemType();
            if (getItemSizeType == null)
            {
                // 如果没有实现获取大小的接口，返回默认值
                return (100f, 0f);
            }

            return YIUISuperScrollStaggeredGridHelper.GetItemSize(getItemSizeType, self.OwnerEntity, self, itemIndex);
        }

        private static LoopStaggeredGridViewItem OnGetItemByPrefabIndex(this YIUISuperScrollStaggeredGridComponent self, LoopStaggeredGridView staggeredGridView, int prefabIndex)
        {
            var item = staggeredGridView.NewListViewItem(prefabIndex);
            if (item == null)
            {
                return null;
            }

            var ownerEntity = item.OwnerEntity;
            if (ownerEntity == null)
            {
                var resName = item.ResName;
                var vo      = self.YIUIBind.GetBindVoByResName(resName);
                if (vo == null)
                {
                    Log.Error($"创建失败,找不到资源, {resName}");
                    return null;
                }

                var entity = YIUIFactory.CreateByObjVo(vo.Value, item.gameObject, self);
                if (!item.SetOwnerEntity(entity))
                {
                    return null;
                }

                self.AddOnClickEvent(item);
            }

            return item;
        }

        private static Type GetItemRendererType(this YIUISuperScrollStaggeredGridComponent self, LoopStaggeredGridViewItem item)
        {
            var resName = item.ResName;
            if (!self.m_RendererSystemTypeDict.TryGetValue(resName, out var getType))
            {
                getType = typeof(IYIUISuperScrollStaggeredGridRenderer<,>).MakeGenericType(self.OwnerEntity?.GetType(), item.OwnerEntity?.GetType());
                self.m_RendererSystemTypeDict.Add(resName, getType);
            }

            return getType;
        }

        private static Type GetItemClickType(this YIUISuperScrollStaggeredGridComponent self, LoopStaggeredGridViewItem item)
        {
            var resName = item.ResName;
            if (!self.m_ClickSystemTypeDict.TryGetValue(resName, out var getType))
            {
                getType = typeof(IYIUISuperScrollStaggeredGridOnClick<,>).MakeGenericType(self.OwnerEntity?.GetType(), item.OwnerEntity?.GetType());
                self.m_ClickSystemTypeDict.Add(resName, getType);
            }

            return getType;
        }

        private static Type GetItemClickCheckType(this YIUISuperScrollStaggeredGridComponent self, LoopStaggeredGridViewItem item)
        {
            var resName = item.ResName;
            if (!self.m_ClickCheckSystemTypeDict.TryGetValue(resName, out var getType))
            {
                getType = typeof(IYIUISuperScrollStaggeredGridOnClickCheck<,>).MakeGenericType(self.OwnerEntity?.GetType(), item.OwnerEntity?.GetType());
                self.m_ClickCheckSystemTypeDict.Add(resName, getType);
            }

            return getType;
        }

        private static Type GetItemSizeSystemType(this YIUISuperScrollStaggeredGridComponent self)
        {
            var ownerType = self.OwnerEntity?.GetType();
            if (ownerType == null) return null;

            var key = ownerType.Name;
            if (!self.m_GetItemSizeSystemTypeDict.TryGetValue(key, out var getType))
            {
                getType = typeof(IYIUISuperScrollStaggeredGridGetItemSize<>).MakeGenericType(ownerType);
                self.m_GetItemSizeSystemTypeDict.Add(key, getType);
            }

            return getType;
        }
    }
}
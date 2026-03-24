//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;
using SuperScrollView;

namespace ET.Client
{
    [FriendOf(typeof(YIUISuperScrollGridComponent))]
    [EntitySystemOf(typeof(YIUISuperScrollGridComponent))]
    public static partial class YIUISuperScrollGridComponentSystem
    {
        [EntitySystem]
        private static void Awake(this YIUISuperScrollGridComponent self, LoopGridView gridView)
        {
            self.m_YIUIBindRef = self.Scene().YIUIBind();
            self.m_OwnerEntity = self.GetParent<Entity>();
            self.m_Owner = gridView;
            var prefabCount = self.Owner.ItemPrefabDataList.Count;
            if (prefabCount <= 0)
            {
                Log.Error($"[{self.Owner.name}]的ItemPrefabDataList为空");
                return;
            }

            if (prefabCount == 1)
            {
                self.Owner.InitGridView(0, self.OnGetOneItemByRowColumn);
            }
            else
            {
                self.Owner.InitGridView(0, self.OnGetOtherItemByRowColumn);
            }

            self.AwakeOnClick();
        }

        static partial void AwakeOnClick(this YIUISuperScrollGridComponent self);

        [EntitySystem]
        private static void Destroy(this YIUISuperScrollGridComponent self)
        {
        }

        //只有一个所以直接使用第一个对象池的数据
        //只有一个的不需要实现GetPrefabIndex
        private static LoopGridViewItem OnGetOneItemByRowColumn(this YIUISuperScrollGridComponent self, LoopGridView gridView, int itemIndex, int row, int column)
        {
            var item = self.OnGetItemByPrefabIndex(gridView, 0);
            if (item == null)
            {
                return null;
            }

            var getType = self.GetItemRendererType(item);
            var select = self.m_OnClickItemHashSet.Contains(itemIndex);
            YIUISuperScrollGridHelper.Renderer(getType, self.OwnerEntity, item.OwnerEntity, self, itemIndex, row, column, select);
            return item;
        }

        //有多个的情况下根据实际返回索引去获取对象池的数据
        private static LoopGridViewItem OnGetOtherItemByRowColumn(this YIUISuperScrollGridComponent self, LoopGridView gridView, int itemIndex, int row, int column)
        {
            var prefabIndex = YIUISuperScrollGridHelper.GetPrefabIndex(self.GetPrefabIndexType, self.OwnerEntity, self, itemIndex, row, column);
            var item = self.OnGetItemByPrefabIndex(gridView, prefabIndex);
            if (item == null)
            {
                return null;
            }

            var getType = self.GetItemRendererType(item);
            var select = self.m_OnClickItemHashSet.Contains(itemIndex);
            YIUISuperScrollGridHelper.Renderer(getType, self.OwnerEntity, item.OwnerEntity, self, itemIndex, row, column, select);
            return item;
        }

        private static LoopGridViewItem OnGetItemByPrefabIndex(this YIUISuperScrollGridComponent self, LoopGridView gridView, int prefabIndex)
        {
            var item = gridView.NewListViewItem(prefabIndex);
            if (item == null)
            {
                return null;
            }

            var ownerEntity = item.OwnerEntity;
            if (ownerEntity == null)
            {
                var resName = item.ResName;
                var vo = self.YIUIBind.GetBindVoByResName(resName);
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

        private static Type GetItemRendererType(this YIUISuperScrollGridComponent self, LoopGridViewItem item)
        {
            var resName = item.ResName;
            if (!self.m_RendererSystemTypeDict.TryGetValue(resName, out var getType))
            {
                getType = typeof(IYIUISuperScrollGridRenderer<,>).MakeGenericType(self.OwnerEntity?.GetType(), item.OwnerEntity?.GetType());
                self.m_RendererSystemTypeDict.Add(resName, getType);
            }

            return getType;
        }
    }
}
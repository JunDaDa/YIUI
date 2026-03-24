//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;
using SuperScrollView;

namespace ET.Client
{
    [FriendOf(typeof(YIUISuperScrollListComponent))]
    [EntitySystemOf(typeof(YIUISuperScrollListComponent))]
    public static partial class YIUISuperScrollListComponentSystem
    {
        [EntitySystem]
        private static void Awake(this YIUISuperScrollListComponent self, LoopListView2 listView2)
        {
            self.m_YIUIBindRef = self.Scene().YIUIBind();
            self.m_OwnerEntity = self.GetParent<Entity>();
            self.m_Owner = listView2;
            var prefabCount = self.Owner.ItemPrefabDataList.Count;
            if (prefabCount <= 0)
            {
                Log.Error($"[{self.Owner.name}]的ItemPrefabDataList为空");
                return;
            }

            if (prefabCount == 1)
            {
                self.Owner.InitListView(0, self.OnGetOneItemByIndex);
            }
            else
            {
                self.Owner.InitListView(0, self.OnGetOtherItemByIndex);
            }

            self.AwakeOnClick();
        }

        static partial void AwakeOnClick(this YIUISuperScrollListComponent self);

        [EntitySystem]
        private static void Destroy(this YIUISuperScrollListComponent self)
        {
        }

        //只有一个所以直接使用第一个对象池的数据
        //只有一个的不需要实现GetPrefabIndex
        private static LoopListViewItem2 OnGetOneItemByIndex(this YIUISuperScrollListComponent self, LoopListView2 listView, int index)
        {
            var item = self.OnGetItemByPrefabIndex(listView, 0);
            if (item == null)
            {
                return null;
            }

            var getType = self.GetItemRendererType(item);
            var select = self.m_OnClickItemHashSet.Contains(index);
            YIUISuperScrollListHelper.Renderer(getType, self.OwnerEntity, item.OwnerEntity, self, index, select);
            return item;
        }

        //有多个的情况下根据实际返回索引去获取对象池的数据
        private static LoopListViewItem2 OnGetOtherItemByIndex(this YIUISuperScrollListComponent self, LoopListView2 listView, int index)
        {
            var prefabIndex = YIUISuperScrollListHelper.GetPrefabIndex(self.GetPrefabIndexType, self.OwnerEntity, self, index);
            var item = self.OnGetItemByPrefabIndex(listView, prefabIndex);
            if (item == null)
            {
                return null;
            }

            var getType = self.GetItemRendererType(item);
            var select = self.m_OnClickItemHashSet.Contains(index);
            YIUISuperScrollListHelper.Renderer(getType, self.OwnerEntity, item.OwnerEntity, self, index, select);
            return item;
        }

        private static LoopListViewItem2 OnGetItemByPrefabIndex(this YIUISuperScrollListComponent self, LoopListView2 listView, int prefabIndex)
        {
            var item = listView.NewListViewItem(prefabIndex);
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

        private static Type GetItemRendererType(this YIUISuperScrollListComponent self, LoopListViewItem2 item)
        {
            var resName = item.ResName;
            if (!self.m_RendererSystemTypeDict.TryGetValue(resName, out var getType))
            {
                getType = typeof(IYIUISuperScrollListRenderer<,>).MakeGenericType(self.OwnerEntity?.GetType(), item.OwnerEntity?.GetType());
                self.m_RendererSystemTypeDict.Add(resName, getType);
            }

            return getType;
        }
    }
}
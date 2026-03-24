//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace ET.Client
{
    public partial class YIUISuperScrollListComponent
    {
        public const string ClickItemEvent = "u_EventClick"; //点击item默认事件名
        public readonly Dictionary<string, bool> m_OnClickInit = new(); //是否已初始化
        public readonly Dictionary<string, string> m_ItemClickEventName = new(); //事件名称
        public readonly Dictionary<string, bool> m_ItemClickCheck = new(); //点击检查
        public readonly Dictionary<string, bool> m_ItemBanSelect = new(); //禁止选择

        public readonly Queue<int> m_OnClickItemQueue = new(); //当前所有已选择 遵循先进先出 有序
        public readonly HashSet<int> m_OnClickItemHashSet = new(); //当前所有已选择 无序 为了更快查找
        public int m_MaxClickCount = 1; //可选最大数量 >=2 就是复选 最小1
        public bool m_RepetitionCancel = true; //重复选择 则取消选择
        public bool m_AutoCancelLast = true; //当选择操作最大数量过后 自动取消第一个选择的 否则选择无效

        public int ItemStart => Owner.GetFirstItemIndex();
        public int ItemEnd => Owner.GetLastItemIndex();

        public readonly Dictionary<string, Type> m_ClickSystemTypeDict = new();
        public readonly Dictionary<string, Type> m_ClickCheckSystemTypeDict = new();
    }
}
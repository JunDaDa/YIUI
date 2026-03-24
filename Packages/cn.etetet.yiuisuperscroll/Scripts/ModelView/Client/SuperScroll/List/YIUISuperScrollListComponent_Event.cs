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
        public readonly Dictionary<string, Type> m_FinishedSystemTypeDict = new();
        public readonly Dictionary<string, Type> m_ChangedSystemTypeDict = new();
        public readonly Dictionary<string, Type> m_MovedSystemTypeDict = new();

        /*
        事件名	                            什么时候回调	                典型用途

        mOnSnapItemFinished	                吸附动画真正结束	            做最终动画、更新页码、上报埋点
        mOnSnapNearestChanged	            吸附动画最近 Item 发生变化	实时高亮、实时预览、提前加载资源
        mOnSmoothMovePanelToItemFinished	平滑滚动动画完成 调用	        滚动结束后的动画、埋点、链式滚动
         */
    }
}
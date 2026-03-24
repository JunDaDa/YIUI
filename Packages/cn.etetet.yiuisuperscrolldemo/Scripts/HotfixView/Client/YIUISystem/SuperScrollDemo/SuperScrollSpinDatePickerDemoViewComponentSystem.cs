using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using SuperScrollView;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2025.8.4
    /// Desc
    /// </summary>
    [FriendOf(typeof(SuperScrollSpinDatePickerDemoViewComponent))]
    [FriendOf(typeof(SuperScrollDemoSpinViewItem1Component))]
    public static partial class SuperScrollSpinDatePickerDemoViewComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollSpinDatePickerDemoViewComponent self)
        {
            self.m_YearListScrollRef = self.AddChild<YIUISuperScrollListComponent, LoopListView2>(self.u_ComScrollViewYear);
            self.m_MonthListScrollRef = self.AddChild<YIUISuperScrollListComponent, LoopListView2>(self.u_ComScrollViewMonth);
            self.m_DayListScrollRef = self.AddChild<YIUISuperScrollListComponent, LoopListView2>(self.u_ComScrollViewDay);

            self.YearListScroll.OnSnapItemFinished();
            self.MonthListScroll.OnSnapItemFinished();
            self.DayListScroll.OnSnapItemFinished();

            self.YearListScroll.OnSnapNearestChanged();
            self.MonthListScroll.OnSnapNearestChanged();
            self.DayListScroll.OnSnapNearestChanged();

            self.YearListScroll.OnSmoothMovePanelToItemFinished();
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollSpinDatePickerDemoViewComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollSpinDatePickerDemoViewComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollSpinDatePickerDemoViewComponent self, ParamVo vo)
        {
            var dataTime = DateTime.Now;

            self.YearListScroll.SetListItemCount(-1);
            int mCurSelectedYear = dataTime.Year;
            int indexYear = mCurSelectedYear - self.m_FirstYear - 1;
            self.YearListScroll.MovePanelToItemIndex(indexYear, 0, 0.5f);

            self.MonthListScroll.SetListItemCount(-1);
            int mCurSelectedMonth = dataTime.Month;
            int indexMonth = mCurSelectedMonth - self.m_FirstMonth - 1;
            self.MonthListScroll.MovePanelToItemIndex(indexMonth, 0, 0.5f);

            self.DayListScroll.SetListItemCount(-1);
            var mCurSelectedDay = dataTime.Day;
            int indexDay = mCurSelectedDay - self.m_FirstDay - 1;
            self.DayListScroll.MovePanelToItemIndex(indexDay, 0, 0.5f);

            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static void YIUISuperScrollListRenderer(this SuperScrollSpinDatePickerDemoViewComponent self, SuperScrollDemoSpinViewItem1Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            if (superScrollList == self.YearListScroll)
            {
                self.RendererYear(item, superScrollList, index);
            }
            else if (superScrollList == self.MonthListScroll)
            {
                self.RendererMonth(item, superScrollList, index);
            }
            else if (superScrollList == self.DayListScroll)
            {
                self.RendererDay(item, superScrollList, index);
            }
            else
            {
                Log.Error($"错误的SuperScrollListRenderer");
            }
        }

        private static void RendererYear(this SuperScrollSpinDatePickerDemoViewComponent self, SuperScrollDemoSpinViewItem1Component item, YIUISuperScrollListComponent superScrollList, int index)
        {
            int val = 0;
            if (index >= 0)
            {
                val = index % self.m_YearCount;
            }
            else
            {
                val = self.m_YearCount + ((index + 1) % self.m_YearCount) - 1;
            }

            val = val + self.m_FirstYear;
            item.Refresh(val.ToString());
        }

        private static void RendererMonth(this SuperScrollSpinDatePickerDemoViewComponent self, SuperScrollDemoSpinViewItem1Component item, YIUISuperScrollListComponent superScrollList, int index)
        {
            int val = 0;
            if (index >= 0)
            {
                val = index % self.m_MonthCount;
            }
            else
            {
                val = self.m_MonthCount + ((index + 1) % self.m_MonthCount) - 1;
            }

            val = val + self.m_FirstMonth;
            item.Refresh(val.ToString());
        }

        private static void RendererDay(this SuperScrollSpinDatePickerDemoViewComponent self, SuperScrollDemoSpinViewItem1Component item, YIUISuperScrollListComponent superScrollList, int index)
        {
            int dayCount = DateTime.DaysInMonth(self.CurSelectedYear, self.CurSelectedMonth);
            int val = 0;
            if (index >= 0)
            {
                val = index % dayCount;
            }
            else
            {
                val = dayCount + ((index + 1) % dayCount) - 1;
            }

            val = val + self.m_FirstDay;
            item.Refresh($"{val:D2}");
        }

        [EntitySystem]
        private static void YIUISuperScrollListFinished(this SuperScrollSpinDatePickerDemoViewComponent self, SuperScrollDemoSpinViewItem1Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            if (superScrollList == self.YearListScroll)
            {
                self.u_DataYear.SetValue(item.u_DataContent.GetValue());
            }
            else if (superScrollList == self.MonthListScroll)
            {
                self.u_DataMonth.SetValue(item.u_DataContent.GetValue());
            }
            else if (superScrollList == self.DayListScroll)
            {
                self.u_DataDay.SetValue(item.u_DataContent.GetValue());
            }
            else
            {
                Log.Error($"错误的SuperScrollListRenderer");
            }
        }

        [EntitySystem]
        private static void YIUISuperScrollListChanged(this SuperScrollSpinDatePickerDemoViewComponent self, SuperScrollDemoSpinViewItem1Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            if (superScrollList == self.YearListScroll)
            {
                self.u_DataYear.SetValue(item.u_DataContent.GetValue());
            }
            else if (superScrollList == self.MonthListScroll)
            {
                self.u_DataMonth.SetValue(item.u_DataContent.GetValue());
            }
            else if (superScrollList == self.DayListScroll)
            {
                self.u_DataDay.SetValue(item.u_DataContent.GetValue());
            }
            else
            {
                Log.Error($"错误的SuperScrollListRenderer");
            }
        }

        [EntitySystem]
        private static void YIUISuperScrollListMoved(this SuperScrollSpinDatePickerDemoViewComponent self, SuperScrollDemoSpinViewItem1Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            Log.Info($"滚动结束");
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}
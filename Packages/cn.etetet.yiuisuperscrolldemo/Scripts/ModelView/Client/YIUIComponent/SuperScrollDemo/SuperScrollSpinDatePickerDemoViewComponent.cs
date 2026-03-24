using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    public partial class SuperScrollSpinDatePickerDemoViewComponent : Entity, IYIUIOpen<ParamVo>
    {
        public EntityRef<YIUISuperScrollListComponent> m_YearListScrollRef;
        public YIUISuperScrollListComponent YearListScroll => m_YearListScrollRef;

        public EntityRef<YIUISuperScrollListComponent> m_MonthListScrollRef;
        public YIUISuperScrollListComponent MonthListScroll => m_MonthListScrollRef;

        public EntityRef<YIUISuperScrollListComponent> m_DayListScrollRef;
        public YIUISuperScrollListComponent DayListScroll => m_DayListScrollRef;

        public int m_FirstYear = 1970;
        public int m_FirstMonth = 1;
        public int m_FirstDay = 1;
        public int m_YearCount = 1000;
        public int m_MonthCount = 12;
        public int m_CurSelectedMonth = 2;
        public int m_CurSelectedDay = 12;
        public int m_CurSelectedYear = 2023;
        public int CurSelectedYear => m_CurSelectedYear;
        public int CurSelectedMonth => m_CurSelectedMonth;
        public int CurSelectedDay => m_CurSelectedDay;
    }
}
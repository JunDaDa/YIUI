using System;
using System.Collections.Generic;
using System.Linq;

//此文件由数值系统自动生成，请不要手动修改
namespace ET
{
    [Invoke(859002049334593)] //最大血量_0(200002),当前血量_0(200001)
    public class NumericAffectInvokeHandler_859002049334593 : AInvokeHandler<NumericAffect, long>
    {
        /*
        当前最大血量改变时
        如果当前血量 > 最大血量
        则当前血量修改为最大血量
         */
        public override long Handle(NumericAffect A)
        {
            if (A.AC > A.N)
            {
                return A.N;
            }

            return A.AC;
        }
    }

    [Invoke(429501024667298)] //等级_0(100001),经验值_0(100002)
    public class NumericAffectInvokeHandler_429501024667298 : AInvokeHandler<NumericAffect, long>
    {
        /*
        只要等级变化
        正常肯定是升级
        就直接修改经验值
         */
        public override long Handle(NumericAffect A)
        {
            return 0;
        }
    }
}
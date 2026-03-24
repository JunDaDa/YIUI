using System;
using System.Linq;
using Unity.Mathematics;
using System.Collections.Generic;

//由Numeric自动生成 请勿修改
namespace ET
{
    [Invoke(0)]
    public class On_Invoke_NumericFormula_Handler_0_0 : AInvokeHandler<Invoke_NumericFormula, long>
    {
        public override long Handle(Invoke_NumericFormula args)
        {
            var data = args.Data;
            var result =
                    (
                        (

                            // 1 + 2
                            data.GetRealValue(args.Bas) + data.GetRealValue(args.Add)
                        )

                        // * 3
                      * (NumericConst.IntRate + data.GetRealValue(args.Pct)) / NumericConst.IntRate

                        // + 4
                      + data.GetRealValue(args.FinalAdd)
                    )

                    // * 5
                  * (NumericConst.IntRate + data.GetRealValue(args.FinalPct)) / NumericConst.IntRate

                    // + 6
                  + data.GetRealValue(args.ResultAdd);

            return result;
        }
    }
}
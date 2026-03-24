namespace ET
{
    [Invoke]
    public class On_Invoke_Numeric_CreateNumericData_Handler : AInvokeHandler<Invoke_Numeric_CreateNumericData, NumericData>
    {
        public override NumericData Handle(Invoke_Numeric_CreateNumericData args)
        {
            return args.Data.CreateNumericData();
        }
    }
}
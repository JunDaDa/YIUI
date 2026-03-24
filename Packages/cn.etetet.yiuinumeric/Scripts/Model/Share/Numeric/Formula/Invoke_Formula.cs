namespace ET
{
    /// <summary>
    /// 数值公式
    /// </summary>
    public struct Invoke_NumericFormula
    {
        public NumericData Data { get; private set; }
        public int ENumericType { get; private set; }
        public int Final { get; private set; }
        public int Bas { get; private set; }
        public int Add { get; private set; }
        public int Pct { get; private set; }
        public int FinalAdd { get; private set; }
        public int FinalPct { get; private set; }
        public int ResultAdd { get; private set; }

        public Invoke_NumericFormula(NumericData data, int numericType)
        {
            Data = data;
            ENumericType = numericType;
            Final = numericType / 10;
            Bas = Final * 10 + 1;
            Add = Final * 10 + 2;
            Pct = Final * 10 + 3;
            FinalAdd = Final * 10 + 4;
            FinalPct = Final * 10 + 5;
            ResultAdd = Final * 10 + 6;
        }
    }
}
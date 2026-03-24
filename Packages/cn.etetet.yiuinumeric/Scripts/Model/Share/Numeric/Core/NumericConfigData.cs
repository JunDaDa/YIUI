namespace ET
{
    public sealed partial class NumericConfigData
    {
        private NumericData m_NumericData;

        public NumericData NumericData
        {
            get
            {
                return m_NumericData ??= EventSystem.Instance.Invoke<Invoke_Numeric_CreateNumericData, NumericData>(new Invoke_Numeric_CreateNumericData(ConfigData));
            }
        }
    }
}
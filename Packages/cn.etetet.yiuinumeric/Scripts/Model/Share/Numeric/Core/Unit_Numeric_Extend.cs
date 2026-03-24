namespace ET
{
    public partial class Unit
    {
        //TODO 以后这里做成分析器
        //给高频使用的组件Component上添加一个特性
        //[自动属性] (之类的) 然后就自动生成下面的代码
        //并且以后Unit GetXXX 就报错 提示已经扩展了特性
        //直接使用.XXX 就行了

        //TODO 胆子再大一点 以后取消GetComponent
        //直接用.XXX 根据现在有的
        //[Component(typeof(XXXComponent))]
        //[Child(typeof(XXXComponent))]
        //分析器自动生成下面的代码 所有Get 都没了
        //缺点肯定是内存会变高 如果能解决这个就更完美了

        #region 数值组件的封装 因为使用频率很高

        private EntityRef<NumericDataComponent> m_Numeric;
        private NumericDataComponent            m_NumericComponent => m_Numeric;

        public NumericDataComponent NumericComponent
        {
            get
            {
                if (m_NumericComponent == null)
                {
                    var numeric = GetComponent<NumericDataComponent>();
                    if (numeric == null)
                    {
                        Log.Error($"没有数值组件 请检查");
                        m_Numeric = default;
                    }
                    else
                    {
                        m_Numeric = numeric;
                    }
                }

                return m_NumericComponent;
            }
        }

        #endregion
    }
}
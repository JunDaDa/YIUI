using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 创建数值数据
    /// </summary>
    public struct Invoke_Numeric_CreateNumericData
    {
        public Dictionary<ENumericType, long> Data { get; private set; }

        public Invoke_Numeric_CreateNumericData(Dictionary<ENumericType, long> data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// 数值改变通知消息
    /// 一般情况不需要监听此消息
    /// 会统一监听然后分发
    /// NumericHandlerAttribute
    /// NumericHandlerDynamicAttribute
    /// 注意这个值不能直接使用 因为涉及到转int float 等等
    /// 请使用扩展方法获取对应的值 NumericChange.cs
    /// </summary>
    public struct NumericChange
    {
        public EntityRef<Entity> _ChangeEntityRef; //NumericDataComponent.Parent
        public Entity _ChangeEntity => _ChangeEntityRef;
        public int _NumericType; //被修改的数值类型ID
        public long _Old; //修改前的值
        public long _New; //修改后的值
    }

    /// <summary>
    /// 数值的GM命令修改消息
    /// </summary>
    public struct NumericGMChange
    {
        public EntityRef<Entity> OwnerEntityRef; //就是NumericDataComponent
        public Entity OwnerEntity => OwnerEntityRef;
        public int ENumericType; //被修改的数值类型ID
        public long Old; //修改前的值
        public long New; //修改后的值
    }

    /// <summary>
    /// 数值影响
    /// </summary>
    public struct NumericAffect
    {
        public NumericData Data; //当前的数值数据
        public int ENumericType; //触发者的数值类型ID
        public long Old; //触发者的修改前的值
        public long New; //触发者的修改后的值
        public int AffectNumericType; //被影响的数值类型ID
        public long AffectCurrent; //被影响的数值的当前值

        public NumericData D => Data;
        public int NT => ENumericType;
        public long O => Old;
        public long N => New;
        public int AT => AffectNumericType;
        public long AC => AffectCurrent;
    }
}
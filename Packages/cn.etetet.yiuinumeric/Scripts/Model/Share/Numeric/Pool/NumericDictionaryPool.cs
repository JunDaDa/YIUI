using System.Collections.Generic;

namespace ET
{
    [EnableClass]
    public class NumericDictionaryPool<K, V> : Dictionary<K, V>, IPool
    {
        public void Dispose()
        {
            this.Clear();
            ObjectPool.Recycle(this);
        }

        public static NumericDictionaryPool<K, V> Create()
        {
            return ObjectPool.Fetch(typeof(NumericDictionaryPool<K, V>)) as NumericDictionaryPool<K, V>;
        }

        public bool IsFromPool { get; set; }
    }
}
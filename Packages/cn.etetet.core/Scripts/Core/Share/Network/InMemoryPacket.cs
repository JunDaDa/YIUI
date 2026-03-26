using System;
using System.Collections.Generic;

namespace ET
{
    public sealed class InMemoryPacket : IDisposable
    {
        [StaticField]
        private static readonly Queue<InMemoryPacket> g_Pool = new();

        [StaticField]
        private static readonly object g_PoolLock = new();

        private byte[] m_Buffer;
        private int m_Length;

        public int Length => m_Length;
        public int Capacity => m_Buffer.Length;

        private InMemoryPacket(int capacity)
        {
            m_Buffer = new byte[capacity];
            m_Length = 0;
        }

        public static InMemoryPacket Rent(int minCapacity = 1400)
        {
            if (InMemoryTransportRegistry.ThreadSafe)
            {
                lock (g_PoolLock)
                {
                    return RentFromPool(minCapacity);
                }
            }

            return RentFromPool(minCapacity);
        }

        public void CopyFrom(byte[] src, int srcOffset, int count)
        {
            if (count > m_Buffer.Length)
            {
                m_Buffer = new byte[count];
            }

            Buffer.BlockCopy(src, srcOffset, m_Buffer, 0, count);
            m_Length = count;
        }

        public int CopyTo(byte[] dst)
        {
            Buffer.BlockCopy(m_Buffer, 0, dst, 0, m_Length);
            return m_Length;
        }

        public void Dispose()
        {
            m_Length = 0;
            if (InMemoryTransportRegistry.ThreadSafe)
            {
                lock (g_PoolLock)
                {
                    g_Pool.Enqueue(this);
                }
            }
            else
            {
                g_Pool.Enqueue(this);
            }
        }

        private static InMemoryPacket RentFromPool(int minCapacity)
        {
            if (g_Pool.Count > 0)
            {
                InMemoryPacket packet = g_Pool.Dequeue();
                if (packet.m_Buffer.Length < minCapacity)
                {
                    packet.m_Buffer = new byte[minCapacity];
                }

                packet.m_Length = 0;
                return packet;
            }

            return new InMemoryPacket(minCapacity);
        }
    }
}

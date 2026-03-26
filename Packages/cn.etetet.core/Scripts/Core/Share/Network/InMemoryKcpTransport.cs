using System.Collections.Generic;
using System.Net;

namespace ET
{
    public sealed class InMemoryKcpTransport : IKcpTransport
    {
        private readonly Queue<InMemoryPacket> m_RecvQueue = new();
        private readonly object m_RecvLock = new();
        private InMemoryMuxTransport m_ClientMux;
        private readonly EndPoint m_SelfEndPoint;

        [StaticField]
        private static readonly EndPoint s_ClientEndPoint = new IPEndPoint(IPAddress.Loopback, 0);

        public InMemoryKcpTransport(EndPoint selfEndPoint)
        {
            m_SelfEndPoint = selfEndPoint;
        }

        public void BindClientMux(InMemoryMuxTransport clientMux)
        {
            m_ClientMux = clientMux;
        }

        public void EnqueueFromClient(InMemoryPacket packet)
        {
            if (InMemoryTransportRegistry.ThreadSafe)
            {
                lock (m_RecvLock)
                {
                    m_RecvQueue.Enqueue(packet);
                }
            }
            else
            {
                m_RecvQueue.Enqueue(packet);
            }
        }

        public void Send(byte[] bytes, int index, int length, EndPoint endPoint, ChannelType channelType)
        {
            InMemoryMuxTransport mux = m_ClientMux;
            if (mux == null)
            {
                return;
            }

            InMemoryPacket packet = InMemoryPacket.Rent(length);
            packet.CopyFrom(bytes, index, length);
            mux.DeliverToClient(packet, m_SelfEndPoint);
        }

        public int Recv(byte[] buffer, ref EndPoint endPoint)
        {
            InMemoryPacket packet;
            if (InMemoryTransportRegistry.ThreadSafe)
            {
                lock (m_RecvLock)
                {
                    if (m_RecvQueue.Count == 0)
                    {
                        return 0;
                    }

                    packet = m_RecvQueue.Dequeue();
                }
            }
            else
            {
                if (m_RecvQueue.Count == 0)
                {
                    return 0;
                }

                packet = m_RecvQueue.Dequeue();
            }

            endPoint = s_ClientEndPoint;
            int length = packet.CopyTo(buffer);
            packet.Dispose();
            return length;
        }

        public int Available()
        {
            if (InMemoryTransportRegistry.ThreadSafe)
            {
                lock (m_RecvLock)
                {
                    return m_RecvQueue.Count;
                }
            }

            return m_RecvQueue.Count;
        }

        public void Update()
        {
        }

        public void OnError(long id, int error)
        {
        }

        public void Dispose()
        {
            m_ClientMux = null;
            if (InMemoryTransportRegistry.ThreadSafe)
            {
                lock (m_RecvLock)
                {
                    while (m_RecvQueue.Count > 0)
                    {
                        m_RecvQueue.Dequeue().Dispose();
                    }
                }
            }
            else
            {
                while (m_RecvQueue.Count > 0)
                {
                    m_RecvQueue.Dequeue().Dispose();
                }
            }
        }
    }
}

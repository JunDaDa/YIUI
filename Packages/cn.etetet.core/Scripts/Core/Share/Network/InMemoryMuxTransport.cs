using System.Collections.Generic;
using System.Net;

namespace ET
{
    public sealed class InMemoryMuxTransport : IKcpTransport
    {
        private readonly Queue<(InMemoryPacket packet, EndPoint from)> m_RecvQueue = new();
        private readonly object m_RecvLock = new();
        private readonly Dictionary<int, InMemoryKcpTransport> m_ServerTransports = new();

        public void AddRoute(int port, InMemoryKcpTransport serverTransport)
        {
            m_ServerTransports[port] = serverTransport;
        }

        public void DeliverToClient(InMemoryPacket packet, EndPoint fromEndPoint)
        {
            if (InMemoryTransportRegistry.ThreadSafe)
            {
                lock (m_RecvLock)
                {
                    m_RecvQueue.Enqueue((packet, fromEndPoint));
                }
            }
            else
            {
                m_RecvQueue.Enqueue((packet, fromEndPoint));
            }
        }

        public void Send(byte[] bytes, int index, int length, EndPoint endPoint, ChannelType channelType)
        {
            int port = ((IPEndPoint)endPoint).Port;
            if (!m_ServerTransports.TryGetValue(port, out InMemoryKcpTransport serverTransport))
            {
                return;
            }

            InMemoryPacket packet = InMemoryPacket.Rent(length);
            packet.CopyFrom(bytes, index, length);
            serverTransport.EnqueueFromClient(packet);
        }

        public int Recv(byte[] buffer, ref EndPoint endPoint)
        {
            (InMemoryPacket packet, EndPoint from) item;
            if (InMemoryTransportRegistry.ThreadSafe)
            {
                lock (m_RecvLock)
                {
                    if (m_RecvQueue.Count == 0)
                    {
                        return 0;
                    }

                    item = m_RecvQueue.Dequeue();
                }
            }
            else
            {
                if (m_RecvQueue.Count == 0)
                {
                    return 0;
                }

                item = m_RecvQueue.Dequeue();
            }

            endPoint = item.from;
            int length = item.packet.CopyTo(buffer);
            item.packet.Dispose();
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
            if (InMemoryTransportRegistry.ThreadSafe)
            {
                lock (m_RecvLock)
                {
                    while (m_RecvQueue.Count > 0)
                    {
                        m_RecvQueue.Dequeue().packet.Dispose();
                    }
                }
            }
            else
            {
                while (m_RecvQueue.Count > 0)
                {
                    m_RecvQueue.Dequeue().packet.Dispose();
                }
            }

            m_ServerTransports.Clear();
        }
    }
}

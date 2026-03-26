using System.Net;
using System.Net.Sockets;

namespace ET.Client
{
    [EnableClass]
    public class LocalNetSessionCreator : INetSessionCreator
    {
        public async ETTask<Session> ConnectRealm(Scene root, string address, string account, string password, int ownerFiberId)
        {
            root.GetComponent<FiberParentComponent>().ParentFiberId = ownerFiberId;

            // Local模式下 UdpTransport 构造函数内部自动委托到 MuxTransport
            root.AddComponent<NetComponent, IKcpTransport>(new UdpTransport(AddressFamily.InterNetwork));
            NetComponent netComponent = root.GetComponent<NetComponent>();

            StartSceneConfig realmConfig = this.GetRealmConfig(account);
            Session session = netComponent.Create(realmConfig.InnerIPPort);
            await ETTask.CompletedTask;
            return session;
        }

        public async ETTask<Session> ConnectGate(Scene root, string gateAddress, string account, string password)
        {
            NetComponent netComponent = root.GetComponent<NetComponent>();
            Session session = netComponent.Create(NetworkHelper.ToIPEndPoint(gateAddress));
            session.AddComponent<PingComponent>();
            await ETTask.CompletedTask;
            return session;
        }

        private StartSceneConfig GetRealmConfig(string account)
        {
            var realms = StartSceneConfigCategory.Instance.GetBySceneType(SceneType.Realm);
            int index = account.Mode(realms.Count);
            return realms[index];
        }
    }
}

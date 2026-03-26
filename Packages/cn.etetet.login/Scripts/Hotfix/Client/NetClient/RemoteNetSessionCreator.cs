using System.Net;

namespace ET.Client
{
    [EnableClass]
    public class RemoteNetSessionCreator : INetSessionCreator
    {
        public async ETTask<Session> ConnectRealm(Scene root, string address, string account, string password, int ownerFiberId)
        {
            root.RemoveComponent<RouterAddressComponent>();
            RouterAddressComponent routerAddressComponent =
                    root.AddComponent<RouterAddressComponent, string>(address);
            await routerAddressComponent.Init();
#if UNITY_WEBGL
            root.AddComponent<NetComponent, IKcpTransport>(new WebSocketTransport(routerAddressComponent.AddressFamily));
#else
            root.AddComponent<NetComponent, IKcpTransport>(new UdpTransport(routerAddressComponent.AddressFamily));
#endif
            root.GetComponent<FiberParentComponent>().ParentFiberId = ownerFiberId;

            NetComponent netComponent = root.GetComponent<NetComponent>();
            IPEndPoint realmAddress = routerAddressComponent.GetRealmAddress(account);

            return await netComponent.CreateRouterSession(realmAddress, account, password);
        }

        public async ETTask<Session> ConnectGate(Scene root, string gateAddress, string account, string password)
        {
            NetComponent netComponent = root.GetComponent<NetComponent>();
            return await netComponent.CreateRouterSession(NetworkHelper.ToIPEndPoint(gateAddress), account, password);
        }
    }
}

namespace ET.Client
{
    public interface INetSessionCreator
    {
        ETTask<Session> ConnectRealm(Scene root, string address, string account, string password, int ownerFiberId);
        ETTask<Session> ConnectGate(Scene root, string gateAddress, string account, string password);
    }

    public static class NetSessionCreator
    {
        [StaticField]
        public static INetSessionCreator Instance;
    }
}

namespace ET
{
    public static class InMemoryTransportRegistry
    {
        [StaticField]
        public static bool ThreadSafe;

        [StaticField]
        private static InMemoryMuxTransport s_ClientMux;

        public static void RegisterMux(InMemoryMuxTransport mux)
        {
            s_ClientMux = mux;
        }

        public static InMemoryMuxTransport GetMux()
        {
            return s_ClientMux;
        }

        public static void Clear()
        {
            s_ClientMux?.Dispose();
            s_ClientMux = null;
        }
    }
}

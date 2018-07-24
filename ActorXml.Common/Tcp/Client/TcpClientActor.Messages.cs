namespace ActorXml.Common.Tcp.Client
{
    public partial class TcpClientActor {
        private class TryConnectMessage { }

        private class CheckAliveMessage { }

        public static class Messages {
            public static object TryConnect() => new TryConnectMessage();
            public static object CheckAlive() => new CheckAliveMessage();
        }
    }
}

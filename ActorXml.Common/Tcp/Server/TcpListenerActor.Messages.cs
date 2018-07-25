using System.Net.Sockets;
using Proto;

namespace ActorXml.Common.Tcp.Server {
    public partial class TcpListenerActor : IActor {
        private class TryHostMessage {
        }

        private class NewTcpClientMessage {
            public TcpClient Client { get; }

            public NewTcpClientMessage(TcpClient client) {
                Client = client;
            }
        }

        private class TcpClientClosedMessage {
        }

        public static class Messages {
            internal static object TryHost() => new TryHostMessage();
            internal static object NewTcpClient(TcpClient client) => new NewTcpClientMessage(client);
            internal static object TcpClientClosed() => new TcpClientClosedMessage();
        }
    }
}
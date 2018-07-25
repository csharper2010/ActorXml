using System;
using System.Xml.Linq;
using Proto;

namespace ActorXml.Common {
    public partial class ActorXmlActor {
        private class GetDeviceInfosMessage {
        }

        private abstract class ActorXmlMessage {
            public XElement Message { get; }

            protected ActorXmlMessage(XElement message) {
                Message = message;
            }
        }

        private class ActorXmlIncomingMessage : ActorXmlMessage {
            public PID SourceClient { get; }

            public ActorXmlIncomingMessage(PID sourceClient, XElement message) : base(message) {
                SourceClient = sourceClient;
            }
        }

        private class ActorXmlOutgoingMessage : ActorXmlMessage {
            public string ClientName { get; }

            public ActorXmlOutgoingMessage(string clientName, XElement message) : base(message) {
                ClientName = clientName;
            }
        }

        private class ActorXmlRequestMessage : ActorXmlMessage {
            public string ClientName { get; }
            public DateTime AllowGarbageCollectAfter { get; }

            public ActorXmlRequestMessage(string clientName, XElement message, DateTime allowGarbageCollectAfter) : base(message) {
                ClientName = clientName;
                AllowGarbageCollectAfter = allowGarbageCollectAfter;
            }
        }

        private class StartTcpListenerMessage {
            public int Port { get; }

            public StartTcpListenerMessage(int port) {
                Port = port;
            }
        }

        private class StartTcpClientMessage {
            public string IPAddress { get; }
            public int Port { get; }

            public StartTcpClientMessage(string ipAddress, int port) {
                IPAddress = ipAddress;
                Port = port;
            }
        }

        private class InitiateHandshakeMessage { }

        public static class Messages {
            public static object GetDeviceInfos() => new GetDeviceInfosMessage();
            public static object IncomingMessage(PID sourceClient, XElement message) => new ActorXmlIncomingMessage(sourceClient, message);
            public static object OutgoingMessage(string clientName, XElement message) => new ActorXmlOutgoingMessage(clientName, message);
            public static object RequestMessage(string clientName, XElement message, DateTime allowGarbageCollectAfter) => new ActorXmlRequestMessage(clientName, message, allowGarbageCollectAfter);

            public static object StartTcpListener(int port) => new StartTcpListenerMessage(port);
            public static object StartTcpClient(string ipAddress, int port) => new StartTcpClientMessage(ipAddress, port);

            public static object InitiateHandshake() => new InitiateHandshakeMessage();
        }
    }
}

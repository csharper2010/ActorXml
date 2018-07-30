using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Proto;
using TcpListenerActor = ActorXml.Common.Tcp.Server.TcpListenerActor;

namespace ActorXml.Common.Tcp {
    public class TcpChannelActor : IActor {
        private readonly TcpClient _client;
        private readonly PID _actorXmlActor;
        private readonly bool _shouldInitiateHandshake;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public TcpChannelActor(TcpClient client, PID actorXmlActor, bool shouldInitiateHandshake) {
            _client = client;
            _actorXmlActor = actorXmlActor;
            _shouldInitiateHandshake = shouldInitiateHandshake;
        }

        public Task ReceiveAsync(IContext context) {
            switch (context.Message) {
                case Started _:
                    //context.SetReceiveTimeout(TimeSpan.FromSeconds(5));
                    context.Spawn(Actor.FromProducer(() => new TcpChannelReadActor(_client, _cts)));

                    if (_shouldInitiateHandshake) {
                        _actorXmlActor.Request(ActorXmlActor.Messages.InitiateHandshake(), context.Self);
                    }
                    break;

                case WriteMessageMessage writeMessage:
                    byte[] buffer = Encoding.UTF8.GetBytes(writeMessage.Message.ToString());
                    _client.GetStream().Write(buffer, 0, buffer.Length);
                    break;

                case MessageReadMessage clientMessageRead:
                    _actorXmlActor.Tell(ActorXmlActor.Messages.IncomingMessage(context.Self, clientMessageRead.Message));
                    break;

                case ClientClosedMessage _:
                    _actorXmlActor.Request(ActorXmlActor.Messages.ChannelClosed(), context.Self);
                    context.Parent.Request(TcpListenerActor.Messages.TcpClientClosed(), context.Self);
                    context.Self.Stop();
                    break;

                case Stopping _:
                    try {
                        _client.Dispose();
                    } catch (Exception e) {
                        Console.WriteLine(e.Message);
                    }
                    break;
            }
            return Actor.Done;
        }

        private class MessageReadMessage {
            public XElement Message { get; }

            public MessageReadMessage(XElement message) {
                Message = message;
            }
        }

        private class WriteMessageMessage {
            public XElement Message { get; }

            public WriteMessageMessage(XElement message) {
                Message = message;
            }
        }

        private class ClientClosedMessage {
        }

        public static class Messages {
            public static object MessageRead(XElement message) => new MessageReadMessage(message);
            public static object WriteMessage(XElement message) => new WriteMessageMessage(message);
            public static object ClientClosed() => new ClientClosedMessage();
        }
    }
}
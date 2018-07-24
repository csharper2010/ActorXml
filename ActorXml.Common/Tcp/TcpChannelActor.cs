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
            if (context.Message is Started) {
                //context.SetReceiveTimeout(TimeSpan.FromSeconds(5));
                context.Spawn(Actor.FromProducer(() => new TcpChannelReadActor(_client, _cts)));

                if (_shouldInitiateHandshake) {
                    _actorXmlActor.Request(ActorXmlActor.Messages.InitiateHandshake(), context.Self);
                }

                return Actor.Done;
            }

            //if (context.Message is ReceiveTimeout) {
            //    context.SetReceiveTimeout(TimeSpan.FromSeconds(5));
            //    Console.WriteLine("ReceiveTimeout");
            //}

            if (context.Message is WriteMessageMessage writeMessage) {
                byte[] buffer = Encoding.UTF8.GetBytes(writeMessage.Message.ToString());
                _client.GetStream().Write(buffer, 0, buffer.Length);
                return Actor.Done;
            }

            if (context.Message is MessageReadMessage clientMessageRead) {
                _actorXmlActor.Tell(ActorXmlActor.Messages.IncomingMessage(context.Self, clientMessageRead.Message));
                return Actor.Done;
            }

            if (context.Message is ClientClosedMessage) {
                _actorXmlActor.Tell(ActorXmlActor.Messages.IncomingMessage(context.Self, new XElement("bye", null)));
                context.Parent.Request(TcpListenerActor.Messages.TcpClientClosed(), context.Self);
                context.Self.Stop();
                return Actor.Done;
            }

            if (context.Message is Stopping) {
                try {
                    _client.Dispose();
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
                return Actor.Done;
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
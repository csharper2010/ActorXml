using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Proto;

namespace ActorXml.Common.Tcp.Client {
    public partial class TcpClientActor : IActor {
        private readonly string _ipAddress;
        private readonly int _port;
        private readonly PID _actorXmlActor;
        private TcpClient _tcpClient;

        public TcpClientActor(string ipAddress, int port, PID actorXmlActor) {
            _ipAddress = ipAddress;
            _port = port;
            _actorXmlActor = actorXmlActor;
        }

        public Task ReceiveAsync(IContext context) {
            // TODO auf welcher Ebene findet der echte regelmäßige PING statt? Ist das von der Anwendung initiiert (z.B. weil auch die Konfiguration dafür in der Anwendung liegt) oder eine Low-Level-Aufgabe
            if (context.Message is Started) {
                context.Tell(context.Self, Messages.TryConnect());
                return Actor.Done;
            }
            if (context.Message is TryConnectMessage) {
                try {
                    Console.WriteLine($"Trying to connect to {_ipAddress}, {_port}...");
                    TcpClient client = new TcpClient(_ipAddress, _port);
                    if (client.Connected) {
                        _tcpClient = client;
                        context.Spawn(Actor.FromProducer(() => new TcpChannelActor(_tcpClient, _actorXmlActor, shouldInitiateHandshake: true)));
                    }
                    context.ReenterAfter(Task.Delay(TimeSpan.FromSeconds(30)), () => context.Tell(context.Self, Messages.CheckAlive()));
                } catch (Exception e) {
                    Console.WriteLine($"Exception {e.Message}, Retry in 5 Seconds");
                    context.ReenterAfter(Task.Delay(TimeSpan.FromSeconds(5)), () => context.Tell(context.Self, Messages.TryConnect()));
                }
                return Actor.Done;
            }
            if (context.Message is CheckAliveMessage) {
                if (_tcpClient.Connected) {
                    Console.WriteLine($"Connection is alive");
                    context.ReenterAfter(Task.Delay(TimeSpan.FromSeconds(30)), () => context.Tell(context.Self, Messages.CheckAlive()));
                } else {
                    try {
                        _tcpClient.Dispose();
                    } catch (Exception e) {
                        Console.WriteLine($"Fehler bei Dispose: {e.Message}");
                    }
                    _tcpClient = null;
                    context.Tell(context.Self, Messages.TryConnect());
                }
                return Actor.Done;
            }
            return Actor.Done;
        }
    }
}

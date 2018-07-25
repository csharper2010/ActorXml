using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Proto;

namespace ActorXml.Common.Tcp.Server {
    public partial class TcpListenerActor {
        private TcpListener _server;

        private PID _acceptActor;

        private int _counter;
        private readonly int _port;
        private readonly PID _actorXmlActor;
        private readonly Dictionary<PID, int> _listenerActors = new Dictionary<PID, int>();

        public TcpListenerActor(int port, PID actorXmlActor) {
            _port = port;
            _actorXmlActor = actorXmlActor;
        }

        public Task ReceiveAsync(IContext context) {
            // TODO auf welcher Ebene findet der echte regelmäßige PING statt? Ist das von der Anwendung initiiert (z.B. weil auch die Konfiguration dafür in der Anwendung liegt) oder eine Low-Level-Aufgabe
            switch (context.Message) {
                case Started _:
                    Console.WriteLine("Receiving Started");
                    context.Tell(context.Self, Messages.TryHost());
                    return Actor.Done;
                case TryHostMessage _:
                    try {
                        IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                        Console.WriteLine($"Trying to host on {localAddr}, {_port}...");

                        // TcpListener server = new TcpListener(port);
                        _server = new TcpListener(localAddr, _port);

                        // Start listening for client requests.
                        _server.Start();

                        _acceptActor = context.Spawn(Actor.FromProducer(() => new AcceptActor()));
                        _acceptActor.Request(_server, context.Self);

                        Console.WriteLine("TryHost Done");
                    } catch (Exception e) {
                        Console.WriteLine($"Exception {e.Message}, Retry in 30 Seconds");
                        context.ReenterAfter(Task.Delay(TimeSpan.FromSeconds(30)), () => context.Tell(context.Self, Messages.TryHost()));
                    }
                    return Actor.Done;
                case NewTcpClientMessage newTcpClientMessage:
                    Console.WriteLine("Receiving NewTcpClient");

                    _acceptActor.Request(_server, context.Self);

                    _listenerActors[context.Spawn(Actor.FromProducer(() => new TcpChannelActor(newTcpClientMessage.Client, _actorXmlActor, false)))] = ++_counter;

                    Console.WriteLine("NewTcpClient Done");

                    return Actor.Done;
                case TcpClientClosedMessage _:
                    Console.WriteLine($"Receiving TcpClientClosedMessage for {context.Sender}");

                    if (_listenerActors.TryGetValue(context.Sender, out int id)) {
                        Console.WriteLine($"Client {id} found, removing it from Dictionary");
                        context.Sender.Stop();
                        _listenerActors.Remove(context.Sender);
                    } else {
                        Console.WriteLine($"Client not found");
                    }

                    Console.WriteLine($"TcpClientClosedMessage Done");
                    return Actor.Done;
                case Stopping _:
                    Console.WriteLine("Receiving Stopping");

                    foreach (var listener in _listenerActors.Keys) {
                        listener.Tell("EXIT");
                    }

                    _acceptActor.Stop();
                    _server?.Stop();
                    _server = null;
                    Console.WriteLine("Stopping done");

                    return Actor.Done;
            }
            return Actor.Done;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Proto;
using ActorXml.Common.Tcp;
using ActorXml.Common.Tcp.Client;
using ActorXml.Common.Tcp.Server;

namespace ActorXml.Common {
    public partial class ActorXmlActor : IActor {
        private readonly Dictionary<PID, DeviceInfo> _deviceInfos = new Dictionary<PID, DeviceInfo>();
        private readonly Action<XElement, DeviceInfo> _callHandler;
        private readonly Func<XElement> _createHelloMessage;

        private readonly IDictionary<int, PID> _tcpListenerActors = new Dictionary<int, PID>();
        private readonly IDictionary<string, PID> _tcpClientActors = new Dictionary<string, PID>();

        private readonly Dictionary<string, (PID resultPid, DateTime allowGarbageCollectAfter)> _openRequests = new Dictionary<string, (PID resultPid, DateTime allowGarbageCollectAfter)>();

        public ActorXmlActor(Action<XElement, DeviceInfo> callHandler, Func<XElement> createHelloMessage) {
            _callHandler = callHandler;
            _createHelloMessage = createHelloMessage;
        }

        public Task ReceiveAsync(IContext context) {
            // TODO Sollen TcpListener und TcpClient über diesen Weg erstellt werden?
            switch (context.Message) {
                case StartTcpListenerMessage startTcpListener:
                    _tcpListenerActors[startTcpListener.Port] = context.Spawn(Actor.FromProducer(() => new TcpListenerActor(startTcpListener.Port, context.Self)));
                    break;

                case StartTcpClientMessage startTcpClient:
                    _tcpClientActors[$"{startTcpClient.IPAddress}:{startTcpClient.Port}"] = context.Spawn(Actor.FromProducer(() => new TcpClientActor(startTcpClient.IPAddress, startTcpClient.Port, context.Self)));
                    break;

                case InitiateHandshakeMessage _:
                    // TODO ist der Handshake über diesen Weg glücklich gewählt?
                    context.Respond(TcpChannelActor.Messages.WriteMessage(_createHelloMessage()));
                    break;

                case GetDeviceInfosMessage _:
                    context.Respond(_deviceInfos.Values.ToArray());
                    break;

                case ActorXmlIncomingMessage incomingMessage:
                    XAttribute idAttribute = incomingMessage.Message.Attribute("id");
                    if (idAttribute != null && _openRequests.TryGetValue(idAttribute.Value, out var record)) {
                        _openRequests.Remove(idAttribute.Value);
                        context.Tell(record.resultPid, incomingMessage.Message);
                    } else {
                        HandleIncomingMessage(incomingMessage);
                    }
                    break;

                case ActorXmlOutgoingMessage outgoingMessage:
                    bool found = false;
                    foreach (var client in _deviceInfos.Where(kvp =>
                        kvp.Value.Name.Equals(outgoingMessage.ClientName, StringComparison.CurrentCultureIgnoreCase))) {
                        client.Key.Tell(TcpChannelActor.Messages.WriteMessage(outgoingMessage.Message));
                        found = true;
                    }
                    if (!found) {
                        Console.WriteLine($"Client {outgoingMessage.ClientName} not found");
                    }
                    break;

                case ActorXmlRequestMessage requestMessage:
                    foreach (var kvp in _openRequests.Where(r => r.Value.allowGarbageCollectAfter < DateTime.Now).ToArray()) {
                        _openRequests.Remove(kvp.Key);
                    }

                    var targetClient = _deviceInfos.FirstOrDefault(kvp =>
                        kvp.Value.Name.Equals(requestMessage.ClientName, StringComparison.CurrentCultureIgnoreCase));
                    if (targetClient.Key == null) {
                        Console.WriteLine($"Client {requestMessage.ClientName} not found");
                    } else {
                        string id = Guid.NewGuid().ToString();
                        _openRequests.Add(id, (context.Sender, requestMessage.AllowGarbageCollectAfter));
                        requestMessage.Message.Add(new XAttribute("id", id));
                        targetClient.Key.Tell(TcpChannelActor.Messages.WriteMessage(requestMessage.Message));
                    }
                    break;
            }
            return Actor.Done;
        }

        private void HandleIncomingMessage(ActorXmlIncomingMessage incomingMessage) {
            switch (incomingMessage.Message.Name.LocalName) {
                case "bye":
                    _deviceInfos.Remove(incomingMessage.SourceClient);
                    return;

                case "hello":
                case "helloResponse":
                    if (!Enum.TryParse(incomingMessage.Message.Attribute("type")?.Value, out DeviceType deviceType)) {
                        Console.Write("Konnte DeviceType nicht ermitteln");
                        break;
                    }
                    XAttribute name = incomingMessage.Message.Attribute("name");
                    if (name == null) {
                        Console.WriteLine("Konnte Namen nicht ermitteln");
                        break;
                    }
                    if (_deviceInfos.ContainsKey(incomingMessage.SourceClient)) {
                        Console.WriteLine("Deviceinfo wird aktualisiert");
                    }
                    _deviceInfos[incomingMessage.SourceClient] = new DeviceInfo(deviceType, name.Value);
                    break;
            }

            if (!_deviceInfos.TryGetValue(incomingMessage.SourceClient, out DeviceInfo deviceInfo)) {
                Console.WriteLine("Device hat sich nicht fachlich angemeldet, Nachricht wird ignoriert");
                return;
            }
            _callHandler(incomingMessage.Message, deviceInfo);
        }
    }

    public class DeviceInfo {
        public DeviceInfo(DeviceType deviceType, string name) {
            DeviceType = deviceType;
            Name = name;
        }

        public DeviceType DeviceType { get; }

        public string Name { get; }
    }

    public enum DeviceType {
        Sichtwahl,
        KS,
        Warenwirtschaft,
    }
}

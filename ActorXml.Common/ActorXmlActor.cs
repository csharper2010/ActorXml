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
        private readonly Action<DeviceInfo> _doHandshake;


        private readonly IDictionary<int, PID> _tcpListenerActors = new Dictionary<int, PID>();
        private readonly IDictionary<string, PID> _tcpClientActors = new Dictionary<string, PID>();

        private readonly Dictionary<string, (PID resultPid, DateTime allowGarbageCollectAfter)> _openRequests = new Dictionary<string, (PID resultPid, DateTime allowGarbageCollectAfter)>();

        public ActorXmlActor(Action<XElement, DeviceInfo> callHandler, Action<DeviceInfo> doHandshake) {
            _callHandler = callHandler;
            _doHandshake = doHandshake;
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
                    DoHandshakeWithDevice(context.Sender);
                    break;

                case ChannelClosedMessage _:
                    _deviceInfos.Remove(context.Sender);
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
                    PID client = outgoingMessage.DeviceInfo.ResponsibleActor;
                    if (!_deviceInfos.ContainsKey(outgoingMessage.DeviceInfo.ResponsibleActor)) {
                        Console.WriteLine($"Client {outgoingMessage.DeviceInfo} not found");
                    } else {
                        client.Tell(TcpChannelActor.Messages.WriteMessage(outgoingMessage.Message));
                        found = true;
                    }
                    if (!found) {
                    }
                    break;

                case ActorXmlOutgoingRequestMessage requestMessage:
                    foreach (var kvp in _openRequests.Where(r => r.Value.allowGarbageCollectAfter < DateTime.Now).ToArray()) {
                        _openRequests.Remove(kvp.Key);
                    }

                    PID targetClient = requestMessage.DeviceInfo.ResponsibleActor;
                    if (!_deviceInfos.ContainsKey(targetClient)) {
                        Console.WriteLine($"Client {requestMessage.DeviceInfo} not found");
                    } else {
                        string id = Guid.NewGuid().ToString();
                        _openRequests.Add(id, (context.Sender, requestMessage.AllowGarbageCollectAfter));
                        requestMessage.Message.Add(new XAttribute("id", id));
                        targetClient.Tell(TcpChannelActor.Messages.WriteMessage(requestMessage.Message));
                    }
                    break;
            }
            return Actor.Done;
        }

        private void HandleIncomingMessage(ActorXmlIncomingMessage incomingMessage) {
            switch (incomingMessage.Message.Name.LocalName) {
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
                    _deviceInfos[incomingMessage.SourceClient] = new DeviceInfo(deviceType, name.Value, incomingMessage.SourceClient);
                    break;
            }

            if (!_deviceInfos.TryGetValue(incomingMessage.SourceClient, out DeviceInfo deviceInfo) || deviceInfo.DeviceType == DeviceType.Unknown) {
                Console.WriteLine("Device hat sich nicht fachlich angemeldet, Nachricht wird ignoriert und ein Handshake versucht");
                DoHandshakeWithDevice(incomingMessage.SourceClient);
            } else {
                _callHandler(incomingMessage.Message, deviceInfo);
            }
        }

        private void DoHandshakeWithDevice(PID client) {
            if (!_deviceInfos.TryGetValue(client, out DeviceInfo deviceInfo)) {
                _deviceInfos[client] = deviceInfo = new DeviceInfo(DeviceType.Unknown, Guid.NewGuid().ToString(), client);
            }
            _doHandshake(deviceInfo);
        }
    }

    public class DeviceInfo {
        internal DeviceInfo(DeviceType deviceType, string name, PID responsibleActor) {
            DeviceType = deviceType;
            Name = name;
            ResponsibleActor = responsibleActor;
        }

        public DeviceType DeviceType { get; }

        public string Name { get; }

        internal PID ResponsibleActor { get; }

        public override string ToString() => $"{DeviceType} {Name} ({ResponsibleActor})";
    }

    public enum DeviceType {
        Unknown,
        Warenwirtschaft,
        KS,
        Sichtwahl,
    }
}

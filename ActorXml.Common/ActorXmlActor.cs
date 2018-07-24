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

        public ActorXmlActor(Action<XElement, DeviceInfo> callHandler, Func<XElement> createHelloMessage) {
            _callHandler = callHandler;
            _createHelloMessage = createHelloMessage;
        }

        public Task ReceiveAsync(IContext context) {
            // TODO Sollen TcpListener und TcpClient über diesen Weg erstellt werden?
            if (context.Message is StartTcpListenerMessage startTcpListener) {
                _tcpListenerActors[startTcpListener.Port] = context.Spawn(Actor.FromProducer(() => new TcpListenerActor(startTcpListener.Port, context.Self)));
                return Actor.Done;
            }
            if (context.Message is StartTcpClientMessage startTcpClient) {
                _tcpClientActors[$"{startTcpClient.IPAddress}:{startTcpClient.Port}"] = context.Spawn(Actor.FromProducer(() => new TcpClientActor(startTcpClient.IPAddress, startTcpClient.Port, context.Self)));
                return Actor.Done;
            }
            if (context.Message is InitiateHandshakeMessage) {
                // TODO ist der Handshake über diesen Weg glücklich gewählt?
                context.Respond(TcpChannelActor.Messages.WriteMessage(_createHelloMessage()));
                return Actor.Done;
            }
            if (context.Message is GetDeviceInfosMessage) {
                context.Respond(_deviceInfos.Values.ToArray());
                return Actor.Done;
            }
            if (context.Message is ActorXmlIncomingMessage incomingMessage) {
                HandleIncomingMessage(incomingMessage);
                return Actor.Done;
            }
            if (context.Message is ActorXmlOutgoingMessage outgoingMessage) {
                bool found = false;
                foreach (var targetClient in _deviceInfos.Where(kvp =>
                    kvp.Value.Name.Equals(outgoingMessage.ClientName, StringComparison.CurrentCultureIgnoreCase))) {
                    targetClient.Key.Tell(TcpChannelActor.Messages.WriteMessage(outgoingMessage.Message));
                    found = true;
                }
                if (!found) {
                    Console.WriteLine($"Client {outgoingMessage.ClientName} not found");
                }
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

using System;
using System.Linq;
using System.Xml.Linq;
using ActorXml.Common;
using Proto;

namespace ActorXml.KS {
    public class ActorXmlKSService : ActorXmlService {
        private readonly ActorXmlDispatcher _actorXmlDispatcher;
        private readonly PID _ksActor;

        public ActorXmlKSService() {
            _actorXmlDispatcher = new ActorXmlDispatcher(DeviceType.KS, "SuperKS");
            _ksActor = Actor.Spawn(Actor.FromProducer(() => new KSActor()));

            _actorXmlDispatcher.AddIncomingMessageHandler("bestand", new Version(), IncomingMessageHandlers.Bestand(_ksActor));
            _actorXmlDispatcher.AddIncomingMessageHandler("auslagerung", new Version(), IncomingMessageHandlers.Auslagerung(_ksActor));
        }

        protected override ActorXmlDispatcher ActorXmlDispatcher => _actorXmlDispatcher;

        public void Start() {
            _actorXmlDispatcher.Start();
            _actorXmlDispatcher.StartTcpListener(13001);
        }

        public void Stop() {
            _actorXmlDispatcher.Stop();
        }

        public bool HasWarenwirtschaft() => GetWarenwirtschaft() != null;

        public DeviceInfo GetWarenwirtschaft() => ActorXmlDispatcher.GetDeviceInfos().FirstOrDefault(i => i.DeviceType == DeviceType.Warenwirtschaft);

        private static class IncomingMessageHandlers {
            public static Action<XElement, DeviceInfo, ActorXmlDispatcher> Bestand(PID ksActor) {
                return (message, device, dispatcher) => {
                    if (!TryGetPzn(message, out var pzn)) {
                        return;
                    }
                    int bestand = ksActor.RequestAsync<int>(KSActor.Messages.Bestandsabfrage(pzn)).Result;
                    dispatcher.Send(device.Name, new XElement("bestandResponse", message.Attributes().Concat(new [] {new XAttribute("menge", bestand)})));
                };
            }

            public static Action<XElement, DeviceInfo, ActorXmlDispatcher> Auslagerung(PID ksActor) {
                return (message, device, dispatcher) => {
                    if (!TryGetPzn(message, out var pzn) || !TryGetMenge(message, out int menge)) {
                        return;
                    }
                    int auslagerungsmenge = ksActor.RequestAsync<int>(KSActor.Messages.Auslagerung(pzn, menge)).Result;
                    dispatcher.Send(device.Name, new XElement("auslagerungResponse", message.Attributes().Concat(new [] {new XAttribute("ausgelagerteMenge", auslagerungsmenge)})));
                };
            }

            private static bool TryGetPzn(XElement message, out int pzn) {
                XAttribute pznAttr;
                if ((pznAttr = message.Attribute("pzn")) == null) {
                    Console.WriteLine("pzn fehlt");
                    pzn = 0;
                    return false;
                }
                if (!int.TryParse(pznAttr.Value, out pzn)) {
                    Console.WriteLine($"pzn {pznAttr.Value} ungültig");
                    return false;
                }
                return true;
            }

            private static bool TryGetMenge(XElement message, out int menge) {
                XAttribute mengeAttr;
                if ((mengeAttr = message.Attribute("menge")) == null) {
                    Console.WriteLine("menge fehlt");
                    menge = 0;
                    return false;
                }
                if (!int.TryParse(mengeAttr.Value, out menge)) {
                    Console.WriteLine($"menge {mengeAttr.Value} ungültig");
                    return false;
                }
                return true;
            }
        }
    }
}

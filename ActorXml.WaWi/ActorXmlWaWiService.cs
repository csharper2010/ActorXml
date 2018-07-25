using System;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using ActorXml.Common;

namespace ActorXml.WaWi {
    public class ActorXmlWaWiService : ActorXmlService, IStartable {
        private readonly ActorXmlDispatcher _actorXmlDispatcher;
        private readonly ArtikelService _artikelService;

        public ActorXmlWaWiService() {
            _actorXmlDispatcher = new ActorXmlDispatcher(DeviceType.Warenwirtschaft, "SuperWaWi");
            _artikelService = new ArtikelService();

            AddIncomingMessageHandlers();
        }

        protected override ActorXmlDispatcher ActorXmlDispatcher => _actorXmlDispatcher;

        public void Start() {
            _actorXmlDispatcher.Start();
            _actorXmlDispatcher.StartTcpListener(13000);
            _actorXmlDispatcher.StartTcpClient("localhost", 13001);
        }

        public void Stop() {
            _actorXmlDispatcher.Stop();
        }

        public bool HasKS() => ActorXmlDispatcher.GetDeviceInfos().Any(i => i.DeviceType == DeviceType.KS);

        public DeviceInfo GetKS() => ActorXmlDispatcher.GetDeviceInfos().FirstOrDefault(i => i.DeviceType == DeviceType.KS);

        public bool HasSichtwahl() => ActorXmlDispatcher.GetDeviceInfos().Any(i => i.DeviceType == DeviceType.Sichtwahl);

        private void AddIncomingMessageHandlers() {
            _actorXmlDispatcher.AddIncomingMessageHandler("getArtikelInfo", new Version(), MessageHandlers.GetArtikelInfo(_artikelService));
            _actorXmlDispatcher.AddIncomingMessageHandler("addArtikel", new Version(), MessageHandlers.AddArtikel);
        }

        public new class MessageFactories : ActorXmlService.MessageFactories {
            // PROGRAMMIERMODELL Request/Response aus der WaWi heraus (zu Benutzen mit Request, optional auch mit Send(r.Request))
            public static RequestHandler<int> Auslagerung(int pzn, int menge) =>
                new RequestHandler<int>(new XElement("auslagerung", new XAttribute("pzn", pzn), new XAttribute("menge", menge)),
                    elem => int.Parse(elem.Attribute("menge")?.Value ?? "0"));

            public static RequestHandler<int?> Bestand(int pzn) =>
                new RequestHandler<int?>(new XElement("bestand", new XAttribute("pzn", pzn)),
                    elem => int.Parse(elem.Attribute("menge")?.Value ?? "0"));
            
            // PROGRAMMIERMODELL FireAndForget aus der WaWi heraus (zu Benutzen mit Send)
            public static XElement NurZurInfo() => new XElement("nurZurInfo");
        }

        private static class MessageHandlers {
            // PROGRAMMIERMODELL Request/Response von der Außenwelt angestoßen
            public static Action<XElement, DeviceInfo, ActorXmlDispatcher> GetArtikelInfo(ArtikelService artikelService) {
                return (message, deviceInfo, ActorXmlService) => {
                    XAttribute pzn;
                    if ((pzn = message.Attribute("pzn")) == null) {
                        Console.WriteLine("pzn fehlt");
                        return;
                    }
                    ActorXmlService.Send(deviceInfo.Name,
                        new XElement("getArtikelInfoResponse", 
                            pzn,
                            new XAttribute("preis", artikelService.GetPreis(pzn.Value))));
                };
            }

            public static void AddArtikel(XElement message, DeviceInfo deviceInfo, ActorXmlDispatcher ActorXmlDispatcher) {
                XAttribute pzn;
                if ((pzn = message.Attribute("pzn")) == null) {
                    Console.WriteLine("pzn fehlt");
                    return;
                }
                ActorXmlDispatcher.Send(deviceInfo.Name,
                    new XElement("addArtikelResponse", pzn, new XAttribute("deviceName", deviceInfo.Name)));
                Thread.Sleep(500);
                ActorXmlDispatcher.Broadcast(DeviceType.Sichtwahl,
                    new XElement("updateVerkäufe", null));
            }
        }
    }
}
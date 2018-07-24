using System;
using System.Threading;
using System.Xml.Linq;
using ActorXml.Common;

namespace ActorXml.WaWi {
    public class ActorXmlWaWiService : ActorXmlServiceBase, IStartable {
        private readonly ActorXmlDispatcher _actorXmlDispatcher;
        private readonly ArtikelService _artikelService;

        public ActorXmlWaWiService() {
            _actorXmlDispatcher = new ActorXmlDispatcher(DeviceType.Warenwirtschaft, "SuperWaWi");
            _artikelService = new ArtikelService();

            _actorXmlDispatcher.AddMessageHandler("getArtikelInfo", new Version(), MessageHandlers.GetArtikelInfo(_artikelService));
            _actorXmlDispatcher.AddMessageHandler("nurZurInfo", new Version(), MessageHandlers.NurZurInfo);
            _actorXmlDispatcher.AddMessageHandler("addArtikel", new Version(), MessageHandlers.AddArtikel);
        }

        private static class MessageHandlers {
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

            public static void NurZurInfo(XElement message, DeviceInfo deviceInfo, ActorXmlDispatcher ActorXmlDispatcher) {
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

        public void Start() {
            _actorXmlDispatcher.Start();
            _actorXmlDispatcher.StartTcpListener(13000);
            _actorXmlDispatcher.StartTcpClient("localhost", 13001);
        }

        public void Stop() {
            _actorXmlDispatcher.Stop();
        }

        protected override ActorXmlDispatcher ActorXmlDispatcher => _actorXmlDispatcher;
    }
}
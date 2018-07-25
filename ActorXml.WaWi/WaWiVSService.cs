using ActorXml.Common;

namespace ActorXml.WaWi {
    public class WaWiVSService {
        private readonly ActorXmlWaWiService _actorXmlService;

        public WaWiVSService(ActorXmlWaWiService actorXmlService) {
            _actorXmlService = actorXmlService;
        }

        public bool IstVirtuelleSichtwahlAngeschlossen() => _actorXmlService.HasSichtwahl();

        public void NurZurInfoAnAlle() => _actorXmlService.Broadcast(DeviceType.Sichtwahl, ActorXmlWaWiService.MessageFactories.NurZurInfo());
    }
}
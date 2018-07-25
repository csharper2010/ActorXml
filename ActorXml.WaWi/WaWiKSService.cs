using System;
using ActorXml.Common;

namespace ActorXml.WaWi {
    public class WaWiKSService {
        private readonly ActorXmlWaWiService _actorXmlService;

        public WaWiKSService(ActorXmlWaWiService actorXmlService) {
            _actorXmlService = actorXmlService;
        }

        public int StarteAuslagerung(int pzn, int menge) {
            DeviceInfo ks = _actorXmlService.GetKS();
            if (ks == null) {
                return 0;
            }
            return _actorXmlService.Request(ks.Name, ActorXmlWaWiService.MessageFactories.Auslagerung(pzn, menge), TimeSpan.FromMilliseconds(300));
        }
    }
}

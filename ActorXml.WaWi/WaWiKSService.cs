using System;
using System.Collections.Generic;
using ActorXml.Common;

namespace ActorXml.WaWi {
    public class WaWiKSService {
        private readonly ActorXmlWaWiService _actorXmlService;

        public WaWiKSService(ActorXmlWaWiService actorXmlService) {
            _actorXmlService = actorXmlService;
        }

        public bool IstKommissioniererAngeschlossen() => _actorXmlService.HasKS();

        public int? GetBestand(int pzn) {
            DeviceInfo ks = _actorXmlService.GetKS();
            if (ks == null) {
                return null;
            }
            return _actorXmlService.Request(ks, ActorXmlWaWiService.MessageFactories.Bestand(pzn), TimeSpan.FromMilliseconds(300));
        }

        public int? DoAuslagerung(int pzn, int menge) {
            DeviceInfo ks = _actorXmlService.GetKS();
            if (ks == null) {
                return null;
            }
            return _actorXmlService.Request(ks, ActorXmlWaWiService.MessageFactories.Auslagerung(pzn, menge), TimeSpan.FromMilliseconds(300));
        }

        public void Async_DoAuslagerung(IEnumerable<int> pzns) {
            DeviceInfo ks = _actorXmlService.GetKS();
            if (ks == null) {
                return;
            }
            foreach (var pzn in pzns) {
                _actorXmlService.Send(ks, ActorXmlWaWiService.MessageFactories.Auslagerung(pzn, 1).Request);
            }
        }
    }
}

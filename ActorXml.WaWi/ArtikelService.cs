using System;

namespace ActorXml.WaWi {
    public class ArtikelService {
        public decimal GetPreis(string pzn) {
            return Math.Abs(pzn.GetHashCode() % 100000 / 100m) + 0.01m;
        }
    }
}

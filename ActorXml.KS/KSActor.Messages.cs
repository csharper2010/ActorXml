namespace ActorXml.KS {
    public partial class KSActor {
        private class BestandsabfrageMessage {
            public int Pzn { get; }

            public BestandsabfrageMessage(int pzn) {
                Pzn = pzn;
            }
        }

        private class AuslagerungMessage {
            public int Pzn { get; }
            public int Menge { get; }

            public AuslagerungMessage(int pzn, int menge) {
                Pzn = pzn;
                Menge = menge;
            }
        }

        public static class Messages {
            public static object Bestandsabfrage(int pzn) => new BestandsabfrageMessage(pzn);
            public static object Auslagerung(int pzn, int menge) => new AuslagerungMessage(pzn, menge);
        }
    }
}
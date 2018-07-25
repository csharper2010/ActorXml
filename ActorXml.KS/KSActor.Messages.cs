namespace ActorXml.KS {
    public partial class KSActor {
        private class BestandsabfrageMessage {
            public int Pzn { get; }

            public BestandsabfrageMessage(int pzn) {
                Pzn = pzn;
            }
        }

        public static class Messages {
            public static object Bestandsabfrage(int pzn) => new BestandsabfrageMessage(pzn);
        }
    }
}
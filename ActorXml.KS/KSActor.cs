using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Proto;

namespace ActorXml.KS {
    public partial class KSActor : IActor {
        private readonly Random _random = new Random();
        private int _timeoutMinMilliseconds = 100;
        private int _timeoutMaxMilliseconds = 350;

        private readonly Dictionary<int, int> _database = new Dictionary<int, int> {
            { 313, 5 },
            { 106, 1 },
        };

        public Task ReceiveAsync(IContext context) {
            switch (context.Message) {
                case BestandsabfrageMessage bestandsabfrage:
                    context.ReenterAfter(
                        Task.Delay(_random.Next(_timeoutMaxMilliseconds - _timeoutMinMilliseconds) + _timeoutMinMilliseconds),
                        () => {
                            if (_database.TryGetValue(bestandsabfrage.Pzn, out int bestand)) {
                                context.Respond(bestand);
                            }
                            context.Respond(0);
                        });
                    break;
            }
            return Actor.Done;
        }
    }
}

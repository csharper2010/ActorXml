using ActorXml.Common;

namespace ActorXml.KS {
    public class ActorXmlKSService : ActorXmlServiceBase {
        private readonly ActorXmlDispatcher _actorXmlDispatcher;

        public ActorXmlKSService() {
            _actorXmlDispatcher = new ActorXmlDispatcher(DeviceType.KS, "SuperKS");
        }

        public void Start() {
            _actorXmlDispatcher.Start();
            _actorXmlDispatcher.StartTcpListener(13001);
        }

        public void Stop() {
            _actorXmlDispatcher.Stop();
        }

        protected override ActorXmlDispatcher ActorXmlDispatcher => _actorXmlDispatcher;
    }
}

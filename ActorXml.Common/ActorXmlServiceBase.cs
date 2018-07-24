using System.Linq;
using System.Xml.Linq;

namespace ActorXml.Common {
    public abstract class ActorXmlServiceBase {
        protected abstract ActorXmlDispatcher ActorXmlDispatcher { get; }

        public bool HasKS() => ActorXmlDispatcher.GetDeviceInfos().Any(i => i.DeviceType == DeviceType.KS);

        public bool HasSichtwahl() => ActorXmlDispatcher.GetDeviceInfos().Any(i => i.DeviceType == DeviceType.Sichtwahl);

        public void Ping(string client) {
            ActorXmlDispatcher.Send(client, new XElement("ping", null));
        }
    }
}

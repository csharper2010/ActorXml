using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Proto;

namespace ActorXml.Common {
    public class ActorXmlDispatcher : IStartable {
        private readonly Func<XElement> _getHelloMessage;
        private PID _actor;
        private readonly IDictionary<string, SortedDictionary<Version, Action<XElement, DeviceInfo, ActorXmlDispatcher>>> _incomingMessageHandlers = new Dictionary<string, SortedDictionary<Version, Action<XElement, DeviceInfo, ActorXmlDispatcher>>>();

        public ActorXmlDispatcher(Func<XElement> getHelloMessage) {
            _getHelloMessage = getHelloMessage;
        }

        public void Start() {
            if (_actor != null) {
                Stop();
            }
            _actor = Actor.Spawn(Actor.FromProducer(() => new ActorXmlActor(HandleIncomingMessage, DoHandshake)));
        }

        public void Stop() {
            _actor.Stop();
        }

        public void StartTcpListener(int port) {
            _actor.Tell(ActorXmlActor.Messages.StartTcpListener(port));
        }

        public void StartTcpClient(string ipAddress, int port) {
            _actor.Tell(ActorXmlActor.Messages.StartTcpClient(ipAddress, port));
        }

        public IEnumerable<DeviceInfo> GetDeviceInfos() {
            return _actor.RequestAsync<IEnumerable<DeviceInfo>>(ActorXmlActor.Messages.GetDeviceInfos()).Result;
        }

        public void Send(DeviceInfo client, XElement xElement) {
            _actor.Tell(ActorXmlActor.Messages.OutgoingMessage(client, xElement));
        }

        public void Broadcast(DeviceType deviceType, XElement xElement) {
            foreach (var device in GetDeviceInfos().Where(d => d.DeviceType == deviceType)) {
                Send(device, xElement);
            }
        }

        public TResult Request<TResult>(DeviceInfo client, XElement request, Func<XElement, TResult> responseHandler, TimeSpan timeout) {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            TResult result;
            try {
                result = responseHandler(_actor.RequestAsync<XElement>(ActorXmlActor.Messages.OutgoingRequestMessage(client, request, DateTime.UtcNow + timeout + TimeSpan.FromSeconds(10)), timeout).Result);
            } catch (Exception e) when (e is TimeoutException || e.InnerException is TimeoutException) {
                Console.WriteLine($"Timeout {timeout} hat angeschlagen");
                result = default(TResult);
            }
            if (Thread.CurrentThread.ManagedThreadId != threadId) {
                Console.WriteLine("Falscher Thread");
            }
            return result;
        }

        public void AddIncomingMessageHandler(string elementName, Version version, Action<XElement, DeviceInfo, ActorXmlDispatcher> action) {
            if (!_incomingMessageHandlers.TryGetValue(elementName, out SortedDictionary<Version, Action<XElement, DeviceInfo, ActorXmlDispatcher>> versionDict)) {
                _incomingMessageHandlers[elementName] = versionDict = new SortedDictionary<Version, Action<XElement, DeviceInfo, ActorXmlDispatcher>>();
            }
            versionDict[version] = action;
        }

        private void HandleIncomingMessage(XElement message, DeviceInfo deviceInfo) {
            if (!_incomingMessageHandlers.TryGetValue(message.Name.LocalName, out var versionDict)) {
                Console.WriteLine($"DEVINFO: Message {message.Name.LocalName} wird nicht behandelt");
            } else {
                // TODO: VersionDict berücksichtigen
                // hier wird in der echten Welt ein neuer Thread erzeugt, BusinessContext aufgebaut und die eigentliche Aufgabe ausgeführt
                ThreadPool.QueueUserWorkItem(_ => versionDict.Values.First().Invoke(message, deviceInfo, this));
            }
        }

        private void DoHandshake(DeviceInfo deviceInfo) {
            Send(deviceInfo, _getHelloMessage());
        }
    }

    public interface IStartable {
        void Start();
        void Stop();
    }
}
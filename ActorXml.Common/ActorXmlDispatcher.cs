using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Proto;

namespace ActorXml.Common {
    public class ActorXmlDispatcher : IStartable {
        private PID _actor;
        private readonly IDictionary<string, SortedDictionary<Version, Action<XElement, DeviceInfo, ActorXmlDispatcher>>> _incomingMessageHandlers = new Dictionary<string, SortedDictionary<Version, Action<XElement, DeviceInfo, ActorXmlDispatcher>>>();

        private readonly DeviceType _ownDeviceType;
        private readonly string _ownDeviceName;

        public ActorXmlDispatcher(DeviceType ownDeviceType, string ownDeviceName) {
            _ownDeviceType = ownDeviceType;
            _ownDeviceName = ownDeviceName;

            AddIncomingMessageHandler("hello", new Version(), MessageHandlers.Hello);
            AddIncomingMessageHandler("ping", new Version(), MessageHandlers.Ping);
        }

        public void Start() {
            if (_actor != null) {
                Stop();
            }
            _actor = Actor.Spawn(Actor.FromProducer(() => new ActorXmlActor(HandleIncomingMessage, () => GetHelloMessage(false))));
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

        public void Send(string client, XElement xElement) {
            _actor.Tell(ActorXmlActor.Messages.OutgoingMessage(client, xElement));
        }

        public void Broadcast(DeviceType deviceType, XElement xElement) {
            foreach (var device in GetDeviceInfos().Where(d => d.DeviceType == deviceType)) {
                Send(device.Name, xElement);
            }
        }

        public TResult Request<TResult>(string client, XElement request, Func<XElement, TResult> responseHandler, TimeSpan timeout) {
            try {
                return responseHandler(_actor.RequestAsync<XElement>(ActorXmlActor.Messages.RequestMessage(client, request, DateTime.UtcNow + timeout + TimeSpan.FromSeconds(10)), timeout).Result);
            } catch (Exception e) when (e is TimeoutException || e.InnerException is TimeoutException) {
                Console.WriteLine($"Timeout {timeout} hat angeschlagen");
                return default(TResult);
            }
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
                ThreadPool.QueueUserWorkItem(_ => versionDict.Values.First().Invoke(message, deviceInfo, this));
            }
        }

        private static class MessageHandlers {
            public static void Hello(XElement message, DeviceInfo deviceInfo, ActorXmlDispatcher ActorXmlDispatcher) {
                ActorXmlDispatcher.Send(deviceInfo.Name, ActorXmlDispatcher.GetHelloMessage(true));
            }

            public static void Ping(XElement message, DeviceInfo deviceInfo, ActorXmlDispatcher ActorXmlDispatcher) {
                ActorXmlDispatcher.Send(deviceInfo.Name, new XElement("pingResponse", message.Attributes()));
            }
        }

        private XElement GetHelloMessage(bool response) => new XElement(response ? "helloResponse" : "hello",
            new XAttribute("type", _ownDeviceType.ToString()), new XAttribute("name", _ownDeviceName));
    }

    public interface IStartable {
        void Start();
        void Stop();
    }
}
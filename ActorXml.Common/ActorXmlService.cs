﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ActorXml.Common {
    public abstract class ActorXmlService {
        private readonly DeviceType _ownDeviceType;
        private readonly string _ownDeviceName;

        protected ActorXmlDispatcher ActorXmlDispatcher { get; }

        protected ActorXmlService(DeviceType ownDeviceType, string ownDeviceName) {
            _ownDeviceType = ownDeviceType;
            _ownDeviceName = ownDeviceName;

            ActorXmlDispatcher = new ActorXmlDispatcher(() => GetHelloMessage(false));

            ActorXmlDispatcher.AddIncomingMessageHandler("hello", new Version(), MessageHandlers.Hello(this));
            ActorXmlDispatcher.AddIncomingMessageHandler("ping", new Version(), MessageHandlers.Ping);
        }

        public void Broadcast(DeviceType deviceType, XElement elem) => ActorXmlDispatcher.Broadcast(deviceType, elem);

        public void Send(DeviceInfo client, XElement elem) => ActorXmlDispatcher.Send(client, elem);

        public TResult Request<TResult>(DeviceInfo client, RequestHandler<TResult> requestHandler, TimeSpan timeout) =>
            ActorXmlDispatcher.Request(client, requestHandler.Request, requestHandler.ResponseHandler, timeout);

        public DeviceInfo GetDevice(string deviceName) {
            return ActorXmlDispatcher.GetDeviceInfos().FirstOrDefault(d => d.Name.Equals(deviceName, StringComparison.CurrentCultureIgnoreCase));
        }

        public IEnumerable<DeviceInfo> GetDevices(DeviceType deviceType) {
            return ActorXmlDispatcher.GetDeviceInfos().Where(d => d.DeviceType == deviceType);
        }

        public class MessageFactories {
            public static XElement Hello(ActorXmlService service) {
                return service.GetHelloMessage(false);
            }
            public static RequestHandler<bool> Ping() => new RequestHandler<bool>(new XElement("ping", null), x => true);
        }

        private XElement GetHelloMessage(bool response) => new XElement(response ? "helloResponse" : "hello",
            new XAttribute("type", _ownDeviceType.ToString()), new XAttribute("name", _ownDeviceName));

        private static class MessageHandlers {
            public static Action<XElement, DeviceInfo, ActorXmlDispatcher> Hello(ActorXmlService service) {
                return (message, deviceInfo, dispatcher) => dispatcher.Send(deviceInfo, service.GetHelloMessage(true));
            }

            public static void Ping(XElement message, DeviceInfo deviceInfo, ActorXmlDispatcher ActorXmlDispatcher) {
                ActorXmlDispatcher.Send(deviceInfo, new XElement("pingResponse", message.Attributes()));
            }
        }

        public class RequestHandler<TResult> {
            public XElement Request { get; }
            public Func<XElement, TResult> ResponseHandler { get; }

            public RequestHandler(XElement request, Func<XElement, TResult> responseHandler) {
                Request = request;
                ResponseHandler = responseHandler;
            }
        }
    }
}

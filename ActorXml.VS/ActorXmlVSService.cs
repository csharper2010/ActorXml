﻿using System;
using System.Linq;
using System.Xml.Linq;
using ActorXml.Common;

namespace ActorXml.VS
{
    public class ActorXmlVSService : ActorXmlService, IStartable {
        private readonly ActorXmlDispatcher _actorXmlDispatcher;

        public ActorXmlVSService(string deviceName) {
            _actorXmlDispatcher = new ActorXmlDispatcher(DeviceType.Sichtwahl, deviceName);

            _actorXmlDispatcher.AddIncomingMessageHandler("nurZurInfo", new Version(), IncomingMessageHandlers.NurZurInfo);
        }

        protected override ActorXmlDispatcher ActorXmlDispatcher => _actorXmlDispatcher;

        public void Start() {
            _actorXmlDispatcher.Start();
            _actorXmlDispatcher.StartTcpClient("localhost", 13000);
        }

        public void Stop() {
            _actorXmlDispatcher.Stop();
        }

        public bool HasWarenwirtschaft() => GetWarenwirtschaft() != null;

        public DeviceInfo GetWarenwirtschaft() => ActorXmlDispatcher.GetDeviceInfos().FirstOrDefault(i => i.DeviceType == DeviceType.Warenwirtschaft);

        private static class IncomingMessageHandlers {
            // PROGRAMMIERMODELL FireAndForget-Nachricht von Außenwelt erhalten
            public static void NurZurInfo(XElement message, DeviceInfo deviceInfo, ActorXmlDispatcher ActorXmlDispatcher) {
            }
        }
    }
}

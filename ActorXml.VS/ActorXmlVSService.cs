using System;
using System.Linq;
using System.Xml.Linq;
using ActorXml.Common;

namespace ActorXml.VS
{
    public class ActorXmlVSService : ActorXmlService, IStartable {
        public ActorXmlVSService(string deviceName)
            : base(DeviceType.Sichtwahl, deviceName) {

            ActorXmlDispatcher.AddIncomingMessageHandler("nurZurInfo", new Version(), IncomingMessageHandlers.NurZurInfo);
        }

        public void Start() {
            ActorXmlDispatcher.Start();
            ActorXmlDispatcher.StartTcpClient("localhost", 13000);
        }

        public void Stop() {
            ActorXmlDispatcher.Stop();
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

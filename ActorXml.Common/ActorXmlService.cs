using System;
using System.Xml.Linq;

namespace ActorXml.Common {
    public abstract class ActorXmlService {
        protected abstract ActorXmlDispatcher ActorXmlDispatcher { get; }

        public void Broadcast(DeviceType deviceType, XElement elem) => ActorXmlDispatcher.Broadcast(deviceType, elem);

        public void Send(string client, XElement elem) => ActorXmlDispatcher.Send(client, elem);

        public TResult Request<TResult>(string client, RequestHandler<TResult> requestHandler, TimeSpan timeout) =>
            ActorXmlDispatcher.Request(client, requestHandler.Request, requestHandler.ResponseHandler, timeout);

        public class MessageFactories {
            public static RequestHandler<bool> Ping() => new RequestHandler<bool>(new XElement("ping", null), x => true);
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

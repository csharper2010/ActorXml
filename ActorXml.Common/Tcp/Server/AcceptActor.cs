using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Proto;

namespace ActorXml.Common.Tcp.Server {
    public class AcceptActor : IActor {
        public async Task ReceiveAsync(IContext context) {
            if (context.Message is TcpListener tcpListener) {
                Console.WriteLine($"Sender: {context.Sender}, waiting for Accept");
                TcpClient client = await tcpListener.AcceptTcpClientAsync();
                context.Respond(TcpListenerActor.Messages.NewTcpClient(client));
            }
        }
    }
}
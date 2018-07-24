using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Connecting to TCP-Server");
            TcpClient client = new TcpClient("127.0.0.1", 13000);
            var cts = new CancellationTokenSource();
            NetworkStream stream = client.GetStream();

            StartReceive(stream, cts, client);

            bool exit = false;
            while (!exit) {
                Console.WriteLine("Erwarte Eingabe");
                string s = Console.ReadLine();

                bool isExit = s?.Equals("EXIT", StringComparison.CurrentCultureIgnoreCase) == true;
                if (isExit) {
                    s = "<bye />";
                }

                Console.WriteLine($"Eingabe {s}");
                byte[] buffer = Encoding.UTF8.GetBytes(s);
                await stream.WriteAsync(buffer, 0, buffer.Length);
                if (isExit) {
                    Console.WriteLine("Exit");
                    cts.Cancel();
                    exit = true;
                    stream.Close();
                }
            }
            Thread.Sleep(1000);
        }

        private static void StartReceive(NetworkStream stream, CancellationTokenSource cts, TcpClient client) {
            Task.Run(async () => {
                await Receive(stream, cts);
                Console.WriteLine("Closing TcpClient");
                client.Close();
                Environment.Exit(0);
            });
        }

        static async Task Receive(NetworkStream stream, CancellationTokenSource cts) {
            var buffer = new byte[8192];
            int read;
            do {
                Console.WriteLine($"Awaiting Read");
                read = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                if (read > 0) {
                    string result = Encoding.UTF8.GetString(buffer, 0, read);
                    Console.WriteLine($"Read {read} Bytes ({result})");
                    if (result?.Equals("EXIT", StringComparison.CurrentCultureIgnoreCase) == true) {
                        return;
                    }
                } else {
                    Console.WriteLine($"Not Read anything");
                }
            } while (read > 0);
        }
    }
}

using System;
using System.Threading;
using ActorXml.WaWi;

namespace Server {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Creating ActorXmlService");
            var service = new ActorXmlWaWiService();
            service.Start();
            bool stop = false;
            while (!stop) {
                string line = Console.ReadLine()?.Trim();

                if (line == null) {
                    continue;
                }

                if (line.Equals("exit", StringComparison.CurrentCultureIgnoreCase)) {
                    stop = true;
                } else {
                    string[] parts = line.Split(" ");
                    switch (parts[0].ToLower()) {
                        case "ping":
                            service.Ping(parts[1]);
                            break;
                        case "devices":
                            Console.WriteLine($"KS: {service.HasKS()}, Sichtwahl: {service.HasSichtwahl()}");
                            break;
                    }
                }
            }
            service.Stop();
            Thread.Sleep(1000);
        }
    }
}

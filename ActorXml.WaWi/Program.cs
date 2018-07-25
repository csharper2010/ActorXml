using System;
using System.Threading;
using ActorXml.Common;

namespace ActorXml.WaWi {
    class Program {
        static void Main(string[] args) {
            var random = new Random();
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
                            Console.WriteLine(service.Request(parts[1], ActorXmlService.MessageFactories.Ping(), TimeSpan.FromTicks(random.Next(100000))));
                            break;
                        case "devices":
                            Console.WriteLine($"KS: {service.HasKS()}, Sichtwahl: {service.HasSichtwahl()}");
                            break;
                        case "bestand":
                            var ks = service.GetKS();
                            if (ks == null) {
                                Console.WriteLine("kein KS");
                            } else {
                                Console.WriteLine(service.Request(ks.Name, ActorXmlWaWiService.MessageFactories.Bestand(int.Parse(parts[1])), TimeSpan.FromSeconds(1)));
                            }
                            break;
                    }
                }
            }
            service.Stop();
            Thread.Sleep(1000);
        }
    }
}

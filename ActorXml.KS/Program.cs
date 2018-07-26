using System;
using System.Threading;
using ActorXml.Common;

namespace ActorXml.KS {
    class Program {
        static void Main() {
            var random = new Random();
            Console.WriteLine("Creating ActorXmlService");
            var service = new ActorXmlKSService();
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
                            Console.WriteLine(service.Request(service.GetDevice(parts[1]), ActorXmlService.MessageFactories.Ping(), TimeSpan.FromTicks(random.Next(100000))));
                            break;
                        case "devices":
                            Console.WriteLine($"Warenwirtschaft: {service.HasWarenwirtschaft()}{(service.GetWarenwirtschaft() != null ? " (" + service.GetWarenwirtschaft().Name + ")" : null)}");
                            break;
                    }
                }
            }
            service.Stop();
            Thread.Sleep(1000);
        }
    }
}

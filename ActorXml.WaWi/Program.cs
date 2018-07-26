using System;
using System.Linq;
using System.Threading;
using ActorXml.Common;

namespace ActorXml.WaWi {
    class Program {
        static void Main() {
            var random = new Random();
            Console.WriteLine("Creating ActorXmlService");
            var rawService = new ActorXmlWaWiService();
            rawService.Start();

            var ksService = new WaWiKSService(rawService);
            var vsService = new WaWiVSService(rawService);

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
                            Console.WriteLine(rawService.Request(rawService.GetDevice(parts[1]), ActorXmlService.MessageFactories.Ping(), TimeSpan.FromTicks(random.Next(100000))));
                            break;
                        case "devices":
                            Console.WriteLine($"KS: {rawService.HasKS()}, Sichtwahl: {rawService.HasSichtwahl()}");
                            break;
                        case "bestand":
                            Console.WriteLine(ksService.GetBestand(int.Parse(parts[1])));
                            break;
                        case "auslagerung":
                            Console.WriteLine(ksService.DoAuslagerung(int.Parse(parts[1]), 1));
                            break;
                        case "auslagerungasync":
                            ksService.Async_DoAuslagerung(parts.Skip(1).Select(int.Parse));
                            Console.WriteLine("Auslagerung gestartet, Ergebnis egal");
                            break;
                        case "info":
                            vsService.NurZurInfoAnAlle();
                            Console.WriteLine("Info an alle VS gestartet, Ergebnis egal");
                            break;
                    }
                }
            }
            rawService.Stop();
            Thread.Sleep(1000);
        }
    }
}

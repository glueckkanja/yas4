using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Mono.Options;
using YaS4Core;

namespace YaS4
{
    internal class Program
    {
        private static readonly CancellationTokenSource Ctc = new CancellationTokenSource();

        private static int Main(string[] args)
        {
            Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Ctc.Cancel();
                };

            var cmd = new Dictionary<string, object>();

            var p = new OptionSet
                {
                    //{
                    //    "src=|source=", "source account credentials (account,key) or 'dev'",
                    //    s => cmd["source"] = ParseAccount(s)
                    //},
                    //{
                    //    "dst=|destination=", "destination account credentials (account,key) or 'dev'",
                    //    s => cmd["destination"] = ParseAccount(s)
                    //},
                    {
                        "h|help", "show this message and exit",
                        s => cmd["help"] = s
                    }
                };

            p.Parse(args);

            if (cmd.ContainsKey("help") && cmd["help"] != null)
            {
                ShowHelp(p);
                return 0;
            }

            //if (cmd.ContainsKey("source") && cmd.ContainsKey("destination"))
            {
                //var src = cmd["source"];
                //var dst = cmd["destination"];

                return MainExec().Result;
            }

            ShowHelp(p);
            return 0;
        }

        private static async Task<int> MainExec()
        {
            Exception ex = null;

            try
            {
                var client = new AmazonS3Client(
                    "",
                    "",
                    RegionEndpoint.EUWest1);

                var ca = new CloudSync(
                    new S3StorageProvider(client, "spielbucket", "bin"),
                    new FileSystemStorageProvider(@"D:\sync"),
                    Ctc.Token);

                ca.ActionFinished +=
                    (sender, e) =>
                        {
                            double kbs = e.Action.Properties.Size/e.Duration.TotalMilliseconds;
                            Console.WriteLine("{0,6:f0} ms - {1,5:f0} KB/s - {2}",
                                              e.Duration.TotalMilliseconds, kbs, e.Action);
                        };

                IList<StorageAction> result = ca.ComputeDestinationActions().Result;

                foreach (StorageAction storageAction in result)
                {
                    Console.WriteLine(storageAction);
                }

                Console.WriteLine("continue to sync");
                Console.ReadLine();

                ca.ExecuteSync(result).Wait();
            }
            catch (Exception e)
            {
                ex = e;
            }

            if (Ctc.IsCancellationRequested)
            {
                Console.WriteLine("User cancelled");
                return 1;
            }

            if (ex != null)
            {
                Console.Error.WriteLine("General error");
                Console.Error.WriteLine(ex);
                return 100;
            }

            return 0;
        }

        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("yas4");
            Console.WriteLine("");
            Console.WriteLine();

            p.WriteOptionDescriptions(Console.Out);
        }
    }
}
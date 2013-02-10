using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.S3;
using YaS4Core;

namespace YaS4
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var client = new AmazonS3Client(
                "",
                "",
                RegionEndpoint.EUWest1);

            var ca = new CloudSync(new FileSystemStorageProvider(@"..\..\..\_stuff\sync"),
                                   new S3StorageProvider(client, "spielbucket", "sync2"));

            IList<StorageAction> result = ca.ComputeDestinationActions().Result;

            foreach (StorageAction storageAction in result)
            {
                Console.WriteLine(storageAction);
            }

            Console.WriteLine("continue to sync");
            Console.ReadLine();

            ca.ExecuteSync(result).Wait();
        }
    }
}
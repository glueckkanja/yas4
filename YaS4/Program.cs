using System;
using System.Collections.Generic;
using System.Linq;
using YaS4Core;

namespace YaS4
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var ca = new CloudSync(new FileSystemStorageProvider(@"..\..\..\_stuff\sync"),
                                   new FileSystemStorageProvider(@"..\..\..\_stuff\sync2"));

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
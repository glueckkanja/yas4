using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace YaS4Core
{
    public class CloudSync
    {
        public CloudSync(IStorageProvider sourceSite, IStorageProvider destinationSite)
        {
            SourceSite = sourceSite;
            DestinationSite = destinationSite;
        }

        public IStorageProvider SourceSite { get; private set; }
        public IStorageProvider DestinationSite { get; private set; }

        public async Task<IList<StorageAction>> ComputeDestinationActions(
            CancellationToken ct = default(CancellationToken))
        {
            IList<FileProperties> source = await SourceSite.ListObjects(ct).ConfigureAwait(false);
            IList<FileProperties> destination = await DestinationSite.ListObjects(ct).ConfigureAwait(false);

            // why src<>dst swapped? - thats actually correct:
            // listdiff works like this: what is my src state (DestinationSite) and 
            // how do i get to my dst state (SourceSite)
            var diff = new ListDiff<FileProperties, FileProperties>(destination, source, (s, d) => Equals(s, d));

            IEnumerable<StorageAction> actions = diff.Actions.Select(StorageAction.Convert);

            return DestinationSite.OptimizeActions(actions).ToList();
        }

        public async Task ExecuteSync(IEnumerable<StorageAction> actions,
                                      CancellationToken ct = default(CancellationToken))
        {
            foreach (StorageAction action in actions)
            {
                if (ct.IsCancellationRequested)
                    break;

                Task task = null;

                if (action.Operation == StorageOperation.Add)
                {
                    task = AddImpl(action, false, ct);
                }
                else if (action.Operation == StorageOperation.Overwrite)
                {
                    task = AddImpl(action, true, ct);
                }
                else if (action.Operation == StorageOperation.Delete)
                {
                    task = DeleteImpl(action, ct);
                }

                if (task != null)
                {
                    await task.ConfigureAwait(false);
                }
            }

            await Task.Run(() => { }).ConfigureAwait(false);
        }

        private async Task AddImpl(StorageAction action, bool overwrite, CancellationToken ct)
        {
            using (Stream src = await SourceSite.ReadObject(action.Properties, ct).ConfigureAwait(false))
            {
                await DestinationSite.AddObject(action.Properties, src, overwrite, ct).ConfigureAwait(false);
            }
        }

        private async Task DeleteImpl(StorageAction action, CancellationToken ct)
        {
            await DestinationSite.DeleteObject(action.Properties, ct).ConfigureAwait(false);
        }
    }
}
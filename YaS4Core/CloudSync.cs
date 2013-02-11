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
        private readonly CancellationToken _ct;

        public CloudSync(IStorageProvider sourceSite, IStorageProvider destinationSite, CancellationToken ct)
        {
            _ct = ct;
            SourceSite = sourceSite;
            DestinationSite = destinationSite;
        }

        public IStorageProvider SourceSite { get; private set; }
        public IStorageProvider DestinationSite { get; private set; }

        public async Task<IList<StorageAction>> ComputeDestinationActions()
        {
            var actions = new List<StorageAction>();

            IList<FileProperties> source = await SourceSite.ListObjects(_ct).ConfigureAwait(false);

            if (_ct.IsCancellationRequested) return actions;

            IList<FileProperties> destination = await DestinationSite.ListObjects(_ct).ConfigureAwait(false);

            if (_ct.IsCancellationRequested) return actions;

            var srcKeys = new HashSet<string>(source.Select(x => x.Key));
            var dstListing = destination.ToDictionary(x => x.Key, x => x);

            foreach (FileProperties src in source)
            {
                FileProperties dst;

                if (!dstListing.TryGetValue(src.Key, out dst))
                {
                    actions.Add(new StorageAction(src, StorageOperation.Add));
                    continue;
                }

                if (src.Key == dst.Key && src.Size == dst.Size && src.Timestamp <= dst.Timestamp)
                {
                    actions.Add(new StorageAction(dst, StorageOperation.Keep));
                }
                else
                {
                    actions.Add(new StorageAction(src, StorageOperation.Overwrite));
                }
            }

            foreach (FileProperties dst in destination)
            {
                if (!srcKeys.Contains(dst.Key))
                {
                    actions.Add(new StorageAction(dst, StorageOperation.Delete));
                }
            }

            return actions
                .OrderByDescending(x => x.Operation == StorageOperation.Delete)
                .ThenBy(x => x.Properties.Size)
                .ToList();
        }

        public async Task ExecuteSync(IEnumerable<StorageAction> actions)
        {
            foreach (StorageAction action in actions)
            {
                if (_ct.IsCancellationRequested)
                    break;

                Task task = null;

                if (action.Operation == StorageOperation.Add)
                {
                    task = AddImpl(action, false);
                }
                else if (action.Operation == StorageOperation.Overwrite)
                {
                    task = AddImpl(action, true);
                }
                else if (action.Operation == StorageOperation.Delete)
                {
                    task = DeleteImpl(action);
                }

                if (task != null)
                {
                    await task.ConfigureAwait(false);
                }
            }

            await Task.Run(() => { }).ConfigureAwait(false);
        }

        private async Task AddImpl(StorageAction action, bool overwrite)
        {
            string path = null;

            try
            {
                Stream src;

                // > 1mb?
                if (action.Properties.Size > Math.Pow(2, 20))
                {
                    path = Path.GetTempFileName();
                    src = File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                }
                else
                {
                    src = new MemoryStream();
                }

                using (src)
                {
                    await SourceSite.ReadObject(action.Properties, src, _ct).ConfigureAwait(false);
                    src.Position = 0;
                    await DestinationSite.AddObject(action.Properties, src, overwrite, _ct).ConfigureAwait(false);
                }
            }
            finally
            {
                if (path != null)
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private async Task DeleteImpl(StorageAction action)
        {
            await DestinationSite.DeleteObject(action.Properties, _ct).ConfigureAwait(false);
        }
    }
}
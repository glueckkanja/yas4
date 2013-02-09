using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace YaS4Core
{
    public abstract class StorageProviderBase : IStorageProvider
    {
        public abstract Task<IList<FileProperties>> ListObjects(CancellationToken ct);
        public abstract Task<IList<FileProperties>> ListObjects(string localKeyPrefix, CancellationToken ct);

        public virtual IEnumerable<StorageAction> MakeActionsAtomic(IEnumerable<StorageAction> actions)
        {
            var act = new List<StorageAction>();

            foreach (StorageAction action in actions)
            {
                if (action.Operation == StorageOperation.Add)
                {
                    int idx = act.FindIndex(x => x.Properties.Key == action.Properties.Key &&
                                                 x.Operation == StorageOperation.Delete);

                    if (idx >= 0)
                    {
                        act.RemoveAt(idx);
                        act.Add(new StorageAction(action.Properties, StorageOperation.Overwrite));
                        continue;
                    }
                }

                act.Add(action);
            }

            return act;
        }

        public abstract Task<Stream> ReadObject(FileProperties properties, CancellationToken ct);
        public abstract Task AddObject(FileProperties properties, Stream stream, bool overwrite, CancellationToken ct);
        public abstract Task DeleteObject(FileProperties properties, CancellationToken ct);
    }
}
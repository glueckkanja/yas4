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

        public virtual IEnumerable<StorageAction> OptimizeActions(IEnumerable<StorageAction> actions)
        {
            var act = new Stack<StorageAction>();

            StorageAction last = default(StorageAction);

            foreach (StorageAction action in actions)
            {
                if (last.Properties.Key == action.Properties.Key &&
                    last.Operation == StorageOperation.Delete &&
                    action.Operation == StorageOperation.Add)
                {
                    act.Pop();
                    act.Push(new StorageAction(action.Properties, StorageOperation.Overwrite));
                }
                else
                {
                    act.Push(action);
                }

                last = action;
            }

            return act.Reverse();
        }

        public abstract Task<Stream> ReadObject(FileProperties properties, CancellationToken ct);
        public abstract Task AddObject(FileProperties properties, Stream stream, bool overwrite, CancellationToken ct);
        public abstract Task DeleteObject(FileProperties properties, CancellationToken ct);
    }
}
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

        public abstract Task ReadObject(FileProperties properties, Stream stream, CancellationToken ct);
        public abstract Task AddObject(FileProperties properties, Stream stream, bool overwrite, CancellationToken ct);
        public abstract Task DeleteObject(FileProperties properties, CancellationToken ct);
    }
}
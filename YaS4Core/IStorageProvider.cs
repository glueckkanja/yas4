using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace YaS4Core
{
    public interface IStorageProvider
    {
        Task<IList<FileProperties>> ListObjects(CancellationToken ct);
        Task<IList<FileProperties>> ListObjects(string localKeyPrefix, CancellationToken ct);

        Task ReadObject(FileProperties properties, Stream stream, CancellationToken ct);
        Task AddObject(FileProperties properties, Stream stream, bool overwrite, CancellationToken ct);
        Task DeleteObject(FileProperties properties, CancellationToken ct);
    }
}
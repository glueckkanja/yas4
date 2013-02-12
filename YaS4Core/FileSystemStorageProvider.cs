using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace YaS4Core
{
    public class FileSystemStorageProvider : StorageProviderBase
    {
        private readonly string _rootPath;

        public FileSystemStorageProvider(string rootPath)
        {
            _rootPath = rootPath;
        }

        public override Task<IList<FileProperties>> ListObjects(CancellationToken ct)
        {
            return ListObjects(null, ct);
        }

        public override Task<IList<FileProperties>> ListObjects(string localKeyPrefix, CancellationToken ct)
        {
            return Task.Run(() => ListObjectImpl(localKeyPrefix, ct));
        }

        public override async Task ReadObject(FileProperties properties, Stream stream, CancellationToken ct)
        {
            string path = ResolvePath(properties.Key);

            using (FileStream file = File.OpenRead(path))
            {
                await file.CopyToAsync(stream);
            }
        }

        public override async Task AddObject(FileProperties properties, Stream stream, bool overwrite,
                                             CancellationToken ct)
        {
            string path = ResolvePath(properties.Key);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            FileMode mode = overwrite ? FileMode.Create : FileMode.CreateNew;

            using (FileStream dst = File.Open(path, mode, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(dst).ConfigureAwait(false);
            }

            File.SetLastWriteTimeUtc(path, properties.Timestamp.UtcDateTime);
        }

        public override Task DeleteObject(FileProperties properties, CancellationToken ct)
        {
            string path = ResolvePath(properties.Key);
            File.Delete(path);
            return Task.FromResult(true);
        }

        private IList<FileProperties> ListObjectImpl(string localKeyPrefix, CancellationToken ct)
        {
            var result = new List<FileProperties>();

            localKeyPrefix = SanitizeKey(localKeyPrefix ?? "");

            if (Directory.Exists(_rootPath))
            {
                foreach (string path in Directory.EnumerateFiles(_rootPath, "*", SearchOption.AllDirectories))
                {
                    if (ct.IsCancellationRequested)
                        break;

                    var info = new FileInfo(path);
                    var prop = new FileProperties
                        {
                            LocalKey = path,
                            Size = info.Length,
                            Key = ResolveKey(path),
                            Timestamp = ResolveTimestamp(path)
                        };

                    if (prop.Key.StartsWith(localKeyPrefix))
                    {
                        result.Add(prop);
                    }
                }
            }

            return result.OrderBy(x => x.Key).ToList();
        }

        private static DateTimeOffset ResolveTimestamp(string path)
        {
            return new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);
        }

        private static string SanitizeKey(string key)
        {
            return key.Replace('\\', '/').Trim().TrimStart('/').Trim();
        }

        private string ResolveKey(string path)
        {
            return SanitizeKey(path.Replace(_rootPath, ""));
        }

        private string ResolvePath(string key)
        {
            return Path.Combine(_rootPath, SanitizeKey(key).Replace('/', '\\'));
        }
    }
}
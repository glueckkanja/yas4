using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace YaS4Core
{
    public class S3StorageProvider : StorageProviderBase
    {
        private const string MetaKeyPrefix = "__yas3/";

        private readonly string _bucket;
        private readonly string _rootKey;
        private readonly AmazonS3 _s3;

        public S3StorageProvider(AmazonS3 s3, string bucket, string rootKey)
        {
            _s3 = s3;
            _bucket = bucket;
            _rootKey = SanitizeKey(rootKey);
        }

        public override Task<IList<FileProperties>> ListObjects(CancellationToken ct)
        {
            return ListObjects(null, ct);
        }

        public override Task<IList<FileProperties>> ListObjects(string localKeyPrefix, CancellationToken ct)
        {
            return Task.Run(() => ListObjectImpl(localKeyPrefix, ct));
        }

        public override async Task<Stream> ReadObject(FileProperties properties, CancellationToken ct)
        {
            string key = ResolveLocalKey(properties.Key);

            FileStream file = File.OpenWrite(Path.GetTempFileName());

            using (var util = new TransferUtility(_s3))
            {
                using (Stream srcStream = await util.OpenStreamAsync(_bucket, key))
                {
                    await srcStream.CopyToAsync(file);
                }
            }

            file.Position = 0;
            return file;
        }

        public override async Task AddObject(FileProperties properties, Stream stream, bool overwrite,
                                             CancellationToken ct)
        {
            string key = ResolveLocalKey(properties.Key);

            using (var util = new TransferUtility(_s3))
            {
                var request = new TransferUtilityUploadRequest
                    {
                        BucketName = _bucket,
                        InputStream = stream,
                        Key = key,
                    };

                request.AddHeader("x-amz-meta-yas4-ts", properties.Timestamp.ToString("o"));

                await util.UploadAsync(request);

                request = new TransferUtilityUploadRequest
                    {
                        BucketName = _bucket,
                        InputStream = new MemoryStream(0),
                        Key = ResolveLocalMetaKey(properties)
                    };

                await util.UploadAsync(request);
            }
        }

        public override Task DeleteObject(FileProperties properties, CancellationToken ct)
        {
            string key = ResolveLocalKey(properties.Key);

            var request = new DeleteObjectsRequest
                {
                    BucketName = _bucket,
                    Keys = {new KeyVersion(key), new KeyVersion(ResolveLocalMetaKey(properties))}
                };

            return _s3.DeleteObjectsAsync(request);
        }

        private async Task<IList<FileProperties>> ListObjectImpl(string localKeyPrefix, CancellationToken ct)
        {
            var result = new List<FileProperties>();

            localKeyPrefix = SanitizeKey(localKeyPrefix ?? "");

            var request = new ListObjectsRequest
                {
                    BucketName = _bucket,
                    Prefix = _rootKey
                };

            IList<S3Object> objects = await _s3.ListAllObjectsAsync(request, ct).ConfigureAwait(false);

            foreach (S3Object obj in objects.Where(x => !x.Key.Contains(MetaKeyPrefix)))
            {
                if (ct.IsCancellationRequested)
                    break;

                S3Object meta = objects.FirstOrDefault(x => MetaKeyToKey(x.Key) == obj.Key);

                if (obj == null && meta != null)
                {
                    // cleanup
                    result.Add(new FileProperties
                        {
                            LocalKey = meta.Key,
                            Size = meta.Size,
                            Key = ResolveKey(meta.Key),
                            Timestamp = GetTimestampFromMeta(meta.Key)
                        });
                }
                else if (obj != null)
                {
                    DateTimeOffset timestamp = meta != null
                                                   ? GetTimestampFromMeta(meta.Key)
                                                   : default(DateTimeOffset);

                    var prop = new FileProperties
                        {
                            LocalKey = obj.Key,
                            Size = obj.Size,
                            Key = ResolveKey(obj.Key),
                            Timestamp = timestamp
                        };

                    if (prop.Key.StartsWith(localKeyPrefix))
                    {
                        result.Add(prop);
                    }
                }
            }

            return result.OrderBy(x => x.Key).ToList();
        }

        private string MetaKeyToKey(string key)
        {
            if (!key.Contains(MetaKeyPrefix))
                return null;

            key = key.Replace(MetaKeyPrefix, "");

            Match match = Regex.Match(key, @" \(([0-9]+)\)$");

            return key.Substring(0, match.Index);
        }

        private DateTimeOffset GetTimestampFromMeta(string key)
        {
            Match match = Regex.Match(key, @" \(([0-9]+)\)$");

            long ticks;
            if (long.TryParse(match.Groups[1].Value, out ticks))
            {
                return new DateTimeOffset(ticks, TimeSpan.Zero);
            }

            return default(DateTimeOffset);
        }

        private static string SanitizeKey(string key)
        {
            return key.Trim().TrimStart('/').Trim();
        }

        private string ResolveKey(string path)
        {
            return SanitizeKey(path.Replace(_rootKey, ""));
        }

        private string ResolveLocalKey(string key)
        {
            return Path.Combine(_rootKey, SanitizeKey(key)).Replace('\\', '/');
        }

        private string ResolveLocalMetaKey(FileProperties properties)
        {
            string key = string.Format("{0} ({1})", properties.Key, properties.Timestamp.UtcTicks);
            return Path.Combine(_rootKey, Path.Combine(MetaKeyPrefix, key)).Replace('\\', '/');
        }
    }
}
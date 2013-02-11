using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace YaS4Core
{
    public class S3StorageProvider : StorageProviderBase
    {
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

            FileStream file = File.Open(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None);

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

            var request = new TransferUtilityUploadRequest
                {
                    BucketName = _bucket,
                    InputStream = stream,
                    Key = key
                };

            request.AddHeader("x-amz-meta-yas4-ts", properties.Timestamp.ToString("o"));

            using (var util = new TransferUtility(_s3))
            {
                await util.UploadAsync(request);
            }
        }

        public override Task DeleteObject(FileProperties properties, CancellationToken ct)
        {
            string key = ResolveLocalKey(properties.Key);

            var request = new DeleteObjectsRequest
                {
                    BucketName = _bucket,
                    Keys = {new KeyVersion(key)}
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
                    Prefix = _rootKey + "/"
                };

            IList<S3Object> objects = await _s3.ListAllObjectsAsync(request, ct).ConfigureAwait(false);

            foreach (S3Object obj in objects)
            {
                if (ct.IsCancellationRequested)
                    break;

                DateTimeOffset timestamp = DateTimeOffset.Parse(obj.LastModified);

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

            return result.OrderBy(x => x.Key).ToList();
        }

        private static string SanitizeKey(string key)
        {
            return key.Trim().TrimStart('/').Trim();
        }

        private string ResolveKey(string path)
        {
            return SanitizeKey(path.Substring(_rootKey.Length));
        }

        private string ResolveLocalKey(string key)
        {
            return Path.Combine(_rootKey, SanitizeKey(key)).Replace('\\', '/');
        }
    }
}
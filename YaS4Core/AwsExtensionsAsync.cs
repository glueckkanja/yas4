using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace YaS4Core
{
    public static class AwsExtensionsAsync
    {
        public static Task<Stream> OpenStreamAsync(
            this TransferUtility utility, string bucketName, string key)
        {
            IAsyncResult ar = utility.BeginOpenStream(bucketName, key, null, null);
            return Task.Factory.FromAsync<Stream>(ar, utility.EndOpenStream);
        }

        public static Task UploadAsync(
            this TransferUtility utility, TransferUtilityUploadRequest request)
        {
            IAsyncResult ar = utility.BeginUpload(request, null, null);
            return Task.Factory.FromAsync(ar, utility.EndUpload);
        }

        public static Task<GetObjectResponse> GetObjectAsync(
            this AmazonS3 client, GetObjectRequest request)
        {
            IAsyncResult ar = client.BeginGetObject(request, null, null);
            return Task.Factory.FromAsync<GetObjectResponse>(ar, client.EndGetObject);
        }

        public static Task<DeleteObjectsResponse> DeleteObjectsAsync(
            this AmazonS3 client, DeleteObjectsRequest request)
        {
            IAsyncResult ar = client.BeginDeleteObjects(request, null, null);
            return Task.Factory.FromAsync<DeleteObjectsResponse>(ar, client.EndDeleteObjects);
        }

        public static Task<ListObjectsResponse> ListObjectsAsync(
            this AmazonS3 client, ListObjectsRequest request)
        {
            IAsyncResult ar = client.BeginListObjects(request, null, null);
            return Task.Factory.FromAsync<ListObjectsResponse>(ar, client.EndListObjects);
        }

        public static async Task<IList<S3Object>> ListAllObjectsAsync(
            this AmazonS3 client, ListObjectsRequest request, CancellationToken ct)
        {
            string token = null;
            var result = new List<S3Object>();

            do
            {
                var req = new ListObjectsRequest
                    {
                        BucketName = request.BucketName,
                        Delimiter = request.Delimiter,
                        InputStream = request.InputStream,
                        MaxKeys = request.MaxKeys,
                        Prefix = request.Prefix,
                        Marker = token
                    };

                using (ListObjectsResponse response = await client.ListObjectsAsync(req).ConfigureAwait(false))
                {
                    result.AddRange(response.S3Objects);
                    token = response.NextMarker;
                }
            } while (token != null && !ct.IsCancellationRequested);

            return result;
        }
    }
}
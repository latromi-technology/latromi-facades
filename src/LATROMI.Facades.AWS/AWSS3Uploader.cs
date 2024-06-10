using Amazon.S3.Transfer;
using Amazon.S3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LATROMI.Facades.AWS
{
    public class AWSS3Uploader
    {
        private string _accessKeyId;
        private string _secretAccessKey;
        private string _bucketName;
        private string _region;

        public void SetCredenditals(string accessKeyId, string secretAccessKey)
        {
            if (string.IsNullOrWhiteSpace(accessKeyId))
            {
                throw new ArgumentException($"'{nameof(accessKeyId)}' cannot be null or whitespace.", nameof(accessKeyId));
            }

            if (string.IsNullOrWhiteSpace(secretAccessKey))
            {
                throw new ArgumentException($"'{nameof(secretAccessKey)}' cannot be null or whitespace.", nameof(secretAccessKey));
            }

            _accessKeyId = accessKeyId;
            _secretAccessKey = secretAccessKey;
        }

        public void SetBucket(string bucketName)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                throw new ArgumentException($"'{nameof(bucketName)}' cannot be null or whitespace.", nameof(bucketName));
            }

            _bucketName = bucketName;
        }

        public void SetRegion(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentException($"'{nameof(region)}' cannot be null or whitespace.", nameof(region));
            }

            _region = region;
        }

        public string UploadFromFile(string filePath, string fileKey = null)
        {
            using (var uploadStream = File.OpenRead(filePath))
            {
                return Upload(uploadStream, fileKey);
            }
        }

        public string Upload(Stream fileStream, string fileKey = null)
        {
            if (fileStream is null)
            {
                throw new ArgumentNullException(nameof(fileStream));
            }

            if (string.IsNullOrEmpty(fileKey))
            {
                fileKey = Guid.NewGuid().ToString("N");
            }

            EnsureCredentialsSpecified();
            EnsureBucketSpecified();
            EnsureRegionSpecified();

            try
            {
                // Obtém Endpoint da região
                var regionEndPoint = Amazon.RegionEndpoint.GetBySystemName(_region);

                // Cria um objeto de cliente AmazonS3
                using (var client = new AmazonS3Client(_accessKeyId, _secretAccessKey, regionEndPoint))
                using (var transferUtility = new TransferUtility(client)) // Cria um objeto TransferUtility
                {
                    // Faz o upload do arquivo para o Amazon S3
                    transferUtility.Upload(fileStream, _bucketName, fileKey);
                }
                return fileKey;
            }
            catch (AmazonS3Exception)
            {
                throw;
            }
        }

        public int Delete(string fileKey)
        {
            if (string.IsNullOrEmpty(fileKey))
            {
                throw new ArgumentException($"'{nameof(fileKey)}' cannot be null or empty.", nameof(fileKey));
            }

            EnsureCredentialsSpecified();
            EnsureBucketSpecified();
            EnsureRegionSpecified();

            // Obtém Endpoint da região
            var regionEndPoint = Amazon.RegionEndpoint.GetBySystemName(_region);

            // Cria um objeto de cliente AmazonS3
            using (var client = new AmazonS3Client(_accessKeyId, _secretAccessKey, regionEndPoint))
            {
                // Remove o arquivo do Amazon S3
#if NET48
                var r = client.DeleteObject(_bucketName, fileKey);
#else
                var r = client.DeleteObjectAsync(_bucketName, fileKey).Result;
#endif
                return (int)r.HttpStatusCode;
            }
        }

        private void EnsureCredentialsSpecified()
        {
            if (_accessKeyId is null)
            {
                throw new InvalidOperationException("Credentials are not provided");
            }
        }

        private void EnsureBucketSpecified()
        {
            if (_bucketName is null)
            {
                throw new InvalidOperationException("Bucket name is not provided");
            }
        }

        private void EnsureRegionSpecified()
        {
            if (_region is null)
            {
                throw new InvalidOperationException("Region is not provided");
            }
        }
    }
}

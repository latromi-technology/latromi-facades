using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;

namespace LATROMI.Facades.Azure
{
    public class AzureBlobUploader
    {
        private string _accountName;
        private string _accountKey;
        private string _containerName;

        public void SetCredentials(string accountName, string accountKey)
        {
            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new ArgumentException($"'{nameof(accountName)}' cannot be null or whitespace.", nameof(accountName));
            }

            if (string.IsNullOrWhiteSpace(accountKey))
            {
                throw new ArgumentException($"'{nameof(accountKey)}' cannot be null or whitespace.", nameof(accountKey));
            }

            _accountName = accountName;
            _accountKey = accountKey;
        }

        public void SetContainer(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException($"'{nameof(containerName)}' cannot be null or whitespace.", nameof(containerName));
            }

            _containerName = containerName;
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
            EnsureContainerSpecified();

            try
            {
                var blobUri = $"https://{_accountName}.blob.core.windows.net";
                var credential = new StorageSharedKeyCredential(_accountName, _accountKey);
                var blobServiceClient = new BlobServiceClient(new Uri(blobUri), credential);
                var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

                containerClient.CreateIfNotExists(PublicAccessType.None);

                var blobClient = containerClient.GetBlobClient(fileKey);

                blobClient.Upload(fileStream, overwrite: true);

                return fileKey;
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Erro ao fazer upload para o Azure Blob Storage.", ex);
            }
        }

        public int Delete(string fileKey)
        {
            if (string.IsNullOrEmpty(fileKey))
            {
                throw new ArgumentException($"'{nameof(fileKey)}' cannot be null or empty.", nameof(fileKey));
            }

            EnsureCredentialsSpecified();
            EnsureContainerSpecified();

            var blobUri = $"https://{_accountName}.blob.core.windows.net";
            var credential = new StorageSharedKeyCredential(_accountName, _accountKey);
            var blobServiceClient = new BlobServiceClient(new Uri(blobUri), credential);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileKey);

            var result = blobClient.DeleteIfExists();

            return result ? 204 : 404;
        }

        private void EnsureCredentialsSpecified()
        {
            if (_accountName is null || _accountKey is null)
            {
                throw new InvalidOperationException("Azure Blob Storage credentials are not provided");
            }
        }

        private void EnsureContainerSpecified()
        {
            if (_containerName is null)
            {
                throw new InvalidOperationException("Container name is not provided");
            }
        }
    }
}

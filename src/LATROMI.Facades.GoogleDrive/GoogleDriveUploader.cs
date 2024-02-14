using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using HeyRed.Mime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace LATROMI.Facades.GoogleDrive
{
    public class GoogleDriveUploader
    {
        private GoogleCredential _credential;
        private IList<string> _parentIds;

        public void LoadCredentialFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentException($"'{nameof(json)}' cannot be null or empty.", nameof(json));
            }

            _credential = GoogleCredential.FromJson(json)
                .CreateScoped(DriveService.ScopeConstants.Drive);
        }

        public void LoadCredentialFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or empty.", nameof(filePath));
            }

            _credential = GoogleCredential.FromFile(filePath)
                .CreateScoped(DriveService.ScopeConstants.Drive);
        }

        public void SetFolder(params string[] foldersIds)
        {
            _parentIds = new List<string>(foldersIds);
        }

        public string UploadFromFile(string filePath, string fileName = null, string contentType = null)
        {
            using (var uploadStream = File.OpenRead(filePath))
            {
                return Upload(uploadStream,
                    string.IsNullOrEmpty(fileName) ? Path.GetFileName(filePath): fileName,
                    string.IsNullOrEmpty(contentType) ? MimeTypesMap.GetMimeType(filePath): contentType
                    );
            }
        }

        public string Upload(Stream fileStream, string fileName, string contentType)
        {
            if (fileStream is null)
            {
                throw new ArgumentNullException(nameof(fileStream));
            }

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException($"'{nameof(fileName)}' cannot be null or empty.", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                throw new ArgumentException($"'{nameof(contentType)}' cannot be null or whitespace.", nameof(contentType));
            }

            EnsureCredentialsSpecified();

            using (var service = new DriveService(
                new BaseClientService.Initializer { HttpClientInitializer = _credential }))
            {
                var driveFile = new Google.Apis.Drive.v3.Data.File { Name = fileName };

                // Informa as pastas
                if (_parentIds?.Count > 0)
                {
                    driveFile.Parents = new List<string>(_parentIds);
                }

                var insertRequest = service.Files.Create(driveFile, fileStream, contentType);
                // Permite "Pastas" e "Drives Compartilhados"
                insertRequest.SupportsAllDrives = true;

                // Tenta fazer o upload
                var result = insertRequest.Upload();

                if (result.Status == UploadStatus.Failed)
                {
                    // Dispara a exception
                    throw result.Exception;
                }
                else
                {
                    // Retorna o ID do arquivo
                    return insertRequest.ResponseBody.Id;
                }
            }
        }

        private void EnsureCredentialsSpecified()
        {
            if (_credential is null)
            {
                throw new InvalidOperationException("Credentials are not provided");
            }
        }
    }
}

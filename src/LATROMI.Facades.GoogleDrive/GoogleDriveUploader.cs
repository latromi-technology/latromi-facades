using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using HeyRed.Mime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace LATROMI.Facades.GoogleDrive
{
    public class GoogleDriveUploader
    {
        private const string NullOrEmptyMessage = "'{0}' cannot be null or empty.";
        private const string NullOrWhiteSpaceMessage = "'{0}' cannot be null or whitespace.";

        private const string LatromiShared = @"C:\LATROMI\shared";
        private const string PathStore = "LATROMI.G.AUTH";

        private readonly string[] _scopes = new string[] { DriveService.ScopeConstants.Drive };
        private readonly Regex _typeFinderRegex = new Regex("\"type\":", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private IHttpExecuteInterceptor _credential;
        private IList<string> _parentIds;

        public void LoadCredentialFromJson(string json)
            => LoadCredentialInternal(json);

        public void LoadCredentialFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException(string.Format(NullOrEmptyMessage, nameof(filePath)), nameof(filePath));

            string content = string.Empty;
            using (StreamReader reader = new StreamReader(filePath))
                content = reader.ReadToEnd();

            LoadCredentialInternal(content);
        }

        private void LoadCredentialInternal(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentException(string.Format(NullOrEmptyMessage, nameof(content)), nameof(content));

            bool typeFound = _typeFinderRegex.IsMatch(content);

            if (typeFound)
            {
                _credential = GoogleCredential.FromJson(content)
                    .CreateScoped(_scopes);
            }
            else
            {
                EnsureMetadataDirectory();

                byte[] buffer = Encoding.UTF8.GetBytes(content);
                string fullPathStore = Path.Combine(LatromiShared, PathStore);

                using (Stream credStream = new MemoryStream(buffer))
                {
                    _credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(credStream).Secrets,
                        _scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(fullPathStore)
                    ).GetAwaiter().GetResult();
                }
            }
        }

        public void SetFolder(params string[] foldersIds)
            => _parentIds = new List<string>(foldersIds);

        public string UploadFromFile(string filePath, string fileName = null, string contentType = null)
        {
            string parentId = _parentIds?.Count > 0 ? _parentIds[0] : null;
            return UploadFromFile(filePath, fileName, contentType, parentId);
        }

        public string UploadFromFile(string filePath, string fileName = null, string contentType = null, string parentId = null)
        {
            using (var stream = File.OpenRead(filePath))
            {
                return NewFile(stream,
                    string.IsNullOrEmpty(fileName) ? Path.GetFileName(filePath) : fileName,
                    string.IsNullOrEmpty(contentType) ? MimeTypesMap.GetMimeType(filePath) : contentType,
                    parentId
                );
            }
        }

        public string UploadFromFolder(string folderPath, string parentId = null)
        {
            if (string.IsNullOrEmpty(folderPath))
                throw new ArgumentNullException(string.Format(NullOrEmptyMessage, nameof(folderPath)), nameof(folderPath));

            List<string> idsUploadeds = new List<string>();
            var directoryEntries = Directory.EnumerateFileSystemEntries(folderPath);
            foreach (var entryPath in directoryEntries)
            {
                FileAttributes attr = File.GetAttributes(entryPath);

                if (attr.HasFlag(FileAttributes.Directory))
                {
                    var driveFolder = NewFolder(entryPath.Substring(entryPath.LastIndexOf('\\') + 1), parentId);
                    idsUploadeds.Add(driveFolder);

                    if (UploadFromFolder(entryPath, driveFolder) is string ids && !string.IsNullOrEmpty(ids))
                        idsUploadeds.AddRange(ids.Split(','));
                }
                else
                {
                    using (Stream stream = File.OpenRead(entryPath))
                        return NewFile(stream, Path.GetFileName(entryPath), MimeTypesMap.GetMimeType(entryPath), parentId);
                }
            }

            return string.Join(",", idsUploadeds);
        }

        public string NewFolder(string name, string parentId = null)
        {
            EnsureCredentialsSpecified();

            if (TryGetFolder(name, parentId) is string folderId && folderId != null)
                return folderId;

            var folderMetadata = CreateGoogleMetadata(name, "application/vnd.google-apps.folder", parentId);

            using (var service = new DriveService(new BaseClientService.Initializer() { HttpClientInitializer = (IConfigurableHttpClientInitializer)_credential }))
            {
                var request = service.Files.Create(folderMetadata);
                request.SupportsAllDrives = true;
                request.Fields = "id";

                var folder = request.Execute();

                return folder.Id;
            }
        }

        public string NewFolders(string folderPath, string parentId = null)
        {
            folderPath = folderPath.Replace("\\", "/");
            string[] folders = folderPath.Split('/');
            List<string> createdFolders = new List<string>(folders.Length);
            string currentFolderId = parentId;

            foreach (var folder in folders)
            {
                currentFolderId = NewFolder(folder, currentFolderId);
                createdFolders.Add(currentFolderId);
            }

            return string.Join("/", createdFolders);
        }

        public string NewFile(Stream stream, string fileName, string contentType, string parentId = null)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            EnsureCredentialsSpecified();

            var fileMetadata = CreateGoogleMetadata(fileName, contentType, parentId);

            using (var service = new DriveService(
                new BaseClientService.Initializer { HttpClientInitializer = (IConfigurableHttpClientInitializer)_credential }))
            {
                var updateRequest = service.Files.Create(fileMetadata, stream, contentType);
                updateRequest.SupportsAllDrives = true;
                updateRequest.Fields = "id";

                var response = updateRequest.Upload();

                if (response.Status == UploadStatus.Failed)
                    throw response.Exception;

                return updateRequest.ResponseBody.Id;
            }
        }

        [Obsolete]
        public string Upload(Stream fileStream, string fileName, string contentType)
        {
            if (fileStream is null)
                throw new ArgumentNullException(nameof(fileStream));
            if (fileName is null)
                throw new ArgumentException(string.Format(NullOrEmptyMessage, nameof(fileName)), nameof(fileName));
            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException(string.Format(NullOrWhiteSpaceMessage, nameof(contentType)), nameof(contentType));

            EnsureCredentialsSpecified();

            using (var service = new DriveService(
                new BaseClientService.Initializer { HttpClientInitializer = (IConfigurableHttpClientInitializer)_credential }))
            {
                var driveFile = new Google.Apis.Drive.v3.Data.File { Name = fileName };

                // Informa as pastas
                if (_parentIds?.Count > 0)
                    driveFile.Parents = new List<string>(_parentIds);

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

        private string TryGetFolder(string name, string parentId = null)
        {
            StringBuilder queryBuilder = new StringBuilder();

            queryBuilder.Append($"mimeType = 'application/vnd.google-apps.folder' and name = '{name}' and trashed = false");

            if (!string.IsNullOrEmpty(parentId))
                queryBuilder.Append($" and '{parentId}' in parents");

            using (var service = new DriveService(new BaseClientService.Initializer() { HttpClientInitializer = (IConfigurableHttpClientInitializer)_credential }))
            {
                var queryRequest = service.Files.List();
                queryRequest.SupportsAllDrives = true;
                queryRequest.Fields = "files(id)";
                queryRequest.PageSize = 1;
                queryRequest.Q = queryBuilder.ToString();

                var response = queryRequest.Execute();

                if (response.Files?.Count > 0)
                    return response.Files[0].Id;

                return null;
            }
        }

        private void EnsureCredentialsSpecified()
        {
            if (_credential is null)
            {
                throw new InvalidOperationException("Credentials are not provided.");
            }
        }

        private void EnsureMetadataDirectory()
            => Directory.CreateDirectory(LatromiShared);

        private Google.Apis.Drive.v3.Data.File CreateGoogleMetadata(string name, string mimeType, string parentId)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(string.Format(NullOrEmptyMessage, nameof(name)), nameof(name));
            if (string.IsNullOrWhiteSpace(mimeType))
                throw new ArgumentException(string.Format(NullOrWhiteSpaceMessage, nameof(mimeType)), nameof(mimeType));

            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = name,
                MimeType = mimeType
            };

            if (!string.IsNullOrEmpty(parentId))
                fileMetadata.Parents = new List<string>() { parentId };

            return fileMetadata;
        }
    }
}

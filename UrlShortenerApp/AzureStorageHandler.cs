using Azure.Storage.Blobs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace UrlShortenerApp
{
    public class AzureStorageHandler
    {
        private readonly string _connectionString;
        private readonly string _containerName;

        public AzureStorageHandler(string connectionString, string containerName)
        {
            _connectionString = connectionString;
            _containerName = containerName;
        }

        public async Task UploadFileAsync(string blobName, string filePath)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();
            var blobClient = containerClient.GetBlobClient(blobName);
            using FileStream uploadFileStream = File.OpenRead(filePath);
            await blobClient.UploadAsync(uploadFileStream, overwrite: true);
        }

        public async Task DownloadFileAsync(string blobName, string downloadFilePath)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            var response = await blobClient.DownloadAsync();
            using FileStream downloadFileStream = File.OpenWrite(downloadFilePath);
            await response.Value.Content.CopyToAsync(downloadFileStream);
        }

        public async Task<bool> BlobExistsAsync(string blobName)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            return await blobClient.ExistsAsync();
        }
    }
} 
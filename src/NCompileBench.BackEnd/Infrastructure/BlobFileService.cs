using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NCompileBench.Backend.Infrastructure
{
    public class BlobFileService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<BlobFileService> _logger;

        public BlobFileService(BlobServiceClient blobServiceClient, IConfiguration configuration, ILogger<BlobFileService> logger)
        {
            // _containerName = configuration["Storage:Container"];
            
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        private BlobClient GetBlobClient(string blobName, string containerName)
        {
            var containerClient = GetContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            return blobClient;
        }

        private BlobContainerClient GetContainerClient(string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            return containerClient;
        }

        public static Stream CreateStreamFromString(string @string)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(@string);
            writer.Flush();
            stream.Position = 0;

            return stream;
        }

        public async Task<string> GetFileContentsAsync(string blobName, string containerName)
        {
            var client = GetBlobClient(blobName, containerName);

            await using var stream = await client.OpenReadAsync();
            using var reader = new StreamReader(stream);

            var fileContent = await reader.ReadToEndAsync();

            return fileContent;
        }

        public async Task<string> SaveBlob(string blobName, string containerName, string contents, Dictionary<string, string> metadata = null)
        {
            var client = GetBlobClient(blobName, containerName);
            
            try
            {
                // Save the result with result details as metadata
                await using var stream = CreateStreamFromString(contents);

                await client.UploadAsync(
                    stream,
                    new BlobHttpHeaders { ContentType = "application/json" },
                    conditions: null, metadata: metadata);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to store file with name {BlobName}", blobName);

                try
                {
                    // Try to store without metadata
                    await using var stream = CreateStreamFromString(contents);
                    await client.UploadAsync(
                        stream,
                        new BlobHttpHeaders { ContentType = "application/json" },
                        conditions: null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to store file with name {BlobName} and without metadata. Abandon the result", blobName);

                    throw;
                }
            }
            
            return client.Uri.ToString();
        }

        public async IAsyncEnumerable<string> GetContainerFiles([EnumeratorCancellation] CancellationToken cancellationToken, string containerName)
        {
            var client = GetContainerClient(containerName);

            var blobs = client.GetBlobsAsync(cancellationToken: cancellationToken);

            await foreach (var blob in blobs)
            {
                yield return blob.Name;
            }
        }
    }
}

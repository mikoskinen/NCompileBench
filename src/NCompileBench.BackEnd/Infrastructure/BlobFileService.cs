using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using NCompileBench.Shared;

namespace NCompileBench.BackEnd
{
    public class BlobFileService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public BlobFileService(BlobServiceClient blobServiceClient, IConfiguration configuration)
        {
            _containerName = configuration["Storage:Container"];
            _blobServiceClient = blobServiceClient;
        }

        private BlobClient GetBlobClient(string blobName)
        {
            var containerClient = GetContainerClient();
            var blobClient = containerClient.GetBlobClient(blobName);

            return blobClient;
        }

        private BlobContainerClient GetContainerClient()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

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

        public async Task<string> GetFileContentsAsync(string blobName)
        {
            var client = GetBlobClient(blobName);

            await using var stream = await client.OpenReadAsync();
            using var reader = new StreamReader(stream);

            var fileContent = await reader.ReadToEndAsync();

            return fileContent;
        }

        public async Task WriteFileContentsAsync(string blobName, string contents)
        {
            var client = GetBlobClient(blobName);

            await using var stream = CreateStreamFromString(contents);
            await client.UploadAsync(stream, true);
        }
        
        public async IAsyncEnumerable<string> GetContainerFiles([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var client = GetContainerClient();

            var blobs = client.GetBlobsAsync(cancellationToken: cancellationToken);

            await foreach (var blob in blobs)
            {
                yield return blob.Name;
            }
        }
    }
}

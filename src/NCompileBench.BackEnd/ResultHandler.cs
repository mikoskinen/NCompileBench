using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCompileBench.Backend.Infrastructure;
using NCompileBench.Shared;
using Newtonsoft.Json;
using Weikio.EventFramework.Abstractions;

namespace NCompileBench.Backend
{
    public class ResultHandler
    {
        private readonly Decryptor _decryptor;
        private readonly ILogger<ResultHandler> _logger;
        private readonly BlobFileService _blobFileService;

        public ResultHandler(Decryptor decryptor, ILogger<ResultHandler> logger, BlobFileService blobFileService)
        {
            _decryptor = decryptor;
            _logger = logger;
            _blobFileService = blobFileService;
        }

        public async Task Handle(CloudEvent<Result> cloudEvent)
        {
            _logger.LogDebug("Handling new event {CloudEvent}", cloudEvent);

            try
            {
                // Get the result from the decrypted attributes
                var encryptedKey = cloudEvent.GetAttributes().Single(x => x.Key == EncryptedKeyCloudEventExtension.EncryptedKeyExtension).Value.ToString();
                var encryptedResult = cloudEvent.GetAttributes().Single(x => x.Key == EncryptedResultCloudEventExtension.EncryptedResultExtension).Value.ToString();

                try
                {
                    var plainResultString = _decryptor.Decrypt(encryptedResult, encryptedKey);
                    var result = JsonConvert.DeserializeObject<Result>(plainResultString);

                    _logger.LogDebug("Decrypted results OK");
                    var fileName = result.ToFileName();

                    _logger.LogDebug("Generated file name {FileName} from result", fileName);
                    await _blobFileService.WriteFileContentsAsync(fileName, plainResultString);
                    
                    _logger.LogDebug("Successfully stored result with file name {FileName}", fileName);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to decrypt result from encrypted key {EncryptedKey} and encrypted result {EncryptedResult}", encryptedKey, encryptedResult);
                    throw new Exception("Failed to decrypt result", e);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to handle event");

                throw;
            }
        }
    }
}

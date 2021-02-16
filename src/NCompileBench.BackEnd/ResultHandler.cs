using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NCompileBench.Backend.Infrastructure;
using NCompileBench.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Weikio.EventFramework.Abstractions;

namespace NCompileBench.Backend
{
    public class ResultHandler
    {
        private readonly Decryptor _decryptor;
        private readonly ILogger<ResultHandler> _logger;
        private readonly BlobFileService _blobFileService;
        private readonly IConfiguration _configuration;

        public ResultHandler(Decryptor decryptor, ILogger<ResultHandler> logger, BlobFileService blobFileService, IConfiguration configuration)
        {
            _decryptor = decryptor;
            _logger = logger;
            _blobFileService = blobFileService;
            _configuration = configuration;
        }

        public async Task Handle(CloudEvent<Result> cloudEvent)
        {
            _logger.LogDebug("Handling new event {CloudEvent}", cloudEvent);

            try
            {
                // Get the result from the decrypted attributes
                var encryptedKey = cloudEvent.GetAttributes().Single(x => x.Key == EncryptedKeyCloudEventExtension.EncryptedKeyExtension).Value.ToString();

                var encryptedResult = cloudEvent.GetAttributes().Single(x => x.Key == EncryptedResultCloudEventExtension.EncryptedResultExtension).Value
                    .ToString();

                // In the future we may get multiple results, one for each supported platform
                List<Result> results;

                try
                {
                    var plainResultsString = _decryptor.Decrypt(encryptedResult, encryptedKey);
                    results = JsonConvert.DeserializeObject<List<Result>>(plainResultsString);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to decrypt result from encrypted key {EncryptedKey} and encrypted result {EncryptedResult}", encryptedKey,
                        encryptedResult);

                    throw new Exception("Failed to decrypt result", e);
                }

                _logger.LogDebug("Decrypted results OK. Received results for {PlatformCount} different platforms", results.Count);

                // Save each result separately
                foreach (var result in results)
                {
                    var fileName = result.ToFileName();

                    _logger.LogDebug("Generated file name {FileName} from result", fileName);

                    // Store score and details as metadata to allow easier search in the future
                    var (sanitizedMetadata, originalMetadata) = GetMetadata(result);

                    var resultFileName = await SaveResult(fileName, JsonConvert.SerializeObject(result, Formatting.Indented), sanitizedMetadata);
                    await SaveMetadata(fileName, originalMetadata, resultFileName);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to handle event");

                throw;
            }
        }

        private async Task<string> SaveResult(string fileName, string plainResultString, Dictionary<string, string> resultMetadata)
        {
            var resultFileName = "";
            try
            {
                resultFileName = await _blobFileService.SaveBlob(fileName, _configuration["Storage:ResultsContainer"], plainResultString, resultMetadata);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to store file with name {BlobName} and with metadata. Try saving without metadata", fileName);

                try
                {
                    resultFileName = await _blobFileService.SaveBlob(fileName, _configuration["Storage:ResultsContainer"], plainResultString);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to store file with name {BlobName} and without metadata. Abandon result", fileName);

                    throw;
                }
            }
            
            _logger.LogDebug("Successfully stored result with file name {FileName}", resultFileName);

            return resultFileName;
        }
        
        private async Task SaveMetadata(string fileName, Dictionary<string, string> resultMetadata, string resultFileName)
        {
            var metadataFileName = fileName.Replace("result_", "meta_");

            try
            {
                // Also save the metadata separately, we may need this in the future for searching the result
                resultMetadata.Add("originalresultfile", resultFileName);
                var json = JsonConvert.SerializeObject(resultMetadata, Formatting.Indented);

                await _blobFileService.SaveBlob(metadataFileName, _configuration["Storage:MetaContainer"], json);
                
                _logger.LogDebug("Successfully stored result metadata with file name {FileName}", fileName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to store metadata with name {BlobName}. Allow to fail", metadataFileName);
            }
        }

        private (Dictionary<string, string> Sanitized, Dictionary<string, string> Original) GetMetadata(Result scoreResult)
        {
            var jObject = JObject.Parse(JsonConvert.SerializeObject(scoreResult));

            // https://stackoverflow.com/a/35838986/66988
            var jTokens = jObject.Descendants().Where(p => !p.Any()).ToList();

            // This will flatten the result for an easier searching
            
            // We need to remove all the special characters when the result is added to blob as metadata
            var sanitized = jTokens.Aggregate(new Dictionary<string, string>(), (properties, jToken) =>
            {
                properties.Add(Uri.EscapeDataString(jToken.Path.Replace(".", "_").Replace("[", "_").Replace("]", "_")).Trim(),  Uri.EscapeDataString(jToken.ToString().Replace(" ", "_")).Trim());

                return properties;
            });

            // But we also want to have the original (flattened) metadata
            var original = jTokens.Aggregate(new Dictionary<string, string>(), (properties, jToken) =>
            {
                properties.Add(jToken.Path,  jToken.ToString());

                return properties;
            });

            return (sanitized, original);
        }
    }
}

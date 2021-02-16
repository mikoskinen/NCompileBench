using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NCompileBench.Backend.Infrastructure;
using NCompileBench.Shared;
using Newtonsoft.Json;

namespace NCompileBench.Backend
{
    public class ResultApi
    {
        private readonly BlobFileService _blobFileService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ResultApi> _logger;

        public ResultApi(BlobFileService blobFileService, IConfiguration configuration, ILogger<ResultApi> logger)
        {
            _blobFileService = blobFileService;
            _configuration = configuration;
            _logger = logger;
        }

        public async IAsyncEnumerable<ResultSummary> GetTopResults()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var count = 0;

            await foreach (var blobFileName in _blobFileService.GetContainerFiles(cancellationTokenSource.Token, _configuration["Storage:ResultsContainer"]))
            {
                var resultSummary = blobFileName.ToResultSummary();

                yield return resultSummary;

                count += 1;

                if (count > 500)
                {
                    cancellationTokenSource.Cancel();
                }
            }
        }

        public async Task<ActionResult<Result>> GetDetails(string fileName)
        {
            string json = null;

            try
            {
                json = await _blobFileService.GetFileContentsAsync(fileName, _configuration["Storage:ResultsContainer"]);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get file with file name {FileName}", fileName);

                return new NotFoundResult();
            }

            var result = JsonConvert.DeserializeObject<Result>(json);

            return result;
        }
    }
}

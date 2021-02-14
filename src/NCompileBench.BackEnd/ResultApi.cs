using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NCompileBench.Backend.Infrastructure;
using NCompileBench.Shared;
using Newtonsoft.Json;

namespace NCompileBench.Backend
{
    public class ResultApi
    {
        private readonly BlobFileService _blobFileService;
        private readonly IConfiguration _configuration;

        public ResultApi(BlobFileService blobFileService, IConfiguration configuration)
        {
            _blobFileService = blobFileService;
            _configuration = configuration;
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
        
        public async Task<Result> GetDetails(string fileName)
        {
            var json = await _blobFileService.GetFileContentsAsync(fileName, _configuration["Storage:ResultsContainer"]);
            var result = JsonConvert.DeserializeObject<Result>(json);

            return result ;
        }
    }
}

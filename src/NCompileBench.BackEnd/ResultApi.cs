using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NCompileBench.Shared;
using Newtonsoft.Json;
using Weikio.EventFramework.Abstractions;

namespace NCompileBench.BackEnd
{
    public class ResultApi
    {
        private readonly BlobFileService _blobFileService;

        public ResultApi(BlobFileService blobFileService)
        {
            _blobFileService = blobFileService;
        }

        public async IAsyncEnumerable<ResultSummary> GetTopResults()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var count = 0;
            
            await foreach (var blobFileName in _blobFileService.GetContainerFiles(cancellationTokenSource.Token))
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
            var json = await _blobFileService.GetFileContentsAsync(fileName);
            var result = JsonConvert.DeserializeObject<Result>(json);

            return result ;
        }
    }
}

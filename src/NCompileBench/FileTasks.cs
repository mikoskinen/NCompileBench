using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace NCompileBench
{
    public static class FileTasks
    {
        public static async Task Unzip(string zipPath, string expandedDirPath, bool deleteZippedFiles = true, string tempTarPath = null)
        {
            await UnWinZip(zipPath, expandedDirPath);

            if (deleteZippedFiles)
            {
                File.Delete(zipPath);
            }
        }

        public static async Task UnWinZip(string zipPath, string expandedDirPath)
        {
            using (var zipStream = File.OpenRead(zipPath))
            {
                var zip = new ZipArchive(zipStream);

                foreach (var entry in zip.Entries)
                {
                    if (entry.CompressedLength == 0)
                    {
                        continue;
                    }

                    var extractedFilePath = Path.Combine(expandedDirPath, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(extractedFilePath));

                    using (var zipFileStream = entry.Open())
                    {
                        using (var extractedFileStream = File.OpenWrite(extractedFilePath))
                        {
                            await zipFileStream.CopyToAsync(extractedFileStream);
                        }
                    }
                }
            }
        }
    }
}

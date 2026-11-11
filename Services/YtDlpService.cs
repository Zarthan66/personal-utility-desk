using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PrototypePT1.Services
{
    public static class YtDlpService
    {
        public static async Task<string> DownloadVideoAsync(string url, string saveDir, string filename, string extension, int threads, IProgress<string> progress)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(filename)) filename = "%(title)s";
                    if (string.IsNullOrWhiteSpace(extension)) extension = "%(ext)s";

                    string outputTemplate = $"{filename}.{extension}";

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "yt-dlp",
                        Arguments = $"\"{url}\" -o \"{saveDir}\\{outputTemplate}\" -N {threads}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using var process = new Process { StartInfo = startInfo };

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            progress?.Report(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            progress?.Report($"Error: {e.Data}");
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await process.WaitForExitAsync();

                    return process.ExitCode == 0 ? "Download completed successfully!" : "Download finished with errors.";
                }
                catch (Exception ex)
                {
                    return $"Download failed: {ex.Message}";
                }
            });
        }
    }
}
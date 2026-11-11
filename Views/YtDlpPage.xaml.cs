using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PrototypePT1.Models;
using PrototypePT1.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace PrototypePT1.Views
{
    public sealed partial class YtDlpPage : Page
    {
        public YtDlpPage()
        {
            InitializeComponent();

            var settings = AppSettingService.LoadSettings();
            if (!string.IsNullOrWhiteSpace(settings.VideoDownloaderSaveDir))
            {
                DirTextBox.Text = settings.VideoDownloaderSaveDir;
            }
            UrlTextBox.TextChanged += UrlTextBox_TextChanged;
            DirTextBox.TextChanged += Inputs_TextChanged;
            UpdateDownloadButtonState();

            if (CheckInstallation("yt-dlp"))
            {
                InstallYtdlpButton.Visibility = Visibility.Collapsed;
                StatusTextBlock.Text = "yt-dlp is installed and ready to use.";
            }
            else
            {
                if (CheckInstallation("winget"))
                {
                    StatusTextBlock.Text = "yt-dlp is not installed or cannot be found in system PATH.";
                    InstallYtdlpButton.Visibility = Visibility.Visible;
                }
                else
                {
                    StatusTextBlock.Text = "yt-dlp is not installed and winget is required for download button.";
                    LearnWingetButton.Visibility = Visibility.Visible;
                }
            }
        }

        private void Inputs_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDownloadButtonState();
        }

        private void UpdateDownloadButtonState()
        {
            bool isUrlValid = CheckUrlValidation();
            bool isDirValid = !string.IsNullOrWhiteSpace(DirTextBox?.Text) && Directory.Exists(DirTextBox.Text.Trim());
            DownloadVideoButton.IsEnabled = isUrlValid && isDirValid;
        }

        private async void LearnWingetButton_Click(object sender, RoutedEventArgs e)
        {
            var winGetDocsUri = new Uri("https://learn.microsoft.com/en-us/windows/package-manager/winget/");
            await Windows.System.Launcher.LaunchUriAsync(winGetDocsUri);
        }

        private async void InstallYtdlpButton_Click(object sender, RoutedEventArgs e)
        {
            InstallYtdlpButton.IsEnabled = false;
            StatusTextBlock.Text = "Installing yt-dlp...";

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c winget install --id yt-dlp.yt-dlp -e --accept-source-agreements --accept-package-agreements & timeout /t 10 & exit",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                }

                if (CheckInstallation("yt-dlp"))
                {
                    StatusTextBlock.Text = "yt-dlp is installed and ready to use.";
                    InstallYtdlpButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    StatusTextBlock.Text = "Installation completed, but yt-dlp was not detected. Try restarting the app.";
                    InstallYtdlpButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Installation failed: {ex.Message}";
                InstallYtdlpButton.IsEnabled = true;
            }
        }

        private async void BrowseSaveDirButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                DirTextBox.Text = folder.Path;

                var settings = AppSettingService.LoadSettings();
                settings.VideoDownloaderSaveDir = folder.Path;
                AppSettingService.SaveSettings(settings);
            }
        }
        private async void UrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDownloadButtonState();

            string url = UrlTextBox.Text.Trim();
            if (CheckUrlValidation())
            {
                await ExtractVideoMetadataAsync(url);
            }
        }

        private async Task ExtractVideoMetadataAsync(string url)
        {
            StatusTextBlock.Text = "Fetching video details...";

            await Task.Run(() =>
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "yt-dlp",
                        // Use separate --print flags for title and ext to prevent command syntax errors
                        Arguments = $"--print title --print ext --skip-download \"{url}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                        {
                            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            if (lines.Length >= 2)
                            {
                                string title = lines[0].Trim();
                                string ext = lines[1].Trim();

                                DispatcherQueue.TryEnqueue(() =>
                                {
                                    FilenameTextBox.Text = title;
                                    ExtensionTextBox.Text = ext;
                                    StatusTextBlock.Text = "Metadata fetched successfully.";
                                });
                                return;
                            }
                        }
                    }

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        StatusTextBlock.Text = "Could not automatically fetch metadata.";
                    });
                }
                catch (Exception ex)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        StatusTextBlock.Text = $"Metadata extraction failed: {ex.Message}";
                    });
                }
            });
        }
        private async void DownloadVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckUrlValidation() || !Directory.Exists(DirTextBox.Text.Trim()))
            {
                StatusTextBlock.Text = "Error: Invalid URL or Save Directory.";
                UpdateDownloadButtonState();
                return;
            }

            int threads = (int)ThreadCountBox.Value;
            string url = UrlTextBox.Text.Trim();
            string saveDir = DirTextBox.Text.Trim();
            string filename = FilenameTextBox.Text.Trim();
            string extension = ExtensionTextBox.Text.Trim();

            DownloadVideoButton.IsEnabled = false;
            StatusTextBlock.Text = "Initializing download...";

            // Create a progress handler to intercept live lines from yt-dlp
            var progress = new Progress<string>(message =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    StatusTextBlock.Text = message;
                });
            });

            // Run service asynchronously with progress reporting
            string resultMessage = await YtDlpService.DownloadVideoAsync(url, saveDir, filename, extension, threads, progress);

            StatusTextBlock.Text = resultMessage;
            UpdateDownloadButtonState();
        }

        private bool CheckInstallation(string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();

                return process?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private bool CheckUrlValidation()
        {
            if (UrlTextBox == null || string.IsNullOrWhiteSpace(UrlTextBox.Text)) return false;

            return Uri.TryCreate(UrlTextBox.Text.Trim(), UriKind.Absolute, out Uri? uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
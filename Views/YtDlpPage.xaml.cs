using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PrototypePT1.Services;
using PrototypePT1.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Microsoft.UI;

namespace PrototypePT1.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
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

            if(CheckInstallation("yt-dlp"))
            {
                InstallYtdlpButton.Visibility = Visibility.Collapsed;
                StatusTextBlock.Text = "yt-dlp is installed and ready to use.";
            }
            else
            {
                if(CheckInstallation("winget"))
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

                bool isInstalled = CheckInstallation("yt-dlp");
                if (isInstalled)
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

                // Save persistently via the centralized AppSettingService
                var settings = AppSettingService.LoadSettings();
                settings.VideoDownloaderSaveDir = folder.Path;
                AppSettingService.SaveSettings(settings);
            }
        }

        private async void DownloadVideoButton_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for download logic when ready
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
            if (UrlTextBox == null) return false;

            return Uri.TryCreate(UrlTextBox.Text, UriKind.Absolute, out Uri? uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
using System;
using System.IO;
using System.Text.Json;
using PrototypePT1.Models;

namespace PrototypePT1.Services
{
    public static class AppSettingService
    {
        private static string GetSettingsFilePath()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrototypePT1");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "settings.json");
        }

        public static AppSettings LoadSettings()
        {
            try
            {
                string path = GetSettingsFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadSettings Error: {ex.Message}");
            }
            return new AppSettings();
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                string path = GetSettingsFilePath();
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveSettings Error: {ex.Message}");
                throw;
            }
        }
    }
}
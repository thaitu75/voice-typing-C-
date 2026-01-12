using System;
using System.IO;
using Newtonsoft.Json;

namespace VoiceTyping.Services
{
    public class AppSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public double WindowX { get; set; } = -1;
        public double WindowY { get; set; } = -1;
    }

    public class SettingsService
    {
        private readonly string _settingsPath;
        private AppSettings _settings;

        public AppSettings Settings => _settings;

        public SettingsService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VoiceTyping"
            );
            
            Directory.CreateDirectory(appDataPath);
            _settingsPath = Path.Combine(appDataPath, "settings.json");
            _settings = Load();
        }

        private AppSettings Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                // If loading fails, return default settings
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
                // Silently fail if we can't save settings
            }
        }

        public void UpdateApiKey(string apiKey)
        {
            _settings.ApiKey = apiKey;
            Save();
        }

        public void UpdateLanguage(string language)
        {
            _settings.Language = language;
            Save();
        }

        public void UpdateWindowPosition(double x, double y)
        {
            _settings.WindowX = x;
            _settings.WindowY = y;
            Save();
        }
    }
}

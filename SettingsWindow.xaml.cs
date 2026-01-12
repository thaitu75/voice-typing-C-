using System.Windows;
using System.Windows.Input;
using VoiceTyping.Services;

namespace VoiceTyping
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly WhisperApiService _whisperService;

        public SettingsWindow(SettingsService settingsService, WhisperApiService whisperService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _whisperService = whisperService;

            // Load current settings
            ApiKeyBox.Password = _settingsService.Settings.ApiKey;
            LanguageBox.Text = _settingsService.Settings.Language;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var apiKey = ApiKeyBox.Password.Trim();
            var language = LanguageBox.Text.Trim();

            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Please enter your OpenAI API key.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save settings
            _settingsService.UpdateApiKey(apiKey);
            _settingsService.UpdateLanguage(language);
            
            // Update whisper service
            _whisperService.SetApiKey(apiKey);

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

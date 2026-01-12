using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using VoiceTyping.Services;

namespace VoiceTyping
{
    public partial class MainWindow : Window
    {
        private readonly AudioRecordingService _audioService;
        private readonly WhisperApiService _whisperService;
        private readonly AutoTypingService _typingService;
        private readonly GlobalHotkeyService _hotkeyService;
        private readonly SettingsService _settingsService;

        private bool _isRecording;
        private bool _isProcessing;
        private Point _dragStartPoint;
        private Storyboard? _pulseAnimation;
        private Storyboard? _spinnerAnimation;

        public MainWindow()
        {
            InitializeComponent();

            _audioService = new AudioRecordingService();
            _whisperService = new WhisperApiService();
            _typingService = new AutoTypingService();
            _hotkeyService = new GlobalHotkeyService();
            _settingsService = new SettingsService();

            // Set API key if saved
            if (!string.IsNullOrEmpty(_settingsService.Settings.ApiKey))
            {
                _whisperService.SetApiKey(_settingsService.Settings.ApiKey);
            }

            // Subscribe to hotkey
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Register global hotkey
            try
            {
                _hotkeyService.Register(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hotkey Registration Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Restore window position
            var settings = _settingsService.Settings;
            if (settings.WindowX >= 0 && settings.WindowY >= 0)
            {
                Left = settings.WindowX;
                Top = settings.WindowY;
            }
            else
            {
                // Default position: bottom-right
                Left = SystemParameters.WorkArea.Right - Width - 20;
                Top = SystemParameters.WorkArea.Bottom - Height - 20;
            }

            // Create animations
            CreateAnimations();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save window position
            _settingsService.UpdateWindowPosition(Left, Top);

            // Cleanup
            _hotkeyService.Dispose();
            _audioService.Dispose();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
                // Save position after drag
                _settingsService.UpdateWindowPosition(Left, Top);
            }
        }

        private void MainButton_Click(object sender, RoutedEventArgs e)
        {
            // If we just finished a drag, don't toggle recording
            // DragMove blocks, so we rely on the fact that DragMove eats the MouseUp event
            ToggleRecording();
        }

        private void MainButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(this);
        }

        private void MainButton_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPoint = e.GetPosition(this);
                if (Math.Abs(currentPoint.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPoint.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    try
                    {
                        DragMove();
                        _settingsService.UpdateWindowPosition(Left, Top);
                        e.Handled = true;
                    }
                    catch (InvalidOperationException) { }
                }
            }
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OnHotkeyPressed(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(ToggleRecording);
        }

        private async void ToggleRecording()
        {
            if (_isProcessing) return;

            if (!_isRecording)
            {
                StartRecording();
            }
            else
            {
                await StopRecordingAndTranscribe();
            }
        }

        private void StartRecording()
        {
            if (string.IsNullOrEmpty(_settingsService.Settings.ApiKey))
            {
                ShowSettings();
                return;
            }

            _isRecording = true;
            _audioService.StartRecording();
            UpdateVisualState();
        }

        private async Task StopRecordingAndTranscribe()
        {
            _isRecording = false;
            _isProcessing = true;
            UpdateVisualState();

            try
            {
                var audioData = _audioService.StopRecording();
                
                // Check for silence (prevent hallucinations)
                if (_audioService.MaxVolume < 0.015f) // 1.5% threshold
                {
                    _isProcessing = false;
                    UpdateVisualState();
                    return;
                }
                
                if (audioData.Length > 0)
                {
                    var transcription = await _whisperService.TranscribeAsync(
                        audioData, 
                        _settingsService.Settings.Language
                    );

                    if (!string.IsNullOrEmpty(transcription))
                    {
                        await _typingService.TypeTextAsync(transcription);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isProcessing = false;
                UpdateVisualState();
            }
        }

        private void UpdateVisualState()
        {
            var template = MainButton.Template;
            var mainCircle = (Ellipse)template.FindName("MainCircle", MainButton);
            var logoImage = (Image)template.FindName("LogoImage", MainButton);
            var waveIcon = (Path)template.FindName("WaveIcon", MainButton);
            var spinnerIcon = (Path)template.FindName("SpinnerIcon", MainButton);
            var pulseRing = (Ellipse)template.FindName("PulseRing", MainButton);

            if (_isProcessing)
            {
                // Processing state - yellow with spinner
                mainCircle.Fill = (Brush)FindResource("ProcessingGradient");
                mainCircle.Opacity = 1;
                logoImage.Opacity = 0;
                waveIcon.Opacity = 0;
                spinnerIcon.Opacity = 1;
                StopPulseAnimation(pulseRing);
                StartSpinnerAnimation(spinnerIcon);
            }
            else if (_isRecording)
            {
                // Recording state - red with wave
                mainCircle.Fill = (Brush)FindResource("RecordingGradient");
                mainCircle.Opacity = 1;
                logoImage.Opacity = 0;
                waveIcon.Opacity = 1;
                spinnerIcon.Opacity = 0;
                StopSpinnerAnimation();
                StartPulseAnimation(pulseRing);
            }
            else
            {
                // Idle state - purple/blue with mic
                mainCircle.Fill = (Brush)FindResource("IdleGradient");
                mainCircle.Opacity = 0; // Hide purple circle to show full logo
                logoImage.Opacity = 1;
                waveIcon.Opacity = 0;
                spinnerIcon.Opacity = 0;
                StopPulseAnimation(pulseRing);
                StopSpinnerAnimation();
            }
        }

        private void CreateAnimations()
        {
            // Pulse animation for recording
            _pulseAnimation = new Storyboard();
            var scaleXAnim = new DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(800))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            var scaleYAnim = new DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(800))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            var opacityAnim = new DoubleAnimation(0.5, 0, TimeSpan.FromMilliseconds(800))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("RenderTransform.ScaleX"));
            Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("RenderTransform.ScaleY"));
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));

            _pulseAnimation.Children.Add(scaleXAnim);
            _pulseAnimation.Children.Add(scaleYAnim);
            _pulseAnimation.Children.Add(opacityAnim);

            // Spinner animation for processing
            _spinnerAnimation = new Storyboard();
            var rotateAnim = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(1000))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTargetProperty(rotateAnim, new PropertyPath("RenderTransform.Angle"));
            _spinnerAnimation.Children.Add(rotateAnim);
        }

        private void StartPulseAnimation(Ellipse target)
        {
            _pulseAnimation?.Begin(target, true);
        }

        private void StopPulseAnimation(Ellipse target)
        {
            _pulseAnimation?.Stop(target);
            target.Opacity = 0;
        }

        private void StartSpinnerAnimation(Path target)
        {
            _spinnerAnimation?.Begin(target, true);
        }

        private void StopSpinnerAnimation()
        {
            _spinnerAnimation?.Stop();
        }

        private void ShowSettings()
        {
            var settingsWindow = new SettingsWindow(_settingsService, _whisperService)
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }
    }
}
using System;
using System.IO;
using NAudio.Wave;

namespace VoiceTyping.Services
{
    public class AudioRecordingService : IDisposable
    {
        private WaveInEvent? _waveIn;
        private MemoryStream? _audioStream;
        private WaveFileWriter? _waveWriter;
        private bool _isRecording;

        public event EventHandler<float>? AudioLevelChanged;
        public bool IsRecording => _isRecording;

        public void StartRecording()
        {
            if (_isRecording) return;

            _audioStream = new MemoryStream();
            
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 16, 1) // 16kHz, 16-bit, mono - optimal for Whisper
            };

            _waveWriter = new WaveFileWriter(_audioStream, _waveIn.WaveFormat);

            _waveIn.DataAvailable += (s, e) =>
            {
                _waveWriter?.Write(e.Buffer, 0, e.BytesRecorded);
                
                // Calculate audio level for visual feedback
                float max = 0;
                for (int i = 0; i < e.BytesRecorded; i += 2)
                {
                    short sample = (short)(e.Buffer[i + 1] << 8 | e.Buffer[i]);
                    float sample32 = sample / 32768f;
                    if (sample32 < 0) sample32 = -sample32;
                    if (sample32 > max) max = sample32;
                }
                AudioLevelChanged?.Invoke(this, max);
            };

            _waveIn.StartRecording();
            _isRecording = true;
        }

        public byte[] StopRecording()
        {
            if (!_isRecording || _waveIn == null || _waveWriter == null || _audioStream == null)
                return Array.Empty<byte>();

            _waveIn.StopRecording();
            _waveWriter.Flush();
            _isRecording = false;

            // Create a proper WAV file in memory
            var resultStream = new MemoryStream();
            _audioStream.Position = 0;
            
            // Copy the audio data to a new stream with proper WAV header
            using (var tempStream = new MemoryStream())
            {
                using (var writer = new WaveFileWriter(tempStream, _waveIn.WaveFormat))
                {
                    _audioStream.Position = 44; // Skip the original header
                    var buffer = new byte[_audioStream.Length - 44];
                    _audioStream.Read(buffer, 0, buffer.Length);
                    writer.Write(buffer, 0, buffer.Length);
                }
                return tempStream.ToArray();
            }
        }

        public void Dispose()
        {
            _waveIn?.Dispose();
            _waveWriter?.Dispose();
            _audioStream?.Dispose();
        }
    }
}

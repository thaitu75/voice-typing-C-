using System;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace VoiceTyping.Services
{
    public class AutoTypingService
    {
        private readonly InputSimulator _inputSimulator;
        private const int CharacterDelayMs = 10;

        public AutoTypingService()
        {
            _inputSimulator = new InputSimulator();
        }

        public async Task TypeTextAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // Give the previous window time to regain focus
            await Task.Delay(100);

            // Type the text character by character with a small delay
            foreach (char c in text)
            {
                _inputSimulator.Keyboard.TextEntry(c);
                await Task.Delay(CharacterDelayMs);
            }
        }

        public void TypeTextImmediate(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            _inputSimulator.Keyboard.TextEntry(text);
        }
    }
}

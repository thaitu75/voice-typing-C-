using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace VoiceTyping.Services
{
    public class GlobalHotkeyService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_F8 = 9000;
        private const int HOTKEY_ID_F9 = 9001;
        private const uint VK_F8 = 0x77;
        private const uint VK_F9 = 0x78;
        private const uint MOD_NONE = 0x0000;
        private const int WM_HOTKEY = 0x0312;

        private IntPtr _windowHandle;
        private HwndSource? _source;

        public event EventHandler? HotkeyPressed;
        public event EventHandler? TranslateHotkeyPressed;

        public void Register(Window window)
        {
            _windowHandle = new WindowInteropHelper(window).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source?.AddHook(HwndHook);

            if (!RegisterHotKey(_windowHandle, HOTKEY_ID_F8, MOD_NONE, VK_F8))
            {
                throw new InvalidOperationException("Could not register F8 hotkey. It may be in use by another application.");
            }

            if (!RegisterHotKey(_windowHandle, HOTKEY_ID_F9, MOD_NONE, VK_F9))
            {
                throw new InvalidOperationException("Could not register F9 hotkey. It may be in use by another application.");
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID_F8)
                {
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
                else if (id == HOTKEY_ID_F9)
                {
                    TranslateHotkeyPressed?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            _source?.RemoveHook(HwndHook);
            if (_windowHandle != IntPtr.Zero)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID_F8);
                UnregisterHotKey(_windowHandle, HOTKEY_ID_F9);
            }
        }
    }
}

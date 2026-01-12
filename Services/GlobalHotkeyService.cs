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

        private const int HOTKEY_ID = 9000;
        private const uint VK_F8 = 0x77;
        private const uint MOD_NONE = 0x0000;
        private const int WM_HOTKEY = 0x0312;

        private IntPtr _windowHandle;
        private HwndSource? _source;

        public event EventHandler? HotkeyPressed;

        public void Register(Window window)
        {
            _windowHandle = new WindowInteropHelper(window).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source?.AddHook(HwndHook);

            if (!RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, VK_F8))
            {
                throw new InvalidOperationException("Could not register F8 hotkey. It may be in use by another application.");
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            _source?.RemoveHook(HwndHook);
            if (_windowHandle != IntPtr.Zero)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
            }
        }
    }
}

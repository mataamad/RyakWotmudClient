using System;
using System.Runtime.InteropServices;
using MudClient.Management;

[StructLayout(LayoutKind.Sequential)]
internal struct FLASHWINFO {
    internal uint cbSize;
    internal IntPtr hwnd;
    internal int dwFlags;
    internal uint uCount;
    internal int dwTimeout;
}

// dwFlags can be one of the following:
internal enum FlashType {
    // stop flashing
    FLASHW_STOP = 0,

    // flash the window title 
    FLASHW_CAPTION = 1,

    // flash the taskbar button
    FLASHW_TRAY = 2,

    // 1 | 2
    FLASHW_ALL = 3,

    // flash continuously 
    FLASHW_TIMER = 4,

    // flash until the window comes to the foreground 
    FLASHW_TIMERNOFG = 12,
}

internal static class WindowFlasher {

    [DllImport("user32.dll")]
    static private extern int FlashWindowEx(ref FLASHWINFO pwfi);

    internal static void Flash(MudClientForm window) {
        if (!window.IsShown) {
            return;
        }

        void Flash() {
            var fw = new FLASHWINFO {
                cbSize = Convert.ToUInt32(Marshal.SizeOf<FLASHWINFO>()),
                hwnd = window.Handle,
                dwFlags = (int)(FlashType.FLASHW_TRAY | FlashType.FLASHW_TIMERNOFG),
                uCount = UInt32.MaxValue
            };

            FlashWindowEx(ref fw);
        }

        window.Invoke(Flash);
    }
}

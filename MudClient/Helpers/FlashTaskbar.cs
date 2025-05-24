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
    private static extern int FlashWindowEx(ref FLASHWINFO pwfi);

    internal static void Flash(MudClientForm window) {
        if (!window.IsShown) {
            return;
        }

        Action Flash = () => {
            FLASHWINFO fw = new FLASHWINFO();
            fw.cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(FLASHWINFO)));
            fw.hwnd = window.Handle;
            fw.dwFlags = (int)(FlashType.FLASHW_TRAY | FlashType.FLASHW_TIMERNOFG);
            fw.uCount = UInt32.MaxValue;

            FlashWindowEx(ref fw);
        };

        window.Invoke(Flash);
    }
}

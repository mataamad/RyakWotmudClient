using System;
using System.Runtime.InteropServices;
using MudClient.Management;

[StructLayout(LayoutKind.Sequential)]
public struct FLASHWINFO {
    public UInt32 cbSize;
    public IntPtr hwnd;
    public Int32 dwFlags;
    public UInt32 uCount;
    public Int32 dwTimeout;
}

// dwFlags can be one of the following:
public enum FlashType {
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

public static class WindowFlasher {

    [DllImport("user32.dll")]
    static extern Int32 FlashWindowEx(ref FLASHWINFO pwfi);

    public static void Flash(MudClientForm window) {
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

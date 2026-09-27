using System;
using AutoHideDesktopIcons11.Interop;

namespace AutoHideDesktopIcons11.Services;

internal static class IdleTracker
{
    /// <summary>Liefert die Zeit seit der letzten Benutzereingabe (Maus/Tastatur), systemweit.</summary>
    public static TimeSpan GetIdleTime()
    {
        var lii = new NativeMethods.LASTINPUTINFO
        {
            cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.LASTINPUTINFO>()
        };

        if (!NativeMethods.GetLastInputInfo(ref lii))
        {
            return TimeSpan.Zero;
        }

        uint tickCount = (uint)Environment.TickCount;
        uint idleTicks = tickCount - lii.dwTime; // funktioniert auch über TickCount-Überlauf hinweg (unsigned)
        return TimeSpan.FromMilliseconds(idleTicks);
    }
}

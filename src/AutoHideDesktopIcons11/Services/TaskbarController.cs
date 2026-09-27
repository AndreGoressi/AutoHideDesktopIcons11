using System;
using System.Collections.Generic;
using AutoHideDesktopIcons11.Interop;

namespace AutoHideDesktopIcons11.Services;

/// <summary>
/// Blendet die primäre Taskleiste ("Shell_TrayWnd") sowie sekundäre Taskleisten
/// auf weiteren Monitoren ("Shell_SecondaryTrayWnd") ein/aus.
/// </summary>
internal static class TaskbarController
{
    public static void SetTaskbarVisible(bool visible)
    {
        var primary = NativeMethods.FindWindow("Shell_TrayWnd", null);
        if (primary != IntPtr.Zero)
        {
            NativeMethods.ShowWindow(primary, visible ? NativeMethods.SW_SHOW : NativeMethods.SW_HIDE);
        }

        foreach (var secondary in FindSecondaryTaskbars())
        {
            NativeMethods.ShowWindow(secondary, visible ? NativeMethods.SW_SHOW : NativeMethods.SW_HIDE);
        }
    }

    private static List<IntPtr> FindSecondaryTaskbars()
    {
        var result = new List<IntPtr>();
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            var sb = new System.Text.StringBuilder(256);
            NativeMethods.GetClassName(hWnd, sb, sb.Capacity);
            if (sb.ToString() == "Shell_SecondaryTrayWnd")
            {
                result.Add(hWnd);
            }
            return true;
        }, IntPtr.Zero);
        return result;
    }
}

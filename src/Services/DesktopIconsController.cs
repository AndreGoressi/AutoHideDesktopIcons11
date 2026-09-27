using System;
using AutoHideDesktopIcons11.Interop;

namespace AutoHideDesktopIcons11.Services;

/// <summary>
/// Blendet die Desktop-Symbole (SysListView32 "FolderView") ein/aus.
/// Seit Windows 8 hostet der Explorer die Icons manchmal direkt unter "Progman",
/// manchmal unter einem separaten "WorkerW"-Fenster (abhängig von aktivem Live-Wallpaper
/// / Widgets). Deshalb wird nötigenfalls über alle Top-Level-Fenster gesucht.
/// </summary>
internal static class DesktopIconsController
{
    public static bool AreIconsVisible()
    {
        var listView = FindDesktopListView();
        return listView == IntPtr.Zero || NativeMethods.IsWindowVisible(listView);
    }

    public static void SetIconsVisible(bool visible)
    {
        var listView = FindDesktopListView();
        if (listView != IntPtr.Zero)
        {
            NativeMethods.ShowWindow(listView, visible ? NativeMethods.SW_SHOW : NativeMethods.SW_HIDE);
        }
    }

    private static IntPtr FindDesktopListView()
    {
        var progman = NativeMethods.FindWindow("Progman", null);

        var defView = NativeMethods.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);

        if (defView == IntPtr.Zero)
        {
            // Fallback: WorkerW-Fenster durchsuchen, das SHELLDLL_DefView als Kind besitzt.
            NativeMethods.EnumWindows((hWnd, _) =>
            {
                var candidate = NativeMethods.FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (candidate != IntPtr.Zero)
                {
                    defView = candidate;
                    return false; // Suche beenden
                }
                return true; // weitersuchen
            }, IntPtr.Zero);
        }

        if (defView == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        return NativeMethods.FindWindowEx(defView, IntPtr.Zero, "SysListView32", "FolderView");
    }
}

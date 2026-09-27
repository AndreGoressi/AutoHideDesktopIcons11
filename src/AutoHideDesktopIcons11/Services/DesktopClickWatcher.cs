using System;
using System.Text;
using AutoHideDesktopIcons11.Interop;

namespace AutoHideDesktopIcons11.Services;

internal enum MouseButtonKind
{
    Left,
    Middle,
    Right
}

/// <summary>
/// Registriert einen systemweiten Low-Level-Mouse-Hook und meldet Klicks, die auf
/// den Desktop selbst (Progman/WorkerW/SHELLDLL_DefView/SysListView32) gehen –
/// unabhängig davon, ob die Icons dort gerade sichtbar oder ausgeblendet sind.
/// </summary>
internal sealed class DesktopClickWatcher : IDisposable
{
    private readonly NativeMethods.LowLevelMouseProc _proc;
    private IntPtr _hookHandle = IntPtr.Zero;

    public event Action<MouseButtonKind>? DesktopClicked;

    public DesktopClickWatcher()
    {
        _proc = HookCallback; // Referenz halten, damit der Delegate nicht vom GC eingesammelt wird
    }

    public void Start()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            return;
        }

        using var curModule = System.Diagnostics.Process.GetCurrentProcess().MainModule!;
        var hModule = NativeMethods.GetModuleHandle(curModule.ModuleName);
        _hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _proc, hModule, 0);
    }

    public void Stop()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            MouseButtonKind? button = msg switch
            {
                NativeMethods.WM_LBUTTONDOWN => MouseButtonKind.Left,
                NativeMethods.WM_MBUTTONDOWN => MouseButtonKind.Middle,
                NativeMethods.WM_RBUTTONDOWN => MouseButtonKind.Right,
                _ => null
            };

            if (button is not null && IsClickOnDesktop())
            {
                DesktopClicked?.Invoke(button.Value);
            }
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private static bool IsClickOnDesktop()
    {
        if (!NativeMethods.GetCursorPos(out var pt))
        {
            return false;
        }

        var hWnd = NativeMethods.WindowFromPoint(pt);
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        var sb = new StringBuilder(256);
        NativeMethods.GetClassName(hWnd, sb, sb.Capacity);
        var className = sb.ToString();

        return className is "Progman" or "WorkerW" or "SHELLDLL_DefView" or "SysListView32";
    }

    public void Dispose() => Stop();
}

using System;
using AutoHideDesktopIcons11.Interop;

namespace AutoHideDesktopIcons11.Services;

/// <summary>
/// Kapselt das klassische Shell_NotifyIcon-API (Infobereich-Symbol), da WinUI 3
/// kein eigenes Tray-Icon-Control mitbringt. Das Host-Fenster wird per
/// Window-Subclassing (GWLP_WNDPROC) erweitert, um die Tray-Callback-Message
/// sowie eine registrierte "Toggle"-Message (vom Desktop-Kontextmenü) zu empfangen.
/// </summary>
internal sealed class TrayIconService : IDisposable
{
    private const uint TrayId = 1;

    private readonly IntPtr _hWnd;
    private readonly NativeMethods.WndProcDelegate _newWndProc;
    private IntPtr _oldWndProc = IntPtr.Zero;
    private IntPtr _hIcon = IntPtr.Zero;
    public readonly uint ToggleMessageId;

    public event Action? OpenRequested;
    public event Action? ExitRequested;
    public event Action? ToggleRequested;

    public TrayIconService(IntPtr hWnd, string iconPath)
    {
        _hWnd = hWnd;
        _newWndProc = WndProc;
        ToggleMessageId = NativeMethods.RegisterWindowMessage("AutoHideDesktopIcons11_ToggleMessage");

        _oldWndProc = NativeMethods.SetWindowLongPtr(_hWnd, NativeMethods.GWLP_WNDPROC, _newWndProc);

        _hIcon = NativeMethods.LoadImage(IntPtr.Zero, iconPath, NativeMethods.IMAGE_ICON, 16, 16,
            NativeMethods.LR_LOADFROMFILE | NativeMethods.LR_DEFAULTSIZE);
    }

    public void Show(string tooltip)
    {
        var data = new NativeMethods.NOTIFYICONDATA
        {
            cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
            hWnd = _hWnd,
            uID = TrayId,
            uFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP,
            uCallbackMessage = NativeMethods.WM_APP_TRAYCALLBACK,
            hIcon = _hIcon,
            szTip = tooltip
        };
        NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref data);
    }

    public void Remove()
    {
        var data = new NativeMethods.NOTIFYICONDATA
        {
            cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
            hWnd = _hWnd,
            uID = TrayId
        };
        NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref data);
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == NativeMethods.WM_APP_TRAYCALLBACK)
        {
            int mouseMsg = lParam.ToInt32();
            if (mouseMsg == NativeMethods.WM_LBUTTONDOWN)
            {
                OpenRequested?.Invoke();
            }
            else if (mouseMsg == NativeMethods.WM_RBUTTONDOWN)
            {
                ShowContextMenu();
            }
            return IntPtr.Zero;
        }

        if (msg == ToggleMessageId)
        {
            ToggleRequested?.Invoke();
            return IntPtr.Zero;
        }

        return NativeMethods.CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    private void ShowContextMenu()
    {
        const uint idOpen = 1;
        const uint idToggle = 2;
        const uint idExit = 3;

        var menu = NativeMethods.CreatePopupMenu();
        NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING, idOpen, "Einstellungen öffnen");
        NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING, idToggle, "Icons jetzt ein-/ausblenden");
        NativeMethods.AppendMenu(menu, NativeMethods.MF_SEPARATOR, 0, string.Empty);
        NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING, idExit, "Beenden");

        NativeMethods.GetCursorPos(out var pt);
        NativeMethods.SetForegroundWindow(_hWnd);

        uint cmd = NativeMethods.TrackPopupMenuEx(menu, NativeMethods.TPM_RIGHTBUTTON | NativeMethods.TPM_RETURNCMD,
            pt.X, pt.Y, _hWnd, IntPtr.Zero);

        NativeMethods.DestroyMenu(menu);

        switch (cmd)
        {
            case idOpen:
                OpenRequested?.Invoke();
                break;
            case idToggle:
                ToggleRequested?.Invoke();
                break;
            case idExit:
                ExitRequested?.Invoke();
                break;
        }
    }

    public void Dispose()
    {
        Remove();
        if (_oldWndProc != IntPtr.Zero)
        {
            NativeMethods.SetWindowLongPtr(_hWnd, NativeMethods.GWLP_WNDPROC,
                Marshal_GetDelegateForFunctionPointer(_oldWndProc));
        }
    }

    // Hilfsfunktion, um den alten WndProc-Zeiger wieder als Delegate einzuhängen.
    private static NativeMethods.WndProcDelegate Marshal_GetDelegateForFunctionPointer(IntPtr ptr) =>
        (NativeMethods.WndProcDelegate)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(
            ptr, typeof(NativeMethods.WndProcDelegate));
}

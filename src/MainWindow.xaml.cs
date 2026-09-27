using System;
using System.Diagnostics;
using System.IO;
using AutoHideDesktopIcons11.Interop;
using AutoHideDesktopIcons11.Models;
using AutoHideDesktopIcons11.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI;
using WinRT.Interop;

namespace AutoHideDesktopIcons11;

public sealed partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly IntPtr _hwnd;
    private readonly AppWindow _appWindow;

    private TrayIconService? _tray;
    private DesktopClickWatcher? _clickWatcher;
    private DispatcherQueueTimer? _idleTimer;

    private bool _isApplyingSettingsToUi;
    private bool _iconsCurrentlyHidden;

    public MainWindow(bool startMinimized)
    {
        InitializeComponent();

        _hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(iconPath))
        {
            _appWindow.SetIcon(iconPath);
        }

        _appWindow.Resize(new Windows.Graphics.SizeInt32(760, 560));
        _appWindow.Closing += OnAppWindowClosing;

        _settings = AppSettings.Load();
        ApplySettingsToUi();
        ApplyAllEffects(initial: true);

        SetupTrayIcon(iconPath);
        SetupClickWatcher();
        SetupIdleTimer();

        if (startMinimized)
        {
            _appWindow.Hide();
        }
    }

    // ---------------- UI <-> Settings ----------------

    private void ApplySettingsToUi()
    {
        _isApplyingSettingsToUi = true;

        DisabledCheckBox.IsChecked = _settings.Disabled;
        StartWithWindowsCheckBox.IsChecked = _settings.StartWithWindows;
        StartMinimizedCheckBox.IsChecked = _settings.StartMinimizedToTray;
        AlwaysOnTopCheckBox.IsChecked = _settings.AlwaysOnTop;
        HideTaskbarCheckBox.IsChecked = _settings.HideTaskbar;

        ShowOnLeftClickCheckBox.IsChecked = _settings.ShowOnLeftClick;
        ShowOnMiddleClickCheckBox.IsChecked = _settings.ShowOnMiddleClick;
        ShowOnRightClickCheckBox.IsChecked = _settings.ShowOnRightClick;
        ShowOnContextMenuCheckBox.IsChecked = _settings.ShowOnDesktopContextMenu;

        IdleSecondsSlider.Value = _settings.IdleSeconds;
        IdleSecondsLabel.Text = $"{_settings.IdleSeconds} Sek.";

        _isApplyingSettingsToUi = false;
    }

    private void ReadUiIntoSettings()
    {
        _settings.Disabled = DisabledCheckBox.IsChecked == true;
        _settings.StartWithWindows = StartWithWindowsCheckBox.IsChecked == true;
        _settings.StartMinimizedToTray = StartMinimizedCheckBox.IsChecked == true;
        _settings.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked == true;
        _settings.HideTaskbar = HideTaskbarCheckBox.IsChecked == true;

        _settings.ShowOnLeftClick = ShowOnLeftClickCheckBox.IsChecked == true;
        _settings.ShowOnMiddleClick = ShowOnMiddleClickCheckBox.IsChecked == true;
        _settings.ShowOnRightClick = ShowOnRightClickCheckBox.IsChecked == true;
        _settings.ShowOnDesktopContextMenu = ShowOnContextMenuCheckBox.IsChecked == true;
    }

    private void OnAnySettingChanged(object sender, RoutedEventArgs e)
    {
        if (_isApplyingSettingsToUi)
        {
            return;
        }

        ReadUiIntoSettings();
        _settings.Save();
        ApplyAllEffects(initial: false);
    }

    private void OnIdleSecondsChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        IdleSecondsLabel.Text = $"{(int)e.NewValue} Sek.";

        if (_isApplyingSettingsToUi)
        {
            return;
        }

        _settings.IdleSeconds = (int)e.NewValue;
        _settings.Save();
    }

    // ---------------- Effekte anwenden ----------------

    private void ApplyAllEffects(bool initial)
    {
        AutostartService.SetEnabled(_settings.StartWithWindows);
        ContextMenuService.SetEnabled(_settings.ShowOnDesktopContextMenu);
        SetAlwaysOnTop(_settings.AlwaysOnTop);

        if (_settings.Disabled && _iconsCurrentlyHidden)
        {
            RestoreDesktop();
        }

        if (!initial)
        {
            UpdateStatusText();
        }
    }

    private void SetAlwaysOnTop(bool onTop)
    {
        NativeMethods.SetWindowPos(_hwnd, onTop ? NativeMethods.HWND_TOPMOST : NativeMethods.HWND_NOTOPMOST,
            0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE);
    }

    private void UpdateStatusText()
    {
        StatusText.Text = _settings.Disabled
            ? "Deaktiviert – Desktop-Icons werden nicht automatisch ausgeblendet."
            : "Läuft im Infobereich weiter, wenn das Fenster geschlossen wird.";
    }

    // ---------------- Idle-Überwachung ----------------

    private void SetupIdleTimer()
    {
        _idleTimer = DispatcherQueue.CreateTimer();
        _idleTimer.Interval = TimeSpan.FromMilliseconds(250);
        _idleTimer.Tick += OnIdleTick;
        _idleTimer.Start();
    }

    private void OnIdleTick(DispatcherQueueTimer sender, object args)
    {
        if (_settings.Disabled)
        {
            IdleProgressBar.Value = 0;
            return;
        }

        var idle = IdleTracker.GetIdleTime();
        double thresholdMs = Math.Max(1, _settings.IdleSeconds) * 1000.0;
        double progress = Math.Min(1.0, idle.TotalMilliseconds / thresholdMs);
        IdleProgressBar.Value = progress;

        if (idle.TotalMilliseconds >= thresholdMs && !_iconsCurrentlyHidden)
        {
            DesktopIconsController.SetIconsVisible(false);
            if (_settings.HideTaskbar)
            {
                TaskbarController.SetTaskbarVisible(false);
            }
            _iconsCurrentlyHidden = true;
        }
    }

    // ---------------- Klick auf Desktop erkennen ----------------

    private void SetupClickWatcher()
    {
        _clickWatcher = new DesktopClickWatcher();
        _clickWatcher.DesktopClicked += OnDesktopClicked;
        _clickWatcher.Start();
    }

    private void OnDesktopClicked(MouseButtonKind button)
    {
        if (_settings.Disabled || !_iconsCurrentlyHidden)
        {
            return;
        }

        bool shouldShow = button switch
        {
            MouseButtonKind.Left => _settings.ShowOnLeftClick,
            MouseButtonKind.Middle => _settings.ShowOnMiddleClick,
            MouseButtonKind.Right => _settings.ShowOnRightClick,
            _ => false
        };

        if (shouldShow)
        {
            RestoreDesktop();
        }
    }

    private void RestoreDesktop()
    {
        DesktopIconsController.SetIconsVisible(true);
        if (_settings.HideTaskbar)
        {
            TaskbarController.SetTaskbarVisible(true);
        }
        _iconsCurrentlyHidden = false;
    }

    // ---------------- Tray-Icon ----------------

    private void SetupTrayIcon(string iconPath)
    {
        _tray = new TrayIconService(_hwnd, iconPath);
        _tray.Show("AutoHideDesktopIcons11");

        _tray.OpenRequested += () =>
        {
            _appWindow.Show();
            this.Activate();
        };

        _tray.ToggleRequested += () =>
        {
            if (_iconsCurrentlyHidden)
            {
                RestoreDesktop();
            }
            else
            {
                DesktopIconsController.SetIconsVisible(false);
                if (_settings.HideTaskbar)
                {
                    TaskbarController.SetTaskbarVisible(false);
                }
                _iconsCurrentlyHidden = true;
            }
        };

        _tray.ExitRequested += () =>
        {
            Cleanup();
            Application.Current.Exit();
        };
    }

    // ---------------- Fenster schließen = in den Tray minimieren ----------------

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        args.Cancel = true;
        sender.Hide();
    }

    private void OnGitHubLinkClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/AndreGoressi/AutoHideDesktopIcons11",
                UseShellExecute = true
            });
        }
        catch
        {
            // Kein Standardbrowser verfügbar o.ä. — bewusst ignoriert.
        }
    }

    private void Cleanup()
    {
        _idleTimer?.Stop();
        _clickWatcher?.Dispose();
        _tray?.Dispose();

        // Beim Beenden nichts versteckt zurücklassen.
        DesktopIconsController.SetIconsVisible(true);
        TaskbarController.SetTaskbarVisible(true);
    }
}

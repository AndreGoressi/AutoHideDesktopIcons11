using System;
using System.Linq;
using System.Threading;
using AutoHideDesktopIcons11.Interop;
using Microsoft.UI.Xaml;

namespace AutoHideDesktopIcons11;

public partial class App : Application
{
    private const string MutexName = "AutoHideDesktopIcons11_SingleInstance";
    private static Mutex? _mutex;

    public MainWindow? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var cmdArgs = Environment.GetCommandLineArgs();
        bool toggleOnly = cmdArgs.Any(a => a.Equals("--toggle", StringComparison.OrdinalIgnoreCase));
        bool startMinimized = cmdArgs.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase));

        _mutex = new Mutex(initiallyOwned: true, MutexName, out bool isFirstInstance);

        if (!isFirstInstance)
        {
            // Es läuft bereits eine Instanz: entweder Toggle-Message senden (Desktop-Kontextmenü)
            // oder einfach das offene Fenster in den Vordergrund holen.
            var toggleMsg = NativeMethods.RegisterWindowMessage("AutoHideDesktopIcons11_ToggleMessage");
            var existing = NativeMethods.FindWindow(null, "AutoHideDesktopIcons11");
            if (existing != IntPtr.Zero)
            {
                if (toggleOnly)
                {
                    NativeMethods.PostMessage(existing, toggleMsg, IntPtr.Zero, IntPtr.Zero);
                }
                else
                {
                    NativeMethods.SetForegroundWindow(existing);
                }
            }

            Environment.Exit(0);
            return;
        }

        MainWindow = new MainWindow(startMinimized);

        // MainWindow.Activate() macht das Fenster sichtbar. Beim minimierten Start
        // bleibt es unsichtbar (die Fensterhandle existiert trotzdem für Hooks/Tray),
        // erst ein Klick auf "Einstellungen öffnen" im Tray zeigt es an.
        if (!startMinimized)
        {
            MainWindow.Activate();
        }
    }
}

using Microsoft.Win32;

namespace AutoHideDesktopIcons11.Services;

/// <summary>
/// Registriert (bzw. entfernt) einen Eintrag im Rechtsklick-Kontextmenü des Desktop-Hintergrunds,
/// über den die Icons per Klick sofort ein-/ausgeblendet werden können.
/// Ruft die bereits laufende Instanz über eine registrierte Fenster-Message auf
/// (siehe TrayIconService/App), startet die App also NICHT erneut.
/// </summary>
internal static class ContextMenuService
{
    private const string KeyPath = @"Software\Classes\DesktopBackground\Shell\AutoHideDesktopIcons11";
    private const string MenuText = "Desktop-Icons ein-/ausblenden";

    public static void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            var exePath = System.Environment.ProcessPath
                ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                ?? string.Empty;

            using var shellKey = Registry.CurrentUser.CreateSubKey(KeyPath);
            shellKey.SetValue(string.Empty, MenuText);
            shellKey.SetValue("Icon", $"\"{exePath}\",0");

            using var cmdKey = shellKey.CreateSubKey("command");
            cmdKey.SetValue(string.Empty, $"\"{exePath}\" --toggle");
        }
        else
        {
            Registry.CurrentUser.DeleteSubKeyTree(KeyPath, throwOnMissingSubKey: false);
        }
    }
}

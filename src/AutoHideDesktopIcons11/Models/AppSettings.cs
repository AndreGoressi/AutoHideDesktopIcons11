using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoHideDesktopIcons11.Models;

/// <summary>
/// Entspricht 1:1 den Einstellungen des klassischen "AutoHideDesktopIcons" (SoftwareOK) Tools.
/// </summary>
public class AppSettings
{
    public bool Disabled { get; set; } = false;
    public bool StartWithWindows { get; set; } = true;
    public bool StartMinimizedToTray { get; set; } = true;
    public bool AlwaysOnTop { get; set; } = false;
    public bool HideTaskbar { get; set; } = false;

    public bool ShowOnLeftClick { get; set; } = true;
    public bool ShowOnMiddleClick { get; set; } = true;
    public bool ShowOnRightClick { get; set; } = false; // im Original ausgegraut (Rechtsklick öffnet i.d.R. Kontextmenü)
    public bool ShowOnDesktopContextMenu { get; set; } = true;

    /// <summary>Sekunden ohne Eingabe, bevor die Icons ausgeblendet werden (3–100, wie im Original).</summary>
    public int IdleSeconds { get; set; } = 5;

    [JsonIgnore]
    public static string SettingsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoHideDesktopIcons11");

    [JsonIgnore]
    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded is not null)
                {
                    return loaded;
                }
            }
        }
        catch
        {
            // Bei defekter/fehlender Settings-Datei: Defaults verwenden.
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Speichern ist "best effort" - App bleibt trotzdem funktionsfähig.
        }
    }
}

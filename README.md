# AutoHideDesktopIcons11

Eine moderne, quelloffene Neuimplementierung des klassischen **AutoHideDesktopIcons**
(SoftwareOK) im **Fluent-Design für Windows 11** — gebaut mit **WinUI 3 (.NET 8 /
Windows App SDK)**.

Blendet die Desktop-Symbole (und optional die Taskleiste) nach einer einstellbaren
Zeit ohne Mausbewegung aus, damit der Blick auf das Hintergrundbild frei wird.
Ein Klick auf den Desktop (links/mittel/rechts konfigurierbar) oder über das
Desktop-Kontextmenü blendet sie sofort wieder ein.

<img width="256" height="256" alt="AppIcon" src="https://github.com/user-attachments/assets/d64b43dd-ee12-43cd-886c-59f4f18ca031" />


## Funktionsumfang (1:1 zum Original)

- **Deaktivieren** — Funktion komplett pausieren
- **Mit Windows starten** — Autostart-Eintrag (Registry `Run`-Key)
- **Minimiert starten (Infobereich)** — startet direkt im Tray
- **Immer im Vordergrund** — Einstellungsfenster bleibt "always on top"
- **Taskleiste verstecken** — blendet zusätzlich Primär- und Sekundär-Taskleisten aus
- **Zeigen, durch Klick auf den Desktop**: linke / mittlere / rechte Maustaste,
  einzeln aktivierbar
- **Desktop-Kontextmenü** — fügt einen Rechtsklick-Menüpunkt auf dem Desktop
  hinzu, um sofort umzuschalten
- Einstellbare Inaktivitätszeit (**3–100 Sekunden**), Live-Fortschrittsanzeige

## Unterschiede / bewusste Vereinfachungen gegenüber dem Original

- Kein Closed-Source-Binary, kein Ads/Werbung — komplett quelloffen (MIT-Lizenz)
- Einstellungen werden **sofort live angewendet** (kein separater "Speichern"-Button)
- "Weitere Optionen" ist als Info-Expander umgesetzt statt als eigener Dialog
- Der Desktop-Kontextmenü-Eintrag ruft die bereits laufende Instanz per Fenster-
  Message auf; die App muss dafür laufen (z. B. via Autostart / Tray)

## Build

Voraussetzungen: [.NET 8 SDK](https://dotnet.microsoft.com/download) mit
Workload `wasdk` (Windows App SDK), Windows 10 1809+ oder Windows 11.

```powershell
dotnet workload install wasdk
dotnet publish src/AutoHideDesktopIcons11/AutoHideDesktopIcons11.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:WindowsAppSDKSelfContained=true
```

Das Ergebnis liegt danach als **portable .exe** (+ `Assets`-Ordner) unter
`src/AutoHideDesktopIcons11/bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/`
— einfach als ZIP mitnehmen, kein Installer nötig.

## Automatischer Build via GitHub Actions

`.github/workflows/build.yml` baut bei jedem Push/PR automatisch für
**x64, x86 und ARM64** auf einem `windows-latest`-Runner, packt das Ergebnis
jeweils als ZIP und lädt es als Workflow-Artifact hoch. Bei einem Tag im Format
`vX.Y.Z` wird zusätzlich automatisch ein GitHub Release mit den ZIPs erstellt:

```bash
git tag v1.0.0
git push origin v1.0.0
```

## Projektstruktur

```
src/AutoHideDesktopIcons11/
├── App.xaml(.cs)              Single-Instance-Handling, Kommandozeilen-Args
├── MainWindow.xaml(.cs)       UI + gesamte Steuerlogik
├── Interop/NativeMethods.cs   Sämtliche Win32-P/Invokes an einem Ort
├── Services/
│   ├── DesktopIconsController.cs   Ein-/Ausblenden der Icons (SysListView32)
│   ├── TaskbarController.cs        Ein-/Ausblenden Taskleiste(n)
│   ├── IdleTracker.cs              Inaktivitätszeit via GetLastInputInfo
│   ├── DesktopClickWatcher.cs      Low-Level-Mouse-Hook auf Desktop-Klicks
│   ├── TrayIconService.cs          Tray-Icon inkl. Kontextmenü
│   ├── AutostartService.cs         Registry Run-Key
│   └── ContextMenuService.cs       Desktop-Rechtsklick-Menüeintrag
├── Models/AppSettings.cs      Einstellungsmodell + JSON-Persistenz (%AppData%)
└── Assets/AppIcon.svg/.ico/.png    Fluent-Icon (Quelle + gerenderte Formate)
```

## Icon

`Assets/AppIcon.svg` ist die Vektor-Quelle im Fluent-Stil (abgerundete Kachel,
Akzent-Blauverlauf wie bei nativen Windows-11-Apps, Monitor-Glyph mit
ausblendenden Icon-Quadraten). `AppIcon.ico` (16–256px, für Fenster/Taskleiste/
Tray) und `AppIcon.png` (für die XAML-UI) werden daraus gerendert.

## Lizenz

MIT — siehe [LICENSE](LICENSE).

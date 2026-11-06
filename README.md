# Fokus-App

Eine Windows-App für konzentrierte Lernphasen. Erster Prototyp mit C#, .NET 10 und WPF.

## Aktuell möglich

- Modi Lernen, Schule und Eigener Modus mit jeweils eigener Programmliste und Dauer.
- Fokusphase für 1–480 Minuten mit Countdown starten.
- Ausgewählte EXE-Dateien werden beim Wechsel in den Vordergrund minimiert. Prozesse werden nicht beendet, offene Dokumente bleiben erhalten.
- Einstellungen während einer laufenden Sitzung gesperrt.
- Vorzeitig beenden nach 30 Sekunden Bedenkzeit; alternativ weiter fokussieren.
- Normales Schließen wird während einer Sitzung verhindert. Nach einem erzwungenen App-Neustart wird eine noch laufende Sitzung wieder aufgenommen.
- Einstellungen liegen lokal unter `%LOCALAPPDATA%\Fokus-App\state.json`.

Die Sperre ist eine Fokushilfe: Task-Manager und Windows bleiben zugänglich. Während die App beendet ist, kann sie keine Programme minimieren. Programme mit höheren Rechten können möglicherweise nicht erfasst werden. Webseiten, Benachrichtigungssteuerung, Pausenautomatik und Schul-WLAN-Erkennung sind noch nicht implementiert. Wähle keinen kompletten Browser, wenn du ihn zum Lernen brauchst.

## Wie ein normales Windows-Programm starten

Auf diesem Laptop gibt es auf dem Desktop und im Startmenü eine Verknüpfung **Fokus-App** mit eigenem Symbol. Beide öffnen direkt die Windows-App, ohne Befehlsfenster. Die veröffentlichte Version bringt die benötigte .NET-Laufzeit mit.

Die Verknüpfungen zeigen auf `artifacts\windows\Fokus-App.exe` im Projektordner. Diesen Ordner deshalb nicht verschieben oder löschen. Nach einem Verschieben lassen sich die Verknüpfungen erneut anlegen:

```powershell
.\tools\Install-Shortcuts.ps1
```

Das Skript erstellt die Verknüpfungen für den aktuellen Benutzer; ein systemweites Installationsprogramm ist noch nicht enthalten. Das App-Symbol liegt als SVG und mehrstufige ICO-Datei unter `src/Fokus.App/Assets`. Unter Windows kann es mit `tools/New-AppIcon.ps1` neu erzeugt werden.

## Entwickeln und starten

Voraussetzung: Windows und .NET 10 SDK (siehe [Microsofts Installationsanleitung](https://learn.microsoft.com/dotnet/core/install/windows)).

```powershell
dotnet run --project src/Fokus.App
```

Bei der lokalen SDK-Installation in diesem Projekt:

```powershell
.\.tools\dotnet\dotnet.exe run --project src/Fokus.App
```

## Prüfen und bauen

```powershell
dotnet run --project tests/Fokus.Checks
dotnet build src/Fokus.App -c Release
dotnet publish src/Fokus.App -c Release -r win-x64 --self-contained true -o artifacts/windows
```

Die Prüfungen testen Ablauf, Abbruchverzögerung und Wiederherstellung nach Neustart. Für die Programmsperre manuell eine unkritische Testanwendung wählen, Fokus für eine Minute starten und die Anwendung öffnen. Sie sollte minimiert werden und nach Ablauf wieder benutzbar sein.

## Idee

Die ursprüngliche Beschreibung steht in [LilErklärung.md](LilErklärung.md).

Für die Entwicklung bleibt Start.cmd verfügbar. Im Alltag die Desktop- oder Startmenü-Verknüpfung verwenden. Build-Ausgaben, lokale Verknüpfungen und das lokale SDK werden nicht auf GitHub hochgeladen.

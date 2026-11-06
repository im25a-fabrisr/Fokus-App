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

Lokal kannst du auch Start.cmd doppelt anklicken. Es startet die gebaute App oder verwendet das vorhandene SDK. Build-Ausgaben und das lokale SDK werden nicht auf GitHub hochgeladen.

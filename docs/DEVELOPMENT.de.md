# PopTiles

Gruppen als angepinnte Icons in der Windows-Taskleiste. Ein Klick öffnet eine Kachel mit den Einträgen der Gruppe.

## Benutzung

1. `dist\PopTiles.exe` starten, später auch über das Startmenü: „PopTiles“.
2. Mit **New group** eine Gruppe anlegen und benennen. Windows fragt danach unten rechts, ob sie an die Taskleiste angeheftet werden soll. Das kann einige Sekunden dauern, weil Windows die neue Verknüpfung erst erkennen muss.
3. Einträge hinzufügen: **Add app** (auch Store-Apps), **File**, **Folder**, **Web address** oder per Drag & Drop in die Liste.

Die Rückfrage schreibt Windows vor, und Windows-Benachrichtigungen müssen dafür eingeschaltet sein. Später lässt sich eine Gruppe über **Pin to taskbar** nachholen. Beim Löschen einer Gruppe wird ihr Icon automatisch gelöst.

## Bedienung der Kachel

- Klick auf einen Eintrag startet ihn und schließt die Kachel.
- Klick daneben, Esc oder ein zweiter Klick auf das Taskleisten-Icon schließt sie.
- Tab und Enter funktionieren ebenfalls, mit `Win` + Zahl lässt sich die Gruppe öffnen.
- Das Zahnrad öffnet die Bearbeitung.

## Gut zu wissen

- Die Gruppen sind Starter: Ein laufendes Programm bekommt weiterhin ein eigenes Taskleisten-Icon.
- Die EXE muss im `dist`-Ordner bleiben, weil angepinnte Icons auf ihren Pfad zeigen. Nach einem Verschieben müssen die Gruppen neu angepinnt werden.
- Das Gruppen-Icon zeigt automatisch die ersten vier Einträge. Über **Change icon** wählst du stattdessen ein Symbol mit Farbe, das Icon eines Eintrags, ein installiertes Icon aus der Galerie (alle Apps und die Windows-Sammlungen) oder ein eigenes Bild (PNG, JPG, BMP, ICO) oder das Icon eines Programms (EXE, DLL). Falls die Taskleiste danach noch das alte Bild zeigt, hilft eine Neuanmeldung.
- Web-Adressen bekommen eine Buchstaben-Kachel. Favicons werden bewusst nicht aus dem Netz geladen.
- Beim Umbenennen behält ein angeheftetes Icon seinen alten Namen, bis es neu angeheftet wird.

## Daten

| Was | Wo |
|---|---|
| Gruppen und Icons | Ordner `data` neben der EXE |
| Verknüpfungen | Startmenü, Ordner `PopTiles` |

Die Daten liegen bewusst nicht in `%APPDATA%`: Startet eine als Paket installierte App das Tool, leitet Windows diesen Ordner in einen privaten Bereich um, den die Taskleiste nicht sieht.

Beim Start der Verwaltung werden alle Verknüpfungen und angepinnten Icons aktualisiert.

Zum Entfernen einer Version ohne Installer: Icons von der Taskleiste lösen und den Startmenü-Ordner sowie den Projektordner löschen. Das Programm selbst legt keine Einträge in der Registry an.

## An andere weitergeben

Weiterzugeben ist nur `installer\output\PopTiles-Setup-1.0.0.exe`.

- Die Installation läuft pro Benutzer nach `%LOCALAPPDATA%\Programs\PopTiles` und braucht keine Admin-Rechte.
- Eigene Gruppen sind nicht enthalten.
- Das Setup ist nicht signiert: Windows SmartScreen kann warnen. Über „Weitere Informationen“ und „Trotzdem ausführen“ geht es weiter.
- Die Deinstallation läuft über die Windows-Einstellungen und fragt, ob auch die Gruppen gelöscht werden sollen. Angepinnte Icons muss man selbst von der Taskleiste lösen.

## Neu bauen

Benötigt das .NET SDK. Die fertige EXE läuft ohne Zusatzinstallation mit dem in Windows 11 enthaltenen .NET Framework 4.8.

```powershell
.\build.ps1
.\build-installer.ps1
```

`build.ps1` baut das Programm, `build-installer.ps1` zusätzlich das Setup. Inno Setup kommt dabei als NuGet-Paket und wird nicht im System installiert.

Vorher offene Kacheln und die Verwaltung schließen.

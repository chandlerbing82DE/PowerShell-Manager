# PowerShell Manager (psmgr.exe)

## Beschreibung
Diese Windows Forms (WinForms)-Anwendung ist ein professionelles Werkzeug zur Verwaltung, Analyse und Konsolidierung von PowerShell-Skripten. Das Tool scannt ausgewählte Quellordner (einschließlich Unterordner) nach `.ps1`-Dateien, führt eine lokale Ähnlichkeitsanalyse durch und nutzt künstliche Intelligenz (OpenAI & Google Gemini), um Skripte zu analysieren, Duplikate zu bewerten und neue Dateinamen vorzuschlagen.

Die Anwendung speichert alle Skriptinformationen, Quellpfade und virtuellen Alben in einer lokalen SQLite-Datenbank, sodass analysierte Daten nicht verloren gehen.

---

## Hauptfunktionen

### 1. Skrypt-Analyse & Beschreibung via AI
- Massen- oder Einzelanalyse von PowerShell-Skripten mit Modellen von **OpenAI** (z. B. GPT-4o) und **Google Gemini** (z. B. Gemini 2.5 Flash).
- Automatische Erstellung einer prägnanten, deutschsprachigen Funktionsbeschreibung.
- Visuelle Statusanzeige (Ausstehend ⏳, Erfolg 🟢, Fehler 🔴).

### 2. Lokale Duplikatsuche & KI-Bereinigung
- **Vektorisierte Ähnlichkeitsanalyse:** Berechnet kosinusbasierte Textähnlichkeiten lokal, um Skript-Duplikate in Gruppen (np. G001, G002) zusammenzufassen.
- **Code-Vergleich (Diff-Ansicht):** Side-by-Side-Vergleich von zwei ähnlichen Skripten in einer übersichtlichen RichTextBox-Ansicht.
- **KI-gestützte Bereinigung:** Das ausgewählte AI-Modell analysiert die Duplikate und entscheidet im JSON-Format, welches Skript behalten (z. B. wegen Modernität, Fehlerbehandlung) und welches gelöscht werden kann.
- **Massenlöschung:** Möglichkeit, alle von der KI zum Löschen empfohlenen Duplikate mit einem Klick physisch von der Festplatte zu entfernen.

### 3. Virtuelle Bibliothek (Alben)
- Strukturierung von Skripten in einer hierarchischen Baumstruktur (TreeView) über Alben i Unteralben.
- Drag-and-Drop-Unterstützung zum einfachen Verschieben von Alben und Skripten.
- Physische Dateien bleiben an ihrem Ursprungsort – die Zuordnung erfolgt rein virtuell in der Datenbank.

### 4. Intelligente Dateiumbenennung (Rename)
- Vorschlagen von standardisierten, professionellen Dateinamen (PowerShell Verb-Noun oder PascalCase) basierend auf der KI-Skriptbeschreibung.
- Automatisches Hinzufügen der eindeutigen Skript-ID als Präfix (z. B. `0001_Get-ActiveDirectoryUsers.ps1`).
- Komfortable Massenumbenennung direkt auf der Festplatte inklusive automatischer Pfadaktualisierung in der Datenbank.

---

## Technische Details & Architektur
- **Framework:** .NET 10.0 (Windows Forms).
- **Datenbank:** SQLite (`System.Data.SQLite.Core`), die standardmäßig im Benutzerverzeichnis (`AppData/Roaming/PowerShellScriptAnalyzer`) abgelegt wird.
- **Modell-Vorauswahl:** Standardmäßig ist beim Start das Modell **Gemini 2.5 Flash** ausgewählt, da es das beste Preis-Leistungs-Verhältnis für Code-Analysen bietet.
- **Sicherheitsüberprüfung (PIN):** Um eine unbeabsichtigte Massenanalyse aller Skripte (Kostenrisiko beim API-Anbieter) zu verhindern, ist die globale Analyse ohne vorherige Checkbox-Auswahl durch den PIN **`8203`** geschützt.

---

## Wichtige Hinweise zur Konfiguration
1. **Einstellungen-Dialog:** API-Schlüssel für OpenAI i Google Gemini werden nicht mehr im Code hinterlegt, sondern direkt über die Schaltfläche **⚙ Einstellungen** im Hauptfenster konfiguriert i verschlüsselt in der SQLite-Datenbank gespeichert.
2. **Datenbankverwaltung:** Im Einstellungsfenster kann der Pfad zur SQLite-Datenbank flexibel geändert, eine bestehende Datenbank geöffnet (`📂`) lub kopiert/gesichert (`💾`) werden.

---

## Empfohlene nächste Ausbaustufen
- **Retry-Logik und Rate-Limit-Behandlung:** Abfangen von API-Fehlern (z. B. HTTP 429) bei sehr vielen parallelen Anfragen.
- **Besseres Chunking / Token-Management:** Optimierte Aufteilung von extrem großen Skripten vor dem Senden an die API.
- **Erweiterte Syntaxprüfung:** Integration von `PSScriptAnalyzer` zur lokalen Qualitäts- und Sicherheitsprüfung vor der AI-Analyse.
- **HTML/Markdown-Report:** Exportfunktion für die gesamte Bibliothek als strukturierte Dokumentation.
- **SHA256-Caching:** Vermeidung doppelter API-Kosten durch Erkennung identischer Datei-Hashes.
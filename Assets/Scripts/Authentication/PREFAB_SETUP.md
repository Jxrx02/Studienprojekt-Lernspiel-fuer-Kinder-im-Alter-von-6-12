# Anleitung zum Erstellen eines Octolearn-Auth-Prefabs

Diese Anleitung führt Sie durch die Schritte zum Erstellen eines wiederverwendbaren Prefabs für die Octolearn-Authentifizierung und API-Integration.

## 1. Erstellen der Grundstruktur

1. Erstellen Sie ein leeres GameObject in der Szene und nennen Sie es "OctoLearnSystem"
2. Fügen Sie dem GameObject das `OctoSystemInitializer`-Skript hinzu
3. Konfigurieren Sie die API-Anmeldedaten im Inspector (apiKey und apiSecret)

## 2. Erstellen der UI-Elemente

### 2.1 Auth Canvas erstellen

1. Erstellen Sie ein Canvas (Create > UI > Canvas) und nennen Sie es "OctoAuthCanvas"
2. Fügen Sie ein EventSystem hinzu, falls noch nicht vorhanden (Create > UI > Event System)
3. Setzen Sie das Canvas als Kind des "OctoLearnSystem" GameObjects

### 2.2 Login-Panel erstellen

1. Erstellen Sie ein Panel innerhalb des Canvas und nennen Sie es "LoginPanel"
2. Fügen Sie folgende UI-Elemente zum Panel hinzu:
   - Ein Button mit dem Text "Bei Octolearn anmelden"
   - Ein Button mit dem Text "Abmelden" (initial deaktiviert)
   - Ein TextMeshPro Text für Statusmeldungen
   - Ein TextMeshPro Text mit dem Titel "Octolearn Login"
3. Fügen Sie dem LoginPanel das `OctoAuthUI`-Skript hinzu
4. Verbinden Sie die UI-Elemente mit den entsprechenden Feldern im Inspector:
   - loginButton → Der "Bei Octolearn anmelden" Button
   - logoutButton → Der "Abmelden" Button
   - statusText → Der Status-Text
   - loggedInPanel → Ein neues GameObject, das den Abmelde-Button enthält
   - loggedOutPanel → Ein neues GameObject, das den Anmelde-Button enthält

### 2.3 Token-Eingabe-Panel erstellen

1. Erstellen Sie ein Panel innerhalb des Canvas und nennen Sie es "TokenInputPanel"
2. Fügen Sie folgende UI-Elemente zum Panel hinzu:
   - Ein TMP_InputField für die Token-Eingabe
   - Ein Button mit dem Text "Token übernehmen"
   - Ein Button mit dem Text "Abbrechen"
   - Ein TextMeshPro Text mit Anweisungen
3. Fügen Sie dem TokenInputPanel das `TokenCallbackHandler`-Skript hinzu
4. Verbinden Sie die UI-Elemente mit den entsprechenden Feldern im Inspector:
   - tokenInputField → Das TMP_InputField
   - submitButton → Der "Token übernehmen" Button
   - cancelButton → Der "Abbrechen" Button
   - mainPanel → Das TokenInputPanel selbst
   - instructionsText → Der Anweisungs-Text

### 2.4 Session-Eingabe-Panel erstellen

1. Erstellen Sie ein Panel innerhalb des Canvas und nennen Sie es "SessionInputPanel"
2. Fügen Sie folgende UI-Elemente zum Panel hinzu:
   - Ein TMP_InputField für die Session-ID-Eingabe
   - Ein Button mit dem Text "Session-ID übernehmen"
   - Ein Button mit dem Text "Abbrechen"
   - Ein TextMeshPro Text mit Anweisungen
   - Ein TextMeshPro Text, der den JavaScript-Code anzeigt
3. Fügen Sie dem SessionInputPanel das `SessionInputHandler`-Skript hinzu
4. Verbinden Sie die UI-Elemente mit den entsprechenden Feldern im Inspector:
   - sessionIdInput → Das TMP_InputField
   - submitButton → Der "Session-ID übernehmen" Button
   - cancelButton → Der "Abbrechen" Button
   - statusText → Der Status-Text
   - mainPanel → Das SessionInputPanel selbst
   - jsCodeText → Der Text mit dem JavaScript-Code

### 2.5 Demo-Panel erstellen (optional)

1. Erstellen Sie ein Panel innerhalb des Canvas und nennen Sie es "DemoPanel"
2. Fügen Sie folgende UI-Elemente zum Panel hinzu:
   - Buttons für "Fragen abrufen", "Antwort senden" und "Benutzerinfo abrufen"
   - Ein TMP_InputField für die Antwort-Eingabe
   - Ein TextMeshPro Text für die Frage-Anzeige
   - Ein TextMeshPro Text für Statusmeldungen
   - Ein Container für Multiple-Choice-Optionen
3. Erstellen Sie ein Button-Prefab für Multiple-Choice-Optionen
4. Fügen Sie dem DemoPanel das `OctoLearnDemo`-Skript hinzu
5. Verbinden Sie die UI-Elemente mit den entsprechenden Feldern im Inspector

## 3. Prefab erstellen

1. Aktivieren Sie anfänglich nur das LoginPanel, die anderen Panels sollten deaktiviert sein
2. Ziehen Sie das "OctoLearnSystem" GameObject aus der Hierarchie in den Project-View, um ein Prefab zu erstellen
3. Speichern Sie das Prefab unter "Assets/Prefabs/OctoLearnSystem"

## 4. Verwendung des Prefabs

1. Ziehen Sie das OctoLearnSystem-Prefab in jede Szene, in der Sie die Octolearn-Authentifizierung benötigen
2. Passen Sie bei Bedarf die API-Anmeldedaten im OctoSystemInitializer an
3. Rufen Sie die Komponenten über die statischen Instance-Properties auf:
   ```csharp
   OctoAuthManager.Instance.IsAuthenticated()
   OctoApiService.Instance.GetNextQuestions(...)
   ```

## 5. Anpassung des UI

1. Sie können das Design der Panels anpassen, indem Sie Farben, Schriftarten und Layout ändern
2. Achten Sie darauf, die Referenzen im Inspector nicht zu verlieren
3. Sie können auch zusätzliche UI-Elemente hinzufügen, ohne die Funktionalität zu beeinträchtigen 
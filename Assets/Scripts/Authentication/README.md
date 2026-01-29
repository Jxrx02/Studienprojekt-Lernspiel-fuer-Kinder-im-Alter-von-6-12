# Octolearn Authentication System for Unity

Dieses System ermöglicht die Authentifizierung mit Octolearn über OAuth und JWT-Tokens sowie den Zugriff auf alle Octolearn-API-Endpoints.

## Komponenten

1. **OctoAuthManager** - Kernkomponente, die die Authentifizierung und Token-Verwaltung übernimmt
2. **OctoAuthUI** - UI-Komponente für Login/Logout-Funktionalität
3. **OctoApiService** - Service zum Abrufen von Daten von der Octolearn API
4. **TokenCallbackHandler** - Hilft bei der Verarbeitung von Token nach Browser-Redirection
5. **SecurityUtils** - Hilfsfunktionen zur Verschlüsselung und Sicherung von sensiblen Daten
6. **OctoSessionManager** - Verwaltet Okta-Sessions für Session-Ende und andere Operationen
7. **SessionInputHandler** - UI zum manuellen Eingeben von Session-IDs
8. **OctoLearnDemo** - Beispiel-Komponente zur Demonstration der Fragen/Antworten-Funktionalität

## Implementierungsdetails

### Sicherheit

- JWT-Tokens werden verschlüsselt im PlayerPrefs gespeichert
- Die Verschlüsselung ist geräteabhängig (basierend auf Geräte-ID)
- Token-Expiration wird beim Laden überprüft
- Token werden nur im verschlüsselten Format persistent gespeichert

### Authentifizierungsablauf

1. Benutzer startet Authentifizierung über OctoAuthUI
2. OctoAuthManager öffnet den Browser mit den korrekten OAuth-Parametern
3. Nach erfolgreicher Authentifizierung auf oktolearn.de wird der Benutzer zu auth.octolearn.de weitergeleitet
4. Der Benutzer kopiert den erhaltenen Token manuell
5. TokenCallbackHandler nimmt den Token entgegen und reicht ihn an OctoAuthManager weiter
6. OctoAuthManager speichert den Token verschlüsselt und benachrichtigt das System über erfolgreiche Authentifizierung

### API-Zugriff

- OctoApiService nutzt den gespeicherten Token und API-Keys für authentifizierte API-Requests
- Generische Request-Methode für verschiedene Endpoints
- Unterstützt alle aktuellen Octolearn-API-Funktionen:
  - Abrufen von Fragen (GetNextQuestions)
  - Einreichen von Antworten (SubmitAnswer)
  - Benutzerinfo abrufen (GetUserInfo)
  - Session beenden (TerminateSession)
  - Highscores speichern und abrufen (SaveGameHighscore/GetGameHighscores)
  - Lernordner erstellen (CreateLearningContentFolder)
  - Lerninhalte erstellen (CreateLearningContents)

## Setup-Anleitung

### 1. Vorbereitung der Szene

1. Erstellen Sie in Ihrer Szene ein Canvas für die Authentifizierung
2. Fügen Sie die folgenden UI-Elemente hinzu:

   a. **Auth Panel** (GameObject)
      - Fügen Sie ein `OctoAuthUI` Script hinzu
      - Enthält Login/Logout Buttons und Status-Text

   b. **Token Input Panel** (GameObject)
      - Fügen Sie ein `TokenCallbackHandler` Script hinzu
      - Enthält ein TMP_InputField für Token-Eingabe
      - Enthält Submit/Cancel Buttons
    
   c. **Session Input Panel** (GameObject)
      - Fügen Sie ein `SessionInputHandler` Script hinzu
      - Enthält ein TMP_InputField für Session-ID-Eingabe
      - Enthält Submit/Cancel Buttons

   d. **Demo Panel** (optional) (GameObject)
      - Fügen Sie ein `OctoLearnDemo` Script hinzu
      - Enthält UI-Elemente für die Demonstration von Fragen/Antworten

### 2. API-Konfiguration

1. Im OctoApiService:
   - Setzen Sie die korrekten API-Keys in den Inspector-Feldern:
     - apiKey: Ihr Octolearn API-Key
     - apiSecret: Ihr Octolearn API-Secret

### 3. Prefab-Setup

Für einfache Integration können Sie ein fertiges Prefab erstellen:

1. Erstellen Sie ein neues Prefab "OctoAuthSystem" mit:
   - OctoAuthManager
   - OctoApiService
   - OctoSessionManager
   - UI Canvas mit OctoAuthUI, TokenCallbackHandler und SessionInputHandler

2. Dieses Prefab kann dann in jede Szene gezogen werden, in der die Authentifizierung benötigt wird.

## API-Nutzungsbeispiele

### Authentifizierung

```csharp
// Token abrufen (falls authentifiziert)
string token = OctoAuthManager.Instance.GetToken();
```

### Fragen abrufen

```csharp
// 3 Fragen vom Octolearn System abrufen
OctoApiService.Instance.GetNextQuestions(
    3,
    (questions) => {
        // Fragen verarbeiten
        foreach (var question in questions) {
            Debug.Log(question.question);
        }
    },
    (error) => {
        Debug.LogError("Fehler: " + error);
    }
);
```

### Antwort einreichen

```csharp
// Antwort auf eine Frage einreichen
OctoApiService.Instance.SubmitAnswer(
    questionObject,  // FrontendContent-Objekt
    "Meine Antwort",
    (response) => {
        Debug.Log("Korrekt: " + response.correct);
    },
    (error) => {
        Debug.LogError("Fehler: " + error);
    }
);
```

### Session beenden

```csharp
// Session-ID manuell setzen oder abrufen
string sessionId = "..."; // Von Okta-API
OctoSessionManager.Instance.SetSessionId(sessionId);

// Session beenden
OctoSessionManager.Instance.EndSession(
    (success) => {
        Debug.Log("Session beendet");
    },
    (error) => {
        Debug.LogError("Fehler: " + error);
    }
);
```

### Nutzerdaten abrufen

```csharp
OctoApiService.Instance.GetUserInfo(
    (userInfo) => {
        Debug.Log($"Hallo {userInfo.firstName} {userInfo.lastName}");
    },
    (error) => {
        Debug.LogError("Fehler: " + error);
    }
);
```

## Mögliche Erweiterungen

1. **Silent Refresh** - Automatisches Erneuern des Tokens
2. **Session Persistenz** - Längeres Speichern der Authentifizierung
3. **Lokaler Server** - Einrichten eines lokalen Servers für direkten Callback in Unity
4. **WebGL-Optimierung** - Bessere Integration für WebGL-Builds mit JS-Interop
5. **Erweiterte Fehlerbehandlung** - Differenziertere Fehlerbehandlung und Retry-Mechanismen 
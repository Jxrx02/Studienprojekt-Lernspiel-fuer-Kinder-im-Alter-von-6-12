using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class OctoSessionManager : MonoBehaviour
{
    private static OctoSessionManager _instance;
    public static OctoSessionManager Instance { get { return _instance; } }
    
    private const string SESSION_API_URL = "https://id.octolearn.de/api/v1/sessions/me";
    
    private string _currentSessionId = null;
    
    public string CurrentSessionId { get { return _currentSessionId; } }
    
    private void Awake()
    {
        // Singleton pattern
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Get the current Okta session ID
    public void GetSessionId(Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(RequestSessionId(onSuccess, onError));
    }
    
    private IEnumerator RequestSessionId(Action<string> onSuccess, Action<string> onError)
    {
        // Create a Unity WebGL link that the user can click to fetch the session
        // Since Unity WebGL can't directly access the session due to CORS limitations,
        // we need a different approach for WebGL builds
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        // For WebGL builds, we have to rely on browser interop
        Debug.Log("WebGL build detected, session ID must be retrieved via browser interop");
        onError?.Invoke("Session ID retrieval not directly supported in WebGL. Use JavaScript interop instead.");
        yield break;
        #else
        
        // For non-WebGL builds, we can try a direct request
        // Note: This will still likely fail due to CORS restrictions unless we're running in the browser
        UnityWebRequest request = UnityWebRequest.Get(SESSION_API_URL);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");
        
        // Add withCredentials flag for CORS requests with cookies
        request.downloadHandler = new DownloadHandlerBuffer();
        
        // Send the request
        yield return request.SendWebRequest();
        
        // Check for errors
        if (request.result == UnityWebRequest.Result.ConnectionError || 
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Error getting session ID: {request.error}");
            onError?.Invoke($"Failed to retrieve session ID: {request.error}");
            yield break;
        }
        
        try
        {
            // Parse the response
            string jsonResponse = request.downloadHandler.text;
            Debug.Log("Session response: " + jsonResponse);
            
            SessionResponse response = JsonUtility.FromJson<SessionResponse>(jsonResponse);
            if (response != null && !string.IsNullOrEmpty(response.id))
            {
                _currentSessionId = response.id;
                onSuccess?.Invoke(_currentSessionId);
            }
            else
            {
                onError?.Invoke("Invalid session response");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing session response: {e.Message}");
            onError?.Invoke($"Error parsing session: {e.Message}");
        }
        #endif
    }
    
    // Set the session ID manually (useful when obtained through browser interop)
    public void SetSessionId(string sessionId)
    {
        if (!string.IsNullOrEmpty(sessionId))
        {
            _currentSessionId = sessionId;
            Debug.Log("Session ID set manually: " + sessionId);
        }
    }
    
    // End the current Okta session
    public void EndSession(Action<bool> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(_currentSessionId))
        {
            onError?.Invoke("No session ID available");
            return;
        }
        
        OctoApiService apiService = OctoApiService.Instance;
        if (apiService != null)
        {
            apiService.TerminateSession(_currentSessionId, onSuccess, onError);
        }
        else
        {
            onError?.Invoke("API service not available");
        }
    }
}

[Serializable]
public class SessionResponse
{
    public string id;
    public string userId;
    public string login;
    // Add other session properties as needed
} 
using UnityEngine;

/// <summary>
/// This component initializes all Octolearn system services when attached to a GameObject.
/// Can be used as part of a prefab to quickly add all required services to a scene.
/// </summary>
public class OctoSystemInitializer : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private bool initializeAuthManager = true;
    [SerializeField] private bool initializeApiService = true;
    [SerializeField] private bool initializeSessionManager = true;

    [Header("API Configuration")]
    [SerializeField] private string apiKey = "63275f89-cf80-4c1c-a4e0-773b5d1af0ae";
    [SerializeField] private string apiSecret = "98faeb33-694c-4960-9c47-628be6d584e2";

    private void Awake()
    {
        // Create services that don't exist yet
        if (initializeAuthManager && OctoAuthManager.Instance == null)
        {
            GameObject authObject = new GameObject("OctoAuthManager");
            authObject.AddComponent<OctoAuthManager>();
            authObject.transform.SetParent(transform);
        }

        if (initializeApiService && OctoApiService.Instance == null)
        {
            GameObject apiObject = new GameObject("OctoApiService");
            OctoApiService apiService = apiObject.AddComponent<OctoApiService>();
            
            // Use reflection to set the API key and secret
            var keyField = typeof(OctoApiService).GetField("apiKey", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            var secretField = typeof(OctoApiService).GetField("apiSecret", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (keyField != null) keyField.SetValue(apiService, apiKey);
            if (secretField != null) secretField.SetValue(apiService, apiSecret);
            
            apiObject.transform.SetParent(transform);
        }

        if (initializeSessionManager && OctoSessionManager.Instance == null)
        {
            GameObject sessionObject = new GameObject("OctoSessionManager");
            sessionObject.AddComponent<OctoSessionManager>();
            sessionObject.transform.SetParent(transform);
        }
    }

    // Provide public methods to set API credentials at runtime
    public void SetApiCredentials(string apiKey, string apiSecret)
    {
        OctoApiService apiService = OctoApiService.Instance;
        if (apiService != null)
        {
            var keyField = typeof(OctoApiService).GetField("apiKey", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            var secretField = typeof(OctoApiService).GetField("apiSecret", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (keyField != null) keyField.SetValue(apiService, apiKey);
            if (secretField != null) secretField.SetValue(apiService, apiSecret);
        }
    }
} 
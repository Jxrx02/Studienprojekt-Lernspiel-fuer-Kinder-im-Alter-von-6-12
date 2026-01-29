using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OctoAuthManager : MonoBehaviour
{
    private static OctoAuthManager _instance;
    public static OctoAuthManager Instance { get { return _instance; } }

    // Octolearn OAuth configuration
    private const string CLIENT_ID = "0oa8dbw900zb67NFJ5d5";
    private const string REDIRECT_URI = "https://auth.octolearn.de/";
    private const string AUTH_ENDPOINT = "https://id.octolearn.de/oauth2/default/v1/authorize";
    private const string STATE = "octolearn";
    
    // Token storage key
    private const string TOKEN_PREFS_KEY = "OctoJwtToken";
    private const string TOKEN_EXPIRY_KEY = "OctoJwtExpiry";

    // Current token
    private string _jwtToken = null;
    private DateTime _tokenExpiry = DateTime.MinValue;

    // Event for token updates
    public event Action<bool> OnAuthStateChanged;

    private void Awake()
    {
        // Singleton pattern
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Try to load token from PlayerPrefs
            LoadTokenFromStorage();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Loads token from PlayerPrefs if available
    private void LoadTokenFromStorage()
    {
        if (PlayerPrefs.HasKey(TOKEN_PREFS_KEY))
        {
            string encryptedToken = PlayerPrefs.GetString(TOKEN_PREFS_KEY);
            _jwtToken = SecurityUtils.Decrypt(encryptedToken);
            
            // Check expiry
            if (PlayerPrefs.HasKey(TOKEN_EXPIRY_KEY))
            {
                string encryptedExpiry = PlayerPrefs.GetString(TOKEN_EXPIRY_KEY);
                string expiryTicks = SecurityUtils.Decrypt(encryptedExpiry);
                
                if (!string.IsNullOrEmpty(expiryTicks))
                {
                    _tokenExpiry = new DateTime(Convert.ToInt64(expiryTicks));
                    
                    // If token is expired, clear it
                    if (DateTime.Now > _tokenExpiry)
                    {
                        ClearToken();
                    }
                }
            }
        }
    }

    // Save token to PlayerPrefs
    private void SaveTokenToStorage(string token, DateTime expiry)
    {
        string encryptedToken = SecurityUtils.Encrypt(token);
        string encryptedExpiry = SecurityUtils.Encrypt(expiry.Ticks.ToString());
        
        PlayerPrefs.SetString(TOKEN_PREFS_KEY, encryptedToken);
        PlayerPrefs.SetString(TOKEN_EXPIRY_KEY, encryptedExpiry);
        PlayerPrefs.Save();
    }

    // Clear token from memory and storage
    public void ClearToken()
    {
        _jwtToken = null;
        _tokenExpiry = DateTime.MinValue;
        
        if (PlayerPrefs.HasKey(TOKEN_PREFS_KEY))
        {
            PlayerPrefs.DeleteKey(TOKEN_PREFS_KEY);
        }
        
        if (PlayerPrefs.HasKey(TOKEN_EXPIRY_KEY))
        {
            PlayerPrefs.DeleteKey(TOKEN_EXPIRY_KEY);
        }
        
        PlayerPrefs.Save();
        
        // Notify listeners
        OnAuthStateChanged?.Invoke(false);
    }

    // Check if user is authenticated
    public bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(_jwtToken) && DateTime.Now < _tokenExpiry;
    }

    // Get the current token
    public string GetToken()
    {
        if (IsAuthenticated())
        {
            return _jwtToken;
        }
        return null;
    }

    // Generate a random nonce for authentication
    private string GenerateNonce()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new System.Random();
        var nonce = new StringBuilder(10);
        
        for (int i = 0; i < 10; i++)
        {
            nonce.Append(chars[random.Next(chars.Length)]);
        }
        
        return nonce.ToString();
    }

    // Open the browser for authentication
    public void StartAuthentication()
    {
        string nonce = GenerateNonce();
        string encodedRedirect = UnityWebRequest.EscapeURL(REDIRECT_URI);
        
        // Build the authentication URL
        string authUrl = $"{AUTH_ENDPOINT}?client_id={CLIENT_ID}&redirect_uri={encodedRedirect}" +
                         $"&scope=openid%20profile%20email&response_type=token&response_mode=form_post" +
                         $"&state={STATE}&nonce={nonce}";
        
        // Open in browser
        Application.OpenURL(authUrl);
        
        Debug.Log("Opening browser for authentication: " + authUrl);
        
        // Show the token input panel
        TokenCallbackHandler.Show();
    }

    // This method should be called when receiving the token from an external source
    public void ProcessReceivedToken(string token, int expiresIn)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Received empty token");
            return;
        }
        
        // Calculate expiry time
        DateTime expiry = DateTime.Now.AddSeconds(expiresIn);
        
        // Store token
        _jwtToken = token;
        _tokenExpiry = expiry;
        
        // Save to persistent storage
        SaveTokenToStorage(token, expiry);
        
        // Notify listeners
        OnAuthStateChanged?.Invoke(true);
        
        Debug.Log("Token received and stored. Valid until: " + expiry.ToString());
    }

    // Manual token entry for testing/development
    public void SetManualToken(string token, int expiresIn = 3600)
    {
        ProcessReceivedToken(token, expiresIn);
    }
} 
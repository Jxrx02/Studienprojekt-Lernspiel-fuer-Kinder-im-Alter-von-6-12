using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SessionInputHandler : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField sessionIdInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject mainPanel;
    
    [Header("JavaScript Code")]
    [SerializeField] private TextMeshProUGUI jsCodeText;
    
    private OctoSessionManager sessionManager;
    
    private void Start()
    {
        // Find session manager
        sessionManager = OctoSessionManager.Instance;
        if (sessionManager == null)
        {
            GameObject sessionObject = new GameObject("OctoSessionManager");
            sessionManager = sessionObject.AddComponent<OctoSessionManager>();
            DontDestroyOnLoad(sessionObject);
        }
        
        // Set up button listeners
        submitButton.onClick.AddListener(OnSubmitClicked);
        
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(() => HidePanel());
        }
        
        // Set the JavaScript code
        if (jsCodeText != null)
        {
            jsCodeText.text = @"// Run this in browser console on oktolearn.de:
var baseUrl = 'https://id.octolearn.de';
var xhr = new XMLHttpRequest();
xhr.open('GET', baseUrl + '/api/v1/sessions/me', true);
xhr.withCredentials = true;
xhr.onload = function() {
    console.log('Session ID:', JSON.parse(this.responseText).id);
};
xhr.send();";
        }
        
        // Hide panel initially
        HidePanel();
    }
    
    public void ShowPanel()
    {
        mainPanel.SetActive(true);
        sessionIdInput.text = "";
        statusText.text = "Enter your Okta session ID";
    }
    
    public void HidePanel()
    {
        mainPanel.SetActive(false);
    }
    
    private void OnSubmitClicked()
    {
        string sessionId = sessionIdInput.text.Trim();
        if (string.IsNullOrEmpty(sessionId))
        {
            statusText.text = "Please enter a session ID";
            return;
        }
        
        sessionManager.SetSessionId(sessionId);
        statusText.text = "Session ID set successfully";
        
        // Wait a moment then hide
        Invoke("HidePanel", 1.5f);
    }
    
    // Helper method for other scripts to easily show this panel
    public static void Show()
    {
        SessionInputHandler handler = UnityEngine.Object.FindAnyObjectByType<SessionInputHandler>();
        if (handler != null)
        {
            handler.ShowPanel();
        }
        else
        {
            Debug.LogError("No SessionInputHandler found in the scene.");
        }
    }
} 
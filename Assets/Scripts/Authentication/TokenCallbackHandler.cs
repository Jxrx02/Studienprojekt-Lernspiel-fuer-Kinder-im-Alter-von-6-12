using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TokenCallbackHandler : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField tokenInputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private TextMeshProUGUI instructionsText;
    
    [Header("Settings")]
    [SerializeField] private int defaultTokenExpiry = 3600; // 1 hour in seconds
    
    private OctoAuthManager authManager;
    
    private void Start()
    {
        // Find auth manager
        authManager = OctoAuthManager.Instance;
        if (authManager == null)
        {
            Debug.LogError("TokenCallbackHandler: No OctoAuthManager instance found.");
            return;
        }
        
        // Set up button listeners
        submitButton.onClick.AddListener(OnSubmitClicked);
        
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
        
        // Set instructions
        instructionsText.text = "Bitte kopieren Sie den Token von der Octolearn Auth-Seite hier hinein und klicken Sie auf 'Einreichen'.";
    }
    
    public void ShowPanel()
    {
        mainPanel.SetActive(true);
    }
    
    public void HidePanel()
    {
        mainPanel.SetActive(false);
    }
    
    private void OnSubmitClicked()
    {
        string token = tokenInputField.text.Trim();
        if (!string.IsNullOrEmpty(token))
        {
            // Process the token
            authManager.SetManualToken(token, defaultTokenExpiry);
            tokenInputField.text = ""; // Clear for security
            HidePanel();
        }
        else
        {
            Debug.LogWarning("No token entered.");
        }
    }
    
    // Helper method for other scripts to easily show this panel
    public static void Show()
    {
         TokenCallbackHandler handler = UnityEngine.Object.FindAnyObjectByType<TokenCallbackHandler>();
        if (handler != null)
        {
            handler.ShowPanel();
        }
        else
        {
            Debug.LogError("No TokenCallbackHandler found in the scene.");
        }
    }
} 
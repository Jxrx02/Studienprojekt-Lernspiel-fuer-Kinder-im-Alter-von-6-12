using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OctoAuthUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private GameObject loggedInPanel;
    [SerializeField] private GameObject loggedOutPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TMP_InputField tokenInputField;
    [SerializeField] private Button submitTokenButton;
    [SerializeField] private GameObject tokenInputPanel;
    [SerializeField] private Button showTokenInputButton;
    [SerializeField] private Button hideTokenInputButton;

    private OctoAuthManager authManager;

    private void Start()
    {
        // Find or create auth manager
        authManager = OctoAuthManager.Instance;
        if (authManager == null)
        {
            GameObject authObject = new GameObject("OctoAuthManager");
            authManager = authObject.AddComponent<OctoAuthManager>();
        }

        // Setup UI event listeners
        loginButton.onClick.AddListener(OnLoginClicked);
        logoutButton.onClick.AddListener(OnLogoutClicked);
        submitTokenButton.onClick.AddListener(OnSubmitTokenClicked);
        showTokenInputButton.onClick.AddListener(() => tokenInputPanel.SetActive(true));
        hideTokenInputButton.onClick.AddListener(() => tokenInputPanel.SetActive(false));

        // Listen for auth state changes
        authManager.OnAuthStateChanged += UpdateUIState;

        // Initial UI state
        UpdateUIState(authManager.IsAuthenticated());
        tokenInputPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // Clean up event listeners
        if (authManager != null)
        {
            authManager.OnAuthStateChanged -= UpdateUIState;
        }
    }

    private void UpdateUIState(bool isAuthenticated)
    {
        loggedInPanel.SetActive(isAuthenticated);
        loggedOutPanel.SetActive(!isAuthenticated);

        if (isAuthenticated)
        {
            statusText.text = "Eingeloggt bei Octolearn";
            statusText.color = Color.green;
        }
        else
        {
            statusText.text = "Nicht eingeloggt";
            statusText.color = Color.red;
        }
    }

    private void OnLoginClicked()
    {
        authManager.StartAuthentication();
    }

    private void OnLogoutClicked()
    {
        authManager.ClearToken();
    }

    private void OnSubmitTokenClicked()
    {
        string token = tokenInputField.text.Trim();
        if (!string.IsNullOrEmpty(token))
        {
            authManager.SetManualToken(token);
            tokenInputField.text = "";
            tokenInputPanel.SetActive(false);
        }
    }
} 
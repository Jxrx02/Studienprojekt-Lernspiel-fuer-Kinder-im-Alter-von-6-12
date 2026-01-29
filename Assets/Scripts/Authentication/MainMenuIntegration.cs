using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Example class that shows how to integrate the Octolearn authentication system
/// with an existing main menu
/// </summary>
public class MainMenuIntegration : MonoBehaviour
{
    [Header("Octolearn Integration")]
    [SerializeField] private Button octoLoginButton;
    [SerializeField] private TextMeshProUGUI loginStatusText;
    [SerializeField] private GameObject authPanel;
    [SerializeField] private bool checkLoginOnStart = true;
    
    [Header("Additional Features")]
    [SerializeField] private Button getQuestionsButton;
    [SerializeField] private Button getUserInfoButton;
    [SerializeField] private Button logoutButton;
    
    private OctoAuthManager authManager;
    private OctoApiService apiService;
    
    private void Start()
    {
        // Initialize services if needed
        InitializeOctoServices();
        
        // Set up button listeners
        if (octoLoginButton != null)
        {
            octoLoginButton.onClick.AddListener(OnLoginButtonClicked);
        }
        
        if (getQuestionsButton != null)
        {
            getQuestionsButton.onClick.AddListener(OnGetQuestionsClicked);
            getQuestionsButton.gameObject.SetActive(false); // Hide until logged in
        }
        
        if (getUserInfoButton != null)
        {
            getUserInfoButton.onClick.AddListener(OnGetUserInfoClicked);
            getUserInfoButton.gameObject.SetActive(false); // Hide until logged in
        }
        
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutClicked);
            logoutButton.gameObject.SetActive(false); // Hide until logged in
        }
        
        // Subscribe to auth state changes
        if (authManager != null)
        {
            authManager.OnAuthStateChanged += UpdateLoginState;
            
            // Check login state on start
            if (checkLoginOnStart)
            {
                UpdateLoginState(authManager.IsAuthenticated());
            }
        }
    }
    
    private void OnDestroy()
    {
        // Clean up event subscription
        if (authManager != null)
        {
            authManager.OnAuthStateChanged -= UpdateLoginState;
        }
    }
    
    private void InitializeOctoServices()
    {
        // Try to find existing services
        authManager = OctoAuthManager.Instance;
        apiService = OctoApiService.Instance;
        
        // If services don't exist, create them
        if (authManager == null || apiService == null)
        {
            // Check if there's a system initializer in the scene
            OctoSystemInitializer initializer = UnityEngine.Object.FindAnyObjectByType<OctoSystemInitializer>();
            
            if (initializer == null)
            {
                // Create services manually
                if (authManager == null)
                {
                    GameObject authObject = new GameObject("OctoAuthManager");
                    authManager = authObject.AddComponent<OctoAuthManager>();
                    DontDestroyOnLoad(authObject);
                }
                
                if (apiService == null)
                {
                    GameObject apiObject = new GameObject("OctoApiService");
                    apiService = apiObject.AddComponent<OctoApiService>();
                    DontDestroyOnLoad(apiObject);
                }
            }
            else
            {
                // System initializer will create the services
                // Just get the references afterward
                authManager = OctoAuthManager.Instance;
                apiService = OctoApiService.Instance;
            }
        }
    }
    
    private void UpdateLoginState(bool isLoggedIn)
    {
        if (loginStatusText != null)
        {
            loginStatusText.text = isLoggedIn ? "Mit Octolearn verbunden" : "Nicht mit Octolearn verbunden";
            loginStatusText.color = isLoggedIn ? Color.green : Color.red;
        }
        
        if (octoLoginButton != null)
        {
            octoLoginButton.gameObject.SetActive(!isLoggedIn);
        }
        
        if (getQuestionsButton != null)
        {
            getQuestionsButton.gameObject.SetActive(isLoggedIn);
        }
        
        if (getUserInfoButton != null)
        {
            getUserInfoButton.gameObject.SetActive(isLoggedIn);
        }
        
        if (logoutButton != null)
        {
            logoutButton.gameObject.SetActive(isLoggedIn);
        }
    }
    
    private void OnLoginButtonClicked()
    {
        if (authPanel != null)
        {
            authPanel.SetActive(true);
        }
        else if (authManager != null)
        {
            authManager.StartAuthentication();
        }
    }
    
    private void OnLogoutClicked()
    {
        if (authManager != null)
        {
            authManager.ClearToken();
        }
    }
    
    private void OnGetQuestionsClicked()
    {
        if (apiService != null)
        {
            apiService.GetNextQuestions(
                3, // Get 3 questions
                (questions) => {
                    Debug.Log($"Received {questions.Count} questions from Octolearn");
                    if (questions.Count > 0)
                    {
                        Debug.Log($"First question: {questions[0].question}");
                    }
                },
                (error) => {
                    Debug.LogError($"Error getting questions: {error}");
                }
            );
        }
    }
    
    private void OnGetUserInfoClicked()
    {
        if (apiService != null)
        {
            apiService.GetUserInfo(
                (userInfo) => {
                    Debug.Log($"User: {userInfo.firstName} {userInfo.lastName} ({userInfo.email})");
                },
                (error) => {
                    Debug.LogError($"Error getting user info: {error}");
                }
            );
        }
    }
} 
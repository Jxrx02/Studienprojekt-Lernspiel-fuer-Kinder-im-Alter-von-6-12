using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Script to initialize and connect the Authentication question system with 
/// the ResearchManager, LevelUnlocker, and QuestionPuffer in the MainMenuScene
/// </summary>
public class MainMenuQuestionInitializer : MonoBehaviour
{
    [Header("GameObject References")]
    [SerializeField] private GameObject dontDestroyContainer;
    [SerializeField] private GameObject questionPrefab;
    [SerializeField] private Transform questionPrefabParent;

    [Header("UI Elements")]
    [SerializeField] private Button getQuestionsButton;
    [SerializeField] private Button showQuestionUIButton;
    [SerializeField] private Button checkStatusButton;

    [Header("Configuration")]
    [SerializeField] private bool createComponentsIfMissing = true;
    [SerializeField] private bool ensureAuthSystemExists = true;

    // References
    private QuestionIntegrationManager questionManager;
    private QuestionUIManager uiManager;
    private ResearchManagerWrapper researchWrapper;
    private OctoSystemInitializer systemInitializer;
    private OctoAuthManager authManager;
    private OctoApiService apiService;

    private void Awake()
    {
        // Create the container if it doesn't exist
        if (dontDestroyContainer == null)
        {
            dontDestroyContainer = new GameObject("DontDestroy_ResearchManager_LvlUnlocker_Questionpuffer");
        }

        // Make sure the authentication system is initialized first
        if (ensureAuthSystemExists)
        {
            EnsureAuthSystemInitialized();
        }

        // Initialize the ResearchManagerWrapper
        researchWrapper = ResearchManagerWrapper.Instance;

        StartCoroutine(InitializeWithDelay());
    }

    private void EnsureAuthSystemInitialized()
    {
        // Check if system initializer exists
        systemInitializer = FindAnyObjectByType<OctoSystemInitializer>();
        
        if (systemInitializer == null && createComponentsIfMissing)
        {
            Debug.Log("Creating OctoSystemInitializer");
            GameObject systemObj = new GameObject("OctoSystemInitializer");
            systemObj.transform.SetParent(dontDestroyContainer.transform);
            systemInitializer = systemObj.AddComponent<OctoSystemInitializer>();
            DontDestroyOnLoad(systemObj);
        }
    }

    private IEnumerator InitializeWithDelay()
    {
        // Wait a frame to allow OctoSystemInitializer to initialize
        yield return null;
        
        // Find auth services
        authManager = OctoAuthManager.Instance;
        apiService = OctoApiService.Instance;
        
        // Find the question manager with delay to ensure auth services are up
        yield return new WaitForSeconds(0.5f);
        
        // Find the question manager
        questionManager = FindAnyObjectByType<QuestionIntegrationManager>();

        // If not found, create it
        if (questionManager == null && createComponentsIfMissing)
        {
            Debug.Log("Creating QuestionIntegrationManager");
            questionManager = dontDestroyContainer.AddComponent<QuestionIntegrationManager>();
        }

        // Find or create the UI manager
        uiManager = FindAnyObjectByType<QuestionUIManager>();
        if (uiManager == null && createComponentsIfMissing && questionPrefab != null)
        {
            Debug.Log("Creating QuestionUIManager");
            GameObject uiObject = Instantiate(questionPrefab, questionPrefabParent);
            uiManager = uiObject.GetComponent<QuestionUIManager>();

            if (uiManager == null)
            {
                uiManager = uiObject.AddComponent<QuestionUIManager>();
            }
        }

        // Set up button listeners
        if (getQuestionsButton != null)
        {
            getQuestionsButton.onClick.AddListener(OnGetQuestionsClicked);
        }

        if (showQuestionUIButton != null)
        {
            showQuestionUIButton.onClick.AddListener(OnShowQuestionUIClicked);
        }
        
        if (checkStatusButton != null)
        {
            checkStatusButton.onClick.AddListener(CheckAuthenticationStatus);
        }

        // Log debug info
        Debug.Log("MainMenuQuestionInitializer: Initialization completed.");
        Debug.Log($"OctoSystemInitializer initialized: {systemInitializer != null}");
        Debug.Log($"ResearchManagerWrapper initialized: {researchWrapper != null}");
        Debug.Log($"QuestionIntegrationManager found: {questionManager != null}");
        Debug.Log($"QuestionUIManager found: {uiManager != null}");
    }
    
    private void OnGetQuestionsClicked()
    {
        if (questionManager != null)
        {
            Debug.Log("Manually requesting questions from Octolearn...");
            questionManager.LoadQuestionsFromAuthentication();
        }
        else
        {
            Debug.LogError("Question manager not initialized");
        }
    }

    private void OnShowQuestionUIClicked()
    {
        if (uiManager != null)
        {
            uiManager.ShowNextQuestion();
        }
        else
        {
            Debug.LogError("Question UI manager not initialized");
        }
    }
    
    // Debug method to check authentication status
    public void CheckAuthenticationStatus()
    {
        Debug.Log("=== Authentication Status Check ===");
        
        if (authManager == null)
        {
            authManager = OctoAuthManager.Instance;
            Debug.Log("AuthManager: Not initialized, attempted to find instance");
        }
        
        if (apiService == null)
        {
            apiService = OctoApiService.Instance;
            Debug.Log("ApiService: Not initialized, attempted to find instance");
        }
        
        if (authManager != null)
        {
            bool isLoggedIn = authManager.IsAuthenticated();
            Debug.Log($"AuthManager found: {authManager != null}, IsAuthenticated: {isLoggedIn}");
            
            if (isLoggedIn)
            {
                Debug.Log("User is logged in and authenticated");
                
                // Force reload questions
                if (questionManager != null)
                {
                    Debug.Log("Manually forcing question loading...");
                    questionManager.LoadQuestionsFromAuthentication();
                }
            }
            else
            {
                Debug.Log("User is NOT logged in");
            }
        }
        else
        {
            Debug.LogError("AuthManager not found!");
        }
        
        if (apiService != null)
        {
            Debug.Log("ApiService found");
        }
        else
        {
            Debug.LogError("ApiService not found!");
        }
        
        if (questionManager != null)
        {
            Debug.Log($"QuestionManager found, Buffer size: {questionManager.questionBuffer.Count}");
        }
        
        Debug.Log("=============================");
    }
}
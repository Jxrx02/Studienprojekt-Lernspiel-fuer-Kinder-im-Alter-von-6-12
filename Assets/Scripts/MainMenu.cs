using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Octolearn Authentication")]
    [SerializeField] private GameObject authPanel;
    [SerializeField] private Button authButton;

    private OctoAuthManager authManager;
    private OctoApiService apiService;

    void Start()
    {
        // Initialize auth manager and API service if not already existing
        InitializeAuthSystem();
        
        // Set up login button
        if (authButton != null)
        {
            authButton.onClick.AddListener(ShowAuthPanel);
        }
    }

    private void InitializeAuthSystem()
    {
        // Find or create the auth manager
        authManager = OctoAuthManager.Instance;
        if (authManager == null)
        {
            GameObject authObject = new GameObject("OctoAuthManager");
            authManager = authObject.AddComponent<OctoAuthManager>();
            DontDestroyOnLoad(authObject);
        }
        
        // Find or create the API service
        apiService = OctoApiService.Instance;
        if (apiService == null)
        {
            GameObject apiObject = new GameObject("OctoApiService");
            apiService = apiObject.AddComponent<OctoApiService>();
            DontDestroyOnLoad(apiObject);
        }
    }
    
    private void ShowAuthPanel()
    {
        if (authPanel != null)
        {
            authPanel.SetActive(true);
        }
    }
    
    // Example method to test getting questions
    public void TestGetQuestions()
    {
        if (apiService != null && authManager != null && authManager.IsAuthenticated())
        {
            apiService.GetNextQuestions(
                3, // Get 3 questions
                (questions) => Debug.Log($"Got {questions.Count} questions from Octolearn"),
                (error) => Debug.LogError($"Error getting questions: {error}")
            );
        }
        else
        {
            Debug.LogWarning("Not authenticated. Please log in first.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    



}

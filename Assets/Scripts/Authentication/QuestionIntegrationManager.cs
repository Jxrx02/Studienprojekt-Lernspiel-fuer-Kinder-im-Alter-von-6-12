using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class QuestionIntegrationManager : MonoBehaviour
{
    // GameObject reference for the list that will store our questions
    [SerializeField] private GameObject dontDestroyContainer;
    
    // Authentication components
    private OctoAuthManager authManager;
    private OctoApiService apiService;
    
    // Use the wrapper instead of direct reference
    private ResearchManagerWrapper researchManagerWrapper;
    
    // Questions buffer
    public List<FrontendContent> questionBuffer = new List<FrontendContent>();
    
    // Number of questions to request
    [SerializeField] private int numberOfQuestionsToLoad = 5;
    
    // Flag to check if questions are loaded on start
    [SerializeField] private bool loadQuestionsOnStart = true;
    
    // Points awarded per correct answer
    [SerializeField] private int researchPointsPerCorrectAnswer = 10;
    
    // Time to wait before loading questions after login
    private const float LOGIN_BUFFER_TIME = 2.0f;
    // Time to retry finding services
    private const float SERVICE_RETRY_TIME = 0.5f;

    private bool isInitialized = false;
    private bool isRetryingServiceInitialization = false;
    
    private void Awake()
    {
        // Make this object persistent across scenes
        if (dontDestroyContainer != null)
        {
            DontDestroyOnLoad(dontDestroyContainer);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
        
        // Try to initialize services
        TryInitializeServices();
    }
    
    private void TryInitializeServices()
    {
        // Get references to required services
        authManager = OctoAuthManager.Instance;
        apiService = OctoApiService.Instance;
        
        // Get the research manager wrapper
        researchManagerWrapper = ResearchManagerWrapper.Instance;
        
        if (authManager == null || apiService == null)
        {
            Debug.LogWarning("Authentication services not available yet. Will retry initialization.");
            if (!isRetryingServiceInitialization)
            {
                isRetryingServiceInitialization = true;
                StartCoroutine(RetryServiceInitialization());
            }
            return;
        }
        
        if (researchManagerWrapper == null)
        {
            Debug.LogWarning("ResearchManagerWrapper not found. Research points won't be awarded.");
        }
        
        // Subscribe to authentication state changes
        authManager.OnAuthStateChanged += OnAuthStateChanged;
        
        isInitialized = true;
        isRetryingServiceInitialization = false;
        
        Debug.Log("QuestionIntegrationManager successfully initialized");
        
        // Check if we need to load questions on start
        if (loadQuestionsOnStart && IsLoggedIn())
        {
            Debug.Log("Already logged in, loading questions after initialization...");
            LoadQuestionsAfterDelay(LOGIN_BUFFER_TIME);
        }
    }
    
    private IEnumerator RetryServiceInitialization()
    {
        int retryCount = 0;
        while (!isInitialized && retryCount < 10) // Limit retries to avoid infinite loop
        {
            yield return new WaitForSeconds(SERVICE_RETRY_TIME);
            Debug.Log($"Retrying service initialization (attempt {retryCount + 1})");
            TryInitializeServices();
            retryCount++;
        }
        
        if (!isInitialized)
        {
            Debug.LogError("Failed to initialize authentication services after multiple attempts.");
        }
    }
    
    private void Start()
    {
        if (!isInitialized) 
        {
            Debug.Log("Not initialized yet in Start, will attempt initialization");
            TryInitializeServices();
            return;
        }
        
        // Check if we're already logged in
        if (loadQuestionsOnStart && IsLoggedIn())
        {
            Debug.Log("Already logged in, loading questions on start...");
            LoadQuestionsAfterDelay(LOGIN_BUFFER_TIME); // Longer delay to ensure everything is initialized
        }
        else
        {
            Debug.Log("Not logged in yet. Waiting for login...");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (authManager != null)
        {
            authManager.OnAuthStateChanged -= OnAuthStateChanged;
        }
    }
    
    // Check if we're logged in
    private bool IsLoggedIn()
    {
        return authManager != null && authManager.IsAuthenticated();
    }
    
    private void OnAuthStateChanged(bool isLoggedIn)
    {
        Debug.Log($"Auth state changed: isLoggedIn={isLoggedIn}");
        
        if (isLoggedIn)
        {
            // User just logged in - load questions after a short delay
            Debug.Log("User logged in, loading questions after delay...");
            LoadQuestionsAfterDelay(LOGIN_BUFFER_TIME);
        }
        else
        {
            // User logged out - clear the buffer
            Debug.Log("User logged out, clearing question buffer");
            questionBuffer.Clear();
        }
    }
    
    // Load questions after a delay to ensure auth token is properly set
    private void LoadQuestionsAfterDelay(float delay)
    {
        StartCoroutine(DelayedQuestionLoading(delay));
    }
    
    private System.Collections.IEnumerator DelayedQuestionLoading(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Double-check initialization and authentication status
        if (!isInitialized)
        {
            Debug.LogWarning("Cannot load questions: not initialized yet");
            TryInitializeServices();
            yield break;
        }
        
        if (IsLoggedIn())
        {
            Debug.Log("Loading questions after delay...");
            LoadQuestionsFromAuthentication();
        }
        else
        {
            Debug.LogWarning("Still not logged in after delay, won't load questions");
        }
    }
    
    // Public method to manually trigger question loading
    public void LoadQuestionsFromAuthentication()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Cannot load questions: not initialized");
            TryInitializeServices();
            return;
        }
        
        if (!IsLoggedIn())
        {
            Debug.LogWarning("Cannot load questions: not authenticated");
            return;
        }
        
        if (apiService == null)
        {
            Debug.LogWarning("Cannot load questions: API service not available");
            return;
        }
        
        Debug.Log("Loading questions from Octolearn...");
        
        apiService.GetNextQuestions(
            numberOfQuestionsToLoad,
            OnQuestionsReceived,
            OnApiError
        );
    }
    
    private void OnQuestionsReceived(List<FrontendContent> questions)
    {
        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("No questions received from Octolearn");
            return;
        }
        
        // Store the questions in our buffer
        questionBuffer.Clear();
        questionBuffer.AddRange(questions);
        
        Debug.Log($"Received {questions.Count} questions from Octolearn");
    }
    
    private void OnApiError(string error)
    {
        Debug.LogError($"Error loading questions from Octolearn: {error}");
    }
    
    // Submit an answer to a question
    public void SubmitAnswer(FrontendContent question, string userAnswer)
    {
        if (!IsLoggedIn())
        {
            Debug.LogWarning("Cannot submit answer: not authenticated");
            return;
        }
        
        if (apiService == null || !questionBuffer.Contains(question))
        {
            Debug.LogWarning("Cannot submit answer: question not found in buffer or service not available");
            return;
        }
        
        apiService.SubmitAnswer(
            question,
            userAnswer,
            (response) => OnAnswerSubmitted(response, question),
            OnApiError
        );
    }
    
    private void OnAnswerSubmitted(AnswerResponse response, FrontendContent question)
    {
        // Remove the question from the buffer
        questionBuffer.Remove(question);
        
        // Award research points if answer is correct
        if (response.correct && researchManagerWrapper != null)
        {
            researchManagerWrapper.AddResearchPoints(researchPointsPerCorrectAnswer);
            Debug.Log($"Correct answer! Awarded {researchPointsPerCorrectAnswer} research points");
        }
        
        // If the buffer is getting low, load more questions
        if (questionBuffer.Count < 2 && IsLoggedIn())
        {
            LoadQuestionsFromAuthentication();
        }
    }
    
    // Get the next question from the buffer
    public FrontendContent GetNextQuestion()
    {
        if (questionBuffer.Count == 0)
        {
            // Only try to load more questions if logged in
            if (IsLoggedIn())
            {
                Debug.Log("Question buffer empty, loading more questions...");
                LoadQuestionsFromAuthentication();
            }
            else
            {
                Debug.LogWarning("Question buffer empty but not logged in. Cannot load more questions.");
            }
            return null;
        }
        
        return questionBuffer[0];
    }
    
    // Get all questions in the buffer
    public List<FrontendContent> GetAllQuestions()
    {
        return new List<FrontendContent>(questionBuffer);
    }
} 
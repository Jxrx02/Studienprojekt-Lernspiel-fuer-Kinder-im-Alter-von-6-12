using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class OctoApiService : MonoBehaviour
{
    private static OctoApiService _instance;
    public static OctoApiService Instance { get { return _instance; } }
    
    // Base URL for API calls
    private const string API_BASE_URL = "https://app.octolearn.de/api/third-party";
    
    // API credentials
    [Header("API Credentials")]
    [SerializeField] private string apiKey = "xxxx-aaaa-ccc-bbb-uuu";
    [SerializeField] private string apiSecret = "dddd-iii-qqqq-aaaa-xde";
    
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
    
    // Generic API request method with JWT authentication
    public IEnumerator SendRequest<T>(string endpoint, string method, Action<T> onSuccess, Action<string> onError, Dictionary<string, string> parameters = null, string bodyJson = null)
    {
        // Check if authenticated
        OctoAuthManager authManager = OctoAuthManager.Instance;
        if (authManager == null || !authManager.IsAuthenticated())
        {
            onError?.Invoke("Not authenticated. Please log in first.");
            yield break;
        }

        // Get the JWT token
        string token = authManager.GetToken();
        
        // Build the complete URL
        string url = API_BASE_URL + endpoint;
        
        // Add query parameters if any
        if (parameters != null && parameters.Count > 0)
        {
            url += "?";
            bool first = true;
            
            foreach (var param in parameters)
            {
                if (!first)
                {
                    url += "&";
                }
                
                url += $"{UnityWebRequest.EscapeURL(param.Key)}={UnityWebRequest.EscapeURL(param.Value)}";
                first = false;
            }
        }
        
        // Create the request
        UnityWebRequest request = new UnityWebRequest(url, method);
        
        // Add JWT token to header
        request.SetRequestHeader("Authorization", "Bearer " + token);
        
        // Add API credentials
        request.SetRequestHeader("apiKey", apiKey);
        request.SetRequestHeader("apiSecret", apiSecret);
        
        // Add JSON body if needed
        if (!string.IsNullOrEmpty(bodyJson))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(bodyJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.SetRequestHeader("Content-Type", "application/json");
        }
        
        // Add download handler to receive response
        request.downloadHandler = new DownloadHandlerBuffer();
        
        // Send the request
        yield return request.SendWebRequest();
        
        // Check for network error
        if (request.result == UnityWebRequest.Result.ConnectionError || 
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            onError?.Invoke($"Error: {request.error}, Response: {request.downloadHandler.text}");
            yield break;
        }
        
        // Parse the response JSON
        try
        {
            T responseData = JsonUtility.FromJson<T>(request.downloadHandler.text);
            onSuccess?.Invoke(responseData);
        }
        catch (Exception e)
        {
            onError?.Invoke($"Error parsing response: {e.Message}, Response: {request.downloadHandler.text}");
        }
    }
    
    // Get next questions from Octolearn
    public void GetNextQuestions(int numberOfQuestions, Action<List<FrontendContent>> onSuccess, Action<string> onError)
    {
        if (numberOfQuestions < 1 || numberOfQuestions > 5)
        {
            onError?.Invoke("Number of questions must be between 1 and 5");
            return;
        }
        
        // Check if authenticated
        OctoAuthManager authManager = OctoAuthManager.Instance;
        if (authManager == null || !authManager.IsAuthenticated())
        {
            Debug.LogError("Not authenticated. Please log in before requesting questions.");
            onError?.Invoke("Not authenticated. Please log in first.");
            return;
        }
        
        Debug.Log($"Getting {numberOfQuestions} questions from API. Authentication status: {authManager.IsAuthenticated()}");
        
        Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            { "numberOfNeededQuestions", numberOfQuestions.ToString() }
        };
        
        // Get the JWT token
        string token = authManager.GetToken();
        
        // Build the URL
        string url = API_BASE_URL + "/next-relevant-questions";
        
        // Add query parameters
        url += $"?numberOfNeededQuestions={numberOfQuestions}";
        
        // Create the request
        UnityWebRequest request = UnityWebRequest.Get(url);
        
        // Add JWT token to header
        request.SetRequestHeader("Authorization", "Bearer " + token);
        
        // Add API credentials
        request.SetRequestHeader("apiKey", apiKey);
        request.SetRequestHeader("apiSecret", apiSecret);
        
        // Start coroutine
        StartCoroutine(SendQuestionsRequest(request, onSuccess, onError));
    }
    
    // Special coroutine for handling the question response as array
    private IEnumerator SendQuestionsRequest(UnityWebRequest request, Action<List<FrontendContent>> onSuccess, Action<string> onError)
    {
        // Send request
        yield return request.SendWebRequest();
        
        // Check for errors
        if (request.result == UnityWebRequest.Result.ConnectionError || 
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Error getting questions: {request.error}");
            onError?.Invoke($"Network error: {request.error}");
            yield break;
        }
        
        // Get response text
        string responseText = request.downloadHandler.text;
        
        try
        {
            // Try parsing as array of FrontendContent
            List<FrontendContent> questions = new List<FrontendContent>();
            
            // Hacky fix: We know the API returns an array but JsonUtility doesn't support direct array deserialization
            // So wrap the array in an object structure
            string wrappedJson = "{\"contents\":" + responseText + "}";
            FrontendContentList contentList = JsonUtility.FromJson<FrontendContentList>(wrappedJson);
            
            if (contentList != null && contentList.contents != null && contentList.contents.Count > 0)
            {
                Debug.Log($"Successfully received {contentList.contents.Count} questions from API");
                onSuccess?.Invoke(contentList.contents);
            }
            else
            {
                Debug.LogWarning("Received empty question list from API");
                onSuccess?.Invoke(new List<FrontendContent>());
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing questions: {e.Message}");
            onError?.Invoke($"Error parsing response: {e.Message}, Response: {responseText}");
        }
    }
    
    // Submit an answer to a question
    public void SubmitAnswer(FrontendContent question, string userAnswer, Action<AnswerResponse> onSuccess, Action<string> onError)
    {
        FrontendContentAndAnswer answerData = new FrontendContentAndAnswer
        {
            frontendContent = question,
            answer = userAnswer
        };
        
        string bodyJson = JsonUtility.ToJson(answerData);
        
        StartCoroutine(SendRequest<AnswerResponse>("", "PUT", onSuccess, onError, null, bodyJson));
    }
    
    // Get user account information
    public void GetUserInfo(Action<UserAccount> onSuccess, Action<string> onError)
    {
        StartCoroutine(SendRequest<UserAccount>("/account", "GET", onSuccess, onError));
    }
    
    // Terminate the current browser session
    public void TerminateSession(string sessionId, Action<bool> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            onError?.Invoke("Session ID is required");
            return;
        }
        
        StartCoroutine(SendRequest<object>($"/terminate-session/{sessionId}", "GET", 
            (_) => onSuccess?.Invoke(true), 
            onError));
    }
    
    // Save a game highscore
    public void SaveGameHighscore(string record, string additionalInfo, Action<bool> onSuccess, Action<string> onError)
    {
        // Create the request body
        GameHighscoreRequest highscoreData = new GameHighscoreRequest
        {
            record = record,
            additionalInformation = additionalInfo
        };
        
        string bodyJson = JsonUtility.ToJson(highscoreData);
        
        StartCoroutine(SendRequest<object>("/record", "POST", 
            (_) => onSuccess?.Invoke(true), 
            onError, 
            null, 
            bodyJson));
    }
    
    // Get all game highscores
    public void GetGameHighscores(Action<List<GameHighscore>> onSuccess, Action<string> onError)
    {
        StartCoroutine(SendRequest<GameHighscoreList>("/record", "GET", 
            (response) => onSuccess?.Invoke(response.highscores), 
            onError));
    }
    
    // Create a learning content folder
    public void CreateLearningContentFolder(LearningContentFolder folder, Action<LearningContentFolder> onSuccess, Action<string> onError)
    {
        string bodyJson = JsonUtility.ToJson(folder);
        
        StartCoroutine(SendRequest<LearningContentFolder>("/learning-content-folders", "POST", 
            onSuccess, 
            onError, 
            null, 
            bodyJson));
    }
    
    // Create learning contents
    public void CreateLearningContents(List<LearningContent> contents, Action<bool> onSuccess, Action<string> onError)
    {
        string bodyJson = JsonUtility.ToJson(new LearningContentList { contents = contents });
        
        StartCoroutine(SendRequest<object>("/learning-contents", "POST", 
            (_) => onSuccess?.Invoke(true), 
            onError, 
            null, 
            bodyJson));
    }
}

// Data models for API requests and responses

[Serializable]
public class FrontendContentList
{
    public List<FrontendContent> contents;
}

[Serializable]
public class FrontendContent
{
    public long id;
    public string question;
    public bool examPreparationModeActive;
    public string learningMode;
    public string queryFormat;
    public int learningLevel;
    public string answer;
    public string[] wrongAnswers;
    public bool learningLevelShallBeChanged;
    public string learningContentFolderName;
    public bool isItVocabulary;
    public string sourceLanguage;
    public string targetLanguage;
}

[Serializable]
public class FrontendContentAndAnswer
{
    public FrontendContent frontendContent;
    public string answer;
}

[Serializable]
public class AnswerResponse
{
    public bool correct;
    public string feedback;
}

[Serializable]
public class UserAccount
{
    public string id;
    public string login;
    public string firstName;
    public string lastName;
    public string email;
    public string imageUrl;
    public bool activated;
    public string langKey;
    public string createdBy;
    public string createdDate;
    public string lastModifiedBy;
    public string lastModifiedDate;
    public string[] authorities;
}

[Serializable]
public class GameHighscoreRequest
{
    public string record;
    public string additionalInformation;
}

[Serializable]
public class GameHighscore
{
    public string id;
    public string entry;
    public string additionalInformation;
    public string date;
    public string game;
    public string userExtra;
}

[Serializable]
public class GameHighscoreList
{
    public List<GameHighscore> highscores;
}

[Serializable]
public class LanguageToVocabulary
{
    public string sourceLanguageKey;
    public string targetLanguageKey;
}

[Serializable]
public class LearningContentFolder
{
    public long? id;
    public string name;
    public bool activeForQuery;
    public bool examPreparationModeActive;
    public string learningMode;
    public bool isItVocabulary;
    public LanguageToVocabulary languageToVocabulary;
    public bool shared;
    public bool visible;
    public string chapter;
    public string grade;
    public string schoolBook;
    public string subject;
    public string topic;
}

[Serializable]
public class Dictation
{
    public string lernwort;
}

[Serializable]
public class Arithmetic
{
    public string operation;
    public int numbercountResult;
}

[Serializable]
public class QuestionAnswer
{
    public string question;
    public string answer;
}

[Serializable]
public class LearningContent
{
    public Dictation dictation;
    public Arithmetic arithmetic;
    public QuestionAnswer questionAnswer;
    public LearningContentFolder learningContentFolder;
}

[Serializable]
public class LearningContentList
{
    public List<LearningContent> contents;
} 
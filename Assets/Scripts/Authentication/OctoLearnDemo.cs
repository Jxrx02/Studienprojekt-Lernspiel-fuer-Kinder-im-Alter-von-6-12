using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OctoLearnDemo : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button getQuestionsButton;
    [SerializeField] private Button submitAnswerButton;
    [SerializeField] private Button getUserInfoButton;
    [SerializeField] private TMP_InputField answerInputField;
    [SerializeField] private TextMeshProUGUI questionTextDisplay;
    [SerializeField] private TextMeshProUGUI statusDisplay;
    [SerializeField] private GameObject optionsContainer;
    [SerializeField] private GameObject optionButtonPrefab;
    
    [Header("Options")]
    [SerializeField] private int numberOfQuestionsToGet = 3;
    
    private OctoAuthManager authManager;
    private OctoApiService apiService;
    private List<FrontendContent> currentQuestions = new List<FrontendContent>();
    private FrontendContent currentQuestion = null;
    private int currentQuestionIndex = 0;
    
    private void Start()
    {
        // Get references to required services
        authManager = OctoAuthManager.Instance;
        apiService = OctoApiService.Instance;
        
        if (authManager == null || apiService == null)
        {
            statusDisplay.text = "Error: Services not initialized";
            return;
        }
        
        // Set up button listeners
        getQuestionsButton.onClick.AddListener(OnGetQuestionsClicked);
        submitAnswerButton.onClick.AddListener(OnSubmitAnswerClicked);
        getUserInfoButton.onClick.AddListener(OnGetUserInfoClicked);
        
        // Initial setup
        submitAnswerButton.interactable = false;
        ClearQuestion();
    }
    
    private void OnGetQuestionsClicked()
    {
        if (!authManager.IsAuthenticated())
        {
            statusDisplay.text = "Please log in first";
            return;
        }
        
        statusDisplay.text = "Loading questions...";
        
        apiService.GetNextQuestions(
            numberOfQuestionsToGet, 
            OnQuestionsReceived,
            OnApiError
        );
    }
    
    private void OnQuestionsReceived(List<FrontendContent> questions)
    {
        if (questions == null || questions.Count == 0)
        {
            statusDisplay.text = "No questions available";
            return;
        }
        
        currentQuestions = questions;
        currentQuestionIndex = 0;
        DisplayCurrentQuestion();
        
        statusDisplay.text = $"Received {questions.Count} questions";
    }
    
    private void DisplayCurrentQuestion()
    {
        if (currentQuestions.Count == 0)
        {
            ClearQuestion();
            return;
        }
        
        currentQuestion = currentQuestions[currentQuestionIndex];
        questionTextDisplay.text = currentQuestion.question;
        answerInputField.text = "";
        submitAnswerButton.interactable = true;
        
        // Clear and rebuild option buttons if this is a multiple choice question
        ClearOptions();
        
        if (currentQuestion.queryFormat == "MultipleChoice" && currentQuestion.wrongAnswers != null)
        {
            // Create option buttons for the correct answer and wrong answers
            List<string> allOptions = new List<string>();
            allOptions.Add(currentQuestion.answer);
            allOptions.AddRange(currentQuestion.wrongAnswers);
            
            // Shuffle options
            for (int i = 0; i < allOptions.Count; i++)
            {
                string temp = allOptions[i];
                int randomIndex = Random.Range(i, allOptions.Count);
                allOptions[i] = allOptions[randomIndex];
                allOptions[randomIndex] = temp;
            }
            
            // Create buttons
            foreach (string option in allOptions)
            {
                GameObject buttonObj = Instantiate(optionButtonPrefab, optionsContainer.transform);
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                
                buttonText.text = option;
                button.onClick.AddListener(() => {
                    answerInputField.text = option;
                });
            }
            
            optionsContainer.SetActive(true);
        }
        else
        {
            optionsContainer.SetActive(false);
        }
    }
    
    private void ClearOptions()
    {
        // Remove all option buttons
        foreach (Transform child in optionsContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }
    
    private void OnSubmitAnswerClicked()
    {
        if (currentQuestion == null)
        {
            statusDisplay.text = "No question to answer";
            return;
        }
        
        string answer = answerInputField.text.Trim();
        if (string.IsNullOrEmpty(answer))
        {
            statusDisplay.text = "Please enter an answer";
            return;
        }
        
        statusDisplay.text = "Submitting answer...";
        
        apiService.SubmitAnswer(
            currentQuestion, 
            answer,
            OnAnswerSubmitted,
            OnApiError
        );
    }
    
    private void OnAnswerSubmitted(AnswerResponse response)
    {
        string result = response.correct ? "Correct!" : "Incorrect";
        if (!string.IsNullOrEmpty(response.feedback))
        {
            result += " - " + response.feedback;
        }
        
        statusDisplay.text = result;
        
        // Move to the next question if available
        currentQuestionIndex++;
        if (currentQuestionIndex < currentQuestions.Count)
        {
            DisplayCurrentQuestion();
        }
        else
        {
            statusDisplay.text += " - All questions completed";
            ClearQuestion();
        }
    }
    
    private void OnGetUserInfoClicked()
    {
        if (!authManager.IsAuthenticated())
        {
            statusDisplay.text = "Please log in first";
            return;
        }
        
        statusDisplay.text = "Loading user info...";
        
        apiService.GetUserInfo(
            OnUserInfoReceived,
            OnApiError
        );
    }
    
    private void OnUserInfoReceived(UserAccount userInfo)
    {
        statusDisplay.text = $"Welcome {userInfo.firstName} {userInfo.lastName} ({userInfo.email})";
    }
    
    private void OnApiError(string error)
    {
        statusDisplay.text = "Error: " + error;
    }
    
    private void ClearQuestion()
    {
        currentQuestion = null;
        questionTextDisplay.text = "No question loaded";
        answerInputField.text = "";
        submitAnswerButton.interactable = false;
        ClearOptions();
        optionsContainer.SetActive(false);
    }
} 
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class QuestionUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private QuestionIntegrationManager questionManager;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private GameObject optionsContainer;
    [SerializeField] private GameObject optionButtonPrefab;
    [SerializeField] private TextMeshProUGUI statusMessageText;
    
    [Header("Settings")]
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private float feedbackDisplayTime = 2f;
    [SerializeField] private float questionManagerSearchDelay = 1f;
    
    private FrontendContent currentQuestion;
    private bool isWaitingForNextQuestion = false;
    private bool isInitialized = false;
    
    private void Awake()
    {
        Debug.Log("QuestionUIManager: Awake");
        
        // Hide UI elements initially
        if (questionPanel != null)
        {
            questionPanel.SetActive(false);
        }
        
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }
        
        if (statusMessageText != null)
        {
            statusMessageText.gameObject.SetActive(false);
        }
    }
    
    private void Start()
    {
        Debug.Log("QuestionUIManager: Start - Finding QuestionIntegrationManager");
        StartCoroutine(Initialize());
    }
    
    private IEnumerator Initialize()
    {
        yield return new WaitForSeconds(questionManagerSearchDelay);
        
        // Find the question manager if not set
        if (questionManager == null)
        {
            // Try to find by using FindAnyObjectByType
            Debug.Log("QuestionUIManager: Attempting to find QuestionIntegrationManager");
            questionManager = FindAnyObjectByType<QuestionIntegrationManager>();
            
            if (questionManager == null)
            {
                Debug.LogError("QuestionIntegrationManager not found in the scene. Please add it to the DontDestroy object.");
                yield break;
            }
            else
            {
                Debug.Log("QuestionUIManager: Successfully found QuestionIntegrationManager");
            }
        }
        
        // Set up UI button listeners
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitAnswer);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseQuestionPanel);
        }
        
        isInitialized = true;
        Debug.Log("QuestionUIManager: Initialization complete");
    }
    
    // Public method to show the question panel with the next question
    public void ShowNextQuestion()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("QuestionUIManager not initialized yet");
            StartCoroutine(ShowNextQuestionWhenInitialized());
            return;
        }
        
        if (questionManager == null)
        {
            Debug.LogError("QuestionIntegrationManager reference not set");
            ShowStatusMessage("Question service not available");
            return;
        }
        
        // Get the next question
        currentQuestion = questionManager.GetNextQuestion();
        
        if (currentQuestion == null)
        {
            // No questions available
            Debug.Log("No questions available currently. Try again later.");
            ShowStatusMessage("No questions available. Please try again later.");
            return;
        }
        
        // Display the question
        DisplayQuestion(currentQuestion);
        
        // Show the panel
        if (questionPanel != null)
        {
            questionPanel.SetActive(true);
        }
        
        // Hide any status message
        if (statusMessageText != null)
        {
            statusMessageText.gameObject.SetActive(false);
        }
    }
    
    private IEnumerator ShowNextQuestionWhenInitialized()
    {
        // Show loading message
        ShowStatusMessage("Initializing question system...");
        
        float timeout = 5f; // Timeout after 5 seconds
        float elapsed = 0f;
        
        while (!isInitialized && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }
        
        if (!isInitialized)
        {
            ShowStatusMessage("Could not initialize question system.");
            yield break;
        }
        
        ShowNextQuestion();
    }
    
    private void ShowStatusMessage(string message)
    {
        if (statusMessageText != null)
        {
            statusMessageText.text = message;
            statusMessageText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"Status message: {message}");
        }
    }
    
    private void DisplayQuestion(FrontendContent question)
    {
        // Set the question text
        if (questionText != null)
        {
            questionText.text = question.question;
        }
        
        // Clear the answer input
        if (answerInput != null)
        {
            answerInput.text = "";
        }
        
        // Clear previous feedback
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }
        
        // Enable submit button
        if (submitButton != null)
        {
            submitButton.interactable = true;
        }
        
        // Handle multiple choice questions
        if (optionsContainer != null)
        {
            // Clear existing options
            foreach (Transform child in optionsContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Check if this is a multiple choice question
            if (question.queryFormat == "MultipleChoice" && question.wrongAnswers != null && question.wrongAnswers.Length > 0)
            {
                // Create a list of all options
                List<string> allOptions = new List<string>();
                allOptions.Add(question.answer);
                allOptions.AddRange(question.wrongAnswers);
                
                // Shuffle the options
                for (int i = 0; i < allOptions.Count; i++)
                {
                    string temp = allOptions[i];
                    int randomIndex = Random.Range(i, allOptions.Count);
                    allOptions[i] = allOptions[randomIndex];
                    allOptions[randomIndex] = temp;
                }
                
                // Create option buttons
                foreach (string option in allOptions)
                {
                    GameObject buttonObj = Instantiate(optionButtonPrefab, optionsContainer.transform);
                    Button button = buttonObj.GetComponent<Button>();
                    TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                    
                    if (buttonText != null)
                    {
                        buttonText.text = option;
                    }
                    
                    if (button != null)
                    {
                        button.onClick.AddListener(() => {
                            answerInput.text = option;
                        });
                    }
                }
                
                optionsContainer.SetActive(true);
                answerInput.gameObject.SetActive(false);
            }
            else
            {
                // Not a multiple choice question
                optionsContainer.SetActive(false);
                answerInput.gameObject.SetActive(true);
            }
        }
    }
    
    private void OnSubmitAnswer()
    {
        if (questionManager == null || currentQuestion == null || answerInput == null)
        {
            return;
        }
        
        string userAnswer = answerInput.text.Trim();
        if (string.IsNullOrEmpty(userAnswer))
        {
            // Cannot submit empty answer
            return;
        }
        
        // Disable the submit button to prevent multiple submissions
        if (submitButton != null)
        {
            submitButton.interactable = false;
        }
        
        // Submit the answer
        SubmitAnswer(userAnswer);
    }
    
    private void SubmitAnswer(string answer)
    {
        // Store the question locally since it will be removed from the buffer after submission
        FrontendContent questionToAnswer = currentQuestion;
        
        // Use our question manager to submit the answer
        questionManager.SubmitAnswer(questionToAnswer, answer);
        
        // For now we'll display immediate feedback
        bool isCorrect = answer.Trim().ToLower() == questionToAnswer.answer.Trim().ToLower();
        
        // Display feedback
        DisplayFeedback(isCorrect);
        
        // After a delay, either show the next question or close the panel
        isWaitingForNextQuestion = true;
        Invoke(nameof(AfterAnswerSubmitted), feedbackDisplayTime);
    }
    
    private void DisplayFeedback(bool isCorrect)
    {
        if (feedbackText != null)
        {
            feedbackText.text = isCorrect ? "Correct!" : "Incorrect";
            feedbackText.color = isCorrect ? correctColor : incorrectColor;
            feedbackText.gameObject.SetActive(true);
        }
    }
    
    private void AfterAnswerSubmitted()
    {
        isWaitingForNextQuestion = false;
        
        // Get the next question if available
        currentQuestion = questionManager.GetNextQuestion();
        
        if (currentQuestion != null)
        {
            // Show the next question
            DisplayQuestion(currentQuestion);
        }
        else
        {
            // No more questions available
            CloseQuestionPanel();
        }
    }
    
    private void CloseQuestionPanel()
    {
        if (questionPanel != null)
        {
            questionPanel.SetActive(false);
        }
    }
} 
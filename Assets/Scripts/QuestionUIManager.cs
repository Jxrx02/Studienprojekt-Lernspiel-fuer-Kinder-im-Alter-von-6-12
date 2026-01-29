using System;
using TowerDefense.Research;

namespace TowerDefense
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    
public class QuestionUIManager : MonoBehaviour
{
    public List<FrontendContent> questionBuffer = new List<FrontendContent>();
    public Text questionText;
    public Transform answerPanel; // Parent-Objekt mit 4 Buttons als Kinder

    private string correctAnswer;

    void Start()
    {
        String[] s = new String[] {"11", "12", "9"};

        FrontendContent ct = new FrontendContent();
        ct.question = "Was ist die Quersumme von 1234?";
        ct.answer = "10";
        ct.wrongAnswers = s;
        questionBuffer.Add(ct);
        ShowQuestion();
    }

    public void ShowQuestion()
    {
        questionBuffer = ResearchManager.Instance.GetComponent<QuestionIntegrationManager>().questionBuffer;

        if (questionBuffer.Count == 0)
        {
            Debug.LogWarning("Keine Fragen im Buffer!");
            return;
        }

        // Erste Frage holen
        FrontendContent currentQuestion = questionBuffer[0];
        questionText.text = currentQuestion.question.ToString();
        correctAnswer = currentQuestion.answer;

        // Antworten mischen
        List<string> allAnswers = new List<string>(currentQuestion.wrongAnswers);
        allAnswers.Add(currentQuestion.answer);
        Shuffle(allAnswers);

        // Buttons aktualisieren
        for (int i = 0; i < answerPanel.childCount && i < allAnswers.Count; i++)
        {
            Transform buttonTransform = answerPanel.GetChild(i);
            Button button = buttonTransform.GetComponent<Button>();
            Text buttonText = buttonTransform.GetComponentInChildren<Text>();
            string answer = allAnswers[i];
            buttonText.text = answer;

            // Listener zurücksetzen, dann neue hinzufügen
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnAnswerClicked(answer));
        }
    }

    void OnAnswerClicked(string selectedAnswer)
    {
        if (selectedAnswer == correctAnswer)
        {
            Debug.Log("Richtig!");
            LevelManager.instance.DoRevive();

        }
        else
        {
            Debug.Log("Falsch!"); 
            LevelManager.instance.ReviveFailed();

            
        }
        questionBuffer.RemoveAt(0);
         
    }

    // Hilfsfunktion zum Mischen der Liste
    void Shuffle(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            string temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }
}
}
using UnityEngine;
using System;

/// <summary>
/// Simple wrapper to allow access to the ResearchManager through reflection.
/// </summary>
public class ResearchManagerWrapper : MonoBehaviour
{
    private static ResearchManagerWrapper _instance;
    public static ResearchManagerWrapper Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject wrapperObj = new GameObject("ResearchManagerWrapper");
                _instance = wrapperObj.AddComponent<ResearchManagerWrapper>();
                DontDestroyOnLoad(wrapperObj);
            }
            return _instance;
        }
    }
    
    // Cache the ResearchManager reference
    private MonoBehaviour researchManager;
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            FindResearchManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void FindResearchManager()
    {
        // Find all MonoBehaviours in the scene using the newer, non-deprecated method
        MonoBehaviour[] allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        
        // Loop through them and look for one named "ResearchManager"
        foreach (MonoBehaviour mb in allMonoBehaviours)
        {
            if (mb.GetType().Name == "ResearchManager")
            {
                researchManager = mb;
                Debug.Log("ResearchManagerWrapper: Found ResearchManager");
                return;
            }
        }
        
        Debug.LogWarning("ResearchManagerWrapper: Could not find ResearchManager");
    }
    
    /// <summary>
    /// Add research points to the player's total.
    /// </summary>
    public void AddResearchPoints(int amount)
    {
        try
        {
            if (researchManager == null)
            {
                // Try to find it again
                FindResearchManager();
                
                if (researchManager == null)
                {
                    Debug.LogWarning("Could not add research points - ResearchManager not found");
                    return;
                }
            }
            
            // Call the method using reflection
            researchManager.SendMessage("AddResearchPoints", amount, SendMessageOptions.DontRequireReceiver);
            Debug.Log($"Added {amount} research points");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error adding research points: {ex.Message}");
        }
    }
} 
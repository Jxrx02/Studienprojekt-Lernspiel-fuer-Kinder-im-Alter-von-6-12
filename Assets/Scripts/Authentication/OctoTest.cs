using UnityEngine;

namespace OctoLearn.Test
{
    public class OctoTest : MonoBehaviour
    {
        public void Start()
        {
            Debug.Log("OctoTest initialized successfully!");
            
            // Try to access other classes to verify they compile correctly
            var authManager = OctoAuthManager.Instance;
            if (authManager == null)
            {
                Debug.Log("No OctoAuthManager instance found - this is expected if none exists yet");
            }
            else
            {
                Debug.Log("Found existing OctoAuthManager instance");
            }
        }
    }
} 
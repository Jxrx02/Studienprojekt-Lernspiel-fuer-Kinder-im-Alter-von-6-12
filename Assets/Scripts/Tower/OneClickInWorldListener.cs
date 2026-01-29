using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OneClickInWorldListener : MonoBehaviour
{
    private static OneClickInWorldListener instance;

    // Statt nur einer Funktion → Liste
    private List<Action<Vector3>> clickCallbacks = new List<Action<Vector3>>();
    private bool isWaitingForClick;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (!isWaitingForClick) return;
        if (EventSystem.current.IsPointerOverGameObject()) return; 
        
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldClick = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldClick.z = 0;

            // Alle registrierten Callbacks aufrufen
            foreach (var callback in clickCallbacks)
            {
                callback?.Invoke(worldClick);
            }

            // aufräumen
            clickCallbacks.Clear();
            isWaitingForClick = false;
        }
    }

    public static void ListenOnce(Action<Vector3> callback)
    {
        if (instance == null)
        {
            Debug.LogError("No OneClickInWorldListener in the scene!");
            return;
        }

        instance.isWaitingForClick = true;

        // Neue Callback zur Liste hinzufügen
        instance.clickCallbacks.Add(callback);
    }
}
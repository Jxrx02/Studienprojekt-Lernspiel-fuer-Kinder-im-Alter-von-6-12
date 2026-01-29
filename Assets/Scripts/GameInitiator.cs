using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameInitiator : MonoBehaviour
{
    [SerializeField] private AudioListener audioSystem;
    [SerializeField] private SceneFadeIn fadeCanvas;
    
    
    private IEnumerator Start()
    {
        BindObjects();
        
        StartCoroutine(InitializeObjects());
        yield return StartCoroutine(CreateObjects());

        fadeCanvas.FadeToScene(1);

    }

    private IEnumerator InitializeObjects()
    {
        //DontDestroyOnLoad(audioSystem);
        yield return new WaitForSeconds(3);
    }

    private IEnumerator CreateObjects()
    {
        yield return new WaitForSeconds(3);
    }


    private void BindObjects()
    {
        //if(audioSystem== null)
        //    audioSystem = Instantiate(audioSystem);


        
    }
}


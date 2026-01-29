using System;
using System.Collections;
using System.Collections.Generic;
using LoadingScreen;
using UnityEngine;
using UnityEngine.UI;

public class SceneFadeIn : MonoBehaviour
{
    
    public float fadeDuration = 0.5f;
    public Image fadeOverlay;

    public IEnumerator Fade(bool fadein)
    {
        fadeOverlay.gameObject.SetActive(true);
        fadeOverlay.enabled = true;

        if (fadein)
        {
            fadeOverlay.CrossFadeAlpha(1, 0, true);
            fadeOverlay.CrossFadeAlpha(0, fadeDuration, true);
            yield return new WaitForSeconds(fadeDuration);
            fadeOverlay.gameObject.SetActive(false);
            fadeOverlay.enabled = false;
        }
        else     
        {
            fadeOverlay.CrossFadeAlpha(0, 0, true);
            fadeOverlay.CrossFadeAlpha(1, fadeDuration, true);
        }
    }

    /*
     * Call this method to switch scenes
     */
    public void FadeToScene(int scene)
    {
        StartCoroutine(fadeToScene(scene));
    }



    private IEnumerator fadeToScene(int scene)
    {
        fadeOverlay.gameObject.SetActive(true);
        fadeOverlay.enabled = true;
        
        fadeOverlay.CrossFadeAlpha(0, 0, true);
        fadeOverlay.CrossFadeAlpha(1, fadeDuration, true);
        yield return new WaitForSeconds(fadeDuration);
        LoaderInfo.LoadScene(scene);

        
    }
    

}

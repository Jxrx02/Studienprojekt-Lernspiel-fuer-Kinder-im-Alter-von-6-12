using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DialogCanvas : MonoBehaviour
{
    private Text text;
    private Animator anim;

    public Vector2 offset; 
    
    public static DialogCanvas instance;

    private void Awake()
    {
        gameObject.SetActive(false);

        anim = GetComponent<Animator>();
        text = GetComponentInChildren <Text>();
        
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    
    public void TriggerDialog(String text, Vector3 pos)
    {
        gameObject.SetActive(true);

        transform.position = pos + new Vector3(offset.x, offset.y, 0);
        this.text.text = text;
        anim.SetTrigger("DialogAppear");
    }
    
    public void OnDialogEnd()
    {
        gameObject.SetActive(false);

    }

}

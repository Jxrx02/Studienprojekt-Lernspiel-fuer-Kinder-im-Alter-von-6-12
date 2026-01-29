using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(SpriteRenderer))]
public class ShadowEffect : MonoBehaviour
{

    public Vector3 offset = new Vector3(-.1f, -.1f);
    public Vector3 scale = new Vector3(1f, 1f);
    public Material material;
    GameObject shadow;

    // Start is called before the first frame update
    void Start()
    {
        shadow = new GameObject("Shadow");
        shadow.transform.parent = transform;

        shadow.transform.localPosition = offset;
        shadow.transform.localRotation = Quaternion.identity;
        shadow.transform.localScale = scale;


        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        SpriteRenderer sr = shadow.AddComponent<SpriteRenderer>();
        sr.sprite = renderer.sprite;
        sr.material = material;

        //render shadow in front 
        sr.sortingLayerName = renderer.sortingLayerName;
        sr.sortingOrder = renderer.sortingOrder - 1;


    }

    // Update is called once per frame
    void Update()
    {
        shadow.transform.localPosition = offset;
    }
}

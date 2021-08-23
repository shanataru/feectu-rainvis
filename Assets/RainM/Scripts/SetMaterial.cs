using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SetMaterial : MonoBehaviour
{
    public Material mat;
    // Start is called before the first frame update
    void Start()
    {
        foreach (Transform child in transform) {
            child.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

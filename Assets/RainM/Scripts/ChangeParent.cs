using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ChangeParent : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //foreach (Transform child in transform) {
        //    foreach (Transform grandchild in child) {
        //        grandchild.parent = transform;
        //    }
        //}

        for (int i = 0; i < transform.childCount; i++) {
            transform.GetChild(i).name = "Road (" + i + ")";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

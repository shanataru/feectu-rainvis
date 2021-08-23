using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyObjects : MonoBehaviour
{
    GameObject RM;
    void Start()
    {
        RM = GameObject.Find("RainManager");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (RM.GetComponent<RainManager>().octaneRenderer)
        {
            other.GetComponent<DetectCar>().Clear();
        }
        
        //other.gameObject.SetActive(false);
        Destroy(other.gameObject);
    }
}

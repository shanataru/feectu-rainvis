using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowGO : MonoBehaviour
{
    public GameObject go;
    private Vector3 offset;            //Private variable to store the offset distance between the player and camera
    // Start is called before the first frame update
    void Start()
    {
        offset = transform.position - go.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = go.transform.position + offset;
    }
}

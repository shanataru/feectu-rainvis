using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogControl : MonoBehaviour
{
    public GameObject RM;
    public float m_cameraOffset = 10.0f;
    bool octaneRenderer = false;

    Camera cam;
    Quaternion camRot;
    Vector3 camPos;



    // Start is called before the first frame update
    void Start()
    {
        if (RM.GetComponent<RainManager>().octaneRenderer) {
            octaneRenderer = true;
        }

        //position rainfall areas to camera
        cam = RM.GetComponent<RainManager>().mainCamera;
        camPos = cam.transform.position;
    }

    void UpdateFogArea() {
        camRot = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
        Vector3 camPos = cam.transform.position + transform.localScale.x / 2.0f * (cam.transform.forward);
        transform.position = camPos + cam.transform.forward * m_cameraOffset + Vector3.up * transform.localScale.y/3.0f;
        transform.rotation = camRot;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateFogArea();
    }
}

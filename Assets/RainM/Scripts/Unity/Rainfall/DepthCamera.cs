using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthCamera : MonoBehaviour
{
    Camera ortographicCam;
    RenderTexture rtColor;
    RenderTexture rtDepth;
    int width = 512;
    int height = 512;

    void Start()
    {
        ortographicCam = transform.GetComponent<Camera>();
        //rtColor = new RenderTexture(width, height, 0, RenderTextureFormat.RGB111110Float);
        //rtDepth = new RenderTexture(width, height, 24, RenderTextureFormat.Depth);
        //ortographicCam.SetTargetBuffers(rtColor.colorBuffer, rtDepth.depthBuffer);
        ortographicCam.depthTextureMode = DepthTextureMode.Depth; //write into the depth buffer
    }

    void Update()
    {
        
    }
}

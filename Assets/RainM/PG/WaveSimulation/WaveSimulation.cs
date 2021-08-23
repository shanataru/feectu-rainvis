/**
 * <summary>
 * 
 * Script for simulating interactions of a car wheel with shallow water surface used for making puddles on the road. 
 * 
 * Sets update zones to create ripples in custom render texture. One custom render texture (1024x1024) covers a few blocks of road.
 * Sends parameters which affect the kind of splashes the wheel of a car will emit (if splashes are enabled).
 * 
 * 
 * This script is for custom render texture - WaterSurfaceSimulation.shader 
 * Main source: http://tips.hecomi.com/entry/2017/05/17/020037, accessed 20 May, 2020
 * 
 * </summary>
 *	
 */


using System;
using System.Collections.Generic;
using UnityEngine;

public class WaveSimulation : MonoBehaviour
{
    public CustomRenderTexture m_TextureWaterPlane;
    //private Material m_MaterialWaterPlane = null;

    void Start()
    {
       //m_MaterialWaterPlane = GetComponent<Renderer>().material;
       //m_MaterialWaterPlane.SetTexture("_WaterDispTex", m_TextureWaterPlane);
    }

    void Update()
    {
        //m_TextureWaterPlane.ClearUpdateZones();
        //SimulateWaterSurfaceInteraction();
        //m_TextureWaterPlane.Update();
    }

    void SimulateWaterSurfaceInteraction()
    {
        //MOUSE DEBUG
        bool leftClick = Input.GetMouseButton(0);
        bool rightClick = Input.GetMouseButton(1);
        if (!leftClick && !rightClick) return;

        RaycastHit hitMouse;
        var rayMouse = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(rayMouse, out hitMouse))
        {
            var defaultZone = new CustomRenderTextureUpdateZone();
            defaultZone.needSwap = true;
            defaultZone.passIndex = 0;
            defaultZone.rotation = 0f;
            defaultZone.updateZoneCenter = new Vector2(0.5f, 0.5f);
            defaultZone.updateZoneSize = new Vector2(1f, 1f);

            var clickZone = new CustomRenderTextureUpdateZone();
            clickZone.needSwap = true;
            clickZone.passIndex = 1;
            clickZone.rotation = 0f;
            clickZone.updateZoneCenter = new Vector2(hitMouse.textureCoord.x, 1f - hitMouse.textureCoord.y);
            clickZone.updateZoneSize = new Vector2(0.01f, 0.01f);
            m_TextureWaterPlane.SetUpdateZones(new CustomRenderTextureUpdateZone[] { defaultZone, clickZone });
        }
    }
}

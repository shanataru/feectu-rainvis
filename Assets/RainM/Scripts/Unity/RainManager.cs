/**
 * <summary>
 * 
 * Rain manager controls the intensity and amount of "rain" in the scene
 * Switches between road object with a material defined with Custom/WetSurface shader (for Unity) or PBROverride shader for Octane rendering
 * 
 * Stores main bool "octaneRenderer" variable
 * 
 * 
 * </summary>
 */

using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RainManager : MonoBehaviour
{
    public enum RainIntensity {
        None = 0,
        Light = 1,
        Moderate = 2,
        Heavy = 3
    };

    [Header("Rainfall")]
    public RainIntensity m_RainIntensity = RainIntensity.None;

    [Header("Surface")]
    [Range(0, 1)]
    public float m_Wetness = 0.0f;
    [Range(0, 1)]
    public float m_WaterLevel = 0.0f;

    [HideInInspector]
    public bool m_dry;

    [Header("Puddles (Unity)")]
    public bool m_WaterSurfaceSimulation = true;
    [Range(0.1f, 1.5f)]
    public float m_ReflectionIntensity = 0.5f;
    [Range(0.01f, 1.0f)]
    public float m_WaveDisplacement = 0.07f;
    [Range(0.02f, 0.5f)]
    public float m_WaveScale = 0.063f;
    public Vector4 m_WaveSpeed = new Vector4(4.0f, 5.0f, -8.0f, -1.0f);
    public CustomRenderTexture m_CRTWaterSurface;

    [Header("Fog (Octane)")]
    public bool m_Fog = false;

    [Header("Road Materials")]
    public Material unityRoadMaterial;
    public Material unityPavementMaterial;
    public Material octaneRoadMaterial;
    public Material octanePavementMaterial;

    [Header("Gameobjects")]
    public GameObject road;
    public GameObject pavement;

    [Header("Camera settings")]
    public Camera mainCamera;

    [Header("Octane Renderer")]
    public bool octaneRenderer = false;

    [HideInInspector]
    public int m_CRTWaterSurfaceScale = 4;

    protected GameObject reflectionPlane;
    protected GameObject octaneDummy;
    protected GameObject rainfall;
    protected GameObject fogCube;


    int FloatToRGBColor(Color col) {
        int r = (int)(255 * (col.r < 0 ? 0 : col.r));
        int g = (int)(255 * (col.g < 0 ? 0 : col.g));
        int b = (int)(255 * (col.b < 0 ? 0 : col.b));
        return (r << 16) | (g << 8) | b;
    } 

    /// <summary>
    /// Set materials for Unity or Octane.
    /// </summary>
    void UpdateMaterials() {
        if (octaneRenderer) {
            foreach (Transform c in road.transform) {
                c.GetComponent<MeshRenderer>().material = octaneRoadMaterial;
            }

            foreach (Transform c in pavement.transform) {
                c.GetComponent<MeshRenderer>().material = octanePavementMaterial;
            }
        }
        else {
            foreach (Transform c in road.transform) {
                c.GetComponent<MeshRenderer>().material = unityRoadMaterial;
            }

            foreach (Transform c in pavement.transform) {
                c.GetComponent<MeshRenderer>().material = unityPavementMaterial;
            }
        }
    }

    void UpdateRMOctane() {
        var RM_octaneDummy = octaneDummy.GetComponent<OctaneUnity.PBRInstanceProperties>();

        //encode wetness and waterLevel to Color property of PBRInstanceProperties
        Color a = new Color(m_Wetness, m_WaterLevel, 0.0f);
        try {
            RM_octaneDummy.Color = FloatToRGBColor(a);
        }
        catch {
            Debug.Log("Could not access OctaneUnity.PBRInstanceProperties of " + octaneDummy.name + ". Load Octane!");
        }
        
        //TODO: encode rain intensity
    }

    public void MakeRain() {
        //fetch all child GOs
        reflectionPlane = transform.Find("ReflectionPlane").gameObject;
        octaneDummy = transform.Find("RM_OctaneDummy").gameObject;
        rainfall = transform.Find("Rainfall").gameObject;
        fogCube = transform.Find("Fog").gameObject;

        var reflectionComponent = reflectionPlane.GetComponent<Reflection>();

        //update materials accordingly
        UpdateMaterials();

        if (octaneRenderer){
            UpdateRMOctane();
            reflectionComponent.enabled = false;
            reflectionPlane.SetActive(false);
        }
        else {
            m_Fog = false;
            reflectionPlane.SetActive(true);
            reflectionComponent.enabled = true;
        }
        fogCube.SetActive(m_Fog);

        mainCamera = Camera.main;
    }

    private void Start()
    {
        //set water surface texture mapping scale (set only ONCE)
        Shader.SetGlobalFloat("_WaterCRTScale", m_CRTWaterSurfaceScale);
        Shader.SetGlobalFloat("_WaterCRTSize", m_CRTWaterSurface.width); //must be set globally because the water surface simulation parts are toggled on/off
        m_dry = false;
    }
    private void Update()
    {
        //flag determining whether its dry or wet
        if (m_WaterLevel <= 0.0f && m_Wetness <= 0.0f)
            m_dry = true;
        else
            m_dry = false;

        //edit ambient waves according to the rain intensity
        switch (m_RainIntensity) {
            case RainIntensity.Light:
                m_WaveDisplacement = 0.05f;
                m_WaveScale = 0.1f;
                break;
            case RainIntensity.Moderate:
                m_WaveDisplacement = 0.08f;
                m_WaveScale = 0.3f;
                break;
            case RainIntensity.Heavy:
                m_WaveDisplacement = 0.15f;
                m_WaveScale = 0.5f;
                break;
            default:
                m_WaveDisplacement = 0.02f;
                m_WaveScale = 0.05f;
                break;
        }

        //set rain parameters for Unity custom shader (globally)
        if (!octaneRenderer) {
            Shader.SetGlobalFloat("_Wetness", m_Wetness);
            Shader.SetGlobalFloat("_WaterLevel", m_WaterLevel);
            Shader.SetGlobalFloat("_ReflIntensity", m_ReflectionIntensity);
            Shader.SetGlobalFloat("_ReflDistort", m_WaveDisplacement);
            Shader.SetGlobalFloat("_ReflDistort", m_WaveDisplacement);

            // Make water bump texture scroll
            Vector4 waveScale4 = new Vector4(m_WaveScale, m_WaveScale, m_WaveScale * 0.4f, m_WaveScale * 0.45f);
            // Time since level load, and do intermediate calculations with doubles
            double t = Time.timeSinceLevelLoad / 20.0f;
            Vector4 offsetClamped = new Vector4(
                (float)Math.IEEERemainder(m_WaveSpeed.x * waveScale4.x * t, 1.0f),
                (float)Math.IEEERemainder(m_WaveSpeed.y * waveScale4.y * t, 1.0f),
                (float)Math.IEEERemainder(m_WaveSpeed.z * waveScale4.z * t, 1.0f),
                (float)Math.IEEERemainder(m_WaveSpeed.w * waveScale4.w * t, 1.0f));

            Shader.SetGlobalVector("_WaveOffset", offsetClamped);
            Shader.SetGlobalVector("_WaveScale4", waveScale4);
        }
    }
}

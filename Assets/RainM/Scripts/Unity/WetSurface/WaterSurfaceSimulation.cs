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

public class WaterSurfaceSimulation : MonoBehaviour
{
    protected static float HITPOINT_SHIFT = 0.05f;
    /// <summary>
    /// A class saving wheel hit point (correctly modified by rigidbody velocity and delta time)
    /// </summary>
    public class CWheelHit
    {
        public Vector3 point;
        public Vector3 normal;

        public CWheelHit(Vector3 p, Vector3 n)
        {
            //randomize the placing of the contact point so the repeated pattern is not visible
            //TODO: faster random generator
            point = p + new Vector3(UnityEngine.Random.Range(-HITPOINT_SHIFT, HITPOINT_SHIFT), 0, UnityEngine.Random.Range(-HITPOINT_SHIFT, HITPOINT_SHIFT));

            point = p;
            normal = n;
        }
    }

    public Queue<Tuple<CWheelHit, GameObject>> wheelContacts; //queue of contact points with a wheel and a splash GO reference

    //texture generating wavy ripples for puddles when wheel drives in
    private CustomRenderTexture m_InstancedCRTWaterSurface = null;
    private Material m_MaterialWaterPlane = null;

    //shader variables
    //Note: these values are taken manually for Octane Renderer - OctaneRoadMaterial.cs
    int m_PuddleMapScale;
    int m_HeightContrast;

    //shader textures
    //Note: these textures are set manually for Octane Renderer (cannot be retrieved from the PBR override material) - OctaneRoadMaterial.cs
    //[HideInInspector]
    Texture2D puddleMap; 
    Texture2D heightMap;

    //Get the main rain manager
    GameObject RM;
    float waterLevel; //for enabling water splash simulation
    bool octaneRenderer = false;

    //DEBUG
    int WaterSurfaceScale;

    void Start()
    {
        //DEBUG: for debug reasons only, do not edit
        RM = GameObject.Find("RainManager");
        //simulation = RM.GetComponent<RainManager>().m_WaterSurfaceSimulation;
        WaterSurfaceScale = RM.GetComponent<RainManager>().m_CRTWaterSurfaceScale;

        //list of wheel contacts
        wheelContacts = new Queue<Tuple<CWheelHit, GameObject>>();

        //Acquire textures and set CRT
        octaneRenderer = RM.GetComponent<RainManager>().octaneRenderer;
        if (octaneRenderer)
        {
            //Fetch from OctaneRoadMaterial.cs, TODO
            m_PuddleMapScale = transform.parent.GetComponent<OctaneRoadMaterial>().m_PuddleMapScale;
            m_HeightContrast = transform.parent.GetComponent<OctaneRoadMaterial>().m_HeightContrast;
            puddleMap = transform.parent.GetComponent<OctaneRoadMaterial>().puddleMap;
            heightMap = transform.parent.GetComponent<OctaneRoadMaterial>().heightMap;
        }
        else
        {   //instantiate water surface simulation CRT
            //m_TextureWaterPlane = Instantiate(m_TextureWaterRipple); //Doesnt work?

            CustomRenderTexture CRTWaterSurface = RM.GetComponent<RainManager>().m_CRTWaterSurface; //from which the water plane will instantiate
            m_InstancedCRTWaterSurface = new CustomRenderTexture(CRTWaterSurface.width, CRTWaterSurface.height, CRTWaterSurface.format);
            m_InstancedCRTWaterSurface.wrapMode = CRTWaterSurface.wrapMode;
            m_InstancedCRTWaterSurface.filterMode = CRTWaterSurface.filterMode;
            m_InstancedCRTWaterSurface.anisoLevel = CRTWaterSurface.anisoLevel;
            m_InstancedCRTWaterSurface.material = CRTWaterSurface.material;
            m_InstancedCRTWaterSurface.shaderPass = CRTWaterSurface.shaderPass;
            m_InstancedCRTWaterSurface.initializationMode = CRTWaterSurface.initializationMode;
            m_InstancedCRTWaterSurface.updateMode = CRTWaterSurface.updateMode;
            m_InstancedCRTWaterSurface.doubleBuffered = CRTWaterSurface.doubleBuffered;
            m_InstancedCRTWaterSurface.updateZoneSpace = CRTWaterSurface.updateZoneSpace;

            //Assign custom render texture as a height texture for the road material
            m_MaterialWaterPlane = GetComponent<Renderer>().material;
            m_MaterialWaterPlane.SetTexture("_WaterCRT", m_InstancedCRTWaterSurface);

            //Warning: this should not be changed in play time
            m_PuddleMapScale = (int)m_MaterialWaterPlane.GetFloat("_PuddleMapScale");
            m_HeightContrast = (int)m_MaterialWaterPlane.GetFloat("_HeightContrast");

            //get from custom shader (material)
            puddleMap = (Texture2D)m_MaterialWaterPlane.GetTexture("_PuddleMap");
            heightMap = (Texture2D)m_MaterialWaterPlane.GetTexture("_HeightMap");
        }
    }

    void Update()
    {
        //Enable ripples and splashes according to wetness, dynamic
        waterLevel = RM.GetComponent<RainManager>().m_WaterLevel;
        bool simulation = RM.GetComponent<RainManager>().m_WaterSurfaceSimulation;

        //not enough water to simulate shallow water or simulation is disabled
        if (waterLevel <= 0.0f) {
            return;
        }
        else {
            if (octaneRenderer)
            {
                SimulateWaterSurfaceInteractionOctane();
            }
            else {
                if (simulation) {
                    m_InstancedCRTWaterSurface.ClearUpdateZones();
                    SimulateWaterSurfaceInteraction();
                    m_InstancedCRTWaterSurface.Update();
                }
            }
        }
    }

    //Non-negative modulo
    float mod(float x, int m)
    {
        return (x % m + m) % m;
    }

    //Screen blending of two values
    float Screen(float a, float b)
    {
        float r = 1.0f - (1.0f - a) * (1.0f - b);
        return r;
    }

    /// <summary>
    /// Computes the UV coordinate which the ray hits and read the color value (puddle or not)
    /// </summary>
    /// <param name="hit"> Contact point of tire and road</param>
    /// <param name="splash"> Reference to the game object Splash (belongs to the wheel)</param>

    void UpdateSplashParameters(RaycastHit hit, GameObject splash)
    {
        //Reading from 2 textures
        Vector2 uvPuddleMap = new Vector2(mod(hit.point.x, m_PuddleMapScale) / m_PuddleMapScale, mod(hit.point.z, m_PuddleMapScale) / m_PuddleMapScale);
        Color puddleMapColor = puddleMap.GetPixelBilinear(uvPuddleMap.x, uvPuddleMap.y); //no need to edit y coordinate

        Vector2 uvHeightMap = new Vector2(hit.textureCoord.x, hit.textureCoord.y);
        Color heightMapColor = heightMap.GetPixelBilinear(uvHeightMap.x, 1.0f - uvHeightMap.y); //here yes, the orientation of the map is different

        float puddle = Screen(puddleMapColor.r, Mathf.Lerp(0.5f, heightMapColor.r, m_HeightContrast)); //puddle according to height information of the road
        float puddleIntensity = Math.Max(0.0f, waterLevel - puddle);

        //Assign the calculated water film depth
        splash.GetComponent<WheelSplashControl>().WFD = puddleIntensity * 10.0f; //in mm
    }

    /// <summary>
    /// Detect puddles and simulate puddle interaction:
    /// Takes the contact point queue and assign new CRT update zones
    /// </summary>
    void SimulateWaterSurfaceInteraction()
    {
        if (wheelContacts.Count > 0)
        {
            List<CustomRenderTextureUpdateZone> zones = new List<CustomRenderTextureUpdateZone>();

            var defaultZone = new CustomRenderTextureUpdateZone();
            defaultZone.needSwap = true;
            defaultZone.passIndex = 0;
            defaultZone.rotation = 0f;
            defaultZone.updateZoneCenter = new Vector2(0.5f, 0.5f);
            defaultZone.updateZoneSize = new Vector2(1f, 1f);
            zones.Add(defaultZone);
            

            while (wheelContacts.Count > 0)
            {
                Tuple<CWheelHit, GameObject> cp = wheelContacts.Dequeue();

                //more randomness?
                //if (UnityEngine.Random.Range(0.0f, 1.0f) >= 0.8)
                //    continue;

                CWheelHit wh = cp.Item1;
                GameObject splash = cp.Item2; //get reference to the splash connected to this wheel

                RaycastHit hit;
                float rayLength = 0.25f;
                Ray raywh = new Ray(wh.point + new Vector3(0, 0.01f, 0), -wh.normal);

                //Debug.DrawRay(wh.point, -wh.normal * rayLength, Color.green, 5.0f);

                //the road was hit by the wheel
                if (transform.GetComponent<Collider>().Raycast(raywh, out hit, rayLength)) //collider of road
                {
                    Vector2 uvHit = new Vector2((float)Math.Round(mod(hit.point.x, WaterSurfaceScale) / WaterSurfaceScale, 3), 
                        1.0f - (float)Math.Round(mod(hit.point.z, WaterSurfaceScale) / WaterSurfaceScale, 3));

                    var CPZone = new CustomRenderTextureUpdateZone();
                    CPZone.needSwap = true;
                    CPZone.passIndex = 1;
                    CPZone.rotation = 0f;

                    ////find where the ray hit the UV main
                    //Vector2 uv = new Vector2(hit.textureCoord.x, 1.0f - hit.textureCoord.y); //how to find texture 
                    //CPZone.updateZoneCenter = new Vector2(uv.x, uv.y);

                    //find where the ray hit the UV at world coordinates
                    CPZone.updateZoneCenter = new Vector2(uvHit.x, uvHit.y);

                    CPZone.updateZoneSize = new Vector2(0.02f, 0.02f);
                    zones.Add(CPZone);


                    //Compute the water film thickness for the splash
                    if (splash != null){
                        UpdateSplashParameters(hit, splash);
                    }
                }
            }
            m_InstancedCRTWaterSurface.SetUpdateZones(zones.ToArray());
            zones.Clear();

        }
    }

    /// <summary>
    /// Detecting water puddles in the octane PBR material
    /// </summary>
    void SimulateWaterSurfaceInteractionOctane() {
        while (wheelContacts.Count > 0)
        {
            Tuple<CWheelHit, GameObject> cp = wheelContacts.Dequeue();

            CWheelHit wh = cp.Item1;
            GameObject splash = cp.Item2; //get reference to the splash connected to this wheel

            RaycastHit hit;
            float rayLength = 0.25f;
            Ray raywh = new Ray(wh.point + new Vector3(0, 0.001f, 0), -wh.normal);

            //the road was hit by the wheel
            if (transform.GetComponent<Collider>().Raycast(raywh, out hit, rayLength)) //collider of road
            {
                Vector2 uvHit = new Vector2((float)Math.Round(mod(hit.point.x, WaterSurfaceScale) / WaterSurfaceScale, 3), 
                    1.0f - (float)Math.Round(mod(hit.point.z, WaterSurfaceScale) / WaterSurfaceScale, 3));

                //Compute the water film thickness for splash
                if (splash != null){
                    UpdateSplashParameters(hit, splash);
                }
            }
        }
    }

    public void ClearOutCRT()
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = m_InstancedCRTWaterSurface;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = rt;
    }
}

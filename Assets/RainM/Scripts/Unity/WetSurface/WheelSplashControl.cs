/**
 * <summary>
 * 
 * The script edits each particle system to create visually credible splashing of a car wheel. 
 * Particles are being baked every frame for Octane renderer.
 * 
 * 
 * Source: https://trl.co.uk/sites/default/files/PPR602.pdf
 * 
 * 
 * 
 * The particles depend on 
 *      WFD - water film depth (0-10mm): determines whether splash will occur or not
 *      MRW - the maximum amount of water available for splash and spray (based on WFD)
 *          source: https://saferroadsconference.com/wp-content/uploads/2016/05/Tuesday-am-SandC-6-Alan-Dunford-Predicting-splash-and-spray-and-its-impact-on-drivers.pdf
 *          
 *          
 * 
 * 
 * </summary>
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelSplash {
    protected ParticleSystem TreadPickup;
    //protected ParticleSystem Tread;
    protected ParticleSystem SideWave;
    protected ParticleSystem Mist;
    protected ParticleSystem Foam;

    protected List<BakedParticleGO> bakedParticles;
    protected List<GameObject> bakedInstances;

    protected static float TREADPU_EM_MAX = 200.0f;
    protected static float SMALL_DROP_EM_MAX = 20.0f;
    protected static float FRONT_SIDE_EM_MAX = 1000.0f;
    protected static float BACK_SIDE_EM_MAX = 1000.0f;
    protected static float WHEEL_SPEED_MAX = 130.0f;
    protected static float WHEEL_SPEED_HI = 90.0f;
    protected static float WHEEL_SPEED_MID = 48.0f;
    protected static float WHEEL_SPEED_MID_MIST = 60.0f;
    protected static float SIDE_VOL_Z_MAX = 5.0f;
    protected static float SIDE_VOL_Z_MIN = 1.0f;
    protected static float SIDE_VOL_Y_MAX = 4.0f;
    protected static float MIN_WFD = 2.0f;
    protected static float MIN_FOAM_WFD = 0.1f;
    protected static float FOAM_EM_MAX = 50.0f;

    protected static float EPS = 0.0001f;

    public WheelSplash() {}
    public virtual void CalculateParticles(float wetness, float speed, float wfd, float mrw, bool reverse) {
        //TPU - amount
        var em = TreadPickup.emission;
        em.rateOverDistance = Mathf.Clamp(speed*wetness * (TREADPU_EM_MAX / WHEEL_SPEED_HI), 0.0f, TREADPU_EM_MAX);

        //calculate small drops amount
        em = TreadPickup.transform.GetChild(0).GetComponent<ParticleSystem>().emission;
        em.rateOverDistance = Mathf.Clamp(speed * (SMALL_DROP_EM_MAX / WHEEL_SPEED_HI), 5.0f, SMALL_DROP_EM_MAX);

        //SW - orientation (reversing - change orientation of emission)
        var vol = SideWave.velocityOverLifetime;
        if (reverse) 
        {
            vol.z = new ParticleSystem.MinMaxCurve(SIDE_VOL_Z_MAX, SIDE_VOL_Z_MIN);
            vol.orbitalX = SIDE_VOL_Z_MIN;
        }
        else
        {
            vol.z = new ParticleSystem.MinMaxCurve(-SIDE_VOL_Z_MAX, -SIDE_VOL_Z_MIN);
            vol.orbitalX = -SIDE_VOL_Z_MIN;
        }

        //SW - height of splash
        float y = Mathf.Clamp(speed * (SIDE_VOL_Y_MAX / WHEEL_SPEED_MAX), 0.0f, SIDE_VOL_Y_MAX);
        vol.y = new ParticleSystem.MinMaxCurve(y, 0.0f);

        //Foam amount
        em = Foam.emission;
        em.rateOverDistance = Mathf.Clamp(wfd * (speed / WHEEL_SPEED_MID), wfd*2.0f, FOAM_EM_MAX);

    }

    public virtual void PlayParticles(float roadWetness, float speed, float wfd) {

        if (wfd > (MIN_WFD - EPS)) //puddle is deep enough to create a splash
        {
            if (!TreadPickup.isPlaying)
                TreadPickup.Play();


            if (!SideWave.isPlaying)
                SideWave.Play(true);
        }
        else
        {
            if (roadWetness > 0.4f) //road is wet to make tread pick ups but there are no puddles
            {
                if (!TreadPickup.isPlaying)
                    TreadPickup.Play();
            }
            else
                TreadPickup.Stop(); //road is dry

            SideWave.Stop();
        }

        //some puddle to make foam
        if (wfd > MIN_FOAM_WFD)
        {
            if (!Foam.isPlaying)
                Foam.Play();
        }
        else
        {
            Foam.Stop();
        }

    }

    public virtual void CreateBakedParticlesGO(GameObject localSpaceSplash, GameObject worldSpaceSplash, Material[] PBRMaterials) {
        bakedParticles = new List<BakedParticleGO>(); //creates GO 
        bakedInstances = new List<GameObject>(); //for instantiated GOs

        BakedParticleGO bakedTreadPickup = new BakedParticleGO(new GameObject(), TreadPickup, "(BM)TreadPickup", new Mesh(), PBRMaterials[0], worldSpaceSplash.transform);
        BakedParticleGO bakedSideWave = new BakedParticleGO(new GameObject(), SideWave, "(BM)SideWave", new Mesh(), PBRMaterials[1], localSpaceSplash.transform, true);
        BakedParticleGO bakedBigDrops = new BakedParticleGO(new GameObject(), SideWave.transform.GetChild(0).GetComponent<ParticleSystem>(), "(BM)BigDrops", new Mesh(), PBRMaterials[2], worldSpaceSplash.transform);
        BakedParticleGO bakedFoam = new BakedParticleGO(new GameObject(), Foam, "(BM)Foam", new Mesh(), PBRMaterials[4], worldSpaceSplash.transform);

        bakedParticles.Add(bakedTreadPickup); // + bow wave
        bakedParticles.Add(bakedSideWave);
        bakedParticles.Add(bakedBigDrops);
        bakedParticles.Add(bakedFoam);

        //small drops (capillary adhesion) are not baked, too small
        //BakedParticleGO bakedCapillary = new BakedParticleGO(new GameObject(), TreadPickup.transform.GetChild(0).GetComponent<ParticleSystem>(), "(BM)Capillary", new Mesh(), PBRMaterials[2], worldSpaceSplash.transform);
        //bakedParticles.Add(bakedCapillary);

        //deal with animated particles - disabled
        //BakedParticleGO bakedTread = new BakedParticleGO(new GameObject(), Tread, "(BM)Tread", new Mesh(), localSpaceSplash.transform, true);
        //BakedParticleGO bakedTrail = new BakedParticleGO(new GameObject(), Tread.transform.GetChild(0).GetComponent<ParticleSystem>(), "(BM)Trail", new Mesh(), worldSpaceSplash.transform);
        //bakedParticles.Add(bakedTread);
        //bakedParticles.Add(bakedTrail);

        //disables renderer on particle systems
        TreadPickup.GetComponent<Renderer>().enabled = false;
        TreadPickup.transform.GetChild(0).GetComponent<Renderer>().enabled = false;

        SideWave.GetComponent<Renderer>().enabled = false;
        SideWave.transform.GetChild(0).GetComponent<Renderer>().enabled = false;

        Foam.GetComponent<Renderer>().enabled = false;
    }

    public void MakeSplash(float wetness, float speed, float wfd, float mrw, bool reverse) {
        CalculateParticles(wetness, speed, wfd, mrw, reverse);
        PlayParticles(wetness, speed, wfd);
    }

    public void ClearBakedParticles() {
        //destroy previous instantiated GO
        foreach (GameObject i in bakedInstances)
            Object.Destroy(i);
        bakedInstances.Clear(); //remove all items in the list
    }
    public void BakeParticles() {
        ClearBakedParticles();

        //instantiate new baked particles
        for (int i = 0; i < bakedParticles.Count; ++i){
            BakedParticleGO p = bakedParticles[i]; //copy
            bakedInstances.Add(p.Bake());
        }
    }
    public virtual void Enable (bool state){
        TreadPickup.gameObject.SetActive(state);
        SideWave.gameObject.SetActive(state);
    }
}

//------------------------------------------------------------------------------------------------------------------------------------------------

public class FrontSplash : WheelSplash
{
    public FrontSplash(ParticleSystem tpu, ParticleSystem sw, ParticleSystem m, ParticleSystem f)
    {
        TreadPickup = tpu;
        SideWave = sw;
        Mist = m;
        Mist.gameObject.SetActive(false);
        Foam = f;

        //ParticleSystem trail = Tread.transform.GetChild(0).GetComponent<ParticleSystem>();
        //var mainModule = trail.main;
        //mainModule.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        //mainModule.startSizeX = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        //mainModule.startSizeY = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);

        //set numbers for front splash
        var mainModule = SideWave.main;
        mainModule.startSizeX = new ParticleSystem.MinMaxCurve(0.01f, 0.5f);
        mainModule.startSizeY = new ParticleSystem.MinMaxCurve(0.01f, 0.5f);
    }

    public override void CalculateParticles(float wetness, float speed, float wfd, float mrw, bool reverse) {
        base.CalculateParticles(wetness, speed, wfd, mrw, reverse);

        //SW - amount
        var em = SideWave.emission;
        em.rateOverDistance = Mathf.Clamp(mrw*2, 0.0f, FRONT_SIDE_EM_MAX);
    }
}

//------------------------------------------------------------------------------------------------------------------------------------------------

public class BackSplash : WheelSplash
{
    public BackSplash(ParticleSystem tpu, ParticleSystem sw, ParticleSystem m, ParticleSystem f) {
        TreadPickup = tpu;
        SideWave = sw;
        Mist = m;
        Foam = f;
    }
    public override void CalculateParticles(float wetness, float speed, float wfd, float mrw, bool reverse) {
        base.CalculateParticles(wetness, speed, wfd, mrw, reverse);

        //SW - amount 
        var em = SideWave.emission;
        //em.rateOverDistance = Mathf.Clamp(mrw * 3.5f, 0.0f, BACK_SIDE_EM_MAX);
        em.rateOverDistance = Mathf.Clamp(mrw * 2.0f, 0.0f, BACK_SIDE_EM_MAX);
    }

    public override void PlayParticles(float roadWetness, float speed, float wfd)
    {
        base.PlayParticles(roadWetness, speed, wfd);

        //control Mist according to road wetness
        if (roadWetness > 0.7f && speed > WHEEL_SPEED_MID_MIST)
        {
            if (!Mist.isPlaying)
                Mist.Play();
        }
        else
        {
            Mist.Stop();
        }
    }
    public override void Enable(bool state)
    {
        base.Enable(state);
        Mist.gameObject.SetActive(state);
    }
    public override void CreateBakedParticlesGO(GameObject localSpaceSplash, GameObject worldSpaceSplash, Material[] PBRMaterials)
    {
        base.CreateBakedParticlesGO(localSpaceSplash, worldSpaceSplash, PBRMaterials);

        //handle Mist particle in the rear wheel
        //disable texture sheet animation
        var tsa = Mist.textureSheetAnimation;
        tsa.enabled = false;

        BakedParticleGO bakedMist = new BakedParticleGO(new GameObject(), Mist, "(BM)Mist", new Mesh(), PBRMaterials[3], worldSpaceSplash.transform);
        bakedParticles.Add(bakedMist);

        Mist.GetComponent<Renderer>().enabled = false;
    }

}
public class WheelSplashControl : MonoBehaviour
{
    public bool frontWheel = false;

    [Header("Splash Particles")]
    public ParticleSystem TreadPickup;
    //public ParticleSystem Tread;
    public ParticleSystem SideWave;
    public ParticleSystem Mist;
    public ParticleSystem Foam;

    [Header("PBR Override Materials")]
    public Material PBRTreadPickUpMaterial;
    public Material PBRSideWaveMaterial;
    public Material PBRWaterDropMaterial;
    public Material PBRMistMaterial;
    public Material PBRFoamMaterial;

    bool bakeParticles;

    GameObject RM;
    GameObject wheel;
    float tireWidth = 0f;

    [HideInInspector] public float vehicleVelocity = 0; //speed of car in km/h (DetectRoadWheelCollision)
    [HideInInspector] public bool vehicleReversed = false; 
    [HideInInspector] public float WFD = 0; //water film thickness, in mm (WaterSurfaceSimulation)
    [HideInInspector] public bool enableSplash = true;

    WheelSplash cp;
    GameObject localSpaceBakedSplashGO;
    GameObject worldSpaceBakedSplashGO;

    //Computes MRw = speed(m/s) * tireWidth(m) * WFD(m) * waterDensity(kg/m^3) 
    float ComputeMaxAmountOfWater(float speed, float thickness) {
        float v = speed * 3.6f;
        return (v * tireWidth * thickness); //thickness is in mm and water density is cca 1000
    }

    void Start()
    {
        RM = GameObject.Find("RainManager");
        if (RM.GetComponent<RainManager>().octaneRenderer) {
            bakeParticles = true;
        }

        wheel = transform.parent.gameObject;
        tireWidth = wheel.GetComponent<WheelCollider>().radius;

        //List of PBROverride materials for octane renderer
        Material[] PBRMaterials = { PBRTreadPickUpMaterial, PBRSideWaveMaterial, PBRWaterDropMaterial, PBRMistMaterial, PBRFoamMaterial };

        if (frontWheel)
        {
            cp = new FrontSplash(TreadPickup, SideWave, Mist, Foam);
        }
        else {
            cp = new BackSplash(TreadPickup, SideWave, Mist, Foam);
        }

        //Bake world space particles if needed
        if (bakeParticles)
        {
            localSpaceBakedSplashGO = gameObject;
            worldSpaceBakedSplashGO = new GameObject();
            worldSpaceBakedSplashGO.name = "(BM)Splash_WorldSpace";
            cp.CreateBakedParticlesGO(localSpaceBakedSplashGO, worldSpaceBakedSplashGO, PBRMaterials);
        }
    }

    void Update()
    {
        if (RM.GetComponent<RainManager>().m_dry) {
            return;
        }

        //check if wheel is going reverse
        //cp.Reverse(wheel);

        //calculate splash amount and other
        cp.MakeSplash(RM.GetComponent<RainManager>().m_Wetness, vehicleVelocity, WFD, ComputeMaxAmountOfWater(vehicleVelocity, WFD), vehicleReversed);
        //cp.MakeSplash(RM.GetComponent<RainManager>().m_Wetness, 80, 10, ComputeMaxAmountOfWater(80, 10)); //DEBUG

        if (bakeParticles) //according to "Octane Renderer" bool in RM
        {
            cp.BakeParticles();
        }
    }

    public void Clear() {
        cp.ClearBakedParticles();
    }
}

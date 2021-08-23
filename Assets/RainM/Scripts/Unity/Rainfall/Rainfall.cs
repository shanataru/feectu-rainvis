using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


/// <summary>
/// A script controlling the rainfall: raindrops are generated within the container according to the rain intensity, together with splashes and ripple effects in puddles.
/// </summary>
public class Rainfall : MonoBehaviour {
    public GameObject RM;
    public CustomRenderTexture m_textureRainPlane;

    public int m_raindropRate = 0;
    public int m_rainRippleRate = 0;
    public int m_perUnitRaindropCount;
    public int m_perUnitRainRippleCount;
    public float m_splashProbability;

    int m_baseRippleRate = 10;
    int m_baseRaindropRate = 50;

    static float m_skyHeight = 15.0f;

    int m_rainIntensity = 0;
    float m_splashProbabilityBase = 1.0f;

    //float m_diagBase = 3200.0f;
    float m_diagBase = 1000.0f;

    Vector3 m_raindropCotainer;
    Vector3 m_rainSplashCotainer;
    bool octaneRenderer = false;
    bool staticCamera = false;

    Camera cam;
    Quaternion camRot;
    GameObject camFollowGO;
    DropSizeProbability[] dropSizeProbabilities;
    RandomGaussian randomValuesX;
    RandomGaussian randomValuesZ;
    Vector3 camPos;

    [Header("Rainfall Particles")]
    public ParticleSystem m_raindropParticles;
    public ParticleSystem m_rainSplashParticles;
    public ParticleSystem m_rainSheetParticles;

    [Header("PBR Override Materials")]
    public Material PBRRaindropMaterial;
    public Material PBRRainSplashMaterial;
    public Material PBRRainSheetMaterial;

    bool bakeParticles;

    protected GameObject worldSpaceBakedParticlesGO;
    protected List<BakedParticleGO> bakedParticles;
    protected List<GameObject> bakedInstances;

    int count = 0;

    //3D start size of a raindrop
    float START_SIZE_MIN_X = 0.05f;
    float START_SIZE_MAX_X = 0.1f;
    float START_SIZE_MIN_Y = 0.5f;
    float START_SIZE_MAX_Y = 1.0f;
    float OCTANE_START_SIZE_MIN_X = 0.05f;
    float OCTANE_START_SIZE_MAX_X = 0.07f;
    float OCTANE_START_SIZE_MIN_Y = 0.2f;
    float OCTANE_START_SIZE_MAX_Y = 0.6f;

    /// <summary>
    /// Struct storing raindrop size probabilities for a specific rain intensity. Returns a random drop size according to the probabilities.
    /// </summary>
    struct DropSizeProbability {
        public float[] m_dropSizeProbabilities;
        private int m_size;
        public DropSizeProbability(float[] probabilities) {
            //categories are defined implicitly as the indces
            m_dropSizeProbabilities = probabilities;
            m_size = m_dropSizeProbabilities.Length;
        }

        /// <summary>
        /// Returns a category of a raindrop size according to the intensity of rain.
        /// </summary>
        /// <returns>Integer 0-5 describing a drop size category</returns>
        public int GetDropSize() {
            float r = Random.value; //random value between 0.0f - 1.0f
            float currentProbability = 0;
            for (int i = 0; i < m_size; ++i) {
                currentProbability += m_dropSizeProbabilities[i];
                if (r <= currentProbability) //falls into this drop size category
                    return i; //returns 0-5 according to the probability of the size
            }
            return -1; //in case the probabilities do not sum up to 1
        }
    }

    /// <summary>
    /// https://answers.unity.com/questions/421968/normal-distribution-random.html
    /// </summary>
    struct RandomGaussian {
        public float m_minValue;
        public float m_maxValue;

        public RandomGaussian(float min, float max) {
            m_minValue = min;
            m_maxValue = max;
        }
        public float NextRandomNumber() {
            float u, v, S;

            do {
                u = 2.0f * Random.value - 1.0f;
                v = 2.0f * Random.value - 1.0f;
                S = u * u + v * v;
            }
            while (S >= 1.0f);

            // Standard Normal Distribution
            float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

            // Normal Distribution centered between the min and max value
            // and clamped following the "three-sigma rule"
            float mean = (m_minValue + m_maxValue) / 2.0f;
            float sigma = (m_maxValue - mean) / 3.0f;
            return Mathf.Clamp(std * sigma + mean, m_minValue, m_maxValue);
        }
    }

    public void DumpRenderTexture(CustomRenderTexture rt, string pngOutPath) {
        //RainManager.RainIntensity TODO
        //string rainIntensity = RM.GetComponent<RainManager>().RainIntensity
        RenderTexture oldRT = RenderTexture.active;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGFloat, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(pngOutPath + "moderate_normal_" + count.ToString() + ".png", bytes);
        count++;
        RenderTexture.active = oldRT;
    }

    void SetRainfall() {
        var mainModule = m_raindropParticles.main;

        //set appearance and baking
        if (octaneRenderer) {
            if (bakeParticles) {
                worldSpaceBakedParticlesGO = new GameObject();
                worldSpaceBakedParticlesGO.name = "(BM)Rainfall_WorldSpace";
                CreateBakedParticlesGO();
            }
            mainModule.startSizeX = new ParticleSystem.MinMaxCurve(OCTANE_START_SIZE_MIN_X, OCTANE_START_SIZE_MAX_X);
            mainModule.startSizeY = new ParticleSystem.MinMaxCurve(OCTANE_START_SIZE_MIN_Y, OCTANE_START_SIZE_MAX_Y);
            mainModule.startSizeZ = new ParticleSystem.MinMaxCurve(OCTANE_START_SIZE_MIN_X, OCTANE_START_SIZE_MAX_X); //same as X

        }
        else {
            mainModule.startSizeX = new ParticleSystem.MinMaxCurve(START_SIZE_MIN_X, START_SIZE_MAX_X);
            mainModule.startSizeY = new ParticleSystem.MinMaxCurve(START_SIZE_MIN_Y, START_SIZE_MAX_Y);
            mainModule.startSizeZ = new ParticleSystem.MinMaxCurve(START_SIZE_MIN_X, START_SIZE_MAX_X); //same as X
        }

        //Raindrop size probabilities according to Kelkar (1945)
        //https://www.ias.ac.in/article/fulltext/seca/022/06/0394-0399
        //float[] lightRainDSP = { 0.015f, 0.32f, 0.31f, 0.285f, 0.07f, 0.0f };
        float[] lightRainDSP = { 0.02f, 0.32f, 0.31f, 0.28f, 0.07f, 0.0f };
        float[] moderateRainDSP = { 0.01f, 0.65f, 0.44f, 0.354f, 0.114f, 0.26f };
        float[] heavyRainDSP = { 0, 0.1f, 0.13f, 0.51f, 0.35f, 0f };

        DropSizeProbability lightRain = new DropSizeProbability(new float[] { 0.02f, 0.32f, 0.31f, 0.28f, 0.7f, 0.0f });
        DropSizeProbability moderateRain = new DropSizeProbability(new float[] { 0.0f, 0.07f, 0.44f, 0.35f, 0.11f, 0.03f });
        DropSizeProbability heavyRain = new DropSizeProbability(new float[] { 0.0f, 0.01f, 0.13f, 0.51f, 0.35f, 0.0f });

        dropSizeProbabilities = new DropSizeProbability[] { lightRain, moderateRain, heavyRain };
        Shader.SetGlobalFloat("_RainCRTSize", m_textureRainPlane.width);
    }
    void Start() {
        //check if particles should be baked for Octane
        if (RM.GetComponent<RainManager>().octaneRenderer) {
            bakeParticles = true;
            octaneRenderer = true;
        }

        SetRainfall();

        //position rainfall areas to camera
        cam = RM.GetComponent<RainManager>().mainCamera;
        try {
            camFollowGO = cam.GetComponent<FollowGO>().go;
        }
        catch {
            staticCamera = true;
        }
        m_raindropCotainer = new Vector3(m_raindropParticles.shape.scale.x, m_raindropParticles.shape.scale.y, m_raindropParticles.shape.scale.z); //container is rotated y <-> z
        camPos = cam.transform.position;

        //update rainSplashParticles according to raindropParticles shape 
        var rainSplashParticleShape = m_rainSplashParticles.shape;
        rainSplashParticleShape.scale = new Vector3(m_raindropCotainer.x, m_raindropCotainer.y, 1.0f);

        //rainfall areas follow the main camera
        UpdateRainfallAreas();

        //create a random value generator
        randomValuesX = new RandomGaussian(-m_raindropCotainer.x, m_raindropCotainer.x);
        randomValuesZ = new RandomGaussian(-m_raindropCotainer.y, m_raindropCotainer.y);
    }

    /// <summary>
    /// Updates Rainfall particle areas according to the camera cam GO.
    /// </summary>
    void UpdateRainfallAreas() {
        camRot = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
        Quaternion camRotUp = Quaternion.LookRotation(new Vector3(0, 0, 0), cam.transform.up);
        Vector3 camPosContainer = cam.transform.position + m_raindropCotainer.y / 2.0f * cam.transform.forward - cam.transform.forward * 7.0f;
        m_raindropParticles.transform.position = new Vector3(camPosContainer.x, m_skyHeight / 2.0f, camPosContainer.z);
        m_raindropParticles.transform.rotation = camRotUp;
        m_rainSplashParticles.transform.position = new Vector3(camPosContainer.x, 0.0f, camPosContainer.z);
        m_rainSplashParticles.transform.rotation = camRotUp;
        m_rainSheetParticles.transform.position = new Vector3(camPosContainer.x, m_skyHeight * 2.0f, camPosContainer.z) + cam.transform.forward * 10.0f;
        m_rainSheetParticles.transform.rotation = camRotUp;
    }

    /// <summary>
    /// Creates a random custom render texture update zone to create a splash after a raindrop hits the surface. The zones are created in a rate of rain intensity.
    /// There texture covers 2x2 units square of the game world. 
    /// </summary>
    void MakeRaindropRipple(ref List<CustomRenderTextureUpdateZone> zones) {
        Vector2 uv = new Vector2(Random.value, Random.value);
        var RDZone = new CustomRenderTextureUpdateZone();
        RDZone.needSwap = true;
        RDZone.passIndex = RandomDropSize() + 1; //pass is shifted by 1
        RDZone.rotation = 0f;
        RDZone.updateZoneCenter = new Vector2(uv.x, uv.y);
        RDZone.updateZoneSize = new Vector2(0.01f, 0.01f);
        //m_textureRainPlane.SetUpdateZones(new CustomRenderTextureUpdateZone[] { RDZone });
        zones.Add(RDZone);
    }

    Vector3 MakeRaindropSplash(Vector3 pos) {
        RaycastHit hit;
        //raycast ignores trigger colliders
        if (Physics.Raycast(pos, transform.TransformDirection(Vector3.down), out hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) {
            //Debug.DrawRay(rayPosition, transform.TransformDirection(Vector3.down) * hit.distance, Color.yellow);

            if (Random.value < m_splashProbability) { //if splash is generated
                var emitParamsSplash = new ParticleSystem.EmitParams(); //Note: does not seem to work with SizeOverLifeTime in separate axis
                                                                        //emitParams.applyShapeToPosition = true; //keep original settings of the particle system?
                emitParamsSplash.position = hit.point;

                //if(hit.transform.GetComponent<Rigidbody>) todo
                Rigidbody rb = hit.collider.gameObject.GetComponent<Rigidbody>();
                if (rb != null) {
                    if ((rb.velocity.magnitude * 3.6f) >= 20.0f)
                        //Debug.Log("car hit");
                        emitParamsSplash.startLifetime = 0.03f; //only when something moving is hit, shorten its lifespan
                }
                m_rainSplashParticles.Emit(emitParamsSplash, 1);
            }

        }
        return hit.point;
    }


    Vector3 GetRaindropSize(ParticleSystem.MainModule mainModule) {
        Vector3 size = new Vector3(Random.Range(mainModule.startSizeX.constantMin, mainModule.startSizeX.constantMax),
                                   Random.Range(mainModule.startSizeY.constantMin, mainModule.startSizeY.constantMax),
                                   Random.Range(mainModule.startSizeZ.constantMin, mainModule.startSizeZ.constantMax));
        return size;
    }

    /// <summary>
    /// Creates raindrops and rain splashes a to the position of the camera (more particles closer to the camera)
    /// </summary>
    void MakeRaindrop() {
        //float x = m_rainSplashParticles.transform.position.x + randomValuesX.NextRandomNumber();
        float x = m_rainSplashParticles.transform.position.x + Random.Range(-m_raindropCotainer.x, m_raindropCotainer.x) / 2.0f;
        float z = m_rainSplashParticles.transform.position.z + Mathf.Abs(randomValuesZ.NextRandomNumber()) - m_raindropCotainer.y / 2.0f;
        Vector3 rayPosition = new Vector3(x, m_skyHeight, z);
        //can follow the m_rainSplashParticles instead of camRot
        Vector3 rayPositionRot = camRot * (rayPosition - m_rainSplashParticles.transform.position) + m_rainSplashParticles.transform.position;

        //raindrops
        var emitParamsDrop = new ParticleSystem.EmitParams();

        //set position
        emitParamsDrop.position = new Vector3(rayPositionRot.x, m_skyHeight, rayPositionRot.z);

        //detect rain collision, draw rainsplash
        Vector3 hitPoint = MakeRaindropSplash(rayPositionRot);

        var mainModule = m_raindropParticles.main;

        //set lifetime

        float lifetime = Mathf.Clamp(mainModule.startLifetime.constant * (m_skyHeight - hitPoint.y) / m_skyHeight, 0, 0.57f); //0.52
        emitParamsDrop.startLifetime = lifetime;

        //set size
        float diag = m_raindropCotainer.x * m_raindropCotainer.y / 10.0f;
        float d = Vector3.Distance(camPos, rayPositionRot);
        float sizeFactor = Mathf.Clamp(d * d / diag, 0.5f, 4.0f); // y<-> z rotated
        Vector3 sizeLOD = GetRaindropSize(mainModule);
        //Vector3 sizeLODscale = new Vector3(sizeLOD.x * sizeFactor, sizeLOD.y, sizeLOD.z * sizeFactor);
        Vector3 sizeLODscale = sizeLOD * sizeFactor; 
        emitParamsDrop.startSize3D = sizeLODscale;

        //emit raindrop particle
        m_raindropParticles.Emit(emitParamsDrop, 1);
    }

    /// <summary>
    /// Creates ripples and splashes according to the rain intenstiy.
    /// </summary>
    void Raindrops() {
        float waterLevel = RM.GetComponent<RainManager>().m_WaterLevel;
        bool simulation = RM.GetComponent<RainManager>().m_WaterSurfaceSimulation;

        float t = Time.deltaTime;

        float val = Random.value;
        //raindrops splashes on the road
        if (val <= m_raindropRate * t) {
            for (int i = 0; i < m_perUnitRaindropCount; ++i) { //tmpCount per unit
                MakeRaindrop();
            }
        }


        //raindrop ripples in puddles -- TODO: covered rain
        List<CustomRenderTextureUpdateZone> zones = new List<CustomRenderTextureUpdateZone>();
        var defaultZone = new CustomRenderTextureUpdateZone();
        defaultZone.needSwap = true;
        defaultZone.passIndex = 0;
        defaultZone.rotation = 0f;
        defaultZone.updateZoneCenter = new Vector2(0.5f, 0.5f);
        defaultZone.updateZoneSize = new Vector2(1f, 1f);
        zones.Add(defaultZone);

        if (val <= m_rainRippleRate * t) {
            //MakeRaindropSplash(pos);

            //not enough water to simulate shallow water or simulation is disabled
            if (waterLevel <= 0.0f || !simulation) {
                zones.Clear();
                return;
            }
            for (int i = 0; i < m_perUnitRainRippleCount; ++i) {
                MakeRaindropRipple(ref zones);
            }
        }
        m_textureRainPlane.SetUpdateZones(zones.ToArray());
        zones.Clear();
    }

    void CreateBakedParticlesGO() {
        bakedParticles = new List<BakedParticleGO>(); //creates GO 
        bakedInstances = new List<GameObject>(); //for instantiated GOs

        BakedParticleGO bakedRaindrop = new BakedParticleGO(new GameObject(), m_raindropParticles, "(BM)Raindrop", new Mesh(), PBRRaindropMaterial, worldSpaceBakedParticlesGO.transform);
        BakedParticleGO bakedRainSplash = new BakedParticleGO(new GameObject(), m_rainSplashParticles, "(BM)RainSplash", new Mesh(), PBRRainSplashMaterial, worldSpaceBakedParticlesGO.transform);
        BakedParticleGO bakedRainSheet = new BakedParticleGO(new GameObject(), m_rainSheetParticles, "(BM)RainSheet", new Mesh(), PBRRainSheetMaterial, worldSpaceBakedParticlesGO.transform);

        bakedParticles.Add(bakedRaindrop);
        bakedParticles.Add(bakedRainSplash);
        bakedParticles.Add(bakedRainSheet);

        m_raindropParticles.GetComponent<Renderer>().enabled = false;
        m_rainSplashParticles.GetComponent<Renderer>().enabled = false;
        m_rainSheetParticles.GetComponent<Renderer>().enabled = false;
    }
    void BakeParticles() {
        ClearBakedParticles();

        //instantiate new baked particles
        for (int i = 0; i < bakedParticles.Count; ++i) {
            BakedParticleGO p = bakedParticles[i]; //copy
            bakedInstances.Add(p.Bake());
        }
    }

    void ClearBakedParticles() {
        //destroy previous instantiated GO
        foreach (GameObject i in bakedInstances)
            Destroy(i);
        bakedInstances.Clear(); //remove all items in the list
    }

    Vector3 CameraVelocity() {
        Vector3 previousCamPos = camPos;
        camPos = cam.transform.position;
        return camPos - previousCamPos;
    }

    /// <summary>
    /// Aligns rainfall to the driving car and the camera - raindrops dont fall vertically when car drives fast under the rain.
    /// </summary>
    void AlignRainfall() { //TODO: shift rain
        var volRaindrop = m_raindropParticles.velocityOverLifetime;
        var volRainSheet = m_rainSheetParticles.velocityOverLifetime;
        var carControl = camFollowGO.GetComponent<SimpleCarController>();
        //Vector3 vel = camFollowGO.GetComponent<Rigidbody>().velocity;
        Vector3 alignment = -CameraVelocity();
        float min = 0.7f;
        float max = 1.4f;
        volRaindrop.x = new ParticleSystem.MinMaxCurve(alignment.x * min, alignment.x * max);
        volRaindrop.z = new ParticleSystem.MinMaxCurve(alignment.z * min, alignment.z * max);
        volRainSheet.x = new ParticleSystem.MinMaxCurve(alignment.x * min, alignment.x * max);
        volRainSheet.z = new ParticleSystem.MinMaxCurve(alignment.z * min, alignment.z * max);
    }

    void CalculateAmountOfRaindrops() {
        m_raindropRate = m_baseRaindropRate; //how often raindrops happen
        m_rainRippleRate = m_rainIntensity * m_baseRippleRate; //how often ripples happen

        float diag = m_raindropCotainer.x * m_raindropCotainer.y;
        float m_perUnitRaindropCountF = m_rainIntensity * (m_rainIntensity + 1) * (m_rainIntensity + 1) * (diag / m_diagBase);
        m_perUnitRaindropCount = (int)m_perUnitRaindropCountF; //how many raindrops
        m_perUnitRainRippleCount = m_rainIntensity * 3; //how many ripples

        m_splashProbability = m_splashProbabilityBase / (m_rainIntensity + 1);
        //m_splashProbability = m_splashProbabilityBase;

        if (octaneRenderer) {
            m_raindropRate = 50;
            m_perUnitRaindropCount *= 15;
            m_splashProbability = 0.3f;
        }
        else{
            m_perUnitRaindropCount *= 5;
        }

        //turn on rain sheets when heavy -- TODO
        //var em = m_rainSheetParticles.emission;
        //if (m_rainIntensity >= 3) {
        //    em.rateOverTime = 20.0f;
        //    if (!m_rainSheetParticles.isPlaying)
        //        m_rainSheetParticles.Play();
        //}
        //else {
        //    em.rateOverTime = 0.0f;
        //    m_rainSheetParticles.Stop();
        //}

    }

    void Update() {
        if (RM.GetComponent<RainManager>().m_dry) {
            return;
        }

        m_rainIntensity = (int)RM.GetComponent<RainManager>().m_RainIntensity;
        UpdateRainfallAreas();

        m_textureRainPlane.ClearUpdateZones();

        if (m_rainIntensity > 0) //it is raining!
        {
            if (!staticCamera) {
                AlignRainfall();
            }
            CalculateAmountOfRaindrops();
            Raindrops();
            m_textureRainPlane.Update(); //update the custom render texture every frame
            //DumpRenderTexture(m_textureRainPlane, ".\\normal_outputs\\");

            if (bakeParticles) {
                BakeParticles();
            }
        }
    }

    int RandomDropSize() {
        return dropSizeProbabilities[m_rainIntensity - 1].GetDropSize();
    }
}

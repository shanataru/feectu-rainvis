using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BakedParticleGO {
    public GameObject m_GO;
    public ParticleSystem m_Particle;
    public Mesh m_BakeMesh;

    public BakedParticleGO(GameObject GO, ParticleSystem p, string name, Mesh mesh, Material octaneMat, Transform parent = null, bool local = false) {
        m_GO = GO;
        m_Particle = p;
        m_BakeMesh = mesh;

        m_GO.name = name;
        m_GO.transform.parent = parent;
        m_GO.AddComponent<MeshFilter>();
        m_GO.AddComponent<MeshRenderer>();
        //m_GO.GetComponent<MeshRenderer>().material = p.GetComponent<Renderer>().material;
        m_GO.GetComponent<MeshRenderer>().material = octaneMat;
        m_GO.GetComponent<MeshRenderer>().receiveShadows = false;
        //m_GO.GetComponent<OctaneUnity.PBRInstanceProperties>().ShadowVisibility = false; //reduce self shadow...

        //locally simulated particles
        if (local) {
            m_GO.transform.position = p.transform.position;
            m_GO.transform.rotation = p.transform.rotation;
        }

        m_GO.SetActive(false);
    }

    public GameObject Bake() {
        m_BakeMesh = new Mesh();
        m_Particle.GetComponent<ParticleSystemRenderer>().BakeMesh(m_BakeMesh);
        m_GO.GetComponent<MeshFilter>().mesh = m_BakeMesh;
        GameObject tmp = Object.Instantiate(m_GO, m_GO.transform.position, m_GO.transform.rotation, m_GO.transform.parent) as GameObject;
        //try {
        //    m_GO.GetComponent<OctaneUnity.PBRInstanceProperties>().ShadowVisibility = false;
        //}
        //catch { }
        tmp.SetActive(true);
        return tmp;
    }
}

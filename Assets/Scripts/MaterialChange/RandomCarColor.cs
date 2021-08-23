using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;

public class RandomCarColor : MonoBehaviour
{
    public GameObject carBody;
    public string folderPath;
    private ArrayList materials;
    private bool enableMaterialChange;

    // Start is called before the first frame update
    void Start()
    {
   
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Generate random nontransparent color
    public Color GetRandomColor()
    {
        return new Color(Random.Range(0f,1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
    }

    // Assign color to a material
    public void ChangeMaterialColor(Material mat, Color col)
    {
        mat.SetColor("_Color", col);
    }

    public void LoadMaterials()
    {
        // Get material files paths.
        DirectoryInfo directory = new DirectoryInfo(folderPath);
        FileInfo[] files = directory.GetFiles("*.mat");

        materials = new ArrayList();
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(".*Assets");

        // Add materials to an array.
        foreach(FileInfo f in files)
        {
            string path = regex.Replace(f.ToString(), "Assets");
            //Debug.Log(path);
            materials.Add((Material)AssetDatabase.LoadAssetAtPath(path, typeof(Material)));
        }

        // Enable material useage.
        enableMaterialChange = true;
        if (materials.Count == 0)
            enableMaterialChange = false;
    }

    // Chooses random material from destigned folder and applies it on selected mesh.
    public void AssignMaterial()
    {
        if (!enableMaterialChange) return;

        // newly added
        //Color col = GetRandomColor();

        int index = Random.Range(0, materials.Count);
        //Debug.Log(materials[index].ToString());
        Material[] mats = carBody.GetComponent<Renderer>().sharedMaterials;
        //Debug.Log(mats.Length.ToString() + "   " + mats.ToString());
        // newly added
        //Material newMat = new Material((Material)materials[index]);
        //ChangeMaterialColor(newMat, col);

        for (int i = 0; i < mats.Length; ++i)
        {
            mats[i] = (Material)materials[index];
        }
        carBody.GetComponent<Renderer>().materials = mats;

        foreach(Material m in carBody.GetComponent<Renderer>().sharedMaterials) {
            //Debug.Log(m.ToString() + Time.time.ToString());
        }

        //Resources.UnloadUnusedAssets();
    }
}
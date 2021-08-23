using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode] // Make live-update even when not in play mode

public class DrawPuddles : MonoBehaviour
{
    public bool draw = false;

    [Header("Draw tool")]
    [Range(1, 10)]
    public int m_drawSize = 100;
    [Range(0.1f, 1.0f)]
    public float m_GreyColor = 0.5f;

    Material mat;
    //shader variables
    int m_PuddleMapScale;
    Texture2D puddleMap;

    void Start()
    {
        mat = transform.GetChild(0).GetComponent<Renderer>().sharedMaterial;
        puddleMap = (Texture2D)mat.GetTexture("_PuddleMap");
        m_PuddleMapScale = (int)mat.GetFloat("_PuddleMapScale");
    }

    // Update is called once per frame
    void Update()
    {
        if (draw) {
            DrawOnMouseClick(m_drawSize, new Color(1, 0, 0, 1.0f));
        }

    }

    //Non-negative modulo
    float mod(float x, int m)
    {
        return (x % m + m) % m;
    }

    void DrawOnMouseClick(int size, Color col) {
        bool leftClick = Input.GetMouseButton(0);
        bool rightClick = Input.GetMouseButton(1);
        if (!leftClick && !rightClick) return;

        RaycastHit hitMouse;
        var rayMouse = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(rayMouse, out hitMouse))
        {
            //clickZone.updateZoneCenter = new Vector2(hitMouse.textureCoord.x, 1f - hitMouse.textureCoord.y);
            Vector2 uvPuddleMap = new Vector2(mod(hitMouse.point.x, m_PuddleMapScale) / m_PuddleMapScale, mod(hitMouse.point.z, m_PuddleMapScale) / m_PuddleMapScale);

            //Vector2 uv = new Vector2(hitMouse.point.x, hitMouse.point.z);
            Debug.Log("UV: " + uvPuddleMap);

            int x = (int)(uvPuddleMap.x * puddleMap.width);
            int y = (int)(uvPuddleMap.y * puddleMap.height);

            Circle(x, y, size, col);
        }
    }

    //https://github.com/ProtoTurtle/BitmapDrawingExampleProject/blob/master/Assets/BitmapDrawing.cs
    public void Circle(int cx, int cy, int r, Color col)
    {
        int x, y, px, nx, py, ny, d;
        Color[] tempArray = puddleMap.GetPixels();

        for (x = 0; x <= r; x++)
        {
            d = (int)Mathf.Ceil(Mathf.Sqrt(r * r - x * x));
            for (y = 0; y <= d; y++)
            {
                px = cx + x;
                nx = cx - x;
                py = cy + y;
                ny = cy - y;

                tempArray[py * puddleMap.width + px] = col;
                tempArray[py * puddleMap.width + nx] = col;
                tempArray[ny * puddleMap.width + px] = col;
                tempArray[ny * puddleMap.width + nx] = col;
            }
        }
        puddleMap.SetPixels(tempArray);
        puddleMap.Apply();
    }
}

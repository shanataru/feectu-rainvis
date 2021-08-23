using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Video;

/// <summary>
/// Changes lights by swapping meshes. All "disabled" lights are pur into "blackbox"
/// </summary>
public class CarDynamicLights : MonoBehaviour
{
    public float blinkIntervalSec = 0.5f;

    public enum Group
    {
        BLINKERS_L,
        BLINKERS_R,
        BRAKE_STRIP,
        BACK_LIGHTS,

        COUNT
    }

    public string[] lightsGroupRepr = { "blinkers-left", "blinkers-right", "back-brake-strip", "back" };
    private GameObject[] primaryLights = new GameObject[(int)Group.COUNT];
    private GameObject[] secondaryLights = new GameObject[(int)Group.COUNT];
    private bool[] blinkActive = new bool[(int)Group.COUNT];
    private bool[] secondaryLightActive = new bool[(int)Group.COUNT];

    private void Start()
    {
        parsePrefab();
        blink(Group.BLINKERS_L, true);
        blink(Group.BLINKERS_R, true);
        blink(Group.BRAKE_STRIP, true);
        blink(Group.BACK_LIGHTS, true);

    }

    /// <summary>
    /// Play mode only
    /// </summary>
    public void blink(Group lights, bool on)
    {
        if (on)
        {
            if (!blinkActive[(int)lights])
            {
                blinkActive[(int)lights] = true;
                StartCoroutine(blinkCoroutine(lights));
            }
        }
        else
        {
            blinkActive[(int)lights] = false;
        }
    }

    private IEnumerator blinkCoroutine(Group lights)
    {
        while (blinkActive[(int)lights])
        {
            swapLights(lights);
            yield return new WaitForSecondsRealtime(blinkIntervalSec);
            swapLights(lights);
            yield return new WaitForSecondsRealtime(blinkIntervalSec);
        }
    }

    public void setLight(Group lights, bool on)
    {
        if(secondaryLightActive[(int)lights] == on)
        {
            return;
        }
        parsePrefab();
        swapLights(lights);
    }

    private void swapLights(Group lights)
    {
        var prim = primaryLights[(int)lights];
        var sec = secondaryLights[(int)lights];
        if(prim == null || sec == null)
        {
            Debug.Log("Lights group is empty");
            return;
        }
        var tr = prim.transform.position;
        prim.transform.position = sec.transform.position;
        sec.transform.position = tr;
        var sc = prim.transform.localScale;
        prim.transform.localScale = sec.transform.localScale;
        sec.transform.localScale = sc;
        secondaryLightActive[(int)lights] = !secondaryLightActive[(int)lights];
    }

    void parsePrefab()
    {
        getLightsFromTransform(transform);
    }

    void getLightsFromTransform(Transform tr)
    {
        foreach(Transform child in tr)
        {
            var seg = child.gameObject.name.Split('_');
            if(seg.Length < 3)
            {
                continue;
            }
            if(seg[0] != "lights")
            {
                continue;
            }
            int id = Array.IndexOf(lightsGroupRepr, seg[1]);
            if(id == -1)
            {
                continue;
            }
            var lightsArr = seg[2] == "s" ? secondaryLights : primaryLights;
            lightsArr[id] = child.gameObject;
        }
    }
}

/**
 * <summary>
 * 
 * The collider enables or disables shallow water simulation script according to the vicinity of the car to a block of road.
 * The road should be labeled as "Road" in the Unity layer.
 * 
 * </summary>
 *
 */

using UnityEngine;

public class DetectCar : MonoBehaviour
{
    private LayerMask roadLayer;
    public GameObject WheelColliders;

    void Start()
    {
        roadLayer = LayerMask.NameToLayer("Road");
    }

    private void OnTriggerStay(Collider otherCol)
    {
        if (otherCol.gameObject.layer == roadLayer)
        {
            otherCol.transform.GetComponent<WaterSurfaceSimulation>().enabled = true;
        }
    }

    private void OnTriggerExit(Collider otherCol)
    {
        if (otherCol.gameObject.layer == roadLayer)
        {
            //otherCol.transform.GetComponent<WaterSurfaceSimulation>().ClearOutCRT();
            otherCol.transform.GetComponent<WaterSurfaceSimulation>().enabled = false;
        }
    }

    public void Clear() {
        foreach (Transform child in WheelColliders.transform)
        {
            //Debug.Log(child.name);
            child.GetChild(0).GetComponent<WheelSplashControl>().Clear();
        }
    }
}

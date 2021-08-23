/** 
 * <summary>
 * 
 * The script detects collisions of a wheel collider with the road. Wheel contact points are then used to create ripples in custom render texture 
 *      which simulates shallow water surface in puddles.
 * 
 * </summary>
 */

using System.Collections;
using System;
using UnityEngine;

public class DetectRoadWheelCollision : MonoBehaviour
{
    public GameObject car;
    WheelCollider wheelCollider;
    GameObject carSplashParticles; //each wheel collider (wheel) has its own "splash" particle

    private LayerMask roadLayer;
    GameObject RM;
    float EPS = 0.01f;
    WheelHit prevHit;
    bool firstHit = true;

    void Start()
    {
        RM = GameObject.Find("RainManager");
        roadLayer = LayerMask.NameToLayer("Road");
        wheelCollider = transform.GetComponent<WheelCollider>();

        if (transform.childCount > 0)
            carSplashParticles = transform.GetChild(0).gameObject;
        else
            Debug.Log("CarSplash object is missing from the wheelcollider " + wheelCollider);

        //the road is dry, turn off splash particles
        if (RM.GetComponent<RainManager>().m_dry)
        {
            carSplashParticles.SetActive(false);
        }
        else
        {
            carSplashParticles.SetActive(true);
        }
    }

    void LateUpdate()
    {
        float carSpeed = car.GetComponent<SimpleCarController>().m_velocity;
        bool carReversed = car.GetComponent<SimpleCarController>().m_reverse;
        //the road is dry, turn off splash particles
        if (RM.GetComponent<RainManager>().m_dry)
        {
            carSplashParticles.SetActive(false);
            return;
        }
        else {
            carSplashParticles.SetActive(true);
        }

        //update speed information for generating splashes
        carSplashParticles.GetComponent<WheelSplashControl>().vehicleVelocity = carSpeed;
        carSplashParticles.GetComponent<WheelSplashControl>().vehicleReversed = carReversed;

        //puddle simulation is disabled - TODO: not working
        //if (!RM.GetComponent<RainManager>().m_WaterSurfaceSimulation)
        //    return;

        //car tire hit something
        if (wheelCollider.GetGroundHit(out WheelHit hit))
        {
            //car is standing still - no need to generate ripples
            if (carSpeed <= EPS)
                return;

            if (firstHit) //first hit after the car is placed correctly
            {
                prevHit = hit;
                if (wheelCollider.GetGroundHit(out WheelHit hitx))  //local variable..
                {
                    hit = hitx;
                    firstHit = false;
                }
                else
                    return;
            }

            //contact point is created if wheel hits the road (SWheelHit)
            if (hit.collider.gameObject.layer == roadLayer) 
            {
                try
                {
                    //new contact
                    Vector3 hitPoint = hit.point + (car.GetComponent<Rigidbody>().velocity * (Time.deltaTime));
                    WaterSurfaceSimulation.CWheelHit wh = new WaterSurfaceSimulation.CWheelHit(hitPoint, hit.normal);

                    //previous contact
                    Vector3 hitPointPrev = prevHit.point + (car.GetComponent<Rigidbody>().velocity * (Time.deltaTime));
                    WaterSurfaceSimulation.CWheelHit whPrev = new WaterSurfaceSimulation.CWheelHit(hitPointPrev, prevHit.normal);

                    var contactWh = Tuple.Create(wh, carSplashParticles);
                    var contactWhprev = Tuple.Create(whPrev, carSplashParticles);
                    hit.collider.transform.GetComponent<WaterSurfaceSimulation>().wheelContacts.Enqueue(contactWh);
                    hit.collider.transform.GetComponent<WaterSurfaceSimulation>().wheelContacts.Enqueue(contactWhprev);

                    //lerp between those two contact points to get continous contact line tire-road
                    float stepSize = 0.05f;
                    float dist = Vector3.Distance(hitPointPrev, hitPoint);
                    float frac = dist;
                    while (frac > 0) {
                        Vector3 hitPointLerp = Vector3.Lerp(hitPointPrev, hitPoint, frac / dist);
                        WaterSurfaceSimulation.CWheelHit whLerp = new WaterSurfaceSimulation.CWheelHit(hitPointLerp, hit.normal);
                        var contact = Tuple.Create(whLerp, carSplashParticles);
                        hit.collider.transform.GetComponent<WaterSurfaceSimulation>().wheelContacts.Enqueue(contact);
                        frac -= stepSize;
                    }
                    prevHit = hit;
                }
                catch
                {
                    return;
                }
            }
        }
    }
}

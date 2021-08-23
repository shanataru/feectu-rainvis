using UnityEngine;
using System.Collections;

public class enableWheelPhysicMaterial : MonoBehaviour
{
    private WheelCollider wheel;
    void Start()
    {
        wheel = GetComponent<WheelCollider>();
    }
    // static friction of the ground material.
    void FixedUpdate()
    {
        WheelHit hit;
        if (wheel.GetGroundHit(out hit))
        {
            WheelFrictionCurve fFriction = wheel.forwardFriction;
            fFriction.stiffness = hit.collider.material.staticFriction;
            wheel.forwardFriction = fFriction;
            WheelFrictionCurve sFriction = wheel.sidewaysFriction;
            sFriction.stiffness = hit.collider.material.staticFriction;
            wheel.sidewaysFriction = sFriction;
        }
    }
}

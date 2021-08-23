using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AxleInfo
{
    public WheelCollider RLWheel;
    public WheelCollider RRWheel;
    public WheelCollider FLWheel;
    public WheelCollider FRWheel;
    public bool motor; // is this wheel attached to motor?
    public bool steering; // does this wheel apply steer angle?
}

public class SimpleCarController : MonoBehaviour
{
    [Header("Car movement")]
    public float m_velocity = 0; //km/h
    public bool m_reverse = false;

    [Header("Controls")]
    public List<AxleInfo> axleInfos; // the information about each individual axle
    public float maxMotorTorque; // maximum torque the motor can apply to wheel
    public float maxSteeringAngle; // maximum steer angle the wheel can have
    public float acceleration = 0.3f;

    static float SPEED_MAX = 60;
    Rigidbody car;

    public void SpeedMeter() {
        //float circ = 2.0f * 3.14f * wheelCollider.radius;
        //wheelSpeed = circ * Mathf.Abs(wheelCollider.rpm) * 0.06f; // km/h
        m_velocity = GetComponent<Rigidbody>().velocity.magnitude * 3.6f;
    }

    public void Reverse() {
        Vector3 vel = GetComponent<Rigidbody>().velocity;
        Vector3 localVel = transform.InverseTransformDirection(vel);
        if (localVel.z > 0) {
            m_reverse = false;
        }
        else {
            m_reverse = true;
        }
    }
    public void Start()
    {
        car = transform.GetComponent<Rigidbody>();
        car.centerOfMass += new Vector3(0, 0, 1.0f);
    }
    public void FixedUpdate()
    {
        SpeedMeter();
        Reverse(); //is car moving backwards?
        float steeringAngle = maxSteeringAngle * Input.GetAxis("Horizontal");
        float motorTorque = maxMotorTorque  * acceleration;
        //float motorTorque = maxMotorTorque * Input.GetAxis("Vertical");
        float speed = car.velocity.magnitude * 3.6f;

        foreach (AxleInfo axleInfo in axleInfos)
        {
            if (axleInfo.steering)
            {
                axleInfo.FLWheel.steerAngle = steeringAngle;
                axleInfo.FRWheel.steerAngle = steeringAngle;
            }
            if (axleInfo.motor)
            {
                var RL = axleInfo.RLWheel;
                var RR = axleInfo.RRWheel;
                var FL = axleInfo.FLWheel;
                var FR = axleInfo.FRWheel;

                if (speed <= SPEED_MAX) {
                    RL.motorTorque = motorTorque;
                    RR.motorTorque = motorTorque;
                    FL.brakeTorque = 0f;
                    FR.brakeTorque = 0f;
                }
                else {
                    RL.motorTorque = 0f;
                    RR.motorTorque = 0f;
                    FL.brakeTorque = 0.1f;
                    FR.brakeTorque = 0.1f;
                }                  
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using NWH.DWP2.ShipController;
using TMPro;
public class USVAgent : Agent
{
    Rigidbody rBody;
    AdvancedShipController shipController;
    List<Engine> engines;
    ShipInputHandler input;
    int stepCount = 0;
    float startTime = 0;
    float delayTime = 30;
    // Start is called before the first frame update
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
        shipController = this.GetComponent<AdvancedShipController>();
        input = shipController.input;
    }

    public Transform Target;
    public Transform targetCamera;
    public TextMeshProUGUI uiPanelText;
    // Update is called once per episode
    public override void OnEpisodeBegin()
    {
        if (this.transform.localPosition.y < 0)
        {
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3( 0, 0.5f, 0);
        }
        this.transform.localPosition = new Vector3(0, 0.5f, 0);
        // Move the target to a new spot
        Target.localPosition = new Vector3(Random.value * 20,
                                           0.5f,
                                           Random.value * 20);
        stepCount = 0;
        startTime = Time.time;
        targetCamera.localPosition = new Vector3(Target.localPosition.x, 10, Target.localPosition.z);
    }

    // Collect all the observations
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(Target.localPosition);
        sensor.AddObservation(this.transform.localPosition);
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);
    }

    // Update is called once per frame
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        uiPanelText.text = "Target\nx: " + Target.localPosition.x.ToString() + "\ny: " + Target.localPosition.z.ToString() + "\nUSV\nx: " + this.transform.localPosition.x.ToString() + "\ny: " + this.transform.localPosition.z.ToString();
        input.Throttle = actionBuffers.ContinuousActions[0];
        input.Steering = actionBuffers.ContinuousActions[1];
        stepCount++;
        // Rewards
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);
        SetReward(-0.01f);
        // Reached target
        if (distanceToTarget < 1.42f)
        {
            SetReward(1.0f);
            EndEpisode();
        }

        // Fell off platform
        else if (Time.time - startTime > delayTime)
        {
            SetReward(-1.0f);
            EndEpisode();
        }
    }
}

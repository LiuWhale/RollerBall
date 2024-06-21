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
    private bool wrongDirection = false;
    private bool distanceOut = false;
    private float headingAngle = 0;
    public Transform Target;
    public Transform Target1;
    public Transform usvCamera;
    public Transform targetCamera;
    public Transform targetCamera1;
    public Transform targetCircle;
    public Transform targetCircle1;
    public TextMeshProUGUI uiPanelText;
    public LineRenderer lineRenderer;
    private StringLogSideChannel stringChannel;

    // Start is called before the first frame update
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
        shipController = this.GetComponent<AdvancedShipController>();
        stringChannel = this.GetComponent<RegisterStringLogSideChannel>().stringChannel;
        input = shipController.input;
    }
    // Update is called once per episode
    public override void OnEpisodeBegin()
    {
        // Reset the velocity, position and rotation
        this.rBody.angularVelocity = Vector3.zero;
        this.rBody.velocity = Vector3.zero;
        this.transform.localPosition = new Vector3( 0, 0.5f, 0);
        this.rBody.rotation = Quaternion.identity;
        // Move the target to a new spot
        var distance = Random.value * 20;
        var theta = Random.value * 2 * Mathf.PI;
        Target.localPosition = new Vector3(distance * Mathf.Abs(Mathf.Cos(theta)),
                                           0.5f,
                                           distance * Mathf.Abs(Mathf.Sin(theta)));
        distance = Random.value * 10 + 100;
        theta = Random.value * 0.9f * Mathf.PI - 0.9f * Random.value / 2;
        Target1.localPosition = new Vector3(distance * Mathf.Abs(Mathf.Cos(theta)),
                                           0.5f,
                                           distance * Mathf.Abs(Mathf.Sin(theta)));
        // Move the camera to the target
        targetCamera.localPosition = new Vector3(Target.localPosition.x, 5, Target.localPosition.z);
        targetCamera1.localPosition = new Vector3(Target1.localPosition.x, 5, Target1.localPosition.z);
        targetCircle.localPosition = new Vector3(Target.localPosition.x, 0.5f, Target.localPosition.z);
        targetCircle1.localPosition = new Vector3(Target1.localPosition.x, 0.5f, Target1.localPosition.z);
        
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, Target.localPosition);
        lineRenderer.SetPosition(1, Target1.localPosition);
        lineRenderer.startWidth = 1f;
        lineRenderer.endWidth = 1f;

        GetHeadingAngle();
        wrongDirection = false;
        distanceOut = false;
    }

    // Collect all the observations
    public override void CollectObservations(VectorSensor sensor)
    {
        float distance2Line = Point2Line(Target.localPosition, Target1.localPosition, this.transform.localPosition);
        distance2Line = distance2Line * JudgeLineSide();

        float theta = DifferenceAngle();
        float costheta = Mathf.Cos(theta * Mathf.Deg2Rad);
        float sintheta = Mathf.Sin(theta * Mathf.Deg2Rad);

        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);
        sensor.AddObservation(distance2Line);
        // angle velocity
        sensor.AddObservation(rBody.angularVelocity.y);
        sensor.AddObservation(costheta);
        sensor.AddObservation(sintheta);
    }

    // Update is called once per frame
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        usvCamera.localPosition = new Vector3(this.transform.localPosition.x, 35, this.transform.localPosition.z);
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target1.localPosition);
        float distance2Line = Point2Line(Target.localPosition, Target1.localPosition, this.transform.localPosition);
        // take action value from actionBuffers
        input.Throttle = actionBuffers.ContinuousActions[0];
        input.Steering = actionBuffers.ContinuousActions[1];
        
        GetHeadingAngle();
        // Rewards
        SetReward(-distance2Line-distanceToTarget);
        CheckWrongDirection();
        CheckDistanceOut();
        bool wd = false;
        bool distOut = false;
        bool finished = false;
        // Reached target
        if (distanceToTarget < 3.0f)
        {
            finished = true;
            stringChannel.SendStringToPython("True," + wd.ToString() + "," + distOut.ToString() + "," + this.transform.localPosition.x.ToString() + "," + this.transform.localPosition.z.ToString());
            EndEpisode();
        }
        else if (wrongDirection)
        {
            wd = wrongDirection;
            stringChannel.SendStringToPython(finished.ToString() + "," + "True" + "," + distOut.ToString() + "," + this.transform.localPosition.x.ToString() + "," + this.transform.localPosition.z.ToString());
            EndEpisode();
        }
        else if (distanceOut)
        {
            distOut = distanceOut;
            stringChannel.SendStringToPython(finished.ToString() + "," + wd.ToString() + "," + "True" + "," + this.transform.localPosition.x.ToString() + "," + this.transform.localPosition.z.ToString());
            EndEpisode();
        }

        // show the info on ui panel
        uiPanelText.text = "Target x: " + Target.localPosition.x.ToString() + " y: " + Target.localPosition.z.ToString() + "\nTarget1 x: " + Target1.localPosition.x.ToString() + " y: " + Target1.localPosition.z.ToString() + "\nUSV x: " + this.transform.localPosition.x.ToString() + " y: " + this.transform.localPosition.z.ToString() + "\nAngularVel: " + rBody.angularVelocity.y.ToString() + "\nHeadingAngle: " + headingAngle.ToString() + "\nDiffAngle: " + DifferenceAngle().ToString() + "\nDistanceOut: " + distanceOut.ToString() + "\nWrongDirection: " + wd.ToString() + "\ndistanceToTarget: " + distOut.ToString() + "\ndistance2Line: " + distance2Line.ToString() + "\ninput.Throttle: " + input.Throttle.ToString() + "\ninput.Steering: " + input.Steering.ToString();
    }
    private void GetHeadingAngle()
    {
        headingAngle = rBody.rotation.eulerAngles.y;
        if (headingAngle > 180)
        {
            headingAngle -= 360;
        }
        else if (headingAngle < -180)
        {
            headingAngle += 360;
        }
    }
    public float Point2Line(Vector3 p1, Vector3 p2, Vector3 usvPos)
    {
        //p1->p2的向量
        Vector3 p1_2 = p2 - p1;
        //p1->target向量
        Vector3 p1_target = usvPos - p1;
        //计算投影p1->f
        Vector3 p1f = Vector3.Project(p1_target, p1_2);
        //加上p1坐标 然后计算距离
        float distance = Vector3.Distance(usvPos, p1f + p1);
        return distance;
    }
    public float TwoPointAngle(Vector3 p1, Vector3 p2)
    {
        Vector2 p1_2 = new Vector2(p2.x - p1.x, p2.z - p1.z);
        Vector2 worldDirection = new Vector2(0, 1);
        float angle = Vector2.Angle(worldDirection, p1_2);
        return angle;
    }
    public float DifferenceAngle()
    {
        float diffAngle =  TwoPointAngle(Target.localPosition, Target1.localPosition) - headingAngle;
        if (diffAngle > 180)
        {
            diffAngle -= 360;
        }
        else if (diffAngle < -180)
        {
            diffAngle += 360;
        }
        return diffAngle;
    }
    public int JudgeLineSide()
    {
        Vector3 p1 = Target.localPosition;
        Vector3 p2 = Target1.localPosition;
        Vector3 usvPos = this.transform.localPosition;
        float result = (p2.x - p1.x) * (usvPos.z - p1.z) - (p2.z - p1.z) * (usvPos.x - p1.x);
        if (result > 0)
        {
            return 1;
        }
        else if (result < 0)
        {
            return -1;
        }
        else
        {
            return 0;
        }
    }
    public void CheckWrongDirection()
    {
        float diffAngle = Mathf.Abs(DifferenceAngle());
        if (diffAngle > 90)
        {
            wrongDirection = true;
        }
    }
    public void CheckDistanceOut()
    {
        float distance = Point2Line(Target.localPosition, Target1.localPosition, this.transform.localPosition);
        if (distance > 20)
        {
            distanceOut = true;
        }
    }
}

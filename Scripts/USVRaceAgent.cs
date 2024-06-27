using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using NWH.DWP2.ShipController;
using TMPro;
using PathCreation;
public class USVRaceAgent : Agent
{
    Rigidbody rBody;
    AdvancedShipController shipController;
    List<Engine> engines;
    ShipInputHandler input;
    private bool wrongDirection = false;
    private bool distanceOut = false;
    private float headingAngle = 0;
    private StringLogSideChannel stringChannel;
    public Transform usvCamera;
    public TextMeshProUGUI uiPanelText;
    public LineRenderer[] lineRenderers;
    public PathCreator pathCreator;
    public Transform rudder;
    public float maxOutDistance = 20;
    public float deltaDistance = 1.0f;

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
        this.transform.localPosition = new Vector3(0, 0.5f, 1);
        this.rBody.rotation = Quaternion.identity;
        // this.rBody.rotation = new Quaternion(0, -90, 0, 0);
        
        headingAngle = GetHeadingAngle(rBody);
        wrongDirection = false;
        distanceOut = false;

        var items = FindClosestPoint(this.transform, lineRenderers[2]);
        float time = pathCreator.path.GetTimeAtDistance(items.Item1);
        float deltaTime = pathCreator.path.GetTimeAtDistance(deltaDistance);
        Debug.Log("Current Time: " + time + " Length: " + items.Item1 + " Distance: " +items.Item2);
        for (int i = 0; i < Mathf.FloorToInt(1 / deltaTime); i++)
        {
            Vector3 pt = pathCreator.path.GetPointAtTime(time);
            // Create cube to show the center point
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.GetComponent<BoxCollider>().isTrigger = true;
            cube.AddComponent<DestroyOnTrigger>();
            cube.transform.position = pt;
            cube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            time += deltaTime;
        }
    }
    // Collect all the observations
    public override void CollectObservations(VectorSensor sensor)
    {
        float longitVelocity = GetLocalVelocity(rBody).z;
        float angularVelocity = rBody.angularVelocity.y;
        // rudder angle
        float rudderAngle = rudder.rotation.y;
        // Distance to the closest point on the LineRenderer
        var items = FindClosestPoint(this.transform, lineRenderers[2]);
        float centerDistance = items.Item2;
        // Difference Angle between the heading angle and the direction on the line
        Vector2 baseDirection = items.Item3;
        Vector2 forwardVector = new Vector2(this.transform.forward.x, this.transform.forward.z);
        float diffAngle = DifferenceAngle(baseDirection, forwardVector) * Mathf.Deg2Rad;
        // Judge
        if (centerDistance > maxOutDistance)
        {
            distanceOut = true;
        }
        if (diffAngle > Mathf.PI/2 || diffAngle < -Mathf.PI/2)
        {
            wrongDirection = true;
        }
        // Get the center forward points of the path
        float time = pathCreator.path.GetTimeAtDistance(items.Item1);
        float deltaTime = pathCreator.path.GetTimeAtDistance(deltaDistance);
        List<float> centerPointList = new List<float>();
        Debug.Log("Current Time: " + time + " Length: " + items.Item1 + " Distance: " +items.Item2);
        for (int i = 0; i < 13; i++)
        {
            time += deltaTime;
            Vector3 pt = pathCreator.path.GetPointAtTime(time);
            centerPointList.Add(pt.x);
            centerPointList.Add(pt.y);     
        }

        sensor.AddObservation(longitVelocity);
        sensor.AddObservation(angularVelocity);
        sensor.AddObservation(rudderAngle);
        // angle velocity
        sensor.AddObservation(centerDistance);
        sensor.AddObservation(diffAngle);
        sensor.AddObservation(centerPointList);
    }
    // Update is called once per frame
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        usvCamera.localPosition = new Vector3(this.transform.localPosition.x, 35, this.transform.localPosition.z);
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, this.transform.localPosition);
        float distance2Line = Point2Line(this.transform.localPosition, this.transform.localPosition, this.transform.localPosition);
        // take action value from actionBuffers
        input.Throttle = actionBuffers.ContinuousActions[0];
        input.Steering = actionBuffers.ContinuousActions[1];
        
        headingAngle = GetHeadingAngle(rBody);
        // Rewards
        SetReward(-distance2Line);
        // CheckWrongDirection();
        // CheckDistanceOut();
        bool wd = false;
        bool distOut = false;
        bool finished = false;
        // Reached target
        // if (distanceToTarget < 3.0f)
        // {
        //     finished = true;
        //     stringChannel.SendStringToPython("True," + wd.ToString() + "," + distOut.ToString() + "," + this.transform.localPosition.x.ToString() + "," + this.transform.localPosition.z.ToString());
        //     EndEpisode();
        // }
        // else if (wrongDirection)
        // {
        //     wd = wrongDirection;
        //     stringChannel.SendStringToPython(finished.ToString() + "," + "True" + "," + distOut.ToString() + "," + this.transform.localPosition.x.ToString() + "," + this.transform.localPosition.z.ToString());
        //     EndEpisode();
        // }
        // else if (distanceOut)
        // {
        //     distOut = distanceOut;
        //     stringChannel.SendStringToPython(finished.ToString() + "," + wd.ToString() + "," + "True" + "," + this.transform.localPosition.x.ToString() + "," + this.transform.localPosition.z.ToString());
        //     EndEpisode();
        // }
        // show the info on ui panel
        // uiPanelText.text = "USV x: " + this.transform.localPosition.x.ToString() + " y: " + this.transform.localPosition.z.ToString() + "\nAngularVel: " + rBody.angularVelocity.y.ToString() + "\nHeadingAngle: " + headingAngle.ToString() + "\nDiffAngle: " + DifferenceAngle().ToString() + "\nDistanceOut: " + distanceOut.ToString() + "\nWrongDirection: " + wd.ToString() + "\ndistanceToTarget: " + distOut.ToString() + "\ndistance2Line: " + distance2Line.ToString() + "\ninput.Throttle: " + input.Throttle.ToString() + "\ninput.Steering: " + input.Steering.ToString();
    }
    // Get object velocity in local axis
    private Vector3 GetLocalVelocity(Rigidbody rigidbody)
    {
        Vector3 localVelocity = rigidbody.transform.InverseTransformDirection(rigidbody.velocity);
        return localVelocity;
    }
    // Get the heading angle of the USV
    private float GetHeadingAngle(Rigidbody rigidbody)
    {
        float angle = rigidbody.rotation.eulerAngles.y;
        if (angle > 180)
        {
            angle -= 360;
        }
        else if (angle < -180)
        {
            angle += 360;
        }
        return angle;
    }
    // Calculate the distance between the USV and the line
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
    // Calculate the difference angle between the heading angle and the angle between two points in world direction
    public float DifferenceAngle(Vector2 baseDirection, Vector2 destination)
    {
        float diffAngle =  Vector2.Angle(baseDirection, destination);
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
        Vector3 p1 = this.transform.localPosition;
        Vector3 p2 = this.transform.localPosition;
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
    // public void CheckWrongDirection()
    // {
    //     float diffAngle = Mathf.Abs(DifferenceAngle());
    //     if (diffAngle > 90)
    //     {
    //         wrongDirection = true;
    //     }
    // }
    // Check if the USV is out of the line
    public void CheckDistanceOut(Vector3 p1, Vector3 p2, Vector3 usvPos)
    {
        float distance = Point2Line(p1, p2, usvPos);
        float maxOutDistance = 20;
        if (distance > maxOutDistance)
        {
            distanceOut = true;
        }
    }
    // Find the closest point on the LineRenderer to the object
    System.Tuple<float, float, Vector2> FindClosestPoint(Transform objectTransform, LineRenderer lineRenderer)
    {
        Vector3 objectPosition = objectTransform.position;
        Vector3[] linePositions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(linePositions);

        float minDistance = Mathf.Infinity;
        Vector3 closestPoint = Vector3.zero;
        int closestSegmentIndex = 0;
        // Debug.Log("Fisrt postion: " + linePositions[0].ToString());
        // Find the closest point on the LineRenderer to the object
        for (int i = 0; i < linePositions.Length - 1; i++)
        {
            Vector3 A = linePositions[i];
            Vector3 B = linePositions[i + 1];
            // 在A和B生成一个cube
            // GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // cube.transform.position = A;

            Vector3 AB = B - A;
            Vector3 AP = objectPosition - A;
            float t = Vector3.Dot(AP, AB) / Vector3.Dot(AB, AB);
            t = Mathf.Clamp01(t); // 确保 t 在 [0, 1] 之间，保证点在线段上

            closestPoint = A + t * AB; // 计算线段上的最近点
            float distance = Vector3.Distance(objectPosition, closestPoint); // 计算距离

            if (distance < minDistance && t >= 0 && t <= 1)
            {
                minDistance = distance;
                closestSegmentIndex = i;
            }
        }

        // Debug.Log("Minimum Distance: " + minDistance);
        // Debug.Log("Closest Point on LineRenderer: " + closestPoint);

        // Calculate the length from LineRenderer start to closest point
        float lengthFromStart = 0f;
        for (int i = 0; i < closestSegmentIndex; i++)
        {
            lengthFromStart += Vector3.Distance(linePositions[i], linePositions[i + 1]);
        }
        lengthFromStart += Vector3.Distance(linePositions[closestSegmentIndex], closestPoint);

        // Debug.Log("Length from LineRenderer start to closest point: " + lengthFromStart);
        Vector2 lineDirection = new Vector2(closestPoint.x - linePositions[closestSegmentIndex].x, closestPoint.z - linePositions[closestSegmentIndex].z);
        return System.Tuple.Create(lengthFromStart, minDistance, lineDirection);
    }
}

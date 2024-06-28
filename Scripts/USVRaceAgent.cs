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
    public StringLogSideChannel stringChannel;
    private Vector3 previousVelocity = Vector3.zero;
    private Vector3 acceleration = Vector3.zero;
    private float raceScale;

    public Transform usvCamera;
    public TextMeshProUGUI uiPanelText;
    public LineRenderer[] lineRenderers;
    public PathCreator pathCreator;
    public Transform rudder;
    public float maxOutDistance = 10;
    public float deltaDistance = 5.0f;
    public float driectionDistance = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
        shipController = this.GetComponent<AdvancedShipController>();
        input = shipController.input;
        // Notice: the scale is important for travelled length and others about the race path!!!
        raceScale = pathCreator.transform.localScale.x;
        deltaDistance /= raceScale;
        driectionDistance /= raceScale;
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
        for (int i = 0; i < Mathf.FloorToInt(1 / deltaTime); i++)
        {
            Vector3 pt = pathCreator.path.GetPointAtTime(time);
            // Create cube to show the center point
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.GetComponent<BoxCollider>().isTrigger = true;
            cube.AddComponent<DestroyOnTrigger>();
            cube.transform.position = pt;
            cube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            // cube挂载到Road Creator物体下
            cube.transform.parent = pathCreator.transform;

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
        float time = pathCreator.path.GetTimeAtDistance(items.Item1);
        float secondTime = pathCreator.path.GetTimeAtDistance(items.Item1 + 0.1f);
        float centerDistance = items.Item2;
        // Difference Angle between the heading angle and the direction on the line
        Vector3 p1 = pathCreator.path.GetPointAtTime(time);
        Vector3 p2 = pathCreator.path.GetPointAtTime(secondTime);
        Vector2 baseDirection = new Vector2(p2.x - p1.x, p2.z - p1.z);
        Vector2 forwardVector = new Vector2(this.transform.forward.x, this.transform.forward.z);
        float diffAngle = DifferenceAngle(baseDirection, forwardVector) * Mathf.Deg2Rad;
        // Judge distanceOut and wrongDirection
        if (centerDistance > maxOutDistance)
        {
            distanceOut = true;
        }
        if (diffAngle > Mathf.PI/2 || diffAngle < -Mathf.PI/2)
        {
            wrongDirection = true;
        }
        
        // Get the center forward points of the path
        float deltaTime = pathCreator.path.GetTimeAtDistance(deltaDistance);
        List<float> centerPointList = new List<float>();
        for (int i = 0; i < 13; i++)
        {
            time += deltaTime;
            Vector3 pt = pathCreator.path.GetPointAtTime(time);
            centerPointList.Add(pt.x / 100 - this.transform.localPosition.x / 100);
            centerPointList.Add(pt.z / 100 - this.transform.localPosition.z / 100);
        }

        sensor.AddObservation(longitVelocity / 13.86f);
        sensor.AddObservation(angularVelocity / Mathf.PI * 2);
        sensor.AddObservation(rudderAngle / Mathf.PI * 4);
        sensor.AddObservation(centerDistance / maxOutDistance);
        sensor.AddObservation(diffAngle / Mathf.PI);
        sensor.AddObservation(centerPointList);

        // 计算当前速度
        Vector3 currentVelocity = GetComponent<Rigidbody>().velocity;
        // 计算速度变化量
        Vector3 velocityChange = currentVelocity - previousVelocity;
        // 计算加速度：速度变化量除以时间变化量
        acceleration = rBody.transform.InverseTransformDirection(velocityChange / Time.deltaTime);
        // 更新前一帧的速度为当前速度
        previousVelocity = currentVelocity;
    }
    // Update is called once per frame
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        usvCamera.localPosition = new Vector3(this.transform.localPosition.x, 35, this.transform.localPosition.z);
        var items = FindClosestPoint(this.transform, lineRenderers[2]);
        float time = pathCreator.path.GetTimeAtDistance(items.Item1);
        float secondTime = pathCreator.path.GetTimeAtDistance(items.Item1 + 0.1f);

        Vector3[] linePositions = new Vector3[lineRenderers[2].positionCount];
        lineRenderers[2].GetPositions(linePositions);
        Vector3 targetPoint = linePositions[0];
        Vector3 closetPoint = linePositions[items.Item4];
        Vector3 p1 = pathCreator.path.GetPointAtTime(time);
        Vector3 p2 = pathCreator.path.GetPointAtTime(secondTime);
        Vector2 baseDirection = new Vector2(p2.x - p1.x, p2.z - p1.z);
        Vector2 forwardVector = new Vector2(this.transform.forward.x, this.transform.forward.z);

        int side = JudgeLineSide(p1, p2);
        float diffAngle = DifferenceAngle(baseDirection, forwardVector) * side;
        float distanceToTarget = Vector3.Distance(targetPoint, closetPoint);
        float distance2Line = items.Item2 * side;

        float travelledTime = pathCreator.path.GetTimeAtDistance(items.Item1);
        // take action value from actionBuffers
        input.Throttle = actionBuffers.ContinuousActions[0];
        input.Steering = actionBuffers.ContinuousActions[1];

        bool wd = false;
        bool distOut = false;
        bool finished = false;
        // Reached target
        if (distanceToTarget < 0.2f)
        {
            finished = true;
            AddReward(100);
            EndEpisode();
        }
        else if (wrongDirection)
        {
            wd = wrongDirection;
            AddReward(-100);
            EndEpisode();
        }
        else if (distanceOut)
        {
            distOut = distanceOut;
            AddReward(-100);
            EndEpisode();
        }
        else
        {
            // Rewards
            AddReward(GetLocalVelocity(rBody).z * Mathf.Cos(diffAngle * Mathf.Deg2Rad) / 10);
        }
        stringChannel.SendStringToPython(finished.ToString() + "," + wd.ToString() + "," + distOut.ToString() + "," + this.transform.localPosition.x.ToString() + "," + this.transform.localPosition.z.ToString());
        // show the info on ui panel
        if (uiPanelText != null)
        {
            uiPanelText.text = "USV x: " + this.transform.localPosition.x.ToString() + " y: " + this.transform.localPosition.z.ToString() + "\nAngularVel: " + rBody.angularVelocity.y.ToString() + "\nAcceleration: " + acceleration.ToString() + "\nLongutude Speed: " + GetLocalVelocity(rBody).z.ToString() + "\nCloset point: " + items.Item5.ToString() + "\nTravelled length: " + (items.Item1 * raceScale).ToString() + "\ntime: " + time.ToString() + "\nSecond time: " + secondTime.ToString() +"\nFisrt Point: " + p1.ToString() + "\nSecond Point: " + p2.ToString() + "\nCloset Point: " + linePositions[items.Item4].ToString() + "\nDiffAngle: " + diffAngle.ToString() + "\nDistanceOut: " + distanceOut.ToString() + "\nWrongDirection: " + wd.ToString() + "\ndistanceToTarget: " + distOut.ToString() + "\ndistance2Line: " + distance2Line.ToString() + "\ninput.Throttle: " + input.Throttle.ToString() + "\ninput.Steering: " + input.Steering.ToString();
        }
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
        return diffAngle;
    }
    public int JudgeLineSide(Vector3 p1, Vector3 p2)
    {
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
    System.Tuple<float, float, Vector2, int, Vector3> FindClosestPoint(Transform objectTransform, LineRenderer lineRenderer)
    {
        Vector3 objectPosition = objectTransform.position;
        Vector3[] linePositions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(linePositions);

        float minDistance = Mathf.Infinity;
        Vector3 closestPoint = Vector3.zero;
        int closestSegmentIndex = 0;
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
            if (t >= 0 && t <= 1)
            {
                closestPoint = A + t * AB; // 计算线段上的最近点
                float distance = Vector3.Distance(objectPosition, closestPoint); // 计算距离

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestSegmentIndex = i;
                }
            }
        }

        // Calculate the length from LineRenderer start to closest point
        float lengthFromStart = 0f;
        for (int i = 0; i < closestSegmentIndex; i++)
        {
            lengthFromStart += Vector3.Distance(linePositions[i], linePositions[i + 1]);
        }
        // lengthFromStart += Vector3.Distance(linePositions[closestSegmentIndex], closestPoint);

        Vector2 lineDirection = new Vector2(linePositions[closestSegmentIndex + 1].x - linePositions[closestSegmentIndex].x, linePositions[closestSegmentIndex + 1].z - linePositions[closestSegmentIndex].z);
        return System.Tuple.Create(lengthFromStart / raceScale, minDistance, lineDirection, closestSegmentIndex, closestPoint);
    }
}

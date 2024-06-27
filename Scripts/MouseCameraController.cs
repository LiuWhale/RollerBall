using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseCameraControl : MonoBehaviour
{
    public float moveSpeed = 10f;        // 摄像机移动速度
    public float rotationSpeed = 100f;   // 摄像机旋转速度
    public float zoomSpeed = 10f;        // 缩放速度
    public float minFov = 15f;           // 最小视野
    public float maxFov = 90f;           // 最大视野
    public float sensitivity = 10f;      // 鼠标灵敏度

    private float currentFov;
    private Camera m_camera;

    void Start()
    {   m_camera = this.GetComponent<Camera>();
        currentFov = m_camera.fieldOfView; // 获取当前视野
    }

    void Update()
    {
        HandleMovement();
        // HandleRotation();
        HandleZoom();
    }

    void HandleMovement()
    {
        // 获取键盘输入
        float moveX = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float moveZ = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;

        // 移动摄像机
        transform.Translate(moveX, moveZ, 0);
    }

    void HandleRotation()
    {
        // 获取鼠标移动输入
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        // 计算旋转
        Vector3 rotation = transform.localEulerAngles;
        rotation.y += mouseX;
        rotation.x -= mouseY;

        // 应用旋转
        transform.localEulerAngles = rotation;
    }

    void HandleZoom()
    {
        // 获取滚轮输入
        float scroll = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        // 调整视野
        currentFov -= scroll;
        currentFov = Mathf.Clamp(currentFov, minFov, maxFov);
        // 应用视野
        m_camera.fieldOfView = currentFov;
    }
}



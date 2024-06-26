using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraSwitcher : MonoBehaviour
{
    public Camera camera1;
    public Camera camera2;
    public Button switchButton;

    private bool isCamera1Active = true;

    void Start()
    {
        // 确保初始状态正确
        camera1.enabled = true;
        camera2.enabled = false;

        // 添加按钮点击事件监听器
        switchButton.onClick.AddListener(SwitchCamera);
    }

    void SwitchCamera()
    {
        isCamera1Active = !isCamera1Active;

        camera1.enabled = isCamera1Active;
        camera2.enabled = !isCamera1Active;
    }
}

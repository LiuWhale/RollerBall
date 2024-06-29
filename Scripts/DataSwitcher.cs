using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DataSwitcher : MonoBehaviour
{
    public Button switchButton;
    public delegate void SwitchDataShow();
    public SwitchDataShow switchDataShow;

    void Start()
    {
        // 添加按钮点击事件监听器
        switchButton.onClick.AddListener(SwitchCameraOnClick);
    }

    void SwitchCameraOnClick()
    {
        switchDataShow();
    }
}

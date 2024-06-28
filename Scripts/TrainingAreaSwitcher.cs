using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class TrainingAreaSwitcher : MonoBehaviour
{
    public TMP_Dropdown areaDropdown; // 拖入下拉菜单
    public RenderTexture cameraTexture;
    public TextMeshProUGUI textMeshProUGUI;
    public List<Texture> cameraTargetTextures;
    public List<Transform> usvCameras;
    public List<Transform> upDownCameras;
    public List<Transform> followCameras;
    private CameraSwitcher cameraSwitcher;
    public List<USVRaceAgent> uSVRaceAgents;
    private int historyIndex = 0;

    void Start()
    {
        cameraSwitcher = this.GetComponent<CameraSwitcher>();
        // 初始化下拉菜单选项
        areaDropdown.options.Clear();
        for (int i = 0; i < usvCameras.Count; i++)
        {
            areaDropdown.options.Add(new TMP_Dropdown.OptionData("Area " + (i + 1)));
        }
        areaDropdown.value = 0; // 设置默认选择项
        areaDropdown.onValueChanged.AddListener(delegate {
            SwitchCamera(areaDropdown.value);
        });
    }

    void SwitchCamera(int index)
    {
        cameraTargetTextures[historyIndex] = null;
        cameraTargetTextures[index] = cameraTexture;
        uSVRaceAgents[historyIndex].uiPanelText = null;
        uSVRaceAgents[index].uiPanelText = textMeshProUGUI;
        cameraSwitcher.camera1 = followCameras[index].Find("Vehicle Camera Cinemachine").GetComponent<Camera>();
        cameraSwitcher.camera2 = upDownCameras[index].GetComponent<Camera>();
        
        for (int i = 0; i < usvCameras.Count; i++)
        {
            usvCameras[i].gameObject.SetActive(i==index);
            upDownCameras[i].gameObject.SetActive(i==index);
            followCameras[i].gameObject.SetActive(i==index);      
            uSVRaceAgents[i].pathCreator.gameObject.SetActive(i==index);      
        }
        historyIndex = index;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class MultiTrainingAreaManagement : MonoBehaviour
{
    public GameObject trainingAreaPrefab;
    public int trainingAreaCount = 10;
    public int rows = 2;
    public float xSpace = 4000;
    public float ySpace = 4000;
    public Button switchButton;
    public RenderTexture cameraTexture;
    public TextMeshProUGUI textMeshProUGUI;
    private List<Texture> cameraTargetTextures = new List<Texture>();
    private List<Transform> usvCameras = new List<Transform>();
    private List<Transform> upDownCameras = new List<Transform>();
    private List<Transform> followCameras = new List<Transform>();
    private CameraSwitcher cameraSwitcher;
    private List<USVRaceAgent> uSVRaceAgents = new List<USVRaceAgent>();
    private TrainingAreaSwitcher trainingAreaSwitcher;
    // Start is called before the first frame update
    void Start()
    {
        cameraSwitcher = this.AddComponent<CameraSwitcher>();
        cameraSwitcher.switchButton = switchButton;
        trainingAreaSwitcher = this.GetComponent<TrainingAreaSwitcher>();
        // 实例化trainingAreaPrefab，位置通过xSpace和ySpace偏移进行计算，并用rows进行排列排序
        for (int i = 0; i < trainingAreaCount; i++)
        {
            GameObject trainingArea = Instantiate(trainingAreaPrefab, new Vector3(i % rows * xSpace, 0, i / rows * ySpace), Quaternion.identity);
            // 获取USVCamera名称物体下的camera并将target texture赋值
            cameraTargetTextures.Add(trainingArea.transform.Find("USVCamera").GetComponent<Camera>().targetTexture);
            cameraTargetTextures[cameraTargetTextures.Count - 1] = null;
            // mini map
            usvCameras.Add(trainingArea.transform.Find("USVCamera"));
            usvCameras[usvCameras.Count - 1].gameObject.SetActive(false);

            upDownCameras.Add(trainingArea.transform.Find("Environment").Find("UpDownCamera"));
            upDownCameras[upDownCameras.Count - 1].gameObject.SetActive(false);

            followCameras.Add(trainingArea.transform.Find("USV").Find("Cameras"));
            followCameras[followCameras.Count - 1].gameObject.SetActive(false);

            uSVRaceAgents.Add(trainingArea.transform.Find("USV").GetComponent<USVRaceAgent>());
            uSVRaceAgents[uSVRaceAgents.Count - 1].uiPanelText = null;
            uSVRaceAgents[uSVRaceAgents.Count - 1].stringChannel = this.GetComponent<RegisterStringLogSideChannel>().stringChannel;
        }
        // 设置第一个trainingArea的cameraTargetTextures为cameraTexture
        cameraTargetTextures[0] = cameraTexture;
        usvCameras[0].gameObject.SetActive(true);
        upDownCameras[0].gameObject.SetActive(true);
        followCameras[0].gameObject.SetActive(true);
        uSVRaceAgents[0].uiPanelText = textMeshProUGUI;

        cameraSwitcher.camera1 = followCameras[0].Find("Vehicle Camera Cinemachine").GetComponent<Camera>();
        cameraSwitcher.camera2 = upDownCameras[0].GetComponent<Camera>();

        trainingAreaSwitcher.cameraTargetTextures = cameraTargetTextures;
        trainingAreaSwitcher.usvCameras = usvCameras;
        trainingAreaSwitcher.upDownCameras = upDownCameras;
        trainingAreaSwitcher.followCameras = followCameras;
        trainingAreaSwitcher.uSVRaceAgents = uSVRaceAgents;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

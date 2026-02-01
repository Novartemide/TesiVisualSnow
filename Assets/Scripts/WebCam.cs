using UnityEngine;
using System.Collections;
using UnityEngine.Android;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class WebCam : MonoBehaviour{
    private int currentCamIndex = 0;
    private WebCamTexture tex;
    private Grain grainLayer;
    [Header("Camera Setup")]
    [SerializeField] private RawImage display;
    [Header("Visual Snow Effect")]
    [SerializeField] private PostProcessVolume mainVolume;
    [Header("UI Settings")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider intensitySlider;
    [SerializeField] private GameObject openSettingsButton;
    [SerializeField] private GameObject startButton;
    [SerializeField] private Slider sizeSlider; 
    [SerializeField] private Toggle colorToggle; 


    void Start(){
        if(mainVolume != null && mainVolume.profile.TryGetSettings(out grainLayer)){
            if(intensitySlider != null) {
                intensitySlider.value = grainLayer.intensity.value;
                intensitySlider.onValueChanged.AddListener(UpdateIntensity);
            }
        }
        if(settingsPanel != null) settingsPanel.SetActive(false);
        if(openSettingsButton != null) openSettingsButton.SetActive(false); 
        if(startButton != null) startButton.SetActive(true);
    }


    public void OnStartPressed(){
        #if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera)){
                Permission.RequestUserPermission(Permission.Camera);
            }
        #endif

        if(startButton != null) startButton.SetActive(false);
        if(openSettingsButton != null) openSettingsButton.SetActive(true);
        
        StartStopCam_Clicked();
    }

    public void OpenSettings(){
        settingsPanel.SetActive(true);       
        openSettingsButton.SetActive(false); 
    }

    public void CloseSettings(){
        settingsPanel.SetActive(false);      
        openSettingsButton.SetActive(true);  
    }

    public void SwapCam_Clicked(){
        if (WebCamTexture.devices.Length > 0){
            currentCamIndex++;
            currentCamIndex %= WebCamTexture.devices.Length;
            if (tex != null){
                StopWebCam();
                StartStopCam_Clicked();
            }
        }
    }
    
    public void StartStopCam_Clicked(){
        if (tex != null){
            StopWebCam();
        }
        else{
            if (WebCamTexture.devices.Length > 0)
            {
                WebCamDevice device = WebCamTexture.devices[currentCamIndex];
                tex = new WebCamTexture(device.name);
                display.texture = tex;
                tex.Play();
                display.rectTransform.localEulerAngles = new Vector3(0, 0, -tex.videoRotationAngle);
            }
        }
    }

    public void UpdateIntensity(float value){
        if(grainLayer != null) grainLayer.intensity.value = value;
    }

    private void StopWebCam(){
        display.texture = null;
        if(tex != null) tex.Stop();
        tex = null;
    }
}
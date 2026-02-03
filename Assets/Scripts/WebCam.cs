using UnityEngine;
using System.Collections;
using UnityEngine.Android;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class WebCam : MonoBehaviour
{
    // --- VARIABILI INTERNE ---
    private int currentCamIndex = 0;
    private WebCamTexture tex;
    private Grain grainLayer;

    // --- VARIABILI INSPECTOR ---
    [Header("Camera Setup")]
    [SerializeField] private RawImage display;

    [Header("Visual Snow Effect")]
    [SerializeField] private PostProcessVolume mainVolume;

    [Header("UI Settings")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject openSettingsButton;
    [SerializeField] private GameObject startButton;
    
    [Header("Controlli")]
    [SerializeField] private Slider intensitySlider;
    [SerializeField] private Slider sizeSlider; // <--- NUOVO SLIDER

    void Start()
    {
        // 1. Recuperiamo il Profilo Grain
        if(mainVolume != null && mainVolume.profile.TryGetSettings(out grainLayer))
        {
            // Setup Slider Intensità
            if(intensitySlider != null) {
                intensitySlider.value = grainLayer.intensity.value;
                intensitySlider.onValueChanged.AddListener(UpdateIntensity);
            }

            // Setup Slider Grandezza (NUOVO)
            if(sizeSlider != null) {
                sizeSlider.value = grainLayer.size.value;
                sizeSlider.onValueChanged.AddListener(UpdateSize);
            }
        }

        // 2. Stato Iniziale UI
        if(settingsPanel != null) settingsPanel.SetActive(false);
        if(openSettingsButton != null) openSettingsButton.SetActive(false); 
        if(startButton != null) startButton.SetActive(true);
    }

    // --- FUNZIONI UI (Pubbliche) ---

    public void OnStartPressed()
    {
        #if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera)){
                Permission.RequestUserPermission(Permission.Camera);
            }
        #endif

        if(startButton != null) startButton.SetActive(false);
        if(openSettingsButton != null) openSettingsButton.SetActive(true);
        StartStopCam_Clicked();
    }

    public void UpdateIntensity(float value)
    {
        if(grainLayer != null) grainLayer.intensity.value = value;
    }

    // NUOVA FUNZIONE PER LA GRANDEZZA
    public void UpdateSize(float value)
    {
        if(grainLayer != null) grainLayer.size.value = value;
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);       
        openSettingsButton.SetActive(false); 
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);      
        openSettingsButton.SetActive(true);  
    }

    public void SwapCam_Clicked()
    {
        if (WebCamTexture.devices.Length > 0)
        {
            currentCamIndex++;
            currentCamIndex %= WebCamTexture.devices.Length;
            if (tex != null) { StopWebCam(); StartStopCam_Clicked(); }
        }
    }

    public void StartStopCam_Clicked()
    {
        if (tex != null)
        {
            StopWebCam();
        }
        else
        {
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

    private void StopWebCam()
    {
        display.texture = null;
        if(tex != null) tex.Stop();
        tex = null;
    }
}
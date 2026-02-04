using UnityEngine;
using System.Collections;
using UnityEngine.Android;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class WebCam : MonoBehaviour
{
    private int currentCamIndex = 0;
    private WebCamTexture tex;
    
    // LAYER DEGLI EFFETTI
    private Grain grainLayer;
    private ChromaticAberration ghostingLayer; 
    private Bloom bloomLayer;
    private ColorGrading colorGradingLayer; // <--- NUOVO LAYER (CONTRASTO)

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
    [SerializeField] private Slider sizeSlider; 
    [SerializeField] private Toggle colorToggle;
    [SerializeField] private Slider flickerSlider;
    [SerializeField] private Slider trailSlider; 
    [SerializeField] private Slider haloSlider; 
    [SerializeField] private Slider contrastSlider; // <--- NUOVO SLIDER

    private float nextFlickerTime;
    private bool isFlickerOn = true;

    void Start()
    {
        // 1. CONFIGURAZIONE INIZIALE
        if(mainVolume != null)
        {
            // GRAIN
            if(mainVolume.profile.TryGetSettings(out grainLayer))
            {
                if(intensitySlider != null) intensitySlider.onValueChanged.AddListener(UpdateIntensity);
                if(sizeSlider != null) sizeSlider.onValueChanged.AddListener(UpdateSize);
                if(colorToggle != null) colorToggle.onValueChanged.AddListener(UpdateColor);
            }

            // GHOSTING (Scie)
            if(mainVolume.profile.TryGetSettings(out ghostingLayer))
            {
                if(trailSlider != null)
                {
                    trailSlider.minValue = 0f;
                    trailSlider.maxValue = 1f; 
                    trailSlider.onValueChanged.AddListener(UpdateTrail);
                }
            }

            // BLOOM (Aloni)
            if(mainVolume.profile.TryGetSettings(out bloomLayer))
            {
                if(haloSlider != null)
                {
                    haloSlider.minValue = 0f;
                    haloSlider.maxValue = 30f; 
                    haloSlider.onValueChanged.AddListener(UpdateHalo);
                }
            }

            // COLOR GRADING (Contrasto) - NUOVO
            if(mainVolume.profile.TryGetSettings(out colorGradingLayer))
            {
                if(contrastSlider != null)
                {
                    contrastSlider.minValue = -100f; // Sbiadito
                    contrastSlider.maxValue = 100f;  // Forte
                    contrastSlider.onValueChanged.AddListener(UpdateContrast);
                }
            }
        }

        // 2. CARICAMENTO PROFILO
        LoadProfile();

        // 3. UI
        if(settingsPanel != null) settingsPanel.SetActive(false);
        if(openSettingsButton != null) openSettingsButton.SetActive(false); 
        if(startButton != null) startButton.SetActive(true);
    }

    void Update()
    {
        // Gestione Sfarfallio
        if (grainLayer == null || flickerSlider == null) return;
        if (flickerSlider.value >= 59f) {
            grainLayer.intensity.value = intensitySlider.value;
        } else if (Time.time >= nextFlickerTime) {
            isFlickerOn = !isFlickerOn;
            float valoreAlto = intensitySlider.value;
            float valoreBasso = intensitySlider.value * 0.4f; // 40% della potenza (non 0)

            grainLayer.intensity.value = isFlickerOn ? valoreAlto : valoreBasso;
            
            nextFlickerTime = Time.time + (1f / flickerSlider.value);
        }
    }

    // --- FUNZIONI DI SALVATAGGIO ---

    public void SaveProfile()
    {
        PlayerPrefs.SetFloat("VS_Intensity", intensitySlider.value);
        PlayerPrefs.SetFloat("VS_Size", sizeSlider.value);
        PlayerPrefs.SetInt("VS_Color", colorToggle.isOn ? 1 : 0);
        PlayerPrefs.SetFloat("VS_Flicker", flickerSlider.value);
        PlayerPrefs.SetFloat("VS_Trail", trailSlider.value);
        PlayerPrefs.SetFloat("VS_Halo", haloSlider.value);
        PlayerPrefs.SetFloat("VS_Contrast", contrastSlider.value); // <--- SALVIAMO IL CONTRASTO

        PlayerPrefs.Save();
        Debug.Log("Profilo Salvato!");
    }

    public void LoadProfile()
    {
        if (PlayerPrefs.HasKey("VS_Intensity"))
        {
            intensitySlider.value = PlayerPrefs.GetFloat("VS_Intensity");
            sizeSlider.value = PlayerPrefs.GetFloat("VS_Size");
            colorToggle.isOn = PlayerPrefs.GetInt("VS_Color") == 1;
            flickerSlider.value = PlayerPrefs.GetFloat("VS_Flicker");
            trailSlider.value = PlayerPrefs.GetFloat("VS_Trail");
            haloSlider.value = PlayerPrefs.GetFloat("VS_Halo");
            
            // Carichiamo il contrasto (con controllo sicurezza se è vecchio salvataggio)
            if(PlayerPrefs.HasKey("VS_Contrast")) 
                contrastSlider.value = PlayerPrefs.GetFloat("VS_Contrast");
            else
                contrastSlider.value = 0f;

            Debug.Log("Profilo Caricato!");
        }
    }

    // --- AGGIORNAMENTO EFFETTI ---
    public void UpdateIntensity(float value) { /* Update gestisce questo */ }
    public void UpdateSize(float value) { if(grainLayer != null) grainLayer.size.value = value; }
    public void UpdateColor(bool isColored) { if(grainLayer != null) grainLayer.colored.value = isColored; }
    public void UpdateTrail(float value) { if(ghostingLayer != null) ghostingLayer.intensity.value = value; }
    public void UpdateHalo(float value) { if(bloomLayer != null) bloomLayer.intensity.value = value; }
    
    // NUOVA FUNZIONE CONTRASTO
    public void UpdateContrast(float value) 
    { 
        if(colorGradingLayer != null) colorGradingLayer.contrast.value = value; 
    }

    // --- SISTEMA ---
    public void OnStartPressed()
    {
        #if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
                Permission.RequestUserPermission(Permission.Camera);
        #endif
        if(startButton != null) startButton.SetActive(false);
        if(openSettingsButton != null) openSettingsButton.SetActive(true);
        StartStopCam_Clicked();
    }
    
    public void OpenSettings() { settingsPanel.SetActive(true); openSettingsButton.SetActive(false); }
    public void CloseSettings() { settingsPanel.SetActive(false); openSettingsButton.SetActive(true); }

    public void SwapCam_Clicked()
    {
        if (WebCamTexture.devices.Length > 0)
        {
            currentCamIndex = (currentCamIndex + 1) % WebCamTexture.devices.Length;
            if (tex != null) { StopWebCam(); StartStopCam_Clicked(); }
        }
    }

    public void StartStopCam_Clicked()
    {
        if (tex != null) StopWebCam();
        else if (WebCamTexture.devices.Length > 0)
        {
            tex = new WebCamTexture(WebCamTexture.devices[currentCamIndex].name);
            display.texture = tex;
            tex.Play();
            display.rectTransform.localEulerAngles = new Vector3(0, 0, -tex.videoRotationAngle);
        }
    }

    private void StopWebCam() { display.texture = null; if(tex != null) tex.Stop(); tex = null; }
}
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
    private ColorGrading colorGradingLayer;

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
    [SerializeField] private Slider contrastSlider;

    [Header("Entoptic Phenomena")]
    [SerializeField] private GameObject bfepObject; // Solo BFEP
    [SerializeField] private Toggle bfepToggle;     // Solo BFEP

    private float nextFlickerTime;
    private bool isFlickerOn = true;

    void Start()
    {
        // 1. CONFIGURAZIONE INIZIALE SLIDER & EFFETTI
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

            // COLOR GRADING (Contrasto)
            if(mainVolume.profile.TryGetSettings(out colorGradingLayer))
            {
                if(contrastSlider != null)
                {
                    contrastSlider.minValue = -100f; 
                    contrastSlider.maxValue = 100f;  
                    contrastSlider.onValueChanged.AddListener(UpdateContrast);
                }
            }
        }

        // 2. CONFIGURAZIONE BFEP (Fenomeni Entoptici)
        if(bfepToggle != null) bfepToggle.onValueChanged.AddListener(UpdateBFEP);
        
        // Stato iniziale: spento (verrà sovrascritto dal LoadProfile se c'è un salvataggio)
        if(bfepObject != null) bfepObject.SetActive(false);

        // 3. CARICAMENTO PROFILO (Spostato alla fine per attivare i listener)
        LoadProfile();

        // 4. UI INIZIALE
        if(settingsPanel != null) settingsPanel.SetActive(false);
        if(openSettingsButton != null) openSettingsButton.SetActive(false); 
        if(startButton != null) startButton.SetActive(true);
    }

    void Update()
    {
        // Gestione Sfarfallio (Pulsazione)
        if (grainLayer == null || flickerSlider == null) return;
        if (flickerSlider.value >= 59f) {
            grainLayer.intensity.value = intensitySlider.value;
        } else if (Time.time >= nextFlickerTime) {
            isFlickerOn = !isFlickerOn;
            float valoreAlto = intensitySlider.value;
            float valoreBasso = intensitySlider.value * 0.4f; 

            grainLayer.intensity.value = isFlickerOn ? valoreAlto : valoreBasso;
            
            nextFlickerTime = Time.time + (1f / flickerSlider.value);
        }
    }

    // --- FUNZIONI DI SALVATAGGIO (Aggiornate per BFEP) ---

    public void SaveProfile()
    {
        PlayerPrefs.SetFloat("VS_Intensity", intensitySlider.value);
        PlayerPrefs.SetFloat("VS_Size", sizeSlider.value);
        PlayerPrefs.SetInt("VS_Color", colorToggle.isOn ? 1 : 0);
        PlayerPrefs.SetFloat("VS_Flicker", flickerSlider.value);
        PlayerPrefs.SetFloat("VS_Trail", trailSlider.value);
        PlayerPrefs.SetFloat("VS_Halo", haloSlider.value);
        PlayerPrefs.SetFloat("VS_Contrast", contrastSlider.value);
        
        // NUOVO: Salviamo lo stato del BFEP (1 = Acceso, 0 = Spento)
        if (bfepToggle != null)
        {
            PlayerPrefs.SetInt("VS_BFEP", bfepToggle.isOn ? 1 : 0);
        }

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
            
            if(PlayerPrefs.HasKey("VS_Contrast")) 
                contrastSlider.value = PlayerPrefs.GetFloat("VS_Contrast");
            
            // NUOVO: Carichiamo lo stato del BFEP
            if (PlayerPrefs.HasKey("VS_BFEP") && bfepToggle != null)
            {
                // Impostando .isOn, scatterà in automatico il listener che accende l'oggetto
                bfepToggle.isOn = PlayerPrefs.GetInt("VS_BFEP") == 1;
            }

            Debug.Log("Profilo Caricato!");
        }
    }

    // --- AGGIORNAMENTO EFFETTI ---
    public void UpdateIntensity(float value) { /* Gestito in Update */ }
    public void UpdateSize(float value) { if(grainLayer != null) grainLayer.size.value = value; }
    public void UpdateColor(bool isColored) { if(grainLayer != null) grainLayer.colored.value = isColored; }
    public void UpdateTrail(float value) { if(ghostingLayer != null) ghostingLayer.intensity.value = value; }
    public void UpdateHalo(float value) { if(bloomLayer != null) bloomLayer.intensity.value = value; }
    public void UpdateContrast(float value) { if(colorGradingLayer != null) colorGradingLayer.contrast.value = value; }

    // Funzione BFEP (Floaters rimossi)
    public void UpdateBFEP(bool isActive) { if(bfepObject != null) bfepObject.SetActive(isActive); }

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
using UnityEngine;
using System.Collections;
using UnityEngine.Android;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UIElements; // Namespace fondamentale per UI Toolkit

public class WebCam : MonoBehaviour
{
    // --- COMPONENTI UI TOOLKIT ---
    [Header("UI Toolkit Setup")]
    [SerializeField] private UIDocument uiDocument; 

    // Elementi UI
    private VisualElement root;
    private VisualElement settingsPanel;
    private Button openSettingsBtn;
    private Button switchCamBtn;
    private Button closeSettingsBtn;
    private Button saveBtn;
    
    // Controlli
    private Slider intensitySlider;
    private Slider sizeSlider;
    private Slider flickerSlider;
    private Slider trailSlider;
    private Slider haloSlider;
    private Slider contrastSlider;
    private Toggle colorToggle;
    private Toggle bfepToggle;

    // --- LOGICA FOTOCAMERA E EFFETTI ---
    private int currentCamIndex = 0;
    private WebCamTexture tex;
    
    private Grain grainLayer;
    private ChromaticAberration ghostingLayer; 
    private Bloom bloomLayer;
    private ColorGrading colorGradingLayer;

    [Header("Camera Output")]
    [SerializeField] private UnityEngine.UI.RawImage display; 

    [Header("Visual Snow Logic")]
    [SerializeField] private PostProcessVolume mainVolume;
    [SerializeField] private GameObject bfepObject;

    private float nextFlickerTime;
    private bool isFlickerOn = true;

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // 1. TROVA GLI ELEMENTI PER NOME
        // NOTA: Assicurati che i nomi qui sotto siano IDENTICI a quelli nel file UXML (Name)
        settingsPanel = root.Q<VisualElement>("settings"); // Controlla se hai chiamato il pannello "settings" o "settings-panel"
        openSettingsBtn = root.Q<Button>("openSettings");
        switchCamBtn = root.Q<Button>("switchCamera");
        closeSettingsBtn = root.Q<Button>("closeSettings");
        saveBtn = root.Q<Button>("save");

        intensitySlider = root.Q<Slider>("intensity");
        sizeSlider = root.Q<Slider>("size");
        flickerSlider = root.Q<Slider>("flicker");
        trailSlider = root.Q<Slider>("trail");
        haloSlider = root.Q<Slider>("halo");
        contrastSlider = root.Q<Slider>("contrast");

        colorToggle = root.Q<Toggle>("color");
        bfepToggle = root.Q<Toggle>("bfep");

        // 2. COLLEGA GLI EVENTI
        if(openSettingsBtn != null) openSettingsBtn.clicked += () => SetSettingsVisible(true);
        if(closeSettingsBtn != null) closeSettingsBtn.clicked += () => SetSettingsVisible(false);
        if(saveBtn != null) saveBtn.clicked += SaveProfile;
        if(switchCamBtn != null) switchCamBtn.clicked += SwapCam_Clicked;

        // Eventi cambio valore
        if(intensitySlider != null) intensitySlider.RegisterValueChangedCallback(evt => { /* Aggiornato in Update */ });
        if(sizeSlider != null) sizeSlider.RegisterValueChangedCallback(evt => UpdateSize(evt.newValue));
        if(flickerSlider != null) flickerSlider.RegisterValueChangedCallback(evt => { /* Aggiornato in Update */ });
        if(trailSlider != null) trailSlider.RegisterValueChangedCallback(evt => UpdateTrail(evt.newValue));
        if(haloSlider != null) haloSlider.RegisterValueChangedCallback(evt => UpdateHalo(evt.newValue));
        if(contrastSlider != null) contrastSlider.RegisterValueChangedCallback(evt => UpdateContrast(evt.newValue));
        
        if(colorToggle != null) colorToggle.RegisterValueChangedCallback(evt => UpdateColor(evt.newValue));
        if(bfepToggle != null) bfepToggle.RegisterValueChangedCallback(evt => UpdateBFEP(evt.newValue));

        // 3. FOCUS MODE
        RegisterFocusMode(intensitySlider);
        RegisterFocusMode(sizeSlider);
        RegisterFocusMode(flickerSlider);
        RegisterFocusMode(trailSlider);
        RegisterFocusMode(haloSlider);
        RegisterFocusMode(contrastSlider);

        // Stato iniziale UI
        SetSettingsVisible(false);
    }

    void Start()
    {
        if(mainVolume != null)
        {
            mainVolume.profile.TryGetSettings(out grainLayer);
            mainVolume.profile.TryGetSettings(out ghostingLayer);
            mainVolume.profile.TryGetSettings(out bloomLayer);
            mainVolume.profile.TryGetSettings(out colorGradingLayer);
        }

        if(trailSlider != null) { trailSlider.lowValue = 0f; trailSlider.highValue = 1f; }
        if(haloSlider != null) { haloSlider.lowValue = 0f; haloSlider.highValue = 30f; }
        if(contrastSlider != null) { contrastSlider.lowValue = -100f; contrastSlider.highValue = 100f; }
        if(flickerSlider != null) { flickerSlider.lowValue = 1f; flickerSlider.highValue = 60f; }

        // Stato di default prima di caricare il profilo salvato
        if(bfepObject != null) bfepObject.SetActive(false);

        LoadProfile(); // sovrascrive il default se l'utente aveva salvato
        StartCoroutine(InitCameraRoutine());
    }

    void Update()
    {
        if (grainLayer == null || flickerSlider == null || intensitySlider == null) return;
        
        float intensityVal = intensitySlider.value;
        float flickerVal = flickerSlider.value;

        if (flickerVal >= 59f) {
            grainLayer.intensity.value = intensityVal;
        } else if (Time.time >= nextFlickerTime) {
            isFlickerOn = !isFlickerOn;
            float valBasso = intensityVal * 0.4f; 
            grainLayer.intensity.value = isFlickerOn ? intensityVal : valBasso;
            nextFlickerTime = Time.time + (1f / flickerVal);
        }
    }

    // --- FOCUS MODE LOGIC CORRETTA ---
    private void RegisterFocusMode(Slider slider)
    {
        if (slider == null || settingsPanel == null) return;

        slider.RegisterCallback<PointerDownEvent>(evt => {
            // Rende lo sfondo trasparente
            settingsPanel.style.backgroundColor = new StyleColor(Color.clear);
            
            // CORREZIONE ERRORE: Impostiamo i 4 bordi a 0 singolarmente
            settingsPanel.style.borderTopWidth = 0;
            settingsPanel.style.borderBottomWidth = 0;
            settingsPanel.style.borderLeftWidth = 0;
            settingsPanel.style.borderRightWidth = 0;
            
            // Nasconde gli altri elementi
            foreach(var child in settingsPanel.Children())
            {
                if(child != slider) child.style.opacity = 0; 
            }
        });

        slider.RegisterCallback<PointerUpEvent>(evt => {
            // Ripristina sfondo scuro
            settingsPanel.style.backgroundColor = new StyleColor(new Color(0.11f, 0.11f, 0.12f, 0.95f)); 
            
            // CORREZIONE ERRORE: Ripristiniamo i bordi a 1
            settingsPanel.style.borderTopWidth = 1;
            settingsPanel.style.borderBottomWidth = 1;
            settingsPanel.style.borderLeftWidth = 1;
            settingsPanel.style.borderRightWidth = 1;

            // Ripristina visibilità
            foreach(var child in settingsPanel.Children())
            {
                child.style.opacity = 1; 
            }
        });
    }

    private void SetSettingsVisible(bool visible)
    {
        if(settingsPanel == null) return;
        settingsPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        
        if(openSettingsBtn != null) 
            openSettingsBtn.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
        
        if(switchCamBtn != null) 
            switchCamBtn.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
    }

    void UpdateSize(float value) { if(grainLayer != null) grainLayer.size.value = value; }
    void UpdateColor(bool val) { if(grainLayer != null) grainLayer.colored.value = val; }
    void UpdateTrail(float val) { if(ghostingLayer != null) ghostingLayer.intensity.value = val; }
    void UpdateHalo(float val) { if(bloomLayer != null) bloomLayer.intensity.value = val; }
    void UpdateContrast(float val) { if(colorGradingLayer != null) colorGradingLayer.contrast.value = val; }
    void UpdateBFEP(bool val) { if(bfepObject != null) bfepObject.SetActive(val); }

    private IEnumerator InitCameraRoutine()
    {
        #if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
            yield return new WaitUntil(() => Permission.HasUserAuthorizedPermission(Permission.Camera));
        }
        #endif
        StartStopCam_Clicked();
    }

    public void SwapCam_Clicked()
    {
        if (WebCamTexture.devices.Length > 0)
        {
            currentCamIndex = (currentCamIndex + 1) % WebCamTexture.devices.Length;
            if (tex != null) { tex.Stop(); StartStopCam_Clicked(); }
        }
    }

    public void StartStopCam_Clicked()
    {
        if (WebCamTexture.devices.Length > 0)
        {
            tex = new WebCamTexture(WebCamTexture.devices[currentCamIndex].name);
            display.texture = tex;
            tex.Play();
            display.rectTransform.localEulerAngles = new Vector3(0, 0, -tex.videoRotationAngle);
        }
    }

    public void SaveProfile()
    {
        if(intensitySlider != null) PlayerPrefs.SetFloat("VS_Intensity", intensitySlider.value);
        if(sizeSlider != null) PlayerPrefs.SetFloat("VS_Size", sizeSlider.value);
        if(flickerSlider != null) PlayerPrefs.SetFloat("VS_Flicker", flickerSlider.value);
        if(trailSlider != null) PlayerPrefs.SetFloat("VS_Trail", trailSlider.value);
        if(haloSlider != null) PlayerPrefs.SetFloat("VS_Halo", haloSlider.value);
        if(contrastSlider != null) PlayerPrefs.SetFloat("VS_Contrast", contrastSlider.value);
        if(colorToggle != null) PlayerPrefs.SetInt("VS_Color", colorToggle.value ? 1 : 0);
        if(bfepToggle != null) PlayerPrefs.SetInt("VS_BFEP", bfepToggle.value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void LoadProfile()
    {
        if (PlayerPrefs.HasKey("VS_Intensity"))
        {
            if(intensitySlider != null) intensitySlider.value = PlayerPrefs.GetFloat("VS_Intensity");
            if(sizeSlider != null) sizeSlider.value = PlayerPrefs.GetFloat("VS_Size");
            if(flickerSlider != null) flickerSlider.value = PlayerPrefs.GetFloat("VS_Flicker");
            if(trailSlider != null) trailSlider.value = PlayerPrefs.GetFloat("VS_Trail");
            if(haloSlider != null) haloSlider.value = PlayerPrefs.GetFloat("VS_Halo");
            if(contrastSlider != null) contrastSlider.value = PlayerPrefs.GetFloat("VS_Contrast");
            if(colorToggle != null) colorToggle.value = PlayerPrefs.GetInt("VS_Color") == 1;
            if(bfepToggle != null) bfepToggle.value = PlayerPrefs.GetInt("VS_BFEP") == 1;
        }
    }
}
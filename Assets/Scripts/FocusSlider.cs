using UnityEngine;
using UnityEngine.EventSystems;

public class FocusSlider : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Transform originalParent;
    private int originalIndex;
    private GameObject panelObject;

    public void OnPointerDown(PointerEventData eventData)
    {
        // 1. Memorizziamo chi è il papà (SettingsPanel) e in che posizione siamo
        originalParent = transform.parent;
        originalIndex = transform.GetSiblingIndex();
        
        // Se per qualche motivo non abbiamo un genitore, ci fermiamo
        if (originalParent == null) return;

        panelObject = originalParent.gameObject;

        // 2. TRUCCO: Ci spostiamo "fuori" dal pannello.
        // Diventiamo figli del Canvas (o del nonno), così non dipendiamo più dal pannello.
        // 'true' serve a mantenere la posizione esatta dove si trova il dito.
        transform.SetParent(originalParent.parent, true);

        // 3. Ora possiamo spegnere brutalmente tutto il pannello vecchio.
        // Essendo noi "usciti", non verremo spenti.
        panelObject.SetActive(false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (originalParent != null && panelObject != null)
        {
            // 1. Riaccendiamo il pannello (con tutti gli altri slider)
            panelObject.SetActive(true);

            // 2. Torniamo a casa (dentro il pannello)
            transform.SetParent(originalParent, true);

            // 3. IMPORTANTE: Ci rimettiamo nella posizione originale (ordine corretto)
            // Altrimenti finiremmo in fondo alla lista.
            transform.SetSiblingIndex(originalIndex);
        }
    }
    
    // Sicurezza: Se l'app viene chiusa o disattivata mentre premiamo, ripristina tutto
    void OnDisable()
    {
        if (originalParent != null && panelObject != null)
        {
            panelObject.SetActive(true);
            if(transform.parent != originalParent)
            {
                transform.SetParent(originalParent, true);
                transform.SetSiblingIndex(originalIndex);
            }
        }
    }
}
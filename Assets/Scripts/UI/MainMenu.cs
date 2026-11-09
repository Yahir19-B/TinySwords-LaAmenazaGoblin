using TMPro;
using UnityEngine;

// Menú principal: abre la pantalla de niveles directamente si se regresa desde un nivel.
public class MainMenu : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject levelSelectPanel;
    public GameObject controlsPanel;
    public GameObject shopPanel;
    public TMP_Text goldText;

    void Start()
    {
        Time.timeScale = 1f;
        bool openLevels = Progress.OpenLevelSelect;
        Progress.OpenLevelSelect = false;
        // El panel principal siempre queda activo: las demás ventanas se abren encima de él,
        // así al cerrarlas vuelven a verse los botones.
        if (mainPanel != null) mainPanel.SetActive(true);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(openLevels);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    void Update()
    {
        if (goldText != null) goldText.text = Progress.TotalGold.ToString();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
            if (shopPanel != null) shopPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);
        }
    }
}

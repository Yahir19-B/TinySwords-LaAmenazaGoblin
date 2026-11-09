using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Botón de un nivel en el menú de niveles: bloqueado (candado), disponible o completado.
[RequireComponent(typeof(Button))]
public class LevelButton : MonoBehaviour
{
    public int level = 1;
    public TMP_Text label;
    public GameObject lockIcon;
    public GameObject completedMark;
    public Color completedColor = new Color(1f, 0.85f, 0.35f);

    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(() => GameManager.LoadLevel(level));
    }

    void OnEnable() => Refresh();

    public void Refresh()
    {
        if (button == null) button = GetComponent<Button>();
        bool unlocked = Progress.IsUnlocked(level);
        bool completed = Progress.IsCompleted(level);
        button.interactable = unlocked;
        if (lockIcon != null) lockIcon.SetActive(!unlocked);
        if (label != null)
        {
            label.gameObject.SetActive(unlocked);
            label.color = completed ? completedColor : Color.white;
        }
        if (completedMark != null) completedMark.SetActive(completed);
    }
}

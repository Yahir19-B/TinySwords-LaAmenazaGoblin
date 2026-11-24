using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Sonido de un botón al pasar el mouse y al hacer clic. Lo agrega el AudioManager a todos los botones.
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler
{
    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(() => AudioManager.Play("boton"));
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (button != null && button.interactable) AudioManager.Play("boton_encima");
    }
}

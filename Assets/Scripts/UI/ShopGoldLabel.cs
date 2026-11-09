using TMPro;
using UnityEngine;

// Muestra el oro guardado dentro de la tienda.
public class ShopGoldLabel : MonoBehaviour
{
    public TMP_Text text;

    void Update()
    {
        if (text != null) text.text = Progress.TotalGold.ToString();
    }
}

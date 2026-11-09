using UnityEngine;

// Cambia el cursor del mouse por la flecha de Tiny Swords (UI/Pointers).
public class GameCursor : MonoBehaviour
{
    public Texture2D cursorTexture;
    public Vector2 hotspot = new Vector2(20, 14);

    void Start()
    {
        if (cursorTexture != null) Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;

// Accesos rápidos en el menú "Tiny Swords" para abrir cada nivel y manejar el progreso guardado.
public static class TinySwordsLevelMenu
{
    const string Folder = "Assets/Scenes/Niveles/";

    static void Open(string path)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
    }

    [MenuItem("Tiny Swords/Abrir escena/Menú principal", priority = 100)] static void Menu() => Open("Assets/Scenes/MainMenu.unity");
    [MenuItem("Tiny Swords/Abrir escena/Nivel 01", priority = 111)] static void L1() => Open(Folder + "Nivel_01.unity");
    [MenuItem("Tiny Swords/Abrir escena/Nivel 02", priority = 112)] static void L2() => Open(Folder + "Nivel_02.unity");
    [MenuItem("Tiny Swords/Abrir escena/Nivel 03", priority = 113)] static void L3() => Open(Folder + "Nivel_03.unity");
    [MenuItem("Tiny Swords/Abrir escena/Nivel 04", priority = 114)] static void L4() => Open(Folder + "Nivel_04.unity");
    [MenuItem("Tiny Swords/Abrir escena/Nivel 05", priority = 115)] static void L5() => Open(Folder + "Nivel_05.unity");
    [MenuItem("Tiny Swords/Abrir escena/Nivel 06", priority = 116)] static void L6() => Open(Folder + "Nivel_06.unity");
    [MenuItem("Tiny Swords/Abrir escena/Nivel 07", priority = 117)] static void L7() => Open(Folder + "Nivel_07.unity");
    [MenuItem("Tiny Swords/Abrir escena/Nivel 08", priority = 118)] static void L8() => Open(Folder + "Nivel_08.unity");
    [MenuItem("Tiny Swords/Abrir escena/Nivel 09", priority = 119)] static void L9() => Open(Folder + "Nivel_09.unity");
    [MenuItem("Tiny Swords/Abrir escena/Nivel 10 (Jefe)", priority = 120)] static void L10() => Open(Folder + "Nivel_10.unity");

    // Para probar: desbloquea los 10 niveles en el menú de niveles.
    [MenuItem("Tiny Swords/Progreso/Desbloquear todos los niveles", priority = 200)]
    static void UnlockAll()
    {
        for (int i = 1; i < Progress.LevelCount; i++) Progress.CompleteLevel(i, 0);
        EditorUtility.DisplayDialog("Tiny Swords", "Los 10 niveles están desbloqueados.", "OK");
    }

    [MenuItem("Tiny Swords/Progreso/Reiniciar progreso (solo nivel 1)", priority = 201)]
    static void ResetAll()
    {
        Progress.Reset();
        EditorUtility.DisplayDialog("Tiny Swords", "Progreso reiniciado: solo el nivel 1 está desbloqueado.", "OK");
    }
}

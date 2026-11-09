using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Paused,
    Victory,
    Defeat
}

// Estado general de la partida: pausa, victoria, derrota y cambio de escenas.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public string mainMenuScene = "MainMenu";
    public KeyCode pauseKey = KeyCode.Escape;

    public GameState State { get; private set; } = GameState.Playing;

    public event Action<GameState> OnStateChanged;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        // Las unidades no chocan entre sí (evita atascos), pero sí con paredes y obstáculos.
        int units = LayerMask.NameToLayer("Units");
        if (units >= 0) Physics2D.IgnoreLayerCollision(units, units, true);
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey)) TogglePause();
    }

    public void TogglePause()
    {
        if (State == GameState.Playing) SetState(GameState.Paused);
        else if (State == GameState.Paused) SetState(GameState.Playing);
    }

    public void Victory()
    {
        if (State == GameState.Playing) SetState(GameState.Victory);
    }

    public void Defeat()
    {
        if (State == GameState.Playing) SetState(GameState.Defeat);
    }

    void SetState(GameState s)
    {
        State = s;
        Time.timeScale = s == GameState.Playing ? 1f : 0f;
        OnStateChanged?.Invoke(s);
    }

    public void Restart() => LoadScene(SceneManager.GetActiveScene().name);

    public void LoadMainMenu()
    {
        Progress.OpenLevelSelect = false;
        LoadScene(mainMenuScene);
    }

    public void LoadLevelSelect()
    {
        Progress.OpenLevelSelect = true;
        LoadScene(mainMenuScene);
    }

    public void LoadNextLevel()
    {
        var lm = LevelManager.Instance;
        if (lm == null || lm.IsLastLevel) LoadLevelSelect();
        else LoadLevel(lm.levelNumber + 1);
    }

    public static void LoadLevel(int level) => LoadScene(Progress.SceneName(level));

    static void LoadScene(string scene)
    {
        Time.timeScale = 1f;
        Debug.Log("Tiny Swords: cargando escena " + scene);
        SceneManager.LoadScene(scene);
    }

    public void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}

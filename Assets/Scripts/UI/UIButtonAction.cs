using UnityEngine;
using UnityEngine.UI;

// Pon este componente en un botón para darle una acción común sin programar nada.
[RequireComponent(typeof(Button))]
public class UIButtonAction : MonoBehaviour
{
    // Los valores nuevos van al final para no cambiar los botones ya configurados.
    public enum Action
    {
        Play,           // abre el nivel "level"
        Resume,
        Restart,
        MainMenu,
        Quit,
        TrainPawn,
        OpenPanel,      // muestra "target"
        ClosePanel,     // oculta "target"
        LevelSelect,
        NextLevel,
        ShowControls,
        CloseControls,
        ResetProgress
    }

    public Action action;
    [Tooltip("Para Play: número de nivel.")]
    public int level = 1;
    [Tooltip("Para OpenPanel / ClosePanel.")]
    public GameObject target;

    void Awake() => GetComponent<Button>().onClick.AddListener(Run);

    void Run()
    {
        var gm = GameManager.Instance;
        switch (action)
        {
            case Action.Play: GameManager.LoadLevel(level); break;
            case Action.Resume: gm?.TogglePause(); break;
            case Action.Restart: gm?.Restart(); break;
            case Action.MainMenu: gm?.LoadMainMenu(); break;
            case Action.Quit:
                if (gm != null) gm.Quit();
                else Application.Quit();
                break;
            case Action.TrainPawn: PawnManager.Instance?.TryTrain(); break;
            case Action.OpenPanel: if (target != null) target.SetActive(true); break;
            case Action.ClosePanel: if (target != null) target.SetActive(false); break;
            case Action.LevelSelect: gm?.LoadLevelSelect(); break;
            case Action.NextLevel: gm?.LoadNextLevel(); break;
            case Action.ShowControls: LevelHUD.Instance?.ShowControls(true); break;
            case Action.CloseControls: LevelHUD.Instance?.ShowControls(false); break;
            case Action.ResetProgress:
                Progress.Reset();
                foreach (var b in FindObjectsByType<LevelButton>(FindObjectsInactive.Include)) b.Refresh();
                break;
        }
    }
}

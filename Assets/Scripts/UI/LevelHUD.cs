using System.Collections;
using TMPro;
using UnityEngine;

// Interfaz durante un nivel: vida del héroe, oro, nivel y objetivo, barra del jefe,
// mensajes y paneles de pausa, controles, victoria y derrota.
public class LevelHUD : MonoBehaviour
{
    public static LevelHUD Instance { get; private set; }

    [Header("Héroe")]
    public RectTransform healthFill;
    public TMP_Text healthText;
    public TMP_Text goldText;

    [Header("Nivel")]
    public TMP_Text levelText;
    public TMP_Text objectiveText;
    public TMP_Text messageText;

    [Header("Jefe")]
    public GameObject bossPanel;
    public RectTransform bossFill;
    public TMP_Text bossNameText;

    [Header("Paneles")]
    public GameObject pausePanel;
    public GameObject controlsPanel;
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public TMP_Text victoryTitle;
    public TMP_Text victoryText;
    public GameObject nextLevelButton;
    public KeyCode controlsKey = KeyCode.H;

    Coroutine messageRoutine;

    void Awake() => Instance = this;

    void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += OnStateChanged;
        OnStateChanged(GameState.Playing);
        if (messageText != null) messageText.text = "";
        if (bossPanel != null) bossPanel.SetActive(false);

        var lm = LevelManager.Instance;
        if (lm != null)
        {
            SetText(levelText, $"Nivel {lm.levelNumber}: {lm.levelTitle}");
            StartCoroutine(IntroMessages(lm));
            if (lm.showControlsOnStart) ShowControls(true);
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    void Update()
    {
        var player = Player.Instance;
        if (player != null && player.Health != null)
        {
            SetFill(healthFill, player.Health.Normalized);
            SetText(healthText, $"{Mathf.CeilToInt(player.Health.Current)}/{Mathf.CeilToInt(player.Health.maxHealth)}");
        }

        var lm = LevelManager.Instance;
        if (lm != null)
        {
            SetText(goldText, lm.GoldCollected.ToString());
            string obj = $"Goblins: {lm.GoblinsLeft}";
            if (lm.HousesLeft > 0) obj += $"   Casas: {lm.HousesLeft}";
            SetText(objectiveText, obj);

            var boss = lm.Boss;
            bool showBoss = boss != null && boss.IsAwake && !boss.Health.IsDead;
            if (bossPanel != null && bossPanel.activeSelf != showBoss)
            {
                bossPanel.SetActive(showBoss);
                if (showBoss) ShowMessage($"¡{boss.bossName} despertó!", 2.5f);
            }
            if (showBoss)
            {
                SetFill(bossFill, boss.Health.Normalized);
                SetText(bossNameText, boss.Enraged ? boss.bossName + " (FURIOSO)" : boss.bossName);
            }
        }

        if (Input.GetKeyDown(controlsKey)) ShowControls(controlsPanel == null || !controlsPanel.activeSelf);
    }

    // Los controles pausan el juego mientras están abiertos.
    public void ShowControls(bool show)
    {
        if (controlsPanel == null) return;
        controlsPanel.SetActive(show);
        var gm = GameManager.Instance;
        bool playing = gm == null || gm.State == GameState.Playing;
        Time.timeScale = show || !playing ? 0f : 1f;
    }

    void OnStateChanged(GameState state)
    {
        if (pausePanel != null) pausePanel.SetActive(state == GameState.Paused);
        if (victoryPanel != null) victoryPanel.SetActive(state == GameState.Victory);
        if (defeatPanel != null) defeatPanel.SetActive(state == GameState.Defeat);
        if (state != GameState.Paused && controlsPanel != null && controlsPanel.activeSelf) ShowControls(false);

        var lm = LevelManager.Instance;
        if (state == GameState.Victory && lm != null)
        {
            SetText(victoryTitle, lm.IsLastLevel ? "¡Derrotaste al Jefe!" : "¡Nivel completado!");
            SetText(victoryText, lm.IsLastLevel
                ? $"Salvaste el reino de los goblins.\nOro ganado: {lm.GoldCollected} (bono +{lm.completionBonus})"
                : $"Oro ganado: {lm.GoldCollected} (bono +{lm.completionBonus})   Mejor combo: x{lm.BestCombo}\nSe desbloqueó el nivel {lm.levelNumber + 1}");
            if (nextLevelButton != null) nextLevelButton.SetActive(!lm.IsLastLevel);
        }
    }

    IEnumerator IntroMessages(LevelManager lm)
    {
        ShowMessage(lm.objective, 3.5f);
        if (string.IsNullOrEmpty(lm.tip)) yield break;
        yield return new WaitForSecondsRealtime(3.8f);
        ShowMessage(lm.tip, 4f);
    }

    public void ShowMessage(string msg, float seconds = 3f)
    {
        if (messageText == null) return;
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(msg, seconds));
    }

    IEnumerator MessageRoutine(string msg, float seconds)
    {
        messageText.text = msg;
        yield return new WaitForSecondsRealtime(seconds);
        messageText.text = "";
    }

    static void SetText(TMP_Text t, string s)
    {
        if (t != null && t.text != s) t.text = s;
    }

    // Las barras se rellenan moviendo el ancla derecha (0 = vacía, 1 = llena).
    static void SetFill(RectTransform fill, float value)
    {
        if (fill == null) return;
        fill.anchorMax = new Vector2(Mathf.Clamp01(value), fill.anchorMax.y);
    }
}

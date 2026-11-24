using System.Collections;
using UnityEngine;

// Reglas de cada nivel: se gana al derrotar a todos los goblins y destruir sus casas.
// Se pierde si el héroe muere. Matar goblins seguidos hace combos que dan oro extra.
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public int levelNumber = 1;
    public string levelTitle = "El bosque";
    public string objective = "Derrota a todos los goblins";
    [Tooltip("Aviso de lo nuevo en este nivel (enemigo o mecánica).")]
    public string tip;
    [Tooltip("Muestra los controles al empezar (útil en el nivel 1).")]
    public bool showControlsOnStart;
    [Tooltip("Oro extra al completar el nivel.")]
    public int completionBonus = 20;

    [Header("Combos")]
    public float comboWindow = 3f;
    public int comboMinForBonus = 3;
    [Tooltip("Oro máximo que da cada golpe de combo.")]
    public int maxComboBonus = 10;

    public int GoblinsLeft { get; private set; }
    public int HousesLeft { get; private set; }
    public int GoldCollected { get; private set; }
    public int Combo { get; private set; }
    public int BestCombo { get; private set; }
    public GoblinBoss Boss { get; set; }
    public bool Finished { get; private set; }
    public bool IsLastLevel => levelNumber >= Progress.LevelCount;

    float checkTimer;
    float comboTimer;

    void Awake() => Instance = this;

    void OnEnable() => Health.OnAnyDied += OnSomethingDied;
    void OnDisable() => Health.OnAnyDied -= OnSomethingDied;

    void Update()
    {
        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0) Combo = 0;
        }

        if (Finished) return;
        checkTimer -= Time.deltaTime;
        if (checkTimer > 0) return;
        checkTimer = 0.25f;

        int goblins = 0, houses = 0;
        foreach (var h in Health.All)
        {
            if (h.IsDead || h.team != Team.Goblins) continue;
            if (h.isStructure) houses++;
            else goblins++;
        }
        GoblinsLeft = goblins;
        HousesLeft = houses;

        if (goblins == 0 && houses == 0 && Time.timeSinceLevelLoad > 1f)
        {
            // Si quedan emboscadas sin activar, saltan ahora sobre el héroe.
            if (AmbushZone.Pending.Count > 0) AmbushZone.Pending[0].Trigger();
            else StartCoroutine(Win());
        }
    }

    void OnSomethingDied(Health h)
    {
        if (h.team != Team.Goblins || h.isStructure || Finished) return;
        Combo++;
        BestCombo = Mathf.Max(BestCombo, Combo);
        comboTimer = comboWindow;
        if (Combo >= comboMinForBonus)
        {
            AudioManager.Play("combo");
            int bonus = Mathf.Min(Combo, maxComboBonus);
            GoldCollected += bonus;
            var player = Player.Instance;
            if (player != null)
                DamagePopup.ShowText(player.transform.position + Vector3.up * 1.8f, $"Combo x{Combo}  +{bonus}", new Color(1f, 0.8f, 0.2f), 5.5f);
        }
    }

    public void AddGold(int amount) => GoldCollected += amount;

    IEnumerator Win()
    {
        Finished = true;
        GoldCollected += completionBonus;
        Progress.CompleteLevel(levelNumber, GoldCollected);
        yield return new WaitForSeconds(1.2f);
        GameManager.Instance?.Victory();
    }
}

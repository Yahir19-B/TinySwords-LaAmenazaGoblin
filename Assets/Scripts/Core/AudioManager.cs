using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Todo el audio del juego. Se crea solo al iniciar y vive entre escenas (no hay que ponerlo en ningún nivel).
// - Efectos: AudioManager.Play("espada", posición). Si hay espada_1, espada_2... elige una al azar.
// - Música: se escoge según la escena (menú, tienda, niveles, jefe) y cambia con un fundido.
// Los sonidos están en Resources/Audio/{Musica,Efectos} y los genera Tools/audio/generar_audio.py.
// Para usar otra música basta con reemplazar el archivo dejando el mismo nombre. M: silenciar.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    const int Voices = 20;
    const float FadeTime = 1.2f;
    const float NearDistance = 7f, FarDistance = 15f;   // más lejos de la cámara ya no se oye

    // Veces que sonó cada cosa y sonidos que no se encontraron (lo usa la prueba automática).
    public static readonly Dictionary<string, int> Played = new Dictionary<string, int>();
    public static readonly HashSet<string> Missing = new HashSet<string>();

    public string CurrentMusic { get; private set; }

    readonly Dictionary<string, List<AudioClip>> effects = new Dictionary<string, List<AudioClip>>();
    readonly Dictionary<string, AudioClip> musicClips = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    AudioSource[] voices;
    AudioSource music, fadingOut, jingle;
    float musicFade;
    MainMenu menu;
    GameManager hooked;
    bool jingleMode;
    float musicVolume, effectsVolume;
    bool muted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        Played.Clear();
        Missing.Clear();
        if (Instance != null) return;
        var go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        go.AddComponent<AudioManager>();
    }

    // ---------- Volumen (se guarda) ----------

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat("VolMusica", 0.6f);
        set { PlayerPrefs.SetFloat("VolMusica", Mathf.Clamp01(value)); Instance?.ReadVolume(); }
    }

    public static float EffectsVolume
    {
        get => PlayerPrefs.GetFloat("VolEfectos", 0.8f);
        set { PlayerPrefs.SetFloat("VolEfectos", Mathf.Clamp01(value)); Instance?.ReadVolume(); }
    }

    public static bool Muted
    {
        get => PlayerPrefs.GetInt("Silencio", 0) == 1;
        set { PlayerPrefs.SetInt("Silencio", value ? 1 : 0); Instance?.ReadVolume(); }
    }

    void ReadVolume()
    {
        musicVolume = MusicVolume;
        effectsVolume = EffectsVolume;
        muted = Muted;
        AudioListener.volume = muted ? 0f : 1f;
    }

    // ---------- Efectos ----------

    // Sonido de interfaz o del héroe (siempre al mismo volumen).
    public static void Play(string name, float volume = 1f) => Instance?.PlayInternal(name, volume, 0.04f);

    // Sonido en un lugar del mapa: baja con la distancia a la cámara.
    public static void Play(string name, Vector3 position, float volume = 1f)
    {
        if (Instance == null) return;
        var cam = Camera.main;
        if (cam != null)
        {
            float d = Vector2.Distance(cam.transform.position, position);
            volume *= 1f - Mathf.InverseLerp(NearDistance, FarDistance, d);
        }
        Instance.PlayInternal(name, volume, 0.06f);
    }

    void PlayInternal(string name, float volume, float minInterval)
    {
        if (volume < 0.02f) return;
        if (!effects.TryGetValue(name, out var clips))
        {
            if (Missing.Add(name)) Debug.LogWarning("Tiny Swords: no existe el sonido " + name);
            return;
        }
        // El mismo sonido no se encima (por ejemplo, un torbellino que golpea a 6 goblins a la vez).
        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(name, out float last) && now - last < minInterval) return;
        lastPlayed[name] = now;

        var voice = FreeVoice();
        voice.clip = clips[Random.Range(0, clips.Count)];
        voice.volume = volume * effectsVolume;
        voice.pitch = Random.Range(0.94f, 1.06f);
        voice.Play();
        Played.TryGetValue(name, out int n);
        Played[name] = n + 1;
    }

    AudioSource FreeVoice()
    {
        AudioSource best = voices[0];
        float mostAdvanced = -1f;
        foreach (var v in voices)
        {
            if (!v.isPlaying) return v;
            float progress = v.clip != null ? v.time / v.clip.length : 1f;
            if (progress > mostAdvanced) { mostAdvanced = progress; best = v; }
        }
        return best;
    }

    // ---------- Música ----------

    string DesiredMusic()
    {
        if (jingleMode) return null;
        var lm = LevelManager.Instance;
        if (lm != null)
        {
            if (lm.Finished) return null;
            if (lm.levelNumber <= 3) return "musica_inicio";
            if (lm.levelNumber <= 6) return "musica_medio";
            if (!lm.IsLastLevel) return "musica_final";
            // Cuando el jefe despierta su música sigue hasta el final (aunque ya haya muerto).
            bool bossFight = CurrentMusic == "musica_jefe" || (lm.Boss != null && lm.Boss.IsAwake);
            return bossFight ? "musica_jefe" : "musica_final";
        }
        if (menu != null) return menu.shopPanel != null && menu.shopPanel.activeInHierarchy ? "musica_tienda" : "musica_menu";
        return null;
    }

    AudioClip MusicClip(string name)
    {
        if (!musicClips.TryGetValue(name, out var clip))
        {
            clip = Resources.Load<AudioClip>("Audio/Musica/" + name);
            musicClips[name] = clip;
            if (clip == null && Missing.Add(name)) Debug.LogWarning("Tiny Swords: no existe la música " + name);
        }
        return clip;
    }

    void SwitchMusic(string name)
    {
        CurrentMusic = name;
        // La pista que sonaba se desvanece mientras entra la nueva.
        (music, fadingOut) = (fadingOut, music);
        musicFade = 0f;
        music.Stop();
        music.clip = name != null ? MusicClip(name) : null;
        if (music.clip == null) return;
        music.volume = 0f;
        music.Play();
        Played.TryGetValue(name, out int n);
        Played[name] = n + 1;
    }

    void PlayJingle(string name)
    {
        jingleMode = true;
        jingle.clip = MusicClip(name);
        if (jingle.clip == null) return;
        jingle.Play();
        Played.TryGetValue(name, out int n);
        Played[name] = n + 1;
    }

    void OnStateChanged(GameState state)
    {
        if (state == GameState.Victory) PlayJingle("jingle_victoria");
        else if (state == GameState.Defeat) PlayJingle("jingle_derrota");
        else Play("pausa");
    }

    // ---------- Ciclo de vida ----------

    void Awake()
    {
        Instance = this;
        ReadVolume();
        music = NewSource(true);
        fadingOut = NewSource(true);
        jingle = NewSource(false);
        voices = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++) voices[i] = NewSource(false);

        // Agrupa las variantes: "espada_1" y "espada_2" se piden como "espada".
        foreach (var clip in Resources.LoadAll<AudioClip>("Audio/Efectos"))
        {
            string key = clip.name;
            int cut = key.LastIndexOf('_');
            if (cut > 0 && int.TryParse(key.Substring(cut + 1), out _)) key = key.Substring(0, cut);
            if (!effects.TryGetValue(key, out var list)) effects[key] = list = new List<AudioClip>();
            list.Add(clip);
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (hooked != null) hooked.OnStateChanged -= OnStateChanged;
        if (Instance == this) Instance = null;
    }

    AudioSource NewSource(bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;
        return s;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        jingleMode = false;
        jingle.Stop();
        if (CurrentMusic == "musica_jefe") SwitchMusic(null);
        menu = FindAnyObjectByType<MainMenu>();
        if (hooked != null) hooked.OnStateChanged -= OnStateChanged;
        hooked = GameManager.Instance;
        if (hooked != null) hooked.OnStateChanged += OnStateChanged;

        // Todos los botones de la escena suenan al pasar el mouse y al hacer clic.
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include))
            if (!b.TryGetComponent<UIButtonSound>(out _)) b.gameObject.AddComponent<UIButtonSound>();

        VolumePanel.AddToScene(menu, LevelHUD.Instance);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) Muted = !Muted;

        string wanted = DesiredMusic();
        if (wanted != CurrentMusic) SwitchMusic(wanted);

        bool paused = hooked != null && hooked.State == GameState.Paused;
        musicFade = Mathf.MoveTowards(musicFade, 1f, Time.unscaledDeltaTime / FadeTime);
        float target = musicVolume * (paused ? 0.4f : 1f);
        music.volume = target * musicFade;
        fadingOut.volume = Mathf.Min(fadingOut.volume, target * (1f - musicFade));
        if (musicFade >= 1f && fadingOut.isPlaying) fadingOut.Stop();
        jingle.volume = Mathf.Min(1f, musicVolume * 1.3f);
    }
}

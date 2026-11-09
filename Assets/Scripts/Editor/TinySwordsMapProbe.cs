using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Dibuja el mapa de la escena abierta como texto en Logs/TinySwordsMap.txt (menú o archivo "TinySwordsProbe.txt").
// '.' suelo caminable   '#' bloqueado por collider   '~' sin suelo (agua/vacío)   'C' castillo   'P' héroe
[InitializeOnLoad]
public static class TinySwordsMapProbe
{
    const string TriggerFile = "TinySwordsProbe.txt";
    static double nextCheck;

    static TinySwordsMapProbe()
    {
        EditorApplication.update += () =>
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1;
            if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(TriggerFile))
            {
                File.Delete(TriggerFile);
                Run();
            }
        };
    }

    [MenuItem("Tiny Swords/Herramientas/Dibujar mapa en texto")]
    public static void Run()
    {
        var tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude);
        var ground = tilemaps.Where(t => t.name == "Flat" || t.name.StartsWith("Elevations")).ToArray();
        var bounds = new BoundsInt();
        bool first = true;
        foreach (var t in tilemaps)
        {
            t.CompressBounds();
            if (t.cellBounds.size.x == 0) continue;
            if (first) { bounds = t.cellBounds; first = false; continue; }
            bounds.xMin = Mathf.Min(bounds.xMin, t.cellBounds.xMin);
            bounds.yMin = Mathf.Min(bounds.yMin, t.cellBounds.yMin);
            bounds.xMax = Mathf.Max(bounds.xMax, t.cellBounds.xMax);
            bounds.yMax = Mathf.Max(bounds.yMax, t.cellBounds.yMax);
        }

        Physics2D.SyncTransforms();
        var castle = Object.FindAnyObjectByType<Castle>();
        var player = Object.FindAnyObjectByType<Player>();
        var grid = tilemaps.Length > 0 ? tilemaps[0].layoutGrid : null;

        var sb = new StringBuilder();
        sb.AppendLine($"Celdas x {bounds.xMin}..{bounds.xMax - 1}, y {bounds.yMin}..{bounds.yMax - 1} (fila de arriba = y mayor). Cada caracter = 1 celda.");
        sb.AppendLine("Tilemaps: " + string.Join(", ", tilemaps.Select(t => $"{t.name}[{t.cellBounds.xMin},{t.cellBounds.yMin} -> {t.cellBounds.xMax},{t.cellBounds.yMax}] col:{(t.GetComponent<Collider2D>() != null)}")));
        for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
        {
            sb.Append($"{y,4} ");
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                Vector2 world = grid != null ? (Vector2)grid.GetCellCenterWorld(cell) : new Vector2(x + 0.5f, y + 0.5f);
                char c;
                if (castle != null && Vector2.Distance(world, castle.transform.position) < 0.7f) c = 'C';
                else if (player != null && Vector2.Distance(world, player.transform.position) < 0.7f) c = 'P';
                else if (!ground.Any(t => t.HasTile(cell))) c = '~';
                else if (Physics2D.OverlapCircleAll(world, 0.2f).Any(col => !col.isTrigger && col.GetComponentInParent<Health>() == null && col.GetComponentInParent<ResourceNode>() == null)) c = '#';
                else c = '.';
                sb.Append(c);
            }
            sb.AppendLine();
        }
        sb.Append("     ");
        for (int x = bounds.xMin; x < bounds.xMax; x++) sb.Append(Mathf.Abs(x) % 10);
        sb.AppendLine();
        File.WriteAllText("Logs/TinySwordsMap.txt", sb.ToString());
        Debug.Log("Tiny Swords: mapa escrito en Logs/TinySwordsMap.txt");
    }
}

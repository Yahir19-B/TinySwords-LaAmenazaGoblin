using UnityEditor;
using UnityEngine;

// Ajustes de importación del audio del juego: efectos listos al instante, música comprimida.
public class TinySwordsAudioImport : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;
        var importer = (AudioImporter)assetImporter;
        bool music = assetPath.Contains("/Musica/");
        importer.forceToMono = true;
        importer.loadInBackground = false;
        var s = importer.defaultSampleSettings;
        // Decompress On Load también en la música: en WebGL evita cortes al repetir la pista.
        s.loadType = AudioClipLoadType.DecompressOnLoad;
        s.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
        s.quality = 0.6f;
        importer.defaultSampleSettings = s;
    }
}

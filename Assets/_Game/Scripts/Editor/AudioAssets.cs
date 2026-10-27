using System.Collections.Generic;
using BeforeItWasYou.Audio;
using BeforeItWasYou.Player;
using UnityEditor;
using UnityEngine;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>Importación y acceso a los clips de Assets/_Game/Audio (orígenes en REGISTRO_ASSETS.csv).</summary>
    public static class AudioAssets
    {
        public const string Folder = "Assets/_Game/Audio";

        public static void Configure()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                var settings = importer.defaultSampleSettings;
                var ambience = path.Contains("/Ambience/");
                var wanted = ambience ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                if (settings.loadType == wanted && settings.compressionFormat == AudioCompressionFormat.Vorbis) continue;
                settings.loadType = wanted;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = ambience ? 0.6f : 0.75f;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = !ambience;
                importer.SaveAndReimport();
            }
        }

        public static AudioClip Clip(string relative) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{relative}.ogg");

        public static FootstepAudio.Bank[] FootstepBanks()
        {
            var banks = new List<FootstepAudio.Bank>();
            foreach (var (surface, name) in new[] { (Surface.Concrete, "Concrete"), (Surface.Grass, "Grass"), (Surface.Gravel, "Gravel"),
                         (Surface.Tile, "Tile"), (Surface.Wood, "Wood"), (Surface.Metal, "Metal"), (Surface.Carpet, "Grass") })
            {
                var clips = new List<AudioClip>();
                for (var i = 0; i < 6; i++)
                {
                    var clip = Clip($"Footsteps/STEP_{name}_{i}");
                    if (clip != null) clips.Add(clip);
                }
                banks.Add(new FootstepAudio.Bank { surface = surface, clips = clips.ToArray() });
            }
            return banks.ToArray();
        }
    }
}

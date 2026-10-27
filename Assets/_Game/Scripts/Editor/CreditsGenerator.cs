using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>Genera los créditos de terceros a partir de Docs/Proyecto/REGISTRO_ASSETS.csv (solo assets integrados).</summary>
    public static class CreditsGenerator
    {
        public const string OutputPath = "Assets/_Game/Data/Credits.txt";
        private const string RegisterPath = "Docs/Proyecto/REGISTRO_ASSETS.csv";

        public static TextAsset Generate()
        {
            EditorUtil.EnsureFolder("Assets/_Game/Data");
            var authors = new SortedSet<string>();
            if (File.Exists(RegisterPath))
            {
                foreach (var line in File.ReadAllLines(RegisterPath).Skip(1))
                {
                    var cells = SplitCsv(line);
                    if (cells.Count < 10 || !cells[0].StartsWith("PH-") || !cells[9].StartsWith("integrado")) continue;
                    foreach (var author in cells[3].Split(';')) authors.Add(author.Trim());
                }
            }
            var text = new StringBuilder();
            if (authors.Count > 0)
            {
                text.Append("TEXTURAS, MODELOS Y CIELO\n");
                text.Append("Poly Haven (polyhaven.com) · CC0 1.0\n");
                text.Append(string.Join(", ", authors) + "\n\n");
            }
            File.WriteAllText(OutputPath, text.ToString());
            AssetDatabase.ImportAsset(OutputPath);
            return AssetDatabase.LoadAssetAtPath<TextAsset>(OutputPath);
        }

        private static List<string> SplitCsv(string line)
        {
            var cells = new List<string>();
            var current = new StringBuilder();
            var quoted = false;
            foreach (var c in line)
            {
                if (c == '"') quoted = !quoted;
                else if (c == ',' && !quoted)
                {
                    cells.Add(current.ToString());
                    current.Clear();
                }
                else current.Append(c);
            }
            cells.Add(current.ToString());
            return cells;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Batch-translate GameObject names in the active scene or selected assets (Scenes/Prefabs) from Spanish to English.
// Safe: operates only on names; does not touch components, GUIDs, or references.
// Usage:
//  - Open your Hogwarts scene.
//  - Tools → Localization → Translate Object Names (Active Scene).
//  - Optionally: select one or more Prefabs/Scenes in Project, then Tools → Localization → Translate Selected Assets.
// Undo is supported.
namespace OpenHogwarts.Editor.Localization
{
    public static class ObjectNameTranslator
    {
        // Minimal dictionary: extend as needed. Keys lowercase for case-insensitive matching.
        static readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            {"escuela", "school"},
            {"castillo", "castle"},
            {"torre", "tower"},
            {"puerta", "door"},
            {"puertas", "doors"},
            {"ventana", "window"},
            {"ventanas", "windows"},
            {"pared", "wall"},
            {"paredes", "walls"},
            {"suelo", "floor"},
            {"techo", "ceiling"},
            {"pasillo", "hallway"},
            {"escalera", "stair"},
            {"escaleras", "stairs"},
            {"entrada", "entrance"},
            {"salida", "exit"},
            {"biblioteca", "library"},
            {"gran comedor", "great hall"},
            {"comedor", "dining hall"},
            {"patio", "courtyard"},
            {"jardin", "garden"},
            {"baño", "bathroom"},
            {"baños", "bathrooms"},
            {"dormitorio", "dormitory"},
            {"dormitorios", "dormitories"},
            {"profesor", "professor"},
            {"profesora", "professor"},
            {"aula", "classroom"},
            {"aulas", "classrooms"},
            {"laboratorio", "laboratory"},
            {"laboratorios", "laboratories"},
            {"puente", "bridge"},
            {"bosque", "forest"},
            {"lago", "lake"},
            {"mazmorra", "dungeon"},
            {"mazmorras", "dungeons"},
            {"oficina", "office"},
            {"oficinas", "offices"},
            {"sala", "room"},
            {"salas", "rooms"},
            {"gradas", "stands"},
            {"quidditch", "quidditch"},
            {"entrada principal", "main entrance"},
            {"escalera principal", "main stairs"},
            {"corredor", "corridor"},
            {"puerta principal", "main door"},
            {"puerta lateral", "side door"},
            {"torre norte", "north tower"},
            {"torre sur", "south tower"},
            {"torre este", "east tower"},
            {"torre oeste", "west tower"},
            {"patio interior", "inner courtyard"},
        };

        [MenuItem("Tools/Localization/Translate Object Names (Active Scene)")]
        public static void TranslateActiveScene()
        {
            var all = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            var targets = new List<GameObject>();
            foreach (var root in all)
                targets.AddRange(root.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject));

            TranslateObjects(targets, "Active Scene");
        }

        [MenuItem("Tools/Localization/Translate Selected Assets")]
        public static void TranslateSelectedAssets()
        {
            var paths = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var gos = new List<GameObject>();
            foreach (var p in paths)
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(p);
                if (obj is GameObject go)
                {
                    gos.AddRange(go.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject));
                }
                else if (p.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                    if (prefab) gos.AddRange(prefab.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject));
                }
            }
            TranslateObjects(gos, "Selected Assets");
        }

        static void TranslateObjects(IEnumerable<GameObject> objects, string context)
        {
            int renamed = 0;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            foreach (var go in objects)
            {
                var newName = TranslateName(go.name);
                if (!string.IsNullOrEmpty(newName) && newName != go.name)
                {
                    Undo.RecordObject(go, "Translate Object Name");
                    go.name = newName;
                    renamed++;
                }
            }
            Undo.CollapseUndoOperations(group);
            Debug.Log($"[ObjectNameTranslator] Renamed {renamed} objects ({context}).");
            // Save scenes if we changed anything in active scene
            if (renamed > 0 && UnityEngine.SceneManagement.SceneManager.GetActiveScene().isLoaded)
            {
                EditorSceneManager.MarkAllScenesDirty();
            }
        }

        static string TranslateName(string original)
        {
            if (string.IsNullOrWhiteSpace(original)) return original;
            string lower = original.ToLowerInvariant();

            // Exact map first
            if (Map.TryGetValue(lower, out var replacement))
            {
                return PreserveCase(original, replacement);
            }

            // Token-wise replacement (split on spaces/underscores/dashes)
            var tokens = SplitTokens(original);
            bool changed = false;
            for (int i = 0; i < tokens.Length; i++)
            {
                var tLower = tokens[i].ToLowerInvariant();
                if (Map.TryGetValue(tLower, out var repl))
                {
                    tokens[i] = PreserveCase(tokens[i], repl);
                    changed = true;
                }
            }
            if (!changed) return original;

            return string.Join("", tokens);
        }

        static string[] SplitTokens(string s)
        {
            // Keep delimiters by splitting and re-inserting them so we don't change formatting
            var list = new List<string>();
            int i = 0;
            while (i < s.Length)
            {
                if (char.IsLetterOrDigit(s[i]))
                {
                    int j = i + 1;
                    while (j < s.Length && char.IsLetterOrDigit(s[j])) j++;
                    list.Add(s.Substring(i, j - i));
                    i = j;
                }
                else
                {
                    list.Add(s[i].ToString());
                    i++;
                }
            }
            return list.ToArray();
        }

        static string PreserveCase(string originalToken, string replacement)
        {
            // Simple case preservation: if original is all caps, make replacement caps; if title case, capitalize
            if (originalToken.All(char.IsUpper)) return replacement.ToUpperInvariant();
            if (char.IsUpper(originalToken.FirstOrDefault())) return char.ToUpperInvariant(replacement.FirstOrDefault()) + replacement.Substring(1);
            return replacement.ToLowerInvariant();
        }
    }
}

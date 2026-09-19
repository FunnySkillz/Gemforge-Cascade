using System;
using System.Collections.Generic;
using UnityEngine;

namespace GemforgeCascade.Core
{
    public static class LevelCatalog
    {
        private const string ResourcePath = "Levels";

        public static TextAsset[] LoadAll()
        {
            TextAsset[] files = Resources.LoadAll<TextAsset>(ResourcePath);
            if (files == null || files.Length == 0)
                return Array.Empty<TextAsset>();

            var valid = new List<TextAsset>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            Array.Sort(files, (a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (TextAsset file in files)
            {
                if (file == null || !file.name.StartsWith("level-", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(file.text))
                    continue;

                try
                {
                    var candidate = Parse(file);
                    if (!ids.Add(candidate.id))
                    {
                        Debug.LogWarning($"Skipping duplicate level ID '{candidate.id}' in '{file.name}'.");
                        continue;
                    }
                    valid.Add(file);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Skipping level '{file.name}': {exception.Message}");
                }
            }

            valid.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return valid.ToArray();
        }

        public static LevelDefinition Parse(TextAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            LevelDefinition level = JsonUtility.FromJson<LevelDefinition>(asset.text);
            if (level == null)
                throw new InvalidOperationException($"Unable to parse level asset '{asset.name}'.");

            LevelDefinition upgraded = GameDataMigrations.Upgrade(level);
            var validation = LevelValidator.Validate(upgraded);
            if (!validation.IsValid)
            {
                var messages = new List<string>();
                foreach (var issue in validation.Issues) messages.Add($"{issue.Code}: {issue.Message}");
                string issues = string.Join("; ", messages);
                throw new InvalidOperationException($"Level '{asset.name}' failed validation: {issues}");
            }

            return upgraded;
        }
    }
}

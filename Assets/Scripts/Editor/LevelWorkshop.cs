using System.IO;
using GemforgeCascade.Core;
using UnityEditor;
using UnityEngine;

namespace GemforgeCascade.Editor
{
    public sealed class LevelWorkshop : EditorWindow
    {
        private TextAsset asset;
        private LevelDefinition level;
        private BoardModel preview;
        private LevelValidationResult validation;
        private Vector2 scroll;
        private int color;
        private SpecialKind special;
        private int crystalHits;
        private bool paintLayers;

        [MenuItem("Gemforge/Level Workshop")]
        private static void Open() => GetWindow<LevelWorkshop>("Level Workshop");

        private void OnGUI()
        {
            var selected = (TextAsset)EditorGUILayout.ObjectField("Level JSON", asset, typeof(TextAsset), false);
            if (selected != asset)
            {
                asset = selected;
                level = null;
                if (asset != null)
                {
                    try { level = GameDataMigrations.Upgrade(JsonUtility.FromJson<LevelDefinition>(asset.text)); RefreshPreview(); }
                    catch (System.Exception error) { Debug.LogError(error.Message); }
                }
            }
            if (level == null) return;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            level.id = EditorGUILayout.TextField("ID", level.id);
            level.title = EditorGUILayout.TextField("Title", level.title);
            level.moves = EditorGUILayout.IntField("Moves", level.moves);
            level.targetScore = EditorGUILayout.IntField("Legacy score target", level.targetScore);
            level.seed = EditorGUILayout.IntField("Refill seed", level.seed);
            EditorGUILayout.LabelField($"{level.width} x {level.height}, {level.colorCount} colors");
            foreach (var objective in level.objectives)
            {
                EditorGUILayout.BeginHorizontal();
                objective.kind = (int)(ObjectiveKind)EditorGUILayout.EnumPopup((ObjectiveKind)objective.kind);
                objective.colorIndex = EditorGUILayout.IntField(objective.colorIndex, GUILayout.Width(40));
                objective.target = EditorGUILayout.IntField(objective.target, GUILayout.Width(70));
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Add Score Goal"))
            {
                var items = new System.Collections.Generic.List<ObjectiveData>(level.objectives);
                items.Add(new ObjectiveData { kind = 0, target = 100 });
                level.objectives = items.ToArray();
            }
            paintLayers = EditorGUILayout.Toggle("Paint crystals", paintLayers);
            if (paintLayers) crystalHits = EditorGUILayout.IntSlider("Durability", crystalHits, 0, 3);
            else
            {
                color = EditorGUILayout.IntSlider("Color", color, 0, level.colorCount - 1);
                special = (SpecialKind)EditorGUILayout.EnumPopup("Special", special);
            }
            if (preview != null)
            {
                for (int y = level.height - 1; y >= 0; y--)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int x = 0; x < level.width; x++)
                    {
                        int id = y * level.width + x;
                        var piece = preview.Cells[x, y];
                        string marker = piece.Special == SpecialKind.None ? "" : piece.Special.ToString().Substring(0, 1);
                        if (GUILayout.Button($"{piece.ColorIndex}{marker}\n{preview.Layers[x, y].Durability}", GUILayout.Width(42), GUILayout.Height(40)))
                        {
                            BakeOpening();
                            if (paintLayers) level.startingLayers[id] = new CellLayerData { kind = crystalHits == 0 ? 0 : 1, durability = crystalHits };
                            else level.startingPieces[id] = new PieceData(new BoardPiece(color, special));
                            RefreshPreview();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            if (GUILayout.Button("Bake Seeded Opening")) { RefreshPreview(); BakeOpening(); }
            if (GUILayout.Button("Validate")) RefreshPreview();
            if (validation != null)
            {
                EditorGUILayout.LabelField($"Legal opening moves: {validation.LegalMoveCount}");
                foreach (var issue in validation.Issues)
                    EditorGUILayout.HelpBox($"{issue.Code}: {issue.Message}", issue.Severity == ValidationSeverity.Error ? MessageType.Error : MessageType.Warning);
            }
            if (GUILayout.Button("Save Validated Copy"))
            {
                RefreshPreview();
                if (validation.IsValid)
                {
                    string path = EditorUtility.SaveFilePanelInProject("Save Level", level.id, "json", "Choose a level JSON path", "Assets/Resources/Levels");
                    if (!string.IsNullOrEmpty(path))
                    {
                        File.WriteAllText(path, JsonUtility.ToJson(level, true));
                        AssetDatabase.Refresh();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void RefreshPreview()
        {
            validation = LevelValidator.Validate(level);
            try { preview = BoardModel.FromLevel(level); }
            catch { preview = null; }
        }
        private void BakeOpening()
        {
            if (preview == null) return;
            var snapshot = preview.CreateSnapshot(level.id);
            level.startingPieces = snapshot.pieces;
            level.startingLayers = snapshot.layers;
        }
    }
}

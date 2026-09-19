using GemforgeCascade.Core;
using GemforgeCascade.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GemforgeCascade.Editor
{
    public static class GemforgeSceneBuilder
    {
        [MenuItem("Gemforge/Build Main Scene")]
        public static void BuildMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.07f, 0.10f);
            camera.orthographic = true;
            camera.orthographicSize = 5.8f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var gemPrefab = CreateGemPrefab();
            var boardObject = new GameObject("Gem Board");
            var board = boardObject.AddComponent<GemBoard>();
            SetPrivateField(board, "gemPrefab", gemPrefab);

            var canvas = CreateCanvas();
            var title = CreateText(canvas.transform, "Title", "Gemforge Cascade", new Vector2(0f, -24f), 42);
            title.alignment = TextAlignmentOptions.Center;
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.sizeDelta = new Vector2(0f, 64f);

            var scoreText = CreateText(canvas.transform, "Score", "Score 0", new Vector2(28f, -86f), 28);
            scoreText.alignment = TextAlignmentOptions.Left;
            scoreText.rectTransform.anchorMin = new Vector2(0f, 1f);
            scoreText.rectTransform.anchorMax = new Vector2(0f, 1f);
            scoreText.rectTransform.pivot = new Vector2(0f, 1f);
            scoreText.rectTransform.sizeDelta = new Vector2(280f, 42f);

            var movesText = CreateText(canvas.transform, "Moves", "Moves 0", new Vector2(-28f, -86f), 28);
            movesText.alignment = TextAlignmentOptions.Right;
            movesText.rectTransform.anchorMin = new Vector2(1f, 1f);
            movesText.rectTransform.anchorMax = new Vector2(1f, 1f);
            movesText.rectTransform.pivot = new Vector2(1f, 1f);
            movesText.rectTransform.sizeDelta = new Vector2(280f, 42f);

            var hud = canvas.AddComponent<HudView>();
            SetPrivateField(hud, "board", board);
            SetPrivateField(hud, "scoreText", scoreText);
            SetPrivateField(hud, "movesText", movesText);

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            AssetDatabase.SaveAssets();
        }

        private static Gem CreateGemPrefab()
        {
            const string prefabPath = "Assets/Prefabs/Gem.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<Gem>(prefabPath);
            if (existing != null)
            {
                return existing;
            }

            var gemObject = new GameObject("Gem");
            var renderer = gemObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateCircleSprite();
            renderer.sortingOrder = 1;
            gemObject.AddComponent<CircleCollider2D>();
            var gem = gemObject.AddComponent<Gem>();
            SetPrivateField(gem, "spriteRenderer", renderer);

            var prefab = PrefabUtility.SaveAsPrefabAsset(gemObject, prefabPath).GetComponent<Gem>();
            Object.DestroyImmediate(gemObject);
            return prefab;
        }

        private static Sprite CreateCircleSprite()
        {
            const string spritePath = "Assets/Art/GemCircle.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existing != null)
            {
                return existing;
            }

            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    var rawRadius = distance / (size * 0.42f);
                    var normalized = Mathf.Clamp01(rawRadius);
                    var alpha = rawRadius <= 1f ? 1f : 0f;
                    var shine = 1f - normalized * 0.45f;
                    texture.SetPixel(x, y, new Color(shine, shine, shine, alpha));
                }
            }

            texture.Apply();
            System.IO.File.WriteAllBytes(spritePath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(spritePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 96;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("HUD Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, Vector2 anchoredPosition, int size)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = new Color(0.92f, 0.96f, 1f);
            label.rectTransform.anchoredPosition = anchoredPosition;
            return label;
        }

        private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
        {
            var field = typeof(TTarget).GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}

using GemforgeCascade.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GemforgeCascade.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        private GameManager game;
        private Text score, moves, target, result;
        private GameObject endPanel;
        private Font font;

        public void Initialize(GameManager manager)
        {
            game = manager;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 900);
            scaler.matchWidthOrHeight = 0;
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Transform root = canvasObject.transform;
            Label(root, "Gemforge Cascade", 30, new Vector2(0, 0.91f), new Vector2(1, 1));
            score = Label(root, "", 24, new Vector2(0, 0.81f), new Vector2(0.34f, 0.91f));
            target = Label(root, "", 24, new Vector2(0.34f, 0.81f), new Vector2(0.68f, 0.91f));
            moves = Label(root, "", 24, new Vector2(0.68f, 0.81f), new Vector2(1, 0.91f));
            endPanel = new GameObject("End Game Panel", typeof(RectTransform), typeof(Image));
            endPanel.transform.SetParent(root, false);
            Stretch(endPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            endPanel.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.04f, 0.96f);
            result = Label(endPanel.transform, "", 36, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.64f));
            var buttonObject = new GameObject("Restart", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(endPanel.transform, false);
            Stretch(buttonObject.GetComponent<RectTransform>(), new Vector2(0.3f, 0.35f), new Vector2(0.7f, 0.46f));
            buttonObject.GetComponent<Image>().color = new Color(0.1f, 0.5f, 0.36f);
            buttonObject.GetComponent<Button>().onClick.AddListener(game.Restart);
            Label(buttonObject.transform, "Restart", 26, Vector2.zero, Vector2.one);
            game.Changed += Refresh;
            Refresh();
        }

        private Text Label(Transform parent, string value, int size, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(value.Length > 0 ? value : "Stat", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            Stretch(text.rectTransform, min, max);
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }

        private void Refresh()
        {
            score.text = $"Score: {game.Score}";
            target.text = $"Target: {game.Target}";
            moves.text = $"Moves: {game.Moves}";
            endPanel.SetActive(game.State != GameState.Playing);
            result.text = (game.State == GameState.Won ? "Level Complete" : "Game Over") + $"\nScore: {game.Score}";
        }

        private void OnDestroy() { if (game != null) game.Changed -= Refresh; }
    }
}
